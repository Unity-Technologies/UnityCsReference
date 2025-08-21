// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> for adjusting <see href="https://docs.unity3d.com/ScriptReference/Camera-layerCullDistances.html">layer culling</see> distances.
    /// </summary>
    public class AdaptiveLayerCulling : AdaptivePerformanceScaler
    {
        float[] m_defaultDistances = new float[32];
        float[] m_scaledDistances = new float[32];
        bool init = false;
        Camera m_cachedCamera;

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyDefaultSetting(m_Settings.scalerSettings.AdaptiveLayerCulling);
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            init = false;
            if (!Camera.main || m_defaultDistances == null)
                return;
            Camera.main.layerCullDistances = m_defaultDistances;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            AsignDefaultValues();
        }

        /// <summary>
        /// Callback for any level change.
        /// </summary>
        protected override void OnLevel()
        {
            if (!Camera.main)
                return;

            AsignDefaultValues();

            if (ScaleChanged())
            {
                for (var i = 31; i >= 0; --i)
                {
                    if (m_defaultDistances[i] == 0)
                        continue;

                    m_scaledDistances[i] = m_defaultDistances[i] * Scale;
                }
                Camera.main.layerCullDistances = m_scaledDistances;
            }
        }

        void AsignDefaultValues()
        {
            if (m_cachedCamera == null || m_cachedCamera != Camera.main)
            {
                m_cachedCamera = Camera.main;
                init = false;
            }

            if (init || !Camera.main)
                return;

            m_defaultDistances = Camera.main.layerCullDistances;
            init = true;
        }
    }
}
