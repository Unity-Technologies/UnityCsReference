// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageOperationDispatcher : IService
    {
        bool isInstallOrUninstallInProgress { get; }
        bool isEmbedInProgress { get; }
        bool IsUninstallInProgress(IPackage package);
        bool IsInstallInProgress(IPackageVersion version);

        bool Install(IPackageVersion version, OperationType operationType);
        bool Install(IReadOnlyCollection<IPackageVersion> versions, OperationType operationType);
        bool Install(string packageId, OperationType operationType);
        bool InstallFromUrl(string url);
        bool InstallFromPath(string path, out string tempPackageId);
        void Uninstall(IPackage package);
        void Uninstall(IReadOnlyCollection<IPackage> packages);

        void InstallAndResetDependencies(IPackageVersion version, IReadOnlyCollection<IPackage> dependenciesToReset);
        void ResetDependencies(IPackageVersion version, IReadOnlyCollection<IPackage> dependenciesToReset);

        bool Embed(IPackage package);
        void RemoveEmbedded(IPackage package);

        void FetchExtraInfo(IPackageVersion version);

        bool OpenManifest(IPackageVersion version);

        bool Download(IPackage package);
        bool Download(IEnumerable<IPackage> packages);
        void AbortDownload(IPackage package);
        void AbortDownload(IReadOnlyCollection<IPackage> packages);
        void PauseDownload(IPackage package);
        void ResumeDownload(IPackage package);

        void Import(IPackage package);
        void RemoveImportedAssets(IPackage package);
        void RemoveImportedAssets(IReadOnlyCollection<IPackage> packages);
    }

    internal class PackageOperationDispatcher : BaseService<IPackageOperationDispatcher>, IPackageOperationDispatcher
    {
        private readonly IAssetStorePackageInstaller m_AssetStorePackageInstaller;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IUpmClient m_UpmClient;
        private readonly ISelectionProxy m_SelectionProxy;
        private readonly IAssetDatabaseProxy m_AssetDatabaseProxy;

        public PackageOperationDispatcher(
            IAssetStorePackageInstaller assetStorePackageInstaller,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IUpmClient upmClient,
            ISelectionProxy selectionProxy,
            IAssetDatabaseProxy assetDatabaseProxy)
        {
            m_AssetStorePackageInstaller = RegisterDependency(assetStorePackageInstaller);
            m_AssetStoreDownloadManager = RegisterDependency(assetStoreDownloadManager);
            m_UpmClient = RegisterDependency(upmClient);
            m_SelectionProxy = RegisterDependency(selectionProxy);
            m_AssetDatabaseProxy = RegisterDependency(assetDatabaseProxy);
        }

        public bool isInstallOrUninstallInProgress => m_UpmClient.isAddOrRemoveInProgress;
        public bool isEmbedInProgress => m_UpmClient.isEmbedInProgress;

        private string InstallOrRemoveInProgressWarningMessage(string installSource) => string.Format(L10n.Tr("[Package Manager Window] The request to install {0} will be canceled due to an ongoing Install/Remove operation. Please retry your request once the current operation has completed."), installSource);
        public bool IsUninstallInProgress(IPackage package)
        {
            return m_UpmClient.IsRemoveInProgress(package?.name);
        }

        public bool IsInstallInProgress(IPackageVersion version)
        {
            return m_UpmClient.IsAddInProgress(version?.packageId);
        }

        public bool Install(IPackageVersion version, OperationType operationType)
        {
            if (version == null || version.isInstalled)
                return false;

            // When there is an IPackageVersion, we know for sure that the packageId is in the PackageDatabase
            m_UpmClient.AddById(version.packageId, false, operationType);
            return true;
        }

        public bool Install(IReadOnlyCollection<IPackageVersion> versions, OperationType operationType)
        {
            if (versions == null || versions.Count == 0)
                return false;

            m_UpmClient.AddByIds(versions.SelectToNewArray(v => v.packageId), operationType);
            return true;
        }

        public bool Install(string packageId, OperationType operationType)
        {
            if (isInstallOrUninstallInProgress)
            {
                Debug.LogWarning(InstallOrRemoveInProgressWarningMessage(packageId));
                return false;
            }
            m_UpmClient.AddById(packageId, true, operationType);
            return true;
        }

        public bool InstallFromUrl(string url)
        {
            if (isInstallOrUninstallInProgress)
            {
                Debug.LogWarning(InstallOrRemoveInProgressWarningMessage(url));
                return false;
            }
            m_UpmClient.AddByUrl(url, OperationType.Install);
            return true;
        }

        public bool InstallFromPath(string path, out string tempPackageId)
        {
            if (isInstallOrUninstallInProgress)
            {
                tempPackageId = null;
                Debug.LogWarning(InstallOrRemoveInProgressWarningMessage(path));
                return false;
            }
            return m_UpmClient.AddByPath(path, OperationType.Install, out tempPackageId);
        }

        public void Uninstall(IPackage package)
        {
            if (package?.versions.installed == null)
                return;
            m_UpmClient.RemoveByName(package.name, OperationType.Remove);
        }

        public void Uninstall(IReadOnlyCollection<IPackage> packages)
        {
            if (packages == null || packages.Count == 0)
                return;
            m_UpmClient.RemoveByNames(packages.SelectToNewArray(p => p.name), OperationType.Remove);
        }

        public void InstallAndResetDependencies(IPackageVersion version, IReadOnlyCollection<IPackage> dependenciesToReset)
        {
            m_UpmClient.AddAndResetDependencies(version.packageId, dependenciesToReset?.SelectToNewArray(package => package.name), OperationType.Install);
        }

        public void ResetDependencies(IPackageVersion version, IReadOnlyCollection<IPackage> dependenciesToReset)
        {
            m_UpmClient.ResetDependencies(version.packageId, dependenciesToReset?.SelectToNewArray(package => package.name), OperationType.Reset);
        }

        public bool Embed(IPackage package)
        {
            if (package?.versions.installed == null)
                return false;

            try
            {
                m_UpmClient.Embed(package.name);
            }
            catch (Exception e)
            {
                Debug.Log($"[Package Manager Window] Cannot embed package {package.name}: {e.Message}");
                return false;
            }

            return true;
        }

        public void RemoveEmbedded(IPackage package)
        {
            if (package?.versions.installed == null)
                return;
            m_UpmClient.RemoveEmbeddedByName(package.name);
        }

        public void FetchExtraInfo(IPackageVersion version)
        {
            if (version == null || version.isFullyFetched)
                return;
            m_UpmClient.ExtraFetchPackageInfo(version.packageId);
        }

        public bool OpenManifest(IPackageVersion version)
        {
            var path = IOUtils.PathsCombine("Packages", version.name, "package.json");
            var folderObject = m_AssetDatabaseProxy.LoadAssetAtPath<Object>(path);
            if (folderObject is null)
                return false;

            m_SelectionProxy.activeObject = folderObject;
            var inspectorWindow = EditorWindow.GetWindow<InspectorWindow>();
            if (inspectorWindow.isLocked)
            {
                var newInspectorWindow = EditorWindow.CreateWindow<InspectorWindow>();
                newInspectorWindow.Show(true);
            }
            else
                inspectorWindow.Show(true);
            return true;
        }

        public bool Download(IPackage package)
        {
            return Download(new[] { package });
        }

        public bool Download(IEnumerable<IPackage> packages)
        {
            return PlayModeDownload.CanBeginDownload() && m_AssetStoreDownloadManager.Download(packages.SelectAsEnumerable(p => p.product?.id ?? 0).Filter(id => id > 0));
        }

        public void AbortDownload(IPackage package)
        {
            AbortDownload(new[] { package });
        }

        public void AbortDownload(IReadOnlyCollection<IPackage> packages)
        {
            // We use ToNewArray here as the original collection might be modified in the for loop
            foreach (var package in packages.ToNewArray())
                m_AssetStoreDownloadManager.AbortDownload(package.product?.id);
        }

        public void PauseDownload(IPackage package)
        {
            if (package?.versions.primary.HasTag(PackageTag.LegacyFormat) != true)
                return;
            m_AssetStoreDownloadManager.PauseDownload(package.product?.id);
        }

        public void ResumeDownload(IPackage package)
        {
            if (package?.versions.primary.HasTag(PackageTag.LegacyFormat) != true || !PlayModeDownload.CanBeginDownload())
                return;
            m_AssetStoreDownloadManager.ResumeDownload(package.product?.id);
        }

        public void Import(IPackage package)
        {
            if (package?.versions.primary.HasTag(PackageTag.LegacyFormat) != true)
                return;

            try
            {
                m_AssetStorePackageInstaller.Install(package.product.id, true);
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot import package {package.displayName}: {e.Message}");
            }
        }

        public void RemoveImportedAssets(IPackage package)
        {
            if (package?.versions.imported == null)
                return;

            m_AssetStorePackageInstaller.Uninstall(package.product.id, true);
        }

        public void RemoveImportedAssets(IReadOnlyCollection<IPackage> packages)
        {
            if (packages == null || packages.Count == 0)
                return;

            m_AssetStorePackageInstaller.Uninstall(packages.SelectAsEnumerable(p => p.product.id));
        }
    }
}
