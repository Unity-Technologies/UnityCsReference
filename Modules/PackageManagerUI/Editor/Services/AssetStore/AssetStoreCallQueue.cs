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
        internal const int k_CheckUpdateChunkSize = 30;

        internal const int k_FetchDetailsCountPerUpdate = 5;
        internal const int k_MaxFetchDetailsCount = 20;


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
        public void ResolveDependencies(ApplicationProxy application,
            UnityConnectProxy unityConnect,
            PackageFiltering packageFiltering,
            AssetStoreClient assetStoreClient,
            AssetStoreCache assetStoreCache)
        {
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_PackageFiltering = packageFiltering;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreCache = assetStoreCache;
        }

        [NonSerialized]
        private readonly HashSet<string> m_CurrentFetchDetails = new HashSet<string>();

        // We keep a queue in addition to the hash-set so that we can fetch details in order (and skip the ones that's not in the hash-set)
        [NonSerialized]
        private Queue<string> m_FetchDetailsQueue = new Queue<string>();
        [NonSerialized]
        private readonly HashSet<string> m_DetailsToFetch = new HashSet<string>();

        [SerializeField]
        private string[] m_SerializedCheckUpdateStack = new string[0];

        [NonSerialized]
        private bool m_CheckUpdateInProgress = false;

        [NonSerialized]
        private Stack<string> m_CheckUpdateStack = new Stack<string>();

        public void OnEnable()
        {
            m_PackageFiltering.onFilterTabChanged += OnFilterChanged;
            m_AssetStoreCache.onLocalInfosChanged += OnLocalInfosChanged;
            m_Application.update += ProcessCallQueue;
        }

        public void OnDisable()
        {
            m_PackageFiltering.onFilterTabChanged -= OnFilterChanged;
            m_AssetStoreCache.onLocalInfosChanged -= OnLocalInfosChanged;
            m_Application.update -= ProcessCallQueue;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedCheckUpdateStack = m_CheckUpdateStack.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_CheckUpdateStack = new Stack<string>(m_SerializedCheckUpdateStack);
        }

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            if (m_PackageFiltering.previousFilterTab == PackageFilterTab.AssetStore)
                Clear();
        }

        private void OnLocalInfosChanged(IEnumerable<AssetStoreLocalInfo> addedOrUpdated, IEnumerable<AssetStoreLocalInfo> removed)
        {
            // Right now we only want to check updates for downloaded assets that's also in the purchase list because users
            // don't always load all purchases and we don't want to waste time checking updates for items that are not visible.
            // In the future if we want to check update for all downloaded assets, we can remove the purchase info check here.
            InsertToCheckUpdateQueue(addedOrUpdated?.Where(info => m_AssetStoreCache.GetPurchaseInfo(info.id) != null && !info.updateInfoFetched).Select(info => info.id));
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

        public virtual void Clear()
        {
            m_FetchDetailsQueue.Clear();
            m_CurrentFetchDetails.Clear();
            m_DetailsToFetch.Clear();
            m_CheckUpdateStack.Clear();
        }

        private void FetchDetailsFromQueue()
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return;

            var numItemsAdded = 0;
            while (m_FetchDetailsQueue.Any() && numItemsAdded < k_FetchDetailsCountPerUpdate && m_CurrentFetchDetails.Count < k_MaxFetchDetailsCount)
            {
                var packageId = m_FetchDetailsQueue.Dequeue();
                if (!m_DetailsToFetch.Remove(packageId))
                    continue;
                if (long.TryParse(packageId, out var productId))
                {
                    m_CurrentFetchDetails.Add(packageId);
                    numItemsAdded++;
                    m_AssetStoreClient.FetchDetail(productId, package => m_CurrentFetchDetails.Remove(packageId));
                }
            }
        }

        private void CheckUpdateFromStack()
        {
            if (!m_UnityConnect.isUserLoggedIn || m_CheckUpdateInProgress || !m_CheckUpdateStack.Any())
                return;

            var checkUpdateList = new List<string>(k_CheckUpdateChunkSize);
            while (m_CheckUpdateStack.Any() && checkUpdateList.Count < k_CheckUpdateChunkSize)
                checkUpdateList.Add(m_CheckUpdateStack.Pop());

            if (checkUpdateList.Any())
            {
                m_CheckUpdateInProgress = true;
                m_AssetStoreClient.CheckUpdate(checkUpdateList, () => m_CheckUpdateInProgress = false);
            }
        }

        public virtual void InsertToCheckUpdateQueue(string productId)
        {
            if (!string.IsNullOrEmpty(productId))
                m_CheckUpdateStack.Push(productId);
        }

        public virtual void InsertToCheckUpdateQueue(IEnumerable<string> productIds)
        {
            foreach (var productId in productIds?.Reverse() ?? Enumerable.Empty<string>())
                InsertToCheckUpdateQueue(productId);
        }

        private void ProcessCallQueue()
        {
            FetchDetailsFromQueue();
            CheckUpdateFromStack();
        }
    }
}
