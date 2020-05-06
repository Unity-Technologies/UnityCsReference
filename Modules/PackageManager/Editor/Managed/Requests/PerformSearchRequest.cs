// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    internal sealed partial class PerformSearchRequest : Request<SearchResults>
    {
        [SerializeField]
        private SearchOptions m_Options;

        /// <summary>
        /// Gets the search options.
        /// </summary>
        public SearchOptions Options
        {
            get
            {
                return m_Options;
            }
        }

        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private PerformSearchRequest()
        {
        }

        internal PerformSearchRequest(long operationId, NativeStatusCode initialStatus, SearchOptions options)
            : base(operationId, initialStatus)
        {
            m_Options = options;
        }

        protected override SearchResults GetResult()
        {
            return GetOperationData(Id);
        }
    }
}
