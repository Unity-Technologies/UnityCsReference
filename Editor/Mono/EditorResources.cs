// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Internal;
using System.Collections.Generic;
using UnityEditor;
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
        public static string DataPath
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
    }
}
