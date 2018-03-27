// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Internal;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Experimental
{
    [ExcludeFromDocs]
    public partial class EditorResources
    {
        private static bool s_editorResourcesPackageLoaded;

        // Editor resources package root path.
        private static readonly string packagePathPrefix = $"Packages/{packageName}";

        // Checks if the editor resources are mounted as a package.
        public static bool EditorResourcesPackageAvailable
        {
            get
            {
                if (s_editorResourcesPackageLoaded)
                    return true;
                bool isRootFolder, isReadonly;
                bool validPath = AssetDatabase.GetAssetFolderInfo(packagePathPrefix, out isRootFolder, out isReadonly);
                s_editorResourcesPackageLoaded = validPath && isRootFolder;
                return s_editorResourcesPackageLoaded;
            }
        }

        // Returns the editor resources absolute file system path.
        public static string dataPath
        {
            get
            {
                if (EditorResourcesPackageAvailable)
                    return new DirectoryInfo(Path.Combine(packagePathPrefix, "Assets")).FullName;
                return Application.dataPath;
            }
        }

        // Resolve an editor resource asset path.
        public static string ExpandPath(string path)
        {
            if (!EditorResourcesPackageAvailable)
                return path;
            if (path.StartsWith(packagePathPrefix))
                return path.Replace("\\", "/");
            return Path.Combine(packagePathPrefix, path).Replace("\\", "/");
        }

        // Returns the full file system path of an editor resource asset path.
        public static string GetFullPath(string path)
        {
            if (File.Exists(path))
                return path;
            return new FileInfo(ExpandPath(path)).FullName;
        }

        // Checks if an editor resource asset path exists.
        public static bool Exists(string path)
        {
            return File.Exists(ExpandPath(path));
        }

        // Loads an editor resource asset.
        public static T Load<T>(string assetPath, bool isRequired = true) where T : UnityEngine.Object
        {
            var obj = Load(assetPath, typeof(T));
            if (!obj && isRequired)
                throw new FileNotFoundException("Could not find editor resource " + assetPath);
            return obj as T;
        }

        // Mount the editor resources folder as a package.
        private static bool LoadEditorResourcesPackage(string editorResourcesPath)
        {
            // Make sure the folder contains a package.
            if (!File.Exists(Path.Combine(editorResourcesPath, "package.json")))
            {
                Debug.LogError(editorResourcesPath + "does not contain a package descriptor.");
                return false;
            }

            // We need editor resources meta files to be visible to prevent build issues.
            EditorSettings.Internal_UserGeneratedProjectSuffix = "-testable";
            EditorSettings.externalVersionControl = ExternalVersionControl.Generic;
            AssetDatabase.SaveAssets();

            PackageManager.Client.Add($"{packageName}@file:{editorResourcesPath}");
            return true;
        }

        [MenuItem("Tools/Load Editor Resources", false, 5000, true)]
        internal static void LoadEditorResourcesIntoProject()
        {
            // Set default editor resources project.
            var editorResourcesPath = Path.Combine(Unsupported.GetBaseUnityDeveloperFolder(), "External/Resources/editor_resources");
            if (!Directory.Exists(editorResourcesPath))
                editorResourcesPath = Directory.GetCurrentDirectory();

            // Ask the user to select the editor resources package folder.
            editorResourcesPath = EditorUtility.OpenFolderPanel("Select editor resources folder", editorResourcesPath, "");
            if (String.IsNullOrEmpty(editorResourcesPath))
                return;

            // Make sure the editor_resources project does not contain any Library/ folder which could make the asset database crash if imported.
            var editorResourcesLibraryPath = Path.Combine(editorResourcesPath, "Library");
            if (Directory.Exists(editorResourcesLibraryPath))
            {
                Debug.LogError($"Please dispose of the Library folder under {editorResourcesPath} as it might fail to be imported.");
                return;
            }

            if (LoadEditorResourcesPackage(editorResourcesPath))
                EditorApplication.OpenProject(Path.Combine(Application.dataPath, ".."), Environment.GetCommandLineArgs());
        }
    }
}
