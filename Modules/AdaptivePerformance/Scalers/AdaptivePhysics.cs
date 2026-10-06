// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> for adjusting how physics is applied.
    /// </summary>
    public class AdaptivePhysics : AdaptivePerformanceScaler
    {
        float m_fixedDeltaTimeDefault;
        int m_idleScale = 1;

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyProfileSettings(m_Settings.scalerSettings.AdaptivePhysics);
        }

        protected override void OnOperationMode()
        {

            if (Holder.Instance == null || Holder.Instance.Indexer == null)
                return;

            if(IndexerOperationMode == OperationMode.BatteryMode && m_idleScale != Holder.Instance.Indexer.IdleScale)
            {
                // Scales fixedDeltaTime based on idle time and the level.
                m_idleScale = Holder.Instance.Indexer.IdleScale;
                Time.fixedDeltaTime = Mathf.Min(m_fixedDeltaTimeDefault * m_idleScale, MaxBound);
            }
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            Time.fixedDeltaTime = m_fixedDeltaTimeDefault;
            m_idleScale = 1;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            m_fixedDeltaTimeDefault = Time.fixedDeltaTime;
        }

        /// <summary>
        /// Callback for any level change.
        /// </summary>
        protected override void OnLevel()
        {
            if (ScaleChanged())
            {
                if(IndexerOperationMode == OperationMode.NormalMode)
                    Time.fixedDeltaTime = m_fixedDeltaTimeDefault / Scale;
            }
        }
    }
}
