// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Profiling;

namespace UnityEditorInternal.Profiling
{
    // Reads the CPU and GPU durations for a frame from the profiler stream, the same counters the
    // Highlights module surfaces, and decides whether a duration is over the frame budget. Kept local
    // to the Screenshots module (rather than shared with the Bottlenecks builders) and split so the
    // pure budget decision can be unit tested without a live capture.
    static class ScreenshotFrameTimings
    {
        const int k_MainThreadIndex = 0;

        // The Frame Timing Manager reports GPU timings at a fixed offset of four frames after the CPU
        // frame they belong to, so the GPU counter is read from frameIndex + 4.
        const int k_FrameTimingManagerGpuFrameDelay = 4;

        const string k_CpuMainThreadActiveTimeCounter = "CPU Main Thread Active Time";
        const string k_CpuRenderThreadActiveTimeCounter = "CPU Render Thread Active Time";
        const string k_GpuFrameTimeCounter = "GPU Frame Time";

        // Reads the CPU active time (the longer of the main and render thread) and the GPU time for
        // the frame. Missing counters (older captures, or the Rendering category disabled) read as 0.
        public static void GetDurationsNs(int frameIndex, out ulong cpuActiveNs, out ulong gpuTimeNs)
        {
            cpuActiveNs = 0UL;
            gpuTimeNs = 0UL;

            if (frameIndex < 0)
                return;

            using (var mainThreadData = ProfilerDriver.GetRawFrameDataView(frameIndex, k_MainThreadIndex))
            {
                if (!mainThreadData.valid)
                    return;

                var mainThreadActiveNs = GetCounterValueAsUInt64(mainThreadData, k_CpuMainThreadActiveTimeCounter);
                var renderThreadActiveNs = GetCounterValueAsUInt64(mainThreadData, k_CpuRenderThreadActiveTimeCounter);
                cpuActiveNs = Math.Max(mainThreadActiveNs, renderThreadActiveNs);
            }

            var gpuFrameIndex = frameIndex + k_FrameTimingManagerGpuFrameDelay;
            if (gpuFrameIndex <= ProfilerDriver.lastFrameIndex)
            {
                using (var gpuFrameData = ProfilerDriver.GetRawFrameDataView(gpuFrameIndex, k_MainThreadIndex))
                {
                    if (gpuFrameData.valid)
                        gpuTimeNs = GetCounterValueAsUInt64(gpuFrameData, k_GpuFrameTimeCounter);
                }
            }
        }

        // The frame budget in nanoseconds, derived from the profiler's target frames-per-second
        // setting. Returns 0 when no valid target is set, which callers treat as "no budget".
        public static ulong GetTargetFrameDurationNs()
        {
            var targetFramesPerSecond = ProfilerUserSettings.targetFramesPerSecond;
            if (targetFramesPerSecond <= 0)
                return 0UL;

            return Convert.ToUInt64((1f / targetFramesPerSecond) * 1e9f);
        }

        public static bool IsOverBudget(ulong durationNs, ulong targetFrameDurationNs)
        {
            return durationNs > 0UL && targetFrameDurationNs > 0UL && durationNs > targetFrameDurationNs;
        }

        static ulong GetCounterValueAsUInt64(RawFrameDataView threadData, string counterName)
        {
            var markerId = threadData.GetMarkerId(counterName);
            if (markerId == FrameDataView.invalidMarkerId)
                return 0UL;

            var value = threadData.GetCounterValueAsLong(markerId);

            // These counters can occasionally report negative values; clamp to zero.
            if (value < 0)
                value = 0L;

            return Convert.ToUInt64(value);
        }
    }
}
