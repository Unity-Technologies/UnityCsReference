// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class SearchRequest : Request<PackageInfo[]>
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

        internal SearchRequest(long operationId, NativeStatusCode initialStatus, string packageIdOrName)
            : base(operationId, initialStatus)
        {
            // This class is used to wrap both "GetPackageInfo" and "GetAllPackageInfo" operations.
            // When used for the latter, packageIdOrName == string.Empty. Aside from that, the SearchRequest
            // class does not care whether it's "searching" for a single PackageInfo or all "searchable" packages.
            m_PackageIdOrName = packageIdOrName;
        }

        protected override PackageInfo[] GetResult()
        {
            return GetOperationData(Id).Where(p => p.type != ShimPackageType).ToArray();
        }
    }
}
