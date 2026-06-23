// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling.Editor;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    // Renders the Screenshots module's chart area timeline strip. Anchors the first and last screenshots
    // to the pixel positions of the frames they came from, then fills in as many evenly-spaced intermediates
    // as physically fit. Independent of the details-panel timeline controller.
    //
    // Layout runs without any textures loaded — it uses a constant 16:9-at-strip-height slot pitch to decide
    // which screenshots to render. Only the screenshots actually picked for display have their pixel data
    // extracted and scaled, which keeps this cheap even for captures with hundreds of screenshots.
    //
    // The authoritative list of screenshot frames comes from the shared ScreenshotIndexCatalogue; this
    // controller's job is layout + per-thumbnail loading.
    internal class ScreenshotsChartTimelineViewController : ViewController
    {
        const string k_UxmlResourceName = "Profiler/Screenshots/ScreenshotsChartTimelineView.uxml";
        const string k_UssClass_Thumbnail = "screenshots-chart-timeline__thumbnail";
        const string k_UssClass_ThumbnailImage = "screenshots-chart-timeline__thumbnail-image";

        const int k_MaxThumbnailWidth = 73;
        const int k_MaxThumbnailHeight = 41;
        const float k_AssumedThumbnailWidth = k_MaxThumbnailWidth;   // 16:9 at strip height
        const float k_AssumedThumbnailHeight = k_MaxThumbnailHeight;
        const float k_SlotPadding = 2f;

        enum ChartScreenshotRole
        {
            Unpicked,
            First,
            Middle,
            Last,
        }

        readonly ProfilerWindow m_ProfilerWindow;

        ScreenshotIndexCatalogue m_Catalogue;
        VisualElement m_Root;
        readonly List<ChartScreenshot> m_Screenshots = new List<ChartScreenshot>();
        CancellationTokenSource m_LoadCancellation;
        bool m_IsDisposed;
        LayoutContext m_LastLayout;
        // Hash of the currently-picked EmissionFrames. Same set ⇒ skip cancel-and-restart so
        // in-flight loads aren't starved by per-frame Relayouts during recording.
        int m_LastPickedSetSignature;
        // FrameCount at the most recent successful DoAppendLayout. Used to early-out per-editor-frame
        // append calls when nothing has grown. -1 ⇒ no append run since the last full DoLayout
        // (so the next append-mode call always does the full re-position work at least once).
        int m_LastAppendFrameCount = -1;

        class ChartScreenshot
        {
            // EmissionFrame is what TryScaleScreenshot needs (metadata + pixel data live there).
            // LogicalFrame is where the thumbnail sits visually on the timeline (the depicted frame).
            // Both are supplied by the catalogue; this controller only reads them.
            public int EmissionFrame;
            public int LogicalFrame;
            public Texture2D Thumbnail;
            public float DisplayWidth;
            public float DisplayHeight;
            public VisualElement Element;
            public ChartScreenshotRole Role;
        }

        struct LayoutContext
        {
            public bool Valid;
            public int FirstFrame;
            public int FrameCount;
            public float LeftOffset;
            public float TimelineWidth;

            public float FrameToX(int logicalFrame)
            {
                if (!Valid || FrameCount <= 0)
                    return 0f;
                if (FrameCount == 1)
                    return LeftOffset + TimelineWidth / 2f;
                float rel = logicalFrame - FirstFrame;
                return LeftOffset + (rel / (FrameCount - 1)) * TimelineWidth;
            }
        }

        public ScreenshotsChartTimelineViewController(ProfilerWindow profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidOperationException($"Failed to load UXML for {nameof(ScreenshotsChartTimelineViewController)}");

            m_Root = view;
            m_Root.pickingMode = PickingMode.Ignore;
            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Catalogue = m_ProfilerWindow?.GetScreenshotIndexCatalogue();
            if (m_Catalogue != null)
            {
                m_Catalogue.Changed += OnCatalogueChanged;
                m_Catalogue.BurstEnded += OnBurstEnded;
            }

            ReloadTimeline();
        }

        void OnCatalogueChanged()
        {
            if (m_IsDisposed)
                return;
            ReloadTimeline();
        }

        void OnBurstEnded()
        {
            if (m_IsDisposed)
                return;
            // Recording stopped: force the full ReloadTimeline path (bypass the burst
            // shortcut) so we reconcile back to canonical stride-pick layout even though
            // IsRecordingBurst may still be borderline-true on the timestamp side.
            DoFullReloadTimeline();
        }

        public void ReloadTimeline()
        {
            // During an active recording burst, hand off to DoAppendLayout — it does its own
            // incremental merge (prune + append + gap-fill) and never cancels in-flight
            // thumbnail loads, so the chart can grow new picks at the right edge without
            // throwing away textures the user can already see. The catalogue's BurstEnded
            // event (handled by OnBurstEnded above) triggers the full reconciliation once
            // recording stops.
            if (m_Catalogue != null && m_Catalogue.IsRecordingBurst && m_LastLayout.Valid && HasAnyPickedSlot())
            {
                Relayout();
                return;
            }

            DoFullReloadTimeline();
        }

        void DoFullReloadTimeline()
        {
            // Merge the catalogue's authoritative frame list into m_Screenshots, preserving any
            // existing entries (and their loaded Thumbnail textures) keyed by EmissionFrame.
            var existingByEmissionFrame = new Dictionary<int, ChartScreenshot>(m_Screenshots.Count);
            foreach (var screenshot in m_Screenshots)
                existingByEmissionFrame[screenshot.EmissionFrame] = screenshot;

            var catalogueFrames = m_Catalogue?.Frames;
            var newList = new List<ChartScreenshot>(catalogueFrames?.Count ?? 0);
            if (catalogueFrames != null)
            {
                foreach (var frame in catalogueFrames)
                {
                    if (existingByEmissionFrame.TryGetValue(frame.EmissionFrame, out var existing))
                    {
                        // LogicalFrame in the catalogue can change between scans for the same emission
                        // frame (e.g. cache-load identity fallback gives way to a real offset once
                        // we re-read metadata) — keep the cached texture but refresh the index.
                        existing.LogicalFrame = frame.LogicalFrame;
                        newList.Add(existing);
                        existingByEmissionFrame.Remove(frame.EmissionFrame); // mark preserved
                    }
                    else
                    {
                        newList.Add(new ChartScreenshot
                        {
                            EmissionFrame = frame.EmissionFrame,
                            LogicalFrame = frame.LogicalFrame,
                        });
                    }
                }
            }

            // Dispose anything that didn't survive the merge (evicted or entirely replaced frames).
            foreach (var orphan in existingByEmissionFrame.Values)
                DisposeScreenshot(orphan);

            m_Screenshots.Clear();
            m_Screenshots.AddRange(newList);

            CancelInFlightLoad();
            // Forget the last-loaded signature so the upcoming DoLayout always re-issues loads,
            // even when a rescan produces an identical picked set — we just cancelled what was
            // in flight and need to restart it.
            m_LastPickedSetSignature = 0;
            Relayout();
        }

        public void Relayout()
        {
            if (m_Root == null)
                return;
            var width = m_Root.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0)
                return;
            DoLayout(width);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            DoLayout(evt.newRect.width);
        }

        static void DisposeScreenshot(ChartScreenshot screenshot)
        {
            if (screenshot.Thumbnail != null)
            {
                UnityEngine.Object.DestroyImmediate(screenshot.Thumbnail);
                screenshot.Thumbnail = null;
            }
            if (screenshot.Element != null)
            {
                screenshot.Element.RemoveFromHierarchy();
                screenshot.Element = null;
            }
        }

        void CancelInFlightLoad()
        {
            ScreenshotIndexCatalogue.ReplaceCts(ref m_LoadCancellation, recreate: false);
        }

        // Compute the right-aligned timeline layout for the current frame range. Shared by the full
        // (DoLayout) and append (DoAppendLayout) paths so both place thumbnails identically. As fewer
        // than frameCapacity frames are recorded the timeline only fills from the right, leaving a gap
        // on the left.
        LayoutContext BuildLayoutContext(float containerWidth, int firstFrame, int lastFrame)
        {
            var frameCount = (lastFrame - firstFrame) + 1;
            var frameCapacity = ProfilerUserSettings.frameCount;
            if (frameCapacity <= 0)
                frameCapacity = frameCount;

            var fill = Mathf.Clamp01((float)frameCount / frameCapacity);
            var leftOffset = containerWidth * (1f - fill);

            return new LayoutContext
            {
                Valid = true,
                FirstFrame = firstFrame,
                FrameCount = frameCount,
                LeftOffset = leftOffset,
                TimelineWidth = containerWidth - leftOffset,
            };
        }

        void DoLayout(float containerWidth)
        {
            // During an active recording burst, take the lightweight append-only path. It
            // re-positions existing picks against the current frame range, prunes evicted
            // entries, and tries to slot the newest catalogue entry in at the right edge. Avoids
            // the per-Refresh "clear-and-refill" flicker that the canonical stride-pick would
            // produce during sustained recording.
            if (m_Catalogue != null && m_Catalogue.IsRecordingBurst
                && m_LastLayout.Valid
                && m_Screenshots.Count > 0
                && HasAnyPickedSlot())
            {
                DoAppendLayout(containerWidth);
                return;
            }

            // Full layout supersedes any in-progress append state; the next burst-mode call
            // must redo its work from scratch rather than early-out on a stale frame count.
            m_LastAppendFrameCount = -1;

            // Clear any prior selection — we'll re-assign Role and show/hide below.
            foreach (var screenshot in m_Screenshots)
                screenshot.Role = ChartScreenshotRole.Unpicked;

            m_LastLayout = default;

            if (containerWidth <= 0)
            {
                ApplyVisibility();
                return;
            }

            var firstFrame = ProfilerDriver.firstFrameIndex;
            var lastFrame = ProfilerDriver.lastFrameIndex;
            if (firstFrame < 0 || lastFrame < 0 || m_Screenshots.Count == 0)
            {
                ApplyVisibility();
                return;
            }

            // Trim to the frame-count display window so both the chart's domain (FrameToX) and the set
            // of thumbnails it shows line up with the main Profiler chart: frames below this stay in
            // memory but are hidden from view once the capture holds more frames than the frame-count
            // setting allows.
            firstFrame = ScreenshotIndexCatalogue.FirstDisplayedFrameIndex();

            // LogicalFrame is set at scan time from FramesSinceScreenshotRequested, so it's stable from the
            // first DoLayout — the picked set doesn't drift as textures finish loading.
            m_Screenshots.Sort((a, b) => a.LogicalFrame.CompareTo(b.LogicalFrame));

            m_LastLayout = BuildLayoutContext(containerWidth, firstFrame, lastFrame);

            // Only consider screenshots whose depicted (logical) frame is still within the display window.
            var firstPicked = 0;
            while (firstPicked < m_Screenshots.Count && m_Screenshots[firstPicked].LogicalFrame < firstFrame)
                firstPicked++;
            var eligibleCount = m_Screenshots.Count - firstPicked;
            if (eligibleCount == 0)
            {
                ApplyVisibility();
                return;
            }

            // Pick last (always shown).
            var last = m_Screenshots[^1];
            var lastFrameX = m_LastLayout.FrameToX(last.LogicalFrame);

            if (eligibleCount == 1)
            {
                // Pick the role whose anchoring keeps the lone thumb on-screen at its frame
                // position. Last anchors the right edge to the frame (left = frameX - width);
                // for a single screenshot near frame 0 that goes negative and gets clipped.
                last.Role = (lastFrameX + k_AssumedThumbnailWidth <= containerWidth)
                    ? ChartScreenshotRole.First
                    : ChartScreenshotRole.Last;
            }
            else
            {
                var assumedLastLeft = lastFrameX - k_AssumedThumbnailWidth;
                last.Role = ChartScreenshotRole.Last;

                var first = m_Screenshots[firstPicked];
                var firstFrameX = m_LastLayout.FrameToX(first.LogicalFrame);
                var assumedFirstRight = firstFrameX + k_AssumedThumbnailWidth;

                // Only place the first screenshot if it fits without colliding with the last (accounting for buffers).
                if (assumedFirstRight + k_SlotPadding <= assumedLastLeft - k_SlotPadding)
                {
                    first.Role = ChartScreenshotRole.First;

                    var middleEnd = m_Screenshots.Count - 1; // exclusive
                    var middleSourceCount = middleEnd - (firstPicked + 1);
                    if (middleSourceCount > 0)
                    {
                        // Constrain intermediate centres to a range that leaves the 2px buffer plus a half-thumbnail's
                        // worth of clearance from first and last; this is what stops intermediates from drawing over
                        // first or last.
                        var centreLow = assumedFirstRight + k_SlotPadding + k_AssumedThumbnailWidth / 2f;
                        var centreHigh = assumedLastLeft - k_SlotPadding - k_AssumedThumbnailWidth / 2f;

                        if (centreLow <= centreHigh)
                        {
                            var candidates = new List<ChartScreenshot>(middleSourceCount);
                            for (var i = firstPicked + 1; i < middleEnd; i++)
                            {
                                var fx = m_LastLayout.FrameToX(m_Screenshots[i].LogicalFrame);
                                if (fx >= centreLow && fx <= centreHigh)
                                    candidates.Add(m_Screenshots[i]);
                            }

                            var pickedMiddles = PickIntermediatesByStride(candidates);

                            // Drop any middle whose assumed-width footprint would still collide with the previous
                            // one (only happens when source frame spacing is irregular).
                            var previousRight = assumedFirstRight;
                            foreach (var middle in pickedMiddles)
                            {
                                var center = m_LastLayout.FrameToX(middle.LogicalFrame);
                                var assumedLeft = center - k_AssumedThumbnailWidth / 2f;
                                if (assumedLeft < previousRight + k_SlotPadding)
                                    continue;
                                if (assumedLeft + k_AssumedThumbnailWidth + k_SlotPadding > assumedLastLeft)
                                    continue;
                                middle.Role = ChartScreenshotRole.Middle;
                                previousRight = assumedLeft + k_AssumedThumbnailWidth;
                            }
                        }
                    }
                }
            }

            // Position picked elements with whatever dimensions we currently know (assumed until the texture
            // arrives), hide everything else.
            foreach (var screenshot in m_Screenshots)
            {
                if (screenshot.Role == ChartScreenshotRole.Unpicked)
                {
                    if (screenshot.Element != null)
                        screenshot.Element.style.display = DisplayStyle.None;
                    continue;
                }

                PositionByRole(screenshot);
            }

            // Only restart loading when the picked set actually changed. During recording the
            // selected frame advances every editor frame but the picked thumbnails are the same,
            // so a per-frame cancel+restart would starve every in-flight load.
            var signature = ComputePickedSetSignature();
            if (signature != m_LastPickedSetSignature)
            {
                m_LastPickedSetSignature = signature;
                BeginLoadingPickedThumbnailsAsync();
            }
        }

        bool HasAnyPickedSlot()
        {
            foreach (var screenshot in m_Screenshots)
            {
                if (screenshot.Role != ChartScreenshotRole.Unpicked)
                    return true;
            }
            return false;
        }

        // Append-only layout path taken during recording bursts. Doesn't clear roles or re-run
        // PickIntermediatesByStride; only:
        //   1. prunes entries that have scrolled out of the ring buffer,
        //   2. recomputes the layout context for the (likely larger) frame range,
        //   3. re-positions existing picks (their pixel X shifts as FrameCount grows),
        //   4. merges new catalogue entries into m_Screenshots,
        //   5. attempts to promote the newest entry to Last if it has clearance, demoting the
        //      previous Last to Middle (skipping the append when there isn't room).
        // Loads are (re)started only when the picked set actually changed, so the per-editor-frame
        // re-layouts during recording don't starve in-flight thumbnail loads.
        void DoAppendLayout(float containerWidth)
        {
            if (containerWidth <= 0)
                return;

            var firstFrame = ProfilerDriver.firstFrameIndex;
            var lastFrame = ProfilerDriver.lastFrameIndex;
            if (firstFrame < 0 || lastFrame < 0)
                return;

            // Prune entries whose EmissionFrame has fallen out of the ring buffer. The
            // catalogue does this in its own PruneBefore; we mirror it here so the chart's
            // m_Screenshots count tracks catalogue.Frames.Count even during a burst (without
            // this, the merge check below would fail once catalogue eviction outpaces
            // capture, and append would silently stop).
            var anyPickedPruned = false;
            for (var i = m_Screenshots.Count - 1; i >= 0; i--)
            {
                if (m_Screenshots[i].EmissionFrame >= firstFrame)
                    continue;
                if (m_Screenshots[i].Role != ChartScreenshotRole.Unpicked)
                    anyPickedPruned = true;
                DisposeScreenshot(m_Screenshots[i]);
                m_Screenshots.RemoveAt(i);
            }

            if (m_Screenshots.Count == 0)
                return;

            var frameCount = (lastFrame - firstFrame) + 1;
            var catalogueFrameCount = m_Catalogue?.Frames.Count ?? 0;

            // Early-out only when nothing changed AND nothing was pruned. FrameCount changes
            // every editor frame during recording, so this rarely triggers — but it's cheap
            // insurance for the no-op case.
            if (!anyPickedPruned
                && frameCount == m_LastAppendFrameCount
                && catalogueFrameCount == m_Screenshots.Count)
                return;

            m_LastLayout = BuildLayoutContext(containerWidth, firstFrame, lastFrame);
            m_LastAppendFrameCount = frameCount;

            // Re-position existing picks. FrameToX depends on FrameCount; as more frames are
            // recorded, the rightmost pick's pixel X drifts left, so even when no new catalogue
            // entry arrives, picks need re-positioning to stay anchored to their LogicalFrames.
            foreach (var screenshot in m_Screenshots)
            {
                if (screenshot.Role != ChartScreenshotRole.Unpicked)
                    PositionByRole(screenshot);
            }

            // Merge any new catalogue entries into m_Screenshots.
            var appended = false;
            if (catalogueFrameCount > m_Screenshots.Count && m_Catalogue != null)
            {
                var existingByEmissionFrame = new Dictionary<int, ChartScreenshot>(m_Screenshots.Count);
                foreach (var screenshot in m_Screenshots)
                    existingByEmissionFrame[screenshot.EmissionFrame] = screenshot;

                ChartScreenshot newestAddition = null;
                foreach (var frame in m_Catalogue.Frames)
                {
                    if (existingByEmissionFrame.TryGetValue(frame.EmissionFrame, out var existing))
                    {
                        // Refresh LogicalFrame in case it firmed up since the last scan
                        // (cache-load identity fallback gave way to a real offset).
                        existing.LogicalFrame = frame.LogicalFrame;
                    }
                    else
                    {
                        var added = new ChartScreenshot
                        {
                            EmissionFrame = frame.EmissionFrame,
                            LogicalFrame = frame.LogicalFrame,
                        };
                        m_Screenshots.Add(added);
                        if (newestAddition == null || added.LogicalFrame > newestAddition.LogicalFrame)
                            newestAddition = added;
                    }
                }
                m_Screenshots.Sort((a, b) => a.LogicalFrame.CompareTo(b.LogicalFrame));

                if (newestAddition != null)
                    appended = TryAppendAtRightEdge(newestAddition);
            }

            if (appended || anyPickedPruned)
            {
                m_LastPickedSetSignature = ComputePickedSetSignature();
                if (appended)
                    BeginLoadingPickedThumbnailsAsync();
            }
        }

        // Attempt to promote `candidate` to the Last role, anchored at the right edge. The
        // current rightmost picked slot is the anchor point for the clearance check:
        //   - If it has Last role, it gets demoted to Middle (centred at its frame X) to make
        //     room for the new Last (which takes over the right-anchor responsibility).
        //   - If it has First role (the single-screenshot edge case where the lone pick was
        //     left-anchored at frame 0), it stays as First — demoting it to Middle would
        //     centre it half off-screen left.
        //   - If it has Middle role (uncommon during append mode), it stays as Middle.
        // Bails (no role mutation) when the new Last would collide with the rightmost slot or clip
        // off-screen right. The deferred work is picked up by the next burst-end full DoLayout.
        // Returns true if `candidate` was successfully promoted to Last. The caller uses this
        // to decide whether to recompute the signature and kick off thumbnail loads.
        bool TryAppendAtRightEdge(ChartScreenshot candidate)
        {
            ChartScreenshot rightmost = null;
            for (var i = m_Screenshots.Count - 1; i >= 0; i--)
            {
                if (m_Screenshots[i].Role != ChartScreenshotRole.Unpicked)
                {
                    rightmost = m_Screenshots[i];
                    break;
                }
            }
            if (rightmost == null || ReferenceEquals(rightmost, candidate))
                return false;

            var candidateX = m_LastLayout.FrameToX(candidate.LogicalFrame);
            var rightmostX = m_LastLayout.FrameToX(rightmost.LogicalFrame);

            // New Last anchors its right edge to candidateX (left = candidateX - width).
            // Compute rightmost's effective right edge: if it's a Last role we're about to
            // demote, use its width-centred edge; otherwise its current-role edge.
            var newLastLeft = candidateX - k_AssumedThumbnailWidth;
            var demoteRightmost = rightmost.Role == ChartScreenshotRole.Last;
            float rightmostRight;
            if (demoteRightmost)
                rightmostRight = rightmostX + k_AssumedThumbnailWidth / 2f;     // post-demotion (Middle)
            else if (rightmost.Role == ChartScreenshotRole.First)
                rightmostRight = rightmostX + k_AssumedThumbnailWidth;          // First stays left-anchored
            else
                rightmostRight = rightmostX + k_AssumedThumbnailWidth / 2f;     // Middle stays centred

            if (newLastLeft < rightmostRight + k_SlotPadding * 2f)
                return false;

            // New Last must actually fit inside the timeline area.
            if (candidateX > m_LastLayout.LeftOffset + m_LastLayout.TimelineWidth)
                return false;

            // Demoting the old Last (right-anchored) to Middle (centred) shifts its span right by
            // half a thumbnail — away from its leftward neighbour — so it can't collide on that
            // side. Only the right-edge clearance checked above can block the append.
            if (demoteRightmost)
            {
                rightmost.Role = ChartScreenshotRole.Middle;
                PositionByRole(rightmost);
            }
            candidate.Role = ChartScreenshotRole.Last;
            PositionByRole(candidate);
            return true;
        }

        int ComputePickedSetSignature()
        {
            // Signature over the picked EmissionFrames. m_Screenshots is always sorted by LogicalFrame
            // before this runs, so the iteration order is stable; only set membership matters (the role
            // is content-addressed by EmissionFrame). Compared only within this session, so HashCode's
            // per-process seed is fine.
            var hash = new HashCode();
            foreach (var screenshot in m_Screenshots)
            {
                if (screenshot.Role == ChartScreenshotRole.Unpicked)
                    continue;
                hash.Add(screenshot.EmissionFrame);
            }
            return hash.ToHashCode();
        }

        // Pick a subset of intermediate candidates at an integer source-index stride that's guaranteed
        // to satisfy slotPitch. The alternative — picking N entries at a non-integer real step and
        // rounding — alternates between consecutive integers and produces pairs of picks whose pixel
        // spacing undershoots slotPitch, which then trip the collision-drop step below and leave
        // visibly patchy gaps even on perfectly regular source data.
        List<ChartScreenshot> PickIntermediatesByStride(List<ChartScreenshot> candidates)
        {
            if (candidates.Count <= 1)
                return new List<ChartScreenshot>(candidates);

            var slotPitch = k_AssumedThumbnailWidth + k_SlotPadding * 2f;
            var firstCandX = m_LastLayout.FrameToX(candidates[0].LogicalFrame);
            var lastCandX = m_LastLayout.FrameToX(candidates[^1].LogicalFrame);
            var candSpan = lastCandX - firstCandX;
            var pxPerCandidate = candSpan / (candidates.Count - 1);
            var pickStep = pxPerCandidate > 0
                ? Mathf.Max(1, Mathf.CeilToInt(slotPitch / pxPerCandidate))
                : candidates.Count;
            var pickedCount = (candidates.Count - 1) / pickStep + 1;
            // Centre the picks within the candidate range so the leftover space at the edges (caused
            // by integer stride not always tiling the range exactly) is distributed symmetrically
            // rather than piling up on one side.
            var pickOffset = ((candidates.Count - 1) - (pickedCount - 1) * pickStep) / 2;

            var picks = new List<ChartScreenshot>(pickedCount);
            for (var i = 0; i < pickedCount; i++)
            {
                var idx = pickOffset + i * pickStep;
                if (idx < 0) idx = 0;
                if (idx >= candidates.Count) idx = candidates.Count - 1;
                picks.Add(candidates[idx]);
            }
            return picks;
        }

        void ApplyVisibility()
        {
            foreach (var screenshot in m_Screenshots)
            {
                if (screenshot.Element != null)
                    screenshot.Element.style.display = DisplayStyle.None;
            }
        }

        void PositionByRole(ChartScreenshot screenshot)
        {
            EnsureElement(screenshot);

            // The element always occupies a fixed assumed-width slot, regardless of the texture's actual
            // dimensions. This keeps element positions stable across re-layouts: they depend only on the
            // frame data, not on whether a given thumbnail has finished loading. The image inside the slot
            // is what shrinks/aligns to its native width per role.
            var frameX = m_LastLayout.FrameToX(screenshot.LogicalFrame);
            float left;
            Justify justify;
            switch (screenshot.Role)
            {
                case ChartScreenshotRole.First:
                    left = frameX;
                    justify = Justify.FlexStart;
                    break;
                case ChartScreenshotRole.Last:
                    left = frameX - k_AssumedThumbnailWidth;
                    justify = Justify.FlexEnd;
                    break;
                default:
                    left = frameX - k_AssumedThumbnailWidth / 2f;
                    justify = Justify.Center;
                    break;
            }

            screenshot.Element.style.left = left;
            screenshot.Element.style.width = k_AssumedThumbnailWidth;
            screenshot.Element.style.height = k_AssumedThumbnailHeight;
            screenshot.Element.style.justifyContent = justify;
            screenshot.Element.style.display = DisplayStyle.Flex;
        }

        void EnsureElement(ChartScreenshot screenshot)
        {
            if (screenshot.Element != null)
                return;

            var container = new VisualElement();
            container.AddToClassList(k_UssClass_Thumbnail);
            container.pickingMode = PickingMode.Ignore;
            container.style.position = Position.Absolute;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            var image = new Image();
            image.AddToClassList(k_UssClass_ThumbnailImage);
            image.pickingMode = PickingMode.Ignore;
            image.image = screenshot.Thumbnail;
            if (screenshot.Thumbnail != null)
            {
                image.style.width = screenshot.DisplayWidth;
                image.style.height = screenshot.DisplayHeight;
            }
            container.Add(image);

            m_Root.Add(container);
            screenshot.Element = container;
        }

        async void BeginLoadingPickedThumbnailsAsync()
        {
            CancelInFlightLoad();

            var pending = new List<ChartScreenshot>();
            foreach (var screenshot in m_Screenshots)
            {
                if (screenshot.Role != ChartScreenshotRole.Unpicked && screenshot.Thumbnail == null)
                    pending.Add(screenshot);
            }

            if (pending.Count == 0)
                return;

            // m_LoadCancellation was nulled by CancelInFlightLoad above; recreate through the shared
            // helper so all CTS lifecycle goes through one place.
            ScreenshotIndexCatalogue.ReplaceCts(ref m_LoadCancellation, recreate: true);
            var token = m_LoadCancellation.Token;

            try
            {
                foreach (var screenshot in pending)
                {
                    if (token.IsCancellationRequested || m_IsDisposed)
                        return;
                    if (screenshot.Role == ChartScreenshotRole.Unpicked)
                        continue; // Got dropped by a re-layout mid-load.

                    await LoadThumbnailAsync(screenshot, token);

                    if (token.IsCancellationRequested || m_IsDisposed)
                        return;
                    if (screenshot.Thumbnail == null)
                        continue;

                    ApplyLoadedThumbnail(screenshot);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // ObjectDisposedException can land here when Dispose runs while a Task.Run
                // registration is still in-flight — it's a cancellation signal, not a bug.
            }
            catch (Exception ex)
            {
                // async void: anything unhandled escapes to the synchronization context and can crash the editor.
                Debug.LogException(ex);
            }
        }

        void ApplyLoadedThumbnail(ChartScreenshot screenshot)
        {
            if (screenshot.Role == ChartScreenshotRole.Unpicked)
                return;
            if (!m_LastLayout.Valid || screenshot.Element == null)
                return;

            // Element position/dimensions are stable — only the image inside needs sizing and the texture.
            var image = screenshot.Element.Q<Image>();
            if (image != null)
            {
                image.image = screenshot.Thumbnail;
                image.style.width = screenshot.DisplayWidth;
                image.style.height = screenshot.DisplayHeight;
            }
        }

        async Task LoadThumbnailAsync(ChartScreenshot screenshot, CancellationToken token)
        {
            byte[] bytes = null;
            var width = 0;
            var height = 0;
            // Pixel data lives at EmissionFrame (where the GPU readback metadata was emitted),
            // even though the thumbnail is positioned at LogicalFrame on the strip.
            var emissionFrame = screenshot.EmissionFrame;
            await Task.Run(() =>
            {
                ScreenshotIndexCatalogue.TryScaleScreenshot(emissionFrame, k_MaxThumbnailWidth, k_MaxThumbnailHeight,
                    out bytes, out width, out height);
            }, token);

            if (token.IsCancellationRequested || m_IsDisposed)
                return;
            if (bytes == null)
                return;

            // TryScaleScreenshot returns a max-sized RGBA32 buffer with only the leading
            // width*height*4 bytes populated. CreateScreenshotTexture calls LoadRawTextureData(byte[])
            // which throws on any size mismatch, so trim before handing off when the aspect-preserving
            // fit produced a smaller-than-max output.
            var exactSize = width * height * 4;
            if (bytes.Length != exactSize)
            {
                var trimmed = new byte[exactSize];
                Buffer.BlockCopy(bytes, 0, trimmed, 0, exactSize);
                bytes = trimmed;
            }

            // Texture2D APIs require the main thread, so this runs on the await continuation.
            var texture = ScreenshotIndexCatalogue.CreateScreenshotTexture(bytes, width, height, TextureFormat.RGBA32);

            if (token.IsCancellationRequested || m_IsDisposed)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return;
            }

            screenshot.Thumbnail = texture;
            screenshot.DisplayWidth = width;
            screenshot.DisplayHeight = height;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_IsDisposed = true;
                CancelInFlightLoad();

                if (m_Catalogue != null)
                {
                    m_Catalogue.Changed -= OnCatalogueChanged;
                    m_Catalogue.BurstEnded -= OnBurstEnded;
                    m_Catalogue = null;
                }

                m_Root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

                foreach (var screenshot in m_Screenshots)
                    DisposeScreenshot(screenshot);
                m_Screenshots.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
