// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

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
            InstallScalers();
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
        }
        public void Deinitialize()
        {
            if (m_ManagerGameObject == null)
                return;

            Destroy(m_ManagerGameObject);

            m_ManagerGameObject = null;
        }

        void InstallScalers()
        {
            Type ti = typeof(AdaptivePerformanceScaler);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (ti.IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        ScriptableObject.CreateInstance(t);
                    }
                }
            }
        }
    }
}
