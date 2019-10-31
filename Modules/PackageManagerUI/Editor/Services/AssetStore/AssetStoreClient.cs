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
            public event Action<Error> onFetchDetailsError = delegate {};

            public event Action<IOperation> onListOperation = delegate {};

            private Dictionary<string, AssetStoreFetchedInfo> m_FetchedInfos = new Dictionary<string, AssetStoreFetchedInfo>();

            private Dictionary<string, AssetStoreLocalInfo> m_LocalInfos = new Dictionary<string, AssetStoreLocalInfo>();

            [SerializeField]
            private AssetStoreListOperation m_ListOperation = new AssetStoreListOperation();

            [SerializeField]
            private AssetStoreFetchedInfo[] m_SerializedFetchedInfos = new AssetStoreFetchedInfo[0];

            [SerializeField]
            private AssetStoreLocalInfo[] m_SerializedLocalInfos = new AssetStoreLocalInfo[0];

            [NonSerialized]
            private bool m_EventsRegistered;

            public void OnAfterDeserialize()
            {
                m_FetchedInfos = m_SerializedFetchedInfos.ToDictionary(info => info.id, info => info);
                m_LocalInfos = m_SerializedLocalInfos.ToDictionary(info => info.id, info => info);
            }

            public void OnBeforeSerialize()
            {
                m_SerializedFetchedInfos = m_FetchedInfos.Values.ToArray();
                m_SerializedLocalInfos = m_LocalInfos.Values.ToArray();
            }

            public void Fetch(long productId)
            {
                if (!ApplicationUtil.instance.isUserLoggedIn)
                {
                    onFetchDetailsError?.Invoke(new Error(NativeErrorCode.Unknown, L10n.Tr("User not logged in")));
                    return;
                }

                RefreshLocalInfos();

                var id = productId.ToString();
                var localInfo = m_LocalInfos.Get(id);
                if (localInfo?.updateInfoFetched == false)
                    RefreshProductUpdateDetails(new[] { localInfo });

                // create a placeholder before fetching data from the cloud for the first time
                if (!m_FetchedInfos.ContainsKey(productId.ToString()))
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
                            // create a placeholder before fetching data from the cloud for the first time
                            if (!m_FetchedInfos.ContainsKey(item.productId.ToString()))
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
                            var fetchedInfo = AssetStoreFetchedInfo.ParseFetchedInfo(id.ToString(), productDetail);
                            if (fetchedInfo == null)
                                package = new AssetStorePackage(id.ToString(), new Error(NativeErrorCode.Unknown, "Error parsing product details."));
                            else
                            {
                                var oldFetchedInfo = m_FetchedInfos.Get(fetchedInfo.id);
                                if (oldFetchedInfo == null || oldFetchedInfo.versionId != fetchedInfo.versionId || oldFetchedInfo.versionString != fetchedInfo.versionString)
                                {
                                    if (string.IsNullOrEmpty(fetchedInfo.packageName))
                                        package = new AssetStorePackage(fetchedInfo, m_LocalInfos.Get(fetchedInfo.id));
                                    else
                                        UpmClient.instance.FetchForProduct(fetchedInfo.id, fetchedInfo.packageName);
                                    m_FetchedInfos[fetchedInfo.id] = fetchedInfo;
                                }
                            }
                        }
                        else
                            package = new AssetStorePackage(id.ToString(), new Error(NativeErrorCode.Unknown, error));

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
                var fetchedInfo = m_FetchedInfos.Get(productId);
                if (fetchedInfo != null)
                {
                    var assetStorePackage = new AssetStorePackage(fetchedInfo, package as UpmPackage);
                    onPackagesChanged?.Invoke(new[] { assetStorePackage });
                }
            }

            private void OnProductPackageVersionUpdated(string productId, IPackageVersion version)
            {
                var upmVersion = version as UpmPackageVersion;
                var fetchedInfo = m_FetchedInfos.Get(productId);
                if (upmVersion != null && fetchedInfo != null)
                    upmVersion.UpdateFetchedInfo(fetchedInfo);
                onPackageVersionUpdated?.Invoke(productId, version);
            }

            private void OnProductPackageFetchError(string productId, Error error)
            {
                var fetchedInfo = m_FetchedInfos.Get(productId);
                if (fetchedInfo != null)
                {
                    var assetStorePackage = new AssetStorePackage(fetchedInfo);
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
                m_FetchedInfos.Clear();

                m_SerializedLocalInfos = new AssetStoreLocalInfo[0];
                m_SerializedFetchedInfos = new AssetStoreFetchedInfo[0];
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
                var fetchedInfo = m_FetchedInfos.Get(localInfo.id);
                if (fetchedInfo == null)
                    return;
                var package = new AssetStorePackage(fetchedInfo, localInfo);
                onPackagesChanged?.Invoke(new[] { package });
            }

            private void OnLocalInfoRemoved(AssetStoreLocalInfo localInfo)
            {
                var fetchedInfo = m_FetchedInfos.Get(localInfo.id);
                if (fetchedInfo == null)
                    return;
                var package = new AssetStorePackage(fetchedInfo, (AssetStoreLocalInfo)null);
                onPackagesChanged?.Invoke(new[] { package });
            }
        }
    }
}
