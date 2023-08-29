// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageOperationDispatcher : IService
    {
        bool isInstallOrUninstallInProgress { get; }
        bool IsUninstallInProgress(IPackage package);
        bool IsInstallInProgress(IPackageVersion version);

        void Install(IPackageVersion version);
        void Install(IEnumerable<IPackageVersion> versions);
        void Install(string packageId);
        void InstallFromUrl(string url);
        bool InstallFromPath(string path, out string tempPackageId);
        void Uninstall(IPackage package);
        void Uninstall(IEnumerable<IPackage> packages);

        void InstallAndResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset);
        void ResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset);

        void RemoveEmbedded(IPackage package);

        void FetchExtraInfo(IPackageVersion version);

        bool Download(IPackage package);
        bool Download(IEnumerable<IPackage> packages);
        void AbortDownload(IPackage package);
        void AbortDownload(IEnumerable<IPackage> packages);
        void PauseDownload(IPackage package);
        void ResumeDownload(IPackage package);

        void Import(IPackage package);
        void RemoveImportedAssets(IPackage package);
        void RemoveImportedAssets(IEnumerable<IPackageVersion> versions);
    }

    internal class PackageOperationDispatcher : BaseService<IPackageOperationDispatcher>, IPackageOperationDispatcher
    {
        private readonly IAssetStorePackageInstaller m_AssetStorePackageInstaller;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IUpmClient m_UpmClient;

        public PackageOperationDispatcher(IAssetStorePackageInstaller assetStorePackageInstaller,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IUpmClient upmClient)
        {
            m_AssetStorePackageInstaller = RegisterDependency(assetStorePackageInstaller);
            m_AssetStoreDownloadManager = RegisterDependency(assetStoreDownloadManager);
            m_UpmClient = RegisterDependency(upmClient);
        }

        public bool isInstallOrUninstallInProgress => m_UpmClient.isAddOrRemoveInProgress;

        public bool IsUninstallInProgress(IPackage package)
        {
            return m_UpmClient.IsRemoveInProgress(package?.name);
        }

        public bool IsInstallInProgress(IPackageVersion version)
        {
            return m_UpmClient.IsAddInProgress(version?.packageId);
        }

        public void Install(IPackageVersion version)
        {
            if (version == null || version.isInstalled)
                return;
            m_UpmClient.AddById(version.packageId);
        }

        public void Install(IEnumerable<IPackageVersion> versions)
        {
            if (versions == null || !versions.Any())
                return;

            m_UpmClient.AddByIds(versions.Select(v => v.packageId));
        }

        public void Install(string packageId)
        {
            m_UpmClient.AddById(packageId);
        }

        public void InstallFromUrl(string url)
        {
            m_UpmClient.AddByUrl(url);
        }

        public bool InstallFromPath(string path, out string tempPackageId)
        {
            return m_UpmClient.AddByPath(path, out tempPackageId);
        }

        public void Uninstall(IPackage package)
        {
            if (package?.versions.installed == null)
                return;
            m_UpmClient.RemoveByName(package.name);
        }

        public void Uninstall(IEnumerable<IPackage> packages)
        {
            if (packages == null || !packages.Any())
                return;
            m_UpmClient.RemoveByNames(packages.Select(p => p.name));
        }

        public void InstallAndResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset)
        {
            m_UpmClient.AddAndResetDependencies(version.packageId, dependenciesToReset?.Select(package => package.name) ?? Enumerable.Empty<string>());
        }

        public void ResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset)
        {
            m_UpmClient.ResetDependencies(version.packageId, dependenciesToReset?.Select(package => package.name) ?? Enumerable.Empty<string>());
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

        public bool Download(IPackage package)
        {
            return Download(new[] { package });
        }

        public bool Download(IEnumerable<IPackage> packages)
        {
            return PlayModeDownload.CanBeginDownload() && m_AssetStoreDownloadManager.Download(packages.Select(p => p.product?.id ?? 0).Where(id => id > 0));
        }

        public void AbortDownload(IPackage package)
        {
            AbortDownload(new[] { package });
        }

        public void AbortDownload(IEnumerable<IPackage> packages)
        {
            // We need to figure out why the IEnumerable is being altered instead of using ToArray.
            // It will be addressed in https://jira.unity3d.com/browse/PAX-1995.
            foreach (var package in packages.ToArray())
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
            if (package?.versions.primary?.importedAssets?.Any() != true)
                return;

            m_AssetStorePackageInstaller.Uninstall(package.product.id, true);
        }

        public void RemoveImportedAssets(IEnumerable<IPackageVersion> versions)
        {
            if (versions?.Any() != true)
                return;

            m_AssetStorePackageInstaller.Uninstall(versions.Select(v => v.package.product.id));
        }
    }
}
