// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmPackageFactory : Package.Factory
    {
        private readonly IUpmCache m_UpmCache;
        private readonly IUpmClient m_UpmClient;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        public UpmPackageFactory(IUpmCache upmCache,
            IUpmClient upmClient,
            IBackgroundFetchHandler backgroundFetchHandler,
            IPackageDatabase packageDatabase,
            IProjectSettingsProxy settingsProxy)
        {
            m_UpmCache = RegisterDependency(upmCache);
            m_UpmClient = RegisterDependency(upmClient);
            m_BackgroundFetchHandler = RegisterDependency(backgroundFetchHandler);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_SettingsProxy = RegisterDependency(settingsProxy);
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

        private void OnPackagesProgressChange(IEnumerable<(string packageIdOrName, PackageProgress progress)> progressUpdates)
        {
            var packagesUpdated = new List<IPackage>();
            foreach (var item in progressUpdates)
            {
                var package = m_PackageDatabase.GetPackageByIdOrName(item.packageIdOrName) as Package;

                // Special handling for installing a package that's not already in the database. This case is most likely to happen
                // when a user adds a git or local package through the special add package UI.
                if (package == null && item.progress == PackageProgress.Installing)
                {
                    var version = new PlaceholderPackageVersion(item.packageIdOrName, L10n.Tr("Installing a new package"), tag: PackageTag.UpmFormat | PackageTag.SpecialInstall);
                    var placeholderPackage = CreatePackage(item.packageIdOrName, new PlaceholderVersionList(version));
                    SetProgress(placeholderPackage, PackageProgress.Installing);
                    m_PackageDatabase.UpdatePackages(new[] { placeholderPackage });
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

        private void OnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> updatedInfos)
        {
            var packageNames = updatedInfos.Select(i => i.oldInfo?.name ?? i.newInfo?.name).ToArray();
            GeneratePackagesAndTriggerChangeEvent(packageNames);
        }

        private void OnLoadAllVersionsChanged(string packageUniqueId, bool _)
        {
            if (!long.TryParse(packageUniqueId, out var _))
                GeneratePackagesAndTriggerChangeEvent(new[] { packageUniqueId });
        }

        private void OnShowPreReleasePackagesOrSeeAllVersionsChanged(bool _)
        {
            var allPackageNames = m_UpmCache.installedPackageInfos.Concat(m_UpmCache.searchPackageInfos).Select(p => p.name).ToHashSet();
            GeneratePackagesAndTriggerChangeEvent(allPackageNames);
        }

        public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<string> packageNames)
        {
            if (packageNames?.Any() != true)
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
                    if (!versionList.Any())
                    {
                        packagesToRemove.Add(packageName);
                        continue;
                    }

                    var package = CreatePackage(packageName, versionList, isDiscoverable: packageData.isDiscoverable, isDeprecated: packageData.isDeprecated, deprecationMessage: packageData.deprecationMessage);
                    updatedPackages.Add(package);

                    // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                    // since the primary version's display name is used in the package list
                    var primaryVersion = package.versions.primary;
                    if (primaryVersion?.isFullyFetched == false)
                        m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(primaryVersion.packageId);
                }
            }

            if (updatedPackages.Any() || packagesToRemove.Any())
                m_PackageDatabase.UpdatePackages(toAddOrUpdate: updatedPackages, toRemove: packagesToRemove);
        }
    }

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

        private void OnShowPreReleasePackagesOrSeeAllVersionsChanged(bool _)
        {
            var allProductIds = m_PackageDatabase.allPackages
                .Where(p => p.product != null && p.versions.Any(v => v.HasTag(PackageTag.UpmFormat)))
                .Select(p => p.product.id);
            GeneratePackagesAndTriggerChangeEvent(allProductIds);
        }

        public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<long> productIds)
        {
            if (productIds?.Any() != true)
                return;

            var packagesChanged = new List<IPackage>();
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
            if (packagesChanged.Any())
                m_PackageDatabase.UpdatePackages(packagesChanged);
        }

        private bool HasMatchingScopedRegistry(string packageName, out RegistryInfo scopedRegistry)
        {
            scopedRegistry = m_SettingsProxy.scopedRegistries.FirstOrDefault(r => r.AnyScopeMatchesPackageName(packageName));
            return scopedRegistry != null;
        }

        private void OnProductInfoChanged(AssetStoreProductInfo productInfo)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { productInfo.productId });
        }

        private void OnPurchaseInfosChanged(IEnumerable<AssetStorePurchaseInfo> purchaseInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(purchaseInfos.Select(info => info.productId));
        }

        private void OnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> updatedInfos)
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
                GeneratePackagesAndTriggerChangeEvent(new[] { productId });
        }

        private void OnLoadAllVersionsChanged(string packageUniqueId, bool _)
        {
            if (long.TryParse(packageUniqueId, out var productId))
                GeneratePackagesAndTriggerChangeEvent(new[] { productId });
        }

        private void OnFetchStatusChanged(FetchStatus fetchStatus)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { fetchStatus.productId });
        }

        private void OnRegistriesModified()
        {
            // We need to clear the regenerate packages when scoped registries settings change as the old or new scoped registry setting
            // could shadow UPM packages from Asset Store. Therefore, we clear package info that could potentially contain product info
            // and the package factory will trigger the fetching of these information again.
            m_UpmCache.ClearProductCache();
            m_UpmCache.ClearExtraInfoCache();
            m_FetchStatusTracker.ClearCache();

            var productIds = m_PackageDatabase.allPackages
                .Where(p => p.product != null && p.versions.Any(v => v.HasTag(PackageTag.UpmFormat)))
                .Select(p => p.product.id).ToArray();
            if (productIds.Length > 0)
                GeneratePackagesAndTriggerChangeEvent(productIds);
        }

        private void OnUserLoginStateChange(bool _, bool loggedIn)
        {
            if (loggedIn)
                return;
            m_UpmCache.ClearProductCache();

            // We only remove packages from the Asset Store that are of UPM format. We handle the Legacy format in AssetStorePackageFactory.
            // Also, we use `ToArray` here as m_PackageDatabase.UpdatePackages will modify the enumerable and throw an error if we don't.
            var packagesToRemove = m_PackageDatabase.allPackages
                .Where(p => p.product != null && p.versions.Any(v => v.HasTag(PackageTag.UpmFormat)) &&
                            p.versions.installed == null).Select(p => p.uniqueId).ToArray();

            if (packagesToRemove.Any())
                m_PackageDatabase.UpdatePackages(toRemove: packagesToRemove);

            var productIds = m_UpmCache.installedPackageInfos.Select(info => info.ParseProductId()).Where(id => id > 0);
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }
    }
}
