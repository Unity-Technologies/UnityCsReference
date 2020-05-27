// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal class AssetDatabaseProxy
    {
        public virtual void ImportPackage(string packagePath, bool interactive)
        {
            AssetDatabase.ImportPackage(packagePath, interactive);
        }

        public virtual void Refresh()
        {
            AssetDatabase.Refresh();
        }

        public virtual T LoadAssetAtPath<T>(string assetPath) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public virtual Object LoadMainAssetAtPath(string assetPath)
        {
            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        public virtual bool GetAssetFolderInfo(string path, out bool rootFolder, out bool immutable)
        {
            return AssetDatabase.GetAssetFolderInfo(path, out rootFolder, out immutable);
        }
    }
}
