// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksDetailsViewModelBuilder
    {
        const int k_FrameTimingManagerFixedDelay = 4;
        const int k_MainThreadIndex = 0;

        static readonly string k_BoundTitleFormat = $"<b>The {{0}} exceeded your target frame time in this frame.</b>";
        static readonly string k_NotBoundTitleFormat = $"<b>The {{0}} within your target frame time in this frame.</b>";
        static readonly string k_CpuBoundExplanationFormat = $"In this frame the CPU spent the majority of its time executing on the <b>{{0}} thread</b>. Therefore, you should initially focus on this to begin your investigation.";
        static readonly string k_SuggestionTitle = $"<b>How to inspect further:</b>";
        static readonly string k_CpuBoundSuggestion = $"To optimize your game’s CPU utilization, begin by using the <link={BottlenecksDetailsViewModel.k_DescriptionLinkId_CpuTimeline}><color=#40a0ff><u>CPU module’s Timeline view</u></color></link> to see which systems contributed the most to this time spent executing on the CPU.\n\nYou might also consider using the <link={BottlenecksDetailsViewModel.k_DescriptionLinkId_ProfileAnalyzer}><color=#40a0ff><u>Profile Analyzer</u></color></link> to perform a deeper statistical analysis and/or to compare Profiler captures after you have made some optimizations.";
        static readonly string k_GpuBoundSuggestion = $"To optimize your game’s GPU utilization, use the <link={BottlenecksDetailsViewModel.k_DescriptionLinkId_FrameDebugger}><color=#40a0ff><u>Frame Debugger</u></color></link> to step through individual draw calls and see in detail how the scene is constructed from its graphical elements.\n\nYou might also consider using a native GPU profiler for the platform you are targeting. Please see the <link={BottlenecksDetailsViewModel.k_DescriptionLinkId_GpuProfilerDocumentation}><color=#40a0ff><u>Unity documentation</u></color></link> for more information.";

        readonly IProfilerCaptureDataService m_DataService;

        public BottlenecksDetailsViewModelBuilder(IProfilerCaptureDataService dataService)
        {
            m_DataService = dataService;
        }

        // Builds a new BottlenecksDetailsViewModel.
        public BottlenecksDetailsViewModel Build(int frameIndex, UInt64 targetFrameDurationNs)
        {
            if (m_DataService.FrameCount == 0)
                return null;

            using var frameData = m_DataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex);
            if (!frameData.valid)
                return null;

            var cpuMainThreadDurationMarkerId = frameData.GetMarkerId("CPU Main Thread Active Time");
            if (cpuMainThreadDurationMarkerId == FrameDataView.invalidMarkerId)
                return null;

            var cpuRenderThreadDurationMarkerId = frameData.GetMarkerId("CPU Render Thread Active Time");
            if (cpuRenderThreadDurationMarkerId == FrameDataView.invalidMarkerId)
                return null;

            var cpuMainThreadDurationNs = Convert.ToUInt64(frameData.GetCounterValueAsLong(cpuMainThreadDurationMarkerId));
            var cpuRenderThreadDurationNs = Convert.ToUInt64(frameData.GetCounterValueAsLong(cpuRenderThreadDurationMarkerId));
            var cpuDurationNs = Math.Max(cpuMainThreadDurationNs, cpuRenderThreadDurationNs);

            var gpuDurationNs = 0UL;
            var frameTimingManagerFrameIndex = frameIndex + k_FrameTimingManagerFixedDelay;
            using var frameTimingManagerFrameData = m_DataService.GetRawFrameDataView(frameTimingManagerFrameIndex, k_MainThreadIndex);
            if (frameTimingManagerFrameData.valid)
            {
                var gpuDurationMarkerId = frameTimingManagerFrameData.GetMarkerId("GPU Frame Time");
                if (gpuDurationMarkerId != FrameDataView.invalidMarkerId)
                    gpuDurationNs = Convert.ToUInt64(frameTimingManagerFrameData.GetCounterValueAsLong(gpuDurationMarkerId));
            }

            if (cpuDurationNs == 0 && gpuDurationNs == 0)
                return null;

            var localizedBottleneckDescription = BuildDescriptionText(
                cpuDurationNs,
                gpuDurationNs,
                targetFrameDurationNs,
                cpuMainThreadDurationNs,
                cpuRenderThreadDurationNs);

            return new BottlenecksDetailsViewModel(
                cpuDurationNs,
                gpuDurationNs,
                targetFrameDurationNs,
                localizedBottleneckDescription);
        }

        string BuildDescriptionText(
            UInt64 cpuDurationNs,
            UInt64 gpuDurationNs,
            UInt64 targetFrameDurationNs,
            UInt64 cpuMainThreadDurationNs,
            UInt64 cpuRenderThreadDurationNs)
        {
            const string k_CpuName = "CPU";
            const string k_GpuName = "GPU";
            const string k_BothCpuGpuText = k_CpuName + " and the " + k_GpuName;
            const string k_CpuIsText = k_CpuName + " is";
            const string k_BothCpuGpuAreText = k_BothCpuGpuText + " are";
            const string k_MainThreadName = "Main";
            const string k_RenderThreadName = "Render";

            var stringBuilder = new StringBuilder();

            string localizedTitleFormat;
            string processorText;
            var cpuExceededTarget = cpuDurationNs > targetFrameDurationNs;
            var gpuExceededTarget = gpuDurationNs > targetFrameDurationNs;
            var isBound = cpuExceededTarget || gpuExceededTarget;
            if (isBound)
            {
                localizedTitleFormat = L10n.Tr(k_BoundTitleFormat);
                if (cpuExceededTarget && gpuExceededTarget)
                    processorText = k_BothCpuGpuText;
                else if (cpuExceededTarget)
                    processorText = k_CpuName;
                else
                    processorText = k_GpuName;
            }
            else
            {
                localizedTitleFormat = L10n.Tr(k_NotBoundTitleFormat);
                if (IsInvalidDuration(gpuDurationNs))
                    processorText = k_CpuIsText;
                else
                    processorText = k_BothCpuGpuAreText;
            }

            var localizedTitle = string.Format(localizedTitleFormat, processorText);
            stringBuilder.Append(localizedTitle);

            if (isBound)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();

                if (cpuExceededTarget)
                {
                    var longestThreadName = (cpuMainThreadDurationNs > cpuRenderThreadDurationNs) ? k_MainThreadName : k_RenderThreadName;
                    var localizedThreadDetailFormat = L10n.Tr(k_CpuBoundExplanationFormat);
                    var localizedThreadDetail = string.Format(localizedThreadDetailFormat, longestThreadName);
                    stringBuilder.AppendLine(localizedThreadDetail);
                    stringBuilder.AppendLine();
                }

                stringBuilder.AppendLine(L10n.Tr(k_SuggestionTitle));

                if (cpuExceededTarget)
                    stringBuilder.Append(L10n.Tr(k_CpuBoundSuggestion));

                if (gpuExceededTarget)
                {
                    if (cpuExceededTarget)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append(L10n.Tr(k_GpuBoundSuggestion));
                }
            }

            return stringBuilder.ToString();
        }

        static bool IsInvalidDuration(float durationNs)
        {
            // A value of -1 is what our existing counters API returns when there is no counter data in the selected frame.
            // A value of 0 is what FTM will write when GPU misses the deadline and does not record a measurement.
            return Mathf.Approximately(durationNs, -1f) || Mathf.Approximately(durationNs, 0f);
        }
    }
}
