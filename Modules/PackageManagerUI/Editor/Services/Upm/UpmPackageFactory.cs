// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmPackageFactory : BasePackage.Factory
    {
        [NonSerialized]
        private UniqueIdMapper m_UniqueIdMapper;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(UniqueIdMapper uniqueIdMapper,
            UpmCache upmCache,
            UpmClient upmClient,
            PackageDatabase packageDatabase,
            PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_UniqueIdMapper = uniqueIdMapper;
            m_UpmCache = upmCache;
            m_UpmClient = upmClient;
            m_PackageDatabase = packageDatabase;
            m_SettingsProxy = settingsProxy;
        }

        public void OnEnable()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged += OnShowPreReleasePackagesesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged += OnShowPreReleasePackagesesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;

            m_UpmClient.onPackagesProgressChange += OnPackagesProgressChange;
            m_UpmClient.onPackageOperationError += OnPackageOperationError;
        }

        public void OnDisable()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged -= OnShowPreReleasePackagesesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged -= OnShowPreReleasePackagesesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched -= OnExtraPackageInfoFetched;

            m_UpmClient.onPackagesProgressChange += OnPackagesProgressChange;
            m_UpmClient.onPackageOperationError += OnPackageOperationError;
        }

        private void OnPackageOperationError(string packageIdOrName, UIError error)
        {
            var package = m_PackageDatabase.GetPackageByIdOrName(packageIdOrName) as BasePackage;
            if (package == null)
                return;

            if (package.versions.primary.HasTag(PackageTag.SpecialInstall))
            {
                m_PackageDatabase.UpdatePackages(toRemove: new[] { package.uniqueId });
                if (!error.HasAttribute(UIError.Attribute.IsDetailInConsole))
                {
                    UnityEngine.Debug.Log(error.message);
                    error.attribute |= UIError.Attribute.IsDetailInConsole;
                }
                return;
            }
            AddError(package, error);
            m_PackageDatabase.OnPackagesModified(new[] { package });
        }

        private void OnPackagesProgressChange(IEnumerable<(string packageIdOrName, PackageProgress progress)> progressUpdates)
        {
            var packagesUpdated = new List<IPackage>();
            foreach (var item in progressUpdates)
            {
                var package = m_PackageDatabase.GetPackageByIdOrName(item.packageIdOrName) as BasePackage;

                // Special handling for installing a package that's not already in the database. This case is most likely to happen
                // when a user adds a git or local package through the special add package UI.
                if (package == null && item.progress == PackageProgress.Installing)
                {
                    var placeholerPackage = new PlaceholderPackage(item.packageIdOrName, L10n.Tr("Adding a new package"), PackageType.Upm, PackageTag.SpecialInstall, progress: PackageProgress.Installing);
                    m_PackageDatabase.UpdatePackages(new[] { placeholerPackage });
                    continue;
                }

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
            if (packagesUpdated.Any())
                m_PackageDatabase.OnPackagesModified(packagesUpdated, true);
        }

        private void OnExtraPackageInfoFetched(PackageInfo packageInfo)
        {
            // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
            var productId = packageInfo.assetStore?.productId;
            if (string.IsNullOrEmpty(productId) && m_UpmCache.GetInstalledPackageInfo(packageInfo.name)?.packageId != packageInfo.packageId)
                GeneratePackagesAndTriggerChangeEvent(new[] { packageInfo.name });
        }

        private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(packageInfos.Select(p => p.name));
        }

        private void OnShowPreReleasePackagesesOrSeeAllVersionsChanged(bool _)
        {
            var allPackageNames = m_UpmCache.installedPackageInfos.Concat(m_UpmCache.searchPackageInfos).Select(p => p.name).ToHashSet();
            GeneratePackagesAndTriggerChangeEvent(allPackageNames);
        }

        public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<string> packageNames)
        {
            if (packageNames?.Any() != true)
                return;

            var updatedPackages = new List<UpmPackage>();
            var packagesToRemove = new List<string>();
            var showPreRelease = m_SettingsProxy.enablePreReleasePackages;
            var seeAllVersions = m_SettingsProxy.seeAllPackageVersions;
            foreach (var packageName in packageNames)
            {
                // Upm packages with product ids are handled in UpmOnAssetStorePackageFactory, we don't want to worry about it here.
                if (!string.IsNullOrEmpty(m_UniqueIdMapper.GetProductIdByName(packageName)))
                    continue;
                var installedInfo = m_UpmCache.GetInstalledPackageInfo(packageName);
                var searchInfo = m_UpmCache.GetSearchPackageInfo(packageName);
                if (installedInfo == null && searchInfo == null)
                    updatedPackages.Add(new UpmPackage(packageName, false, new UpmVersionList()));
                else
                {
                    var isUnityPackage = m_UpmClient.IsUnityPackage(searchInfo ?? installedInfo);
                    var extraVersions = m_UpmCache.GetExtraPackageInfos(packageName);
                    var versionList = new UpmVersionList(searchInfo, installedInfo, isUnityPackage, extraVersions);
                    var filteredVersionList = LifecycleVersonsFilter.GetFilteredVersionList(versionList, seeAllVersions, showPreRelease);
                    if (!filteredVersionList.Any())
                    {
                        packagesToRemove.Add(packageName);
                        continue;
                    }

                    var package = new UpmPackage(packageName, searchInfo != null, filteredVersionList);
                    updatedPackages.Add(package);

                    // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                    // since the primary version's display name is used in the package list
                    var primaryVersion = package.versions.primary;
                    if (primaryVersion?.isFullyFetched == false)
                        m_UpmClient.ExtraFetch(primaryVersion.packageId);
                }
            }

            if (updatedPackages.Any())
                m_PackageDatabase.UpdatePackages(toAddOrUpdate: updatedPackages, toRemove: packagesToRemove);
        }
    }

    internal class UpmOnAssetStorePackageFactory : BasePackage.Factory
    {
        [NonSerialized]
        private UniqueIdMapper m_UniqueIdMapper;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private AssetStoreClient m_AssetStoreClient;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private FetchStatusTracker m_FetchStatusTracker;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private UpmClient m_UpmClient;
        public void ResolveDependencies(UniqueIdMapper uniqueIdMapper,
            UnityConnectProxy unityConnect,
            AssetStoreCache assetStoreCache,
            AssetStoreClient assetStoreClient,
            PackageDatabase packageDatabase,
            FetchStatusTracker fetchStatusTracker,
            UpmCache upmCache,
            UpmClient upmClient)
        {
            m_UniqueIdMapper = uniqueIdMapper;
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreClient = assetStoreClient;
            m_PackageDatabase = packageDatabase;
            m_FetchStatusTracker = fetchStatusTracker;
            m_UpmCache = upmCache;
            m_UpmClient = upmClient;
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;

            m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;

            m_FetchStatusTracker.onFetchStatusChanged += OnFetchStatusChanged;

            RestartInProgressFetches();
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched -= OnExtraPackageInfoFetched;

            m_AssetStoreCache.onPurchaseInfosChanged -= OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged -= OnProductInfoChanged;

            m_FetchStatusTracker.onFetchStatusChanged -= OnFetchStatusChanged;
        }

        private void FetchPurchaseAndProductInfo(string productId)
        {
            if (!long.TryParse(productId, out var id))
                return;
            m_AssetStoreClient.FetchPurchaseInfoWithRetry(id);
            m_AssetStoreClient.FetchDetail(id);
        }

        private void RestartInProgressFetches()
        {
            foreach (var fetchStatus in m_FetchStatusTracker.fetchStatuses)
            {
                var productId = fetchStatus.productId;
                if ((fetchStatus.fetchingInProgress & FetchType.ProductInfo) != 0)
                    FetchPurchaseAndProductInfo(productId);

                if ((fetchStatus.fetchingInProgress & FetchType.ProductSearchInfo) != 0)
                {
                    var productInfo = m_AssetStoreCache.GetProductInfo(productId);
                    var packageName = string.IsNullOrEmpty(productInfo?.packageName) ? m_UniqueIdMapper.GetNameByProductId(productId) : productInfo.packageName;
                    if (string.IsNullOrEmpty(packageName))
                        continue;
                    m_UpmClient.SearchPackageInfoForProduct(productId, packageName);
                }
            }
        }

        public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<string> productIds)
        {
            if (productIds?.Any() != true)
                return;

            var packagesChanged = new List<IPackage>();
            var productIdsToFetch = new List<string>();
            var productIdAndNamesToSearch = new List<(string productId, string packageName)>();
            foreach (var productId in productIds)
            {
                var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
                var productInfo = m_AssetStoreCache.GetProductInfo(productId);

                var packageName = string.IsNullOrEmpty(productInfo?.packageName) ? m_UniqueIdMapper.GetNameByProductId(productId) : productInfo.packageName;
                // Unlike AssetStorePackageFactory or UpmPackageFactory, UpmOnAssetStorePackageFactory is specifically created to handle packages
                // with both productId and packageName, so we skip all other cases here.
                if (string.IsNullOrEmpty(packageName))
                    continue;

                var productSearchInfo = m_UpmCache.GetProductSearchPackageInfo(packageName);
                var installedPackageInfo = m_UpmCache.GetInstalledPackageInfo(packageName);
                var fetchStatus = m_FetchStatusTracker.GetOrCreateFetchStatus(productId);

                BasePackage package = null;
                if (productSearchInfo != null || installedPackageInfo != null)
                {
                    var productInfoFetchError = fetchStatus?.GetFetchError(FetchType.ProductInfo);
                    if (productInfo == null && productInfoFetchError == null && !fetchStatus.IsFetchInProgress(FetchType.ProductInfo))
                    {
                        productIdsToFetch.Add(productId);
                        continue;
                    }

                    if (productSearchInfo == null)
                        productIdAndNamesToSearch.Add((productId: productId, packageName: packageName));

                    var extraVersions = m_UpmCache.GetExtraPackageInfos(packageName);
                    var isUnityPackage = m_UpmClient.IsUnityPackage(productSearchInfo ?? installedPackageInfo);
                    var versionList = new UpmVersionList(productSearchInfo, installedPackageInfo, isUnityPackage, extraVersions);
                    package = new AssetStorePackage(packageName, productId, purchaseInfo, productInfo, versionList);
                    if (productInfoFetchError != null)
                        AddError(package, productInfoFetchError.error);
                    else if (productInfo == null && fetchStatus.IsFetchInProgress(FetchType.ProductInfo))
                        SetProgress(package, PackageProgress.Refreshing);
                }
                else if (productInfo != null)
                {
                    var productSearchInfoFetchError = fetchStatus.GetFetchError(FetchType.ProductSearchInfo);
                    if (productSearchInfoFetchError != null)
                    {
                        var version = new PlaceholderPackageVersion($"{packageName}@{productInfo.versionString}", productInfo.displayName, productInfo.versionString, PackageTag.Installable, productSearchInfoFetchError.error);
                        package = new AssetStorePackage(packageName, productId, purchaseInfo, productInfo, new PlaceholderVersionList(version));
                    }
                    else if (fetchStatus.IsFetchInProgress(FetchType.ProductSearchInfo))
                    {
                        var version = new PlaceholderPackageVersion($"{packageName}@{productInfo.versionString}", productInfo.displayName, productInfo.versionString, PackageTag.Installable);
                        package = new AssetStorePackage(packageName, productId, purchaseInfo, productInfo, new PlaceholderVersionList(version));
                        SetProgress(package, PackageProgress.Refreshing);
                    }
                    else
                        productIdAndNamesToSearch.Add((productId: productId, packageName: packageName));
                }

                // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                // since the primary version's display name is used in the package list
                var primaryVersion = package.versions.primary;
                if (primaryVersion?.isFullyFetched == false)
                    m_UpmClient.ExtraFetch(primaryVersion.packageId);

                if (package != null)
                    packagesChanged.Add(package);
            }
            if (packagesChanged.Any())
                m_PackageDatabase.UpdatePackages(packagesChanged);

            // We do the fetches needed at the end this function so the callbacks triggered does not mess with the package creation process
            foreach (var productId in productIdsToFetch)
                FetchPurchaseAndProductInfo(productId);

            foreach (var item in productIdAndNamesToSearch)
                m_UpmClient.SearchPackageInfoForProduct(item.productId, item.packageName);
        }

        private void OnProductInfoChanged(AssetStoreProductInfo productInfo)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { productInfo.id });
        }

        private void OnPurchaseInfosChanged(IEnumerable<AssetStorePurchaseInfo> purchaseInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(purchaseInfos.Select(info => info.productId.ToString()));
        }

        private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(packageInfos.Select(p => m_UniqueIdMapper.GetProductIdByName(p.name)).Where(id => !string.IsNullOrEmpty(id)));
        }

        private void OnExtraPackageInfoFetched(PackageInfo packageInfo)
        {
            // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
            var productId = packageInfo.assetStore?.productId;
            if (!string.IsNullOrEmpty(productId) && m_UpmCache.GetInstalledPackageInfo(packageInfo.name)?.packageId != packageInfo.packageId)
                GeneratePackagesAndTriggerChangeEvent(new[] { productId });
        }

        private void OnFetchStatusChanged(FetchStatus fetchStatus)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { fetchStatus.productId });
        }

        private void OnUserLoginStateChange(bool _, bool loggedIn)
        {
            if (loggedIn)
                return;
            m_UpmCache.ClearProductCache();

            var productIds = m_UpmCache.installedPackageInfos.Select(info => info.assetStore?.productId).Where(id => !string.IsNullOrEmpty(id));
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }
    }
}
