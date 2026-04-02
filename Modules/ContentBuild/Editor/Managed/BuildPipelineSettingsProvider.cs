// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Build
{
    class BuildPipelineSettingsProvider : SettingsProvider
    {
        static readonly GUIContent k_GenerateTEPFileText = EditorGUIUtility.TrTextContent("Generate build performance profiling file", "Create a trace event profiling file under Logs folder to analyze build performance.");
        static readonly GUIContent k_WriteDebugFileText = EditorGUIUtility.TrTextContent("Write debug files", "Write intermediate files with additional debugging information into the build history.");
        static readonly GUIContent k_ResetBuildHistoryFolderText = EditorGUIUtility.TrTextContent("Reset Build History folder Location");
        static readonly GUIContent k_ChangeBuildHistoryFolderLocationText = EditorGUIUtility.TrTextContent("Change Build History Folder Location", "Change the build history path to a new path on your device. Note that this does not move any existing history. ");
        static readonly GUIContent k_CleanBuildHistoryFolderText = EditorGUIUtility.TrTextContent("Delete Build History", "Deletes all of the build history within the current build history folder.");
        //static readonly GUIContent k_BuildHistoryFolderPathText = EditorGUIUtility.TrTextContent(buildHistoryFolderPath, "The Build History path.");
        private static readonly string k_OpenFolder = L10n.Tr("Open Containing Folder");

        private static readonly string k_ChangeLocation = L10n.Tr("Change Location");
        private static readonly string k_ResetToDefaultLocation = L10n.Tr("Reset to Default Location");
        static readonly string k_GenerateTEPFileKey = "BuildPipelineGenerateTEPFile";
        static readonly string k_WriteDebugFilesKey = "BuildPipelineWriteDebugFiles";
        private BuildPipelineSettingsProvider()
            : base("Preferences/Analysis/Build Pipeline", SettingsScope.User)
        { }

        public static bool generateTEPFile
        {
            get => EditorPrefs.GetBool(k_GenerateTEPFileKey, false);
            set => EditorPrefs.SetBool(k_GenerateTEPFileKey, value);
        }

        public static bool writeDebugFiles
        {
            get => EditorPrefs.GetBool(k_WriteDebugFilesKey, false);
            set => EditorPrefs.SetBool(k_WriteDebugFilesKey, value);
        }

        /*UCBP-PUBLIC*/ internal static string buildHistoryFolderPath
        {
            get => BuildHistory.BuildHistoryDirectory;
            set => BuildHistory.BuildHistoryDirectory = value;
        }

        public override void OnGUI(string searchContext)
        {
            using var _ = new SettingsWindow.GUIScope();

            EditorGUIUtility.labelWidth = 300;
            generateTEPFile = EditorGUILayout.Toggle(k_GenerateTEPFileText, generateTEPFile);
            writeDebugFiles = EditorGUILayout.Toggle(k_WriteDebugFileText, writeDebugFiles);


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
                menu.AddItem(k_CleanBuildHistoryFolderText, false, CleanBuildHistoryDirectories);
                menu.ShowAsContext();
            }
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

        private static void CleanBuildHistoryDirectories()
        {
            BuildHistory.DeleteHistory();
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new BuildPipelineSettingsProvider();
        }
    }
}
