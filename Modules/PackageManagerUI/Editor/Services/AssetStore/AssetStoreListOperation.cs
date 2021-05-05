// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreListOperation : IOperation
    {
        private const int k_QueryLimit = 500;

        public string packageUniqueId => string.Empty;

        public string versionUniqueId => string.Empty;

        [SerializeField]
        protected long m_Timestamp;
        public long timestamp => m_Timestamp;

        public long lastSuccessTimestamp => 0;

        public bool isOfflineMode => false;

        [SerializeField]
        protected bool m_IsInProgress;
        public bool isInProgress => m_IsInProgress;

        public bool isProgressVisible => false;

        public RefreshOptions refreshOptions => RefreshOptions.Purchased;

        public bool isProgressTrackable => false;

        public float progressPercentage => 0;

        public event Action<IOperation, UIError> onOperationError = delegate {};
        public event Action<IOperation> onOperationSuccess = delegate {};
        public event Action<IOperation> onOperationFinalized = delegate {};
        public event Action<IOperation> onOperationProgress = delegate {};

        [SerializeField]
        private PurchasesQueryArgs m_OriginalQueryArgs;
        [SerializeField]
        private PurchasesQueryArgs m_AdjustedQueryArgs;

        [SerializeField]
        private bool m_DownloadAssetsOnly;

        [SerializeField]
        private AssetStorePurchases m_Result;
        public AssetStorePurchases result => m_Result;

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreRestAPI m_AssetStoreRestAPI;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        public void ResolveDependencies(UnityConnectProxy unityConnect, AssetStoreRestAPI assetStoreRestAPI, AssetStoreCache assetStoreCache)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreRestAPI = assetStoreRestAPI;
            m_AssetStoreCache = assetStoreCache;
        }

        private AssetStoreListOperation()
        {
        }

        public AssetStoreListOperation(UnityConnectProxy unityConnect, AssetStoreRestAPI assetStoreRestAPI, AssetStoreCache assetStoreCache)
        {
            ResolveDependencies(unityConnect, assetStoreRestAPI, assetStoreCache);
        }

        public static string QueryToString(PurchasesQueryArgs queryArgs)
        {
            var stringBuilder = new StringBuilder($"?offset={queryArgs.startIndex}&limit={queryArgs.limit}", 512);
            var status = queryArgs.status;
            if (!string.IsNullOrEmpty(status))
                stringBuilder.Append($"&status={status}");

            if (!string.IsNullOrEmpty(queryArgs.searchText))
                stringBuilder.Append($"&query={Uri.EscapeDataString(queryArgs.searchText)}");
            if (!string.IsNullOrEmpty(queryArgs.orderBy))
            {
                stringBuilder.Append($"&orderBy={queryArgs.orderBy}");
                stringBuilder.Append(queryArgs.isReverseOrder ? "&order=desc" : "&order=asc");
            }
            if (queryArgs.labels?.Any() ?? false)
                stringBuilder.Append($"&tagging={string.Join(",", queryArgs.labels.Select(label => Uri.EscapeDataString(label)).ToArray())}");
            if (queryArgs.categories?.Any() ?? false)
                stringBuilder.Append($"&categories={string.Join(",", queryArgs.categories.Select(cat => Uri.EscapeDataString(cat)).ToArray())}");
            if (queryArgs.productIds?.Any() ?? false)
                stringBuilder.Append($"&ids={string.Join(",", queryArgs.productIds.ToArray())}");
            return stringBuilder.ToString();
        }

        public void Start(PurchasesQueryArgs queryArgs)
        {
            SetQueryArgs(queryArgs);
            m_IsInProgress = true;
            m_Timestamp = DateTime.Now.Ticks;

            if (!m_UnityConnect.isUserLoggedIn)
            {
                OnOperationError(new UIError(UIErrorCode.AssetStoreOperationError, L10n.Tr("User not logged in.")));
                return;
            }

            m_Result = new AssetStorePurchases(m_OriginalQueryArgs);
            if (m_DownloadAssetsOnly && !m_AdjustedQueryArgs.productIds.Any())
            {
                m_Result.total = 0;
                onOperationSuccess?.Invoke(this);
                FinalizedOperation();
                return;
            }

            // We need to keep a local version of the current timestamp to make sure the callback timestamp is still the original one.
            var localTimestamp = m_Timestamp;
            m_AssetStoreRestAPI.GetPurchases(QueryToString(m_AdjustedQueryArgs), (result) => GetPurchasesCallback(result, localTimestamp), OnOperationError);
        }

        public void Stop()
        {
            m_Timestamp = DateTime.Now.Ticks;
            FinalizedOperation();
        }

        private void SetQueryArgs(PurchasesQueryArgs queryArgs)
        {
            m_OriginalQueryArgs = queryArgs;

            m_DownloadAssetsOnly = m_OriginalQueryArgs.downloadedOnly;
            // The GetPurchases API has a limit of maximum 1000 items (to avoid performance issues)
            // therefore we do some adjustments to the original query args enforce that limit and split
            // the original query to multiple batches. We make a clone before when adjusting is needed
            m_AdjustedQueryArgs = m_OriginalQueryArgs.Clone();
            m_AdjustedQueryArgs.limit = Math.Min(m_OriginalQueryArgs.limit, k_QueryLimit);

            if (m_DownloadAssetsOnly)
            {
                m_AdjustedQueryArgs.statuses = new List<string>();
                m_AdjustedQueryArgs.productIds = m_AssetStoreCache.localInfos.Select(info => info.id).ToList();
            }
        }

        private void GetPurchasesCallback(IDictionary<string, object> result, long operationTimestamp)
        {
            if (operationTimestamp != m_Timestamp)
                return;

            if (!m_UnityConnect.isUserLoggedIn)
            {
                OnOperationError(new UIError(UIErrorCode.AssetStoreOperationError, L10n.Tr("User not logged in.")));
                return;
            }

            m_Result.AppendPurchases(result);

            if (m_OriginalQueryArgs.limit > k_QueryLimit && m_IsInProgress)
            {
                var numAssetsToFetch = (int)m_Result.total - m_OriginalQueryArgs.startIndex;
                var numAlreadyFetched = m_Result.list.Count;
                var newLimit = Math.Min(k_QueryLimit, Math.Min(numAssetsToFetch, m_OriginalQueryArgs.limit) - numAlreadyFetched);
                if (newLimit > 0)
                {
                    m_AdjustedQueryArgs.startIndex = m_OriginalQueryArgs.startIndex + m_Result.list.Count;
                    m_AdjustedQueryArgs.limit = newLimit;
                    m_AssetStoreRestAPI.GetPurchases(QueryToString(m_AdjustedQueryArgs), (result) => GetPurchasesCallback(result, operationTimestamp), OnOperationError);
                    return;
                }
            }

            onOperationSuccess?.Invoke(this);
            FinalizedOperation();
        }

        private void OnOperationError(UIError error)
        {
            onOperationError?.Invoke(this, error);
            FinalizedOperation();
        }

        private void FinalizedOperation()
        {
            m_IsInProgress = false;
            onOperationFinalized?.Invoke(this);

            onOperationError = delegate {};
            onOperationFinalized = delegate {};
            onOperationSuccess = delegate {};
            onOperationProgress = delegate {};
        }
    }
}
