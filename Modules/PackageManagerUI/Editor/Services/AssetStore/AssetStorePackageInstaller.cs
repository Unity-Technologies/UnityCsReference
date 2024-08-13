// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAssetStorePackageInstaller : IService
    {
        void Install(long productId, bool interactiveInstall = false);
        void Uninstall(long productId, bool interactiveUninstall = false);
        void Uninstall(IEnumerable<long> productIds);
    }

    internal class AssetStorePackageInstaller : BaseService<IAssetStorePackageInstaller>, IAssetStorePackageInstaller
    {
        private readonly IIOProxy m_IOProxy;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IAssetSelectionHandler m_AssetSelectionHandler;
        private readonly IApplicationProxy m_Application;
        public AssetStorePackageInstaller(IIOProxy ioProxy,
            IAssetStoreCache assetStoreCache,
            IAssetDatabaseProxy assetDatabaseProxy,
            IAssetSelectionHandler assetSelectionHandler,
            IApplicationProxy applicationProxy)
        {
            m_IOProxy = RegisterDependency(ioProxy);
            m_AssetStoreCache = RegisterDependency(assetStoreCache);
            m_AssetDatabase = RegisterDependency(assetDatabaseProxy);
            m_AssetSelectionHandler = RegisterDependency(assetSelectionHandler);
            m_Application = RegisterDependency(applicationProxy);
        }

        public override void OnEnable()
        {
            m_AssetSelectionHandler.onRemoveSelectionDone += OnRemoveSelectionDone;
        }

        public override void OnDisable()
        {
            m_AssetSelectionHandler.onRemoveSelectionDone -= OnRemoveSelectionDone;
        }

        public void Install(long productId, bool interactiveInstall = false)
        {
            var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
            try
            {
                if (string.IsNullOrEmpty(localInfo?.packagePath) || !m_IOProxy.FileExists(localInfo.packagePath))
                    return;

                var assetOrigin = new AssetOrigin((int)localInfo.productId, localInfo.title, localInfo.versionString, (int)localInfo.uploadId);
                m_AssetDatabase.ImportPackage(localInfo.packagePath, assetOrigin, interactiveInstall);
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot import package {localInfo?.title}: {e.Message}");
            }
        }

        private void RemoveAssetsAndCleanUpEmptyFolders(IEnumerable<Asset> assets)
        {
            const string assetsPath = "Assets";
            var foldersToRemove = new HashSet<string>();
            foreach (var asset in assets)
            {
                var path = m_IOProxy.GetParentDirectory(asset.importedPath);
                // We want to add an asset's parent folders all the way up to the `Assets` folder, because we don't want to leave behind
                // empty folders after the assets are removed process.
                while (!foldersToRemove.Contains(path) && path.StartsWith(assetsPath) && path.Length > assetsPath.Length)
                {
                    foldersToRemove.Add(path);
                    path = m_IOProxy.GetParentDirectory(path);
                }
            }
            var searchInFoldersFilter = new SearchFilter
            {
                folders = foldersToRemove.ToArray(),
                searchArea = SearchFilter.SearchArea.SelectedFolders
            };

            var leftOverAssetsGuids = m_AssetDatabase.FindAssets(searchInFoldersFilter).ToHashSet();
            foreach (var guid in assets.Select(i => i.guid).Concat(foldersToRemove.Select(i => m_AssetDatabase.AssetPathToGUID(i))))
                leftOverAssetsGuids.Remove(guid);

            foreach (var assetPath in leftOverAssetsGuids.Select(i => m_AssetDatabase.GUIDToAssetPath(i)))
            {
                var path = m_IOProxy.GetParentDirectory(assetPath);
                // If after the removal process, there will still be some assets left behind, we want to make sure the folders containing
                // left over assets are not removed
                while (foldersToRemove.Contains(path))
                {
                    foldersToRemove.Remove(path);
                    path = m_IOProxy.GetParentDirectory(path);
                }
            }

            // We order the folders to be removed so that child folders always come before their parent folders
            // This way m_AssetDatabase.DeleteAssets call won't try to remove parent folders first and fail to remove child folders
            var orderedFoldersToRemove = foldersToRemove.OrderByDescending(i => i);
            var assetAndFoldersToRemove = assets.Select(i => i.importedPath).Concat(orderedFoldersToRemove).ToArray();
            var pathsFailedToRemove = new List<string>();
            m_AssetDatabase.DeleteAssets(assetAndFoldersToRemove, pathsFailedToRemove);

            if (pathsFailedToRemove.Any())
            {
                var errorMessage = L10n.Tr("[Package Manager Window] Failed to remove the following asset(s) and/or folder(s):");
                foreach (var path in pathsFailedToRemove)
                    errorMessage += "\n" + path;
                Debug.LogError(errorMessage);

                m_Application.DisplayDialog("cannotRemoveAsset",
                    L10n.Tr("Cannot Remove"),
                    L10n.Tr("Some assets could not be deleted.\nMake sure nothing is keeping a hook on them, like a loaded DLL for example."),
                    L10n.Tr("OK"));
            }
        }

        public void Uninstall(long productId, bool interactiveUninstall = false)
        {
            if (interactiveUninstall)
            {
                var importedPackage = m_AssetStoreCache.GetImportedPackage(productId);
                m_AssetSelectionHandler.Remove(importedPackage, importedPackage.displayName, importedPackage.versionString);
            }
            else
            {
                var importedPackage = m_AssetStoreCache.GetImportedPackage(productId);
                if (importedPackage != null)
                    RemoveAssetsAndCleanUpEmptyFolders(importedPackage);
            }
        }

        public void Uninstall(IEnumerable<long> productIds)
        {
            var assetsToRemove = new List<Asset>();
            foreach (var productId in productIds ?? Enumerable.Empty<long>())
            {
                var importedPackage = m_AssetStoreCache.GetImportedPackage(productId);
                if (importedPackage != null)
                    assetsToRemove.AddRange(importedPackage);
            }

            if (assetsToRemove.Any())
                RemoveAssetsAndCleanUpEmptyFolders(assetsToRemove);
        }

        private void OnRemoveSelectionDone(IEnumerable<Asset> selections)
        {
            if (selections.Any())
                RemoveAssetsAndCleanUpEmptyFolders(selections);
        }
    }
}
