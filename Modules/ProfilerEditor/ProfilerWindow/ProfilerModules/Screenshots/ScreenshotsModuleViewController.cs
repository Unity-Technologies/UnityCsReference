// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    internal class ScreenshotsModuleViewController : ProfilerModuleViewController
    {
        const string k_UxmlResourceName = "Profiler/Screenshots/ScreenshotsModuleView.uxml";

        ScreenshotsTimelineViewController m_TimelineViewController;
        ScreenshotDetailsViewController m_DetailsViewController;
        VisualElement m_TimelineContainer;
        VisualElement m_DetailsContainer;
        Label m_ScreenshotCountLabel;
        Label m_FrameInfoLabel;
        ScreenshotIndexCatalogue m_Catalogue;
        readonly ScreenshotsProfilerModule m_Module;

        public ScreenshotsModuleViewController(ProfilerWindow profilerWindow, ScreenshotsProfilerModule module)
            : base(profilerWindow)
        {
            m_Module = module;
        }

        protected override VisualElement CreateView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidOperationException($"Failed to load UXML for {nameof(ScreenshotsModuleViewController)}");

            m_TimelineContainer = view.Q("screenshots-module__timeline-container");
            m_DetailsContainer = view.Q("screenshots-module__details-container");
            var toolbar = view.Q<UnityEditor.UIElements.Toolbar>("screenshots-module__toolbar");
            m_ScreenshotCountLabel = view.Q<Label>("screenshots-module__count-label");
            // Per-frame info label (size / source frame mismatch / loading state) is fed by the details
            // view controller's InfoTextChanged event and surfaced here.
            m_FrameInfoLabel = view.Q<Label>("screenshots-module__frame-info-label");

            if (m_TimelineContainer == null || m_DetailsContainer == null || toolbar == null
                || m_ScreenshotCountLabel == null || m_FrameInfoLabel == null)
                throw new InvalidOperationException($"Failed to find required containers in UXML for {nameof(ScreenshotsModuleViewController)}");

            m_Catalogue = ProfilerWindow.GetScreenshotIndexCatalogue();

            m_TimelineViewController = new ScreenshotsTimelineViewController(ProfilerWindow, m_Module);
            // Pass the catalogue so the details panel can resolve LogicalFrame → EmissionFrame in LoadScreenshot.
            m_DetailsViewController = new ScreenshotDetailsViewController(m_Catalogue);
            m_TimelineContainer.Add(m_TimelineViewController.View);
            m_DetailsContainer.Add(m_DetailsViewController.View);

            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameChanged;
            m_TimelineViewController.FrameSelected += OnTimelineFrameSelected;
            // Drive the count + empty-state from the catalogue directly rather than the details
            // strip's ScreenshotCountChanged: the strip stops reloading during a recording burst, so
            // its count goes stale and the empty-state lingered until profiling paused. The catalogue
            // refreshes (throttled) throughout recording.
            if (m_Catalogue != null)
                m_Catalogue.Changed += OnCatalogueChanged;
            m_DetailsViewController.InfoTextChanged += OnDetailsInfoTextChanged;

            // Returning to Edit Mode leaves the details strip and large image stale (no profileCleared
            // fires, but no refresh happens either). Force a reload so they repopulate without waiting
            // for the user to click a new frame.
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Reducing the frame-count setting trims frames from the display window without changing
            // what's in memory (so no Changed event fires). The chart strip refreshes via the module's
            // chart Rebuild on this same setting change, but the details strip lives in this (separately
            // owned) view controller and isn't recreated, so refresh it explicitly.
            ProfilerUserSettings.settingsChanged += OnProfilerSettingsChanged;

            UpdateScreenshotCountAndEmptyState();
            ApplyFrameSelection((int)ProfilerWindow.selectedFrameIndex);

            return view;
        }

        void OnProfilerSettingsChanged()
        {
            // Rebuild the strip so its visible thumbnails match the (possibly) trimmed display window,
            // and re-apply the selection so the detail image tracks the new window.
            m_TimelineViewController?.ReloadTimeline();
            ApplyFrameSelection((int)ProfilerWindow.selectedFrameIndex);
        }

        void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.EnteredEditMode)
                return;

            // Refresh may be a no-op (nothing changed since the last scan) and so not raise Changed,
            // so re-apply the selection explicitly to repopulate the stale strip and detail image.
            m_Catalogue?.Refresh();
            ApplyFrameSelection((int)ProfilerWindow.selectedFrameIndex);
        }

        void OnSelectedFrameChanged(long frameIndex)
        {
            ApplyFrameSelection((int)frameIndex);
        }

        // Sync the strip selection and detail image to the profiler's selected frame. The strip
        // highlights the screenshot the detail panel is actually showing (the nearest one at or
        // before the frame) so the two never disagree about which screenshot is selected.
        void ApplyFrameSelection(int profilerFrame)
        {
            if (profilerFrame < 0)
                profilerFrame = (int)ProfilerWindow.lastAvailableFrameIndex;
            if (profilerFrame < 0)
                return;

            // Bound the detail image's nearest-prior fallback to the same display window the strip and
            // chart use, so it never surfaces a screenshot from a frame trimmed out of the window.
            var firstDisplayedFrame = ScreenshotIndexCatalogue.FirstDisplayedFrameIndex();
            if (firstDisplayedFrame < 0)
                firstDisplayedFrame = 0;

            m_TimelineViewController?.UpdateSelection(ResolveDisplayFrame(profilerFrame));
            m_DetailsViewController?.LoadScreenshot(profilerFrame, firstDisplayedFrame);
        }

        // The screenshot the strip should highlight for a given profiler frame: the screenshot at
        // that frame if one exists, else the nearest one before it (matching what the detail panel
        // displays). When there's no screenshot at or before the frame, returns the frame unchanged
        // and the strip falls back to highlighting the closest thumbnail.
        int ResolveDisplayFrame(int profilerFrame)
        {
            if (m_Catalogue == null || m_Catalogue.TryGetEmissionFrame(profilerFrame, out _))
                return profilerFrame;
            if (m_Catalogue.TryGetNearestPriorLogicalFrame(profilerFrame, out var nearest))
                return nearest.LogicalFrame;

            return profilerFrame;
        }

        void OnTimelineFrameSelected(int frameIndex)
        {
            ProfilerWindow.selectedFrameIndex = frameIndex;
        }

        void OnCatalogueChanged()
        {
            UpdateScreenshotCountAndEmptyState();
            // Reapply selection now that the catalogue has settled.
            ApplyFrameSelection((int)ProfilerWindow.selectedFrameIndex);
        }

        // Source the toolbar count and the details empty-state from the catalogue (the authoritative,
        // always-current frame list) so both stay correct during a live recording, not just once it
        // pauses.
        void UpdateScreenshotCountAndEmptyState()
        {
            var count = m_Catalogue?.Frames.Count ?? 0;
            UpdateScreenshotCountLabel(count);
            m_DetailsViewController?.ShowEmptyState(count == 0);
        }

        void OnDetailsInfoTextChanged(string text)
        {
            if (m_FrameInfoLabel != null)
                m_FrameInfoLabel.text = text ?? string.Empty;
        }

        void UpdateScreenshotCountLabel(int count)
        {
            if (m_ScreenshotCountLabel != null)
            {
                m_ScreenshotCountLabel.text = count == 1
                    ? "1 screenshot"
                    : $"{count} screenshots";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                ProfilerUserSettings.settingsChanged -= OnProfilerSettingsChanged;

                if (ProfilerWindow != null)
                    ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameChanged;

                if (m_Catalogue != null)
                {
                    m_Catalogue.Changed -= OnCatalogueChanged;
                    m_Catalogue = null;
                }

                if (m_TimelineViewController != null)
                {
                    m_TimelineViewController.FrameSelected -= OnTimelineFrameSelected;
                    m_TimelineViewController.Dispose();
                }

                if (m_DetailsViewController != null)
                {
                    m_DetailsViewController.InfoTextChanged -= OnDetailsInfoTextChanged;
                    m_DetailsViewController.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
