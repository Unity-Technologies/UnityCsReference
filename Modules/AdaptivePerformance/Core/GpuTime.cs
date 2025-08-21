// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal class GpuTimeProvider
    {
        private UnityEngine.FrameTiming[] m_FrameTiming = new UnityEngine.FrameTiming[1];

        public GpuTimeProvider()
        {
        }

        public float GpuFrameTime
        {
            get
            {
                if (GetLatestTimings() >= 1)
                {
                    double gpuFrameTime = m_FrameTiming[0].gpuFrameTime;
                    if (gpuFrameTime > 0.0)
                        return (float)(gpuFrameTime * 0.001);
                }
                return -1.0f;
            }
        }

        protected virtual uint GetLatestTimings()
        {
            return UnityEngine.FrameTimingManager.GetLatestTimings(1, m_FrameTiming);
        }

        public void Measure()
        {
            UnityEngine.FrameTimingManager.CaptureFrameTimings();
        }
    }
}
