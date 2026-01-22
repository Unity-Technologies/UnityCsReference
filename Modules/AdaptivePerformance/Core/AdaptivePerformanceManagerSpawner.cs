// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Assemblies;

namespace UnityEngine.AdaptivePerformance
{
    internal class AdaptivePerformanceManagerSpawner : ScriptableObject
    {
        public const string AdaptivePerformanceManagerObjectName = "AdaptivePerformanceManager";

        GameObject m_ManagerGameObject;

        public GameObject ManagerGameObject { get { return m_ManagerGameObject; } }

        void OnEnable()
        {
            if (m_ManagerGameObject != null)
                return;

            m_ManagerGameObject = GameObject.Find(AdaptivePerformanceManagerObjectName);
        }

        public void Initialize(bool isCheckingProvider)
        {
            if (m_ManagerGameObject != null)
                return;

            m_ManagerGameObject = new GameObject(AdaptivePerformanceManagerObjectName);
            var apm = m_ManagerGameObject.AddComponent<AdaptivePerformanceManager>();

            if (isCheckingProvider)
            {
                // if no provider was found we can disable AP and destroy the game object, otherwise continue with initialization.
                if (apm.Indexer == null)
                {
                    Deinitialize();

                    return;
                }
            }

            Holder.Instance = apm;
            DontDestroyOnLoad(m_ManagerGameObject);

            var settings = apm.Settings;
            if (settings == null)
                return;

            var scalerProfiles = settings.GetAvailableScalerProfiles();
            if (scalerProfiles.Length <= 0)
            {
                APLog.Debug("No Scaler Profiles available. Did you remove all profiles manually from the provider Settings?");
                return;
            }
            settings.LoadScalerProfile(scalerProfiles[settings.defaultScalerProfilerIndex]);
            InstallScalers(settings.ScalerProfiles[settings.defaultScalerProfilerIndex], settings);
        }
        public void Deinitialize()
        {
            if (m_ManagerGameObject == null)
                return;

            DestroyImmediate(m_ManagerGameObject);

            m_ManagerGameObject = null;
        }

        void InstallScalers(AdaptivePerformanceScalerProfile profile, IAdaptivePerformanceSettings settings)
        {

            foreach (var scalerName in AdaptivePerformanceScalerSettings.k_DefaultScalerNames)
            {
                ScriptableObject.CreateInstance(scalerName);
            }
            // prioritize scalers added from UI.
            if (profile.AddedScalers != null && profile.AddedScalers.Count > 0)
            {
                profile.EnableAddedScalers();
            }
            // Applies to all profiles like the old way.
            // if the scalers are added via scanning the dir, do not add the custom scalers from UI.
            else if (settings.AddedScalerViaScan != null && settings.AddedScalerViaScan.Count > 0)
            {
                for (int i = 0; i < settings.AddedScalerViaScan.Count; i++)
                {
                    settings.AddedScalerViaScan[i].InitializeScaler();
                }
            }
        }
    }
}
