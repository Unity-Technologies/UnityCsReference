// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal class AutoPerformanceModeController
    {
        string m_FeatureName = "Auto Performance Mode Control";

        public AutoPerformanceModeController(IPerformanceModeStatus perfModeStat)
        {
            perfModeStat.PerformanceModeEvent += (PerformanceMode mode) => OnPerformanceModeChange(mode);
            AdaptivePerformanceAnalytics.RegisterFeature(m_FeatureName, true);
        }

        private void OnPerformanceModeChange(PerformanceMode performanceMode)
        {
            switch (performanceMode)
            {
                case PerformanceMode.Battery:
                    Application.targetFrameRate = 30;
                    break;
                case PerformanceMode.Optimize:
                    Application.targetFrameRate = (int)(Screen.currentResolution.refreshRateRatio.value);
                    break;
                default:
                    Application.targetFrameRate = -1;
                    break;
            }
            APLog.Debug($"[AutoPerformanceModeController] Performance Mode: {performanceMode}, fps: {Application.targetFrameRate}");
        }
    }
}
