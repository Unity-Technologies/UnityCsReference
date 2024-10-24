// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.Profiling.Analytics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksDetailsViewController : ViewController
    {
        const string k_UxmlResourceName = "BottlenecksDetailsView.uxml";
        const string k_UssClass_Dark = "bottlenecks-details-view__dark";
        const string k_UssClass_Light = "bottlenecks-details-view__light";
        const string k_UxmlIdentifier_CpuLabel = "bottlenecks-details-view__cpu-name-label";
        const string k_UxmlIdentifier_GpuLabel = "bottlenecks-details-view__gpu-name-label";
        const string k_UxmlIdentifier_CpuBar = "bottlenecks-details-view__cpu-duration-bar";
        const string k_UxmlIdentifier_GpuBar = "bottlenecks-details-view__gpu-duration-bar";
        const string k_UxmlIdentifier_CpuDurationLabel = "bottlenecks-details-view__cpu-duration-label";
        const string k_UxmlIdentifier_GpuDurationLabel = "bottlenecks-details-view__gpu-duration-label";
        const string k_UxmlIdentifier_TargetFrameDurationIndicator = "bottlenecks-details-view__target-frame-duration-indicator";
        const string k_UxmlIdentifier_TargetFrameDurationIndicatorLabel = "bottlenecks-details-view__target-frame-duration-indicator__label";
        const string k_UxmlIdentifier_TargetFrameDurationInstructionLabel = "bottlenecks-details-view__target-frame-duration-instruction-label";
        const string k_UxmlIdentifier_DescriptionLabel = "bottlenecks-details-view__description-label";
        const string k_UxmlIdentifier_NoDataLabel = "bottlenecks-details-view__no-data-label";
        const string k_UssClass_DurationBarFillHighlighted = "bottlenecks-details-view__chart-bar__fill-highlighted";
        const string k_UssClass_LinkCursor = "link-cursor";
        const string k_LinkDescription_OpenCpuTimeline = "Open CPU Timeline";
        const string k_LinkDescription_OpenProfileAnalyzer = "Open Profile Analyzer";
        const string k_LinkDescription_OpenFrameDebugger = "Open Frame Debugger";
        const string k_LinkDescription_OpenGpuProfilerDocumentation = "Open GPU Profiler Documentation";

        static readonly string k_CpuActiveTimeTooltip = L10n.Tr("CPU Active Time is the duration within the frame that the CPU was doing work for.\n\nThis is computed by taking the longest thread duration between the main thread and the render thread, and subtracting the time that thread spent waiting, including waiting for 'present' and 'target FPS'.\n\nIt is possible for this duration to be longer than the 'CPU Time' value shown in the CPU Usage module's Timeline view when the Render Thread took longer than the Main Thread. This is because the Timeline view displays the beginning and end of the frame on the main thread.");
        static readonly string k_GpuTimeTooltip = L10n.Tr("GPU Time is the duration between when the GPU was sent its first command for the frame and when the GPU completed its work for that frame.");
        static readonly string k_TargetFrameDurationInstruction = $"The target frame time can be changed via the dropdown in the Highlights chart.";
        static readonly string k_NoValueText = L10n.Tr("No Value");

        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        BottlenecksDetailsViewModel m_Model;

        // View.
        Label m_CpuLabel;
        Label m_GpuLabel;
        VisualElement m_CpuBar;
        VisualElement m_GpuBar;
        Label m_CpuDurationLabel;
        Label m_GpuDurationLabel;
        VisualElement m_TargetFrameDurationIndicator;
        Label m_TargetFrameDurationIndicatorLabel;
        Label m_TargetFrameDurationInstructionLabel;
        Label m_DescriptionLabel;
        Label m_NoDataLabel;

        public BottlenecksDetailsViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;

            m_DataService.NewDataLoadedOrCleared += OnNewDataLoadedOrCleared;
            m_SettingsService.TargetFrameDurationChanged += OnTargetFrameDurationChanged;
            m_ProfilerWindow.SelectedFrameIndexChanged += OnNewFrameIndexSelectedInProfilerWindow;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            UpdateTargetFrameDurationIndicatorLabel();

            m_CpuLabel.tooltip = k_CpuActiveTimeTooltip;
            m_GpuLabel.tooltip = k_GpuTimeTooltip;
            m_CpuBar.RegisterCallback<ClickEvent>(OnCpuBarClicked);
            m_CpuBar.tooltip = $"Click to inspect this frame in the CPU module's Timeline view.";
            m_GpuBar.RegisterCallback<ClickEvent>(OnGpuBarClicked);
            m_GpuBar.tooltip = $"Click to open the Frame Debugger.";

            m_TargetFrameDurationInstructionLabel.text = k_TargetFrameDurationInstruction;

            m_DescriptionLabel.RegisterCallback<PointerDownLinkTagEvent>(OnDescriptionLabelLinkSelected);
            m_DescriptionLabel.RegisterCallback<PointerOverLinkTagEvent>(OnLabelLinkPointerOver);
            m_DescriptionLabel.RegisterCallback<PointerOutLinkTagEvent>(OnLabelLinkPointerOut);

            m_NoDataLabel.text = L10n.Tr("There is no data available for the selected frame.");
            UIUtility.SetElementDisplay(m_NoDataLabel, false);

            View.RegisterCallback<GeometryChangedEvent>(ViewPerformedInitialLayout);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_ProfilerWindow.SelectedFrameIndexChanged -= OnNewFrameIndexSelectedInProfilerWindow;
                m_SettingsService.TargetFrameDurationChanged -= OnTargetFrameDurationChanged;
                m_DataService.NewDataLoadedOrCleared -= OnNewDataLoadedOrCleared;
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_CpuLabel = view.Q<Label>(k_UxmlIdentifier_CpuLabel);
            m_GpuLabel = view.Q<Label>(k_UxmlIdentifier_GpuLabel);
            m_CpuBar = view.Q<VisualElement>(k_UxmlIdentifier_CpuBar);
            m_GpuBar = view.Q<VisualElement>(k_UxmlIdentifier_GpuBar);
            m_CpuDurationLabel = view.Q<Label>(k_UxmlIdentifier_CpuDurationLabel);
            m_GpuDurationLabel = view.Q<Label>(k_UxmlIdentifier_GpuDurationLabel);
            m_TargetFrameDurationIndicator  = view.Q<VisualElement>(k_UxmlIdentifier_TargetFrameDurationIndicator);
            m_TargetFrameDurationIndicatorLabel  = view.Q<Label>(k_UxmlIdentifier_TargetFrameDurationIndicatorLabel);
            m_TargetFrameDurationInstructionLabel  = view.Q<Label>(k_UxmlIdentifier_TargetFrameDurationInstructionLabel);
            m_DescriptionLabel = view.Q<Label>(k_UxmlIdentifier_DescriptionLabel);
            m_NoDataLabel = view.Q<Label>(k_UxmlIdentifier_NoDataLabel);
        }

        void ViewPerformedInitialLayout(GeometryChangedEvent evt)
        {
            View.UnregisterCallback<GeometryChangedEvent>(ViewPerformedInitialLayout);
            ReloadData();
        }

        void OnNewDataLoadedOrCleared()
        {
            if (!IsViewLoaded)
                return;

            ReloadData();
        }

        void OnNewFrameIndexSelectedInProfilerWindow(long selectedFrameIndexLong)
        {
            if (!IsViewLoaded)
                return;

            ReloadData();
        }

        void ReloadData()
        {
            // A value of -1 appears to be how the Profiler signifies 'current frame selected', so pick the last frame index.
            var selectedFrameIndex = Convert.ToInt32(m_ProfilerWindow.selectedFrameIndex);
            if (selectedFrameIndex == -1)
                selectedFrameIndex = m_DataService.FirstFrameIndex + m_DataService.FrameCount - 1;

            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;
            var modelBuilder = new BottlenecksDetailsViewModelBuilder(m_DataService);
            m_Model = modelBuilder.Build(selectedFrameIndex, targetFrameDurationNs);

            var hasInvalidModel = m_Model == null;
            UIUtility.SetElementDisplay(m_NoDataLabel, hasInvalidModel);
            if (hasInvalidModel)
                return;

            var largestDurationNs =
                Math.Max(m_Model.CpuDurationNs,
                    Math.Max(m_Model.GpuDurationNs, m_Model.TargetFrameDurationNs));

            var normalizedTargetFrameDuration = (float)m_Model.TargetFrameDurationNs / largestDurationNs;
            m_TargetFrameDurationIndicator.style.width = new Length(normalizedTargetFrameDuration * 100f, LengthUnit.Percent);

            ConfigureBar(
                m_CpuBar,
                m_CpuDurationLabel,
                m_Model.CpuDurationNs,
                largestDurationNs,
                m_Model.TargetFrameDurationNs);
            ConfigureBar(
                m_GpuBar,
                m_GpuDurationLabel,
                m_Model.GpuDurationNs,
                largestDurationNs,
                m_Model.TargetFrameDurationNs);

            m_DescriptionLabel.text = m_Model.LocalizedBottleneckDescription;
        }

        void OnTargetFrameDurationChanged()
        {
            UpdateTargetFrameDurationIndicatorLabel();
            ReloadData();
        }

        void UpdateTargetFrameDurationIndicatorLabel()
        {
            var targetFrameDuration = TimeFormatterUtility.FormatTimeNsToMs(m_SettingsService.TargetFrameDurationNs);
            var targetFramesPerSecond = Mathf.RoundToInt(1e9f / m_SettingsService.TargetFrameDurationNs);
            m_TargetFrameDurationIndicatorLabel.text = $"Target Frame Time\n{targetFrameDuration}\n<b>({targetFramesPerSecond} FPS)</b>";
        }

        void ConfigureBar(
            VisualElement bar,
            Label barLabel,
            UInt64 barDurationNs,
            UInt64 largestDurationNs,
            UInt64 targetFrameDurationNs)
        {
            var barDurationNormalized = (float)barDurationNs / largestDurationNs;
            bar.style.width = new StyleLength(new Length(barDurationNormalized * 100f, LengthUnit.Percent));
            barLabel.text = (barDurationNs == 0) ? k_NoValueText : TimeFormatterUtility.FormatTimeNsToMs(barDurationNs);
            if (barDurationNs > targetFrameDurationNs)
                bar.AddToClassList(k_UssClass_DurationBarFillHighlighted);
            else
                bar.RemoveFromClassList(k_UssClass_DurationBarFillHighlighted);
        }

        void OnCpuBarClicked(ClickEvent evt)
        {
            ProcessModelLink(BottlenecksDetailsViewModel.k_DescriptionLinkId_CpuTimeline);
        }

        void OnGpuBarClicked(ClickEvent evt)
        {
            ProcessModelLink(BottlenecksDetailsViewModel.k_DescriptionLinkId_FrameDebugger);
        }

        void OnDescriptionLabelLinkSelected(PointerDownLinkTagEvent evt)
        {
            var linkId = int.Parse(evt.linkID);
            ProcessModelLink(linkId);
        }

        void OnLabelLinkPointerOver(PointerOverLinkTagEvent evt)
        {
            ((VisualElement)evt.target).AddToClassList(k_UssClass_LinkCursor);
        }

        void OnLabelLinkPointerOut(PointerOutLinkTagEvent evt)
        {
            ((VisualElement)evt.target).RemoveFromClassList(k_UssClass_LinkCursor);
        }

        void ProcessModelLink(int linkId)
        {
            string linkDescription = string.Empty;
            switch (linkId)
            {
                case BottlenecksDetailsViewModel.k_DescriptionLinkId_CpuTimeline:
                    // Open the CPU Module.
                    var cpuModule = m_ProfilerWindow.GetProfilerModule<UnityEditorInternal.Profiling.CPUProfilerModule>(UnityEngine.Profiling.ProfilerArea.CPU);
                    m_ProfilerWindow.selectedModule = cpuModule;
                    cpuModule.ViewType = ProfilerViewType.Timeline;
                    linkDescription = k_LinkDescription_OpenCpuTimeline;
                    break;

                case BottlenecksDetailsViewModel.k_DescriptionLinkId_ProfileAnalyzer:
                    // Open Profile Analyzer in the Package Manager.
                    PackageManagerWindow.OpenAndSelectPackage("Profile Analyzer");
                    linkDescription = k_LinkDescription_OpenProfileAnalyzer;
                    break;

                case BottlenecksDetailsViewModel.k_DescriptionLinkId_FrameDebugger:
                    // Open Frame Debugger.
                    FrameDebuggerWindow.OpenWindow();
                    linkDescription = k_LinkDescription_OpenFrameDebugger;
                    break;

                case BottlenecksDetailsViewModel.k_DescriptionLinkId_GpuProfilerDocumentation:
                    // Open Third party profiling tools documentation page.
                    Application.OpenURL("https://docs.unity3d.com/Manual/performance-profiling-tools.html");
                    linkDescription = k_LinkDescription_OpenGpuProfilerDocumentation;
                    break;

                default:
                    break;
            }

            if (!string.IsNullOrEmpty(linkDescription))
                ProfilerWindowAnalytics.SendBottleneckLinkSelectedEvent(linkDescription);
        }
    }
}
