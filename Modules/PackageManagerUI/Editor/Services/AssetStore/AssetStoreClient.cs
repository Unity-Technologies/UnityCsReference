// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreClient
    {
        public virtual event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
        public virtual event Action<string, IPackageVersion> onPackageVersionUpdated = delegate {};

        public virtual event Action<AssetStorePurchases, bool> onProductListFetched = delegate {};
        public virtual event Action<long> onProductFetched = delegate {};

        public virtual event Action onFetchDetailsStart = delegate {};
        public virtual event Action onFetchDetailsFinish = delegate {};
        public virtual event Action<UIError> onFetchDetailsError = delegate {};

        public virtual event Action<IOperation> onListOperation = delegate {};

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
        private UpmClient m_UpmClient;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetStoreCache assetStoreCache,
            AssetStoreUtils assetStoreUtils,
            AssetStoreRestAPI assetStoreRestAPI,
            UpmClient upmClient,
            IOProxy ioProxy)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreRestAPI = assetStoreRestAPI;
            m_UpmClient = upmClient;
            m_IOProxy = ioProxy;

            m_ListOperation?.ResolveDependencies(unityConnect, assetStoreRestAPI);
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

        public virtual void Fetch(long productId)
        {
            if (!m_UnityConnect.isUserLoggedIn)
            {
                onFetchDetailsError?.Invoke(new UIError(UIErrorCode.AssetStoreAuthorizationError, L10n.Tr("User not logged in.")));
                return;
            }

            var productIdString = productId.ToString();
            var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productIdString);
            if (purchaseInfo != null)
            {
                FetchInternal(productId, purchaseInfo);
            }
            else
            {
                // when the purchase info is not available for a package (either it's not fetched yet or just not available altogether)
                // we'll try to fetch the purchase info first and then call the `FetchInternal`.
                // In the case where a package not purchased, `purchaseInfo` will still be null,
                // but the generated `AssetStorePackage` in the end will contain an error.
                var fetchOperation = new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI);
                var queryArgs = new PurchasesQueryArgs { productIds = new List<long> { productId } };
                fetchOperation.onOperationSuccess += op =>
                {
                    purchaseInfo = fetchOperation.result.list.FirstOrDefault();
                    if (purchaseInfo != null)
                    {
                        var updatedPackages = new List<IPackage>();
                        m_AssetStoreCache.SetPurchaseInfo(purchaseInfo);
                    }
                    ;
                    FetchInternal(productId, purchaseInfo);
                };
                fetchOperation.Start(queryArgs);
            }
        }

        private void FetchInternal(long productId, AssetStorePurchaseInfo purchaseInfo)
        {
            RefreshLocalInfos();

            var id = productId.ToString();
            var localInfo = m_AssetStoreCache.GetLocalInfo(id);
            if (localInfo?.updateInfoFetched == false)
                RefreshProductUpdateDetails(new[] { localInfo });

            // create a placeholder before fetching data from the cloud for the first time
            if (m_AssetStoreCache.GetProductInfo(id) == null)
                onPackagesChanged?.Invoke(new[] { new PlaceholderPackage(id, purchaseInfo?.displayName ?? string.Empty, PackageType.AssetStore, PackageTag.None, PackageProgress.Refreshing) });

            FetchDetails(new[] { productId });
            onProductFetched?.Invoke(productId);
        }

        public virtual void ListPurchases(PurchasesQueryArgs queryArgs, bool fetchDetails = true)
        {
            RefreshLocalInfos();
            if (queryArgs.startIndex == 0)
                RefreshProductUpdateDetails();

            m_ListOperation = m_ListOperation ?? new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI);
            m_ListOperation.onOperationSuccess += op =>
            {
                var result = m_ListOperation.result;
                if (result.list.Count > 0)
                {
                    var updatedPackages = new List<IPackage>();
                    foreach (var purchaseInfo in result.list)
                    {
                        var productIdString = purchaseInfo.productId.ToString();
                        var oldPurchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productIdString);
                        m_AssetStoreCache.SetPurchaseInfo(purchaseInfo);

                        // create a placeholder before fetching data from the cloud for the first time
                        var productInfo = m_AssetStoreCache.GetProductInfo(productIdString);
                        if (productInfo == null)
                            updatedPackages.Add(new PlaceholderPackage(productIdString, purchaseInfo.displayName, PackageType.AssetStore, PackageTag.None, PackageProgress.Refreshing));
                        else if (oldPurchaseInfo != null)
                        {
                            // for now, `tags` is the only component in `purchase info` that can be updated over time, so we only check for changes there
                            var oldTags = oldPurchaseInfo.tags ?? Enumerable.Empty<string>();
                            var newTags = purchaseInfo.tags ?? Enumerable.Empty<string>();
                            if (!oldTags.SequenceEqual(newTags))
                                updatedPackages.Add(new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, purchaseInfo, productInfo, m_AssetStoreCache.GetLocalInfo(productInfo.id)));
                        }
                    }

                    if (updatedPackages.Any())
                        onPackagesChanged?.Invoke(updatedPackages);

                    if (fetchDetails)
                        FetchDetails(result.productIds);
                }

                foreach (var cat in result.categories)
                    m_AssetStoreCache.SetCategory(cat.name, cat.count);

                onProductListFetched?.Invoke(result, fetchDetails);
            };

            onListOperation?.Invoke(m_ListOperation);
            m_ListOperation.Start(queryArgs);
        }

        public virtual void FetchDetails(IEnumerable<long> productIds)
        {
            var countProduct = productIds.Count();
            if (countProduct == 0)
                return;

            onFetchDetailsStart?.Invoke();

            foreach (var id in productIds)
            {
                m_AssetStoreRestAPI.GetProductDetail(id, productDetail =>
                {
                    IPackage package =  null;
                    var error = productDetail.GetString("errorMessage");
                    var idString = id.ToString();
                    if (string.IsNullOrEmpty(error))
                    {
                        var productInfo = AssetStoreProductInfo.ParseProductInfo(m_AssetStoreUtils, idString, productDetail);
                        if (productInfo == null)
                            package = new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, idString, new UIError(UIErrorCode.AssetStoreClientError, L10n.Tr("Error parsing product details.")));
                        else
                        {
                            var oldProductInfo = m_AssetStoreCache.GetProductInfo(idString);
                            if (oldProductInfo == null || oldProductInfo.versionId != productInfo.versionId || oldProductInfo.versionString != productInfo.versionString)
                            {
                                if (string.IsNullOrEmpty(productInfo.packageName))
                                    package = new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, m_AssetStoreCache.GetPurchaseInfo(idString), productInfo, m_AssetStoreCache.GetLocalInfo(idString));
                                else
                                    m_UpmClient.FetchForProduct(idString, productInfo.packageName);
                                m_AssetStoreCache.SetProductInfo(productInfo);
                            }
                        }
                    }
                    else
                    {
                        var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(idString);
                        m_AssetStoreCache.RemoveProductInfo(idString);
                        var uiError = new UIError(UIErrorCode.AssetStoreClientError, error);
                        package = new PlaceholderPackage(idString, purchaseInfo?.displayName ?? string.Empty, PackageType.AssetStore, PackageTag.None, PackageProgress.None, uiError);
                    }

                    if (package != null)
                        onPackagesChanged?.Invoke(new[] { package });

                    countProduct--;
                    if (countProduct == 0)
                        onFetchDetailsFinish?.Invoke();
                });
            }
        }

        public virtual void RefreshLocal()
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return;

            RefreshLocalInfos();
        }

        private void OnProductPackageChanged(string productId, IPackage package)
        {
            var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
            var productInfo = m_AssetStoreCache.GetProductInfo(productId);
            if (productInfo != null)
            {
                var assetStorePackage = new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, purchaseInfo, productInfo, package as UpmPackage);
                onPackagesChanged?.Invoke(new[] { assetStorePackage });
            }
        }

        private void OnProductPackageVersionUpdated(string productId, IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            var productInfo = m_AssetStoreCache.GetProductInfo(productId);
            if (upmVersion != null && productInfo != null)
                upmVersion.UpdateProductInfo(productInfo);
            onPackageVersionUpdated?.Invoke(productId, version);
        }

        private void OnProductPackageFetchError(string productId, UIError error)
        {
            var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(productId);
            var productInfo = m_AssetStoreCache.GetProductInfo(productId);
            if (productInfo != null)
            {
                var assetStorePackage = new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, purchaseInfo, productInfo);
                var assetStorePackageVersion = assetStorePackage.versions.primary as AssetStorePackageVersion;
                assetStorePackageVersion.SetUpmPackageFetchError(error);
                onPackagesChanged?.Invoke(new[] { assetStorePackage });
            }
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_UpmClient.onProductPackageChanged += OnProductPackageChanged;
            m_UpmClient.onProductPackageVersionUpdated += OnProductPackageVersionUpdated;
            m_UpmClient.onProductPackageFetchError += OnProductPackageFetchError;

            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_UpmClient.onProductPackageChanged -= OnProductPackageChanged;
            m_UpmClient.onProductPackageVersionUpdated -= OnProductPackageVersionUpdated;
            m_UpmClient.onProductPackageFetchError -= OnProductPackageFetchError;

            m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
        }

        public virtual void ClearCache()
        {
            m_AssetStoreCache.ClearCache();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                ClearCache();
                m_UpmClient.ClearProductCache();
            }
        }

        public virtual void RefreshProductUpdateDetails(IEnumerable<AssetStoreLocalInfo> localInfos = null)
        {
            localInfos = localInfos ?? m_AssetStoreCache.localInfos.Where(info => !info.updateInfoFetched);
            if (!localInfos.Any())
                return;

            m_AssetStoreRestAPI.GetProductUpdateDetail(localInfos, updateDetails =>
            {
                if (updateDetails.ContainsKey("errorMessage"))
                {
                    Debug.Log(string.Format(L10n.Tr("[Package Manager Window] Error while getting product update details: {0}"), updateDetails["errorMessage"]));
                    return;
                }

                var results = updateDetails.GetList<IDictionary<string, object>>("results");
                if (results == null)
                    return;

                foreach (var updateDetail in results)
                {
                    var id = updateDetail.GetString("id");
                    var localInfo = m_AssetStoreCache.GetLocalInfo(id);
                    if (localInfo != null)
                    {
                        localInfo.updateInfoFetched = true;
                        var newValue = updateDetail.Get("can_update", 0L) != 0L;
                        if (localInfo.canUpdate != newValue)
                        {
                            localInfo.canUpdate = newValue;
                            OnLocalInfosChanged(new[] { localInfo }, null);
                        }
                    }
                }
            });
        }

        private void RefreshLocalInfos()
        {
            var infos = m_AssetStoreUtils.GetLocalPackageList();
            m_AssetStoreCache.SetLocalInfos(infos.Select(info => AssetStoreLocalInfo.ParseLocalInfo(info)));
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            var packagesChanged = new List<IPackage>();
            foreach (var info in addedOrUpdated ?? Enumerable.Empty<AssetStoreLocalInfo>())
            {
                var productInfo = m_AssetStoreCache.GetProductInfo(info.id);
                if (productInfo == null)
                    continue;
                packagesChanged.Add(new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, m_AssetStoreCache.GetPurchaseInfo(info.id), productInfo, info));
            }
            foreach (var info in removed ?? Enumerable.Empty<AssetStoreLocalInfo>())
            {
                var productInfo = m_AssetStoreCache.GetProductInfo(info.id);
                if (productInfo == null)
                    continue;
                packagesChanged.Add(new AssetStorePackage(m_AssetStoreUtils, m_IOProxy, m_AssetStoreCache.GetPurchaseInfo(info.id), productInfo, (AssetStoreLocalInfo)null));
            }
            if (packagesChanged.Any())
                onPackagesChanged?.Invoke(packagesChanged);
        }
    }
}
