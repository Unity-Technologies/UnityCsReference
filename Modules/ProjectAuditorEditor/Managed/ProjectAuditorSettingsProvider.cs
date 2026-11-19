// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
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

        static class Styles
        {
            public static readonly GUIContent SuppressedDiagnostics = L10n.TextContent("Suppressed Issues", "A comma-, semicolon- or space-delimited list of issue IDs to exclude from analysis. Use the search button to add/browse IDs from the list of known issues.", null, null);
            public static readonly GUIContent Manage = L10n.TextContent("Manage", "Open in Search", null, null);
            public static readonly GUIContent ManageDisabled = L10n.TextContent("Manage", "Open the Project Auditor window to enable browsing.", null, null);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider(ProjectAuditor.k_ProjectSettingsProviderPath,SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = ProjectAuditor.DisplayName,
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = SettingsGUI,

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Rules", "Diagnostic Parameters", "Suppressed Issues" })
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

            GUILayout.Space(10f);
            SuppressedDiagnosticsGUI();

            if (EditorGUI.EndChangeCheck())
            {
                settings.ApplyModifiedPropertiesWithoutUndo();
                ProjectAuditorSettings.instance.Save();
            }
        }

        static void SuppressedDiagnosticsGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // A unique control name plus DelayedTextField prevents IMGUI from sharing this field's recycled text
                // editor with the SettingsWindow search box (which otherwise copies this field's value into itself).
                GUI.SetNextControlName("ProjectAuditor.SuppressedDiagnostics");

                RoslynDiagnosticsLibrary.EnsureLoaded();
                var hasLoadedDescriptors = (DescriptorLibrary.GetAllDescriptors().Count > 0);
                UserPreferences.SetSuppressedDiagnostics(EditorGUILayout.DelayedTextField(Styles.SuppressedDiagnostics, UserPreferences.SuppressedDiagnostics, GUILayout.ExpandWidth(true)));
                using (new EditorGUI.DisabledScope(!hasLoadedDescriptors))
                {
                    var content = hasLoadedDescriptors ? Styles.Manage : Styles.ManageDisabled;
                    if (GUILayout.Button(content, GUILayout.ExpandWidth(false)))
                        Utility.SearchWindow(DescriptorSearchProvider.kProviderId, "Project Auditor Issue Types");
                }
            }
        }
    }
}
