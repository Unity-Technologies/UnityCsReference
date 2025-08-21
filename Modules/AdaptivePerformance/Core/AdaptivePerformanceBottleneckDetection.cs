// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal class BottleneckUtil
    {
        public static PerformanceBottleneck DetermineBottleneck(PerformanceBottleneck prevBottleneck, float averageCpuFrameTime, float averageGpuFrametime, float averageOverallFrametime, float targetFrameTime)
        {
            if (HittingFrameRateLimit(averageOverallFrametime, prevBottleneck == PerformanceBottleneck.TargetFrameRate ? 0.03f : 0.02f, targetFrameTime))
                return PerformanceBottleneck.TargetFrameRate;

            if (averageGpuFrametime >= averageOverallFrametime)
            {
                // GPU is active all the time? It's probably the bottleneck
                return PerformanceBottleneck.GPU;
            }
            else if (averageCpuFrameTime >= averageOverallFrametime)
            {
                return PerformanceBottleneck.CPU;
            }
            else
            {
                bool wasGpuBound = prevBottleneck == PerformanceBottleneck.GPU;
                bool wasCpuBound = prevBottleneck == PerformanceBottleneck.CPU;

                float gpuUtilization = averageGpuFrametime / averageOverallFrametime;
                float cpuUtilization = averageCpuFrameTime / averageOverallFrametime;

                // very high main thread CPU time => most likely CPU bound
                float highCpuUtilThreshold = wasCpuBound ? 0.87f : 0.90f;
                if (cpuUtilization > highCpuUtilThreshold)
                {
                    return PerformanceBottleneck.CPU;
                }

                // GPU is active almost all the time? It's probably the bottleneck
                float highGpuUtilThreshold = wasGpuBound ? 0.87f : 0.90f;
                if (averageGpuFrametime > highGpuUtilThreshold)
                {
                    return PerformanceBottleneck.GPU;
                }

                if (averageGpuFrametime > averageCpuFrameTime)
                {
                    // higher GPU time compared to CPU time? => might be GPU bound
                    // but we can only be somewhat sure if we have relatively high GPU utilization

                    float gpuUtilizationThreshold = wasGpuBound ? 0.9f : 0.92f;
                    if (gpuUtilization > gpuUtilizationThreshold)
                    {
                        // significantly higher GPU time compared to CPU time?
                        float gpuFactor = wasGpuBound ? 0.92f : 0.90f;
                        if (averageGpuFrametime * gpuFactor > averageCpuFrameTime)
                        {
                            return PerformanceBottleneck.GPU;
                        }
                    }
                }
                else
                {
                    float cpuUtilizationThreshold = wasCpuBound ? 0.5f : 0.52f;
                    if (cpuUtilization > cpuUtilizationThreshold && averageGpuFrametime < averageCpuFrameTime)
                    {
                        // higher CPU time compared to GPU time?
                        float cpuFactor = wasCpuBound ? 0.85f : 0.80f;
                        if (averageCpuFrameTime * cpuFactor > averageGpuFrametime)
                        {
                            return PerformanceBottleneck.CPU;
                        }
                    }
                }
            }

            return PerformanceBottleneck.Unknown;
        }

        private static bool HittingFrameRateLimit(float actualFrameTime, float thresholdFactor, float targetFrameTime)
        {
            if (targetFrameTime <= 0)
                return false;

            if (actualFrameTime <= targetFrameTime)
            {
                return true;
            }

            if (actualFrameTime - targetFrameTime < thresholdFactor * targetFrameTime)
            {
                return true;
            }

            return false;
        }
    }
}
