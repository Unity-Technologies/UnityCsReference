// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal class DevicePerformanceControlImpl : IDevicePerformanceControl
    {
        Provider.IDevicePerformanceLevelControl m_PerformanceLevelControl;
        public DevicePerformanceControlImpl(Provider.IDevicePerformanceLevelControl performanceLevelControl)
        {
            m_PerformanceLevelControl = performanceLevelControl;
            PerformanceControlMode = PerformanceControlMode.Automatic;
            CurrentCpuLevel = Constants.UnknownPerformanceLevel;
            CurrentGpuLevel = Constants.UnknownPerformanceLevel;
            CpuLevel = Constants.UnknownPerformanceLevel;
            GpuLevel = Constants.UnknownPerformanceLevel;
        }

        public bool Update(out PerformanceLevelChangeEventArgs changeArgs)
        {
            changeArgs = new PerformanceLevelChangeEventArgs();
            changeArgs.PerformanceControlMode = PerformanceControlMode;

            if (PerformanceControlMode == PerformanceControlMode.System)
            {
                bool changed = CurrentCpuLevel != Constants.UnknownPerformanceLevel || CurrentGpuLevel != Constants.UnknownPerformanceLevel;
                CurrentCpuLevel = Constants.UnknownPerformanceLevel;
                CurrentGpuLevel = Constants.UnknownPerformanceLevel;

                if (changed)
                {
                    changeArgs.CpuLevel = CurrentCpuLevel;
                    changeArgs.GpuLevel = CurrentGpuLevel;
                    changeArgs.CpuLevelDelta = 0;
                    changeArgs.GpuLevelDelta = 0;
                }
                return changed;
            }

            if (CpuLevel != Constants.UnknownPerformanceLevel || GpuLevel != Constants.UnknownPerformanceLevel)
            {
                if (CpuLevel != CurrentCpuLevel || GpuLevel != CurrentGpuLevel)
                {
                    var tempCpuLevel = CpuLevel;
                    var tempGpuLevel = GpuLevel;
                    if (m_PerformanceLevelControl.SetPerformanceLevel(ref tempCpuLevel, ref tempGpuLevel))
                    {
                        changeArgs.CpuLevelDelta = ComputeDelta(CurrentCpuLevel, tempCpuLevel);
                        changeArgs.GpuLevelDelta = ComputeDelta(CurrentGpuLevel, tempGpuLevel);
                        if (tempCpuLevel != CpuLevel || tempGpuLevel != GpuLevel)
                            Debug.Log($"Requested CPU level {CpuLevel} and GPU level {GpuLevel} was overriden by System with CPU level {tempCpuLevel} and GPU level {tempGpuLevel}");
                        CurrentCpuLevel = CpuLevel;
                        CurrentGpuLevel = GpuLevel;
                    }
                    else
                    {
                        changeArgs.CpuLevelDelta = 0;
                        changeArgs.GpuLevelDelta = 0;
                        CurrentCpuLevel = Constants.UnknownPerformanceLevel;
                        CurrentGpuLevel = Constants.UnknownPerformanceLevel;
                        CpuLevel = Constants.UnknownPerformanceLevel;
                        GpuLevel = Constants.UnknownPerformanceLevel;
                        return false;
                    }

                    changeArgs.CpuLevel = CurrentCpuLevel;
                    changeArgs.GpuLevel = CurrentGpuLevel;

                    return true;
                }
            }

            return false;
        }

        private int ComputeDelta(int oldLevel, int newLevel)
        {
            if (oldLevel < 0 || newLevel < 0)
                return 0;

            return newLevel - oldLevel;
        }

        /// <summary>
        /// DevicePerformanceControlImpl does not implement AutomaticPerformanceControl.
        /// </summary>
        public bool AutomaticPerformanceControl { get { return false; } set {} }

        public PerformanceControlMode PerformanceControlMode { get; set; }

        public int MaxCpuPerformanceLevel { get { return m_PerformanceLevelControl != null ? m_PerformanceLevelControl.MaxCpuPerformanceLevel : Constants.UnknownPerformanceLevel; } }

        public int MaxGpuPerformanceLevel { get { return m_PerformanceLevelControl != null ? m_PerformanceLevelControl.MaxGpuPerformanceLevel : Constants.UnknownPerformanceLevel; } }

        public int CpuLevel { get; set; }

        public int GpuLevel { get; set; }

        public int CurrentCpuLevel { get; set; }
        public int CurrentGpuLevel { get; set; }

        public bool CpuPerformanceBoost { get; set; }

        public bool GpuPerformanceBoost { get; set; }
    }
}
