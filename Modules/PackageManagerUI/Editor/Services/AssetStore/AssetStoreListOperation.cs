// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    [Serializable]
    internal class AssetStoreListOperation : IOperation
    {
        public string specialUniqueId => string.Empty;

        public string packageUniqueId => string.Empty;

        public string versionUniqueId => string.Empty;

        // a timestamp is added to keep track of how `refresh` the result it
        // in the case of an online operation, it is the time when the operation starts
        // in the case of an offline operation, it is set to the timestamp of the last online operation
        [SerializeField]
        protected long m_Timestamp = 0;
        public long timestamp { get { return m_Timestamp; } }

        public long lastSuccessTimestamp => 0;

        public bool isOfflineMode => false;

        [SerializeField]
        protected bool m_IsInProgress = false;
        public bool isInProgress => m_IsInProgress;

        public RefreshOptions refreshOptions => RefreshOptions.Purchased;

        public event Action<IOperation, Error> onOperationError;
        public event Action<IOperation> onOperationSuccess;
        public event Action<IOperation> onOperationFinalized;

        public void Start()
        {
            m_IsInProgress = true;
            m_Timestamp = DateTime.Now.Ticks;
        }

        public void TriggerOperationError(Error error)
        {
            m_IsInProgress = false;
            onOperationError?.Invoke(this, error);
            onOperationFinalized?.Invoke(this);

            onOperationError = delegate {};
            onOperationFinalized = delegate {};
            onOperationSuccess = delegate {};
        }

        public void TriggeronOperationSuccess()
        {
            m_IsInProgress = false;
            onOperationSuccess?.Invoke(this);
            onOperationFinalized?.Invoke(this);

            onOperationError = delegate {};
            onOperationFinalized = delegate {};
            onOperationSuccess = delegate {};
        }
    }
}
