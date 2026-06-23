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
    // Window-scoped service that owns the canonical list of frames that have screenshots, and the
    // native event subscription that keeps it fresh. Both the details panel scroll strip
    // (ScreenshotsTimelineViewController) and the chart area mini strip
    // (ScreenshotsChartTimelineViewController) consume Frames and subscribe to Changed, so the
    // scan + cache write + debounce happen once per quiet period rather than once per controller.
    // The cache backing is BottlenecksChartViewModel.ScreenshotFrames (the only chart-side store
    // that survives the Profiler window being closed and re-opened) — the catalogue writes full
    // (LogicalFrame, EmissionFrame) pairs to it, so cache load doesn't need to re-read
    // kFramesSinceScreenshotRequested per entry.
    //
    // Frame index spaces — every entry tracks both:
    //   LogicalFrame  — the frame the screenshot DEPICTS (request time). User-facing identity.
    //   EmissionFrame — the frame at which the GPU readback completed and the pixel + texture-info
    //                   metadata was written into the profiler stream (typically LogicalFrame + 1..3).
    //                   This is the frame to query with ProfilerDriver.GetRawFrameDataView.
    // For legacy captures (recorded before kFramesSinceScreenshotRequested was introduced),
    // the offset metadata is absent and LogicalFrame defaults to EmissionFrame (identity).
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

    internal sealed class ScreenshotIndexCatalogue : IDisposable
    {
        // Rebuilding on every NewProfilerFrameRecorded would thrash consumers and re-hit the native
        // scan every frame; instead, defer until recording quiets.
        const long k_RecordingQuietPeriodMs = 1500;

        // Captures written at this version have a bug where each screenshot was re-emitted on every frame,
        // instead of only when a new capture completed, so the same image would repeat until the next real capture.
        // The fix landed at the same time as the metadata version bump.
        const int k_BuggyRepeatMetadataVersion = (int)ProfilingSessionMetaDataEntryVersion.ScreenshotVersion;
        // It was possible to change the capture rate from the original screenshots PR, but only via an API call.
        // Combined with the narrow affected versions range (6000.3.0a5 to 6000.3.0b6, 6000.4.0a1 to 6000.4.0a3),
        // it's fair to assume everyone of the few users this will affect will have had the (then) default capture rate.
        // Used as the sampling stride when collapsing them.
        const int k_BuggyCaptureScreenshotInterval = 15;
        // Side length of the down-scaled image compared to confirm where the picture actually changes
        // between sampled frames. Small enough to be cheap, large enough to distinguish frames.
        const int k_RepeatCompareImageSize = 32;

        readonly ProfilerWindow m_ProfilerWindow;
        readonly List<ScreenshotFrame> m_Frames = new List<ScreenshotFrame>();
        int m_LastScannedFrameIndex = -1;
        IVisualElementScheduledItem m_PendingRefresh;
        IVisualElementScheduledItem m_PendingBurstEndCheck;
        bool m_IsDisposed;
        // Timestamp (EditorApplication.timeSinceStartup) of the most recent OnNewFrameRecorded.
        // 0 means "never". Drives IsRecordingBurst, which the chart strip checks to decide
        // whether to take its lightweight append-only layout path. timeSinceStartup is
        // paused-aware, so a debugger break or modal dialogue can't strand the flag true.
        double m_LastFrameRecordedTime;

        public IReadOnlyList<ScreenshotFrame> Frames => m_Frames;
        public event Action Changed;
        // Fires the moment OnNewFrameRecorded arrives and IsRecordingBurst was false beforehand —
        // i.e. the transition from "not recording" to "actively recording". Consumers that need
        // to swap to a recording-aware presentation (e.g. the details strip hides itself behind a
        // placeholder) hook this so the swap happens at recording-start, not at first throttle.
        public event Action BurstStarted;
        // Fires once after recording stops (k_RecordingQuietPeriodMs after the final throttled
        // Refresh, when no new OnNewFrameRecorded came in to re-arm the throttle). Consumers
        // that took lightweight burst-mode shortcuts use this to do a one-shot full
        // reconciliation back to canonical layout. Not raised during sustained recording.
        public event Action BurstEnded;

        // True while we're inside the throttle window of an active recording — i.e. an
        // OnNewFrameRecorded fired within the last k_RecordingQuietPeriodMs. Consumers (chart
        // strip) use this to choose a low-disturbance "append" layout instead of the full
        // re-pick during sustained recording. Self-clears: once recording stops and no new
        // frames arrive, IsRecordingBurst goes false after the quiet period elapses.
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

            // Initial scan picks up whatever capture is already loaded — including the case where
            // the window was just opened with a previous session capture still resident. Subscribers
            // attach after the constructor returns, so they read Frames directly the first time.
            Refresh();
        }

        // Re-scan from the current driver frame range. Cheap if nothing has changed: the
        // m_LastScannedFrameIndex == currentLast branch just prunes evicted frames.
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
                    Changed?.Invoke();
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
                // Incremental: drop evicted frames, append any new ones since the last scan.
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
                    // Capture got shorter without a clear event (e.g., the user loaded a smaller
                    // capture file). Reset and rescan from scratch.
                    ScanAllFramesIntoList(currentFirst, currentLast);
                    m_LastScannedFrameIndex = currentLast;
                    changed = true;
                }
            }

            if (changed)
            {
                SaveToCache();
                Changed?.Invoke();
            }
        }

        // Full (re)scan of [currentFirst, currentLast] into m_Frames. Captures affected by the V1
        // every-frame repeat bug bypass the cache and are collapsed down to one entry per real
        // screenshot; all others use the normal cache-then-tail-scan path.
        void ScanAllFramesIntoList(int currentFirst, int currentLast)
        {
            m_Frames.Clear();

            if (ReadScreenshotMetadataVersion(currentLast) == k_BuggyRepeatMetadataVersion)
            {
                // The cache stores the already-collapsed (sparse) list, which can't be safely
                // tail-extended, and affected captures are always static file loads — so skip the
                // cache, raw-scan every frame, then discard the repeats.
                AppendEmissionFrames(ProfilerDriver.GetFramesWithScreenshots(currentFirst, currentLast));
                SortByLogicalFrame();
                DiscardBuggyRepeatScreenshots();
                return;
            }

            // Consult the persisted cache first for instant population on previously seen captures,
            // then scan any tail past the highest cached frame. The Bottlenecks cache survives the
            // Profiler window being closed and reopened, but the native capture may keep recording in
            // the background; screenshots taken between close and reopen wouldn't be in the cache and
            // would be silently dropped if we treated it as covering everything up to currentLast.
            var scanFrom = currentFirst;
            if (TryPopulateFromCache(currentFirst, currentLast) && m_Frames.Count > 0)
                scanFrom = HighestEmissionFrame() + 1;
            if (scanFrom <= currentLast)
                AppendEmissionFrames(ProfilerDriver.GetFramesWithScreenshots(scanFrom, currentLast));
            SortByLogicalFrame();
        }

        // Reads the screenshot session-metadata version off any valid frame (session metadata is
        // global, so any frame carries it). Returns -1 when unavailable.
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

        // Collapses the every-frame repeats in a V1 capture down to one entry per real screenshot.
        // Hybrid strategy: sample one frame per k_BuggyCaptureScreenshotInterval (cheap — the repeats
        // come in runs of that length), and keep a sample only when its down-scaled image differs from
        // the last kept one (robust — confirms the picture actually changed, and collapses runs longer
        // than the interval if the capture used the old script API to slow the rate). Images are
        // compared directly with ReadOnlySpan<byte>. m_Frames is assumed sorted by emission frame,
        // which holds here: V1 has no request-offset metadata so LogicalFrame == EmissionFrame.
        void DiscardBuggyRepeatScreenshots()
        {
            if (m_Frames.Count <= 1)
                return;

            var kept = new List<ScreenshotFrame>((m_Frames.Count / k_BuggyCaptureScreenshotInterval) + 1);
            byte[] lastKeptImage = null;
            var nextSampleEmission = int.MinValue;

            foreach (var frame in m_Frames)
            {
                // Skip frames inside the current sampling window without reading their pixels.
                if (lastKeptImage != null && frame.EmissionFrame < nextSampleEmission)
                    continue;
                nextSampleEmission = frame.EmissionFrame + k_BuggyCaptureScreenshotInterval;

                if (!TryReadScaledImage(frame.EmissionFrame, out var image))
                {
                    // Couldn't read the pixels — keep the frame rather than risk dropping a real one,
                    // and force the next sample to be kept too (unknown content can't be compared).
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

        // Reads a small, fixed-size down-scaled RGBA copy of the screenshot, used to compare frames
        // for equality. Reuses the same native scaler as the thumbnails. The fixed target size means
        // every returned buffer has the same length, so SequenceEqual comparisons are well-defined.
        static bool TryReadScaledImage(int emissionFrame, out byte[] image)
        {
            return TryScaleScreenshot(emissionFrame, k_RepeatCompareImageSize, k_RepeatCompareImageSize,
                       out image, out _, out _) && image != null;
        }

        // Reads kFramesSinceScreenshotRequested from each emission frame and pushes a ScreenshotFrame
        // entry. Legacy captures (no such metadata) → identity (LogicalFrame = EmissionFrame).
        void AppendEmissionFrames(int[] emissionFrames)
        {
            for (var i = 0; i < emissionFrames.Length; i++)
                m_Frames.Add(BuildFrame(emissionFrames[i]));
        }

        static ScreenshotFrame BuildFrame(int emissionFrame)
        {
            // TryReadSingleIntMetadata returns false (with value=0) when the metadata is absent —
            // e.g. captures recorded before kFramesSinceScreenshotRequested was added. The
            // false-path gives LogicalFrame = EmissionFrame, which is the correct identity fallback
            // (legacy captures have no readback offset to compensate for in the UI).
            TryReadSingleIntMetadata(emissionFrame,
                ProfilingSessionMetaDataEntry.FramesSinceScreenshotRequested, out int framesSinceRequested);
            return new ScreenshotFrame(emissionFrame - framesSinceRequested, emissionFrame);
        }

        void SortByLogicalFrame()
        {
            // Steady-state with a fixed request rate produces monotonic LogicalFrame order naturally,
            // but a varying readback latency between consecutive screenshots can flip a pair
            // (request A at frame 5, emitted 8; request B at frame 6, emitted 7 — LogicalFrame
            // order is A,B but EmissionFrame order is B,A). Sort explicitly so the strip always
            // displays in depicted-frame order regardless of readback jitter.
            m_Frames.Sort((a, b) => a.LogicalFrame.CompareTo(b.LogicalFrame));
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

        // LogicalFrame-keyed lookup for the details panel. EmissionFrame is what
        // ProfilerDriver.GetRawFrameDataView accepts; LogicalFrame is what the user sees on screen.
        // Performs a binary search against the LogicalFrame-sorted m_Frames.
        public bool TryGetEmissionFrame(int logicalFrame, out int emissionFrame)
        {
            var index = LowerBoundByLogicalFrame(logicalFrame);
            if (index < m_Frames.Count && m_Frames[index].LogicalFrame == logicalFrame)
            {
                emissionFrame = m_Frames[index].EmissionFrame;
                return true;
            }
            emissionFrame = 0;
            return false;
        }

        // Largest entry with LogicalFrame ≤ logicalFrame. Replaces the metadata-driven
        // FindPreviousFrameWithScreenshot — works for any frame regardless of whether
        // kFramesSinceLastScreenshot exists on it (e.g. legacy captures), since the catalogue
        // already holds the full LogicalFrame list in memory.
        public bool TryGetNearestPriorLogicalFrame(int logicalFrame, out ScreenshotFrame match)
        {
            var index = LowerBoundByLogicalFrame(logicalFrame);
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

        int LowerBoundByLogicalFrame(int logicalFrame)
        {
            int lo = 0, hi = m_Frames.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (m_Frames[mid].LogicalFrame < logicalFrame)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        void OnDataLoaded()
        {
            // profileLoaded fires for both initial loads and shift+load appends — Refresh() handles
            // both via its m_LastScannedFrameIndex check.
            Refresh();
        }

        void OnDataCleared()
        {
            m_LastScannedFrameIndex = -1;
            if (m_Frames.Count > 0)
            {
                m_Frames.Clear();
                SaveToCache();
                Changed?.Invoke();
            }
        }

        void OnNewFrameRecorded(int connectionId, int newFrameIndex)
        {
            if (m_IsDisposed || newFrameIndex <= m_LastScannedFrameIndex)
                return;

            // Stamp regardless of whether we end up arming a fresh throttle below — this drives
            // IsRecordingBurst, which the chart consults independently of the throttle pipeline.
            // Detect the no-burst → burst transition before stamping so BurstStarted fires exactly
            // once per recording run, at recording-start (used by the details strip to swap to its
            // "paused while recording" placeholder immediately, not after the first throttle).
            var wasInBurst = IsRecordingBurst;
            m_LastFrameRecordedTime = EditorApplication.timeSinceStartup;
            if (!wasInBurst)
                BurstStarted?.Invoke();

            // Throttle Refresh to at most once per k_RecordingQuietPeriodMs during sustained
            // recording: arm the timer on the first frame of a burst, coalesce subsequent frames
            // into the same window, then clear m_PendingRefresh inside the callback so the next
            // post-fire frame re-arms a fresh timer. The earlier debounce form (re-arming on
            // every frame) never fired during sustained recording — new screenshots wouldn't
            // appear on the strip until the user stopped capturing.
            var host = m_ProfilerWindow?.rootVisualElement;
            if (host == null)
                return;

            if (m_PendingRefresh == null)
            {
                m_PendingRefresh = host.schedule.Execute(() =>
                {
                    m_PendingRefresh = null;
                    Refresh();

                    // Schedule a follow-up "did the burst end?" check. If no further
                    // OnNewFrameRecorded arrives in the next k_RecordingQuietPeriodMs,
                    // m_PendingRefresh stays null and we fire BurstEnded so consumers can
                    // reconcile back to canonical layout. If recording is still active, a new
                    // OnNewFrameRecorded re-arms m_PendingRefresh and the check below no-ops.
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
                        if (m_PendingRefresh == null)
                            BurstEnded?.Invoke();
                    });
                    m_PendingBurstEndCheck.ExecuteLater(k_RecordingQuietPeriodMs);
                });
                m_PendingRefresh.ExecuteLater(k_RecordingQuietPeriodMs);
            }
        }

        bool PruneBefore(int currentFirstFrame)
        {
            // Use EmissionFrame for the bound: ProfilerDriver.firstFrameIndex moves forward as
            // older frames are evicted from the ring buffer, so any entry whose EmissionFrame
            // (the frame metadata actually lives on) has fallen out of range is gone — even if
            // its LogicalFrame would still nominally fit.
            var removed = m_Frames.RemoveAll(f => f.EmissionFrame < currentFirstFrame);
            return removed > 0;
        }

        bool TryPopulateFromCache(int firstFrame, int lastFrame)
        {
            var cached = TryGetCachedFrames();
            if (cached == null || cached.Count == 0)
                return false;

            // Verify every in-range cached frame still has a screenshot. A single-frame probe
            // would accept a stale cache whenever two unrelated captures happened to share even
            // one screenshot frame index (e.g. both have one at frame 0) — populating the
            // timeline with ghost markers for every other frame from the old capture.
            // FrameHasScreenshot is a sub-millisecond metadata read; running it on the
            // capture-load path (not interactive) for the cached count is acceptable.
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

            // Trust the cached LogicalFrame as-is — it was derived at scan time from the same
            // kFramesSinceScreenshotRequested metadata that's still on the frame. Skipping the
            // re-read here is the whole point of persisting both halves of the pair.
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

        // Reads the screenshot texture descriptor (format / width / height) from an already-open
        // frame view. Returns false (with zero outs) when the frame is invalid or carries no
        // screenshot texture-info metadata. Centralises the GetRawFrameDataView + texture-info guard
        // shared by the dimension read, the has-screenshot probe, and the details panel's extract.
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
            // SetScreenshotFrames does its own defensive copy; no need to pre-copy here.
            var model = m_ProfilerWindow?.GetBottlenecksChartViewController()?.Model;
            if (model == null)
                return;
            model.SetScreenshotFrames(m_Frames);
        }

        // Worst-case RGBA32 buffer; native scaler writes only the aspect-preserving subset at the start
        // and reports its actual dimensions in width/height.
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

        // Reads the source screenshot's stored width/height/format from per-frame metadata.
        // Returns false (with zero outs) if the frame is invalid or has no screenshot metadata.
        public static bool TryReadScreenshotDimensions(int frameIndex, out int width, out int height, out TextureFormat format)
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
                return TryGetScreenshotTextureInfo(frameData, out width, out height, out format);
        }

        // Reads a single int profiler session metadata entry from a frame. Returns false with 0 value on any failure path.
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

        // Overload for screenshot pixel data read straight from a frame's NativeArray slice, avoiding
        // a managed copy. Shares the create/apply/destroy-on-failure path with the byte[] overload.
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
                // Native Texture2D memory survives GC — destroy explicitly so a partially
                // initialised texture from a size/format mismatch doesn't leak.
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            return texture;
        }

        // The lowest frame index currently shown in the Profiler window.
        // Used to hide screenshots that are beyond the range of what's accessible to the user.
        public static int FirstDisplayedFrameIndex()
        {
            var firstInMemory = ProfilerDriver.firstFrameIndex;
            var firstDisplayed = ProfilerDriver.lastFrameIndex + 1 - ProfilerUserSettings.frameCount;
            return Mathf.Max(firstInMemory, firstDisplayed);
        }

        // Cancels and disposes an existing CTS, then either reallocates it (recreate: true — for sites
        // that keep the field non-null at all times) or sets it to null (recreate: false — for sites
        // that allocate lazily when work appears).
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

            m_PendingRefresh?.Pause();
            m_PendingRefresh = null;
            m_PendingBurstEndCheck?.Pause();
            m_PendingBurstEndCheck = null;
        }
    }
}
