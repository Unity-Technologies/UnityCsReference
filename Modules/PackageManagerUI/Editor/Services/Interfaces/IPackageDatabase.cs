// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageDatabase
    {
        // args 1,2, 3 are added, removed and preUpdated, and postUpdated packages respectively
        event Action<IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>> onPackagesChanged;

        event Action<IPackage, IPackageVersion> onInstallSuccess;
        event Action<IPackage> onUninstallSuccess;

        event Action<IPackage> onPackageProgressUpdate;

        void RegisterEvents();

        void UnregisterEvents();
        void Reload();

        bool isEmpty { get; }
        bool isInstallOrUninstallInProgress { get; }
        bool IsUninstallInProgress(IPackage package);
        bool IsInstallInProgress(IPackageVersion version);

        void FetchExtraInfo(IPackageVersion version);

        void Install(IPackageVersion version);
        void InstallFromUrl(string url);
        void InstallFromPath(string path);

        void Uninstall(IPackage package);

        bool IsDownloadInProgress(IPackageVersion version);

        void Download(IPackage package);

        void AbortDownload(IPackage package);

        void Import(IPackage package);

        void Embed(IPackageVersion package);
        void RemoveEmbedded(IPackage package);

        IEnumerable<IPackage> allPackages { get; }
        IEnumerable<IPackage> assetStorePackages { get; }
        IEnumerable<IPackage> upmPackages { get; }

        void AddPackageError(IPackage package, UIError error);
        void ClearPackageErrors(IPackage package);

        IPackage GetPackageByDisplayName(string displayName);

        IPackage GetPackage(string uniqueId);
        IPackage GetPackage(IPackageVersion version);

        IPackageVersion GetPackageVersion(string packageUniqueId, string versionUniqueId);
        IPackageVersion GetPackageVersion(DependencyInfo info);

        void GetPackageAndVersion(string packageUniqueId, string versionUniqueId, out IPackage package, out IPackageVersion version);

        IEnumerable<IPackageVersion> GetReverseDependencies(IPackageVersion version);

        IEnumerable<IPackage> packagesInError { get; }
    }
}
