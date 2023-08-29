// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAssetDatabaseProxy : IService
    {
        event Action<string[] /*importedAssets*/, string[] /*deletedAssets*/, string[] /*movedAssets*/, string[] /*movedFromAssetPaths*/> onPostprocessAllAssets;

        void ImportPackage(string packagePath, bool interactive);
        void ImportPackage(string packagePath, AssetOrigin origin, bool interactive);
        bool DeleteAssets(string[] paths, List<string> outFailedPaths);
        void Refresh();
        T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object;
        UnityEngine.Object LoadMainAssetAtPath(string assetPath);
        bool TryGetAssetFolderInfo(string path, out bool rootFolder, out bool immutable);
        AssetOrigin GetAssetOrigin(string guid);
        string GUIDToAssetPath(string guid);
        string AssetPathToGUID(string path);
        string[] FindAssets(SearchFilter filter);
    }

    [ExcludeFromCodeCoverage]
    internal class AssetDatabaseProxy : BaseService<IAssetDatabaseProxy>, IAssetDatabaseProxy
    {
        private class AssetPostprocessor : UnityEditor.AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                ServicesContainer.instance.Resolve<AssetDatabaseProxy>().onPostprocessAllAssets?.Invoke(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
        }

        public event Action<string[] /*importedAssets*/, string[] /*deletedAssets*/, string[] /*movedAssets*/, string[] /*movedFromAssetPaths*/> onPostprocessAllAssets = delegate {};

        public void ImportPackage(string packagePath, bool interactive)
        {
            AssetDatabase.ImportPackage(packagePath, interactive);
        }

        public void ImportPackage(string packagePath, AssetOrigin origin, bool interactive)
        {
            AssetDatabase.ImportPackage(packagePath, origin, interactive);
        }

        public bool DeleteAssets(string[] paths, List<string> outFailedPaths)
        {
            return AssetDatabase.DeleteAssets(paths, outFailedPaths);
        }

        public void Refresh()
        {
            AssetDatabase.Refresh();
        }

        public T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public UnityEngine.Object LoadMainAssetAtPath(string assetPath)
        {
            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        public bool TryGetAssetFolderInfo(string path, out bool rootFolder, out bool immutable)
        {
            return AssetDatabase.TryGetAssetFolderInfo(path, out rootFolder, out immutable);
        }

        public AssetOrigin GetAssetOrigin(string guid)
        {
            return AssetDatabase.GetAssetOrigin(guid);
        }

        public string GUIDToAssetPath(string guid)
        {
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        public string AssetPathToGUID(string path)
        {
            return AssetDatabase.AssetPathToGUID(path);
        }

        public string[] FindAssets(SearchFilter filter)
        {
            return AssetDatabase.FindAssets(filter);
        }
    }
}
