// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> to change the distance at which shadows are rendered.
    /// </summary>
    public class AdaptiveShadowDistance : AdaptivePerformanceScaler
    {
        float m_DefaultShadowDistance;
        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyDefaultSetting(m_Settings.scalerSettings.AdaptiveShadowDistance);
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier = m_DefaultShadowDistance;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            m_DefaultShadowDistance = AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier;
        }

        /// <summary>
        /// Callback for any level change.
        /// </summary>
        protected override void OnLevel()
        {
            if (ScaleChanged())
                AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier = 1 * Scale;
        }
    }
}
