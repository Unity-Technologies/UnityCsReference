// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Abstract bottlenecks builder providing shared functionality used by the 'frame' and 'range' bottlenecks builders.
    abstract class BottlenecksModelBuilder
    {
        protected (ulong, ulong) GetCpuGpuDurationsNsForFrame(IProfilerCaptureDataService dataService, int frameIndex)
        {
            var cpuActiveDurationNs = 0UL;
            const int k_MainThreadIndex = 0;
            using (var mainThreadData = dataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex))
            {
                if (mainThreadData.valid == false)
                    throw new ArgumentException("Invalid Profiler data.");

                const string k_MainThreadActiveTimeCounterName = "CPU Main Thread Active Time";
                var cpuMainThreadActiveDurationNs = GetCounterValueAsUInt64(
                    mainThreadData,
                    k_MainThreadActiveTimeCounterName);

                const string k_RenderThreadActiveTimeCounterName = "CPU Render Thread Active Time";
                var cpuRenderThreadActiveDurationNs = GetCounterValueAsUInt64(
                    mainThreadData,
                    k_RenderThreadActiveTimeCounterName);

                cpuActiveDurationNs = Math.Max(cpuMainThreadActiveDurationNs, cpuRenderThreadActiveDurationNs);
            }

            // Frame Timing Manager reports GPU timings at a fixed offset of four frames.
            const int k_FrameTimingManagerFixedDelay = 4;
            var gpuFrameIndex = frameIndex + k_FrameTimingManagerFixedDelay;
            var gpuActiveDurationNs = 0UL;
            using (var mainThreadData = dataService.GetRawFrameDataView(gpuFrameIndex, k_MainThreadIndex))
            {
                if (mainThreadData.valid)
                {
                    const string k_GpuFrameTimeCounterName = "GPU Frame Time";
                    gpuActiveDurationNs = GetCounterValueAsUInt64(mainThreadData, k_GpuFrameTimeCounterName);
                }
            }

            return (cpuActiveDurationNs, gpuActiveDurationNs);
        }

        ulong GetCounterValueAsUInt64(RawFrameDataView threadData, string markerName)
        {
            var markerId = threadData.GetMarkerId(markerName);

            // If the capture is from prior to Highlights feature, none of the
            // bottlenecks counters will be present. If the Rendering category
            // was disabled, the 'GPU Frame Time' counter won't be present.
            if (markerId == FrameDataView.invalidMarkerId)
                return 0UL;

            var value = threadData.GetCounterValueAsLong(markerId);

            // Sadly it is possible to see negative timings for these counters.
            if (value < 0)
                value = 0L;

            return Convert.ToUInt64(value);
        }
    }
}
