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
        /// <summary>
        /// Sets the targeted framerate for the application. 
        /// </summary>
        /// <param name="framerate">The target framerate to set for the application.</param>
        public void SetFrameRate(int framerate)
        {
            Application.targetFrameRate = framerate;
        }

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyProfileSettings(m_Settings.scalerSettings.AdaptiveFramerate);
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            Application.targetFrameRate = m_DefaultFPS;
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled()
        {
            m_DefaultFPS = Application.targetFrameRate;
            if(IndexerOperationMode == OperationMode.NormalMode)
                Application.targetFrameRate = (int)MaxBound;
        }

        /// <summary>
        /// Callback for when the quality level is decreased/scaler level increased.
        /// </summary>
        protected override void OnLevelIncrease()
        {
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
            var fps = Application.targetFrameRate + 5;
            if (fps >= MinBound && fps <= MaxBound)
                Application.targetFrameRate = fps;
        }
    }
}
