// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed class SearchRequest : Request<PackageInfo[]>
    {
        [SerializeField]
        private string m_PackageIdOrName;

        /// <summary>
        /// Gets the package being searched
        /// </summary>
        public string PackageIdOrName
        {
            get
            {
                return m_PackageIdOrName;
            }
        }

        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private SearchRequest()
        {
        }

        internal SearchRequest(long operationId, NativeClient.StatusCode initialStatus, string packageIdOrName)
            : base(operationId, initialStatus)
        {
            m_PackageIdOrName = packageIdOrName;
        }

        protected override PackageInfo[] GetResult()
        {
            return NativeClient.GetSearchOperationData(Id).Select(p => (PackageInfo)p).ToArray();
        }
    }
}

