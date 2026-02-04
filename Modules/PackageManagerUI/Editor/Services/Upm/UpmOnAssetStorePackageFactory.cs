// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmOnAssetStorePackageFactory : Package.Factory
    {
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IFetchStatusTracker m_FetchStatusTracker;
        private readonly IUpmCache m_UpmCache;
        private readonly IUpmRegistryClient m_UpmRegistryClient;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        public UpmOnAssetStorePackageFactory(IUnityConnectProxy unityConnect,
            IAssetStoreCache assetStoreCache,
            IBackgroundFetchHandler backgroundFetchHandler,
            IPackageDatabase packageDatabase,
            IFetchStatusTracker fetchStatusTracker,
            IUpmCache upmCache,
            IUpmRegistryClient upmRegistryClient,
            IProjectSettingsProxy settingsProxy)
        {
            m_UnityConnect = RegisterDependency(unityConnect);
            m_AssetStoreCache = RegisterDependency(assetStoreCache);
            m_BackgroundFetchHandler = RegisterDependency(backgroundFetchHandler);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
            m_UpmCache = RegisterDependency(upmCache);
            m_UpmRegistryClient = RegisterDependency(upmRegistryClient);
            m_SettingsProxy = RegisterDependency(settingsProxy);
        }

        public override void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_SettingsProxy.onEnablePreReleasePackagesChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;
            m_UpmCache.onLoadAllVersionsChanged += OnLoadAllVersionsChanged;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;

            m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;

            m_FetchStatusTracker.onFetchStatusChanged += OnFetchStatusChanged;
        }

        public override void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_SettingsProxy.onEnablePreReleasePackagesChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;

            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
            m_UpmCache.onExtraPackageInfoFetched -= OnExtraPackageInfoFetched;
            m_UpmCache.onLoadAllVersionsChanged -= OnLoadAllVersionsChanged;

            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;

            m_AssetStoreCache.onPurchaseInfosChanged -= OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged -= OnProductInfoChanged;

            m_FetchStatusTracker.onFetchStatusChanged -= OnFetchStatusChanged;
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal void GeneratePackagesAndTriggerChangeEvent(IReadOnlyCollection<long> productIds)
        {
            if (productIds.Count == 0)
                return;

            var packagesChanged = new List<IPackage>(productIds.Count);
            var tagsToExclude = m_SettingsProxy.seeAllPackageVersions ? PackageTag.None :
                m_SettingsProxy.enablePreReleasePackages ? PackageTag.Experimental : PackageTag.Experimental | PackageTag.PreRelease;

            foreach (var productId in productIds)
            {
                var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
                var productInfo = m_AssetStoreCache.GetProductInfo(productId);
                var packageData = m_UpmCache.GetPackageData(productId);

                // Unlike AssetStorePackageFactory or UpmPackageFactory, UpmOnAssetStorePackageFactory is specifically created to handle packages
                // with both product data and upm package data, so we skip all other cases here.
                // The case where we only have the purchase info and nothing else will be handled in AssetStorePackageFactory and a place holder package
                // will be generated there.
                var packageName = productInfo?.packageName ?? packageData?.name;
                if (string.IsNullOrEmpty(packageName))
                    continue;

                var fetchStatus = m_FetchStatusTracker.GetOrCreateFetchStatus(productId);
                var productSearchInfoFetchError = fetchStatus.GetFetchError(FetchType.ProductSearchInfo);

                Package package = null;
                if ((productSearchInfoFetchError?.error.errorCode == UIErrorCode.UpmError_NotFound || (packageData != null && packageData.availableRegistryType != RegistryType.AssetStore))
                    && HasMatchingScopedRegistry(packageName, out var registry) )
                {
                    var versionString = productInfo?.versionString ?? packageData?.installedInfo?.version ?? packageData?.mainSearchInfo?.version ?? string.Empty;
                    var displayName = productInfo?.displayName ?? packageData?.installedInfo?.displayName ?? packageData?.mainSearchInfo?.displayName ?? string.Empty;
                    var errorMessage = L10n.Tr(
                        "This package is not accessible due to scope conflict with the \"{0}\" scoped registry. Please remove the conflicting entry in your Project Settings to restore access to this package on Asset Store.");
                    var error = new UIError(UIErrorCode.AssetStorePackageError, string.Format(errorMessage, registry.name));
                    var version = new PlaceholderPackageVersion($"{packageName}@{versionString}", displayName, versionString, PackageTag.UpmFormat, error);
                    package = CreatePackage(packageName, new PlaceholderVersionList(version), new Product(productId, purchaseInfo, productInfo));
                }
                else if (packageData != null)
                {
                    var productInfoFetchError = fetchStatus.GetFetchError(FetchType.ProductInfo);
                    if (productInfo == null && productInfoFetchError == null && !fetchStatus.IsFetchInProgress(FetchType.ProductInfo))
                    {
                        m_BackgroundFetchHandler.AddToFetchProductInfoQueue(productId);
                        m_BackgroundFetchHandler.AddToFetchPurchaseInfoQueue(productId);
                        continue;
                    }

                    if (packageData.mainSearchInfo == null && !fetchStatus.IsFetchInProgress(FetchType.ProductSearchInfo))
                        m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(packageName, productId);

                    var versionList = new UpmVersionList(packageData, tagsToExclude);
                    package = CreatePackage(packageName, versionList, new Product(productId, purchaseInfo, productInfo), isDeprecated: packageData.isDeprecated, deprecationMessage: packageData.deprecationMessage);
                    if (productInfoFetchError != null)
                        AddError(package, productInfoFetchError.error);
                    else if (productInfo == null && fetchStatus.IsFetchInProgress(FetchType.ProductInfo))
                        SetProgress(package, PackageProgress.Refreshing);
                    else if (packageData.mainSearchInfo != null && packageData.mainSearchInfo.ParseProductId() != productId)
                    {
                        // This is not really supposed to happen - this happening would mean there's an issue with data from the backend
                        // Right now there isn't any recommended actions we can suggest the users to take, so we'll just add a message here
                        // to expose it if it ever happens (rather than letting it pass silently)
                        var errorMessage = L10n.Tr("Product Id mismatch between product details and package details. Please try to refresh in a few minutes or report the issue.");
                        AddError(package, new UIError(UIErrorCode.AssetStorePackageError, errorMessage, UIError.Attribute.Warning));
                    }
                }
                else if (productInfo != null)
                {
                    if (productSearchInfoFetchError != null)
                    {
                        var version = new PlaceholderPackageVersion($"{packageName}@{productInfo.versionString}", productInfo.displayName, productInfo.versionString, PackageTag.UpmFormat, productSearchInfoFetchError.error);
                        package = CreatePackage(packageName, new PlaceholderVersionList(version), new Product(productId, purchaseInfo, productInfo));
                    }
                    else
                    {
                        var version = new PlaceholderPackageVersion($"{packageName}@{productInfo.versionString}", productInfo.displayName, productInfo.versionString, PackageTag.UpmFormat);
                        package = CreatePackage(packageName, new PlaceholderVersionList(version), new Product(productId, purchaseInfo, productInfo));
                        SetProgress(package, PackageProgress.Refreshing);

                        if (!fetchStatus.IsFetchInProgress(FetchType.ProductSearchInfo))
                            m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(packageName, productId);
                    }
                }

                if (package == null)
                    continue;

                // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                // since the primary version's display name is used in the package list
                var primaryVersion = package.versions.primary;
                if (primaryVersion?.isFullyFetched == false)
                    m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(primaryVersion.packageId);

                packagesChanged.Add(package);
            }

            if (packagesChanged.Count > 0)
                m_PackageDatabase.UpdatePackages(packagesChanged);
        }

        private bool HasMatchingScopedRegistry(string packageName, out RegistryInfo scopedRegistry)
        {
            scopedRegistry = m_SettingsProxy.scopedRegistries.FirstMatch(r => r.AnyScopeMatchesPackageName(packageName));
            return scopedRegistry != null;
        }

        private void OnProductInfoChanged(AssetStoreProductInfo productInfo)
        {
            GeneratePackagesAndTriggerChangeEvent(new [] { productInfo.productId });
        }

        private void OnPurchaseInfosChanged(IReadOnlyCollection<AssetStorePurchaseInfo> purchaseInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(purchaseInfos.SelectToNewArray(info => info.productId));
        }

        private void OnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> updatedInfos, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            var productIds = new List<long>();
            foreach (var (oldInfo, newInfo) in updatedInfos)
            {
                var oldInfoProductId = oldInfo?.ParseProductId() ?? 0;
                var newInfoProductId = newInfo?.ParseProductId() ?? 0;
                if (oldInfoProductId > 0)
                    productIds.Add(oldInfoProductId);
                if (newInfoProductId > 0 && newInfoProductId != oldInfoProductId)
                    productIds.Add(newInfoProductId);
            }
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }

        private void OnExtraPackageInfoFetched(PackageInfo packageInfo)
        {
            // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
            var productId = packageInfo.ParseProductId();
            if (productId > 0 && m_UpmCache.GetInstalledPackageInfo(packageInfo.name)?.packageId != packageInfo.packageId)
                GeneratePackagesAndTriggerChangeEvent(new [] { productId });
        }

        private void OnLoadAllVersionsChanged(string packageUniqueId, bool _)
        {
            if (long.TryParse(packageUniqueId, out var productId))
                GeneratePackagesAndTriggerChangeEvent(new [] { productId });
        }

        private void OnFetchStatusChanged(FetchStatus fetchStatus)
        {
            GeneratePackagesAndTriggerChangeEvent(new [] { fetchStatus.productId });
        }

        private void RegenerateExistingUpmOnAssetStorePackages()
        {
            var productIds = new List<long>();
            foreach (var p in m_PackageDatabase.allPackages)
                if (p.product != null && p.versions.AnyMatches(v => v.HasTag(PackageTag.UpmFormat)))
                    productIds.Add(p.product.id);
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }

        private void OnShowPreReleasePackagesOrSeeAllVersionsChanged(bool _)
        {
            RegenerateExistingUpmOnAssetStorePackages();
        }

        private void OnRegistriesModified()
        {
            // We need to clear the regenerate packages when scoped registries settings change as the old or new scoped registry setting
            // could shadow UPM packages from Asset Store. Therefore, we clear package info that could potentially contain product info
            // and the package factory will trigger the fetching of these information again.
            m_UpmCache.ClearProductCache();
            m_UpmCache.ClearExtraInfoCache();
            m_FetchStatusTracker.ClearCache();

            RegenerateExistingUpmOnAssetStorePackages();
        }

        private void OnUserLoginStateChange(bool _, bool loggedIn)
        {
            if (loggedIn)
                return;

            m_UpmCache.ClearProductCache();

            var packageIdsToRemove = new List<string>();
            var productIdsToGenerate = new List<long>();
            // We only regenerate and remove packages from the Asset Store that are of UPM format. We handle the legacy format in UpmPackageFactory.
            foreach (var p in m_PackageDatabase.allPackages)
                if (p.product != null && p.versions.AnyMatches(v => v.HasTag(PackageTag.UpmFormat)))
                {
                    if (p.versions.installed == null)
                        packageIdsToRemove.Add(p.uniqueId);
                    else
                        productIdsToGenerate.Add(p.product.id);
                }

            if (packageIdsToRemove.Count > 0)
                m_PackageDatabase.UpdatePackages(toRemove: packageIdsToRemove);

            GeneratePackagesAndTriggerChangeEvent(productIdsToGenerate);
        }
    }
}
