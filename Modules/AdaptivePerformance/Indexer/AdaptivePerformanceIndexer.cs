// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Describes what action is needed to stabilize.
    /// </summary>
    public enum StateAction
    {
        /// <summary>
        /// No action is required.
        /// </summary>
        Stale,
        /// <summary>
        /// Recommended to increase quality.
        /// </summary>
        Increase,
        /// <summary>
        /// Quality must be decreased.
        /// </summary>
        Decrease,
        /// <summary>
        /// Quality must be decreased as soon as possible.
        /// </summary>
        FastDecrease,
    }

    /// <summary>
    /// System used for tracking device thermal state.
    /// </summary>
    internal class ThermalStateTracker
    {
        private float warningTemp = 1.0f;
        private float throttlingTemp = 1.0f;

        public ThermalStateTracker()
        {
        }

        public StateAction Update()
        {
            // Do not perform action if thermal level is not supported.
            if(!Holder.Instance.SupportedFeature(Provider.Feature.TemperatureLevel))
                return StateAction.Stale;
            float thermalTrend = Holder.Instance.ThermalStatus.ThermalMetrics.TemperatureTrend;

            var thermalLevel = Holder.Instance.ThermalStatus.ThermalMetrics.TemperatureLevel;
            var warning = Holder.Instance.ThermalStatus.ThermalMetrics.WarningLevel;

            if (warning == WarningLevel.ThrottlingImminent && warningTemp == 1.0f)
                warningTemp = thermalLevel; // remember throttling imminent level

            if (warning == WarningLevel.Throttling && throttlingTemp == 1.0f)
                throttlingTemp = thermalLevel; // remember throttling level

            // Throttling needs to cool down a lot before changing to no warning
            if (warning == WarningLevel.Throttling || thermalLevel >= throttlingTemp)
                return StateAction.FastDecrease;

            // warm device
            if (warning == WarningLevel.ThrottlingImminent || thermalLevel >= warningTemp)
            {
                // halfway to throttling?
                if (thermalLevel > (warningTemp + throttlingTemp) / 2)
                    return StateAction.Decrease;

                if (thermalTrend <= 0)
                    return StateAction.Stale;
                else if (thermalTrend > 0.5)
                    return StateAction.FastDecrease;
                else
                    return StateAction.Decrease;
            }


            // normal operating conditions
            if (warning == WarningLevel.NoWarning && thermalLevel < warningTemp)
            {
                if (thermalTrend <= 0)
                    return StateAction.Increase;
                else if (thermalTrend > 0.5)
                    return StateAction.FastDecrease;
                else if (thermalTrend > 0.1)
                    return StateAction.Decrease;
            }


            return StateAction.Stale;
        }
    }

    /// <summary>
    /// System used for tracking device performance state.
    /// </summary>
    internal class PerformanceStateTracker
    {
        private Queue<float> m_Samples;
        private int m_SampleCapacity;

        public float Trend { get; set; }

        public PerformanceStateTracker(int sampleCapacity)
        {
            m_Samples = new Queue<float>(sampleCapacity);
            m_SampleCapacity = sampleCapacity;
        }

        public StateAction Update()
        {
            var frameMs = Holder.Instance.PerformanceStatus.FrameTiming.AverageFrameTime;
            if (frameMs > 0)
            {
                var targetMs = 1f / GetEffectiveTargetFrameRate();
                var diffMs = (frameMs / targetMs) - 1;

                m_Samples.Enqueue(diffMs);
                if (m_Samples.Count > m_SampleCapacity)
                    m_Samples.Dequeue();
            }

            var trend = 0.0f;
            foreach (var sample in m_Samples)
                trend += sample;
            trend /= m_Samples.Count;
            Trend = trend;

            // It is underperforming heavily, we need to increase performance
            if (Trend >= 0.30)
                return StateAction.FastDecrease;

            // It is underperforming, we need to increase performance
            if (Trend >= 0.15)
                return StateAction.Decrease;

            // TODO: we need to way identify overperforming as currently AverageFrameTime is returned with vsync
            // return StaterAction.Increase;

            return StateAction.Stale;
        }

        protected virtual float GetEffectiveTargetFrameRate()
        {
            return AdaptivePerformanceManager.EffectiveTargetFrameRate();
        }
    }

    /// <summary>
    /// System used for tracking impact of scaler on CPU and GPU counters.
    /// </summary>
    internal class AdaptivePerformanceScalerEfficiencyTracker
    {
        private AdaptivePerformanceScaler m_Scaler;
        private float m_LastAverageGpuFrameTime;
        private float m_LastAverageCpuFrameTime;
        private bool m_IsApplied;

        public bool IsRunning { get => m_Scaler != null; }

        public void Start(AdaptivePerformanceScaler scaler, bool isApply)
        {
            Debug.Assert(!IsRunning, "AdaptivePerformanceScalerEfficiencyTracker is already running");
            m_Scaler = scaler;
            m_LastAverageGpuFrameTime = Holder.Instance.PerformanceStatus.FrameTiming.AverageGpuFrameTime;
            m_LastAverageCpuFrameTime = Holder.Instance.PerformanceStatus.FrameTiming.AverageCpuFrameTime;
            m_IsApplied = true;
        }

        public void Stop()
        {
            var gpu = Holder.Instance.PerformanceStatus.FrameTiming.AverageGpuFrameTime - m_LastAverageGpuFrameTime;
            var cpu = Holder.Instance.PerformanceStatus.FrameTiming.AverageCpuFrameTime - m_LastAverageCpuFrameTime;
            var sign = m_IsApplied ? 1 : -1;
            m_Scaler.GpuImpact = sign * (int)(gpu * 1000);
            m_Scaler.CpuImpact = sign * (int)(cpu * 1000);
            m_Scaler = null;
        }
    }

    /// <summary>
    /// Higher level implementation of Adaptive performance that tracks performance and thermal states of the device and provides them to <see cref="AdaptivePerformanceScaler"/> which use the information to increase or decrease performance levels.
    /// System acts as <see cref="AdaptivePerformanceScaler"/> manager and handles the lifetime of the scalers in the scenes.
    /// </summary>
    public class AdaptivePerformanceIndexer
    {
        private List<AdaptivePerformanceScaler> m_UnappliedScalers;
        private List<AdaptivePerformanceScaler> m_AppliedScalers;
        private List<AdaptivePerformanceScaler> m_DisabledScalers;
        private ThermalStateTracker m_ThermalStateTracker;
        private PerformanceStateTracker m_PerformanceStateTracker;
        private AdaptivePerformanceScalerEfficiencyTracker m_ScalerEfficiencyTracker;
        private IAdaptivePerformanceSettings m_Settings;
        const string m_FeatureName = "Indexer";

        /// <summary>
        /// Time left until next action.
        /// </summary>
        public float TimeUntilNextAction { get; private set; }

        /// <summary>
        /// Current determined action needed from thermal state.
        /// Action <see cref="StateAction.Increase"/> will be ignored if <see cref="PerformanceAction"/> is decreasing.
        /// </summary>
        public StateAction ThermalAction { get; private set; }

        /// <summary>
        /// Current determined action needed from performance state.
        /// Action <see cref="StateAction.Increase"/> will be ignored if <see cref="ThermalAction"/> is decreasing.
        /// </summary>
        public StateAction PerformanceAction { get; private set; }

        /// <summary>
        /// Returns all currently applied scalers.
        /// </summary>
        /// <param name="scalers">Output where scalers will be written.</param>
        public void GetAppliedScalers(ref List<AdaptivePerformanceScaler> scalers)
        {
            scalers.Clear();
            scalers.AddRange(m_AppliedScalers);
        }

        /// <summary>
        /// Returns all currently unapplied scalers.
        /// </summary>
        /// <param name="scalers">Output where scalers will be written.</param>
        public void GetUnappliedScalers(ref List<AdaptivePerformanceScaler> scalers)
        {
            scalers.Clear();
            scalers.AddRange(m_UnappliedScalers);
        }

        /// <summary>
        /// Returns all currently disabled scalers.
        /// </summary>
        /// <param name="scalers">Output where scalers will be written.</param>
        public void GetDisabledScalers(ref List<AdaptivePerformanceScaler> scalers)
        {
            scalers.Clear();
            scalers.AddRange(m_DisabledScalers);
        }

        /// <summary>
        /// Returns all scalers independent of their state.
        /// </summary>
        /// <param name="scalers">Output where scalers will be written.</param>
        public void GetAllRegisteredScalers(ref List<AdaptivePerformanceScaler> scalers)
        {
            scalers.Clear();
            scalers.AddRange(m_DisabledScalers);
            scalers.AddRange(m_UnappliedScalers);
            scalers.AddRange(m_AppliedScalers);
        }

        /// <summary>
        /// Unapply all currently active scalers.
        /// </summary>
        public void UnapplyAllScalers()
        {
            TimeUntilNextAction = m_Settings.indexerSettings.thermalActionDelay;
            while (m_AppliedScalers.Count != 0)
            {
                var scaler = m_AppliedScalers[0];
                UnapplyScaler(scaler);
            }
        }

        internal void UpdateOverrideLevel(AdaptivePerformanceScaler scaler)
        {
            if (scaler.OverrideLevel == -1)
                return;
            while (scaler.OverrideLevel > scaler.CurrentLevel)
                ApplyScaler(scaler);
            while (scaler.OverrideLevel < scaler.CurrentLevel)
                UnapplyScaler(scaler);
        }

        internal void AddScaler(AdaptivePerformanceScaler scaler)
        {
            if (m_UnappliedScalers.Contains(scaler) || m_AppliedScalers.Contains(scaler))
                return;

            m_UnappliedScalers.Add(scaler);
        }

        internal void RemoveScaler(AdaptivePerformanceScaler scaler)
        {
            if (m_UnappliedScalers.Contains(scaler))
            {
                m_UnappliedScalers.Remove(scaler);
                return;
            }

            if (m_AppliedScalers.Contains(scaler))
            {
                while (!scaler.NotLeveled)
                    scaler.DecreaseLevel();
                m_AppliedScalers.Remove(scaler);
            }
        }

        internal AdaptivePerformanceIndexer(ref IAdaptivePerformanceSettings settings, PerformanceStateTracker tracker)
        {
            m_Settings = settings;
            TimeUntilNextAction = m_Settings.indexerSettings.thermalActionDelay;
            m_ThermalStateTracker = new ThermalStateTracker();
            m_PerformanceStateTracker = tracker;
            m_UnappliedScalers = new List<AdaptivePerformanceScaler>();
            m_AppliedScalers = new List<AdaptivePerformanceScaler>();
            m_DisabledScalers = new List<AdaptivePerformanceScaler>();
            m_ScalerEfficiencyTracker = new AdaptivePerformanceScalerEfficiencyTracker();

            AdaptivePerformanceAnalytics.RegisterFeature(m_FeatureName, m_Settings.indexerSettings.active);
        }

        internal void Update()
        {
            if (Holder.Instance == null || !m_Settings.indexerSettings.active)
                return;

            DeactivateDisabledScalers();
            ActivateEnabledScalers();

            var thermalAction = m_ThermalStateTracker.Update();
            var performanceAction = m_PerformanceStateTracker.Update();

            ThermalAction = thermalAction;
            PerformanceAction = performanceAction;

            if (Profiler.enabled)
                CollectProfilerStats();

            // Enforce minimum wait time between any scaler changes
            TimeUntilNextAction = Mathf.Max(TimeUntilNextAction - DeltaTime(), 0);
            if (TimeUntilNextAction != 0)
                return;

            if (m_ScalerEfficiencyTracker.IsRunning)
                m_ScalerEfficiencyTracker.Stop();

            if (thermalAction == StateAction.Increase && performanceAction == StateAction.Stale)
            {
                UnapplyHighestCostScaler();
                TimeUntilNextAction = m_Settings.indexerSettings.thermalActionDelay;
                return;
            }
            if (thermalAction == StateAction.Stale && performanceAction == StateAction.Stale)
            {
                UnapplyHighestCostScaler();
                TimeUntilNextAction = m_Settings.indexerSettings.thermalActionDelay;
                return;
            }
            if (thermalAction == StateAction.Decrease)
            {
                ApplyLowestCostScaler();
                TimeUntilNextAction = m_Settings.indexerSettings.thermalActionDelay;
                return;
            }
            if (performanceAction == StateAction.Decrease)
            {
                ApplyLowestCostScaler();
                TimeUntilNextAction = m_Settings.indexerSettings.performanceActionDelay;
                return;
            }
            if (thermalAction == StateAction.FastDecrease)
            {
                ApplyLowestCostScaler();
                TimeUntilNextAction = m_Settings.indexerSettings.thermalActionDelay / 2;
                return;
            }
            if (performanceAction == StateAction.FastDecrease)
            {
                ApplyLowestCostScaler();
                TimeUntilNextAction = m_Settings.indexerSettings.performanceActionDelay / 2;
                return;
            }
        }
        /// <summary>
        /// Returns <see cref="Time.deltaTime"/> only and is primarily encapsulated for tests.
        /// </summary>
        /// <returns>delta time</returns>
        protected virtual float DeltaTime()
        {
            return Time.deltaTime;
        }

        void CollectProfilerStats()
        {
            for (int i = m_UnappliedScalers.Count - 1; i >= 0; i--)
            {
                var scaler = m_UnappliedScalers[i];
                AdaptivePerformanceProfilerStats.EmitScalerDataToProfilerStream(scaler.Name, scaler.Enabled, scaler.OverrideLevel, scaler.CurrentLevel, scaler.Scale, false, scaler.MaxLevel);
            }

            for (int i = m_AppliedScalers.Count - 1; i >= 0; i--)
            {
                var scaler = m_AppliedScalers[i];
                AdaptivePerformanceProfilerStats.EmitScalerDataToProfilerStream(scaler.Name, scaler.Enabled, scaler.OverrideLevel, scaler.CurrentLevel, scaler.Scale, true, scaler.MaxLevel);
            }
            for (int i = m_DisabledScalers.Count - 1; i >= 0; i--)
            {
                var scaler = m_DisabledScalers[i];
                AdaptivePerformanceProfilerStats.EmitScalerDataToProfilerStream(scaler.Name, scaler.Enabled, scaler.OverrideLevel, scaler.CurrentLevel, scaler.Scale, false, scaler.MaxLevel);
            }
            AdaptivePerformanceProfilerStats.FlushScalerDataToProfilerStream();
        }

        void DeactivateDisabledScalers()
        {
            for (int i = m_UnappliedScalers.Count - 1; i >= 0; i--)
            {
                var scaler = m_UnappliedScalers[i];
                if (!scaler.Enabled && !m_DisabledScalers.Contains(scaler))
                {
                    APLog.Debug($"[Indexer] Deactivated {scaler.Name} scaler.");
                    scaler.Deactivate();
                    m_DisabledScalers.Add(scaler);
                    m_UnappliedScalers.RemoveAt(i);
                }
            }

            for (int i = m_AppliedScalers.Count - 1; i >= 0; i--)
            {
                var scaler = m_AppliedScalers[i];
                if (!scaler.Enabled && !m_DisabledScalers.Contains(scaler))
                {
                    APLog.Debug($"[Indexer] Deactivated {scaler.Name} scaler.");
                    scaler.Deactivate();
                    m_DisabledScalers.Add(scaler);
                    m_AppliedScalers.RemoveAt(i);
                }
            }
        }

        void ActivateEnabledScalers()
        {
            for (int i = m_DisabledScalers.Count - 1; i >= 0; i--)
            {
                var scaler = m_DisabledScalers[i];
                if (scaler.Enabled)
                {
                    scaler.Activate();
                    AddScaler(scaler);
                    m_DisabledScalers.RemoveAt(i);
                    APLog.Debug($"[Indexer] Activated {scaler.Name} scaler.");
                }
            }
        }

        private bool ApplyLowestCostScaler()
        {
            AdaptivePerformanceScaler result = null;
            var lowestCost = float.PositiveInfinity;

            foreach (var scaler in m_UnappliedScalers)
            {
                if (!scaler.Enabled)
                    continue;

                if (scaler.OverrideLevel != -1)
                    continue;

                var cost = scaler.CalculateCost();

                if (lowestCost > cost)
                {
                    result = scaler;
                    lowestCost = cost;
                }
            }

            foreach (var scaler in m_AppliedScalers)
            {
                if (!scaler.Enabled)
                    continue;

                if (scaler.OverrideLevel != -1)
                    continue;

                if (scaler.IsMaxLevel)
                    continue;

                var cost = scaler.CalculateCost();

                if (lowestCost > cost)
                {
                    result = scaler;
                    lowestCost = cost;
                }
            }

            if (result != null)
            {
                m_ScalerEfficiencyTracker.Start(result, true);

                ApplyScaler(result);
                return true;
            }

            return false;
        }

        private void ApplyScaler(AdaptivePerformanceScaler scaler)
        {
            APLog.Debug($"[Indexer] Applying {scaler.Name} scaler at level {scaler.CurrentLevel} and try to increase level to {scaler.CurrentLevel+1}");
            if (scaler.NotLeveled)
            {
                m_UnappliedScalers.Remove(scaler);
                m_AppliedScalers.Add(scaler);
            }
            scaler.IncreaseLevel();
        }

        private bool UnapplyHighestCostScaler()
        {
            AdaptivePerformanceScaler result = null;
            var highestCost = float.NegativeInfinity;

            foreach (var scaler in m_AppliedScalers)
            {
                if (scaler.OverrideLevel != -1)
                    continue;

                var cost = scaler.CalculateCost();

                if (highestCost < cost)
                {
                    result = scaler;
                    highestCost = cost;
                }
            }

            if (result != null)
            {
                m_ScalerEfficiencyTracker.Start(result, false);

                UnapplyScaler(result);
                return true;
            }

            return false;
        }

        private void UnapplyScaler(AdaptivePerformanceScaler scaler)
        {
            APLog.Debug($"[Indexer] Unapplying {scaler.Name} scaler at level {scaler.CurrentLevel} and try to decrease level to {scaler.CurrentLevel-1}");
            scaler.DecreaseLevel();
            if (scaler.NotLeveled)
            {
                m_AppliedScalers.Remove(scaler);
                m_UnappliedScalers.Add(scaler);
            }
        }
    }
}
