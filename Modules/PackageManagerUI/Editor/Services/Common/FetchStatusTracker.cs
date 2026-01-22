// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IFetchStatusTracker : IService
    {
        event Action<FetchStatus> onFetchStatusChanged;

        IEnumerable<FetchStatus> fetchStatuses { get; }

        FetchStatus GetOrCreateFetchStatus(long productId);
        void SetFetchInProgress(long productId, FetchType fetchType);
        void SetFetchSuccess(long productId, FetchType fetchType);
        void SetFetchError(long productId, FetchType fetchType, UIError error);
        void ClearCache();
    }

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
        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public FetchError GetFetchError(FetchType fetchType) => errors.FirstOrDefault(error => (error.fetchType & fetchType) != 0);
#pragma warning restore RS0030
    }

    [Serializable]
    internal class FetchStatusTracker : BaseService<IFetchStatusTracker>, IFetchStatusTracker, ISerializationCallbackReceiver
    {
        private Dictionary<long, FetchStatus> m_FetchStatuses = new Dictionary<long, FetchStatus>();
        public IEnumerable<FetchStatus> fetchStatuses => m_FetchStatuses.Values;

        public event Action<FetchStatus> onFetchStatusChanged;

        [SerializeField]
        private FetchStatus[] m_SerializedFetchStatuses = Array.Empty<FetchStatus>();

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

        public void SetFetchInProgress(long productId, FetchType fetchType)
        {
            var status = GetOrCreateFetchStatus(productId);
            status.fetchingInProgress |= fetchType;
            onFetchStatusChanged?.Invoke(status);
        }

        public void SetFetchSuccess(long productId, FetchType fetchType)
        {
            var status = GetOrCreateFetchStatus(productId);
            status.fetchingInProgress &= ~fetchType;
            status.fetchingFinished |= fetchType;
            status.errors.RemoveAll(e => e.fetchType == fetchType);
            onFetchStatusChanged?.Invoke(status);
        }

        public void SetFetchError(long productId, FetchType fetchType, UIError error)
        {
            var status = GetOrCreateFetchStatus(productId);
            status.fetchingInProgress &= ~fetchType;
            status.fetchingFinished |= fetchType;
            status.errors.Add(new FetchError { fetchType = fetchType, error = error });
            onFetchStatusChanged?.Invoke(status);
        }

        public void ClearCache()
        {
            m_FetchStatuses.Clear();
        }

        public void OnBeforeSerialize()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SerializedFetchStatuses = m_FetchStatuses.Values.ToArray();
#pragma warning restore RS0030
        }

        public void OnAfterDeserialize()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_FetchStatuses = m_SerializedFetchStatuses.ToDictionary(status => status.productId, status => status);
#pragma warning restore RS0030
        }

    }
}
