// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreClient
    {
        public virtual event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
        public virtual event Action<AssetStorePurchases> onProductListFetched = delegate {};
        public virtual event Action<long> onProductExtraFetched = delegate {};

        public virtual event Action<IOperation> onListOperation = delegate {};

        public virtual event Action<IEnumerable<string>> onUpdateChecked = delegate {};

        [SerializeField]
        private AssetStoreListOperation m_ListOperation;

        private AssetStorePackageFactory m_AssetStorePackageFactory = new AssetStorePackageFactory();
        private UpmOnAssetStorePackageFactory m_UpmOnAssetStorePackageFactory = new UpmOnAssetStorePackageFactory();

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;
        [NonSerialized]
        private AssetStoreRestAPI m_AssetStoreRestAPI;
        [NonSerialized]
        private FetchStatusTracker m_FetchStatusTracker;
        [NonSerialized]
        private UpmCache m_UpmCache;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetStoreCache assetStoreCache,
            AssetStoreUtils assetStoreUtils,
            AssetStoreRestAPI assetStoreRestAPI,
            FetchStatusTracker fetchStatusTracker,
            UpmCache upmCache,
            UpmClient upmClient,
            IOProxy ioProxy)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreRestAPI = assetStoreRestAPI;

            m_FetchStatusTracker = fetchStatusTracker;
            m_UpmCache = upmCache;

            m_ListOperation?.ResolveDependencies(unityConnect, assetStoreRestAPI, assetStoreCache);
            m_AssetStorePackageFactory.ResolveDependencies(assetStoreCache, this, assetStoreUtils, fetchStatusTracker, upmCache, ioProxy);
            m_UpmOnAssetStorePackageFactory.ResolveDependencies(assetStoreCache, this, fetchStatusTracker, upmCache, upmClient);
        }

        public virtual void ListCategories(Action<List<string>> callback)
        {
            m_AssetStoreRestAPI.GetCategories(result =>
            {
                var results = result.Get("results");
                var categories = new List<string>(results as IList<string>);
                callback?.Invoke(categories);
            }, error =>
                {
                    Debug.LogWarning(string.Format(L10n.Tr("[Package Manager Window] Error while fetching categories: {0}"), error.message));
                    callback?.Invoke(new List<string>());
                });
        }

        public virtual void ListLabels(Action<List<string>> callback)
        {
            m_AssetStoreRestAPI.GetTaggings(result =>
            {
                var labels = new List<string>(result.GetList<string>("results").ToList());
                labels.Remove("#BIN");
                labels.Sort();
                callback?.Invoke(labels);
            }, error =>
                {
                    Debug.LogWarning(string.Format(L10n.Tr("[Package Manager Window] Error while fetching labels: {0}"), error.message));
                    callback?.Invoke(new List<string>());
                });
        }

        public virtual void ExtraFetch(long productId)
        {
            FetchPurchaseInfoWithRetry(productId);

            FetchDetail(productId, () =>
            {
                // If a list operation is still in progress when extra fetch returns, we'll wait until the list opretaion results
                // are processed, such that the extra fetch result won't be overwritten by the list result.
                if (m_ListOperation.isInProgress)
                    m_ListOperation.onOperationFinalized += op => onProductExtraFetched?.Invoke(productId);
                else
                    onProductExtraFetched?.Invoke(productId);
            });

            var idString = productId.ToString();
            var localInfo = m_AssetStoreCache.GetLocalInfo(idString);
            if (localInfo != null && m_AssetStoreCache.GetUpdateInfo(localInfo.uploadId) == null)
                CheckUpdate(new[] { idString });
        }

        private void FetchPurchaseInfoWithRetry(long productId, bool checkHiddenPurchases = false)
        {
            var idString = productId.ToString();
            if (m_AssetStoreCache.GetPurchaseInfo(idString) != null)
                return;

            if (m_ListOperation.isInProgress)
            {
                m_ListOperation.onOperationFinalized += op => FetchPurchaseInfoWithRetry(productId, checkHiddenPurchases);
                return;
            }

            // when the purchase info is not available for a package (either it's not fetched yet or just not available altogether)
            // we'll try to fetch the purchase info first and then call the `FetchInternal`.
            // In the case where a package not purchased, `purchaseInfo` will still be null,
            // but the generated `AssetStorePackage` in the end will contain an error.
            var fetchOperation = new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache);
            var queryArgs = new PurchasesQueryArgs { productIds = new List<string> { idString }, status = checkHiddenPurchases ? "hidden" : string.Empty};
            fetchOperation.onOperationSuccess += op =>
            {
                var purchaseInfo = fetchOperation.result.list.FirstOrDefault();
                if (purchaseInfo != null)
                    m_AssetStoreCache.SetPurchaseInfos(new[] { purchaseInfo });
                // If we can't find the purchase info the first time, it could be be that the asset is hidden we'll do another check
                else if (!checkHiddenPurchases)
                    FetchPurchaseInfoWithRetry(productId, true);

            };
            fetchOperation.Start(queryArgs);
        }

        public virtual void ListPurchases(PurchasesQueryArgs queryArgs)
        {
            CancelListPurchases();
            RefreshLocal();

            m_ListOperation = m_ListOperation ?? new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache);
            m_ListOperation.onOperationSuccess += op =>
            {
                var result = m_ListOperation.result;
                m_AssetStoreCache.SetPurchaseInfos(result.list);

                foreach (var cat in result.categories)
                    m_AssetStoreCache.SetCategory(cat.name, cat.count);

                onProductListFetched?.Invoke(result);
            };

            onListOperation?.Invoke(m_ListOperation);
            m_ListOperation.Start(queryArgs);
        }

        public void CancelListPurchases()
        {
            m_ListOperation?.Stop();
        }

        public virtual void FetchDetail(long productId, Action doneCallbackAction = null)
        {
            var idString = productId.ToString();
            m_FetchStatusTracker.SetFetchInProgress(idString, FetchType.ProductInfo);
            m_AssetStoreRestAPI.GetProductDetail(productId, productDetail =>
            {
                var errorMessage = productDetail.GetString("errorMessage");
                if (string.IsNullOrEmpty(errorMessage))
                {
                    var productInfo = AssetStoreProductInfo.ParseProductInfo(m_AssetStoreUtils, idString, productDetail);
                    if (productInfo != null)
                    {
                        m_AssetStoreCache.SetProductInfo(productInfo);
                        m_FetchStatusTracker.SetFetchSuccess(idString, FetchType.ProductInfo);
                    }
                    else
                        errorMessage = L10n.Tr("Error parsing product details.");
                }

                if (!string.IsNullOrEmpty(errorMessage))
                    m_FetchStatusTracker.SetFetchError(idString, FetchType.ProductInfo, new UIError(UIErrorCode.AssetStoreClientError, errorMessage));
                doneCallbackAction?.Invoke();
            });
        }

        public virtual void RefreshLocal()
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return;

            var infos = m_AssetStoreUtils.GetLocalPackageList();
            m_AssetStoreCache.SetLocalInfos(infos.Select(info => AssetStoreLocalInfo.ParseLocalInfo(info)));
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_AssetStorePackageFactory.OnEnable();
            m_UpmOnAssetStorePackageFactory.OnEnable();
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_AssetStorePackageFactory.OnDisable();
            m_UpmOnAssetStorePackageFactory.OnDisable();
        }

        public virtual void ClearCache()
        {
            m_AssetStoreCache.ClearCache();
            m_FetchStatusTracker.ClearCache();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                ClearCache();
                m_UpmCache.ClearProductCache();
                m_UpmOnAssetStorePackageFactory.OnUserLogOut();
            }
        }

        public virtual void CheckUpdate(IEnumerable<string> productIds, Action doneCallbackAction = null)
        {
            if (productIds?.Any() != true)
                return;

            m_AssetStoreRestAPI.GetProductUpdateDetail(productIds, updateDetails =>
            {
                if (updateDetails.ContainsKey("errorMessage"))
                {
                    var msg = string.Format(L10n.Tr("[Package Manager Window] Error while getting product update details: {0}"), updateDetails["errorMessage"]);
                    if (updateDetails.ContainsKey("errorCode"))
                        msg += $" [Error {updateDetails["errorCode"]}";
                    Debug.Log(msg);
                }
                else
                {
                    var allProductIds = productIds.ToHashSet();
                    var results = updateDetails.GetList<IDictionary<string, object>>("results") ?? Enumerable.Empty<IDictionary<string, object>>();
                    var newUpdateInfos = new List<AssetStoreUpdateInfo>();
                    foreach (var updateDetail in results)
                    {
                        var productId = updateDetail.GetString("id");
                        allProductIds.Remove(productId);

                        var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
                        if (localInfo == null || m_AssetStoreCache.GetUpdateInfo(localInfo.uploadId) != null)
                            continue;

                        var updateStatus = AssetStoreUpdateInfo.UpdateStatus.UpdateChecked;
                        if (updateDetail.Get("can_update", 0L) != 0L)
                        {
                            var recommendVersionCompare = updateDetail.Get("recommend_version_compare", 0L);
                            if (recommendVersionCompare < 0L)
                                updateStatus |= AssetStoreUpdateInfo.UpdateStatus.CanDowngrade;
                            else
                                updateStatus |= AssetStoreUpdateInfo.UpdateStatus.CanUpdate;
                        }

                        var newUpdateInfo = new AssetStoreUpdateInfo
                        {
                            productId = productId,
                            uploadId = localInfo.uploadId,
                            updateStatus = updateStatus
                        };
                        newUpdateInfos.Add(newUpdateInfo);
                    }

                    // If an asset store package is disabled, we won't get properly update info from the server (the id field will be transformed to something else)
                    // in the past we consider this case as `updateInfo` not checked and that causes the Package Manager to check update indefinitely.
                    // Now we want to mark all packages that we called `CheckUpdate` on as updateInfoFetched to avoid unnecessary calls on disabled packages.
                    foreach (var productId in allProductIds)
                    {
                        var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
                        if (localInfo == null || m_AssetStoreCache.GetUpdateInfo(localInfo.uploadId) != null)
                            continue;

                        var newUpdateInfo = new AssetStoreUpdateInfo
                        {
                            productId = productId,
                            uploadId = localInfo?.uploadId,
                            updateStatus = AssetStoreUpdateInfo.UpdateStatus.UpdateChecked
                        };
                        newUpdateInfos.Add(newUpdateInfo);
                    }

                    m_AssetStoreCache.SetUpdateInfos(newUpdateInfos);
                    onUpdateChecked?.Invoke(productIds);
                }
                doneCallbackAction?.Invoke();
            });
        }

        public virtual void CheckTermOfServiceAgreement(Action<TermOfServiceAgreementStatus> agreementStatusCallback, Action<UIError> errorCallback)
        {
            m_AssetStoreRestAPI.CheckTermsAndConditions(result =>
            {
                var accepted = result.Get<bool>("result");
                agreementStatusCallback?.Invoke(accepted ? TermOfServiceAgreementStatus.Accepted : TermOfServiceAgreementStatus.NotAccepted);
            }, error => errorCallback?.Invoke(error));
        }

        internal class AssetStorePackageFactory
        {
            [NonSerialized]
            private AssetStoreCache m_AssetStoreCache;
            [NonSerialized]
            private AssetStoreClient m_AssetStoreClient;
            [NonSerialized]
            private AssetStoreUtils m_AssetStoreUtils;
            [NonSerialized]
            private FetchStatusTracker m_FetchStatusTracker;
            [NonSerialized]
            private UpmCache m_UpmCache;
            [NonSerialized]
            private IOProxy m_IOProxy;
            public void ResolveDependencies(AssetStoreCache assetStoreCache,
                AssetStoreClient assetStoreClient,
                AssetStoreUtils assetStoreUtils,
                FetchStatusTracker fetchStatusTracker,
                UpmCache upmCache,
                IOProxy ioProxy)
            {
                m_AssetStoreCache = assetStoreCache;
                m_AssetStoreClient = assetStoreClient;
                m_AssetStoreUtils = assetStoreUtils;
                m_FetchStatusTracker = fetchStatusTracker;
                m_UpmCache = upmCache;
                m_IOProxy = ioProxy;
            }

            public void OnEnable()
            {
                m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
                m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
                m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;
                m_AssetStoreCache.onUpdatesFound += OnUpdatesFound;
                m_FetchStatusTracker.onFetchStatusChanged += OnFetchStatusChanged;
            }

            public void OnDisable()
            {
                m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
                m_AssetStoreCache.onPurchaseInfosChanged -= OnPurchaseInfosChanged;
                m_AssetStoreCache.onProductInfoChanged -= OnProductInfoChanged;
                m_AssetStoreCache.onUpdatesFound -= OnUpdatesFound;
                m_FetchStatusTracker.onFetchStatusChanged -= OnFetchStatusChanged;
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

                    var packageName = string.IsNullOrEmpty(productInfo?.packageName) ? m_UpmCache.GetNameByProductId(productId) : productInfo.packageName;
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
                    packagesChanged.Add(new AssetStorePackage(purchaseInfo, productInfo, versionList));
                }
                if (packagesChanged.Any())
                    m_AssetStoreClient.onPackagesChanged?.Invoke(packagesChanged);
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
        }

        internal class UpmOnAssetStorePackageFactory
        {
            [NonSerialized]
            private AssetStoreCache m_AssetStoreCache;
            [NonSerialized]
            private AssetStoreClient m_AssetStoreClient;
            [NonSerialized]
            private FetchStatusTracker m_FetchStatusTracker;
            [NonSerialized]
            private UpmCache m_UpmCache;
            [NonSerialized]
            private UpmClient m_UpmClient;
            public void ResolveDependencies(AssetStoreCache assetStoreCache,
                AssetStoreClient assetStoreClient,
                FetchStatusTracker fetchStatusTracker,
                UpmCache upmCache,
                UpmClient upmClient)
            {
                m_AssetStoreCache = assetStoreCache;
                m_AssetStoreClient = assetStoreClient;
                m_FetchStatusTracker = fetchStatusTracker;
                m_UpmCache = upmCache;
                m_UpmClient = upmClient;
            }

            public void OnEnable()
            {
                m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
                m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;

                m_AssetStoreCache.onPurchaseInfosChanged += OnPurchaseInfosChanged;
                m_AssetStoreCache.onProductInfoChanged += OnProductInfoChanged;

                m_FetchStatusTracker.onFetchStatusChanged += OnFetchStatusChanged;

                RestartInProgressFetches();
            }

            public void OnDisable()
            {
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
                        var packageName = string.IsNullOrEmpty(productInfo?.packageName) ? m_UpmCache.GetNameByProductId(productId) : productInfo.packageName;
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

                    var packageName = string.IsNullOrEmpty(productInfo?.packageName) ? m_UpmCache.GetNameByProductId(productId) : productInfo.packageName;
                    // Unlike AssetStorePackageFactory or UpmPackageFactory, UpmOnAssetStorePackageFactory is specifically created to handle packages
                    // with both productId and packageName, so we skip all other cases here.
                    if (string.IsNullOrEmpty(packageName))
                        continue;

                    var productSearchInfo = m_UpmCache.GetProductSearchPackageInfo(packageName);
                    var installedPackageInfo = m_UpmCache.GetInstalledPackageInfo(packageName);
                    var fetchStatus = m_FetchStatusTracker.GetOrCreateFetchStatus(productId);

                    IPackage package = null;
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
                            package.AddError(productInfoFetchError.error);
                        else if (productInfo == null && fetchStatus.IsFetchInProgress(FetchType.ProductInfo))
                            package.progress = PackageProgress.Refreshing;
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
                            package.progress = PackageProgress.Refreshing;
                        }
                        else
                            productIdAndNamesToSearch.Add((productId: productId, packageName: packageName));
                    }

                    if (package != null)
                        packagesChanged.Add(package);
                }
                if (packagesChanged.Any())
                    m_AssetStoreClient.onPackagesChanged?.Invoke(packagesChanged);

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
                GeneratePackagesAndTriggerChangeEvent(packageInfos.Select(p => m_UpmCache.GetProductIdByName(p.name)).Where(id => !string.IsNullOrEmpty(id)));
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

            public void OnUserLogOut()
            {
                var productIds = m_UpmCache.installedPackageInfos.Select(info => info.assetStore?.productId).Where(id => !string.IsNullOrEmpty(id));
                GeneratePackagesAndTriggerChangeEvent(productIds);
            }
        }
    }
}
