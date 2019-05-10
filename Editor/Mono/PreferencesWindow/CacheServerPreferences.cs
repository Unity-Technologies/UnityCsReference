// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Microsoft.Win32;
using UnityEngine;
using UnityEditor.Collaboration;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.IO;

namespace UnityEditor
{
    internal class CacheServerPreferences
    {
        internal class Properties
        {
            public static readonly GUIContent browse = EditorGUIUtility.TrTextContent("Browse...");
            public static readonly GUIContent maxCacheSize = EditorGUIUtility.TrTextContent("Maximum Cache Size (GB)", "The size of the local asset cache server folder will be kept below this maximum value.");
            public static readonly GUIContent customCacheLocation = EditorGUIUtility.TrTextContent("Custom cache location", "Specify the local asset cache server folder location.");
            public static readonly GUIContent cacheFolderLocation = EditorGUIUtility.TrTextContent("Cache Folder Location", "The local asset cache server folder is shared between all projects.");
            public static readonly GUIContent cleanCache = EditorGUIUtility.TrTextContent("Clean Cache");
            public static readonly GUIContent enumerateCache = EditorGUIUtility.TrTextContent("Check Cache Size", "Check the size of the local asset cache server - can take a while");
            public static readonly GUIContent browseCacheLocation = EditorGUIUtility.TrTextContent("Browse for local asset cache server location");
            public static readonly GUIContent assetPipelineVersion1 = EditorGUIUtility.TrTextContent("Asset pipeline v1");
            public static readonly GUIContent assetPipelineVersion2 = EditorGUIUtility.TrTextContent("Asset pipeline v2 (experimental)");
            public static readonly GUIContent newProjectsAssetPipeline = EditorGUIUtility.TrTextContent("New Projects default asset pipeline", "The default asset pipeline used when creating a new project. Note that this default can be overridden by using a -adb1 or -adb2 command line argument.");
            public static readonly GUIContent activeAssetPipelineVersionLabel = EditorGUIUtility.TrTextContent("Active version", activeAssetPipelineVersionTooltip);
            public static readonly GUIContent activeAssetPipelineVersion = new GUIContent(AssetDatabase.IsV1Enabled() ? "1" : "2", activeAssetPipelineVersionTooltip);
            private const string activeAssetPipelineVersionTooltip = "The active asset import pipeline is chosen at startup by inspecting the following sources by priority: Environment variable, command line argument (-adb1 or -adb2), local per project editor settings, builtin default. In the case of creating a new project, the default pipeline is selected by the dropdown above and will have a priority one higher than the builtin default.";
            public static readonly GUIContent cacheServerDefaultMode = new GUIContent("Cache Server Default Mode", "Specifies if cache server should be enabled or disabled by default. This can be overridden per project in editor settings.");
            public static readonly GUIContent cacheServerIPLabel = new GUIContent("Default IP address", "This IP address is used for the cache server if not overridden in the editor settings per project.");
        }

        internal static class Styles
        {
            public static GUIStyle cacheFolderLocation = new GUIStyle(GUI.skin.label);
            static Styles()
            {
                cacheFolderLocation.wordWrap = true;
            }
        }

        const string kAssetPipelineVersionForNewProjects = "AssetPipelineVersionForNewProjects";
        const string kIPAddress2Key = "CacheServer2IPAddress";
        const string kMode2Key = "CacheServer2Mode";

        const string kIPAddressKey = "CacheServerIPAddress";
        const string kIpAddressKeyArgs = "-" + kIPAddressKey;
        const string kModeKey = "CacheServerMode";
        const string kDeprecatedEnabledKey = "CacheServerEnabled";

        private static bool s_PrefsLoaded;
        private static bool s_HasPendingChanges = false;
        enum ConnectionState { Unknown, Success, Failure };
        private static ConnectionState s_ConnectionState;
        public enum AssetPipelineVersion { Version1, Version2 }
        public static AssetPipelineVersion s_AssetPipelineVersionForNewProjects;
        private static string s_CacheServer2IPAddress;
        public enum CacheServer2Mode { Enabled, Disabled }
        private static CacheServer2Mode s_CacheServer2Mode;

        public enum CacheServerMode { Local, Remote, Disabled }
        private static CacheServerMode s_CacheServerMode;
        private static string s_CacheServerIPAddress;
        private static int s_LocalCacheServerSize;
        private static long s_LocalCacheServerUsedSize = -1;
        private static bool s_EnableCustomPath;
        private static string s_CachePath;

        public static bool IsCacheServerV2Enabled
        {
            get
            {
                EnsurePreferencesRead();
                return AssetDatabase.IsV2Enabled() && s_CacheServer2Mode == CacheServer2Mode.Enabled;
            }
        }

        public static string CachesServerV2Address
        {
            get
            {
                EnsurePreferencesRead();
                return s_CacheServer2IPAddress;
            }
        }

        public static void ReadPreferences()
        {
            s_AssetPipelineVersionForNewProjects = (AssetPipelineVersion)EditorPrefs.GetInt(kAssetPipelineVersionForNewProjects, (int)AssetPipelineVersion.Version2);
            s_CacheServer2IPAddress = EditorPrefs.GetString(kIPAddress2Key, s_CacheServer2IPAddress);
            s_CacheServer2Mode = (CacheServer2Mode)EditorPrefs.GetInt(kMode2Key, (int)CacheServer2Mode.Disabled);
            s_CacheServerIPAddress = EditorPrefs.GetString(kIPAddressKey, s_CacheServerIPAddress);
            s_CacheServerMode = (CacheServerMode)EditorPrefs.GetInt(kModeKey, (int)(EditorPrefs.GetBool(kDeprecatedEnabledKey) ? CacheServerMode.Remote : CacheServerMode.Disabled));
            s_LocalCacheServerSize = EditorPrefs.GetInt(LocalCacheServer.SizeKey, 10);
            s_CachePath = EditorPrefs.GetString(LocalCacheServer.PathKey);
            s_EnableCustomPath = EditorPrefs.GetBool(LocalCacheServer.CustomPathKey);
        }

        public static void WritePreferences()
        {
            // Don't change anything if there's a command line override
            if (GetCommandLineRemoteAddressOverride() != null)
                return;

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
                s_LocalCacheServerUsedSize = -1;
                var message = s_CacheServerMode == CacheServerMode.Local ?
                    "You have changed the location of the local cache storage." :
                    "You have disabled the local cache.";
                message += " Do you want to delete the old locally cached data at " + LocalCacheServer.GetCacheLocation() + "?";
                if (EditorUtility.DisplayDialog("Delete old Cache", message, "Delete", "Don't Delete"))
                {
                    LocalCacheServer.Clear();
                    s_LocalCacheServerUsedSize = -1;
                }
            }

            EditorPrefs.SetInt(kAssetPipelineVersionForNewProjects, (int)s_AssetPipelineVersionForNewProjects);
            EditorPrefs.SetString(kIPAddress2Key, s_CacheServer2IPAddress);
            EditorPrefs.SetInt(kMode2Key, (int)s_CacheServer2Mode);
            EditorPrefs.SetString(kIPAddressKey, s_CacheServerIPAddress);
            EditorPrefs.SetInt(kModeKey, (int)s_CacheServerMode);
            EditorPrefs.SetInt(LocalCacheServer.SizeKey, s_LocalCacheServerSize);
            EditorPrefs.SetString(LocalCacheServer.PathKey, s_CachePath);
            EditorPrefs.SetBool(LocalCacheServer.CustomPathKey, s_EnableCustomPath);
            LocalCacheServer.Setup();

            if (changedDir)
            {
                //Call ExitGUI after bringing up a dialog to avoid an exception
                EditorGUIUtility.ExitGUI();
            }
        }

        public static string GetCommandLineRemoteAddressOverride()
        {
            string address = null;
            var argv = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(argv, kIpAddressKeyArgs);
            if (index >= 0 && argv.Length > index + 1)
                address = argv[index + 1];

            return address;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/Cache Server", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    using (new SettingsWindow.GUIScope())
                        OnGUI(searchContext);
                }
            };
        }

        private static void EnsurePreferencesRead()
        {
            if (!s_PrefsLoaded)
            {
                ReadPreferences();
                s_PrefsLoaded = true;
            }
        }

        public static int GetCommandLineAssetPipelineOverride()
        {
            var argv = System.Environment.GetCommandLineArgs();
            var index = System.Array.IndexOf(argv, "-adb1");
            if (index >= 0 && argv.Length > index + 1)
                return 1;

            index = System.Array.IndexOf(argv, "-adb2");
            if (index >= 0 && argv.Length > index + 1)
                return 2;

            return 0;
        }

        public static bool GetMagicFileAssetPipelineOverride()
        {
            return System.IO.File.Exists("adb2.txt");
        }

        public static bool GetEnvironmentAssetPipelineOverride()
        {
            return !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("UNITY_ASSETS_V2_KATANA_TESTS"));
        }

        private static void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 200f;
            // Get event type before the event is used.
            var eventType = Event.current.type;
            if (!InternalEditorUtility.HasTeamLicense())
                GUILayout.Label(EditorGUIUtility.TempContent("You need to have a Pro or Team license to use the cache server.", EditorGUIUtility.GetHelpIcon(MessageType.Warning)), EditorStyles.helpBox);


            using (new EditorGUI.DisabledScope(!InternalEditorUtility.HasTeamLicense()))
            {
                if (!s_PrefsLoaded)
                {
                    EnsurePreferencesRead();
                    OnPreferencesReadGUI();
                }

                EditorGUI.BeginChangeCheck();

                s_AssetPipelineVersionForNewProjects = (AssetPipelineVersion)EditorGUILayout.EnumPopup(Properties.newProjectsAssetPipeline, s_AssetPipelineVersionForNewProjects);

                EditorGUILayout.LabelField(Properties.activeAssetPipelineVersionLabel, Properties.activeAssetPipelineVersion);
                var overrideAddress = GetCommandLineRemoteAddressOverride();
                bool allowCacheServerChanges = overrideAddress == null;

                if (GetEnvironmentAssetPipelineOverride())
                    EditorGUILayout.HelpBox("Asset pipeline currently forced environment variable UNITY_ASSETS_V2_KATANA_TESTS", MessageType.Info, true);
                else if (GetCommandLineAssetPipelineOverride() != 0)
                    EditorGUILayout.HelpBox("Asset pipeline currently forced by command line argument", MessageType.Info, true);
                else if (GetMagicFileAssetPipelineOverride())
                    EditorGUILayout.HelpBox("Asset pipeline currently forced by magic adb2.txt file", MessageType.Info, true);

                GUILayout.Space(5);

                CacheServerVersion1GUI(allowCacheServerChanges, overrideAddress);

                GUILayout.Space(5);

                CacheServerVersion2GUI(allowCacheServerChanges, overrideAddress);
                GUILayout.Space(10);

                if (!allowCacheServerChanges)
                    EditorGUILayout.HelpBox("Cache Server preferences currently forced via command line argument to " + overrideAddress + " and any changes here will not take effect until starting Unity without that command line argument.", MessageType.Info, true);

                if (EditorGUI.EndChangeCheck())
                    s_HasPendingChanges = true;

                // Only commit changes when we don't have an active hot control, to avoid restarting the cache server all the time while the slider is dragged, slowing down the UI.
                if (s_HasPendingChanges && GUIUtility.hotControl == 0)
                {
                    s_HasPendingChanges = false;
                    WritePreferences();
                    ReadPreferences();
                }
            }

        }

        static void CacheServerVersion1GUI(bool allowCacheServerChanges, string overrideAddress)
        {
            GUILayout.Label(Properties.assetPipelineVersion1, EditorStyles.boldLabel);

            GUILayout.Space(5);

            if (!allowCacheServerChanges)
                EditorGUILayout.HelpBox("Cache Server preferences cannot be modified because a remote address was specified via command line argument. To modify Cache Server preferences, restart Unity without the " + kIpAddressKeyArgs + " command line argument.", MessageType.Info, true);

            using (new EditorGUI.DisabledScope(!allowCacheServerChanges))
            {
                var displayMode = !allowCacheServerChanges ? CacheServerMode.Remote : s_CacheServerMode;
                s_CacheServerMode = (CacheServerMode)EditorGUILayout.EnumPopup("Cache Server Mode", displayMode);
            }

            if (s_CacheServerMode == CacheServerMode.Remote)
            {
                using (new EditorGUI.DisabledScope(!allowCacheServerChanges))
                {
                    var displayAddress = overrideAddress != null ? overrideAddress : s_CacheServerIPAddress;
                    s_CacheServerIPAddress = EditorGUILayout.DelayedTextField("IP Address", displayAddress);

                    if (GUI.changed)
                    {
                        s_ConnectionState = ConnectionState.Unknown;
                    }
                }

                GUILayout.Space(5);

                using (new EditorGUI.DisabledScope(AssetDatabase.IsV2Enabled()))
                {
                    if (GUILayout.Button("Check Connection", GUILayout.Width(150)))
                    {
                        if (InternalEditorUtility.CanConnectToCacheServer())
                            s_ConnectionState = ConnectionState.Success;
                        else
                            s_ConnectionState = ConnectionState.Failure;
                    }
                }

                GUILayout.Space(-25);

                var s = AssetDatabase.IsV1Enabled() ? s_ConnectionState : ConnectionState.Unknown;

                switch (s)
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
            else if (s_CacheServerMode == CacheServerMode.Local)
            {
                const int kMinSizeInGigabytes = 1;
                const int kMaxSizeInGigabytes = 200;

                // Write size in GigaBytes.
                s_LocalCacheServerSize = EditorGUILayout.IntSlider(Properties.maxCacheSize, s_LocalCacheServerSize, kMinSizeInGigabytes, kMaxSizeInGigabytes);

                s_EnableCustomPath = EditorGUILayout.Toggle(Properties.customCacheLocation, s_EnableCustomPath);
                // browse for cache folder if not per project
                if (s_EnableCustomPath)
                {
                    GUIStyle style = EditorStyles.miniButton;
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(Properties.cacheFolderLocation, style);
                    Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
                    GUIContent guiText = string.IsNullOrEmpty(s_CachePath) ? Properties.browse : new GUIContent(s_CachePath);
                    if (EditorGUI.DropdownButton(r, guiText, FocusType.Passive, style))
                    {
                        string pathToOpen = s_CachePath;
                        string path = EditorUtility.OpenFolderPanel(Properties.browseCacheLocation.text, pathToOpen, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (LocalCacheServer.CheckValidCacheLocation(path))
                            {
                                s_CachePath = path;
                                WritePreferences();
                            }
                            else
                                EditorUtility.DisplayDialog("Invalid Cache Location", string.Format("The directory {0} contains some files which don't look like Unity Cache server files. Please delete the directory contents or choose another directory.", path), "OK");
                            EditorGUIUtility.ExitGUI();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                    s_CachePath = "";

                bool locationExists = LocalCacheServer.CheckCacheLocationExists();
                if (locationExists == true)
                {
                    GUIContent cacheSizeIs = EditorGUIUtility.TrTextContent("Cache size is unknown");
                    if (s_LocalCacheServerUsedSize != -1)
                    {
                        cacheSizeIs = EditorGUIUtility.TextContent("Cache size is " + EditorUtility.FormatBytes(s_LocalCacheServerUsedSize));
                    }

                    GUILayout.BeginHorizontal();
                    GUIStyle style = EditorStyles.miniButton;
                    EditorGUILayout.PrefixLabel(cacheSizeIs, style);
                    Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
                    if (EditorGUI.Button(r, Properties.enumerateCache, style))
                    {
                        s_LocalCacheServerUsedSize = LocalCacheServer.CheckCacheLocationExists() ? FileUtil.GetDirectorySize(LocalCacheServer.GetCacheLocation()) : 0;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUIContent spacerContent = EditorGUIUtility.blankContent;
                    EditorGUILayout.PrefixLabel(spacerContent, style);
                    Rect r2 = GUILayoutUtility.GetRect(GUIContent.none, style);
                    if (EditorGUI.Button(r2, Properties.cleanCache, style))
                    {
                        LocalCacheServer.Clear();
                        s_LocalCacheServerUsedSize = 0;
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Local cache directory does not exist - please check that you can access the cache folder and are able to write to it", MessageType.Warning, false);
                    //If the cache server was on an external HDD or on a temporarily unavailable network drive, set the size to unknown so that the user re-queries it when they've reconnected
                    s_LocalCacheServerUsedSize = -1;
                }

                GUILayout.Label(Properties.cacheFolderLocation.text + ":");
                GUILayout.Label(LocalCacheServer.GetCacheLocation(), Styles.cacheFolderLocation);
            }
        }

        static void CacheServerVersion2GUI(bool allowCacheServerChanges, string overrideAddress)
        {
            GUILayout.Label(Properties.assetPipelineVersion2, EditorStyles.boldLabel);

            GUILayout.Space(5);

            bool changeStateBeforeControls = GUI.changed;

            s_CacheServer2Mode = (CacheServer2Mode)EditorGUILayout.EnumPopup(Properties.cacheServerDefaultMode, s_CacheServer2Mode);

            s_CacheServer2IPAddress = EditorGUILayout.DelayedTextField(Properties.cacheServerIPLabel, s_CacheServer2IPAddress);

            if (GUI.changed != changeStateBeforeControls)
            {
                s_ConnectionState = ConnectionState.Unknown;
            }

            GUILayout.Space(5);

            using (new EditorGUI.DisabledScope(AssetDatabase.IsV1Enabled()))
            {
                if (GUILayout.Button("Check Connection", GUILayout.Width(150)))
                {
                    if (InternalEditorUtility.CanConnectToCacheServer())
                        s_ConnectionState = ConnectionState.Success;
                    else
                        s_ConnectionState = ConnectionState.Failure;
                }
            }

            GUILayout.Space(-25);

            var s = AssetDatabase.IsV2Enabled() ? s_ConnectionState : ConnectionState.Unknown;

            switch (s)
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

        private static void OnPreferencesReadGUI()
        {
            bool shouldTryConnect = s_ConnectionState == ConnectionState.Unknown &&
                (AssetDatabase.IsV1Enabled() ? s_CacheServerMode != CacheServerMode.Disabled : s_CacheServer2Mode != CacheServer2Mode.Disabled);

            if (shouldTryConnect)
            {
                if (InternalEditorUtility.CanConnectToCacheServer())
                    s_ConnectionState = ConnectionState.Success;
                else
                    s_ConnectionState = ConnectionState.Failure;
            }
        }
    }
}
