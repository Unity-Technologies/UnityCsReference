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
    internal class AssetStoreClientV2
    {
        public virtual event Action<AssetStorePurchases> onProductListFetched = delegate {};
        public virtual event Action<long> onProductExtraFetched = delegate {};

        public virtual event Action<IOperation> onListOperation = delegate {};

        public virtual event Action<IEnumerable<long>> onUpdateChecked = delegate {};

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
        private AssetDatabaseProxy m_AssetDatabase;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetStoreCache assetStoreCache,
            AssetStoreUtils assetStoreUtils,
            AssetStoreRestAPI assetStoreRestAPI,
            FetchStatusTracker fetchStatusTracker,
            AssetDatabaseProxy assetDatabase)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreRestAPI = assetStoreRestAPI;

            m_FetchStatusTracker = fetchStatusTracker;
            m_AssetDatabase = assetDatabase;

            m_ListOperation?.ResolveDependencies(unityConnect, assetStoreRestAPI, assetStoreCache);
        }

        public virtual void ExtraFetch(long productId)
        {
            FetchPurchaseInfoWithRetry(productId);

            FetchProductInfo(productId, () =>
            {
                // If a list operation is still in progress when extra fetch returns, we'll wait until the list operation results
                // are processed, such that the extra fetch result won't be overwritten by the list result.
                if (m_ListOperation?.isInProgress == true)
                    m_ListOperation.onOperationFinalized += op => onProductExtraFetched?.Invoke(productId);
                else
                    onProductExtraFetched?.Invoke(productId);
            });

            if (m_AssetStoreCache.GetLocalInfo(productId) != null && m_AssetStoreCache.GetUpdateInfo(productId) == null)
                FetchUpdateInfos(new[] { productId });
        }

        public void FetchPurchaseInfoWithRetry(long productId, bool checkHiddenPurchases = false)
        {
            if (m_AssetStoreCache.GetPurchaseInfo(productId) != null)
                return;

            if (m_ListOperation?.isInProgress == true)
            {
                m_ListOperation.onOperationFinalized += op => FetchPurchaseInfoWithRetry(productId, checkHiddenPurchases);
                return;
            }

            // when the purchase info is not available for a package (either it's not fetched yet or just not available altogether)
            // we'll try to fetch the purchase info first and then call the `FetchInternal`.
            // In the case where a package not purchased, `purchaseInfo` will still be null,
            // but the generated `AssetStorePackage` in the end will contain an error.
            var fetchOperation = new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache);
            var queryArgs = new PurchasesQueryArgs { productIds = new List<long> { productId }, status = checkHiddenPurchases ? "hidden" : string.Empty};
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

        public virtual void FetchProductInfo(long productId, Action doneCallback = null)
        {
            m_FetchStatusTracker.SetFetchInProgress(productId, FetchType.ProductInfo);
            m_AssetStoreRestAPI.GetProductDetail(productId,
                productInfo =>
                {
                    m_AssetStoreCache.SetProductInfo(productInfo);
                    m_FetchStatusTracker.SetFetchSuccess(productId, FetchType.ProductInfo);
                    doneCallback?.Invoke();
                },
                error =>
                {
                    m_FetchStatusTracker.SetFetchError(productId, FetchType.ProductInfo, error);
                    doneCallback?.Invoke();
                });
        }

        public virtual void RefreshLocal()
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return;

            var infos = m_AssetStoreUtils.GetLocalPackageList();
            m_AssetStoreCache.SetLocalInfos(infos.Select(info => AssetStoreLocalInfo.ParseLocalInfo(info)));
        }

        public virtual void FetchUpdateInfos(IEnumerable<long> productIds, Action doneCallback = null)
        {
            if (productIds?.Any() != true)
                return;

            var localInfos = productIds.Select(id => m_AssetStoreCache.GetLocalInfo(id)).Where(info => info != null);
            m_AssetStoreRestAPI.GetUpdateDetail(new CheckUpdateInfoArgs(localInfos),
                updateInfos =>
                {
                    m_AssetStoreCache.SetUpdateInfos(updateInfos);
                    onUpdateChecked?.Invoke(productIds);
                    doneCallback?.Invoke();
                },
                error =>
                {
                    var msg = string.Format(L10n.Tr("[Package Manager Window] Error while getting product update details: {0}"), error.message);
                    if (error.operationErrorCode != -1)
                        msg += $" [Error {error.operationErrorCode}";
                    Debug.Log(msg);
                    onUpdateChecked?.Invoke(productIds);
                    doneCallback?.Invoke();
                });
        }

        public virtual IEnumerable<Asset> ListImportedAssets()
        {
            // We need to manually create the SearchFilter so that we look for assetorigins
            var filter = new SearchFilter { searchArea = SearchFilter.SearchArea.AllAssets };
            filter.ClearSearch();
            filter.originalText = "assetorigin:";
            filter.anyWithAssetOrigin = true;

            var guidsWithOrigin = m_AssetDatabase.FindAssets(filter);
            return guidsWithOrigin.Select(guid =>
            {
                var assetOrigin = m_AssetDatabase.GetAssetOrigin(guid);
                var assetPath = m_AssetDatabase.GUIDToAssetPath(guid);
                return new Asset
                {
                    guid = guid,
                    importedPath = assetPath,
                    origin = assetOrigin
                };
            });
        }

        public virtual void OnPostProcessAllAssets(string[] importedAssetPaths, string[] deletedAssetPaths, string[] movedAssetPaths, string[] movedFromAssetPaths)
        {
            var addedOrUpdatedAssets = new List<Asset>();
            var removedAssetPaths = deletedAssetPaths.Union(movedFromAssetPaths).ToHashSet();

            var pathsToCheckForOrigin = importedAssetPaths.Union(movedAssetPaths).ToArray();
            foreach (var path in pathsToCheckForOrigin)
            {
                var guid = m_AssetDatabase.AssetPathToGUID(path);
                var assetOrigin = m_AssetDatabase.GetAssetOrigin(guid);
                if (assetOrigin?.IsValid() == true)
                    addedOrUpdatedAssets.Add(new Asset
                    {
                        guid = guid,
                        importedPath = path,
                        origin = assetOrigin
                    });
                else
                    removedAssetPaths.Add(path);
            }

            m_AssetStoreCache.UpdateImportedAssets(addedOrUpdatedAssets, removedAssetPaths);
        }

        public virtual void RefreshImportedAssets()
        {
            m_AssetStoreCache.UpdateImportedAssets(ListImportedAssets(), m_AssetStoreCache.importedAssets.Select(a => a.importedPath));
        }
    }
}
