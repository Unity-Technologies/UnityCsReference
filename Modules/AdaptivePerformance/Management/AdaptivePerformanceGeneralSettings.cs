// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A `ScriptableObject` that contains global settings applicable to all Adaptive Performance providers.
    /// </summary>
    public class AdaptivePerformanceGeneralSettings : ScriptableObject
    {
        /// <summary>The key used to query to get the current loader settings.</summary>
        public static string k_SettingsKey = "com.unity.adaptiveperformance.loader_settings";
        internal static AdaptivePerformanceGeneralSettings s_RuntimeSettingsInstance = null;

        [SerializeField]
        internal AdaptivePerformanceManagerSettings m_LoaderManagerInstance = null;

        [SerializeField]
        [Tooltip("Enable this to automatically start up Adaptive Performance at runtime.")]
        internal bool m_InitManagerOnStart = true;

        [SerializeField]
        [VisibleToOtherModules("UnityEditor.AdaptivePerformanceModule")]
        internal string m_LastSelectedProvider = "";

        /// <summary>The current active manager used to manage the Adaptive Performance lifetime.</summary>
        public AdaptivePerformanceManagerSettings Manager
        {
            get { return m_LoaderManagerInstance; }
            set { m_LoaderManagerInstance = value; }
        }

        private AdaptivePerformanceManagerSettings m_AdaptivePerformanceManager = null;
        private bool m_ProviderIntialized = false;
        private bool m_ProviderStarted = false;


        /// <summary>
        /// Indicates if provider loader has been initialized.
        /// </summary>
        public bool IsProviderInitialized
        {
            get { return m_ProviderIntialized; }
        }

        /// <summary>
        /// Indicates if provider loader and subsystem has been started.
        /// </summary>
        public bool IsProviderStarted
        {
            get { return m_ProviderStarted; }
        }

        /// <summary>The current settings instance.</summary>
        public static AdaptivePerformanceGeneralSettings Instance
        {
            get
            {
                return s_RuntimeSettingsInstance;
            }
            set
            {
                s_RuntimeSettingsInstance = value;
            }
        }

        /// <summary>
        /// The current active manager used to manage the Adaptive Performance lifetime.
        /// </summary>
        public AdaptivePerformanceManagerSettings AssignedSettings
        {
            get
            {
                return m_LoaderManagerInstance;
            }
            set
            {
                m_LoaderManagerInstance = value;
            }
        }

        /// <summary>
        /// Used to set if the manager is activated and initialized on startup.
        /// </summary>
        public bool InitManagerOnStart
        {
            get
            {
                return m_InitManagerOnStart;
            }
            set
            {
                m_InitManagerOnStart = value;
            }
        }


        static void Quit()
        {
            AdaptivePerformanceGeneralSettings instance = AdaptivePerformanceGeneralSettings.Instance;
            if (instance == null)
                return;

            instance.DeInitAdaptivePerformance();
        }

        void OnDestroy()
        {
            DeInitAdaptivePerformance();
            s_RuntimeSettingsInstance = null;
        }

        [RequiredByNativeCode(optional: true)]
        internal static void AttemptInitializeAdaptivePerformanceGeneralSettingsOnLoad()
        {
            AdaptivePerformanceGeneralSettings instance = AdaptivePerformanceGeneralSettings.Instance;
            if (instance == null || !instance.InitManagerOnStart)
                return;

            instance.InitAdaptivePerformance();
        }

        [RequiredByNativeCode(optional: true)]
        internal static void AttemptStartAdaptivePerformanceGeneralSettingsOnBeforeSplashScreen()
        {
            AdaptivePerformanceGeneralSettings instance = AdaptivePerformanceGeneralSettings.Instance;
            if (instance == null || !instance.InitManagerOnStart)
                return;

            instance.StartAdaptivePerformance();
        }

        internal void InitAdaptivePerformance()
        {
            if (m_ProviderIntialized)
                return;

            if (AdaptivePerformanceGeneralSettings.Instance == null)
                return;

            m_AdaptivePerformanceManager = AdaptivePerformanceGeneralSettings.Instance.m_LoaderManagerInstance;
            if (m_AdaptivePerformanceManager == null)
            {
                Debug.LogError("Assigned GameObject for Adaptive Performance Management loading is invalid. No Adaptive Performance Providers will be automatically loaded.");
                return;
            }

            m_AdaptivePerformanceManager.automaticLoading = false;
            m_AdaptivePerformanceManager.automaticRunning = false;
            m_AdaptivePerformanceManager.InitializeLoaderSync();

            if (m_AdaptivePerformanceManager.activeLoader == null)
                return;

            m_ProviderIntialized = true;
        }

        internal void StartAdaptivePerformance()
        {
            if (!m_ProviderIntialized || m_ProviderStarted)
                return;

            if (m_AdaptivePerformanceManager == null || m_AdaptivePerformanceManager.activeLoader == null)
                return;

            m_AdaptivePerformanceManager.StartSubsystems();
            m_ProviderStarted = true;
        }

        internal void StopAdaptivePerformance()
        {
            if (!m_ProviderIntialized || !m_ProviderStarted)
                return;

            if (m_AdaptivePerformanceManager == null || m_AdaptivePerformanceManager.activeLoader == null)
                return;

            m_AdaptivePerformanceManager.StopSubsystems();
            m_ProviderStarted = false;
        }

        internal void DeInitAdaptivePerformance()
        {
            if (!m_ProviderIntialized)
                return;

            if (m_ProviderStarted)
                StopAdaptivePerformance();

            if (m_AdaptivePerformanceManager != null)
            {
                m_AdaptivePerformanceManager.DeinitializeLoader();
                m_AdaptivePerformanceManager = null;
            }

            m_ProviderIntialized = false;
        }
    }
}
