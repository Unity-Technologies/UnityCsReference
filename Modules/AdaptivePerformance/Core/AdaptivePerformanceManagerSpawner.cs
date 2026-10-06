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
            if (settings.ActiveModeProvider == null)
            {
                switch (settings.IndexerOperationMode)
                {
                    case OperationMode.BatteryMode:
                        Holder.Instance.OperationModeStatus.CurrentOperationalModeProvider = settings.BatteryModeProvider;
                        break;
                    case OperationMode.StableFrameMode:
                        Holder.Instance.OperationModeStatus.CurrentOperationalModeProvider = settings.StableFrameModeProvider;
                        break;
                    case OperationMode.NormalMode:
                    default:
                        Holder.Instance.OperationModeStatus.CurrentOperationalModeProvider = settings.NormalModeProvider;
                        break;
                }
            }
            else
            {
                // The previous manager ended this mode when it was deinitialized
                settings.ActiveModeProvider.OnOperationModeStart();
            }
        }
        public void Deinitialize()
        {
            if (m_ManagerGameObject == null)
                return;

            DestroyImmediate(m_ManagerGameObject);

            m_ManagerGameObject = null;
        }

        // Initialize custom scalers added from UI or via scanning.
        // Profile no longer contains default scalers - only custom added scalers.
        void InstallScalers(AdaptivePerformanceScalerProfile profile, IAdaptivePerformanceSettings settings)
        {
            foreach (var scalerName in AdaptivePerformanceScalerSettings.k_DefaultScalerNames)
            {
                ScriptableObject.CreateInstance(scalerName);
            }
            // Initialize scalers added from UI
            if (profile.AddedScalers != null && profile.AddedScalers.Count > 0)
            {
                profile.EnableAddedScalers();
            }
            // If no UI scalers, try scalers added via scanning the dir
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
