// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.UIElements;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    /// <summary>
    /// Interface for visual elements that can be selected in the details view.
    /// </summary>
    interface ISelectedDetailsViewElement
    {
        /// <summary>
        /// Sets the selected state of the element.
        /// </summary>
        /// <param name="value">True to select the element, false to deselect it.</param>
        void SetSelected(bool value);
    }

    /*  The BottlenecksDetailsViewController has two responsibilities:
     *      1. Allow a user to switch between the 'capture' summary and the 'selection' summary.
     *          - This is achieved by embedding the relevant child view controller in response to a switch.
     *          - When 'Capture' is selected, a RangeSummaryViewController is embedded and configured to show the whole capture.
     *          - When 'Selection' is selected, a SelectionSummaryViewController is embedded and configured with the current selection range. Note that it is this SelectionSummaryViewController that is responsible for displaying either a RangeSummaryViewController or a FrameSummaryViewController, depending on if the selection is a range or a single frame.
     *      2. Handle and respond to Profiler window interactions. These are:
     *          - When Profiler data is cleared or loaded, ensure the child view controllers are reloaded now or when next displayed.
     *          - When Profiler data is recording, ensure the child view controllers are reloaded once recording stops (via a timer).
                - When a new selection is made in the Profiler window, ensure the SelectionSummaryViewController is reloaded now or when next displayed.
     */
    class BottlenecksDetailsViewController : ViewController, SummaryViewController.IResponder, IDetailsElementBinder
    {
        const int    k_DetailsSplitViewFixedPaneMinSize = 270;
        const string k_DetailsSplitViewFixedPaneSizePreferenceKey = "ProfilerWindow.Overview.DetailsPanel.Size";
        const string k_DetailsSplitViewToggleIsVisibleStatePreferenceKey = "ProfilerWindow.Overview.DetailsPanel.Visible";

        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        SummaryType m_SelectedSummaryType;
        ViewController m_SelectedViewController;
        bool m_CaptureSummaryRequiresReload;
        bool m_SelectionSummaryRequiresReload;
        IDetailsProvider m_CaptureSummaryDetailsProvider;
        IDetailsProvider m_SelectionSummaryDetailsProvider;
        double m_TimeOfLastNewProfilerFrame;
        bool m_IsWaitingForNoNewFramesToReloadData;
        protected readonly Dictionary<VisualElement, IDetailsProvider> m_DetailsProviders = new();
        VisualElement m_SelectedDetailsElement;

        // View.
        ToolbarMenu m_SummaryTypeMenu;
        Label m_TitleLabel;
        Button m_DetailsPanelButton;
        TwoPaneSplitView m_DetailsSplitView;
        VisualElement m_ContentContainer;
        VisualElement m_DetailsPanelContainer;
        Label m_RecordingLabel;

        // Children.
        RangeSummaryViewController m_CaptureSummaryViewController;
        SelectionSummaryViewController m_SelectionSummaryViewController;
        BottlenecksDetailsPanelViewController m_DetailsPanelViewController;

        public BottlenecksDetailsViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            m_SelectedSummaryType = SummaryType.None;
            m_CaptureSummaryRequiresReload = true;
            m_SelectionSummaryRequiresReload = true;

            m_DataService.DataCleared += OnProfilerDataClearedOrLoaded;
            m_DataService.DataLoaded += OnProfilerDataClearedOrLoaded;
            m_DataService.NewFrameRecorded += OnNewProfilerFrameRecorded;
            m_ProfilerWindow.SelectedFrameIndexChanged += OnNewFrameIndexSelectedInProfilerWindow;
            m_SettingsService.TargetFrameDurationChanged += OnTargetFrameDurationChanged;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("BottlenecksDetailsView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "bottlenecks-details-view__dark";
            const string k_UssClass_Light = "bottlenecks-details-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            // Configure menu.
            m_SummaryTypeMenu.menu.AppendAction("Capture", (action) => { ShowSummary(SummaryType.Capture); });
            m_SummaryTypeMenu.menu.AppendAction("Selection", (action) => { ShowSummary(SummaryType.Selection); });

            // Embed child view controllers.
            m_CaptureSummaryViewController = new RangeSummaryViewController(
                   m_DataService,
                   m_SettingsService,
                   m_ProfilerWindow,
                   responder: this,
                   this,
                   true);
            AddChild(m_CaptureSummaryViewController);

            m_SelectionSummaryViewController = new SelectionSummaryViewController(
                   m_DataService,
                   m_SettingsService,
                   m_ProfilerWindow,
                   responder: this,
                   this);
            AddChild(m_SelectionSummaryViewController);

            m_DetailsPanelViewController = new BottlenecksDetailsPanelViewController(
                   m_DataService,
                   m_SettingsService,
                   m_ProfilerWindow);
            AddChild(m_DetailsPanelViewController);

            m_RecordingLabel.text = "Recording...";
            SetRecordingLabelVisible(false);

            var selectedSummaryType = (SummaryType)m_SettingsService.BottleneckDetailsViewSelectedSummaryType;
            if (selectedSummaryType == SummaryType.None)
            {
                // Display 'Capture' summary by default.
                selectedSummaryType = SummaryType.Capture;
            }

            ShowSummary(selectedSummaryType);

            var detailsPanelVisible = EditorPrefs.GetBool(k_DetailsSplitViewToggleIsVisibleStatePreferenceKey, true);
            m_DetailsSplitView.fixedPaneIndex = 1;
            SetDetailsPanelVisible(detailsPanelVisible);
            m_DetailsPanelButton.clicked += () => SetDetailsPanelVisible(m_DetailsSplitView.fixedPane.style.display == DisplayStyle.None);

            // Save panel size when user finishes resizing
            var dragLineAnchor = m_DetailsSplitView.Q("unity-dragline-anchor");
            dragLineAnchor?.RegisterCallback<PointerUpEvent>(OnDetailsSplitViewResizeComplete);

            // Register mouse click handler
            View.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        private void SetDetailsPanelVisible(bool visible)
        {
            if (visible)
            {
                // Restore saved size and show the panel.
                var detailsPanelSize = EditorPrefs.GetFloat(k_DetailsSplitViewFixedPaneSizePreferenceKey);
                detailsPanelSize = Mathf.Max(detailsPanelSize, k_DetailsSplitViewFixedPaneMinSize);
                m_DetailsSplitView.fixedPaneInitialDimension = detailsPanelSize;
                m_DetailsSplitView.UnCollapse();

                if (!m_DetailsPanelViewController.IsViewLoaded)
                    m_DetailsPanelContainer.Add(m_DetailsPanelViewController.View);
            }
            else
            {
                m_DetailsSplitView.CollapseChild(1);
            }
            EditorPrefs.SetBool(k_DetailsSplitViewToggleIsVisibleStatePreferenceKey, visible);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_SettingsService.TargetFrameDurationChanged -= OnTargetFrameDurationChanged;
                m_ProfilerWindow.SelectedFrameIndexChanged -= OnNewFrameIndexSelectedInProfilerWindow;
                m_DataService.NewFrameRecorded -= OnNewProfilerFrameRecorded;
                m_DataService.DataLoaded -= OnProfilerDataClearedOrLoaded;
                m_DataService.DataCleared -= OnProfilerDataClearedOrLoaded;
            }

            base.Dispose(disposing);
        }

        void OnDetailsSplitViewResizeComplete(PointerUpEvent evt)
        {
            // Save the size of the details panel when the user finishes resizing it, so it can be restored next time it's shown.
            if (m_DetailsSplitView?.fixedPane is { resolvedStyle: not null })
            {
                var currentSize = m_DetailsSplitView.fixedPane.resolvedStyle.width;
                if (!float.IsNaN(currentSize) && currentSize >= k_DetailsSplitViewFixedPaneMinSize)
                {
                    EditorPrefs.SetFloat(k_DetailsSplitViewFixedPaneSizePreferenceKey, currentSize);
                }
            }
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_SummaryTypeMenu = view.Q<ToolbarMenu>("bottlenecks-details-view__summary-type-menu");
            m_TitleLabel = view.Q<Label>("bottlenecks-details-view__title-label");
            m_DetailsSplitView = view.Q<TwoPaneSplitView>("bottlenecks-details-view-split");
            m_ContentContainer = view.Q<VisualElement>("bottlenecks-details-view__content");
            m_RecordingLabel = view.Q<Label>("bottlenecks-details-view__recording-label");
            m_DetailsPanelButton = view.Q<Button>("bottlenecks-details-view__toolbar-details-panel-button");
            m_DetailsPanelContainer = view.Q<VisualElement>("bottlenecks-details-view-details-panel__content");
        }

        void OnProfilerDataClearedOrLoaded()
        {
            if (IsViewLoaded == false)
                return;

            // Profiler data has changed. Cancel any in-flight builders.
            CancelReloadDataIfNecessary();

            // Profiler data being cleared or loaded stops the Profiler recording
            // process. Therefore, cancel the pending data reload for new profiler
            // frames, if necessary, and reload data now.
            if (m_IsWaitingForNoNewFramesToReloadData)
            {
                m_IsWaitingForNoNewFramesToReloadData = false;
                SetRecordingLabelVisible(false);
            }

            ReloadData();
        }

        void OnNewProfilerFrameRecorded(int connectionId, int newFrameIndex)
        {
            if (IsViewLoaded == false)
                return;

            // Profiler data has changed. Cancel any in-flight builders.
            CancelReloadDataIfNecessary();

            // Record the time at which we received this new profiler frame.
            m_TimeOfLastNewProfilerFrame = Time.realtimeSinceStartupAsDouble;

            // If not already, start a timer to reload data when we stop receiving
            // new Profiler frames.
            if (m_IsWaitingForNoNewFramesToReloadData == false)
            {
                m_IsWaitingForNoNewFramesToReloadData = true;
                SetRecordingLabelVisible(true);

                const float k_ReloadDelayS = 1f;
                View.schedule.Execute(() =>
                {
                    var now = Time.realtimeSinceStartupAsDouble;
                    if (now >= m_TimeOfLastNewProfilerFrame + k_ReloadDelayS)
                    {
                        m_IsWaitingForNoNewFramesToReloadData = false;
                        SetRecordingLabelVisible(false);
                        ReloadData();
                    }
                }).Until(() =>
                {
                    return m_IsWaitingForNoNewFramesToReloadData == false;
                });
            }
        }

        void OnNewFrameIndexSelectedInProfilerWindow(long _)
        {
            if (IsViewLoaded == false)
                return;

            // If there is a pending data reload, do not respond to a change in
            // selection. Data will be reloaded when recording stops.
            if (m_IsWaitingForNoNewFramesToReloadData)
                return;

            // If the selection summary is not the active view, ensure it will be
            // reloaded on its next appearance and switch to it. Otherwise, just
            // reload the already displayed selection summary.
            if (m_SelectedSummaryType != SummaryType.Selection)
            {
                m_SelectionSummaryRequiresReload = true;
                ShowSummary(SummaryType.Selection);
            }
            else
            {
                ReloadSelectionSummary();
            }
        }

        void OnTargetFrameDurationChanged()
        {
            if (IsViewLoaded == false)
                return;

            // Reload data to update details panel with new target frame duration
            ReloadData();
        }

        protected virtual void ReloadData()
        {
            // Reload the active summary view and ensure the other is reloaded the
            // next time.
            var selectedSummaryType = m_SelectedSummaryType;
            m_CaptureSummaryRequiresReload = true;
            m_SelectionSummaryRequiresReload = true;
            ReloadDataForSummaryIfNecessary(selectedSummaryType);
        }

        void ShowSummary(SummaryType type)
        {
            if (type == SummaryType.None)
                throw new ArgumentException("Invalid summary type selected.");

            if (type == m_SelectedSummaryType)
                return;

            // Clear any previous selection when switching views
            if (m_SelectedDetailsElement is ISelectedDetailsViewElement previousSelected)
            {
                previousSelected.SetSelected(false);
            }
            m_SelectedDetailsElement = null;

            SwitchToolbarTextForSummary(type);
            SwitchContentViewForSummary(type);

            var needsReload = type switch
            {
                SummaryType.Capture => m_CaptureSummaryRequiresReload,
                SummaryType.Selection => m_SelectionSummaryRequiresReload,
                _ => false,
            };

            ReloadDataForSummaryIfNecessary(type);

            // If no async reload was triggered, apply the cached provider for this summary type
            // so the details panel reflects the newly shown summary rather than the previous one.
            if (!needsReload)
            {
                var cachedProvider = type switch
                {
                    SummaryType.Capture => m_CaptureSummaryDetailsProvider,
                    SummaryType.Selection => m_SelectionSummaryDetailsProvider,
                    _ => null,
                };
                if (cachedProvider != null)
                    m_DetailsPanelViewController.SetDetailsProvider(cachedProvider);
            }

            m_SelectedSummaryType = type;

            // Preserve selection choice across view lifecycle.
            m_SettingsService.BottleneckDetailsViewSelectedSummaryType = (int)type;
        }

        void SwitchToolbarTextForSummary(SummaryType type)
        {
            if (type == SummaryType.None)
                throw new ArgumentException("Invalid summary type.");

            (m_TitleLabel.text, m_SummaryTypeMenu.text) = type switch
            {
                SummaryType.Capture => ("Capture Highlights", "Show highlights for: Capture"),
                SummaryType.Selection => ("Selection Highlights", "Show highlights for: Selection"),
                _ => throw new NotImplementedException(),
            };
        }

        void SwitchContentViewForSummary(SummaryType type)
        {
            ViewController fromViewController = m_SelectedViewController;
            ViewController toViewController = type switch
            {
                SummaryType.Capture => m_CaptureSummaryViewController,
                SummaryType.Selection => m_SelectionSummaryViewController,
                _ => throw new NotImplementedException(),
            };

            UnityEngine.Debug.Assert(toViewController != fromViewController);

            // Load the 'to' view controller's if being displayed for the first time.
            if (toViewController.IsViewLoaded == false)
                m_ContentContainer.Add(toViewController.View);

            // Make the 'to' view controller's view visible.
            UIUtility.SetElementDisplay(toViewController.View, true);

            // Hide the 'from' view controller's view, if it exists (i.e. if it's not the first selection).
            if (fromViewController != null)
                UIUtility.SetElementDisplay(fromViewController.View, false);

            // Update the selected view controller.
            m_SelectedViewController = toViewController;
        }

        void ReloadDataForSummaryIfNecessary(SummaryType type)
        {
            switch (type)
            {
                case SummaryType.Capture:
                {
                    if (m_CaptureSummaryRequiresReload)
                        ReloadCaptureSummary();
                    break;
                }

                case SummaryType.Selection:
                {
                    if (m_SelectionSummaryRequiresReload)
                        ReloadSelectionSummary();
                    break;
                }

                default:
                    break;
            }
        }

        void ReloadCaptureSummary()
        {
            var frameRange = Range.All;
            m_CaptureSummaryViewController.ReloadData(frameRange, detailsProvider =>
            {
                m_CaptureSummaryDetailsProvider = detailsProvider;
                m_DetailsPanelViewController.SetDetailsProvider(detailsProvider);
            });
            m_CaptureSummaryRequiresReload = false;
        }

        void ReloadSelectionSummary()
        {
            var selectedFrameRange = GetProfilerWindowSelectionRange();
            m_SelectionSummaryViewController.ReloadData(selectedFrameRange, detailsProvider =>
            {
                m_SelectionSummaryDetailsProvider = detailsProvider;
                m_DetailsPanelViewController.SetDetailsProvider(detailsProvider);
            });
            m_SelectionSummaryRequiresReload = false;
        }

        void CancelReloadDataIfNecessary()
        {
            // Instruct both of our child view controllers to cancel any in-flight builders.
            m_CaptureSummaryViewController.CancelReloadDataIfNecessary();
            m_SelectionSummaryViewController.CancelReloadDataIfNecessary();
        }

        Range GetProfilerWindowSelectionRange()
        {
            var selectedFrameRange = m_ProfilerWindow.SelectedFrameRange;
            if (selectedFrameRange.HasValue == false)
            {
                var frameCount = m_DataService.FrameCount;
                if (frameCount > 0)
                {
                    // The Profiler Window uses a frame index of -1 to signify both 'no selection'
                    // as well as 'current frame selected'. In this case, the new SelectedFrameRange
                    // will be null. When this is null, we use the last frame index to match the
                    // existing 'current frame selected' behaviour.
                    var exclusiveLastFrameIndex = m_DataService.FirstFrameIndex + frameCount;
                    selectedFrameRange = new Range(exclusiveLastFrameIndex - 1, exclusiveLastFrameIndex);
                }
                else
                {
                    // No profiler data; return a zero length range.
                    selectedFrameRange = new Range(0, 0);
                }
            }

            return selectedFrameRange.Value;
        }

        void SetRecordingLabelVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_RecordingLabel, visible);
            UIUtility.SetElementDisplay(m_DetailsSplitView, !visible);
        }

        void SummaryViewController.IResponder.OnTopMarkerSelected(Marker marker, TopMarkersViewController.Action action)
        {
            switch (action)
            {
                case TopMarkersViewController.Action.ChangeSelectedFrame:
                {
                    var selectedFrameIndex = m_ProfilerWindow.selectedFrameIndex;
                    var isFrameAlreadySelectedInProfiler = marker.FrameIndex == selectedFrameIndex;
                    m_ProfilerWindow.selectedFrameIndex = marker.FrameIndex;
                    if (isFrameAlreadySelectedInProfiler)
                    {
                        // If this frame is already selected in the Profiler window, invoke
                        // the OnNewFrameIndexSelectedInProfilerWindow callback manually.
                        OnNewFrameIndexSelectedInProfilerWindow(selectedFrameIndex);
                    }
                    break;
                }

                case TopMarkersViewController.Action.SwitchToCpuModule:
                {
                    // We don't use the ProfilerTimeSampleSelection API here because:
                    //  1. We don't have a specific marker instance to highlight; there could be multiple in the frame.
                    //  2. We need to store/lookup more marker data than we currently do to use the API, including threadGroupName, threadName, and rawSampleIndex.
                    var cpuModule = m_ProfilerWindow.GetProfilerModuleByType<CPUProfilerModule>();
                    m_ProfilerWindow.selectedModule = cpuModule;
                    m_ProfilerWindow.selectedFrameIndex = marker.FrameIndex;

                    var showFullScriptingMethodNames = cpuModule.ViewOptions.HasFlag(
                        CPUOrGPUProfilerModule.ProfilerViewFilteringOptions.ShowFullScriptingMethodNames);
                    cpuModule.sampleNameSearchFilter = marker.GetFormattedMarkerName(showFullScriptingMethodNames);
                    cpuModule.focusedThreadIndex = marker.ThreadIndex;
                    cpuModule.ViewType = UnityEditorInternal.ProfilerViewType.Hierarchy;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException("Unknown action type.");
            }
        }

        enum SummaryType
        {
            None,
            Capture,
            Selection
        }

        public void BindDetailsElement(VisualElement detailsElement, IDetailsProvider detailsProvider)
        {
            m_DetailsProviders[detailsElement] = detailsProvider;
        }

        public void UnbindDetailsElement(VisualElement detailsElement)
        {
            m_DetailsProviders.Remove(detailsElement);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;
            if (target == null)
                return;

            if (evt.button != (int)MouseButton.RightMouse && evt.button != (int)MouseButton.LeftMouse)
                return;

            // Go up the hierarchy and try to find a bound details provider
            var currentElement = FindElementWithDetailsProvider(target, out var detailsProvider);
            if (detailsProvider == null)
                return;

            // Always select the element (also before showing context menu)
            SelectDetailsProviderElement(currentElement, detailsProvider);

            if (evt.button == (int)MouseButton.RightMouse)
            {
                HandleRightClick(evt, target, detailsProvider);
            }
        }

        VisualElement FindElementWithDetailsProvider(VisualElement startElement, out IDetailsProvider detailsProvider)
        {
            detailsProvider = null;
            var currentElement = startElement;
            while (currentElement != null)
            {
                if (m_DetailsProviders.TryGetValue(currentElement, out detailsProvider))
                    return currentElement;
                currentElement = currentElement.parent;
            }
            return null;
        }

        void HandleRightClick(PointerDownEvent evt, VisualElement target, IDetailsProvider detailsProvider)
        {
            if (!((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).CpuProfilerAssistantSupported)
                return;

            var dropdownMenu = new DropdownMenu();
            dropdownMenu.AppendAction("Ask Assistant", (action) =>
            {
                try
                {
                    IDetailsProvider.AssistantRequestContext context = detailsProvider.GetAssistantContext(m_DataService);

                    var layout = target.localBound;
                    var worldPos = target.LocalToWorld(new Vector2());
                    var screenPos = GUIUtility.GUIToScreenPoint(worldPos);
                    var screenRect = new Rect(screenPos, layout.size);

                    var attachment = new CpuProfilerAssistantController.CpuProfilerContext(
                        m_ProfilerWindow.CurrentLoadedCaptureFile,
                        context.Attachment.FrameRange,
                        context.Attachment.ThreadName,
                        context.Attachment.MarkerIdPath,
                        context.Attachment.MarkerName);

                    ((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).RequestCpuProfilerAssistance(
                        screenRect, attachment, context.Prompt);

                    const string k_LinkDescription_AskAssistant = "Ask Assistant (Context Menu)";
                    UnityEditor.Profiling.Analytics.ProfilerWindowAnalytics.SendBottleneckLinkSelectedEvent(k_LinkDescription_AskAssistant);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to launch Profiler Assistant: {e.Message}");
                }
            });

            View.panel.contextualMenuManager.DisplayMenu(evt, target, dropdownMenu);
            evt.StopPropagation();
        }

        void SelectDetailsProviderElement(VisualElement currentElement, IDetailsProvider detailsProvider)
        {
            // Clear previous selection
            if (m_SelectedDetailsElement is ISelectedDetailsViewElement previousSelected)
            {
                previousSelected.SetSelected(false);
            }

            // Set new selection if element implements the interface
            if (currentElement is ISelectedDetailsViewElement selectedElement)
            {
                selectedElement.SetSelected(true);
                m_SelectedDetailsElement = currentElement;
            }
            else
            {
                m_SelectedDetailsElement = null;
            }

            m_DetailsPanelViewController.SetDetailsProvider(detailsProvider);
        }
    }
}
