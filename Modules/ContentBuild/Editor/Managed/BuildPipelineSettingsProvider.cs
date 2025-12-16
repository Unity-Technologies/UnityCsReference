// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Build
{
    class BuildPipelineSettingsProvider : SettingsProvider
    {
        static readonly GUIContent k_GenerateTEPFileText = EditorGUIUtility.TrTextContent("Generate build performance profiling file", "Create a trace event profiling file under Logs folder to analyze build performance.");
        static readonly GUIContent k_WriteDebugFileText = EditorGUIUtility.TrTextContent("Write debug files", "Write intermediate files with additional debugging information to BuildMetadata directory.");
        static readonly GUIContent k_ResetMetadataFolderText = EditorGUIUtility.TrTextContent("Reset Metadata Folder Location");
        static readonly GUIContent k_ChangeMetadataFolderLocationText = EditorGUIUtility.TrTextContent("Change Metadata Folder Location", "Change the Build Metadata folder path to a new path on your device. Note that this does not move any existing metadata folders. ");
        static readonly GUIContent k_CleanMetadataFolderText = EditorGUIUtility.TrTextContent("Clean Build Metadata Directories", "Deletes all of the build metadata directories within the current build metadata folder.");
        //static readonly GUIContent k_MetadataFolderPathText = EditorGUIUtility.TrTextContent(metadataFolderPath, "The path that Build metadata folders will be created in.");
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

        /*UCBP-PUBLIC*/ internal static string metadataFolderPath
        {
            get => BuildMetadata.GetRootDirectory();
            set => BuildMetadata.SetRootDirectory(value);
        }

        public override void OnGUI(string searchContext)
        {
            using var _ = new SettingsWindow.GUIScope();

            EditorGUIUtility.labelWidth = 300;
            generateTEPFile = EditorGUILayout.Toggle(k_GenerateTEPFileText, generateTEPFile);
            writeDebugFiles = EditorGUILayout.Toggle(k_WriteDebugFileText, writeDebugFiles);


        }


        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new BuildPipelineSettingsProvider();
        }
    }
}
