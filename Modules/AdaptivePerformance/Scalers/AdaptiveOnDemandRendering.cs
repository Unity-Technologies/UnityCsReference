// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AdaptivePerformance not yet converted
namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> to render frames less frequently by increasing the on-demand rendering frame interval.
    /// </summary>
    /// <remarks>
    /// In battery mode, the scaler raises `OnDemandRendering.renderFrameInterval` based on the Indexer's <see cref="AdaptivePerformanceIndexer.IdleScale"/> to save power while the player is idle. In normal mode, it sets the interval from the scaler's current level.
    /// </remarks>
    public class AdaptiveOnDemandRendering : AdaptivePerformanceScaler
    {
        int m_DefaultRenderingInterval;
        int m_IdleScale = 1;

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyProfileSettings(m_Settings.scalerSettings.AdaptiveOnDemandRendering);
        }

        // Per frame scaler action.
        protected override void OnOperationMode()
        {
            if (Holder.Instance == null || Holder.Instance.Indexer == null)
                return;

            if(IndexerOperationMode == OperationMode.BatteryMode && m_IdleScale != Holder.Instance.Indexer.IdleScale)
            {
                m_IdleScale = Holder.Instance.Indexer.IdleScale;
                Rendering.OnDemandRendering.renderFrameInterval = Mathf.Min((int)MaxBound, m_IdleScale);
            }
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            Rendering.OnDemandRendering.renderFrameInterval = m_DefaultRenderingInterval;
            m_IdleScale = 1;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            m_DefaultRenderingInterval = Rendering.OnDemandRendering.renderFrameInterval;
        }

        /// <summary>
        /// Callback for any level change.
        /// </summary>
        protected override void OnLevel()
        {
            if (ScaleChanged())
            {
                if(IndexerOperationMode == OperationMode.NormalMode)
                    Rendering.OnDemandRendering.renderFrameInterval = Mathf.Max(1, CurrentLevel + 1);
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
