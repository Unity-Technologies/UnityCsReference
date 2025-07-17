// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor.UI
{
    readonly struct FrameBottlenecksModel
    {
        public FrameBottlenecksModel(
            ulong cpuDurationNs,
            ulong gpuDurationNs)
        {
            CpuDurationNs = cpuDurationNs;
            GpuDurationNs = gpuDurationNs;
        }

        public ulong CpuDurationNs { get; }

        public ulong GpuDurationNs { get; }
    }
}
