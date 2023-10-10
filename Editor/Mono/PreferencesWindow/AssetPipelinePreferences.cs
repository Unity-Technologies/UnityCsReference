// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal enum AssetPipelineAutoRefreshMode
    {
        Disabled = 0,
        Enabled = 1,
        EnabledOutsidePlaymode = 2
    }

    internal class AssetPipelinePreferences : PreferencesProvider
    {
        class Properties
        {
            public static readonly GUIContent autoRefresh = EditorGUIUtility.TrTextContent("Auto Refresh", "Automatically import changed assets.");
            public static readonly GUIContent autoRefreshHelpBox = EditorGUIUtility.TrTextContent("Auto Refresh must be set when using Collaboration feature.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent desiredImportWorkerCountPctOfLogicalCPUs = EditorGUIUtility.TrTextContent("Import Worker Count %", "Desired asset import worker count for new projects in percentage of available logical CPU cores.");
            public static readonly GUIContent desiredImportWorkerCountPctOfLogicalCPUsLearnMore = new GUIContent("Learn more...", "Go to import worker documentation.");
            public static readonly GUIContent directoryMonitoring = EditorGUIUtility.TrTextContent("Directory Monitoring", "Monitor directories instead of scanning all project files to detect asset changes.");
            public static readonly GUIContent compressAssetsOnImport = EditorGUIUtility.TrTextContent("Compress Textures on Import", "Disable to skip texture compression during import process (textures will be imported into uncompressed formats, and compressed when making a build).");
            public static readonly GUIContent verifySavingAssets = EditorGUIUtility.TrTextContent("Verify Saving Assets", "Show confirmation dialog whenever Unity saves any assets.");
            public static readonly GUIContent enterSafeModeDialog = EditorGUIUtility.TrTextContent("Show Enter Safe Mode Dialog", "Show confirmation dialog when Unity would enter Safe Mode due to script compilation errors.");

            public static readonly GUIContent cacheServer = new GUIContent("Unity Accelerator (Cache Server)");
            public static readonly GUIContent cacheServerDefaultMode = new GUIContent("Default Mode", "Specifies if Accelerator should be enabled or disabled by default. This can be overridden per project in editor settings.");
            public static readonly GUIContent cacheServerIPLabel = new GUIContent("Default IP address", "This IP address is used for the Accelerator if not overridden in the editor settings per project.");
            public static readonly GUIContent cacheServerLearnMore = new GUIContent("Learn more...", "Open Unity Accelerator documentation.");
        }

        AssetPipelineAutoRefreshMode m_AutoRefresh;
        bool m_DirectoryMonitoring;
        bool m_CompressAssetsOnImport;
        bool m_VerifySavingAssets;
        bool m_EnterSafeModeDialog;
        float m_DesiredImportWorkerCountPctOfLogicalCPUs;

        const string kDesiredImportWorkerCountPctOfLogicalCPUsKey = "DesiredImportWorkerCountPctOfLogicalCPUs";
        const float kDefaultDesiredImportWorkerCountPctOfLogicalCPUs = 0.25f;

        const string kCacheServerIPAddressKey = "CacheServer2IPAddress";
        const string kCacheServerModeKey = "CacheServer2Mode";

        const string kModeKey = "CacheServerMode";
        const string kDeprecatedEnabledKey = "CacheServerEnabled";

        static bool s_CacheServerPrefsLoaded;
        static bool s_HasPendingChanges;
        enum ConnectionState { Unknown, Success, Failure }

        static ConnectionState s_ConnectionState;
        static string s_CacheServer2IPAddress;

        enum CacheServer2Mode { Enabled, Disabled }
        static CacheServer2Mode s_CacheServer2Mode;

        public enum CacheServerMode { Local, Remote, Disabled }
        static CacheServerMode s_CacheServerMode;
        static bool s_EnableCustomPath;
        static string s_CachePath;

        public static bool IsCacheServerEnabled
        {
            get
            {
                ReadCacheServerPreferences();
                return s_CacheServer2Mode == CacheServer2Mode.Enabled;
            }
        }

        public static string CacheServerAddress
        {
            get
            {
                ReadCacheServerPreferences();
                return s_CacheServer2IPAddress;
            }
        }

        private static float GetDesiredImportWorkerCountPctOfLogicalCPUs()
        {
            return EditorPrefs.GetFloat(kDesiredImportWorkerCountPctOfLogicalCPUsKey, kDefaultDesiredImportWorkerCountPctOfLogicalCPUs);
        }

        public AssetPipelineAutoRefreshMode AutoRefreshModeEditorPref
        {
            get
            {
                var legacyAutoRefreshMode = EditorPrefs.GetBool("kAutoRefresh") ? AssetPipelineAutoRefreshMode.Enabled : AssetPipelineAutoRefreshMode.Disabled;
                return (AssetPipelineAutoRefreshMode)EditorPrefs.GetInt("kAutoRefreshMode", (int)legacyAutoRefreshMode);
            }
            set
            {
                EditorPrefs.SetInt("kAutoRefreshMode", (int)value);
            }
        }

        void ReadAssetImportPreferences()
        {
            m_AutoRefresh = AutoRefreshModeEditorPref;
            m_DirectoryMonitoring = EditorPrefs.GetBool("DirectoryMonitoring", true);
            m_VerifySavingAssets = EditorPrefs.GetBool("VerifySavingAssets", false);
            m_CompressAssetsOnImport = Unsupported.GetApplicationSettingCompressAssetsOnImport();
            m_EnterSafeModeDialog = EditorPrefs.GetBool("EnterSafeModeDialog", true);
            m_DesiredImportWorkerCountPctOfLogicalCPUs = GetDesiredImportWorkerCountPctOfLogicalCPUs();
        }

        void WriteAssetImportPreferences()
        {
            AutoRefreshModeEditorPref = m_AutoRefresh;
            bool doRefreshSettings = false;

            bool oldDirectoryMonitoring = EditorPrefs.GetBool("DirectoryMonitoring", true);
            if (oldDirectoryMonitoring != m_DirectoryMonitoring)
            {
                EditorPrefs.SetBool("DirectoryMonitoring", m_DirectoryMonitoring);
                doRefreshSettings = true;
            }

            float oldDesiredImportWorkerCountPctOfLogicalCPUs = EditorPrefs.GetFloat(kDesiredImportWorkerCountPctOfLogicalCPUsKey, kDefaultDesiredImportWorkerCountPctOfLogicalCPUs);
            if (oldDesiredImportWorkerCountPctOfLogicalCPUs != m_DesiredImportWorkerCountPctOfLogicalCPUs)
            {
                EditorPrefs.SetFloat(kDesiredImportWorkerCountPctOfLogicalCPUsKey, m_DesiredImportWorkerCountPctOfLogicalCPUs);
                doRefreshSettings = true;
            }

            EditorPrefs.SetBool("VerifySavingAssets", m_VerifySavingAssets);
            EditorPrefs.SetBool("EnterSafeModeDialog", m_EnterSafeModeDialog);

            if (doRefreshSettings)
                AssetDatabase.RefreshSettings();
        }

        static void ReadCacheServerPreferences()
        {
            if (s_CacheServerPrefsLoaded)
                return;
            s_CacheServer2IPAddress = EditorPrefs.GetString(kCacheServerIPAddressKey, s_CacheServer2IPAddress);
            s_CacheServer2Mode = (CacheServer2Mode)EditorPrefs.GetInt(kCacheServerModeKey, (int)CacheServer2Mode.Disabled);
            s_CacheServerMode = (CacheServerMode)EditorPrefs.GetInt(kModeKey, (int)(EditorPrefs.GetBool(kDeprecatedEnabledKey) ? CacheServerMode.Remote : CacheServerMode.Disabled));
            s_CachePath = EditorPrefs.GetString(LocalCacheServer.PathKey);
            s_EnableCustomPath = EditorPrefs.GetBool(LocalCacheServer.CustomPathKey);
            s_CacheServerPrefsLoaded = true;
        }

        static void WriteCacheServerPreferences()
        {
            CacheServerMode oldMode = (CacheServerMode)EditorPrefs.GetInt(kModeKey);
            var oldPath = EditorPrefs.GetString(LocalCacheServer.PathKey);
            var oldCustomPath = EditorPrefs.GetBool(LocalCacheServer.CustomPathKey);
            bool changedDir = false;
            if (oldMode != s_CacheServerMode && oldMode == CacheServerMode.Local)
                changedDir = true;
            if (s_EnableCustomPath && oldPath != s_CachePath)
                changedDir = true;
            if (s_EnableCustomPath != oldCustomPath && s_CachePath != LocalCacheServer.GetCacheLocation() && s_CachePath != "")
                changedDir = true;
            if (changedDir)
            {
                var message = s_CacheServerMode == CacheServerMode.Local ?
                    "You have changed the location of the local cache storage." :
                    "You have disabled the local cache.";
                message += " Do you want to delete the old locally cached data at " + LocalCacheServer.GetCacheLocation() + "?";
                if (EditorUtility.DisplayDialog("Delete old Cache", message, "Delete", "Don't Delete"))
                {
                    LocalCacheServer.Clear();
                }
            }

            EditorPrefs.SetString(kCacheServerIPAddressKey, s_CacheServer2IPAddress);
            EditorPrefs.SetInt(kCacheServerModeKey, (int)s_CacheServer2Mode);
            EditorPrefs.SetInt(kModeKey, (int)s_CacheServerMode);
            EditorPrefs.SetString(LocalCacheServer.PathKey, s_CachePath);
            EditorPrefs.SetBool(LocalCacheServer.CustomPathKey, s_EnableCustomPath);
            LocalCacheServer.Setup();

            AssetDatabase.RefreshSettings();

            if (changedDir)
            {
                //Call ExitGUI after bringing up a dialog to avoid an exception
                EditorGUIUtility.ExitGUI();
            }
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            ReadAssetImportPreferences();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            var p = new AssetPipelinePreferences("Preferences/Asset Pipeline", GetSearchKeywordsFromGUIContentProperties<Properties>());
            p.guiHandler = searchContext =>
            {
                using (new SettingsWindow.GUIScope())
                    p.ShowGUI();
            };
            return p;
        }

        void ShowGUI()
        {
            EditorGUIUtility.labelWidth = 200f;
            AssetImportGUI();

            if (!s_CacheServerPrefsLoaded)
            {
                ReadCacheServerPreferences();
                bool shouldTryConnect = s_ConnectionState == ConnectionState.Unknown &&
                    s_CacheServer2Mode != CacheServer2Mode.Disabled;
                if (shouldTryConnect)
                {
                    var isConnected = AssetDatabase.IsConnectedToCacheServer();
                    s_ConnectionState = isConnected ? ConnectionState.Success : ConnectionState.Failure;
                }
            }
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Properties.cacheServer, EditorStyles.boldLabel);
                if (GUILayout.Button(Properties.cacheServerLearnMore, EditorStyles.linkLabel))
                {
                    // Known issue with Docs redirect - versioned pages might not open offline docs
                    var help = Help.FindHelpNamed("UnityAccelerator");
                    Help.BrowseURL(help);
                }
                GUILayout.EndHorizontal();
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space();
                CacheServerGUI();

                if (EditorGUI.EndChangeCheck())
                    s_HasPendingChanges = true;

                // Only commit changes when we don't have an active hot control, to avoid restarting the cache server all the time while the slider is dragged, slowing down the UI.
                if (s_HasPendingChanges && GUIUtility.hotControl == 0)
                {
                    s_HasPendingChanges = false;
                    WriteCacheServerPreferences();
                    ReadCacheServerPreferences();
                }
            }

        }

        void AssetImportGUI()
        {
            EditorGUI.BeginChangeCheck();

            DoAutoRefreshMode();
            DoImportWorkerCount();
            DoDirectoryMonitoring();

            bool oldCompressOnImport = m_CompressAssetsOnImport;
            m_CompressAssetsOnImport = EditorGUILayout.Toggle(Properties.compressAssetsOnImport, oldCompressOnImport);
            m_VerifySavingAssets = EditorGUILayout.Toggle(Properties.verifySavingAssets, m_VerifySavingAssets);
            m_EnterSafeModeDialog = EditorGUILayout.Toggle(Properties.enterSafeModeDialog, m_EnterSafeModeDialog);

            if (EditorGUI.EndChangeCheck())
            {
                if (GUI.changed && m_CompressAssetsOnImport != oldCompressOnImport)
                    Unsupported.SetApplicationSettingCompressAssetsOnImport(m_CompressAssetsOnImport);
                WriteAssetImportPreferences();
                ReadAssetImportPreferences();
            }
        }

        void DoAutoRefreshMode()
        {
            m_AutoRefresh = (AssetPipelineAutoRefreshMode)EditorGUILayout.EnumPopup(Properties.autoRefresh, m_AutoRefresh);
        }

        void DoImportWorkerCount()
        {
            GUILayout.BeginHorizontal();

            var val = EditorGUILayout.FloatField(Properties.desiredImportWorkerCountPctOfLogicalCPUs, m_DesiredImportWorkerCountPctOfLogicalCPUs * 100.0f);
            m_DesiredImportWorkerCountPctOfLogicalCPUs = Mathf.Clamp(val / 100f, 0f, 1f);

            if (GUILayout.Button(Properties.desiredImportWorkerCountPctOfLogicalCPUsLearnMore, EditorStyles.linkLabel))
            {
                // Known issue with Docs redirect - versioned pages might not open offline docs
                var help = Help.FindHelpNamed("ParallelImport");
                Help.BrowseURL(help);
            }

            GUILayout.EndHorizontal();
        }

        void DoDirectoryMonitoring()
        {
            bool isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            using (new EditorGUI.DisabledScope(!isWindows))
            {
                m_DirectoryMonitoring = EditorGUILayout.Toggle(Properties.directoryMonitoring, m_DirectoryMonitoring);
                if (!isWindows)
                    EditorGUILayout.HelpBox("Directory monitoring currently only available on windows", MessageType.Info, true);
            }
        }

        internal static bool ParseCacheServerAddress(string input, out string ip, out UInt16 port)
        {
            var address = input.Split(':');
            ip = address[0];
            port = 0;
            if (address.Length == 2)
            {
                try
                {
                    port = UInt16.Parse(address[1]);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.Message} Exception thrown attempting to parse the port '{address[1]}'. Please double check the 'Default IP Address' and try again.");
                    return false;
                }
            }
            else if (address.Length > 2)
            {
                Debug.LogError($"Failure attempting to parse the address '{input}' as multiple ports were detected. Please double check the 'Default IP Address' and try again.");
                return false;
            }
            return true;
        }

        static void CacheServerGUI()
        {
            bool changeStateBeforeControls = GUI.changed;

            s_CacheServer2Mode = (CacheServer2Mode)EditorGUILayout.EnumPopup(Properties.cacheServerDefaultMode, s_CacheServer2Mode);

            s_CacheServer2IPAddress = EditorGUILayout.TextField(Properties.cacheServerIPLabel, s_CacheServer2IPAddress);

            if (GUI.changed != changeStateBeforeControls)
            {
                s_ConnectionState = ConnectionState.Unknown;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Check Connection", GUILayout.Width(150)))
            {
                if (ParseCacheServerAddress(s_CacheServer2IPAddress, out var ip, out var port) && AssetDatabase.CanConnectToCacheServer(ip, port))
                    s_ConnectionState = ConnectionState.Success;
                else
                    s_ConnectionState = ConnectionState.Failure;

            }

            GUILayout.Space(-25);

            switch (s_ConnectionState)
            {
                case ConnectionState.Success:
                    EditorGUILayout.HelpBox("Connection successful.", MessageType.Info, false);
                    break;

                case ConnectionState.Failure:
                    EditorGUILayout.HelpBox("Connection failed.", MessageType.Warning, false);
                    break;

                case ConnectionState.Unknown:
                    GUILayout.Space(44);
                    break;
            }
        }

        public AssetPipelinePreferences(string path, IEnumerable<string> keywords = null) : base(path, keywords)
        {
        }
    }
}
