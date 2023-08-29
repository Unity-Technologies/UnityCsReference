// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAssetStoreClient : IService
    {
        event Action<AssetStorePurchases> onProductListFetched;
        event Action<long> onProductExtraFetched;
        event Action<IOperation> onListOperation;
        event Action<IEnumerable<long>> onUpdateChecked;

        void ExtraFetch(long productId);
        void FetchPurchaseInfos(IEnumerable<long> productIds, Action doneCallback = null);
        void ListPurchases(PurchasesQueryArgs queryArgs);
        void CancelListPurchases();
        void FetchProductInfo(long productId, Action doneCallback = null);
        void RefreshLocal();
        void FetchUpdateInfos(IEnumerable<long> productIds, Action doneCallback = null);
        IEnumerable<Asset> ListImportedAssets();
        void OnPostProcessAllAssets(string[] importedAssetPaths, string[] deletedAssetPaths, string[] movedAssetPaths, string[] movedFromAssetPaths);
        void RefreshImportedAssets();
    }

    [Serializable]
    internal class AssetStoreClientV2 : BaseService<IAssetStoreClient>, IAssetStoreClient, ISerializationCallbackReceiver
    {
        public event Action<AssetStorePurchases> onProductListFetched = delegate {};
        public event Action<long> onProductExtraFetched = delegate {};

        public event Action<IOperation> onListOperation = delegate {};

        public event Action<IEnumerable<long>> onUpdateChecked = delegate {};

        [SerializeField]
        private AssetStoreListOperation m_ListOperation;

        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IAssetStoreRestAPI m_AssetStoreRestAPI;
        private readonly IFetchStatusTracker m_FetchStatusTracker;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IOperationFactory m_OperationFactory;
        private readonly ILocalInfoHandler m_LocalInfoHandler;

        public AssetStoreClientV2(IAssetStoreCache assetStoreCache,
            IAssetStoreRestAPI assetStoreRestAPI,
            IFetchStatusTracker fetchStatusTracker,
            IAssetDatabaseProxy assetDatabase,
            IOperationFactory operationFactory,
            ILocalInfoHandler localInfoHandler)
        {
            m_AssetStoreCache = RegisterDependency(assetStoreCache);
            m_AssetStoreRestAPI = RegisterDependency(assetStoreRestAPI);

            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
            m_AssetDatabase = RegisterDependency(assetDatabase);

            m_OperationFactory = RegisterDependency(operationFactory);
            m_LocalInfoHandler = RegisterDependency(localInfoHandler);
        }

        public void ExtraFetch(long productId)
        {
            FetchPurchaseInfos(new[] { productId });

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

        public void FetchPurchaseInfos(IEnumerable<long> productIds, Action doneCallback = null)
        {
            FetchPurchaseInfosWithRetry(productIds, false, doneCallback);
        }

        private void FetchPurchaseInfosWithRetry(IEnumerable<long> productIds, bool checkHiddenPurchases, Action doneCallback)
        {
            var productIdsWithoutPurchaseInfo = productIds?.Where(id => m_AssetStoreCache.GetPurchaseInfo(id) == null).ToList() ?? new List<long>();
            if (!productIdsWithoutPurchaseInfo.Any())
                return;

            if (m_ListOperation?.isInProgress == true)
            {
                m_ListOperation.onOperationFinalized += op => FetchPurchaseInfosWithRetry(productIdsWithoutPurchaseInfo, checkHiddenPurchases, doneCallback);
                return;
            }

            // when the purchase info is not available for a package (either it's not fetched yet or just not available altogether)
            // we'll try to fetch the purchase info first and then call the `FetchInternal`.
            // In the case where a package not purchased, `purchaseInfo` will still be null,
            // but the generated `Package` in the end will not contain an error.
            var fetchOperation = m_OperationFactory.CreateAssetStoreListOperation();
            var queryArgs = new PurchasesQueryArgs { productIds = productIdsWithoutPurchaseInfo, status = checkHiddenPurchases ? PageFilters.Status.Hidden : PageFilters.Status.None };
            var fetchHiddenProductsRequired = false;
            fetchOperation.onOperationSuccess += op =>
            {
                if (fetchOperation.result.list.Any())
                    m_AssetStoreCache.SetPurchaseInfos(fetchOperation.result.list);

                // If we can't find the all the purchase infos the first time, it could be be that the asset is hidden we'll do another check
                if (fetchOperation.result.list.Count < productIdsWithoutPurchaseInfo.Count && !checkHiddenPurchases)
                {
                    fetchHiddenProductsRequired = true;
                    var productIdsFound = fetchOperation.result.list.Select(info => info.productId).ToHashSet();
                    var potentiallyHiddenProductIds = productIdsWithoutPurchaseInfo.Where(id => !productIdsFound.Contains(id));
                    FetchPurchaseInfosWithRetry(potentiallyHiddenProductIds, true, doneCallback);
                }
            };
            fetchOperation.onOperationFinalized += op =>
            {
                if (!fetchHiddenProductsRequired)
                    doneCallback?.Invoke();
            };
            fetchOperation.Start(queryArgs);
        }

        public void ListPurchases(PurchasesQueryArgs queryArgs)
        {
            CancelListPurchases();
            m_ListOperation ??= m_OperationFactory.CreateAssetStoreListOperation();
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

        public void FetchProductInfo(long productId, Action doneCallback = null)
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

        public void RefreshLocal()
        {
            m_AssetStoreCache.SetLocalInfos(m_LocalInfoHandler.GetParsedLocalInfos());
        }

        public void FetchUpdateInfos(IEnumerable<long> productIds, Action doneCallback = null)
        {
            if (productIds?.Any() != true)
                return;

            m_AssetStoreRestAPI.GetUpdateDetail(new CheckUpdateInfoArgs(productIds),
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

        public IEnumerable<Asset> ListImportedAssets()
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

        public void OnPostProcessAllAssets(string[] importedAssetPaths, string[] deletedAssetPaths, string[] movedAssetPaths, string[] movedFromAssetPaths)
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

        public void RefreshImportedAssets()
        {
            m_AssetStoreCache.UpdateImportedAssets(ListImportedAssets(), m_AssetStoreCache.importedAssets.Select(a => a.importedPath));
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_OperationFactory.ResolveDependenciesForOperation(m_ListOperation);
        }
    }
}
