// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum FetchType
    {
        None                    = 0,
        ProductInfo             = 1 << 0,
        ProductSearchInfo       = 1 << 1,
    }

    [Serializable]
    internal class FetchError
    {
        public FetchType fetchType;
        public UIError error;
    }

    [Serializable]
    internal class FetchStatus
    {
        public long productId;

        public FetchType fetchingInProgress;
        public FetchType fetchingFinished;
        public List<FetchError> errors;

        public FetchStatus(long productId)
        {
            this.productId = productId;
            fetchingInProgress  = FetchType.None;
            fetchingFinished = FetchType.None;
            errors = new List<FetchError>();
        }

        public bool IsFetchInProgress(FetchType fetchType) => (fetchingInProgress & fetchType) != 0;
        public FetchError GetFetchError(FetchType fetchType) => errors.FirstOrDefault(error => (error.fetchType & fetchType) != 0);
    }

    [Serializable]
    internal class FetchStatusTracker : ISerializationCallbackReceiver
    {
        private Dictionary<long, FetchStatus> m_FetchStatuses = new Dictionary<long, FetchStatus>();
        public IEnumerable<FetchStatus> fetchStatuses => m_FetchStatuses.Values;

        public virtual event Action<FetchStatus> onFetchStatusChanged;

        [SerializeField]
        private FetchStatus[] m_SerializedFetchStatuses = new FetchStatus[0];

        public FetchStatus GetOrCreateFetchStatus(long productId)
        {
            var status = m_FetchStatuses.Get(productId);
            if (status == null)
            {
                status = new FetchStatus(productId);
                m_FetchStatuses[productId] = status;
            }
            return status;
        }

        public virtual void SetFetchInProgress(long productId, FetchType fetchType)
        {
            var status = GetOrCreateFetchStatus(productId);
            status.fetchingInProgress |= fetchType;
            onFetchStatusChanged?.Invoke(status);
        }

        public virtual void SetFetchSuccess(long productId, FetchType fetchType)
        {
            var status = GetOrCreateFetchStatus(productId);
            status.fetchingInProgress &= ~fetchType;
            status.fetchingFinished |= fetchType;
            status.errors.RemoveAll(e => e.fetchType == fetchType);
            onFetchStatusChanged?.Invoke(status);
        }

        public virtual void SetFetchError(long productId, FetchType fetchType, UIError error)
        {
            var status = GetOrCreateFetchStatus(productId);
            status.fetchingInProgress &= ~fetchType;
            status.fetchingFinished |= fetchType;
            status.errors.Add(new FetchError { fetchType = fetchType, error = error });
            onFetchStatusChanged?.Invoke(status);
        }

        public virtual void ClearCache()
        {
            m_FetchStatuses.Clear();
        }

        public void OnBeforeSerialize()
        {
            m_SerializedFetchStatuses = m_FetchStatuses.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_FetchStatuses = m_SerializedFetchStatuses.ToDictionary(status => status.productId, status => status);
        }

    }
}
