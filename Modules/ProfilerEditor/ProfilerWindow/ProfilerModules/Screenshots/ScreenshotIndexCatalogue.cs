// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    // Two index spaces per entry: LogicalFrame is the frame the screenshot DEPICTS (its user-facing
    // identity), EmissionFrame is the frame the readback landed on and the only index valid for
    // ProfilerDriver reads. Legacy captures carry no offset metadata, so LogicalFrame falls back to
    // EmissionFrame.
    internal readonly struct ScreenshotFrame
    {
        public readonly int LogicalFrame;
        public readonly int EmissionFrame;

        public ScreenshotFrame(int logicalFrame, int emissionFrame)
        {
            LogicalFrame = logicalFrame;
            EmissionFrame = emissionFrame;
        }
    }

    // Window-scoped service owning the canonical list of frames that have screenshots, scanned once
    // per quiet period rather than once per consuming strip.
    internal sealed class ScreenshotIndexCatalogue : IDisposable
    {
        // Rebuilding on every NewProfilerFrameRecorded would re-hit the native scan every frame.
        const long k_RecordingQuietPeriodMs = 1500;

        // Captures written at this version re-emitted the same screenshot on every frame until the
        // next real capture; the fix landed with the metadata version bump.
        const int k_BuggyRepeatMetadataVersion = (int)ProfilingSessionMetaDataEntryVersion.ScreenshotVersion;
        // Stride when collapsing those repeats: the then-default rate, which the few affected users
        // (6000.3.0a5-b6, 6000.4.0a1-a3) could only have changed via script.
        const int k_BuggyCaptureScreenshotInterval = 15;
        // Cheap enough to decode, large enough to tell two screenshots apart.
        const int k_RepeatCompareImageSize = 32;

        readonly ProfilerWindow m_ProfilerWindow;
        readonly List<ScreenshotFrame> m_Frames = new List<ScreenshotFrame>();
        int m_LastScannedFrameIndex = -1;
        IVisualElementScheduledItem m_PendingRefresh;
        IVisualElementScheduledItem m_PendingBurstEndCheck;
        bool m_IsDisposed;
        // 0 for "never". Pause-aware, so a debugger break can't strand IsRecordingBurst true.
        double m_LastFrameRecordedTime;
        // Keeps BurstEnded to exactly one raise per recording run.
        bool m_BurstEndPending;
        // -1 means "never reported", so the first Refresh always notifies.
        int m_LastNotifiedDisplayedFrameCount = -1;

        public IReadOnlyList<ScreenshotFrame> Frames => m_Frames;

        // Report this rather than Frames.Count, or the count overstates what's on screen.
        public int DisplayedFrameCount
        {
            get => m_Frames.Count - LowerBoundByLogicalFrame(m_Frames, FirstSelectableFrameIndex());
        }

        public event Action Changed;
        // Raised at recording-start, not at the first throttle.
        public event Action BurstStarted;
        // Recording stopped: raised as it happens for the stops we can see (the recording toggle and
        // capture loads, via EndRecordingBurstNow), otherwise a quiet period after the final throttled
        // Refresh. The details strip drops its placeholder here, so a delay is a stale banner.
        public event Action BurstEnded;

        // Lets consumers pick a low-disturbance append layout over a full re-pick. Goes false as soon
        // as a stop is observed, otherwise once the quiet period elapses.
        public bool IsRecordingBurst
        {
            get
            {
                if (m_LastFrameRecordedTime <= 0.0)
                    return false;
                var elapsedMs = (EditorApplication.timeSinceStartup - m_LastFrameRecordedTime) * 1000.0;
                return elapsedMs < k_RecordingQuietPeriodMs;
            }
        }

        public ScreenshotIndexCatalogue(ProfilerWindow profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;

            ProfilerDriver.profileLoaded += OnDataLoaded;
            ProfilerDriver.profileCleared += OnDataCleared;
            ProfilerDriver.NewProfilerFrameRecorded += OnNewFrameRecorded;
            // Reports a toolbar stop as it happens; capture loads stop recording without it, and are
            // covered by OnDataLoaded. See EndRecordingBurstNow.
            if (m_ProfilerWindow != null)
                m_ProfilerWindow.recordingStateChanged += OnRecordingStateChanged;

            // Subscribers attach after this returns, so they read Frames directly for initial state.
            Refresh();
        }

        public void Refresh()
        {
            if (m_IsDisposed)
                return;

            var currentFirst = ProfilerDriver.firstFrameIndex;
            var currentLast = ProfilerDriver.lastFrameIndex;

            if (currentFirst < 0 || currentLast < 0)
            {
                if (m_Frames.Count > 0 || m_LastScannedFrameIndex != -1)
                {
                    m_Frames.Clear();
                    m_LastScannedFrameIndex = -1;
                    SaveToCache();
                    RaiseChanged();
                }
                return;
            }

            var changed = false;

            if (m_LastScannedFrameIndex == -1)
            {
                ScanAllFramesIntoList(currentFirst, currentLast);
                m_LastScannedFrameIndex = currentLast;
                changed = true;
            }
            else
            {
                changed |= PruneBefore(currentFirst);

                if (currentLast > m_LastScannedFrameIndex)
                {
                    var newFrames = ProfilerDriver.GetFramesWithScreenshots(m_LastScannedFrameIndex + 1, currentLast);
                    if (newFrames.Length > 0)
                    {
                        AppendEmissionFrames(newFrames);
                        SortByLogicalFrame();
                        changed = true;
                    }
                    m_LastScannedFrameIndex = currentLast;
                }
                else if (currentLast < m_LastScannedFrameIndex)
                {
                    // Capture got shorter without a clear event (a smaller capture file was loaded).
                    ScanAllFramesIntoList(currentFirst, currentLast);
                    m_LastScannedFrameIndex = currentLast;
                    changed = true;
                }
            }

            if (!changed && m_LastNotifiedDisplayedFrameCount != DisplayedFrameCount)
                changed = true;

            if (changed)
            {
                SaveToCache();
                RaiseChanged();
            }
        }

        void ScanAllFramesIntoList(int currentFirst, int currentLast)
        {
            m_Frames.Clear();

            if (ReadScreenshotMetadataVersion(currentLast) == k_BuggyRepeatMetadataVersion)
            {
                // The cache holds the already-collapsed list, which can't be tail-extended safely.
                AppendEmissionFrames(ProfilerDriver.GetFramesWithScreenshots(currentFirst, currentLast));
                SortByLogicalFrame();
                DiscardBuggyRepeatScreenshots();
                return;
            }

            // The capture can keep recording while the window is closed, so the cache never covers
            // everything up to currentLast.
            var scanFrom = currentFirst;
            if (TryPopulateFromCache(currentFirst, currentLast) && m_Frames.Count > 0)
                scanFrom = HighestEmissionFrame() + 1;
            if (scanFrom <= currentLast)
                AppendEmissionFrames(ProfilerDriver.GetFramesWithScreenshots(scanFrom, currentLast));
            SortByLogicalFrame();
        }

        // Session metadata is global, so any valid frame carries it. -1 when unavailable.
        static int ReadScreenshotMetadataVersion(int frameIndex)
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (!frameData.valid)
                    return -1;
                if (1 > frameData.GetSessionMetaDataCount(
                        ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)ProfilingSessionMetaDataEntry.Version))
                    return -1;
                return frameData.GetProfilingSessionMetaData<int>(ProfilingSessionMetaDataEntry.Version);
            }
        }

        // Collapses V1's every-frame repeats to one entry per real screenshot: sample one frame per
        // interval, keeping it only when its image differs from the last kept one.
        void DiscardBuggyRepeatScreenshots()
        {
            if (m_Frames.Count <= 1)
                return;

            var kept = new List<ScreenshotFrame>((m_Frames.Count / k_BuggyCaptureScreenshotInterval) + 1);
            byte[] lastKeptImage = null;
            var nextSampleEmission = int.MinValue;

            foreach (var frame in m_Frames)
            {
                if (lastKeptImage != null && frame.EmissionFrame < nextSampleEmission)
                    continue;
                nextSampleEmission = frame.EmissionFrame + k_BuggyCaptureScreenshotInterval;

                if (!TryReadScaledImage(frame.EmissionFrame, out var image))
                {
                    // Keep unreadable frames rather than risk dropping a real one.
                    kept.Add(frame);
                    lastKeptImage = null;
                    continue;
                }

                if (lastKeptImage == null || !image.AsSpan().SequenceEqual(lastKeptImage))
                {
                    kept.Add(frame);
                    lastKeptImage = image;
                }
            }

            m_Frames.Clear();
            m_Frames.AddRange(kept);
        }

        // Fixed target size, so SequenceEqual comparisons are well-defined.
        static bool TryReadScaledImage(int emissionFrame, out byte[] image)
        {
            return TryScaleScreenshot(emissionFrame, k_RepeatCompareImageSize, k_RepeatCompareImageSize,
                       out image, out _, out _) && image != null;
        }

        void AppendEmissionFrames(int[] emissionFrames)
        {
            for (var i = 0; i < emissionFrames.Length; i++)
                m_Frames.Add(BuildFrame(emissionFrames[i]));
        }

        static ScreenshotFrame BuildFrame(int emissionFrame)
        {
            // Absent metadata leaves this 0 — the identity fallback legacy captures need.
            TryReadSingleIntMetadata(emissionFrame,
                ProfilingSessionMetaDataEntry.FramesSinceScreenshotRequested, out int framesSinceRequested);
            return new ScreenshotFrame(emissionFrame - framesSinceRequested, emissionFrame);
        }

        void SortByLogicalFrame()
        {
            // Varying readback latency can invert a pair (5 emitted on 8, 6 emitted on 7), so
            // emission order is not depicted order. The lookups below assume the latter.
            m_Frames.Sort((a, b) => a.LogicalFrame.CompareTo(b.LogicalFrame));
        }

        // internal for tests: populating m_Frames for real needs a play-mode capture.
        internal void SetFramesForTests(IEnumerable<ScreenshotFrame> frames)
        {
            m_Frames.Clear();
            m_Frames.AddRange(frames);
            SortByLogicalFrame();
        }

        // internal for tests: entering a burst for real needs live frame callbacks.
        internal void MarkRecordingBurstForTests()
        {
            m_LastFrameRecordedTime = EditorApplication.timeSinceStartup;
            m_BurstEndPending = true;
        }

        int HighestEmissionFrame()
        {
            var highest = int.MinValue;
            foreach (var frame in m_Frames)
            {
                if (frame.EmissionFrame > highest)
                    highest = frame.EmissionFrame;
            }
            return highest;
        }

        public bool TryGetEmissionFrame(int logicalFrame, out int emissionFrame)
        {
            var index = LowerBoundByLogicalFrame(m_Frames, logicalFrame);
            if (index < m_Frames.Count && m_Frames[index].LogicalFrame == logicalFrame)
            {
                emissionFrame = m_Frames[index].EmissionFrame;
                return true;
            }
            emissionFrame = 0;
            return false;
        }

        // Largest entry with LogicalFrame ≤ logicalFrame. Works on legacy captures too.
        public bool TryGetNearestPriorLogicalFrame(int logicalFrame, out ScreenshotFrame match)
        {
            var index = LowerBoundByLogicalFrame(m_Frames, logicalFrame);
            // Exact hit at index, or the element just before (lower_bound returns the first ≥).
            if (index < m_Frames.Count && m_Frames[index].LogicalFrame == logicalFrame)
            {
                match = m_Frames[index];
                return true;
            }
            if (index > 0)
            {
                match = m_Frames[index - 1];
                return true;
            }
            match = default;
            return false;
        }

        // Resolves which screenshot to display for a requested logical frame: the screenshot captured
        // on that exact frame if one exists, otherwise the most recent prior screenshot still inside
        // the display window. firstDisplayedFrame bounds both, so a screenshot trimmed out of the
        // window is never surfaced. Shared by the large preview and the info panel, so the two can
        // never disagree about which screenshot is on screen.
        public bool TryResolveDisplayedScreenshot(int requestedLogicalFrame, int firstDisplayedFrame, out ScreenshotFrame source)
        {
            return TryResolveDisplayedScreenshot(m_Frames, requestedLogicalFrame, firstDisplayedFrame, out source);
        }

        // Pure resolution over a LogicalFrame-sorted list. Static so the behaviour can be unit tested
        // without a live catalogue (which is populated from the native profiler stream).
        internal static bool TryResolveDisplayedScreenshot(IReadOnlyList<ScreenshotFrame> frames, int requestedLogicalFrame, int firstDisplayedFrame, out ScreenshotFrame source)
        {
            source = default;

            if (frames == null || requestedLogicalFrame < 0)
                return false;

            // Bounded before the lookups so it covers an exact hit too: reducing the frame-count
            // preference can shrink the window under an already-selected frame that has one.
            if (requestedLogicalFrame < firstDisplayedFrame)
                return false;

            var index = LowerBoundByLogicalFrame(frames, requestedLogicalFrame);

            // Exact hit on the requested frame.
            if (index < frames.Count && frames[index].LogicalFrame == requestedLogicalFrame)
            {
                source = frames[index];
                return true;
            }

            // Otherwise the most recent prior screenshot, if it's still inside the display window.
            if (index == 0)
                return false;

            var nearest = frames[index - 1];
            if (nearest.LogicalFrame < firstDisplayedFrame)
                return false;

            source = nearest;
            return true;
        }

        static int LowerBoundByLogicalFrame(IReadOnlyList<ScreenshotFrame> frames, int logicalFrame)
        {
            int lo = 0, hi = frames.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (frames[mid].LogicalFrame < logicalFrame)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        void OnDataLoaded()
        {
            // Fires for both initial loads and shift+load appends; Refresh handles both. Not a stop
            // signal, deliberately: ProfilerDriver.LoadProfile raises this before ProfilerWindow
            // disables recording, and it is public — a load can leave recording running, so ending the
            // burst here would drop the placeholder and the next frame would put it straight back.
            Refresh();
        }

        void OnDataCleared()
        {
            m_LastScannedFrameIndex = -1;
            if (m_Frames.Count > 0)
            {
                m_Frames.Clear();
                SaveToCache();
                RaiseChanged();
            }
        }

        void RaiseChanged()
        {
            m_LastNotifiedDisplayedFrameCount = DisplayedFrameCount;
            Changed?.Invoke();
        }

        void OnNewFrameRecorded(int connectionId, int newFrameIndex)
        {
            if (m_IsDisposed || newFrameIndex <= m_LastScannedFrameIndex)
                return;

            // Tail frames already in flight keep landing after the user stops; stamping on those puts
            // the details strip's placeholder back up. The throttled Refresh below still runs.
            if (ProfilerDriver.enabled)
            {
                // Read the transition before stamping, so BurstStarted fires once per run.
                var wasInBurst = IsRecordingBurst;
                m_LastFrameRecordedTime = EditorApplication.timeSinceStartup;
                m_BurstEndPending = true;
                if (!wasInBurst)
                    BurstStarted?.Invoke();
            }

            var host = m_ProfilerWindow?.rootVisualElement;
            if (host == null)
                return;

            // A throttle, not a debounce: frames after the first are absorbed into the armed window,
            // so a sustained recording still refreshes periodically instead of only at the end.
            if (m_PendingRefresh == null)
            {
                m_PendingRefresh = host.schedule.Execute(() =>
                {
                    m_PendingRefresh = null;
                    Refresh();

                    // Follow-up "did the burst end?" check; a still-active recording no-ops it.
                    if (m_IsDisposed)
                        return;
                    var burstEndHost = m_ProfilerWindow?.rootVisualElement;
                    if (burstEndHost == null)
                        return;
                    m_PendingBurstEndCheck?.Pause();
                    m_PendingBurstEndCheck = burstEndHost.schedule.Execute(() =>
                    {
                        m_PendingBurstEndCheck = null;
                        if (m_IsDisposed)
                            return;
                        if (m_PendingRefresh == null && m_BurstEndPending)
                        {
                            m_BurstEndPending = false;
                            BurstEnded?.Invoke();
                        }
                    });
                    m_PendingBurstEndCheck.ExecuteLater(k_RecordingQuietPeriodMs);
                });
                m_PendingRefresh.ExecuteLater(k_RecordingQuietPeriodMs);
            }
        }

        void OnRecordingStateChanged(bool recording)
        {
            // Trust the driver over the argument: this also fires on connection-target changes with
            // whatever m_Recording currently holds, which can lag the driver when something else set
            // ProfilerDriver.enabled. Winding down a live burst would flash the placeholder off and on.
            if (recording || ProfilerDriver.enabled)
                return;
            EndRecordingBurstNow();
        }

        // Collapses the rest of the quiet period the moment we learn recording has stopped: left to the
        // timers, IsRecordingBurst stays true for a quiet period after the final frame and BurstEnded
        // lands up to another after that — seconds of stale placeholder.
        // internal for tests: the production trigger is ProfilerWindow.recordingStateChanged.
        internal void EndRecordingBurstNow()
        {
            if (m_IsDisposed)
                return;

            // recordingStateChanged also fires with the state unchanged, so this runs while idle.
            if (!m_BurstEndPending)
                return;
            m_BurstEndPending = false;

            m_PendingRefresh?.Pause();
            m_PendingRefresh = null;
            m_PendingBurstEndCheck?.Pause();
            m_PendingBurstEndCheck = null;

            // Refresh with the burst stamp still set, so the strips take their cheap burst path for
            // this Changed and reconcile once from BurstEnded below instead of reloading fully twice
            // — the second reload cancels the details strip's thumbnail load mid-flight.
            Refresh();

            // Cleared before BurstEnded: IsRecordingBurst must read false by the time the strips
            // reconcile, or they keep the placeholder up.
            m_LastFrameRecordedTime = 0.0;
            BurstEnded?.Invoke();
        }

        // internal for tests: otherwise only reachable through live ring-buffer eviction.
        internal bool PruneBefore(int currentFirstFrame)
        {
            // Bound on EmissionFrame: an entry whose metadata frame was evicted is gone regardless
            // of where its LogicalFrame sits.
            var removed = m_Frames.RemoveAll(f => f.EmissionFrame < currentFirstFrame);
            return removed > 0;
        }

        bool TryPopulateFromCache(int firstFrame, int lastFrame)
        {
            var cached = TryGetCachedFrames();
            if (cached == null || cached.Count == 0)
                return false;

            // Verify EVERY in-range cached frame: a single-frame probe accepts a stale cache whenever
            // two captures share one screenshot frame index, ghosting the old one onto the timeline.
            var sawInRange = false;
            foreach (var frame in cached)
            {
                if (frame.EmissionFrame < firstFrame || frame.EmissionFrame > lastFrame)
                    continue;
                sawInRange = true;
                if (!FrameHasScreenshot(frame.EmissionFrame))
                    return false;
            }
            if (!sawInRange)
                return false;

            // Trust the cached LogicalFrame; skipping the re-read is why both halves are persisted.
            foreach (var frame in cached)
            {
                if (frame.EmissionFrame >= firstFrame && frame.EmissionFrame <= lastFrame)
                    m_Frames.Add(frame);
            }
            return true;
        }

        static bool FrameHasScreenshot(int frameIndex)
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (!TryGetScreenshotTextureInfo(frameData, out _, out _, out _))
                    return false;
                var data = frameData.GetFrameMetaData<byte>(
                    ProfilerDriver.profilerInternalSessionMetaDataGuid,
                    (int)ProfilingSessionMetaDataEntry.ScreenshotRawTextureData);
                return data.Length > 0;
            }
        }

        // Shared by the dimension read, the has-screenshot probe, and the details panel's extract.
        internal static bool TryGetScreenshotTextureInfo(RawFrameDataView frameData, out int width, out int height, out TextureFormat format)
        {
            width = 0;
            height = 0;
            format = default;
            if (!frameData.valid)
                return false;
            var texInfo = frameData.GetFrameMetaData<ScreenshotTextureInfo>(
                ProfilerDriver.profilerInternalSessionMetaDataGuid,
                (int)ProfilingSessionMetaDataEntry.ScreenshotTextureInfo);
            if (texInfo.Length != 1)
                return false;
            var info = texInfo[0];
            width = info.Width;
            height = info.Height;
            format = (TextureFormat)info.Format;
            return true;
        }

        List<ScreenshotFrame> TryGetCachedFrames()
        {
            var model = m_ProfilerWindow?.GetBottlenecksChartViewController()?.Model;
            if (model == null)
                return null;
            var frames = model.ScreenshotFrames;
            return frames.Count == 0 ? null : new List<ScreenshotFrame>(frames);
        }

        void SaveToCache()
        {
            var model = m_ProfilerWindow?.GetBottlenecksChartViewController()?.Model;
            if (model == null)
                return;
            model.SetScreenshotFrames(m_Frames);
        }

        // Worst-case RGBA32 buffer; the scaler fills the aspect-preserving subset at the start only.
        public static bool TryScaleScreenshot(int frameIndex, int maxWidth, int maxHeight,
                                              out byte[] bytes, out int width, out int height)
        {
            bytes = new byte[maxWidth * maxHeight * 4];
            if (ProfilerDriver.GetScaledScreenshotBytes(frameIndex, maxWidth, maxHeight, bytes, out width, out height))
                return true;

            bytes = null;
            width = 0;
            height = 0;
            return false;
        }

        // Metadata only, no pixel decode.
        public static bool TryReadScreenshotDimensions(int frameIndex, out int width, out int height, out TextureFormat format)
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
                return TryGetScreenshotTextureInfo(frameData, out width, out height, out format);
        }

        public static bool TryReadSingleIntMetadata(int frameIndex, ProfilingSessionMetaDataEntry entry, out int value)
        {
            value = 0;
            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (!frameData.valid)
                    return false;
                var meta = frameData.GetFrameMetaData<int>(
                    ProfilerDriver.profilerInternalSessionMetaDataGuid,
                    (int)entry);
                if (meta.Length != 1 || meta[0] < 0)
                    return false;
                value = meta[0];
                return true;
            }
        }

        public static Texture2D CreateScreenshotTexture(byte[] bytes, int width, int height, TextureFormat format)
        {
            var texture = new Texture2D(width, height, format, mipChain: false);
            return ApplyRawTextureDataOrDestroy(texture, () => texture.LoadRawTextureData(bytes));
        }

        // Overload for pixel data read straight from a frame's NativeArray slice, no managed copy.
        public static Texture2D CreateScreenshotTexture(NativeArray<byte> bytes, int width, int height, TextureFormat format)
        {
            var texture = new Texture2D(width, height, format, mipChain: false);
            return ApplyRawTextureDataOrDestroy(texture, () => texture.LoadRawTextureData(bytes));
        }

        static Texture2D ApplyRawTextureDataOrDestroy(Texture2D texture, Action loadRawData)
        {
            try
            {
                loadRawData();
                texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            }
            catch
            {
                // Native Texture2D memory survives GC, so a partly initialised one leaks.
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            return texture;
        }

        public static int FirstDisplayedFrameIndex()
        {
            var firstInMemory = ProfilerDriver.firstFrameIndex;
            var firstDisplayed = ProfilerDriver.lastFrameIndex + 1 - ProfilerUserSettings.frameCount;
            return Mathf.Max(firstInMemory, firstDisplayed);
        }

        // Shared by every visibility decision, so the strips and the reported count can't drift.
        public static int FirstSelectableFrameIndex()
        {
            return Mathf.Max(0, FirstDisplayedFrameIndex());
        }

        // recreate: true for sites that keep the field non-null; false for lazy allocators.
        public static void ReplaceCts(ref CancellationTokenSource cts, bool recreate)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = recreate ? new CancellationTokenSource() : null;
        }

        public void Dispose()
        {
            if (m_IsDisposed)
                return;
            m_IsDisposed = true;

            ProfilerDriver.profileLoaded -= OnDataLoaded;
            ProfilerDriver.profileCleared -= OnDataCleared;
            ProfilerDriver.NewProfilerFrameRecorded -= OnNewFrameRecorded;
            if (m_ProfilerWindow != null)
                m_ProfilerWindow.recordingStateChanged -= OnRecordingStateChanged;

            m_PendingRefresh?.Pause();
            m_PendingRefresh = null;
            m_PendingBurstEndCheck?.Pause();
            m_PendingBurstEndCheck = null;
        }
    }
}
