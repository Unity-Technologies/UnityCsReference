// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksDetailsViewModel
    {
        public const int k_DescriptionLinkId_CpuTimeline = 1;
        public const int k_DescriptionLinkId_ProfileAnalyzer = 2;
        public const int k_DescriptionLinkId_FrameDebugger = 3;
        public const int k_DescriptionLinkId_GpuProfilerDocumentation = 4;

        public BottlenecksDetailsViewModel(
            UInt64 cpuDurationNs,
            UInt64 gpuDurationNs,
            UInt64 targetFrameDurationNs,
            string localizedBottleneckDescription)
        {
            CpuDurationNs = cpuDurationNs;
            GpuDurationNs = gpuDurationNs;
            TargetFrameDurationNs = targetFrameDurationNs;
            LocalizedBottleneckDescription = localizedBottleneckDescription;
        }

        public UInt64 CpuDurationNs { get; }

        public UInt64 GpuDurationNs { get; }

        public UInt64 TargetFrameDurationNs { get; }

        public string LocalizedBottleneckDescription { get; }
    }
}
