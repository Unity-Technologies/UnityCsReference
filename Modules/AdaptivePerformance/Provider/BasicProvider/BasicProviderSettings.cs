// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEngine.AdaptivePerformance.Basic
{
    /// <summary>
    /// Basic provider Settings.
    /// </summary>
    [System.Serializable]
    [AdaptivePerformanceConfigurationData("Basic", BasicProviderConstants.k_SettingKey)]
    public class BasicProviderSettings: IAdaptivePerformanceSettings
    {
        static BasicProviderSettings m_Instance = null;
        void Awake()
        {
            m_Instance = this;
        }

        internal static BasicProviderSettings GetSettings()
        {
            return m_Instance;
        }
    }
}
