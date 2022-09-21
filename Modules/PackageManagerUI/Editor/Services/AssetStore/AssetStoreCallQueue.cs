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
    internal class AssetStoreCallQueue : ISerializationCallbackReceiver
    {
        public virtual event Action onCheckUpdateProgress = delegate {};

        internal const int k_CheckUpdateChunkSize = 30;

        internal const int k_FetchProductInfoCountPerUpdate = 5;
        internal const int k_MaxFetchProductInfoCount = 20;

        public virtual bool isCheckUpdateInProgress => m_CheckUpdateInProgress || m_CheckUpdateStack.Any();

        public virtual int checkUpdatePercentage
        {
            get
            {
                var numDownloadedAssets = m_AssetStoreCache.localInfos.Count();
                var numItemsChecked = numDownloadedAssets - m_ForceCheckUpdateLookup.Count;
                return numItemsChecked <= 0 || numDownloadedAssets <= 0 ? 0 : Math.Min(100, numItemsChecked * 100 / numDownloadedAssets);
            }
        }

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private PageManager m_PageManager;
        [NonSerialized]
        private PageRefreshHandler m_PageRefreshHandler;
        public void ResolveDependencies(ApplicationProxy application,
            UnityConnectProxy unityConnect,
            PackageManagerPrefs packageManagerPrefs,
            AssetStoreClientV2 assetStoreClient,
            AssetStoreCache assetStoreCache,
            PageManager pageManager,
            PageRefreshHandler pageRefreshHandler)
        {
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreCache = assetStoreCache;
            m_PageManager = pageManager;
            m_PageRefreshHandler = pageRefreshHandler;
        }

        [NonSerialized]
        private readonly HashSet<long> m_CurrentFetchProductInfo = new HashSet<long>();

        // We keep a queue in addition to the hash-set so that we can fetch product info in order (and skip the ones that's not in the hash-set)
        [NonSerialized]
        private Queue<long> m_FetchProductInfoQueue = new Queue<long>();
        [NonSerialized]
        private readonly HashSet<long> m_ProductInfosToFetch = new HashSet<long>();

        [SerializeField]
        private bool m_RefreshAfterCheckUpdates = false;

        [SerializeField]
        private long[] m_SerializedCheckUpdateStack = new long[0];

        [SerializeField]
        private long[] m_SerializedForceCheckUpdateLookupKeys = new long[0];

        [SerializeField]
        private bool[] m_SerializedForceCheckUpdateLookupValues = new bool[0];

        [NonSerialized]
        private bool m_CheckUpdateInProgress = false;

        [NonSerialized]
        private Stack<long> m_CheckUpdateStack = new Stack<long>();

        // This dictionary serves two purposes - the keys of this dictionary are unique asset product ids that we need to check updates
        // and the values of this dictionary tell us whether these assets needs a `force check update`. If `forceCheckUpdate` is false,
        // we'll skip the check for updates if there's already cached previous check results.
        private Dictionary<long, bool> m_ForceCheckUpdateLookup = new Dictionary<long, bool>();

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_PackageManagerPrefs.onFilterTabChanged += OnFilterChanged;
            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_Application.update += ProcessCallQueue;
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_PackageManagerPrefs.onFilterTabChanged -= OnFilterChanged;
            m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
            m_Application.update -= ProcessCallQueue;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedCheckUpdateStack = m_CheckUpdateStack.ToArray();
            m_SerializedForceCheckUpdateLookupKeys = m_ForceCheckUpdateLookup.Keys.ToArray();
            m_SerializedForceCheckUpdateLookupValues = m_ForceCheckUpdateLookup.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_CheckUpdateStack = new Stack<long>(m_SerializedCheckUpdateStack);

            for (var i = 0; i < m_SerializedForceCheckUpdateLookupKeys.Length; i++)
                m_ForceCheckUpdateLookup[m_SerializedForceCheckUpdateLookupKeys[i]] = m_SerializedForceCheckUpdateLookupValues[i];
        }

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            if (m_PackageManagerPrefs.previousFilterTab == PackageFilterTab.AssetStore)
                ClearFetchProductInfo();
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            InsertToCheckUpdateQueue(addedOrUpdated?.Where(info => m_AssetStoreCache.GetPurchaseInfo(info.productId) != null && m_AssetStoreCache.GetUpdateInfo(info.productId) == null).Select(info => info.productId));
        }

        private void OnUserLoginStateChange(bool isUserInfoReady, bool isUserLoggedIn)
        {
            if (!isUserLoggedIn)
                return;

            var page = m_PageManager.GetPage();
            if (page.filters.updateAvailableOnly)
            {
                m_AssetStoreClient.RefreshLocal();
                CheckUpdateForUncheckedLocalInfos();
            }
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
            m_CurrentFetchProductInfo.Clear();
            m_ProductInfosToFetch.Clear();
        }

        private void FetchProductInfoFromQueue()
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return;

            var numItemsAdded = 0;
            while (m_FetchProductInfoQueue.Any() && numItemsAdded < k_FetchProductInfoCountPerUpdate && m_CurrentFetchProductInfo.Count < k_MaxFetchProductInfoCount)
            {
                var productId = m_FetchProductInfoQueue.Dequeue();
                if (!m_ProductInfosToFetch.Remove(productId))
                    continue;
                m_CurrentFetchProductInfo.Add(productId);
                numItemsAdded++;
                m_AssetStoreClient.FetchProductInfo(productId, () => m_CurrentFetchProductInfo.Remove(productId));
            }
        }
        private void CheckUpdateFromStack()
        {
            if (!m_UnityConnect.isUserLoggedIn || m_CheckUpdateInProgress || !m_CheckUpdateStack.Any())
                return;

            var checkUpdateList = new List<long>(k_CheckUpdateChunkSize);
            while (m_CheckUpdateStack.Any() && checkUpdateList.Count < k_CheckUpdateChunkSize)
            {
                var productId = m_CheckUpdateStack.Pop();
                if (m_ForceCheckUpdateLookup.TryGetValue(productId, out var forceCheck))
                {
                    if (forceCheck || m_AssetStoreCache.GetUpdateInfo(productId) == null)
                        checkUpdateList.Add(productId);
                    m_ForceCheckUpdateLookup.Remove(productId);
                }
            }

            if (checkUpdateList.Any())
            {
                m_CheckUpdateInProgress = true;
                m_AssetStoreClient.FetchUpdateInfos(checkUpdateList, () =>
                {
                    m_CheckUpdateInProgress = false;
                    onCheckUpdateProgress?.Invoke();

                    if (m_RefreshAfterCheckUpdates && !m_CheckUpdateStack.Any())
                    {
                        var page = m_PageManager.GetPage();
                        if (page.filters.updateAvailableOnly)
                            m_PageRefreshHandler.Refresh();
                        m_RefreshAfterCheckUpdates = false;
                    }
                });
            }
            onCheckUpdateProgress?.Invoke();
        }

        public virtual void InsertToCheckUpdateQueue(long productId, bool forceCheckUpdate = false)
        {
            if (productId <= 0)
                return;
            if (forceCheckUpdate || !m_ForceCheckUpdateLookup.TryGetValue(productId, out var oldForceCheckValue) || !oldForceCheckValue)
                m_ForceCheckUpdateLookup[productId] = forceCheckUpdate;
            m_CheckUpdateStack.Push(productId);
        }

        public virtual void InsertToCheckUpdateQueue(IEnumerable<long> productIds, bool forceCheckUpdate = false)
        {
            foreach (var productId in productIds?.Reverse() ?? Enumerable.Empty<long>())
                InsertToCheckUpdateQueue(productId, forceCheckUpdate);
        }

        public virtual void ForceCheckUpdateForAllLocalInfos()
        {
            InsertToCheckUpdateQueue(m_AssetStoreCache.localInfos.Select(info => info.productId), true);
        }

        public virtual void CancelCheckUpdates()
        {
            m_CheckUpdateStack.Clear();
            m_ForceCheckUpdateLookup.Clear();
        }

        public virtual void CheckUpdateForUncheckedLocalInfos()
        {
            InsertToCheckUpdateQueue(m_AssetStoreCache.localInfos.Where(info => m_AssetStoreCache.GetUpdateInfo(info?.productId) == null).Select(info => info.productId));
            m_RefreshAfterCheckUpdates = true;
        }

        private void ProcessCallQueue()
        {
            FetchProductInfoFromQueue();
            CheckUpdateFromStack();
        }
    }
}
