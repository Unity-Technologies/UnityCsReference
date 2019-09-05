// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal sealed class AssetStoreClient
    {
        static IAssetStoreClient s_Instance = null;
        public static IAssetStoreClient instance => s_Instance ?? AssetStoreClientInternal.instance;

        [Serializable]
        internal class AssetStoreClientInternal : ScriptableSingleton<AssetStoreClientInternal>, IAssetStoreClient, ISerializationCallbackReceiver
        {
            private static readonly string k_AssetStoreDownloadPrefix = "content__";

            public event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<string, IPackageVersion> onPackageVersionUpdated = delegate {};
            public event Action<DownloadProgress> onDownloadProgress = delegate {};

            public event Action<ProductList, bool> onProductListFetched = delegate {};
            public event Action<long> onProductFetched = delegate {};

            public event Action onFetchDetailsStart = delegate {};
            public event Action onFetchDetailsFinish = delegate {};
            public event Action<Error> onFetchDetailsError = delegate {};

            public event Action<IOperation> onListOperation = delegate {};

            private Dictionary<string, DownloadProgress> m_Downloads = new Dictionary<string, DownloadProgress>();

            private Dictionary<string, FetchedInfo> m_FetchedInfos = new Dictionary<string, FetchedInfo>();

            private Dictionary<string, LocalInfo> m_LocalInfos = new Dictionary<string, LocalInfo>();

            private AssetStoreListOperation m_ListOperation = new AssetStoreListOperation();

            [SerializeField]
            private DownloadProgress[] m_SerializedDownloads = new DownloadProgress[0];

            [SerializeField]
            private FetchedInfo[] m_SerializedFetchedInfos = new FetchedInfo[0];

            [SerializeField]
            private LocalInfo[] m_SerializedLocalInfos = new LocalInfo[0];

            [SerializeField]
            private bool m_SetupDone;

            public void OnAfterDeserialize()
            {
                m_Downloads = m_SerializedDownloads.ToDictionary(d => d.productId, d => d);
                m_FetchedInfos = m_SerializedFetchedInfos.ToDictionary(info => info.id, info => info);
                m_LocalInfos = m_SerializedLocalInfos.ToDictionary(info => info.id, info => info);
            }

            public void OnBeforeSerialize()
            {
                m_SerializedDownloads = m_Downloads.Values.ToArray();
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
                    onPackagesChanged?.Invoke(new[] { new PlaceholderPackage(productId.ToString(), PackageType.AssetStore) });

                FetchDetails(new[] { productId });
                onProductFetched?.Invoke(productId);
            }

            public void List(int offset, int limit, string searchText = "", bool fetchDetails = true)
            {
                m_ListOperation.Start();
                onListOperation?.Invoke(m_ListOperation);

                if (!ApplicationUtil.instance.isUserLoggedIn)
                {
                    m_ListOperation.TriggerOperationError(new Error(NativeErrorCode.Unknown, L10n.Tr("User not logged in")));
                    return;
                }

                RefreshLocalInfos();

                if (offset == 0)
                    RefreshProductUpdateDetails();

                AssetStoreRestAPI.instance.GetProductIDList(offset, limit, searchText, productList =>
                {
                    if (!productList.isValid)
                    {
                        m_ListOperation.TriggerOperationError(new Error(NativeErrorCode.Unknown, productList.errorMessage));
                        return;
                    }

                    if (!ApplicationUtil.instance.isUserLoggedIn)
                    {
                        m_ListOperation.TriggerOperationError(new Error(NativeErrorCode.Unknown, L10n.Tr("User not logged in")));
                        return;
                    }

                    onProductListFetched?.Invoke(productList, fetchDetails);

                    if (productList.list.Count == 0)
                    {
                        m_ListOperation.TriggeronOperationSuccess();
                        return;
                    }

                    var placeholderPackages = new List<IPackage>();

                    foreach (var productId in productList.list)
                    {
                        // create a placeholder before fetching data from the cloud for the first time
                        if (!m_FetchedInfos.ContainsKey(productId.ToString()))
                            placeholderPackages.Add(new PlaceholderPackage(productId.ToString(), PackageType.AssetStore, PackageTag.None, PackageProgress.Refreshing));
                    }

                    if (placeholderPackages.Any())
                        onPackagesChanged?.Invoke(placeholderPackages);

                    m_ListOperation.TriggeronOperationSuccess();

                    if (fetchDetails)
                        FetchDetails(productList.list);
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
                    AssetStoreRestAPI.instance.GetProductDetail(id, productDetail =>
                    {
                        AssetStorePackage package =  null;
                        var error = productDetail.GetString("errorMessage");
                        if (string.IsNullOrEmpty(error))
                        {
                            var fetchedInfo = FetchedInfo.ParseFetchedInfo(id.ToString(), productDetail);
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

            public bool IsAnyDownloadInProgress()
            {
                return m_Downloads.Values.Any(progress => progress.state == DownloadProgress.State.InProgress || progress.state == DownloadProgress.State.Started);
            }

            private static string AssetStoreCompatibleKey(string productId)
            {
                if (productId.StartsWith(k_AssetStoreDownloadPrefix))
                    return productId;

                return k_AssetStoreDownloadPrefix + productId;
            }

            public bool IsDownloadInProgress(string productId)
            {
                DownloadProgress progress;
                if (!GetDownloadProgress(productId, out progress))
                    return false;

                return progress.state == DownloadProgress.State.InProgress || progress.state == DownloadProgress.State.Started;
            }

            public bool GetDownloadProgress(string productId, out DownloadProgress progress)
            {
                progress = null;
                return m_Downloads.TryGetValue(AssetStoreCompatibleKey(productId), out progress);
            }

            public void Download(string productId)
            {
                DownloadProgress progress;
                if (GetDownloadProgress(productId, out progress))
                {
                    if (progress.state != DownloadProgress.State.Started &&
                        progress.state != DownloadProgress.State.InProgress &&
                        progress.state != DownloadProgress.State.Decrypting)
                    {
                        m_Downloads.Remove(AssetStoreCompatibleKey(productId));
                    }
                    else
                    {
                        onDownloadProgress?.Invoke(progress);
                        return;
                    }
                }

                progress = new DownloadProgress(productId);
                m_Downloads[AssetStoreCompatibleKey(productId)] = progress;
                onDownloadProgress?.Invoke(progress);

                var id = long.Parse(productId);
                AssetStoreDownloadOperation.instance.DownloadUnityPackageAsync(id, result =>
                {
                    progress.state = result.downloadState;
                    if (result.downloadState == DownloadProgress.State.Error)
                        progress.message = result.errorMessage;

                    onDownloadProgress?.Invoke(progress);
                });
            }

            public void AbortDownload(string productId)
            {
                DownloadProgress progress;
                if (!GetDownloadProgress(productId, out progress))
                    return;

                if (progress.state == DownloadProgress.State.Aborted || progress.state == DownloadProgress.State.Completed || progress.state == DownloadProgress.State.Error)
                    return;

                var id = long.Parse(productId);
                AssetStoreDownloadOperation.instance.AbortDownloadPackageAsync(id, result =>
                {
                    progress.state = DownloadProgress.State.Aborted;
                    progress.current = progress.total;
                    progress.message = L10n.Tr("Download aborted");

                    onDownloadProgress?.Invoke(progress);

                    m_Downloads.Remove(AssetStoreCompatibleKey(productId));
                });
            }

            // Used by AssetStoreUtils
            public void OnDownloadProgress(string productId, string message, ulong bytes, ulong total)
            {
                DownloadProgress progress;
                if (!GetDownloadProgress(productId, out progress))
                {
                    if (productId.StartsWith(k_AssetStoreDownloadPrefix))
                        productId = productId.Substring(k_AssetStoreDownloadPrefix.Length);
                    progress = new DownloadProgress(productId) { state = DownloadProgress.State.InProgress, message = "downloading" };
                    m_Downloads[AssetStoreCompatibleKey(productId)] = progress;
                }

                progress.current = bytes;
                progress.total = total;
                progress.message = message;

                if (message == "ok")
                    progress.state = DownloadProgress.State.Completed;
                else if (message == "connecting")
                    progress.state = DownloadProgress.State.Started;
                else if (message == "downloading")
                    progress.state = DownloadProgress.State.InProgress;
                else if (message == "decrypt")
                    progress.state = DownloadProgress.State.Decrypting;
                else if (message == "aborted")
                    progress.state = DownloadProgress.State.Aborted;
                else
                    progress.state = DownloadProgress.State.Error;

                onDownloadProgress?.Invoke(progress);
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
                    var assetStorePackageVersion = assetStorePackage.versionList.primary as AssetStorePackageVersion;
                    assetStorePackageVersion.SetUpmPackageFetchError(error);
                    onPackagesChanged?.Invoke(new[] { assetStorePackage });
                }
            }

            public void Setup()
            {
                System.Diagnostics.Debug.Assert(!m_SetupDone);
                m_SetupDone = true;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
                if (ApplicationUtil.instance.isUserLoggedIn)
                {
                    AssetStoreUtils.instance.RegisterDownloadDelegate(this);
                }

                UpmClient.instance.onProductPackageChanged += OnProductPackageChanged;
                UpmClient.instance.onProductPackageVersionUpdated += OnProductPackageVersionUpdated;
                UpmClient.instance.onProductPackageFetchError += OnProductPackageFetchError;
            }

            public void Clear()
            {
                System.Diagnostics.Debug.Assert(m_SetupDone);
                m_SetupDone = false;

                AssetStoreUtils.instance.UnRegisterDownloadDelegate(this);
                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
                UpmClient.instance.onProductPackageChanged -= OnProductPackageChanged;
                UpmClient.instance.onProductPackageVersionUpdated -= OnProductPackageVersionUpdated;
                UpmClient.instance.onProductPackageFetchError -= OnProductPackageFetchError;
            }

            public void Reset()
            {
                m_LocalInfos.Clear();
                m_FetchedInfos.Clear();

                m_SerializedLocalInfos = new LocalInfo[0];
                m_SerializedFetchedInfos = new FetchedInfo[0];
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (!loggedIn)
                {
                    AssetStoreUtils.instance.UnRegisterDownloadDelegate(this);
                    AbortAllDownloads();
                    Reset();
                    UpmClient.instance.ResetProductCache();
                }
                else
                {
                    AssetStoreUtils.instance.RegisterDownloadDelegate(this);
                }
            }

            public void AbortAllDownloads()
            {
                var currentDownloads = m_Downloads.Values.Where(v => v.state == DownloadProgress.State.Started || v.state == DownloadProgress.State.InProgress)
                    .Select(v => long.Parse(v.productId)).ToArray();
                m_Downloads.Clear();

                foreach (var download in currentDownloads)
                    AssetStoreDownloadOperation.instance.AbortDownloadPackageAsync(download);
            }

            public void RefreshProductUpdateDetails(IEnumerable<LocalInfo> localInfos = null)
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
                m_LocalInfos = new Dictionary<string, LocalInfo>();
                foreach (var info in infos)
                {
                    var parsedInfo = LocalInfo.ParseLocalInfo(info);
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

            private void OnLocalInfoChanged(LocalInfo localInfo)
            {
                var fetchedInfo = m_FetchedInfos.Get(localInfo.id);
                if (fetchedInfo == null)
                    return;
                var package = new AssetStorePackage(fetchedInfo, localInfo);
                onPackagesChanged?.Invoke(new[] { package });
            }

            private void OnLocalInfoRemoved(LocalInfo localInfo)
            {
                var fetchedInfo = m_FetchedInfos.Get(localInfo.id);
                if (fetchedInfo == null)
                    return;
                var package = new AssetStorePackage(fetchedInfo, (LocalInfo)null);
                onPackagesChanged?.Invoke(new[] { package });
            }
        }
    }
}
