// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Build
{
    class BuildPipelineSettingsProvider : SettingsProvider
    {
        static readonly GUIContent k_ResetBuildHistoryFolderText = EditorGUIUtility.TrTextContent("Reset Build History folder Location");
        static readonly GUIContent k_ChangeBuildHistoryFolderLocationText = EditorGUIUtility.TrTextContent("Change Build History Folder Location", "Change the build history path to a new path on your device. Note that this does not move any existing history. ");
        static readonly GUIContent k_BuildHistoryLimitText = EditorGUIUtility.TrTextContent(
            "Build History Limit",
            "Maximum number of builds to retain in Build History. 0 disables automatic deletion. Changes take effect on the next build.");
        private static readonly string k_OpenFolder = L10n.Tr("Open Containing Folder");

        private BuildPipelineSettingsProvider()
            : base("Project/Analysis/Build Pipeline", SettingsScope.Project)
        { }

        public static string buildHistoryFolderPath
        {
            get => BuildHistory.BuildHistoryDirectory;
            set => BuildHistory.BuildHistoryDirectory = value;
        }

        public override void OnGUI(string searchContext)
        {
            using var _ = new SettingsWindow.GUIScope();

            EditorGUIUtility.labelWidth = 300;

            DrawBuildHistoryFolderRow();
            DrawBuildHistoryLimitRow();
        }

        private static void DrawBuildHistoryFolderRow()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Build History Folder Path", GUILayout.MaxWidth(175));
            GUI.enabled = false;
            string folderPathLabel = buildHistoryFolderPath;
            EditorGUILayout.TextField(folderPathLabel);
            GUI.enabled = true;
            if (EditorGUILayout.DropdownButton(new GUIContent("Change Build History Path"), FocusType.Passive, GUILayout.MaxWidth(175)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(k_ChangeBuildHistoryFolderLocationText, false, ChangeFolderLocation);
                menu.AddItem(EditorGUIUtility.TrTextContent(k_OpenFolder), false, OpenInExplorer);
                if (buildHistoryFolderPath.Equals(BuildHistory.DefaultRootDirectory))
                {
                    menu.AddDisabledItem(k_ResetBuildHistoryFolderText);
                }
                else
                {
                    menu.AddItem(k_ResetBuildHistoryFolderText, false, ResetFolderLocation);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawBuildHistoryLimitRow()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(k_BuildHistoryLimitText, GUILayout.MaxWidth(175));
            EditorGUI.BeginChangeCheck();
            // DelayedIntField commits on Enter / blur instead of every keystroke, so the
            // ScriptableSingleton isn't re-saved to disk while the user is mid-typing.
            int newLimit = EditorGUILayout.DelayedIntField(BuildHistory.BuildHistoryLimit);
            if (EditorGUI.EndChangeCheck())
                BuildHistory.BuildHistoryLimit = Mathf.Max(0, newLimit);
            EditorGUILayout.EndHorizontal();
        }

        private static void OpenInExplorer()
        {
            EditorUtility.RevealInFinder(buildHistoryFolderPath);
        }

        private static void ChangeFolderLocation()
        {
            string newPath = EditorUtility.OpenFolderPanel("Select new build history folder location", buildHistoryFolderPath, buildHistoryFolderPath);
            if (string.IsNullOrEmpty(newPath))
            {
                return;
            }
            else if (!newPath.Equals(buildHistoryFolderPath))
            {
                string projectRelativePath = FileUtil.GetProjectRelativePath(newPath);
                if (!string.IsNullOrEmpty(projectRelativePath))
                {
                    buildHistoryFolderPath = projectRelativePath;
                }
                else
                {
                    buildHistoryFolderPath = newPath;
                }
            }
        }

        private static void ResetFolderLocation()
        {
            buildHistoryFolderPath = BuildHistory.DefaultRootDirectory;
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new BuildPipelineSettingsProvider();
        }
    }
}
