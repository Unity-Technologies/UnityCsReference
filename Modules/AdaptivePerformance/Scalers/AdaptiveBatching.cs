// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> to control if dynamic batching is enabled.
    /// </summary>
    [System.Obsolete("AdaptiveBatching is deprecated and will be removed in a future release. #from(6000.5)", false)]
    public class AdaptiveBatching : AdaptivePerformanceScaler
    {
        bool m_DefaultState;

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
#pragma warning disable 618
            ApplyDefaultSetting(m_Settings.scalerSettings.AdaptiveBatching);
#pragma warning restore 618
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
#pragma warning disable 618
            AdaptivePerformanceRenderSettings.SkipDynamicBatching = m_DefaultState;
#pragma warning restore 618
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
#pragma warning disable 618
            m_DefaultState = AdaptivePerformanceRenderSettings.SkipDynamicBatching;
#pragma warning restore 618
        }

        /// <summary>
        /// Callback for any level change.
        /// </summary>
        protected override void OnLevel()
        {
            if (ScaleChanged())
            {
#pragma warning disable 618
                AdaptivePerformanceRenderSettings.SkipDynamicBatching = (Scale < 1);
#pragma warning restore 618
            }
        }
    }
}
