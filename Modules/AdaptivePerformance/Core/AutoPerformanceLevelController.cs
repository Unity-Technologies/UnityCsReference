// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal class AutoPerformanceLevelController
    {
        private IDevicePerformanceControl m_PerfControl;
        private IPerformanceStatus m_PerfStats;
        private IThermalStatus m_ThermalStats;

        float m_LastChangeTimeStamp = 0.0f;
        float m_LastGpuLevelRaiseTimeStamp = 0.0f;
        float m_LastCpuLevelRaiseTimeStamp = 0.0f;
        float m_TargetFrameRateHitTimestamp = 0.0f;
        float m_BottleneckUnknownTimestamp = 0.0f;
        bool m_TriedToResolveUnknownBottleneck = false;
        bool m_Enabled = false;
        string m_FeatureName = "Auto Performance Control";

        /// <summary>
        /// Target frame time in seconds per frame.
        /// </summary>
        public float TargetFrameTime { get; set; }

        /// <summary>
        /// AllowedCpuActiveTimeRatio (0.8f) means that the controller might lower the CPU level as long as Unity's main thread is active less than 80% of the time.
        /// </summary>
        public float AllowedCpuActiveTimeRatio { get; set; }

        /// <summary>
        /// AllowedGpuActiveTimeRatio (0.8f) means that the controller might lower the GPU level as long as the GPU is active less than 80% of the time.
        /// </summary>
        public float AllowedGpuActiveTimeRatio { get; set; }

        /// <summary>
        /// The time in seconds to wait before the controller lowers the GPU level after raising it.
        /// </summary>
        public float GpuLevelBounceAvoidanceThreshold { get; set; }

        /// <summary>
        /// The time in seconds to wait before the controller lowers the CPU level after raising it.
        /// </summary>
        public float CpuLevelBounceAvoidanceThreshold { get; set; }

        /// <summary>
        /// The time interval in seconds that the controller uses to make any adjustments.
        /// </summary>
        public float UpdateInterval { get; set; }

        /// <summary>
        /// The time in seconds that the controller waits while bound to PerformanceBottleneck.TargetFrameRate before it considers lowering CPU or GPU levels.
        /// </summary>
        public float MinTargetFrameRateHitTime { get; set; }

        public float MaxTemperatureLevel { get; set; }

        public AutoPerformanceLevelController(IDevicePerformanceControl perfControl, IPerformanceStatus perfStat, IThermalStatus thermalStat)
        {
            UpdateInterval = 5.0f;
            TargetFrameTime = -1.0f;
            AllowedCpuActiveTimeRatio = 0.8f;
            AllowedGpuActiveTimeRatio = 0.9f;
            GpuLevelBounceAvoidanceThreshold = 10.0f;
            CpuLevelBounceAvoidanceThreshold = 10.0f;
            MinTargetFrameRateHitTime = 10.0f;
            MaxTemperatureLevel = 0.9f;

            m_PerfStats = perfStat;
            m_PerfControl = perfControl;
            m_ThermalStats = thermalStat;

            perfStat.PerformanceBottleneckChangeEvent += (PerformanceBottleneckChangeEventArgs ev) => OnBottleneckChange(ev);
            AdaptivePerformanceAnalytics.RegisterFeature(m_FeatureName, m_Enabled);
        }

        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                if (m_Enabled == value)
                    return;

                m_Enabled = value;
                AdaptivePerformanceAnalytics.SendAdaptiveFeatureUpdateEvent(m_FeatureName, m_Enabled);
            }
        }

        public void Update()
        {
            if (!m_Enabled)
                return;

            UpdateImpl(Time.time);
        }

        public void Override(int requestedCpuLevel, int requestedGpuLevel)
        {
            m_LastChangeTimeStamp = Time.time;

            if (requestedCpuLevel > m_PerfControl.CpuLevel)
                m_LastCpuLevelRaiseTimeStamp = m_LastChangeTimeStamp;
            if (requestedGpuLevel > m_PerfControl.GpuLevel)
                m_LastCpuLevelRaiseTimeStamp = m_LastChangeTimeStamp;

            m_PerfControl.CpuLevel = requestedCpuLevel;
            m_PerfControl.GpuLevel = requestedGpuLevel;
        }

        private void UpdateImpl(float timestamp)
        {
            // After a change, wait at least UpdateInterval seconds before making additional changes.
            if (timestamp - m_LastChangeTimeStamp < UpdateInterval)
                return;

            switch (m_PerfStats.PerformanceMetrics.PerformanceBottleneck)
            {
                case PerformanceBottleneck.GPU:
                    if (AllowRaiseGpuLevel())
                        RaiseGpuLevel(timestamp);
                    break;
                case PerformanceBottleneck.CPU:
                    if (AllowRaiseCpuLevel())
                        RaiseCpuLevel(timestamp);
                    break;
                case PerformanceBottleneck.TargetFrameRate:
                    if (timestamp - m_TargetFrameRateHitTimestamp > MinTargetFrameRateHitTime) // Make sure we have been able to hold targetFrameRate for at least 5s before making adjustments that might lower it.
                    {
                        if (AllowLowerCpuLevel(timestamp))
                            LowerCpuLevel(timestamp);
                        if (AllowLowerGpuLevel(timestamp))
                            LowerGpuLevel(timestamp);
                    }
                    break;
                case PerformanceBottleneck.Unknown:
                    // After staying at Unknown bottleneck for more than 10s, we try to resolve it once by raising levels.
                    if (!m_TriedToResolveUnknownBottleneck && timestamp - m_BottleneckUnknownTimestamp > 10.0f)
                    {
                        if (AllowRaiseCpuLevel())
                        {
                            RaiseCpuLevel(timestamp);
                            m_TriedToResolveUnknownBottleneck = true;
                        }
                        else if (AllowRaiseGpuLevel())
                        {
                            RaiseGpuLevel(timestamp);
                            m_TriedToResolveUnknownBottleneck = true;
                        }
                    }
                    break;
            }
        }

        private void OnBottleneckChange(PerformanceBottleneckChangeEventArgs ev)
        {
            if (ev.PerformanceBottleneck == PerformanceBottleneck.TargetFrameRate)
                m_TargetFrameRateHitTimestamp = Time.time;

            if (ev.PerformanceBottleneck == PerformanceBottleneck.Unknown)
                m_BottleneckUnknownTimestamp = Time.time;
            else
                m_TriedToResolveUnknownBottleneck = false;
        }

        private void RaiseGpuLevel(float timestamp)
        {
            ++m_PerfControl.GpuLevel;
            m_LastChangeTimeStamp = timestamp;
            m_LastGpuLevelRaiseTimeStamp = timestamp;
            APLog.Debug("Auto Perf Level: raise GPU level to {0}", m_PerfControl.GpuLevel);
        }

        private void RaiseCpuLevel(float timestamp)
        {
            ++m_PerfControl.CpuLevel;
            m_LastChangeTimeStamp = timestamp;
            m_LastCpuLevelRaiseTimeStamp = timestamp;
            APLog.Debug("Auto Perf Level: raise CPU level to {0}", m_PerfControl.CpuLevel);
        }

        private void LowerCpuLevel(float timestamp)
        {
            --m_PerfControl.CpuLevel;
            m_LastChangeTimeStamp = timestamp;
            APLog.Debug("Auto Perf Level: lower CPU level to {0}", m_PerfControl.CpuLevel);
        }

        private void LowerGpuLevel(float timestamp)
        {
            --m_PerfControl.GpuLevel;
            m_LastChangeTimeStamp = timestamp;
            APLog.Debug("Auto Perf Level: lower GPU level to {0}", m_PerfControl.GpuLevel);
        }

        private bool AllowLowerCpuLevel(float timestamp)
        {
            if (m_PerfControl.CpuLevel > 0 && timestamp - m_LastCpuLevelRaiseTimeStamp > CpuLevelBounceAvoidanceThreshold)
            {
                if (TargetFrameTime <= 0.0f)
                    return true;

                var ft = m_PerfStats.FrameTiming;
                if (ft.AverageCpuFrameTime <= 0.0f)
                    return true;

                // Allow lowering CPU level if main thread is active less than AllowedCpuActiveTimeRatio * 100% of the time.
                if (ft.AverageCpuFrameTime < AllowedCpuActiveTimeRatio * TargetFrameTime)
                    return true;
            }
            return false;
        }

        private bool AllowLowerGpuLevel(float timestamp)
        {
            if (m_PerfControl.GpuLevel > 0 && timestamp - m_LastGpuLevelRaiseTimeStamp > GpuLevelBounceAvoidanceThreshold)
            {
                if (TargetFrameTime <= 0.0f)
                    return true;

                var ft = m_PerfStats.FrameTiming;
                if (ft.AverageGpuFrameTime <= 0.0f)
                    return true;

                // Allow lowering GPU level if main thread is active less than AllowedGpuActiveTimeRatio * 100% of the time.
                if (ft.AverageGpuFrameTime < AllowedGpuActiveTimeRatio * TargetFrameTime)
                    return true;
            }
            return false;
        }

        private bool AllowRaiseLevels()
        {
            float tempLevel = m_ThermalStats.ThermalMetrics.TemperatureLevel;
            if (tempLevel < 0.0f)
                return true; // temperature level not supported, allow changing levels anyway

            // raise CPU/GPU levels only as long as device is reasonably cool
            if (tempLevel < MaxTemperatureLevel)
                return true;

            APLog.Debug("Auto Perf Level: cannot raise performance level, current temperature level ({0}) exceeds {1}", tempLevel, MaxTemperatureLevel);
            return false;
        }

        private bool AllowRaiseCpuLevel()
        {
            if (m_PerfControl.CpuLevel >= m_PerfControl.MaxCpuPerformanceLevel)
            {
                return false;
            }

            return AllowRaiseLevels();
        }

        private bool AllowRaiseGpuLevel()
        {
            if (m_PerfControl.GpuLevel >= m_PerfControl.MaxGpuPerformanceLevel)
            {
                return false;
            }

            return AllowRaiseLevels();
        }
    }
}
