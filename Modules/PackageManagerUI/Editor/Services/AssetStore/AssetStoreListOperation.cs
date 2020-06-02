// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreListOperation : IOperation
    {
        public string specialUniqueId => string.Empty;

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
        private PurchasesQueryArgs m_QueryArgs;
        public PurchasesQueryArgs queryArgs => m_QueryArgs;

        [SerializeField]
        private AssetStorePurchases m_Result;
        public AssetStorePurchases result => m_Result;

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreRestAPI m_AssetStoreRestAPI;
        public void ResolveDependencies(UnityConnectProxy unityConnect, AssetStoreRestAPI assetStoreRestAPI)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreRestAPI = assetStoreRestAPI;
        }

        private AssetStoreListOperation()
        {
        }

        public AssetStoreListOperation(UnityConnectProxy unityConnect, AssetStoreRestAPI assetStoreRestAPI)
        {
            ResolveDependencies(unityConnect, assetStoreRestAPI);
        }

        public void Start(PurchasesQueryArgs queryArgs = null)
        {
            m_QueryArgs = queryArgs;
            m_IsInProgress = true;
            m_Timestamp = DateTime.Now.Ticks;

            if (!m_UnityConnect.isUserLoggedIn)
            {
                OnOperationError(new UIError(UIErrorCode.AssetStoreOperationError, L10n.Tr("User not logged in.")));
                return;
            }

            m_AssetStoreRestAPI.GetPurchases(queryArgs.ToString(), result =>
            {
                if (!m_UnityConnect.isUserLoggedIn)
                {
                    OnOperationError(new UIError(UIErrorCode.AssetStoreOperationError, L10n.Tr("User not logged in.")));
                    return;
                }

                m_Result = new AssetStorePurchases(this.queryArgs);
                m_Result.ParsePurchases(result);
                onOperationSuccess?.Invoke(this);

                FinalizedOperation();
            }, error => OnOperationError(error));
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
