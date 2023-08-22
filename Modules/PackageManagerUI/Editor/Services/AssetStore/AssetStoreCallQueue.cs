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

        internal const int k_FetchDetailsCountPerUpdate = 5;
        internal const int k_MaxFetchDetailsCount = 20;

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
        private PackageFiltering m_PackageFiltering;
        [NonSerialized]
        private AssetStoreClient m_AssetStoreClient;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private PageManager m_PageManager;
        public void ResolveDependencies(ApplicationProxy application,
            UnityConnectProxy unityConnect,
            PackageFiltering packageFiltering,
            AssetStoreClient assetStoreClient,
            AssetStoreCache assetStoreCache,
            PageManager pageManager)
        {
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_PackageFiltering = packageFiltering;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreCache = assetStoreCache;
            m_PageManager = pageManager;
        }

        [NonSerialized]
        private readonly HashSet<string> m_CurrentFetchDetails = new HashSet<string>();

        // We keep a queue in addition to the hash-set so that we can fetch details in order (and skip the ones that's not in the hash-set)
        [NonSerialized]
        private Queue<string> m_FetchDetailsQueue = new Queue<string>();
        [NonSerialized]
        private readonly HashSet<string> m_DetailsToFetch = new HashSet<string>();

        [SerializeField]
        private bool m_RefreshAfterCheckUpdates = false;

        [SerializeField]
        private string[] m_SerializedCheckUpdateStack = new string[0];

        [SerializeField]
        private string[] m_SerializedForceCheckUpdateLookupKeys = new string[0];

        [SerializeField]
        private bool[] m_SerializedForceCheckUpdateLookupValues = new bool[0];

        [NonSerialized]
        private bool m_CheckUpdateInProgress = false;

        [NonSerialized]
        private Stack<string> m_CheckUpdateStack = new Stack<string>();

        // This dictionary serves two purposes - the keys of this dictionary are unique asset product ids that we need to check updates
        // and the values of this dictionary tell us whether these assets needs a `force check update`. If `forceCheckUpdate` is false,
        // we'll skip the check for updates if there's already cached previous check results.
        private Dictionary<string, bool> m_ForceCheckUpdateLookup = new Dictionary<string, bool>();

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_PackageFiltering.onFilterTabChanged += OnFilterChanged;
            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_Application.update += ProcessCallQueue;
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_PackageFiltering.onFilterTabChanged -= OnFilterChanged;
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
            m_CheckUpdateStack = new Stack<string>(m_SerializedCheckUpdateStack);

            for (var i = 0; i < m_SerializedForceCheckUpdateLookupKeys.Length; i++)
                m_ForceCheckUpdateLookup[m_SerializedForceCheckUpdateLookupKeys[i]] = m_SerializedForceCheckUpdateLookupValues[i];
        }

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            if (m_PackageFiltering.previousFilterTab == PackageFilterTab.AssetStore)
                ClearFetchDetails();
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            // Right now we only want to check updates for downloaded assets that's also in the purchase list because users
            // don't always load all purchases and we don't want to waste time checking updates for items that are not visible.
            // In the future if we want to check update for all downloaded assets, we can remove the purchase info check here.
            InsertToCheckUpdateQueue(addedOrUpdated?.Where(info => m_AssetStoreCache.GetPurchaseInfo(info.id) != null && m_AssetStoreCache.GetUpdateInfo(info.uploadId) == null).Select(info => info.id));
        }

        private void OnUserLoginStateChange(bool isUserInfoReady, bool isUserLoggedIn)
        {
            if (!isUserLoggedIn)
                return;

            var page = m_PageManager.GetCurrentPage();
            if (page.filters.updateAvailableOnly)
            {
                m_AssetStoreClient.RefreshLocal();
                CheckUpdateForUncheckedLocalInfos();
            }
        }

        public virtual void AddToFetchDetailsQueue(string packageUniqueId)
        {
            m_FetchDetailsQueue.Enqueue(packageUniqueId);
            m_DetailsToFetch.Add(packageUniqueId);
        }

        public virtual void RemoveFromFetchDetailsQueue(string packageUniqueId)
        {
            m_DetailsToFetch.Remove(packageUniqueId);
        }

        public virtual void ClearFetchDetails()
        {
            m_FetchDetailsQueue.Clear();
            m_CurrentFetchDetails.Clear();
            m_DetailsToFetch.Clear();
        }

        private void FetchDetailsFromQueue()
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return;

            var numItemsAdded = 0;
            while (m_FetchDetailsQueue.Any() && numItemsAdded < k_FetchDetailsCountPerUpdate && m_CurrentFetchDetails.Count < k_MaxFetchDetailsCount)
            {
                var packageId = m_FetchDetailsQueue.Dequeue();
                if (m_CurrentFetchDetails.Contains(packageId) || !m_DetailsToFetch.Remove(packageId) || m_AssetStoreCache.GetProductInfo(packageId) != null)
                    continue;
                if (long.TryParse(packageId, out var productId))
                {
                    m_CurrentFetchDetails.Add(packageId);
                    numItemsAdded++;
                    m_AssetStoreClient.FetchDetail(productId, () => m_CurrentFetchDetails.Remove(packageId));
                }
            }
        }

        private void CheckUpdateFromStack()
        {
            if (!m_UnityConnect.isUserLoggedIn || m_CheckUpdateInProgress || !m_CheckUpdateStack.Any())
                return;

            var checkUpdateList = new List<string>(k_CheckUpdateChunkSize);
            while (m_CheckUpdateStack.Any() && checkUpdateList.Count < k_CheckUpdateChunkSize)
            {
                var id = m_CheckUpdateStack.Pop();
                if (m_ForceCheckUpdateLookup.TryGetValue(id, out var forceCheck))
                {
                    if (forceCheck || m_AssetStoreCache.GetUpdateInfo(m_AssetStoreCache.GetLocalInfo(id)?.uploadId) == null)
                        checkUpdateList.Add(id);
                    m_ForceCheckUpdateLookup.Remove(id);
                }
            }

            if (checkUpdateList.Any())
            {
                m_CheckUpdateInProgress = true;
                m_AssetStoreClient.CheckUpdate(checkUpdateList, () =>
                {
                    m_CheckUpdateInProgress = false;
                    onCheckUpdateProgress?.Invoke();

                    if (m_RefreshAfterCheckUpdates && !m_CheckUpdateStack.Any())
                    {
                        var page = m_PageManager.GetCurrentPage();
                        if (page.filters.updateAvailableOnly)
                            m_PageManager.Refresh();
                        m_RefreshAfterCheckUpdates = false;
                    }
                });
            }
            onCheckUpdateProgress?.Invoke();
        }

        public virtual void InsertToCheckUpdateQueue(string productId, bool forceCheckUpdate = false)
        {
            if (!string.IsNullOrEmpty(productId))
            {
                if (forceCheckUpdate || !m_ForceCheckUpdateLookup.TryGetValue(productId, out var oldForceCheckValue) || !oldForceCheckValue)
                    m_ForceCheckUpdateLookup[productId] = forceCheckUpdate;
                m_CheckUpdateStack.Push(productId);
            }
        }

        public virtual void InsertToCheckUpdateQueue(IEnumerable<string> productIds, bool forceCheckUpdate = false)
        {
            foreach (var productId in productIds?.Reverse() ?? Enumerable.Empty<string>())
                InsertToCheckUpdateQueue(productId, forceCheckUpdate);
        }

        public virtual void ForceCheckUpdateForAllLocalInfos()
        {
            InsertToCheckUpdateQueue(m_AssetStoreCache.localInfos.Select(info => info.id), true);
        }

        public virtual void CancelCheckUpdates()
        {
            m_CheckUpdateStack.Clear();
            m_ForceCheckUpdateLookup.Clear();
        }

        public virtual void CheckUpdateForUncheckedLocalInfos()
        {
            InsertToCheckUpdateQueue(m_AssetStoreCache.localInfos.Where(info => m_AssetStoreCache.GetUpdateInfo(info?.uploadId) == null).Select(info => info.id));
            m_RefreshAfterCheckUpdates = true;
        }

        private void ProcessCallQueue()
        {
            FetchDetailsFromQueue();
            CheckUpdateFromStack();
        }
    }
}
