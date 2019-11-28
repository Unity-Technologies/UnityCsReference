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
        internal class AssetStoreClientInternal : ScriptableSingleton<AssetStoreClientInternal>, IAssetStoreClient, ISerializationCallbackReceiver
        {
            public event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<string, IPackageVersion> onPackageVersionUpdated = delegate {};

            public event Action<AssetStorePurchases, bool> onProductListFetched = delegate {};
            public event Action<long> onProductFetched = delegate {};

            public event Action onFetchDetailsStart = delegate {};
            public event Action onFetchDetailsFinish = delegate {};
            public event Action<UIError> onFetchDetailsError = delegate {};

            public event Action<IOperation> onListOperation = delegate {};

            private Dictionary<string, AssetStorePurchaseInfo> m_PurchaseInfos = new Dictionary<string, AssetStorePurchaseInfo>();

            private Dictionary<string, AssetStoreProductInfo> m_ProductInfos = new Dictionary<string, AssetStoreProductInfo>();

            private Dictionary<string, AssetStoreLocalInfo> m_LocalInfos = new Dictionary<string, AssetStoreLocalInfo>();

            [SerializeField]
            private AssetStoreListOperation m_ListOperation = new AssetStoreListOperation();

            [SerializeField]
            private AssetStorePurchaseInfo[] m_SerializedPurchaseInfos = new AssetStorePurchaseInfo[0];

            [SerializeField]
            private AssetStoreProductInfo[] m_SerializedProductInfos = new AssetStoreProductInfo[0];

            [SerializeField]
            private AssetStoreLocalInfo[] m_SerializedLocalInfos = new AssetStoreLocalInfo[0];

            [NonSerialized]
            private bool m_EventsRegistered;

            public void OnAfterDeserialize()
            {
                m_PurchaseInfos = m_SerializedPurchaseInfos.ToDictionary(info => info.productId.ToString(), info => info);
                m_ProductInfos = m_SerializedProductInfos.ToDictionary(info => info.id, info => info);
                m_LocalInfos = m_SerializedLocalInfos.ToDictionary(info => info.id, info => info);
            }

            public void OnBeforeSerialize()
            {
                m_SerializedPurchaseInfos = m_PurchaseInfos.Values.ToArray();
                m_SerializedProductInfos = m_ProductInfos.Values.ToArray();
                m_SerializedLocalInfos = m_LocalInfos.Values.ToArray();
            }

            public void Fetch(long productId)
            {
                if (!ApplicationUtil.instance.isUserLoggedIn)
                {
                    onFetchDetailsError?.Invoke(new UIError(UIErrorCode.AssetStoreAuthorizationError, L10n.Tr("User not logged in")));
                    return;
                }

                RefreshLocalInfos();

                var id = productId.ToString();
                var localInfo = m_LocalInfos.Get(id);
                if (localInfo?.updateInfoFetched == false)
                    RefreshProductUpdateDetails(new[] { localInfo });

                // create a placeholder before fetching data from the cloud for the first time
                if (!m_ProductInfos.ContainsKey(productId.ToString()))
                    onPackagesChanged?.Invoke(new[] { new PlaceholderPackage(productId.ToString(), string.Empty, PackageType.AssetStore) });

                FetchDetails(new[] { productId });
                onProductFetched?.Invoke(productId);
            }

            public void ListPurchases(PurchasesQueryArgs queryArgs, bool fetchDetails = true)
            {
                RefreshLocalInfos();
                if (queryArgs.startIndex == 0)
                    RefreshProductUpdateDetails();

                m_ListOperation.onOperationSuccess += op =>
                {
                    var result = m_ListOperation.result;
                    if (result.list.Count > 0)
                    {
                        var placeholderPackages = new List<IPackage>();
                        foreach (var item in result.list)
                        {
                            m_PurchaseInfos[item.productId.ToString()] = item;

                            // create a placeholder before fetching data from the cloud for the first time
                            if (!m_ProductInfos.ContainsKey(item.productId.ToString()))
                                placeholderPackages.Add(new PlaceholderPackage(item.productId.ToString(), item.displayName, PackageType.AssetStore, PackageTag.None, PackageProgress.Refreshing));
                        }

                        if (placeholderPackages.Any())
                            onPackagesChanged?.Invoke(placeholderPackages);

                        if (fetchDetails)
                            FetchDetails(result.productIds);
                    }
                    onProductListFetched?.Invoke(result, fetchDetails);
                };

                onListOperation?.Invoke(m_ListOperation);
                m_ListOperation.Start(queryArgs);
            }

            public void FetchDetails(IEnumerable<long> productIds)
            {
                var countProduct = productIds.Count();
                if (countProduct == 0)
                    return;

                onFetchDetailsStart?.Invoke();

                foreach (var id in productIds)
                {
                    AssetStoreRestAPI.instance.GetProductDetail(id, productDetail =>
                    {
                        AssetStorePackage package =  null;
                        var error = productDetail.GetString("errorMessage");
                        if (string.IsNullOrEmpty(error))
                        {
                            var productInfo = AssetStoreProductInfo.ParseProductInfo(id.ToString(), productDetail);
                            if (productInfo == null)
                                package = new AssetStorePackage(id.ToString(), new UIError(UIErrorCode.AssetStoreClientError, "Error parsing product details."));
                            else
                            {
                                var purchaseInfo = m_PurchaseInfos.Get(productInfo.id);
                                var oldProductInfo = m_ProductInfos.Get(productInfo.id);
                                if (oldProductInfo == null || oldProductInfo.versionId != productInfo.versionId || oldProductInfo.versionString != productInfo.versionString)
                                {
                                    if (string.IsNullOrEmpty(productInfo.packageName))
                                        package = new AssetStorePackage(purchaseInfo, productInfo, m_LocalInfos.Get(productInfo.id));
                                    else
                                        UpmClient.instance.FetchForProduct(productInfo.id, productInfo.packageName);
                                    m_ProductInfos[productInfo.id] = productInfo;
                                }
                            }
                        }
                        else
                            package = new AssetStorePackage(id.ToString(), new UIError(UIErrorCode.AssetStoreClientError, error));

                        if (package != null)
                            onPackagesChanged?.Invoke(new[] { package });

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
                var purchaseInfo = m_PurchaseInfos.Get(productId);
                var productInfo = m_ProductInfos.Get(productId);
                if (productInfo != null)
                {
                    var assetStorePackage = new AssetStorePackage(purchaseInfo, productInfo, package as UpmPackage);
                    onPackagesChanged?.Invoke(new[] { assetStorePackage });
                }
            }

            private void OnProductPackageVersionUpdated(string productId, IPackageVersion version)
            {
                var upmVersion = version as UpmPackageVersion;
                var productInfo = m_ProductInfos.Get(productId);
                if (upmVersion != null && productInfo != null)
                    upmVersion.UpdateProductInfo(productInfo);
                onPackageVersionUpdated?.Invoke(productId, version);
            }

            private void OnProductPackageFetchError(string productId, UIError error)
            {
                var purchaseInfo = m_PurchaseInfos.Get(productId);
                var productInfo = m_ProductInfos.Get(productId);
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

                AssetStoreDownloadManager.instance.UnregisterEvents();
            }

            public void ClearCache()
            {
                m_LocalInfos.Clear();
                m_ProductInfos.Clear();

                m_SerializedLocalInfos = new AssetStoreLocalInfo[0];
                m_SerializedProductInfos = new AssetStoreProductInfo[0];
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (!loggedIn)
                {
                    UpmClient.instance.ClearProductCache();
                }
            }

            public void RefreshProductUpdateDetails(IEnumerable<AssetStoreLocalInfo> localInfos = null)
            {
                localInfos = localInfos ?? m_LocalInfos.Values.Where(info => !info.updateInfoFetched);
                if (!localInfos.Any())
                    return;

                AssetStoreRestAPI.instance.GetProductUpdateDetail(localInfos, updateDetails =>
                {
                    if (updateDetails.ContainsKey("errorMessage"))
                        return;

                    var results = updateDetails.GetList<IDictionary<string, object>>("results");
                    if (results == null)
                        return;

                    foreach (var updateDetail in results)
                    {
                        var id = updateDetail.GetString("id");
                        var localInfo = m_LocalInfos.Get(id);
                        if (localInfo != null)
                        {
                            localInfo.updateInfoFetched = true;
                            var newValue = updateDetail.Get("can_update", 0L) != 0L;
                            if (localInfo.canUpdate != newValue)
                            {
                                localInfo.canUpdate = newValue;
                                OnLocalInfoChanged(localInfo);
                            }
                        }
                    }
                });
            }

            private void RefreshLocalInfos()
            {
                var infos = AssetStoreUtils.instance.GetLocalPackageList();
                var oldLocalInfos = m_LocalInfos;
                m_LocalInfos = new Dictionary<string, AssetStoreLocalInfo>();
                foreach (var info in infos)
                {
                    var parsedInfo = AssetStoreLocalInfo.ParseLocalInfo(info);
                    var id = parsedInfo?.id;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    var oldInfo = oldLocalInfos.Get(id);
                    if (oldInfo != null)
                    {
                        oldLocalInfos.Remove(oldInfo.id);

                        if (oldInfo.versionId == parsedInfo.versionId &&
                            oldInfo.versionString == parsedInfo.versionString &&
                            oldInfo.packagePath == parsedInfo.packagePath)
                        {
                            m_LocalInfos[id] = oldInfo;
                            continue;
                        }
                    }

                    m_LocalInfos[id] = parsedInfo;
                    OnLocalInfoChanged(parsedInfo);
                }

                foreach (var info in oldLocalInfos.Values)
                    OnLocalInfoRemoved(info);
            }

            private void OnLocalInfoChanged(AssetStoreLocalInfo localInfo)
            {
                var purchaseInfo = m_PurchaseInfos.Get(localInfo.id);
                var productInfo = m_ProductInfos.Get(localInfo.id);
                if (productInfo == null)
                    return;
                var package = new AssetStorePackage(purchaseInfo, productInfo, localInfo);
                onPackagesChanged?.Invoke(new[] { package });
            }

            private void OnLocalInfoRemoved(AssetStoreLocalInfo localInfo)
            {
                var purchaseInfo = m_PurchaseInfos.Get(localInfo.id);
                var productInfo = m_ProductInfos.Get(localInfo.id);
                if (productInfo == null)
                    return;
                var package = new AssetStorePackage(purchaseInfo, productInfo, (AssetStoreLocalInfo)null);
                onPackagesChanged?.Invoke(new[] { package });
            }
        }
    }
}
