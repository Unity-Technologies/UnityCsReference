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
        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        SummaryType m_SelectedSummaryType;
        ViewController m_SelectedViewController;
        bool m_CaptureSummaryRequiresReload;
        bool m_SelectionSummaryRequiresReload;
        double m_TimeOfLastNewProfilerFrame;
        bool m_IsWaitingForNoNewFramesToReloadData;
        protected readonly Dictionary<VisualElement, IDetailsProvider> m_DetailsProviders = new();

        // View.
        ToolbarMenu m_SummaryTypeMenu;
        Label m_TitleLabel;
        VisualElement m_ContentContainer;
        Label m_RecordingLabel;

        // Children.
        RangeSummaryViewController m_CaptureSummaryViewController;
        SelectionSummaryViewController m_SelectionSummaryViewController;

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

            m_RecordingLabel.text = "Recording...";
            SetRecordingLabelVisible(false);

            var selectedSummaryType = (SummaryType)m_SettingsService.BottleneckDetailsViewSelectedSummaryType;
            if (selectedSummaryType == SummaryType.None)
            {
                // Display 'Capture' summary by default.
                selectedSummaryType = SummaryType.Capture;
            }

            ShowSummary(selectedSummaryType);

            // Register mouse click handler
            View.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_ProfilerWindow.SelectedFrameIndexChanged -= OnNewFrameIndexSelectedInProfilerWindow;
                m_DataService.NewFrameRecorded -= OnNewProfilerFrameRecorded;
                m_DataService.DataLoaded -= OnProfilerDataClearedOrLoaded;
                m_DataService.DataCleared -= OnProfilerDataClearedOrLoaded;
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_SummaryTypeMenu = view.Q<ToolbarMenu>("bottlenecks-details-view__summary-type-menu");
            m_TitleLabel = view.Q<Label>("bottlenecks-details-view__title-label");
            m_ContentContainer = view.Q<VisualElement>("bottlenecks-details-view__content");
            m_RecordingLabel = view.Q<Label>("bottlenecks-details-view__recording-label");
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
            m_TimeOfLastNewProfilerFrame = UnityEngine.Time.realtimeSinceStartupAsDouble;

            // If not already, start a timer to reload data when we stop receiving
            // new Profiler frames.
            if (m_IsWaitingForNoNewFramesToReloadData == false)
            {
                m_IsWaitingForNoNewFramesToReloadData = true;
                SetRecordingLabelVisible(true);

                const float k_ReloadDelayS = 1f;
                View.schedule.Execute(() =>
                {
                    var now = UnityEngine.Time.realtimeSinceStartupAsDouble;
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

        void ReloadData()
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

            SwitchToolbarTextForSummary(type);
            SwitchContentViewForSummary(type);
            ReloadDataForSummaryIfNecessary(type);
            m_SelectedSummaryType = type;

            // Preserve selection choice across view lifecycle.
            m_SettingsService.BottleneckDetailsViewSelectedSummaryType = (int)type;
        }

        void SwitchToolbarTextForSummary(SummaryType type)
        {
            if (type == SummaryType.None)
                throw new ArgumentException("Invalid summary type.");

            m_TitleLabel.text = type switch
            {
                SummaryType.Capture => "Capture Highlights",
                SummaryType.Selection => "Selection Highlights",
                _ => throw new NotImplementedException(),
            };

            m_SummaryTypeMenu.text = type switch
            {
                SummaryType.Capture => "Show highlights for: Capture",
                SummaryType.Selection => "Show highlights for: Selection",
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
            m_CaptureSummaryViewController.ReloadData(Range.All);
            m_CaptureSummaryRequiresReload = false;
        }

        void ReloadSelectionSummary()
        {
            var selectedFrameRange = GetProfilerWindowSelectionRange();
            m_SelectionSummaryViewController.ReloadData(selectedFrameRange);
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
            UIUtility.SetElementDisplay(m_ContentContainer, !visible);
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
            // If no target, return early
            var target = evt.target as VisualElement;
            if (target == null)
                return;

            // If no right mouse button clicked, return early
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            // Go up the hierarchy and try to find a bound details provider
            IDetailsProvider detailsProvider = null;
            VisualElement currentElement = target;
            while (currentElement != null)
            {
                if (m_DetailsProviders.TryGetValue(currentElement, out detailsProvider))
                    break;

                currentElement = currentElement.parent;
            }

            if (detailsProvider == null)
                return;

            if (evt.button == (int)MouseButton.RightMouse)
            {
                // Show assistant popup window on right mouse click
                if (!((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).CpuProfilerAssistantSupported)
                    return;

                try
                {
                    IDetailsProvider.AssistantRequestContext context = detailsProvider.GetAssistantContext(m_DataService);

                    // Invoke profiler assistant
                    var layout = target.localBound;
                    var worldPos = target.LocalToWorld(new Vector2());
                    var screenPos = GUIUtility.GUIToScreenPoint(worldPos);
                    var screenRect = new Rect(screenPos, layout.size);

                    var attachment = new CpuProfilerAssistantController.CpuProfilerContext(m_ProfilerWindow.CurrentLoadedCaptureFile,
                        context.Attachment.FrameRange, context.Attachment.ThreadName, context.Attachment.MarkerIdPath, context.Attachment.MarkerName);
                    string prompt = context.Prompt;

                    ((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).RequestCpuProfilerAssistance(screenRect, attachment, prompt);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to launch Profiler Assistant: {e.Message}");
                }
                finally
                {
                    evt.StopPropagation();
                }
            }
        }
    }
}
