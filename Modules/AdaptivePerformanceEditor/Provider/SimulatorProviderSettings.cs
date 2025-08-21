// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Simulator.Editor
{
    /// <summary>
    /// Provider Settings for Simulator Provider which controls the editor runtime asset instance which stores the Settings.
    /// </summary>
    [System.Serializable]
    [AdaptivePerformanceConfigurationData("Simulator", SimulatorProviderConstants.k_SettingsKey)]
    public class SimulatorProviderSettings : IAdaptivePerformanceSettings
    {
        static SimulatorProviderSettings m_Settings = null;

        /// <summary>
        /// Returns Samsung Provider Settings which are used by Adaptive Performance to apply Provider Settings.
        /// </summary>
        /// <returns>Samsung Provider Settings</returns>
        public static SimulatorProviderSettings GetSettings()
        {
            if (m_Settings == null)
            {
                SimulatorProviderSettings settings;
                EditorBuildSettings.TryGetConfigObject<SimulatorProviderSettings>(SimulatorProviderConstants.k_SettingsKey, out settings);
                // Create a copy, as we do not want to save the settings we apply during runtime to our settings in the Editor.
                m_Settings = ScriptableObject.CreateInstance<SimulatorProviderSettings>();
                EditorUtility.CopySerialized(settings, m_Settings);
            }
            return m_Settings;
        }
    }
}
