// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Flags]
    internal enum RefreshOptions : short
    {
        None            = 0,
        OfflineMode     = 1 << 0,
        ListInstalled   = 1 << 1,
        SearchAll       = 1 << 2,
    }

    internal interface IPackageDatabase
    {
        event Action<long> onUpdateTimeChange;

        // args 1,2, 3 are added, removed and updated packages respectively
        event Action<IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>> onPackagesChanged;
        event Action<IPackageVersion> onPackageVersionUpdated;

        event Action<IPackage, IPackageVersion> onInstallSuccess;
        event Action<IPackage> onUninstallSuccess;

        event Action<IPackage> onPackageOperationStart;
        event Action<IPackage> onPackageOperationFinish;

        event Action onRefreshOperationStart;
        event Action onRefreshOperationFinish;
        event Action<Error> onRefreshOperationError;

        void Setup();

        void Clear();

        void Refresh(RefreshOptions options);

        bool isEmpty { get; }
        bool isInstallOrUninstallInProgress { get; }
        bool IsUninstallInProgress(IPackage package);
        bool IsInstallInProgress(IPackageVersion version);

        void FetchExtraInfo(IPackageVersion version);

        void Install(IPackageVersion version);
        void InstallFromUrl(string url);
        void InstallFromPath(string path);

        void Uninstall(IPackage package);

        void Embed(IPackage package);
        void RemoveEmbedded(IPackage package);

        long lastUpdateTimestamp { get; }

        IEnumerable<IPackage> allPackages { get; }

        void AddPackageError(IPackage package, Error error);
        void ClearPackageErrors(IPackage package);

        IPackage GetPackageByDisplayName(string displayName);

        IPackage GetPackage(string uniqueId);
        IPackage GetPackage(IPackageVersion version);

        IPackageVersion GetPackageVersion(string packageUniqueId, string versionUniqueId);
        IPackageVersion GetPackageVersion(DependencyInfo info);

        void GetPackageAndVersion(string packageUniqueId, string versionUniqueId, out IPackage package, out IPackageVersion version);

        IEnumerable<IPackageVersion> GetDependentVersions(IPackageVersion version);
    }
}
