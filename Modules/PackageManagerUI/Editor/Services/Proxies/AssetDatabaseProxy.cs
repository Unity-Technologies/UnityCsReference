// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [ExcludeFromCodeCoverage]
    internal class AssetDatabaseProxy : AssetPostprocessor
    {
        public virtual event Action<string[] /*importedAssets*/, string[] /*deletedAssets*/, string[] /*movedAssets*/, string[] /*movedFromAssetPaths*/> onPostprocessAllAssets = delegate {};

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            ServicesContainer.instance.Resolve<AssetDatabaseProxy>().onPostprocessAllAssets?.Invoke(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        public virtual void ImportPackage(string packagePath, bool interactive)
        {
            AssetDatabase.ImportPackage(packagePath, interactive);
        }

        public virtual void ImportPackage(string packagePath, AssetOrigin origin, bool interactive)
        {
            AssetDatabase.ImportPackage(packagePath, origin, interactive);
        }

        public virtual bool DeleteAssets(string[] paths, List<string> outFailedPaths)
        {
            return AssetDatabase.DeleteAssets(paths, outFailedPaths);
        }

        public virtual void Refresh()
        {
            AssetDatabase.Refresh();
        }

        public virtual T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public virtual UnityEngine.Object LoadMainAssetAtPath(string assetPath)
        {
            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        public virtual bool GetAssetFolderInfo(string path, out bool rootFolder, out bool immutable)
        {
            return AssetDatabase.GetAssetFolderInfo(path, out rootFolder, out immutable);
        }

        public virtual AssetOrigin GetAssetOrigin(string guid)
        {
            return AssetDatabase.GetAssetOrigin(guid);
        }

        public virtual string GUIDToAssetPath(string guid)
        {
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        public virtual string AssetPathToGUID(string path)
        {
            return AssetDatabase.AssetPathToGUID(path);
        }

        public virtual string[] FindAssets(SearchFilter filter)
        {
            return AssetDatabase.FindAssets(filter);
        }
    }
}
