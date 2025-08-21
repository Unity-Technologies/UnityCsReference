// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> to adjust the application update rate using <see cref="Application.targetFrameRate"/>.
    /// </summary>
    public class AdaptiveFramerate : AdaptivePerformanceScaler
    {
        int m_DefaultFPS;
        int m_FirstTimeStart = 0; // APB-34 When initiated Unity might not have set the target framerate correctly and when disabling the scaler initially it would override the wrong framerate.

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            m_FirstTimeStart = 0;
            if (m_Settings == null)
                return;
            ApplyDefaultSetting(m_Settings.scalerSettings.AdaptiveFramerate);
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            if (m_FirstTimeStart<2)
            {
                m_FirstTimeStart++;
                return;
            }
            Application.targetFrameRate = m_DefaultFPS;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            if (m_FirstTimeStart < 2)
                return;

            m_DefaultFPS = Application.targetFrameRate;
            Application.targetFrameRate = (int)MaxBound;
        }

        /// <summary>
        /// Callback for when the quality level is decreased/scaler level increased.
        /// </summary>
        protected override void OnLevelIncrease()
        {
            base.OnLevelIncrease();

            var framerateDecrease = 1;

            if (Holder.Instance.Indexer.PerformanceAction == StateAction.FastDecrease)
                framerateDecrease = 5;

            var fps = Application.targetFrameRate - framerateDecrease;

            if (fps >= MinBound && fps <= MaxBound)
                Application.targetFrameRate = fps;
        }

        /// <summary>
        /// Callback for when the quality level is increased/scaler level decreased.
        /// </summary>
        protected override void OnLevelDecrease()
        {
            base.OnLevelDecrease();

            var fps = Application.targetFrameRate + 5;
            if (fps >= MinBound && fps <= MaxBound)
                Application.targetFrameRate = fps;
        }
    }
}
