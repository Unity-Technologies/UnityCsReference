// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AssetStorePackageFactory : Package.Factory
    {
        [NonSerialized]
        private UniqueIdMapper m_UniqueIdMapper;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private FetchStatusTracker m_FetchStatusTracker;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(UniqueIdMapper uniqueIdMapper,
            UnityConnectProxy unityConnect,
            AssetStoreCache assetStoreCache,
            AssetStoreClientV2 assetStoreClient,
            AssetStoreDownloadManager assetStoreDownloadManager,
            PackageDatabase packageDatabase,
            FetchStatusTracker fetchStatusTracker,
            IOProxy ioProxy)
        {
            m_UniqueIdMapper = uniqueIdMapper;
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
            m_FetchStatusTracker = fetchStatusTracker;
            m_IOProxy = ioProxy;
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;
            m_AssetStoreCache.onUpdateInfosChanged += OnUpdateInfosChanged;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError += OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadStateChanged += OnDownloadStateChanged;
            m_AssetStoreDownloadManager.onBeforeDownloadStart += OnBeforeDownloadStart;

            m_FetchStatusTracker.onFetchStatusChanged += OnFetchStatusChanged;
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
            m_AssetStoreCache.onPurchaseInfosChanged -= OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged -= OnProductInfoChanged;
            m_AssetStoreCache.onUpdateInfosChanged -= OnUpdateInfosChanged;

            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError -= OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadStateChanged -= OnDownloadStateChanged;
            m_AssetStoreDownloadManager.onBeforeDownloadStart -= OnBeforeDownloadStart;

            m_FetchStatusTracker.onFetchStatusChanged -= OnFetchStatusChanged;
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (loggedIn)
                return;

            m_AssetStoreCache.ClearCache();
            m_FetchStatusTracker.ClearCache();

            var notInstalledAssetStorePackages = m_PackageDatabase.allPackages.Where(p => p.product != null && p.versions.installed == null);
            // We use `ToArray` here as m_PackageDatabase.UpdatePackages will modify the enumerable and throw an error if we don't
            var packageToRemove = notInstalledAssetStorePackages.Select(p => p.uniqueId).ToArray();
            m_PackageDatabase.UpdatePackages(toRemove: packageToRemove);
        }

        private void AddPackageError(Package package, UIError error)
        {
            AddError(package, error);
            m_PackageDatabase.OnPackagesModified(new[] { package });
        }

        private void SetPackagesProgress(IEnumerable<IPackage> packages, PackageProgress progress)
        {
            var packagesUpdated = packages.OfType<Package>().Where(p => p != null && p.progress != progress).ToList();
            foreach (var package in packagesUpdated)
                SetProgress(package, progress);
            if (packagesUpdated.Any())
                m_PackageDatabase.OnPackagesModified(packagesUpdated, true);
        }

        private void SetPackageProgress(IPackage package, PackageProgress progress)
        {
            SetPackagesProgress(new[] { package }, progress);
        }

        private void OnBeforeDownloadStart(long productId)
        {
            var package = m_PackageDatabase.GetPackage(productId) as Package;
            if (package == null)
                return;

            // When we start a new download, we want to clear past operation errors to give it a fresh start.
            // Eventually we want a better design on how to show errors, to be further addressed in https://jira.unity3d.com/browse/PAX-1332
            // We need to clear errors before calling download because Download can fail right away
            var numErrorsRemoved = ClearErrors(package, e => e.errorCode == UIErrorCode.AssetStoreOperationError);
            if (numErrorsRemoved > 0)
                m_PackageDatabase.OnPackagesModified(new[] { package });
        }

        private void OnDownloadStateChanged(AssetStoreDownloadOperation operation)
        {
            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId);
            if (package == null)
                return;

            if (operation.state == DownloadState.Pausing)
                SetPackageProgress(package, PackageProgress.Pausing);
            else if (operation.state == DownloadState.ResumeRequested)
                SetPackageProgress(package, PackageProgress.Resuming);
            else if (operation.state == DownloadState.Paused || operation.state == DownloadState.AbortRequested || operation.state == DownloadState.Aborted)
                SetPackageProgress(package, PackageProgress.None);
        }

        private void OnDownloadProgress(AssetStoreDownloadOperation operation)
        {
            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId);
            if (package == null)
                return;
            SetPackageProgress(package, operation.isInProgress ? PackageProgress.Downloading : PackageProgress.None);
        }

        private void OnDownloadFinalized(AssetStoreDownloadOperation operation)
        {
            // We want to call RefreshLocal() before calling GetPackage(operation.packageUniqueId),
            // because if we call GetPackage first, we might get an old instance of the package.
            // This is due to RefreshLocal potentially replacing the instance in the database with a new one.
            if (operation.state == DownloadState.Completed)
                m_AssetStoreClient.RefreshLocal();

            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId) as Package;
            if (package == null)
                return;

            if (operation.state == DownloadState.Error)
                AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, operation.errorMessage, UIError.Attribute.IsClearable));
            else if (operation.state == DownloadState.Aborted)
                AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, operation.errorMessage ?? L10n.Tr("Download aborted"), UIError.Attribute.IsWarning | UIError.Attribute.IsClearable));

            SetPackageProgress(package, PackageProgress.None);
        }

        private void OnDownloadError(AssetStoreDownloadOperation operation, UIError error)
        {
            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId) as Package;
            if (package == null)
                return;

            AddPackageError(package, error);
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            // Since users could have way more locally downloaded .unitypackages than what's in their purchase list
            // we don't want to trigger change events for all of them, only the ones we already checked before (the ones with productInfos)
            var productIds = addedOrUpdated?.Select(info => info.productId).Concat(removed.Select(info => info.productId) ?? new long[0])?.
                Where(id => m_AssetStoreCache.GetProductInfo(id) != null);
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }

        private void OnUpdateInfosChanged(IEnumerable<AssetStoreUpdateInfo> updateInfos)
        {
            // Right now updateInfo goes hands in hands with localInfo, so we handle it the same way as localInfo changes
            // and only check packages we already checked before (the ones with productInfos). This behaviour might change in the future
            GeneratePackagesAndTriggerChangeEvent(updateInfos?.Select(info => info.productId).Where(id => m_AssetStoreCache.GetProductInfo(id) != null));
        }

        private void OnProductInfoChanged(AssetStoreProductInfo productInfo)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { productInfo.productId });
        }

        private void OnPurchaseInfosChanged(IEnumerable<AssetStorePurchaseInfo> purchaseInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(purchaseInfos.Select(info => info.productId));
        }

        private void OnFetchStatusChanged(FetchStatus fetchStatus)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { fetchStatus.productId });
        }

        public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<long> productIds)
        {
            if (productIds?.Any() != true)
                return;

            var packagesChanged = new List<IPackage>();
            foreach (var productId in productIds)
            {
                var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
                var productInfo = m_AssetStoreCache.GetProductInfo(productId);
                if (purchaseInfo == null && productInfo == null)
                    continue;

                var packageName = string.IsNullOrEmpty(productInfo?.packageName) ? m_UniqueIdMapper.GetNameByProductId(productId) : productInfo.packageName;
                // ProductInfos with package names are handled in UpmOnAssetStorePackageFactory, we don't want to worry about it here.
                if (!string.IsNullOrEmpty(packageName))
                    continue;

                if (productInfo == null)
                {
                    var fetchStatus = m_FetchStatusTracker.GetOrCreateFetchStatus(productId);
                    var productInfoFetchError = fetchStatus.GetFetchError(FetchType.ProductInfo);
                    var version = new PlaceholderPackageVersion(productId.ToString(), purchaseInfo.displayName, tag: PackageTag.LegacyFormat, error: productInfoFetchError?.error);
                    var placeholderPackage = CreatePackage(string.Empty, new PlaceholderVersionList(version), new Product(productId, null, null));
                    if (productInfoFetchError == null)
                        SetProgress(placeholderPackage, PackageProgress.Refreshing);
                    packagesChanged.Add(placeholderPackage);
                    continue;
                }
                var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
                var updateInfo = m_AssetStoreCache.GetUpdateInfo(productId);
                var versionList = new AssetStoreVersionList(m_IOProxy, productInfo, localInfo, updateInfo);
                var package = CreatePackage(string.Empty, versionList, new Product(productId, purchaseInfo, productInfo));
                if (m_AssetStoreDownloadManager.GetDownloadOperation(productId)?.isInProgress == true)
                    SetProgress(package, PackageProgress.Downloading);
                packagesChanged.Add(package);
            }
            if (packagesChanged.Any())
                m_PackageDatabase.UpdatePackages(packagesChanged);
        }
    }
}
