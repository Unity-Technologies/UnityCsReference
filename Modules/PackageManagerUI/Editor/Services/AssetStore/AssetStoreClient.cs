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
        public virtual event Action<AssetStorePurchases> onProductListFetched = delegate {};
        public virtual event Action<long> onProductExtraFetched = delegate {};

        public virtual event Action<IOperation> onListOperation = delegate {};

        public virtual event Action<IEnumerable<string>> onUpdateChecked = delegate {};

        [SerializeField]
        private AssetStoreListOperation m_ListOperation;

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
            UpmCache upmCache)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreRestAPI = assetStoreRestAPI;

            m_FetchStatusTracker = fetchStatusTracker;
            m_UpmCache = upmCache;

            m_ListOperation?.ResolveDependencies(unityConnect, assetStoreRestAPI, assetStoreCache);
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

        public void FetchPurchaseInfoWithRetry(long productId, bool checkHiddenPurchases = false)
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
    }
}
