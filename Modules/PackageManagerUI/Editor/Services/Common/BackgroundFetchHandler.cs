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
    internal class BackgroundFetchHandler : ISerializationCallbackReceiver
    {
        public virtual event Action onCheckUpdateProgress = delegate {};

        internal const int k_MaxFetchUpdateInfoCount = 30;
        internal const int k_MaxFetchPurchaseInfoCount = 30;
        internal const int k_FetchProductInfoCountPerUpdate = 5;
        internal const int k_MaxFetchProductInfoCount = 20;
        internal const int k_ExtraFetchPackageInfoCountPerUpdate = 5;
        internal const int k_MaxExtraFetchPackageInfoCount = 20;

        public virtual bool isCheckUpdateInProgress => m_UnityConnect.isUserLoggedIn && (m_CheckUpdateInProgress.Any() || m_CheckUpdateStack.Any());

        public virtual int checkUpdatePercentage
        {
            get
            {
                var numDownloadedAssets = m_AssetStoreCache.localInfos.Count();
                var numItemsChecked = numDownloadedAssets - m_CheckUpdateStack.Distinct().Count();
                return numItemsChecked <= 0 || numDownloadedAssets <= 0 ? 0 : Math.Min(100, numItemsChecked * 100 / numDownloadedAssets);
            }
        }

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private FetchStatusTracker m_FetchStatusTracker;
        [NonSerialized]
        private PageManager m_PageManager;
        [NonSerialized]
        private PageRefreshHandler m_PageRefreshHandler;
        public void ResolveDependencies(ApplicationProxy application,
            UnityConnectProxy unityConnect,
            UpmCache upmCache,
            UpmClient upmClient,
            AssetStoreClientV2 assetStoreClient,
            AssetStoreCache assetStoreCache,
            FetchStatusTracker fetchStatusTracker,
            PageManager pageManager,
            PageRefreshHandler pageRefreshHandler)
        {
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_UpmCache = upmCache;
            m_UpmClient = upmClient;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreCache = assetStoreCache;
            m_FetchStatusTracker = fetchStatusTracker;
            m_PageManager = pageManager;
            m_PageRefreshHandler = pageRefreshHandler;
        }

        // We keep a queue in addition to the hash-set so that we can fetch product info in order and skip the ones that's not in the hash-set.
        // We only do this for product info because we need to cancel fetches when users are scrolling very fast through the My Assets page.
        [NonSerialized]
        private Queue<long> m_FetchProductInfoQueue = new();
        [NonSerialized]
        private HashSet<long> m_ProductInfosToFetch = new();
        [NonSerialized]
        private HashSet<long> m_FetchProductInfoInProgress = new();

        [NonSerialized]
        private Queue<long> m_FetchPurchaseInfoQueue = new();
        [NonSerialized]
        private List<long> m_FetchPurchaseInfoInProgress = new();

        [NonSerialized]
        private Queue<string> m_ExtraFetchPackageInfoQueue = new();
        [NonSerialized]
        private HashSet<string> m_ExtraFetchPackageInfoInProgress = new();
        [NonSerialized]
        private Dictionary<string, long> m_PackageNameToProductIdMap = new();

        [NonSerialized]
        private Stack<long> m_CheckUpdateStack = new();
        [NonSerialized]
        private HashSet<long> m_CheckUpdateInProgress = new();
        [NonSerialized]
        private HashSet<long> m_ProductIdsToForceCheckUpdate = new();
        [SerializeField]
        private bool m_RefreshAfterCheckUpdates;

        [SerializeField]
        private long[] m_SerializedCheckUpdateStack = Array.Empty<long>();
        [SerializeField]
        private long[] m_SerializedFetchPurchaseInfoQueue = Array.Empty<long>();
        [SerializeField]
        private List<long> m_SerializedFetchProductInfoQueue = new();
        [SerializeField]
        private string[] m_SerializedExtraFetchPackageInfoQueue = Array.Empty<string>();
        [SerializeField]
        private string[] m_SerializedPackageNameToProductIdMapKeys = Array.Empty<string>();
        [SerializeField]
        private long[] m_SerializedPackageNameToProductIdMapValues = Array.Empty<long>();

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_Application.update += ProcessFetchQueue;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
            m_Application.update -= ProcessFetchQueue;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
        }

        public void OnBeforeSerialize()
        {
            // We need to re-trigger all the fetching after serialization, so we just add the current in progress product ids
            // back to the queue/stack to make the queue mechanism handle it properly
            m_SerializedCheckUpdateStack = m_CheckUpdateStack.Concat(m_CheckUpdateInProgress).ToArray();
            m_SerializedFetchPurchaseInfoQueue = m_FetchPurchaseInfoInProgress.Concat(m_FetchPurchaseInfoQueue).ToArray();

            m_SerializedFetchProductInfoQueue = m_FetchProductInfoInProgress
                .Concat(m_FetchProductInfoQueue.Where(id => m_ProductInfosToFetch.Remove(id))).ToList();

            m_SerializedExtraFetchPackageInfoQueue = m_ExtraFetchPackageInfoInProgress.Concat(m_ExtraFetchPackageInfoQueue).ToArray();
            m_SerializedPackageNameToProductIdMapKeys = m_PackageNameToProductIdMap.Keys.ToArray();
            m_SerializedPackageNameToProductIdMapValues = m_PackageNameToProductIdMap.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_CheckUpdateStack = new Stack<long>(m_SerializedCheckUpdateStack);
            m_FetchPurchaseInfoQueue = new Queue<long>(m_SerializedFetchPurchaseInfoQueue);

            m_FetchProductInfoQueue = new Queue<long>(m_SerializedFetchProductInfoQueue);
            m_ProductInfosToFetch = m_FetchProductInfoQueue.ToHashSet();

            m_ExtraFetchPackageInfoQueue = new Queue<string>(m_SerializedExtraFetchPackageInfoQueue);
            for (var i = 0; i < m_SerializedPackageNameToProductIdMapKeys.Length; ++i)
                m_PackageNameToProductIdMap[m_SerializedPackageNameToProductIdMapKeys[i]] = m_SerializedPackageNameToProductIdMapValues[i];
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            PushToCheckUpdateStack(addedOrUpdated?.Where(info =>
                m_AssetStoreCache.GetPurchaseInfo(info.productId) != null &&
                m_AssetStoreCache.GetUpdateInfo(info.productId) == null).Select(info => info.productId));
        }

        private void OnUserLoginStateChange(bool isUserInfoReady, bool isUserLoggedIn)
        {
            if (!isUserLoggedIn)
            {
                ClearFetchProductInfo();
                ClearFetchPurchaseInfo();
                CancelCheckUpdates();
                return;
            }

            var activePage = m_PageManager.activePage;
            if (activePage.filters.status == PageFilters.Status.UpdateAvailable || activePage.id == InProjectPage.k_Id && m_AssetStoreCache.importedPackages.Any())
            {
                m_AssetStoreClient.RefreshLocal();
                CheckUpdateForUncheckedLocalInfos();
            }

            foreach (var fetchStatus in m_FetchStatusTracker.fetchStatuses)
                if (fetchStatus.GetFetchError(FetchType.ProductInfo)?.error.errorCode == UIErrorCode.UserNotSignedIn)
                    AddToFetchProductInfoQueue(fetchStatus.productId);
        }

        private void OnInternetReachabilityChange(bool isInternetReachable)
        {
            if (!isInternetReachable)
                return;

            foreach (var fetchStatus in m_FetchStatusTracker.fetchStatuses)
                // We don't want to create a InternetReachability specific error code because that API is unreliable.
                // However, we check these 2 errors only because they are the only ones possible in an offline case.
                if (fetchStatus.GetFetchError(FetchType.ProductInfo)?.error.errorCode is UIErrorCode.AssetStoreAuthorizationError or UIErrorCode.AssetStoreRestApiError)
                    AddToFetchProductInfoQueue(fetchStatus.productId);
        }

        public virtual void AddToFetchProductInfoQueue(long productId)
        {
            m_FetchProductInfoQueue.Enqueue(productId);
            m_ProductInfosToFetch.Add(productId);
        }

        public virtual void RemoveFromFetchProductInfoQueue(long productId)
        {
            m_ProductInfosToFetch.Remove(productId);
        }

        public virtual void ClearFetchProductInfo()
        {
            m_FetchProductInfoQueue.Clear();
            m_FetchProductInfoInProgress.Clear();
            m_ProductInfosToFetch.Clear();
        }

        private void FetchProductInfoFromQueue()
        {
            // When user is signed out or offline, we still want to proceed because we want the error to be shown in the UI.
            // However, when the user info is not ready, it usually means unity just boot up and we can just wait.
            if (!m_UnityConnect.isUserInfoReady)
                return;

            var numItemsAdded = 0;
            while (m_FetchProductInfoQueue.Any() && numItemsAdded < k_FetchProductInfoCountPerUpdate && m_FetchProductInfoInProgress.Count < k_MaxFetchProductInfoCount)
            {
                var productId = m_FetchProductInfoQueue.Dequeue();
                if (m_FetchProductInfoInProgress.Contains(productId) || !m_ProductInfosToFetch.Remove(productId) || m_AssetStoreCache.GetProductInfo(productId) != null)
                    continue;
                m_FetchProductInfoInProgress.Add(productId);
                numItemsAdded++;
                m_AssetStoreClient.FetchProductInfo(productId, () => m_FetchProductInfoInProgress.Remove(productId));
            }
        }

        public virtual void AddToExtraFetchPackageInfoQueue(string packageNameOrId, long productId = 0)
        {
            m_ExtraFetchPackageInfoQueue.Enqueue(packageNameOrId);
            if (productId > 0)
                m_PackageNameToProductIdMap[packageNameOrId] = productId;
        }

        public virtual void ClearExtraFetchPackageInfo()
        {
            m_ExtraFetchPackageInfoQueue.Clear();
            m_PackageNameToProductIdMap.Clear();
            m_ExtraFetchPackageInfoInProgress.Clear();
        }

        private void ExtraFetchPackageInfoFromQueue()
        {
            var numItemsAdded = 0;
            while (m_ExtraFetchPackageInfoQueue.Any() && numItemsAdded < k_ExtraFetchPackageInfoCountPerUpdate && m_ExtraFetchPackageInfoInProgress.Count < k_MaxExtraFetchPackageInfoCount)
            {
                var packageNameOrId = m_ExtraFetchPackageInfoQueue.Dequeue();
                if (m_ExtraFetchPackageInfoInProgress.Contains(packageNameOrId) || m_UpmCache.GetProductSearchPackageInfo(packageNameOrId) != null || m_UpmCache.GetExtraPackageInfo(packageNameOrId) != null)
                    continue;
                m_ExtraFetchPackageInfoInProgress.Add(packageNameOrId);
                numItemsAdded++;
                m_PackageNameToProductIdMap.TryGetValue(packageNameOrId, out var productId);
                m_UpmClient.ExtraFetchPackageInfo(packageNameOrId, productId, doneCallback: () =>
                {
                    m_ExtraFetchPackageInfoInProgress.Remove(packageNameOrId);
                    m_PackageNameToProductIdMap.Remove(packageNameOrId);
                });
            }
        }

        public virtual void AddToFetchPurchaseInfoQueue(long productId)
        {
            m_FetchPurchaseInfoQueue.Enqueue(productId);
        }

        public virtual void ClearFetchPurchaseInfo()
        {
            m_FetchPurchaseInfoQueue.Clear();
            m_FetchPurchaseInfoInProgress.Clear();
        }

        private void FetchPurchaseInfoFromQueue()
        {
            if (!m_UnityConnect.isUserInfoReady || !m_FetchPurchaseInfoQueue.Any() || m_FetchPurchaseInfoInProgress.Any())
                return;

            while (m_FetchPurchaseInfoQueue.Any() && m_FetchPurchaseInfoInProgress.Count < k_MaxFetchPurchaseInfoCount)
            {
                var productId = m_FetchPurchaseInfoQueue.Dequeue();
                if (m_FetchPurchaseInfoInProgress.Contains(productId) || m_AssetStoreCache.GetPurchaseInfo(productId) != null)
                    continue;
                m_FetchPurchaseInfoInProgress.Add(productId);
            }

            if (m_FetchPurchaseInfoInProgress.Any())
                m_AssetStoreClient.FetchPurchaseInfos(m_FetchPurchaseInfoInProgress, () => m_FetchPurchaseInfoInProgress.Clear());
        }

        private void CheckUpdateFromStack()
        {
            if (!m_UnityConnect.isUserInfoReady || !m_UnityConnect.isUserLoggedIn || m_CheckUpdateInProgress.Any() || !m_CheckUpdateStack.Any())
                return;

            while (m_CheckUpdateStack.Any() && m_CheckUpdateInProgress.Count < k_MaxFetchUpdateInfoCount)
            {
                var productId = m_CheckUpdateStack.Pop();
                if (m_CheckUpdateInProgress.Contains(productId))
                    continue;

                if (m_ProductIdsToForceCheckUpdate.Remove(productId) || m_AssetStoreCache.GetUpdateInfo(productId) == null)
                    m_CheckUpdateInProgress.Add(productId);
            }

            if (m_CheckUpdateInProgress.Any())
            {
                m_AssetStoreClient.FetchUpdateInfos(m_CheckUpdateInProgress, () =>
                {
                    m_CheckUpdateInProgress.Clear();
                    onCheckUpdateProgress?.Invoke();

                    if (m_RefreshAfterCheckUpdates && !m_CheckUpdateStack.Any())
                    {
                        var page = m_PageManager.activePage;
                        if (page.filters.status == PageFilters.Status.UpdateAvailable)
                            m_PageRefreshHandler.Refresh(page);
                        m_RefreshAfterCheckUpdates = false;
                    }
                });
            }
            onCheckUpdateProgress?.Invoke();
        }

        public virtual void PushToCheckUpdateStack(long productId, bool forceCheckUpdate = false)
        {
            if (productId <= 0)
                return;
            if (forceCheckUpdate)
                m_ProductIdsToForceCheckUpdate.Add(productId);
            m_CheckUpdateStack.Push(productId);
        }

        public virtual void PushToCheckUpdateStack(IEnumerable<long> productIds, bool forceCheckUpdate = false)
        {
            foreach (var productId in productIds?.Reverse() ?? Enumerable.Empty<long>())
                PushToCheckUpdateStack(productId, forceCheckUpdate);
        }

        public virtual void ForceCheckUpdateForAllLocalInfos()
        {
            PushToCheckUpdateStack(m_AssetStoreCache.localInfos.Select(info => info.productId), true);
        }

        public virtual void CancelCheckUpdates()
        {
            m_CheckUpdateStack.Clear();
            m_ProductIdsToForceCheckUpdate.Clear();
        }

        public virtual void CheckUpdateForUncheckedLocalInfos()
        {
            var missingLocalInfos = m_AssetStoreCache.localInfos
                .Where(info => m_AssetStoreCache.GetUpdateInfo(info?.productId) == null)
                .Select(info => info.productId).ToArray();

            if (!missingLocalInfos.Any())
                return;

            PushToCheckUpdateStack(missingLocalInfos);
            m_RefreshAfterCheckUpdates = true;
        }

        private void ProcessFetchQueue()
        {
            FetchProductInfoFromQueue();
            FetchPurchaseInfoFromQueue();
            CheckUpdateFromStack();
            ExtraFetchPackageInfoFromQueue();
        }
    }
}
