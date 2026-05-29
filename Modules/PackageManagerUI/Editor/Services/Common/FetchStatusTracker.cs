// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IFetchStatusTracker : IService
    {
        event Action<long> onProductInfoFetchStatusChanged;
        event Action<string> onSearchInfoFetchStatusChanged;

        IEnumerable<long> trackedProductIds { get; }

        void SetProductInfoFetchInProgress(long productId);
        void SetProductInfoFetchSuccess(long productId);
        void SetProductInfoFetchError(long productId, UIError error);
        FetchStatus GetProductInfoFetchStatus(long productId);

        void SetSearchInfoFetchInProgress(string packageName);
        void SetSearchInfoFetchSuccess(string packageName);
        void SetSearchInfoFetchError(string packageName, UIError error);
        FetchStatus GetSearchInfoFetchStatus(string packageName);

        void ClearProductInfoFetchStatuses();
        void ClearSearchInfoFetchStatuses();
    }

    [Serializable]
    internal struct FetchStatus
    {
        public bool inProgress;
        public UIError error;
    }

    [Serializable]
    internal class FetchStatusTracker : BaseService<IFetchStatusTracker>, IFetchStatusTracker, ISerializationCallbackReceiver
    {
        public event Action<long> onProductInfoFetchStatusChanged;
        public event Action<string> onSearchInfoFetchStatusChanged;

        public IEnumerable<long> trackedProductIds => m_ProductInfoFetchStatuses.Keys;

        private readonly Dictionary<long, FetchStatus> m_ProductInfoFetchStatuses = new ();
        private readonly Dictionary<string, FetchStatus> m_SearchInfoFetchStatuses = new ();

        [SerializeField]
        private long[] m_SerializedProductIds = Array.Empty<long>();
        [SerializeField]
        private FetchStatus[] m_SerializedProductInfoFetchStatuses = Array.Empty<FetchStatus>();

        [SerializeField]
        private string[] m_SerializedPackageNames = Array.Empty<string>();
        [SerializeField]
        private FetchStatus[] m_SerializedSearchInfoFetchStatuses = Array.Empty<FetchStatus>();

        public void SetProductInfoFetchInProgress(long productId)
        {
            var fetchStatus = m_ProductInfoFetchStatuses.GetValueOrDefault(productId);
            fetchStatus.inProgress = true;
            m_ProductInfoFetchStatuses[productId] = fetchStatus;
            onProductInfoFetchStatusChanged?.Invoke(productId);
        }

        public void SetProductInfoFetchSuccess(long productId)
        {
            m_ProductInfoFetchStatuses.Remove(productId);
            onProductInfoFetchStatusChanged?.Invoke(productId);
        }

        public void SetProductInfoFetchError(long productId, UIError error)
        {
            var fetchStatus = m_ProductInfoFetchStatuses.GetValueOrDefault(productId);
            fetchStatus.inProgress = false;
            fetchStatus.error = error;
            m_ProductInfoFetchStatuses[productId] = fetchStatus;
            onProductInfoFetchStatusChanged?.Invoke(productId);
        }

        public FetchStatus GetProductInfoFetchStatus(long productId) => m_ProductInfoFetchStatuses.GetValueOrDefault(productId);

        public void SetSearchInfoFetchInProgress(string packageName)
        {
            var fetchStatus = m_SearchInfoFetchStatuses.GetValueOrDefault(packageName);
            fetchStatus.inProgress = true;
            m_SearchInfoFetchStatuses[packageName] = fetchStatus;
            onSearchInfoFetchStatusChanged?.Invoke(packageName);
        }

        public void SetSearchInfoFetchSuccess(string packageName)
        {
            m_SearchInfoFetchStatuses.Remove(packageName);
            onSearchInfoFetchStatusChanged?.Invoke(packageName);
        }

        public void SetSearchInfoFetchError(string packageName, UIError error)
        {
            var fetchStatus = m_SearchInfoFetchStatuses.GetValueOrDefault(packageName);
            fetchStatus.inProgress = false;
            fetchStatus.error = error;
            m_SearchInfoFetchStatuses[packageName] = fetchStatus;
            onSearchInfoFetchStatusChanged?.Invoke(packageName);
        }

        public FetchStatus GetSearchInfoFetchStatus(string packageName) => m_SearchInfoFetchStatuses.GetValueOrDefault(packageName);

        public void ClearProductInfoFetchStatuses() => m_ProductInfoFetchStatuses.Clear();
        public void ClearSearchInfoFetchStatuses() => m_SearchInfoFetchStatuses.Clear();

        public void OnBeforeSerialize()
        {
            m_ProductInfoFetchStatuses.Keys.ToArray(ref m_SerializedProductIds);
            m_ProductInfoFetchStatuses.Values.ToArray(ref m_SerializedProductInfoFetchStatuses);

            m_SearchInfoFetchStatuses.Keys.ToArray(ref m_SerializedPackageNames);
            m_SearchInfoFetchStatuses.Values.ToArray(ref m_SerializedSearchInfoFetchStatuses);
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedProductIds.Length; i++)
                m_ProductInfoFetchStatuses[m_SerializedProductIds[i]] = m_SerializedProductInfoFetchStatuses[i];
            for (var i = 0; i < m_SerializedPackageNames.Length; i++)
                m_SearchInfoFetchStatuses[m_SerializedPackageNames[i]] = m_SerializedSearchInfoFetchStatuses[i];
        }
    }
}
