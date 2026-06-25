// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    // Register a SettingsProvider using IMGUI for the drawing framework:
    static partial class ProjectAuditorSettingsIMGUIRegister
    {
        [AutoStaticsCleanupOnCodeReload]
        private static BuildTargetGroup s_LastBuildTargetGroup = BuildTargetGroup.Unknown;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/ProjectAuditor", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = ProjectAuditor.DisplayName,
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = SettingsGUI,

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Rules", "Diagnostic Parameters" })
            };

            return provider;
        }

        static void SettingsGUI(string searchContext)
        {
            var settings = ProjectAuditorSettings.instance.GetSerializedObject();

            EditorGUI.BeginChangeCheck();

            BuildTargetGroup btg = EditorGUILayout.BeginBuildTargetSelectionGrouping();

            // Remove focus from everything if we swap tab, otherwise the previously selected IntField value can
            // (visually) overwrite its equivalent in the new tab.
            if (btg != s_LastBuildTargetGroup)
                GUIUtility.keyboardControl = 0;
            s_LastBuildTargetGroup = btg;

            ProjectAuditorSettings.instance.DiagnosticParams.DoGUI(btg);

            EditorGUILayout.EndBuildTargetSelectionGrouping();

            if (EditorGUI.EndChangeCheck())
            {
                settings.ApplyModifiedPropertiesWithoutUndo();
                ProjectAuditorSettings.instance.Save();
            }
        }
    }
}
