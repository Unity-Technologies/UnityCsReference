// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal partial class PackageFactory
    {
        private void RegisterEventsForUpmPackages()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;
            m_UpmCache.onLoadAllVersionsChanged += OnLoadAllVersionsChanged;

            m_UpmClient.onPackagesReadyToReevaluate += OnPackagesReadyToReevaluate;
            m_UpmClient.onPackagesProgressChange += OnPackagesProgressChange;
            m_UpmClient.onPackageOperationError += OnPackageOperationError;
            m_UpmClient.onSpecialInstallStart += OnSpecialInstallStart;
            m_UpmClient.onSpecialInstallFinalize += OnSpecialInstallFinalize;

            m_PackageCreator.onPackageCreated += OnPackageCreated;
        }

        private void UnregisterEventsForUpmPackages()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched -= OnExtraPackageInfoFetched;
            m_UpmCache.onLoadAllVersionsChanged -= OnLoadAllVersionsChanged;

            m_UpmClient.onPackagesReadyToReevaluate -= OnPackagesReadyToReevaluate;
            m_UpmClient.onPackagesProgressChange -= OnPackagesProgressChange;
            m_UpmClient.onPackageOperationError -= OnPackageOperationError;
            m_UpmClient.onSpecialInstallStart -= OnSpecialInstallStart;
            m_UpmClient.onSpecialInstallFinalize -= OnSpecialInstallFinalize;

            m_PackageCreator.onPackageCreated -= OnPackageCreated;
        }

        private void OnPackageOperationError(string packageIdOrName, UIError error)
        {
            var package = m_PackageDatabase.GetPackageByIdOrName(packageIdOrName) as Package;
            if (package == null)
                return;

            if (package.versions.primary.HasTag(PackageTag.SpecialInstall))
            {
                m_PackageDatabase.UpdatePackages(toRemove: new[] { package.uniqueId });
                if (!error.HasAttribute(UIError.Attribute.DetailInConsole))
                {
                    UnityEngine.Debug.Log(error.message);
                    error.attribute |= UIError.Attribute.DetailInConsole;
                }
                return;
            }
            AddError(package, error);
            m_PackageDatabase.OnPackagesModified(new[] { package });
        }

        private void OnPackageCreated(string packageName)
        {
            HandleSpecialInstall(packageName, L10n.Tr("Creating a new package"), PackageTag.Custom);
        }

        private void OnPackagesProgressChange(IEnumerable<(string packageIdOrName, PackageProgress progress)> progressUpdates)
        {
            var packagesUpdated = new List<IPackage>();
            foreach (var item in progressUpdates)
            {
                var package = m_PackageDatabase.GetPackageByIdOrName(item.packageIdOrName) as Package;
                if (package == null || package.progress == item.progress)
                    continue;

                // For special installations, we don't want to modify the progress here because we want to keep the
                // installing package in the `In Project` this. The special installation package will be replaced
                // by the actual package in PackageDatabase when we detect uniqueId changes through uniqueIdMapper
                if (package.versions.primary.HasTag(PackageTag.SpecialInstall))
                    continue;

                SetProgress(package, item.progress);
                packagesUpdated.Add(package);
            }
            if (packagesUpdated.Count > 0)
                m_PackageDatabase.OnPackagesModified(packagesUpdated, true);
        }

        private void OnPackagesReadyToReevaluate(IReadOnlyCollection<string> packageNames)
        {
            GeneratePackagesAndTriggerChangeEvent(packageNames);
        }

        private void HandleSpecialInstall(string packageIdOrName, string displayName, PackageTag additionalTags = PackageTag.None)
        {
            var version = new PlaceholderPackageVersion(packageIdOrName, displayName, tag: PackageTag.UpmFormat | PackageTag.SpecialInstall | additionalTags);
            var placeholderPackage = CreatePackage(packageIdOrName, new PlaceholderVersionList(version));
            SetProgress(placeholderPackage, PackageProgress.Installing);
            m_PackageDatabase.UpdatePackages(new[] { placeholderPackage });
        }

        private void OnSpecialInstallStart(string packageIdOrName)
        {
            if (m_PackageDatabase.GetPackageByIdOrName(packageIdOrName) != null)
                return;

            // Special handling for installing a package that's not already in the database. This case is most likely to happen
            // when a user adds a git or local package through the special add package UI.
            HandleSpecialInstall(packageIdOrName, L10n.Tr("Installing a new package"));
        }

        private void OnSpecialInstallFinalize(string packageIdOrName, string finalPackageId)
        {
            m_PackageDatabase.FinalizePackageUniqueId(packageIdOrName, finalPackageId);
        }

        private void OnExtraPackageInfoFetched(PackageInfo packageInfo)
        {
            if (m_UpmCache.GetInstalledPackageInfo(packageInfo.name)?.packageId != packageInfo.packageId)
                GeneratePackagesAndTriggerChangeEvent(new[] { packageInfo.name });
        }

        private void OnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> updatedInfos, PackagesChangedSource changedSource)
        {
            // The `GeneratePackagesAndTriggerChangeEvent` call here already handles cases where an added/updated packageInfo contains product id.
            // We only need to pass packageNames because the packageInfo is still in the UpmCache, so we'll know when there is a productId linked to it
            var packageNames = updatedInfos.SelectToNewArray(i => i.oldInfo?.name ?? i.newInfo?.name);
            GeneratePackagesAndTriggerChangeEvent(packageNames, changedSource);

            // The `GeneratePackagesAndTriggerChangeEvent` call here with productIds handles cases where an old packageInfo with product id is removed
            // or replaced with one without product id (when a scoped registry is added for example). In this case since the packageInfo in UpmCache
            // no longer contains information about that product id, we need to explicitly call the generate function to update or remove the old package.
            var productIds = new List<long>();
            foreach (var (oldInfo, newInfo) in updatedInfos)
            {
                var oldInfoProductId = oldInfo?.ParseProductId() ?? 0;
                if (oldInfoProductId > 0 && oldInfoProductId != newInfo?.ParseProductId())
                    productIds.Add(oldInfoProductId);
            }
            GeneratePackagesAndTriggerChangeEvent(productIds, changedSource);
        }

        private void OnLoadAllVersionsChanged(string packageName, bool _)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { packageName });
        }

        private void OnShowPreReleasePackagesOrSeeAllVersionsChanged(bool _)
        {
            var allPackageNames = m_UpmCache.installedPackageInfos.Join(m_UpmCache.discoverableSearchPackageInfos).Join(m_UpmCache.nonDiscoverableSearchPackageInfos).SelectToNewHashSet(p => p.name);
            GeneratePackagesAndTriggerChangeEvent(allPackageNames);
        }

        private IPackage CreateUpmPackage(string packageName, IUpmPackageData packageData, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            var tagsToExclude = m_SettingsProxy.seeAllPackageVersions ? PackageTag.None :
                m_SettingsProxy.enablePreReleasePackages ? PackageTag.Experimental : PackageTag.Experimental | PackageTag.PreRelease;
            var versionList = new UpmVersionList(packageData, tagsToExclude, m_IOProxy, m_ApplicationProxy, changedSource != PackagesChangedSource.AddAndRemove);
            var primaryVersion = versionList.primary;
            if (primaryVersion == null)
                return null;
            if (!primaryVersion.isFullyFetched)
                m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(primaryVersion.packageId);
            var package = CreatePackage(packageName, versionList, isDiscoverable: packageData.isDiscoverable, isDeprecated: packageData.isDeprecated, deprecationMessage: packageData.deprecationMessage, compliance: packageData.compliance);
            return package;
        }
    }
}
