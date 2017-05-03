// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Microsoft.Win32;
using UnityEngine;
using UnityEditor;
using UnityEditor.Collaboration;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.IO;

namespace UnityEditor
{
    internal class CacheServerPreferences
    {
        internal class Styles
        {
            public static readonly GUIContent browse = EditorGUIUtility.TextContent("Browse...");
            public static readonly GUIContent maxCacheSize = EditorGUIUtility.TextContent("Maximum Cache Size (GB)|The size of the local asset cache server folder will be kept below this maximum value.");
            public static readonly GUIContent customCacheLocation = EditorGUIUtility.TextContent("Custom cache location|Specify the local asset cache server folder location.");
            public static readonly GUIContent cacheFolderLocation = EditorGUIUtility.TextContent("Cache Folder Location|The local asset cache server folder is shared between all projects.");
            public static readonly GUIContent cleanCache = EditorGUIUtility.TextContent("Clean Cache");
            public static readonly GUIContent enumerateCache = EditorGUIUtility.TextContent("Check Cache Size|Check the size of the local asset cache server - can take a while");
            public static readonly GUIContent browseCacheLocation = EditorGUIUtility.TextContent("Browse for local asset cache server location");
        }

        internal class Constants
        {
            public GUIStyle cacheFolderLocation = new GUIStyle(GUI.skin.label);

            public Constants()
            {
                cacheFolderLocation.wordWrap = true;
            }
        }

        const string kIPAddressKey = "CacheServerIPAddress";
        const string kModeKey = "CacheServerMode";
        const string kDeprecatedEnabledKey = "CacheServerEnabled";

        private static bool s_PrefsLoaded;
        private static bool s_HasPendingChanges = false;
        enum ConnectionState { Unknown, Success, Failure };
        private static ConnectionState s_ConnectionState;
        private static bool s_CollabCacheEnabled;
        private static string s_CollabCacheIPAddress; // TODO default to CacheServerIPAddress when blank
        private static bool s_EnableCollabCacheConfiguration = false; // Since it has not been tested enough, we currently want to disable the cache configuration UI

        private static bool IsCollabCacheEnabled()
        {
            return s_EnableCollabCacheConfiguration || Application.HasARGV("enableCacheServer");
        }

        public enum CacheServerMode { Local, Remote, Disabled }
        private static CacheServerMode s_CacheServerMode;
        private static string s_CacheServerIPAddress;
        private static int s_LocalCacheServerSize;
        private static long s_LocalCacheServerUsedSize = -1;
        private static bool s_EnableCustomPath;
        private static string s_CachePath;

        static Constants s_Constants = null;

        public static void ReadPreferences()
        {
            s_CacheServerIPAddress = EditorPrefs.GetString(kIPAddressKey, s_CacheServerIPAddress);
            s_CacheServerMode = (CacheServerMode)EditorPrefs.GetInt(kModeKey, (int)(EditorPrefs.GetBool(kDeprecatedEnabledKey) ? CacheServerMode.Remote : CacheServerMode.Disabled));
            s_LocalCacheServerSize = EditorPrefs.GetInt(LocalCacheServer.SizeKey, 10);
            s_CachePath = EditorPrefs.GetString(LocalCacheServer.PathKey);
            s_EnableCustomPath = EditorPrefs.GetBool(LocalCacheServer.CustomPathKey);
            if (IsCollabCacheEnabled())
            {
                s_CollabCacheIPAddress = EditorPrefs.GetString("CollabCacheIPAddress", s_CollabCacheIPAddress);
                s_CollabCacheEnabled = EditorPrefs.GetBool("CollabCacheEnabled");
            }
        }

        public static void WritePreferences()
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

            EditorPrefs.SetString(kIPAddressKey, s_CacheServerIPAddress);
            EditorPrefs.SetInt(kModeKey, (int)s_CacheServerMode);
            EditorPrefs.SetInt(LocalCacheServer.SizeKey, s_LocalCacheServerSize);
            EditorPrefs.SetString(LocalCacheServer.PathKey, s_CachePath);
            EditorPrefs.SetBool(LocalCacheServer.CustomPathKey, s_EnableCustomPath);
            if (IsCollabCacheEnabled())
            {
                EditorPrefs.SetString("CollabCacheIPAddress", s_CollabCacheIPAddress);
                EditorPrefs.SetBool("CollabCacheEnabled", s_CollabCacheEnabled);
            }
            LocalCacheServer.Setup();

            if (changedDir)
            {
                //Call ExitGUI after bringing up a dialog to avoid an exception
                EditorGUIUtility.ExitGUI();
            }
        }

        [PreferenceItem("Cache Server")]
        public static void OnGUI()
        {
            // Get event type before the event is used.
            var eventType = Event.current.type;
            if (s_Constants == null)
            {
                s_Constants = new Constants();
            }

            if (!InternalEditorUtility.HasTeamLicense())
                GUILayout.Label(EditorGUIUtility.TempContent("You need to have a Pro or Team license to use the cache server.", EditorGUIUtility.GetHelpIcon(MessageType.Warning)), EditorStyles.helpBox);

            using (new EditorGUI.DisabledScope(!InternalEditorUtility.HasTeamLicense()))
            {
                if (!s_PrefsLoaded)
                {
                    ReadPreferences();

                    if (s_CacheServerMode != CacheServerMode.Disabled && s_ConnectionState == ConnectionState.Unknown)
                    {
                        if (InternalEditorUtility.CanConnectToCacheServer())
                            s_ConnectionState = ConnectionState.Success;
                        else
                            s_ConnectionState = ConnectionState.Failure;
                    }

                    s_PrefsLoaded = true;
                }

                EditorGUI.BeginChangeCheck();
                if (IsCollabCacheEnabled())
                {
                    s_CollabCacheEnabled = EditorGUILayout.Toggle("Use Collab Cache", s_CollabCacheEnabled);
                    using (new EditorGUI.DisabledScope(!s_CollabCacheEnabled))
                    {
                        s_CollabCacheIPAddress = EditorGUILayout.TextField("Collab Cache IP Address", s_CollabCacheIPAddress);
                    }
                }

                s_CacheServerMode = (CacheServerMode)EditorGUILayout.EnumPopup("Cache Server Mode", s_CacheServerMode);

                if (s_CacheServerMode == CacheServerMode.Remote)
                {
                    s_CacheServerIPAddress = EditorGUILayout.DelayedTextField("IP Address", s_CacheServerIPAddress);
                    if (GUI.changed)
                    {
                        s_ConnectionState = ConnectionState.Unknown;
                    }

                    GUILayout.Space(5);
                    if (GUILayout.Button("Check Connection", GUILayout.Width(150)))
                    {
                        if (InternalEditorUtility.CanConnectToCacheServer())
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
                else if (s_CacheServerMode == CacheServerMode.Local)
                {
                    const int kMinSizeInGigabytes = 1;
                    const int kMaxSizeInGigabytes = 200;

                    // Write size in GigaBytes.
                    s_LocalCacheServerSize = EditorGUILayout.IntSlider(Styles.maxCacheSize, s_LocalCacheServerSize, kMinSizeInGigabytes, kMaxSizeInGigabytes);

                    s_EnableCustomPath = EditorGUILayout.Toggle(Styles.customCacheLocation, s_EnableCustomPath);
                    // browse for cache folder if not per project
                    if (s_EnableCustomPath)
                    {
                        GUIStyle style = EditorStyles.miniButton;
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel(Styles.cacheFolderLocation, style);
                        Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
                        GUIContent guiText = string.IsNullOrEmpty(s_CachePath) ? Styles.browse : new GUIContent(s_CachePath);
                        if (EditorGUI.DropdownButton(r, guiText, FocusType.Passive, style))
                        {
                            string pathToOpen = s_CachePath;
                            string path = EditorUtility.OpenFolderPanel(Styles.browseCacheLocation.text, pathToOpen, "");
                            if (!string.IsNullOrEmpty(path))
                            {
                                if (LocalCacheServer.CheckValidCacheLocation(path))
                                {
                                    s_CachePath = path;
                                    WritePreferences();
                                }
                                else
                                    EditorUtility.DisplayDialog("Invalid Cache Location", "The directory " + path + " contains some files which don't look like Unity Cache server files. Please delete the directory contents or choose another directory.", "OK");
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                        s_CachePath = "";

                    bool locationExists = LocalCacheServer.CheckCacheLocationExists();
                    if (locationExists == true)
                    {
                        GUIContent cacheSizeIs = EditorGUIUtility.TextContent("Cache size is unknown");
                        if (s_LocalCacheServerUsedSize != -1)
                        {
                            cacheSizeIs = EditorGUIUtility.TextContent("Cache size is " + EditorUtility.FormatBytes(s_LocalCacheServerUsedSize));
                        }

                        GUILayout.BeginHorizontal();
                        GUIStyle style = EditorStyles.miniButton;
                        EditorGUILayout.PrefixLabel(cacheSizeIs, style);
                        Rect r = GUILayoutUtility.GetRect(GUIContent.none, style);
                        if (EditorGUI.Button(r, Styles.enumerateCache, style))
                        {
                            s_LocalCacheServerUsedSize = LocalCacheServer.CheckCacheLocationExists() ? FileUtil.GetDirectorySize(LocalCacheServer.GetCacheLocation()) : 0;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUIContent spacerContent = EditorGUIUtility.blankContent;
                        EditorGUILayout.PrefixLabel(spacerContent, style);
                        Rect r2 = GUILayoutUtility.GetRect(GUIContent.none, style);
                        if (EditorGUI.Button(r2, Styles.cleanCache, style))
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

                    GUILayout.Label(Styles.cacheFolderLocation.text + ":");
                    GUILayout.Label(LocalCacheServer.GetCacheLocation(), s_Constants.cacheFolderLocation);
                }

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
    }
}
