// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.Profiling.Editor.UI
{
    abstract class SummaryDetailsProvider : IDetailsProvider
    {
        // Link IDs for clickable links in description text
        public const int k_DescriptionLinkId_CpuTimeline = 1;
        public const int k_DescriptionLinkId_ProfileAnalyzer = 2;
        public const int k_DescriptionLinkId_FrameDebugger = 3;
        public const int k_DescriptionLinkId_GpuProfilerDocumentation = 4;

        internal static class Content
        {
            // Assistant prompts
            public static readonly string k_SingleFrameAssistantPrompt = L10n.Tr("Why is the selected frame slow?");
            public static readonly string k_RangeAssistantPrompt = L10n.Tr("Why do I have spikes in the profiler capture?");
        }

        protected readonly ProfilerWindow m_ProfilerWindow;
        protected readonly IProfilerCaptureDataService m_DataService;
        protected readonly IProfilerPersistentSettingsService m_SettingsService;
        protected readonly Range m_FrameRange;
        protected readonly bool m_IsSingleFrame;

        protected SummaryDetailsProvider(
            ProfilerWindow profilerWindow,
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            Range frameRange)
        {
            m_ProfilerWindow = profilerWindow;
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_FrameRange = frameRange;
            m_IsSingleFrame = frameRange.Start.Value == frameRange.End.Value;
        }

        public IDetailsProvider.AssistantRequestContext GetAssistantContext(IProfilerCaptureDataService dataService)
        {
            var prompt = m_IsSingleFrame
                ? Content.k_SingleFrameAssistantPrompt
                : Content.k_RangeAssistantPrompt;

            // Convert TargetFrameDurationNs (ulong) to target frame time in ms (float)
            var targetFrameTime = m_SettingsService.TargetFrameDurationNs > 0
                ? (float)(m_SettingsService.TargetFrameDurationNs / 1e6)  // ns to ms
                : -1f;

            var attachment = new CpuProfilerAssistantController.CpuProfilerContext(
                m_ProfilerWindow.CurrentLoadedCaptureFile,
                m_FrameRange,
                targetFrameTime: targetFrameTime);

            return new IDetailsProvider.AssistantRequestContext(prompt, attachment);
        }

        public abstract ViewController GetDetailsViewController(IProfilerCaptureDataService dataService);
    }

    internal class SummaryDetailsPanelViewController : ViewController
    {
        const int k_MainThreadIndex = 0;

        protected static class Content
        {
            // Title formats
            public static readonly string k_SingleFrameTitleFormat = L10n.Tr("Frame {0} Performance Analysis");
            public static readonly string k_RangeFrameTitleFormat = L10n.Tr("Frames {0}-{1} Performance Analysis");

            // Bottleneck status strings
            public static readonly string k_NoDataStatus = L10n.Tr("No Data");
            public static readonly string k_BoundStatusFormat = L10n.Tr("{0} Bound");
            public static readonly string k_WithinTargetStatus = L10n.Tr("Within Target");

            // Bottleneck descriptions (no target set)
            public static readonly string k_NoDataDescription = L10n.Tr("No CPU or GPU timing data available for this frame range.");
            public static readonly string k_OnlyTimingDataDescription = L10n.Tr("Only {0} timing data is available.");
            public static readonly string k_CpuHigherDescription = L10n.Tr("CPU Active Time is higher than GPU Time. Set a target frame rate in the Highlights chart to get detailed optimization suggestions.");
            public static readonly string k_GpuHigherDescription = L10n.Tr("GPU Time is higher than CPU Active Time. Set a target frame rate in the Highlights chart to get detailed optimization suggestions.");

            // Processor and thread name fragments (used as format arguments)
            public static readonly string k_CpuName = L10n.Tr("CPU");
            public static readonly string k_GpuName = L10n.Tr("GPU");
            public static readonly string k_BothCpuGpuName = L10n.Tr("CPU and GPU");
            public static readonly string k_MainThreadName = L10n.Tr("Main");
            public static readonly string k_RenderThreadName = L10n.Tr("Render");

            // Bottleneck description templates (with target set)
            public static readonly string k_BoundTitleFormat = L10n.Tr("<b>The {0} exceeded your target frame time in this frame.</b>");
            public static readonly string k_NotBoundTitle = L10n.Tr("<b>The CPU and the GPU are within your target frame time in this frame.</b>");
            public static readonly string k_CpuBoundExplanationFormat = L10n.Tr("In this frame the CPU spent the majority of its time executing on the <b>{0} thread</b>. Therefore, you should initially focus on this to begin your investigation.");
            public static readonly string k_SuggestionTitle = L10n.Tr("<b>How to inspect further:</b>");
            public static readonly string k_CpuBoundSuggestion = L10n.Tr($"To optimize your game's CPU utilization, begin by using the <link={SummaryDetailsProvider.k_DescriptionLinkId_CpuTimeline}><color=#4C7EFF><u>CPU module's Timeline view</u></color></link> to see which systems contributed the most to this time spent executing on the CPU.\n\nYou might also consider using the <link={SummaryDetailsProvider.k_DescriptionLinkId_ProfileAnalyzer}><color=#4C7EFF><u>Profile Analyzer</u></color></link> to perform a deeper statistical analysis and/or to compare Profiler captures after you have made some optimizations.");
            public static readonly string k_GpuBoundSuggestion = L10n.Tr($"To optimize your game's GPU utilization, use the <link={SummaryDetailsProvider.k_DescriptionLinkId_FrameDebugger}><color=#4C7EFF><u>Frame Debugger</u></color></link> to step through individual draw calls and see in detail how the scene is constructed from its graphical elements.\n\nYou might also consider using a native GPU profiler for the platform you are targeting. Please see the <link={SummaryDetailsProvider.k_DescriptionLinkId_GpuProfilerDocumentation}><color=#4C7EFF><u>Unity documentation</u></color></link> for more information.");
        }

        protected readonly ProfilerWindow m_ProfilerWindow;
        protected readonly IProfilerCaptureDataService m_DataService;
        protected readonly IProfilerPersistentSettingsService m_SettingsService;
        protected readonly Range m_FrameRange;
        protected ulong m_CpuTimeNs;
        protected ulong m_GpuTimeNs;
        protected readonly bool m_IsSingleFrame;

        public SummaryDetailsPanelViewController(
            ProfilerWindow profilerWindow,
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            Range frameRange,
            ulong cpuTimeNs,
            ulong gpuTimeNs)
        {
            m_ProfilerWindow = profilerWindow;
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_FrameRange = frameRange;
            m_CpuTimeNs = cpuTimeNs;
            m_GpuTimeNs = gpuTimeNs;
            m_IsSingleFrame = frameRange.Start.Value == frameRange.End.Value;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("SummaryDetailsView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "summary-details__dark";
            const string k_UssClass_Light = "summary-details__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            // Register link event handlers
            var descriptionLabel = View.Q<Label>("summary-details__description");
            if (descriptionLabel != null)
            {
                descriptionLabel.RegisterCallback<PointerDownLinkTagEvent>(OnDescriptionLabelLinkSelected);
            }

            SetupContent();
        }

        void SetupContent()
        {
            // Set title
            var titleLabel = View.Q<Label>("summary-details__title");
            if (titleLabel != null)
            {
                // Use FrameIndexFormatterUtility to display 1-indexed frame numbers matching the Profiler window
                var startFrameDisplay = FrameIndexFormatterUtility.DisplayStringForFrameIndex(m_FrameRange.Start.Value);
                var endFrameDisplay = FrameIndexFormatterUtility.DisplayStringForFrameIndex(m_FrameRange.End.Value - 1);
                titleLabel.text = m_IsSingleFrame
                    ? string.Format(Content.k_SingleFrameTitleFormat, startFrameDisplay)
                    : string.Format(Content.k_RangeFrameTitleFormat, startFrameDisplay, endFrameDisplay);
            }

            // Determine bottleneck type
            var (bottleneckType, description) = DetermineBottleneck();

            var statusLabel = View.Q<Label>("summary-details__status");
            if (statusLabel != null)
            {
                statusLabel.text = bottleneckType;
            }

            var descriptionLabel = View.Q<Label>("summary-details__description");
            if (descriptionLabel != null)
            {
                descriptionLabel.text = description;
            }
        }

        (ulong cpuNs, ulong gpuNs) GetTimesForBottleneckDetermination()
        {
            return (m_CpuTimeNs, m_GpuTimeNs);
        }

        (string bottleneckType, string description) DetermineBottleneck()
        {
            var (cpuTimeNs, gpuTimeNs) = GetTimesForBottleneckDetermination();

            // Handle zero times
            if (cpuTimeNs == 0 && gpuTimeNs == 0)
                return (Content.k_NoDataStatus, Content.k_NoDataDescription);

            // Use settings service instead of static access
            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;

            // If no target is set, fall back to simple comparison
            if (targetFrameDurationNs == 0)
            {
                if (cpuTimeNs == 0)
                    return (string.Format(Content.k_BoundStatusFormat, Content.k_GpuName),
                        string.Format(Content.k_OnlyTimingDataDescription, Content.k_GpuName));
                if (gpuTimeNs == 0)
                    return (string.Format(Content.k_BoundStatusFormat, Content.k_CpuName),
                        string.Format(Content.k_OnlyTimingDataDescription, Content.k_CpuName));

                return cpuTimeNs > gpuTimeNs
                    ? (string.Format(Content.k_BoundStatusFormat, Content.k_CpuName), Content.k_CpuHigherDescription)
                    : (string.Format(Content.k_BoundStatusFormat, Content.k_GpuName), Content.k_GpuHigherDescription);
            }

            // Build description using target frame comparison
            var stringBuilder = new StringBuilder();

            var cpuExceededTarget = cpuTimeNs > targetFrameDurationNs;
            var gpuExceededTarget = gpuTimeNs > targetFrameDurationNs;
            var isBound = cpuExceededTarget || gpuExceededTarget;

            string bottleneckType;
            string localizedTitle;

            if (isBound)
            {
                string boundProcessorName;

                if (cpuExceededTarget && gpuExceededTarget)
                {
                    boundProcessorName = Content.k_BothCpuGpuName;
                    bottleneckType = string.Format(Content.k_BoundStatusFormat, Content.k_BothCpuGpuName);
                }
                else if (cpuExceededTarget)
                {
                    boundProcessorName = Content.k_CpuName;
                    bottleneckType = string.Format(Content.k_BoundStatusFormat, Content.k_CpuName);
                }
                else
                {
                    boundProcessorName = Content.k_GpuName;
                    bottleneckType = string.Format(Content.k_BoundStatusFormat, Content.k_GpuName);
                }

                localizedTitle = string.Format(GetBoundTitleFormat(), boundProcessorName);
            }
            else
            {
                bottleneckType = Content.k_WithinTargetStatus;
                localizedTitle = GetNotBoundTitle();
            }

            stringBuilder.Append(localizedTitle);

            if (isBound)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();

                // Add thread-specific guidance for CPU bound scenarios in single frames
                if (cpuExceededTarget && m_IsSingleFrame)
                {
                    var (cpuMainThreadDurationNs, cpuRenderThreadDurationNs) = GetThreadSpecificTimings();
                    if (cpuMainThreadDurationNs > 0 || cpuRenderThreadDurationNs > 0)
                    {
                        var longestThreadName = cpuMainThreadDurationNs > cpuRenderThreadDurationNs
                            ? Content.k_MainThreadName
                            : Content.k_RenderThreadName;
                        var localizedThreadDetail = string.Format(Content.k_CpuBoundExplanationFormat, longestThreadName);
                        stringBuilder.AppendLine(localizedThreadDetail);
                        stringBuilder.AppendLine();
                    }
                }

                stringBuilder.AppendLine(Content.k_SuggestionTitle);

                if (cpuExceededTarget)
                    stringBuilder.Append(Content.k_CpuBoundSuggestion);

                if (gpuExceededTarget)
                {
                    if (cpuExceededTarget)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append(Content.k_GpuBoundSuggestion);
                }
            }

            return (bottleneckType, stringBuilder.ToString());
        }

        (ulong mainThreadNs, ulong renderThreadNs) GetThreadSpecificTimings()
        {
            // Only query thread-specific data for single frames
            if (!m_IsSingleFrame || m_DataService == null)
                return (0, 0);

            try
            {
                var frameIndex = m_FrameRange.Start.Value;
                using var frameData = m_DataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex);
                if (!frameData.valid)
                    return (0, 0);

                var cpuMainThreadMarkerId = frameData.GetMarkerId("CPU Main Thread Active Time");
                var cpuRenderThreadMarkerId = frameData.GetMarkerId("CPU Render Thread Active Time");

                var mainThreadNs = 0UL;
                var renderThreadNs = 0UL;

                if (cpuMainThreadMarkerId != FrameDataView.invalidMarkerId)
                    mainThreadNs = Convert.ToUInt64(frameData.GetCounterValueAsLong(cpuMainThreadMarkerId));

                if (cpuRenderThreadMarkerId != FrameDataView.invalidMarkerId)
                    renderThreadNs = Convert.ToUInt64(frameData.GetCounterValueAsLong(cpuRenderThreadMarkerId));

                return (mainThreadNs, renderThreadNs);
            }
            catch
            {
                return (0, 0);
            }
        }

        protected virtual string GetBoundTitleFormat()
        {
            return Content.k_BoundTitleFormat;
        }

        protected virtual string GetNotBoundTitle()
        {
            return Content.k_NotBoundTitle;
        }

        void OnDescriptionLabelLinkSelected(PointerDownLinkTagEvent evt)
        {
            var linkId = int.Parse(evt.linkID);
            switch (linkId)
            {
                case SummaryDetailsProvider.k_DescriptionLinkId_CpuTimeline:
                    // Open the CPU Module Timeline view
                    var cpuModule = m_ProfilerWindow.GetProfilerModule<UnityEditorInternal.Profiling.CPUProfilerModule>(UnityEngine.Profiling.ProfilerArea.CPU);
                    m_ProfilerWindow.selectedModule = cpuModule;
                    cpuModule.ViewType = ProfilerViewType.Timeline;
                    break;

                case SummaryDetailsProvider.k_DescriptionLinkId_ProfileAnalyzer:
                    SummaryViewController.OpenProfileAnalyzer();
                    break;

                case SummaryDetailsProvider.k_DescriptionLinkId_FrameDebugger:
                    // Open Frame Debugger
                    FrameDebuggerWindow.OpenWindow();
                    break;

                case SummaryDetailsProvider.k_DescriptionLinkId_GpuProfilerDocumentation:
                    // Open Third party profiling tools documentation page
                    Application.OpenURL("https://docs.unity3d.com/Manual/performance-profiling-tools.html");
                    break;

                default:
                    break;
            }
        }
    }
}
