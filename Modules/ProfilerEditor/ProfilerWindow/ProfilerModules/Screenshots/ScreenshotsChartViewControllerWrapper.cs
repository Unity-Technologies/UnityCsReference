// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    /// <summary>
    /// Wrapper that presents the screenshot timeline as a ChartViewController.
    /// This allows the screenshot timeline to appear in the main Profiler chart area.
    /// </summary>
    internal class ScreenshotsChartViewControllerWrapper : ChartViewController
    {
        readonly ProfilerWindow m_ProfilerWindow;
        ScreenshotsChartTimelineViewController m_TimelineViewController;
        bool m_HasLoadedData;

        public ScreenshotsChartViewControllerWrapper(ProfilerModule module, ProfilerWindow profilerWindow, ChartModel model)
            : base(module, ProfilerModuleChartType.Line, model)
        {
            m_ProfilerWindow = profilerWindow;

            model.Tooltip = L10n.Tr("A chart showing a selection of recorded screenshots.");

            // We share the module's ChartModel with the base ProfilerModule.Update() path, which
            // re-anchors chartDomainOffset every frame from ProfilerDriver — that's what keeps the
            // frame selector aligned with the other modules during live recording. The downside of
            // sharing is that the model carries the module's placeholder counter series, which the
            // chart mesh would otherwise plot as a line over the thumbnail strip. We only want the
            // mesh's frame-selector overlay, not a plotted line, so disable the series.
            foreach (var series in model.series)
                series.enabled = false;

            // Set up ModuleSelected action (normally done in ProfilerModule.CreateChartViewController)
            ModuleSelected = () =>
            {
                profilerWindow.selectedModule = module;
            };

            // Set up SelectedFrameChanged action (normally done in ProfilerModule.CreateChartViewController)
            SelectedFrameChanged = (frameIndex) =>
            {
                // Clamp frame index to valid range
                // When clicking/dragging in empty space (before first recorded frame), jump to first available frame
                var clampedFrame = frameIndex;
                if (HasValidFrameRange())
                    clampedFrame = Mathf.Clamp(frameIndex, ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);

                profilerWindow.SetCurrentFrame(clampedFrame);
            };
        }

        protected override void ViewLoaded()
        {
            // Base init runs first so chart infrastructure (frame selector, mesh, etc.) is wired up
            // before we slot the timeline view in alongside it.
            base.ViewLoaded();

            View.AddToClassList("screenshots-chart-view");

            // Lock height to 41px to match the Bottlenecks and Highlights chart strips. All three of
            // min/height/max are needed because a flex parent would otherwise stretch or shrink the row.
            View.style.minHeight = 41;
            View.style.height = 41;
            View.style.maxHeight = 41;

            m_TimelineViewController = new ScreenshotsChartTimelineViewController(m_ProfilerWindow);
            var timelineView = m_TimelineViewController.View;

            // Slot the timeline beneath the chart mesh (which carries the frame selector overlay).
            // The selector is absolutely positioned so it stays on top once we insert at index 0.
            var chartContainer = View.Q("profiler-chart-view__chart");
            if (chartContainer != null)
            {
                chartContainer.style.flexGrow = 1;
                chartContainer.style.flexDirection = FlexDirection.Column;
                chartContainer.style.minHeight = 40;

                timelineView.style.position = Position.Absolute;
                timelineView.style.left = 0;
                timelineView.style.top = 0;
                timelineView.style.right = 0;
                timelineView.style.bottom = 0;
                chartContainer.Insert(0, timelineView);
            }
            else
            {
                // Fallback for UXML that doesn't expose the chart container by that name.
                View.Add(timelineView);
            }

            // Hide the legend series list (we have no counters); keep the header for the module name.
            var legendSeries = View.Q("profiler-chart-view__legend__series");
            if (legendSeries != null)
                legendSeries.style.display = DisplayStyle.None;

            m_ProfilerWindow.SelectedFrameIndexChanged += OnProfilerWindowFrameChanged;
            ((IProfilerWindowController)m_ProfilerWindow).currentFrameChanged += OnCurrentFrameChanged;

            // Returning to Edit Mode leaves the chart strip and frame selector stale (no profileCleared
            // fires, but no refresh happens either). Force a reload so the strip and slider repopulate
            // without waiting for the user to click a new frame.
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // The shared ChartModel is seeded and re-anchored by ProfilerModule.Update() (the same
            // path every other module uses), including the synchronous Update() that follows a
            // ProfilerModule.Rebuild() — so the frame selector populates there and we don't need to
            // seed the model here. Just record that data is present so subsequent events relayout
            // rather than full-reload.
            if (ProfilerDriver.firstFrameIndex >= 0 && ProfilerDriver.lastFrameIndex >= 0)
                m_HasLoadedData = true;

            // Initial load — defer one schedule tick so ProfilerDriver data is ready.
            View.schedule.Execute(() => m_TimelineViewController?.ReloadTimeline());
        }

        static bool HasValidFrameRange()
            => ProfilerDriver.firstFrameIndex >= 0 && ProfilerDriver.lastFrameIndex >= 0;

        // Refresh the thumbnail strip. The chart model and frame selector are maintained separately
        // by ProfilerModule.Update(), so there's nothing to push here.
        // fullReload=true rescans all screenshots and rebuilds the strip from scratch (needed when
        // data first arrives or the module re-activates); false just repositions the existing
        // thumbnails (cheaper, sufficient when only the frame range or capacity has shifted).
        void RefreshIfDataAvailable(bool fullReload)
        {
            if (!HasValidFrameRange())
                return;

            if (fullReload)
                m_TimelineViewController?.ReloadTimeline();
            else
                m_TimelineViewController?.Relayout();
            m_HasLoadedData = true;
        }

        void OnCurrentFrameChanged(int frame, bool isRecording)
        {
            RefreshIfDataAvailable(fullReload: !m_HasLoadedData);
        }

        void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.EnteredEditMode)
                return;
            if (!HasValidFrameRange())
                return;

            // Returning to Edit Mode doesn't fire a profile-cleared/refresh. Nudge the base chart to
            // repaint — Update() calls UpdateSelection(), which re-shows the frame selector overlay
            // (gated on m_Model.firstSelectableFrame != -1) — and rebuild the thumbnail strip.
            Update();
            m_TimelineViewController?.ReloadTimeline();
            m_HasLoadedData = true;
        }

        void OnProfilerWindowFrameChanged(long frameIndex)
        {
            // Suppress the very-first reload if we haven't seen a valid frame yet — otherwise we'd
            // rebuild the strip from an empty driver state. Subsequent events (after m_HasLoadedData
            // is set) always relayout so the strip tracks frame-range changes.
            if (!m_HasLoadedData && frameIndex < 0)
                return;

            RefreshIfDataAvailable(fullReload: !m_HasLoadedData);
        }

        public override void SetActiveState(bool active)
        {
            base.SetActiveState(active);

            // Always do a full reload on activation — data may have arrived while we were inactive
            // and a relayout alone would miss it.
            if (active)
                RefreshIfDataAvailable(fullReload: true);
        }

        public override void Clear()
        {
            base.Clear();
            m_HasLoadedData = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_TimelineViewController != null)
                {
                    m_TimelineViewController.Dispose();
                    m_TimelineViewController = null;
                }

                if (m_ProfilerWindow != null)
                {
                    m_ProfilerWindow.SelectedFrameIndexChanged -= OnProfilerWindowFrameChanged;
                    ((IProfilerWindowController)m_ProfilerWindow).currentFrameChanged -= OnCurrentFrameChanged;
                }

                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }

            base.Dispose(disposing);
        }
    }
}
