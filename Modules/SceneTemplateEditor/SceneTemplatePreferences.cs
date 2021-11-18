// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    class SceneTemplatePreferences
    {
        const string k_PreferencesPath = "Preferences/SceneTemplates";
        const string k_PreferencesKeyPrefix = "SceneTemplatePreferences.";

        internal enum NewDefaultSceneOverride
        {
            SceneTemplate,
            DefaultBuiltin
        }

        public NewDefaultSceneOverride newDefaultSceneOverride;

        static SceneTemplatePreferences s_Instance;

        [UsedImplicitly, SettingsProvider]
        static SettingsProvider CreateSettings()
        {
            return new SettingsProvider(k_PreferencesPath, SettingsScope.User)
            {
                keywords = L10n.Tr(new[] { "unity", "editor", "scene", "clone", "template" }),
                activateHandler = (text, rootElement) =>
                {

                },
                label = L10n.Tr("Scene Template"),
                guiHandler = OnGUIHandler
            };
        }

        static void OnGUIHandler(string obj)
        {
            var prefs = Get();
            using (new SettingsWindow.GUIScope())
            {
                EditorGUI.BeginChangeCheck();
                prefs.newDefaultSceneOverride = (NewDefaultSceneOverride)EditorGUILayout.EnumPopup(L10n.TextContent("Default Scene", "Which scene to open when no other scenes were previously opened."), prefs.newDefaultSceneOverride);
                if (EditorGUI.EndChangeCheck())
                {
                    Save();
                }
            }
        }

        public static SceneTemplatePreferences Get()
        {
            if (s_Instance == null)
            {
                s_Instance = new SceneTemplatePreferences();
                s_Instance.newDefaultSceneOverride = (NewDefaultSceneOverride)EditorPrefs.GetInt(GetPreferencesKey("newDefaultSceneOverride"), (int)NewDefaultSceneOverride.DefaultBuiltin);
            }

            return s_Instance;
        }

        public static void Save(SceneTemplatePreferences prefs = null)
        {
            prefs ??= Get();

            EditorPrefs.SetInt(GetPreferencesKey("newDefaultSceneOverride"), (int)prefs.newDefaultSceneOverride);
        }

        internal static void ResetDefaults()
        {
            var prefs = Get();
            prefs.newDefaultSceneOverride = NewDefaultSceneOverride.DefaultBuiltin;
            Save();
        }

        static string GetPreferencesKey(string name)
        {
            return $"{k_PreferencesKeyPrefix}{name}";
        }
    }

}
