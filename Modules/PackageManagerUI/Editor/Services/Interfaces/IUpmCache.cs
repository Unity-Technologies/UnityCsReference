// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IUpmCache
    {
        event Action<IEnumerable<PackageInfo>> onPackageInfosUpdated;

        IEnumerable<PackageInfo> searchPackageInfos { get; }
        IEnumerable<PackageInfo> installedPackageInfos { get; }
        IEnumerable<PackageInfo> productPackageInfos { get; }

        void AddExtraPackageInfo(PackageInfo packageInfo);

        Dictionary<string, PackageInfo> GetExtraPackageInfos(string packageName);

        // if the installed package exists in the cache, remove the corresponding package info from the cache
        // and trigger an `onPackageInfosUpdated` event
        void RemoveInstalledPackageInfo(string packageName);

        bool IsPackageInstalled(string packageName);

        PackageInfo GetInstalledPackageInfo(string packageName);

        // set an individual installed package info in the cache, if the new package info is different from
        // the existing one (added or updated), an `onPackageInfosUpdated` event will be triggered
        void SetInstalledPackageInfo(PackageInfo info);

        // set all installed package infos in the cache, if the new set of packages is different from the
        // the existing set (any packages added, removed, updated) an `onPackageInfosUpdated` event will be triggered
        void SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos);

        PackageInfo GetSearchPackageInfo(string packageName);

        // set all search package infos in the cache, if the new set of packages is different from the
        // the existing set (any packages added, removed, updated) an `onPackageInfosUpdated` event will be triggered
        void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos);

        PackageInfo GetProductPackageInfo(string packageName);

        void SetProductPackageInfo(string productId, PackageInfo info);

        string GetProductId(string packageName);

        void ClearCache();
        void ClearProductCache();
    }
}
