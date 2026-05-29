// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal partial class PackageFactory : Package.Factory
    {
        private readonly IUpmCache m_UpmCache;
        private readonly IUpmClient m_UpmClient;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IPackageCreator m_PackageCreator;

        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IFetchStatusTracker m_FetchStatusTracker;

        private readonly IUpmRegistryClient m_UpmRegistryClient;

        private readonly IIOProxy m_IOProxy;
        private readonly IApplicationProxy m_ApplicationProxy;

        public PackageFactory(
            IUpmCache upmCache,
            IUpmClient upmClient,
            IBackgroundFetchHandler backgroundFetchHandler,
            IPackageDatabase packageDatabase,
            IProjectSettingsProxy settingsProxy,
            IPackageCreator packageCreator,
            IUnityConnectProxy unityConnect,
            IAssetStoreCache assetStoreCache,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IFetchStatusTracker fetchStatusTracker,
            IUpmRegistryClient upmRegistryClient,
            IIOProxy ioProxy,
            IApplicationProxy application)
        {
            m_UpmCache = RegisterDependency(upmCache);
            m_UpmClient = RegisterDependency(upmClient);
            m_BackgroundFetchHandler = RegisterDependency(backgroundFetchHandler);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_SettingsProxy = RegisterDependency(settingsProxy);
            m_PackageCreator = RegisterDependency(packageCreator);
            m_UnityConnect = RegisterDependency(unityConnect);
            m_AssetStoreCache = RegisterDependency(assetStoreCache);
            m_AssetStoreDownloadManager = RegisterDependency(assetStoreDownloadManager);
            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
            m_UpmRegistryClient = RegisterDependency(upmRegistryClient);
            m_IOProxy = RegisterDependency(ioProxy);
            m_ApplicationProxy = RegisterDependency(application);
        }

        public override void OnEnable()
        {
            RegisterEventsForUpmPackages();
            RegisterEventsForAssetStorePackages();
            RegisterEventsForUpmOnAssetStorePackages();
        }

        public override void OnDisable()
        {
            UnregisterEventsForUpmPackages();
            UnregisterEventsForAssetStorePackages();
            UnregisterEventsForUpmOnAssetStorePackages();
        }

        private bool HasMatchingScopedRegistry(string packageName, out RegistryInfo scopedRegistry)
        {
            scopedRegistry = m_SettingsProxy.scopedRegistries.FirstMatch(r => r.AnyScopeMatchesPackageName(packageName));
            return scopedRegistry != null;
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal void GeneratePackagesAndTriggerChangeEvent(IReadOnlyCollection<string> packageNames, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            if (packageNames == null || packageNames.Count == 0)
                return;

            var packagesChanged = new List<IPackage>();
            var packagesToRemove = new List<string>();
            foreach (var packageName in packageNames)
            {
                var packageData = m_UpmCache.GetPackageData(packageName);
                if (packageData == null)
                {
                    packagesToRemove.Add(packageName);
                    continue;
                }

                var productId = packageData.mainSearchInfo?.ParseProductId() ?? packageData.installedInfo?.ParseProductId() ?? 0;
                if (productId > 0)
                {
                    packagesToRemove.Add(packageName);
                    var package = CreateUpmOnAssetStorePackage(productId, packageName, m_AssetStoreCache.GetProductInfo(productId), m_AssetStoreCache.GetPurchaseInfo(productId), packageData, changedSource);
                    if (package == null)
                        packagesToRemove.Add(productId.ToString());
                    else
                        packagesChanged.Add(package);
                }
                else
                {
                    // If a local/git/customized version of an UpmOnAssetStore package is installed, then the installed info won't contain any product information, unlike when they are installed from the registry directly.
                    // And since UpmOnAssetStore packages are not discoverable by default, we need to manually tigger a search operation so we can know for sure when the search result comes back
                    var isPotentiallyUpmOnAssetStore = packageData.mainSearchInfo == null &&
                                                       packageData.availableRegistryType == RegistryType.UnityRegistry &&
                                                       packageData.installedInfo is { source: PackageSource.Local or PackageSource.Embedded or PackageSource.LocalTarball or PackageSource.Git, unityLifecycle.isDiscoverable : false};
                    if (isPotentiallyUpmOnAssetStore && m_FetchStatusTracker.GetSearchInfoFetchStatus(packageName) is { inProgress: false, error: null })
                        m_BackgroundFetchHandler.AddToSearchNonDiscoverableQueue(packageName);

                    var package = CreateUpmPackage(packageName, packageData, changedSource);
                    if (package == null)
                        packagesToRemove.Add(packageName);
                    else
                        packagesChanged.Add(package);
                }
            }

            if (packagesChanged.Count > 0 || packagesToRemove.Count > 0)
                m_PackageDatabase.UpdatePackages(toAddOrUpdate: packagesChanged, toRemove: packagesToRemove, changedSource);
        }

        internal void GeneratePackagesAndTriggerChangeEvent(IReadOnlyCollection<long> productIds, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            if (productIds == null || productIds.Count == 0)
                return;

            var packagesChanged = new List<IPackage>(productIds.Count);
            var packagesToRemove = new List<string>();
            foreach (var productId in productIds)
            {
                var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
                var productInfo = m_AssetStoreCache.GetProductInfo(productId);
                var packageData = m_UpmCache.GetPackageData(productId);
                var packageName = productInfo?.packageName ?? packageData?.name;
                var package = !string.IsNullOrEmpty(packageName)
                    ? CreateUpmOnAssetStorePackage(productId, packageName, productInfo, purchaseInfo, packageData, changedSource)
                    : CreateAssetStorePackage(productId, purchaseInfo, productInfo, m_AssetStoreCache.GetImportedPackage(productId));
                if (package == null)
                    packagesToRemove.Add(productId.ToString());
                else
                    packagesChanged.Add(package);
            }

            if (packagesChanged.Count > 0 || packagesToRemove.Count > 0)
                m_PackageDatabase.UpdatePackages(packagesChanged, packagesToRemove, changedSource);
        }
    }
}
