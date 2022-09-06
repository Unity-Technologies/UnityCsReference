// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AssetStorePackageFactory : BasePackage.Factory
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
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;
        [NonSerialized]
        private FetchStatusTracker m_FetchStatusTracker;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(UniqueIdMapper uniqueIdMapper,
            UnityConnectProxy unityConnect,
            AssetStoreCache assetStoreCache,
            AssetStoreClient assetStoreClient,
            AssetStoreDownloadManager assetStoreDownloadManager,
            PackageDatabase packageDatabase,
            AssetStoreUtils assetStoreUtils,
            FetchStatusTracker fetchStatusTracker,
            UpmCache upmCache,
            IOProxy ioProxy)
        {
            m_UniqueIdMapper = uniqueIdMapper;
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
            m_AssetStoreUtils = assetStoreUtils;
            m_FetchStatusTracker = fetchStatusTracker;
            m_UpmCache = upmCache;
            m_IOProxy = ioProxy;
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;
            m_AssetStoreCache.onUpdatesFound += OnUpdatesFound;

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
            m_AssetStoreCache.onUpdatesFound -= OnUpdatesFound;

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

            var notInstalledAssetStorePackages = m_PackageDatabase.allPackages.Where(p => p.Is(PackageType.AssetStore) && p.versions.installed == null);
            m_PackageDatabase.UpdatePackages(toRemove: notInstalledAssetStorePackages.Select(p => p.uniqueId));
        }

        private void AddPackageError(BasePackage package, UIError error)
        {
            AddError(package, error);
            m_PackageDatabase.OnPackagesModified(new[] { package });
        }

        private void SetPackagesProgress(IEnumerable<IPackage> packages, PackageProgress progress)
        {
            var packagesUpdated = packages.OfType<BasePackage>().Where(p => p != null && p.progress != progress).ToList();
            foreach (var package in packagesUpdated)
                SetProgress(package, progress);
            if (packagesUpdated.Any())
                m_PackageDatabase.OnPackagesModified(packagesUpdated, true);
        }

        private void SetPackageProgress(IPackage package, PackageProgress progress)
        {
            SetPackagesProgress(new[] { package }, progress);
        }

        private void OnBeforeDownloadStart(string productId)
        {
            var package = m_PackageDatabase.GetPackage(productId) as BasePackage;
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
            else if (operation.state == DownloadState.Paused || operation.state == DownloadState.AbortRequsted || operation.state == DownloadState.Aborted)
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

            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId) as BasePackage;
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
            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId) as BasePackage;
            if (package == null)
                return;

            AddPackageError(package, error);
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            // Since users could have way more locally downloaded .unitypackages than what's in their purchase list
            // we don't want to trigger change events for all of them, only the ones we already checked before (the ones with productInfos)
            var productIds = addedOrUpdated?.Select(info => info.id).Concat(removed.Select(info => info.id) ?? new string[0])?.
                Where(id => m_AssetStoreCache.GetProductInfo(id) != null);
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }

        private void OnUpdatesFound(IEnumerable<AssetStoreUpdateInfo> updateInfos)
        {
            // Right now updateInfo goes hands in hands with localInfo, so we handle it the same way as localInfo changes
            // and only check packages we already checked before (the ones with productInfos). This behaviour might change in the future
            GeneratePackagesAndTriggerChangeEvent(updateInfos?.Select(info => info.productId).Where(id => m_AssetStoreCache.GetProductInfo(id) != null));
        }

        private void OnProductInfoChanged(AssetStoreProductInfo productInfo)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { productInfo.id });
        }

        private void OnPurchaseInfosChanged(IEnumerable<AssetStorePurchaseInfo> purchaseInfos)
        {
            GeneratePackagesAndTriggerChangeEvent(purchaseInfos.Select(info => info.productId.ToString()));
        }

        private void OnFetchStatusChanged(FetchStatus fetchStatus)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { fetchStatus.productId });
        }

        public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<string> productIds)
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
                    if (productInfoFetchError != null)
                        packagesChanged.Add(new PlaceholderPackage(productId, purchaseInfo.displayName, PackageType.AssetStore, PackageTag.Downloadable, PackageProgress.None, productInfoFetchError.error, productId: productId));
                    else
                        packagesChanged.Add(new PlaceholderPackage(productId, purchaseInfo.displayName, PackageType.AssetStore, PackageTag.Downloadable, PackageProgress.Refreshing, productId: productId));
                    continue;
                }
                var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
                var updateInfo = m_AssetStoreCache.GetUpdateInfo(localInfo?.uploadId);
                var versionList = new AssetStoreVersionList(m_AssetStoreUtils, m_IOProxy, productInfo, localInfo, updateInfo);
                var package = new AssetStorePackage(purchaseInfo, productInfo, versionList);
                if (m_AssetStoreDownloadManager.GetDownloadOperation(package.uniqueId)?.isInProgress == true)
                    SetProgress(package, PackageProgress.Downloading);
                packagesChanged.Add(package);
            }
            if (packagesChanged.Any())
                m_PackageDatabase.UpdatePackages(packagesChanged);
        }
    }
}
