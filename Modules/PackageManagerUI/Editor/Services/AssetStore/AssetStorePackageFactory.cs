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
        private readonly IUpmCache m_UpmCache;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IFetchStatusTracker m_FetchStatusTracker;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        public AssetStorePackageFactory(IUpmCache upmCache,
            IUnityConnectProxy unityConnect,
            IAssetStoreCache assetStoreCache,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IPackageDatabase packageDatabase,
            IFetchStatusTracker fetchStatusTracker,
            IBackgroundFetchHandler backgroundFetchHandler)
        {
            m_UpmCache = RegisterDependency(upmCache);
            m_UnityConnect = RegisterDependency(unityConnect);
            m_AssetStoreCache = RegisterDependency(assetStoreCache);
            m_AssetStoreDownloadManager = RegisterDependency(assetStoreDownloadManager);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
            m_BackgroundFetchHandler = RegisterDependency(backgroundFetchHandler);
        }

        public override void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;
            m_AssetStoreCache.onUpdateInfosChanged += OnUpdateInfosChanged;
            m_AssetStoreCache.onImportedPackagesChanged += OnImportedPackagesChanged;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError += OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadStateChanged += OnDownloadStateChanged;
            m_AssetStoreDownloadManager.onBeforeDownloadStart += OnBeforeDownloadStart;

            m_FetchStatusTracker.onFetchStatusChanged += OnFetchStatusChanged;
        }

        public override void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
            m_AssetStoreCache.onPurchaseInfosChanged -= OnPurchaseInfosChanged;
            m_AssetStoreCache.onProductInfoChanged -= OnProductInfoChanged;
            m_AssetStoreCache.onUpdateInfosChanged -= OnUpdateInfosChanged;
            m_AssetStoreCache.onImportedPackagesChanged -= OnImportedPackagesChanged;

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

            m_AssetStoreCache.ClearOnlineCache();
            m_FetchStatusTracker.ClearCache();

            // We only regenerate and remove packages from the Asset Store that are of Legacy format. We handle the UPM format in UpmPackageFactory.
            var packagesToRemove = new List<IPackage>();
            var packagesToRegenerate = new List<IPackage>();
            foreach (var package in m_PackageDatabase.allPackages.Where(p => p.product != null && p.versions.Any(v => v.HasTag(PackageTag.LegacyFormat))))
            {
                if (package.versions.Any(v => v.importedAssets?.Any() == true))
                    packagesToRegenerate.Add(package);
                else
                    packagesToRemove.Add(package);
            }

            // We use `ToArray` here as m_PackageDatabase.UpdatePackages will modify the enumerable and throw an error if we don't
            if (packagesToRemove.Any())
                m_PackageDatabase.UpdatePackages(toRemove: packagesToRemove.Select(p => p.uniqueId).ToArray());

            if (packagesToRegenerate.Any())
                GeneratePackagesAndTriggerChangeEvent(packagesToRegenerate.Select(p => p.product.id));
        }

        private void AddPackageError(Package package, UIError error)
        {
            AddError(package, error);
            m_PackageDatabase.OnPackagesModified(new[] { package });
        }

        private void SetPackagesProgress(IEnumerable<IPackage> packages, PackageProgress progress)
        {
            var packagesUpdated = packages.OfType<Package>().Where(p => p.progress != progress).ToArray();
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
            var package = m_PackageDatabase.GetPackage(operation.packageUniqueId) as Package;
            if (package == null)
                return;

            if (operation.state == DownloadState.Error)
                AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, operation.errorMessage, UIError.Attribute.Clearable));
            else if (operation.state == DownloadState.Aborted)
                m_PackageDatabase.OnPackagesModified(new[] { package });

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

        private void OnImportedPackagesChanged(IEnumerable<AssetStoreImportedPackage> addedOrUpdated, IEnumerable<AssetStoreImportedPackage> removed)
        {
            // Since users could have way more locally downloaded .unitypackages than what's in their purchase list
            // we don't want to trigger change events for all of them, only the ones we already checked before (the ones with productInfos)
            var productIds = addedOrUpdated?.Select(info => info.productId).Concat(removed.Select(info => info.productId) ?? new long[0]);
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
            var packagesToRemove = new List<string>();
            foreach (var productId in productIds)
            {
                var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
                var productInfo = m_AssetStoreCache.GetProductInfo(productId);
                var importedPackage = m_AssetStoreCache.GetImportedPackage(productId);
                if (purchaseInfo == null && productInfo == null && importedPackage == null)
                {
                    packagesToRemove.Add(productId.ToString());
                    continue;
                }

                // Asset store products that are potentially UPM packages are handled in UpmOnAssetStorePackageFactory, we don't want to worry about it here.
                var packageName = productInfo?.packageName ?? m_UpmCache.GetPackageData(productId)?.name;
                if (!string.IsNullOrEmpty(packageName))
                    continue;

                var fetchStatus = m_FetchStatusTracker.GetOrCreateFetchStatus(productId);
                var productInfoFetchError = fetchStatus.GetFetchError(FetchType.ProductInfo);
                if (importedPackage == null && productInfo == null)
                {
                    var version = new PlaceholderPackageVersion(productId.ToString(), purchaseInfo.displayName, tag: PackageTag.LegacyFormat, error: productInfoFetchError?.error);
                    var placeholderPackage = CreatePackage(string.Empty, new PlaceholderVersionList(version), new Product(productId, null, null));
                    if (productInfoFetchError == null)
                        SetProgress(placeholderPackage, PackageProgress.Refreshing);
                    packagesChanged.Add(placeholderPackage);
                    continue;
                }

                var isFetchingProductInfo = fetchStatus.IsFetchInProgress(FetchType.ProductInfo);
                if (importedPackage != null && productInfo == null && !isFetchingProductInfo && productInfoFetchError == null)
                {
                    m_BackgroundFetchHandler.AddToFetchPurchaseInfoQueue(productId);
                    m_BackgroundFetchHandler.AddToFetchProductInfoQueue(productId);
                    m_BackgroundFetchHandler.PushToCheckUpdateStack(productId);
                }

                var isDeprecated = productInfo?.state.Equals("deprecated", StringComparison.InvariantCultureIgnoreCase) ?? false;
                var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
                var updateInfo = m_AssetStoreCache.GetUpdateInfo(productId);
                var versionList = new AssetStoreVersionList(productInfo, localInfo, importedPackage, updateInfo);
                var package = CreatePackage(string.Empty, versionList, new Product(productId, purchaseInfo, productInfo), isDeprecated: isDeprecated);
                if (m_AssetStoreDownloadManager.GetDownloadOperation(productId)?.isInProgress == true)
                    SetProgress(package, PackageProgress.Downloading);
                else if (productInfoFetchError != null)
                    AddError(package, productInfoFetchError.error);
                packagesChanged.Add(package);
            }

            if (packagesChanged.Any() || packagesToRemove.Any())
                m_PackageDatabase.UpdatePackages(packagesChanged, packagesToRemove);
        }
    }
}
