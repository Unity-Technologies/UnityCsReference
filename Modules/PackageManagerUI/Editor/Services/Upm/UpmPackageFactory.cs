// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmPackageFactory : Package.Factory
    {
        private readonly IUpmCache m_UpmCache;
        private readonly IUpmClient m_UpmClient;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IPackageCreator m_PackageCreator;
        public UpmPackageFactory(IUpmCache upmCache,
            IUpmClient upmClient,
            IBackgroundFetchHandler backgroundFetchHandler,
            IPackageDatabase packageDatabase,
            IProjectSettingsProxy settingsProxy,
            IPackageCreator packageCreator)
        {
            m_UpmCache = RegisterDependency(upmCache);
            m_UpmClient = RegisterDependency(upmClient);
            m_BackgroundFetchHandler = RegisterDependency(backgroundFetchHandler);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_SettingsProxy = RegisterDependency(settingsProxy);
            m_PackageCreator = RegisterDependency(packageCreator);
        }

        public override void OnEnable()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;
            m_UpmCache.onLoadAllVersionsChanged += OnLoadAllVersionsChanged;

            m_UpmClient.onPackagesProgressChange += OnPackagesProgressChange;
            m_UpmClient.onPackageOperationError += OnPackageOperationError;
            m_UpmClient.onSpecialInstallStart += OnSpecialInstallStart;
            m_UpmClient.onSpecialInstallFinalize += OnSpecialInstallFinalize;

            m_PackageCreator.onPackageCreated += OnPackageCreated;
        }

        public override void OnDisable()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched -= OnExtraPackageInfoFetched;
            m_UpmCache.onLoadAllVersionsChanged -= OnLoadAllVersionsChanged;

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
            // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
            var productId = packageInfo.assetStore?.productId;
            if (string.IsNullOrEmpty(productId) && m_UpmCache.GetInstalledPackageInfoByName(packageInfo.name)?.packageId != packageInfo.packageId)
                GeneratePackagesAndTriggerChangeEvent(new[] { packageInfo.name });
        }

        private void OnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> updatedInfos, PackagesChangedSource changedSource)
        {
            var packageNames = updatedInfos.SelectToNewArray(i => i.oldInfo?.name ?? i.newInfo?.name);
            GeneratePackagesAndTriggerChangeEvent(packageNames, changedSource);
        }

        private void OnLoadAllVersionsChanged(string packageUniqueId, bool _)
        {
            if (!long.TryParse(packageUniqueId, out var _))
                GeneratePackagesAndTriggerChangeEvent(new[] { packageUniqueId });
        }

        private void OnShowPreReleasePackagesOrSeeAllVersionsChanged(bool _)
        {
            var allPackageNames = m_UpmCache.installedPackageInfos.Join(m_UpmCache.searchPackageInfos).SelectToNewHashSet(p => p.name);
            GeneratePackagesAndTriggerChangeEvent(allPackageNames);
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal void GeneratePackagesAndTriggerChangeEvent(IReadOnlyCollection<string> packageNames, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            if (packageNames == null || packageNames.Count == 0)
                return;

            var updatedPackages = new List<IPackage>();
            var packagesToRemove = new List<string>();
            var tagsToExclude = m_SettingsProxy.seeAllPackageVersions ? PackageTag.None :
                m_SettingsProxy.enablePreReleasePackages ? PackageTag.Experimental : PackageTag.Experimental | PackageTag.PreRelease;
            foreach (var packageName in packageNames)
            {
                var packageData = m_UpmCache.GetPackageData(packageName);
                if (packageData == null)
                    packagesToRemove.Add(packageName);
                else if (packageData.installedInfo?.ParseProductId() > 0)
                {
                    // It could happen that an installed package changed source from scoped registry to Asset Store when user modify scoped registry settings
                    // In that case, a package that points to the scoped registry (with package name as unique id) should have already been created, and we need
                    // to remove that package, otherwise two entries of the same package will exist at the same time (the other one's unique id is product id).
                    // Note that we only do this when a package is switched from scoped registry to Asset Store. When it's the other way around, we don't need
                    // to remove the package with product id as unique id, since it's expected to have two packages then, one points to scoped registry with no error
                    // The other one points to the asset store with error saying that the asset store product is no longer accessible due to scoped registry settings.
                    packagesToRemove.Add(packageName);
                }
                else
                {
                    var versionList = new UpmVersionList(packageData, tagsToExclude);
                    var primaryVersion = versionList.primary;
                    if (primaryVersion == null)
                    {
                        packagesToRemove.Add(packageName);
                        continue;
                    }

                    var package = CreatePackage(packageName, versionList, isDiscoverable: packageData.isDiscoverable, isDeprecated: packageData.isDeprecated, deprecationMessage: packageData.deprecationMessage, compliance: packageData.compliance);
                    updatedPackages.Add(package);

                    // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                    // since the primary version's display name is used in the package list
                    if (primaryVersion?.isFullyFetched == false)
                        m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(primaryVersion.packageId);
                }
            }

            if (updatedPackages.Count > 0 || packagesToRemove.Count > 0)
                m_PackageDatabase.UpdatePackages(toAddOrUpdate: updatedPackages, toRemove: packagesToRemove, changedSource);
        }
    }
}
