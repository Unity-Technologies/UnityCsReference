// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> for adjusting what view distance is applied to the camera.
    /// </summary>
    public class AdaptiveViewDistance : AdaptivePerformanceScaler
    {
        float m_DefaultFarClipPlane = -1;

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyDefaultSetting(m_Settings.scalerSettings.AdaptiveViewDistance);
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            if (!Camera.main || m_DefaultFarClipPlane == -1)
                return;

            Camera.main.farClipPlane = m_DefaultFarClipPlane;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            if (!Camera.main)
                return;

            m_DefaultFarClipPlane = Camera.main.farClipPlane;
        }

        /// <summary>
        /// Callback for any level change.
        /// </summary>
        protected override void OnLevel()
        {
            if (!Camera.main)
                return;

            if (m_DefaultFarClipPlane == -1)
                m_DefaultFarClipPlane = Camera.main.farClipPlane;

            if (ScaleChanged())
                Camera.main.farClipPlane = Scale;
        }
    }
}
