// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class AssetStoreClient
    {
        static IAssetStoreClient s_Instance = null;
        public static IAssetStoreClient instance => s_Instance ?? AssetStoreClientInternal.instance;

        [Serializable]
        internal class AssetStoreClientInternal : ScriptableSingleton<AssetStoreClientInternal>, IAssetStoreClient
        {
            public event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<string, IPackageVersion> onPackageVersionUpdated = delegate {};

            public event Action<AssetStorePurchases> onProductListFetched = delegate {};
            public event Action<long> onProductFetched = delegate {};

            public event Action onFetchDetailsStart = delegate {};
            public event Action onFetchDetailsFinish = delegate {};
            public event Action<UIError> onFetchDetailsError = delegate {};

            public event Action<IOperation> onListOperation = delegate {};

            [SerializeField]
            private AssetStoreListOperation m_ListOperation = new AssetStoreListOperation();

            [NonSerialized]
            private bool m_EventsRegistered;

            public void ListCategories(Action<List<string>> callback)
            {
                AssetStoreRestAPI.instance.GetCategories(result =>
                {
                    var results = result.Get("results");
                    var categories = new List<string>(results as IList<string>);
                    callback?.Invoke(categories);
                }, error =>
                    {
                        Debug.LogWarning("[PackageManagerUI] error while fetching categories: " + error.message);
                        callback?.Invoke(new List<string>());
                    });
            }

            public void ListLabels(Action<List<string>> callback)
            {
                AssetStoreRestAPI.instance.GetTaggings(result =>
                {
                    var labels = new List<string>(result.GetList<string>("results").ToList());
                    labels.Remove("#BIN");
                    labels.Sort();
                    callback?.Invoke(labels);
                }, error =>
                    {
                        Debug.LogWarning("[PackageManagerUI] error while fetching labels: " + error.message);
                        callback?.Invoke(new List<string>());
                    });
            }

            public void Fetch(long productId)
            {
                if (!ApplicationUtil.instance.isUserLoggedIn)
                {
                    onFetchDetailsError?.Invoke(new UIError(UIErrorCode.AssetStoreAuthorizationError, ApplicationUtil.instance.GetTranslationForText("User not logged in")));
                    return;
                }

                var productIdString = productId.ToString();
                var purchaseInfo = AssetStoreCache.instance.GetPurchaseInfo(productIdString);
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
                    var fetchOperation = new AssetStoreListOperation();
                    var queryArgs = new PurchasesQueryArgs { productIds = new List<long> { productId } };
                    fetchOperation.onOperationSuccess += op =>
                    {
                        purchaseInfo = fetchOperation.result.list.FirstOrDefault();
                        if (purchaseInfo != null)
                        {
                            AssetStoreCache.instance.SetPurchaseInfo(purchaseInfo);
                        }

                        FetchInternal(productId, purchaseInfo);
                    };
                    fetchOperation.Start(queryArgs);
                }
            }

            private void FetchInternal(long productId, AssetStorePurchaseInfo purchaseInfo)
            {
                RefreshLocalInfos();

                var id = productId.ToString();
                var localInfo = AssetStoreCache.instance.GetLocalInfo(id);
                if (localInfo?.updateInfoFetched == false)
                    RefreshProductUpdateDetails(new[] { localInfo });

                // create a placeholder before fetching data from the cloud for the first time
                if (AssetStoreCache.instance.GetProductInfo(id) == null)
                    onPackagesChanged?.Invoke(new[] { new PlaceholderPackage(id, purchaseInfo?.displayName ?? string.Empty, PackageType.AssetStore, PackageTag.None, PackageProgress.Refreshing) });

                FetchDetails(new[] { productId });
                onProductFetched?.Invoke(productId);
            }

            public void ListPurchases(PurchasesQueryArgs queryArgs)
            {
                // patch fix to avoid User Not Logged In error when first opening the application with My Assets open
                if (!ApplicationUtil.instance.isUserInfoReady)
                {
                    EditorApplication.delayCall += () => ListPurchases(queryArgs);
                    return;
                }

                RefreshLocalInfos();
                if (queryArgs.startIndex == 0)
                    RefreshProductUpdateDetails();

                m_ListOperation.onOperationSuccess += op =>
                {
                    var result = m_ListOperation.result;
                    if (result.list.Count > 0)
                    {
                        var updatedPackages = new List<IPackage>();
                        foreach (var purchaseInfo in result.list)
                        {
                            var productIdString = purchaseInfo.productId.ToString();
                            var oldPurchaseInfo = AssetStoreCache.instance.GetPurchaseInfo(productIdString);
                            AssetStoreCache.instance.SetPurchaseInfo(purchaseInfo);

                            // create a placeholder before fetching data from the cloud for the first time
                            var productInfo = AssetStoreCache.instance.GetProductInfo(productIdString);
                            if (productInfo == null)
                                updatedPackages.Add(new PlaceholderPackage(productIdString, purchaseInfo.displayName, PackageType.AssetStore, PackageTag.None, PackageProgress.Refreshing));
                            else if (oldPurchaseInfo != null)
                            {
                                // for now, `tags` is the only component in `purchase info` that can be updated over time, so we only check for changes there
                                var oldTags = oldPurchaseInfo.tags ?? Enumerable.Empty<string>();
                                var newTags = purchaseInfo.tags ?? Enumerable.Empty<string>();
                                if (!oldTags.SequenceEqual(newTags))
                                    updatedPackages.Add(new AssetStorePackage(purchaseInfo, productInfo, AssetStoreCache.instance.GetLocalInfo(productInfo.id)));
                            }
                        }

                        if (updatedPackages.Any())
                            onPackagesChanged?.Invoke(updatedPackages);
                    }

                    foreach (var cat in result.categories)
                        AssetStoreCache.instance.SetCategory(cat.name, cat.count);

                    onProductListFetched?.Invoke(result);
                };

                onListOperation?.Invoke(m_ListOperation);
                m_ListOperation.Start(queryArgs);
            }

            public void FetchDetail(long productId, Action doneCallbackAction = null)
            {
                AssetStoreRestAPI.instance.GetProductDetail(productId, productDetail =>
                {
                    IPackage package =  null;
                    var error = productDetail.GetString("errorMessage");
                    var idString = productId.ToString();
                    if (string.IsNullOrEmpty(error))
                    {
                        var productInfo = AssetStoreProductInfo.ParseProductInfo(idString, productDetail);
                        if (productInfo == null)
                            package = new AssetStorePackage(idString, new UIError(UIErrorCode.AssetStoreClientError, L10n.Tr("Error parsing product details.")));
                        else
                        {
                            var oldProductInfo = AssetStoreCache.instance.GetProductInfo(idString);
                            if (oldProductInfo == null || oldProductInfo.versionId != productInfo.versionId || oldProductInfo.versionString != productInfo.versionString)
                            {
                                if (string.IsNullOrEmpty(productInfo.packageName))
                                    package = new AssetStorePackage(AssetStoreCache.instance.GetPurchaseInfo(idString), productInfo, AssetStoreCache.instance.GetLocalInfo(idString));
                                else
                                    UpmClient.instance.FetchForProduct(idString, productInfo.packageName);
                                AssetStoreCache.instance.SetProductInfo(productInfo);
                            }
                        }
                    }
                    else
                    {
                        AssetStoreCache.instance.RemoveProductInfo(idString);
                        package = new AssetStorePackage(idString, new UIError(UIErrorCode.AssetStoreClientError, error));
                    }

                    if (package != null)
                        onPackagesChanged?.Invoke(new[] { package });

                    doneCallbackAction?.Invoke();
                });
            }

            public void FetchDetails(IEnumerable<long> productIds)
            {
                var countProduct = productIds.Count();
                if (countProduct == 0)
                    return;

                onFetchDetailsStart?.Invoke();

                foreach (var id in productIds)
                {
                    FetchDetail(id, () =>
                    {
                        countProduct--;
                        if (countProduct == 0)
                            onFetchDetailsFinish?.Invoke();
                    });
                }
            }

            public void RefreshLocal()
            {
                if (!ApplicationUtil.instance.isUserLoggedIn)
                    return;

                RefreshLocalInfos();
            }

            private void OnProductPackageChanged(string productId, IPackage package)
            {
                var purchaseInfo = AssetStoreCache.instance.GetPurchaseInfo(productId);
                var productInfo = AssetStoreCache.instance.GetProductInfo(productId);
                if (productInfo != null)
                {
                    var assetStorePackage = new AssetStorePackage(purchaseInfo, productInfo, package as UpmPackage);
                    onPackagesChanged?.Invoke(new[] { assetStorePackage });
                }
            }

            private void OnProductPackageVersionUpdated(string productId, IPackageVersion version)
            {
                var upmVersion = version as UpmPackageVersion;
                var productInfo = AssetStoreCache.instance.GetProductInfo(productId);
                if (upmVersion != null && productInfo != null)
                    upmVersion.UpdateProductInfo(productInfo);
                onPackageVersionUpdated?.Invoke(productId, version);
            }

            private void OnProductPackageFetchError(string productId, UIError error)
            {
                var purchaseInfo = AssetStoreCache.instance.GetPurchaseInfo(productId);
                var productInfo = AssetStoreCache.instance.GetProductInfo(productId);
                if (productInfo != null)
                {
                    var assetStorePackage = new AssetStorePackage(purchaseInfo, productInfo);
                    var assetStorePackageVersion = assetStorePackage.versions.primary as AssetStorePackageVersion;
                    assetStorePackageVersion.SetUpmPackageFetchError(error);
                    onPackagesChanged?.Invoke(new[] { assetStorePackage });
                }
            }

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;

                m_EventsRegistered = true;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
                UpmClient.instance.onProductPackageChanged += OnProductPackageChanged;
                UpmClient.instance.onProductPackageVersionUpdated += OnProductPackageVersionUpdated;
                UpmClient.instance.onProductPackageFetchError += OnProductPackageFetchError;

                AssetStoreCache.instance.onLocalInfosChanged += OnLocalInfosChanged;

                AssetStoreDownloadManager.instance.RegisterEvents();
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
                UpmClient.instance.onProductPackageChanged -= OnProductPackageChanged;
                UpmClient.instance.onProductPackageVersionUpdated -= OnProductPackageVersionUpdated;
                UpmClient.instance.onProductPackageFetchError -= OnProductPackageFetchError;

                AssetStoreCache.instance.onLocalInfosChanged -= OnLocalInfosChanged;

                AssetStoreDownloadManager.instance.UnregisterEvents();
            }

            public void ClearCache()
            {
                AssetStoreCache.instance.ClearCache();
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (!loggedIn)
                {
                    ClearCache();
                    UpmClient.instance.ClearProductCache();
                }
            }

            public void RefreshProductUpdateDetails(IEnumerable<AssetStoreLocalInfo> localInfos = null)
            {
                localInfos = localInfos ?? AssetStoreCache.instance.localInfos.Where(info => !info.updateInfoFetched);
                if (!localInfos.Any())
                    return;

                AssetStoreRestAPI.instance.GetProductUpdateDetail(localInfos, updateDetails =>
                {
                    if (updateDetails.ContainsKey("errorMessage"))
                    {
                        Debug.Log("[PackageManagerUI] Error while getting product update details: " + updateDetails["errorMessage"]);
                        return;
                    }

                    var results = updateDetails.GetList<IDictionary<string, object>>("results");
                    if (results == null)
                        return;

                    foreach (var updateDetail in results)
                    {
                        var id = updateDetail.GetString("id");
                        var localInfo = AssetStoreCache.instance.GetLocalInfo(id);
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
                var infos = AssetStoreUtils.instance.GetLocalPackageList();
                AssetStoreCache.instance.SetLocalInfos(infos.Select(info => AssetStoreLocalInfo.ParseLocalInfo(info)));
            }

            private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
            {
                var packagesChanged = new List<IPackage>();
                foreach (var info in addedOrUpdated ?? Enumerable.Empty<AssetStoreLocalInfo>())
                {
                    var productInfo = AssetStoreCache.instance.GetProductInfo(info.id);
                    if (productInfo == null)
                        continue;
                    packagesChanged.Add(new AssetStorePackage(AssetStoreCache.instance.GetPurchaseInfo(info.id), productInfo, info));
                }
                foreach (var info in removed ?? Enumerable.Empty<AssetStoreLocalInfo>())
                {
                    var productInfo = AssetStoreCache.instance.GetProductInfo(info.id);
                    if (productInfo == null)
                        continue;
                    packagesChanged.Add(new AssetStorePackage(AssetStoreCache.instance.GetPurchaseInfo(info.id), productInfo, (AssetStoreLocalInfo)null));
                }
                if (packagesChanged.Any())
                    onPackagesChanged?.Invoke(packagesChanged);
            }
        }
    }
}
