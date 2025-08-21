// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.AdaptivePerformance
{
    internal class CpuTimeProvider
    {
        private UnityEngine.FrameTiming[] m_FrameTimings = new UnityEngine.FrameTiming[1];

        public CpuTimeProvider()
        {
        }

        public float CpuFrameTime
        {
            get
            {
                if (GetLatestTimings() >= 1)
                {
                    double cpuFrameTime = m_FrameTimings[0].cpuMainThreadFrameTime + m_FrameTimings[0].cpuRenderThreadFrameTime;
                    if (cpuFrameTime > 0.0)
                        return (float)(cpuFrameTime * 0.001);
                }
                return -1.0f;
            }
        }

        protected virtual uint GetLatestTimings()
        {
            return UnityEngine.FrameTimingManager.GetLatestTimings(1, m_FrameTimings);
        }

        public void Measure()
        {
            UnityEngine.FrameTimingManager.CaptureFrameTimings();
        }
    }
}
