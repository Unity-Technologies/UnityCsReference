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
        public string packageUniqueId => string.Empty;

        public string versionUniqueId => string.Empty;

        [SerializeField]
        protected long m_Timestamp = 0;
        public long timestamp { get { return m_Timestamp; } }

        public long lastSuccessTimestamp => 0;

        public bool isOfflineMode => false;

        [SerializeField]
        protected bool m_IsInProgress = false;
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
        private PurchasesQueryArgs m_AjustedQueryArgs;

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
            if (m_DownloadAssetsOnly && !m_AjustedQueryArgs.productIds.Any())
            {
                m_Result.total = 0;
                onOperationSuccess?.Invoke(this);
                FinalizedOperation();
                return;
            }
            m_AssetStoreRestAPI.GetPurchases(QueryToString(m_AjustedQueryArgs), GetPurchasesCallback, error => OnOperationError(error));
        }

        private void SetQueryArgs(PurchasesQueryArgs queryArgs)
        {
            m_OriginalQueryArgs = queryArgs;

            m_DownloadAssetsOnly = m_OriginalQueryArgs.downloadedOnly;
            if (m_DownloadAssetsOnly)
            {
                m_AjustedQueryArgs = m_OriginalQueryArgs.Clone();
                m_AjustedQueryArgs.statuses = new List<string>();
                m_AjustedQueryArgs.productIds = m_AssetStoreCache.localInfos.Select(info => info.id).ToList();
            }
            else
                m_AjustedQueryArgs = m_OriginalQueryArgs;
        }

        private void GetPurchasesCallback(IDictionary<string, object> result)
        {
            if (!m_UnityConnect.isUserLoggedIn)
            {
                OnOperationError(new UIError(UIErrorCode.AssetStoreOperationError, L10n.Tr("User not logged in.")));
                return;
            }

            m_Result.AppendPurchases(result);

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

            onOperationError = null;
            onOperationFinalized = null;
            onOperationSuccess = null;
        }
    }
}
