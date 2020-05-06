// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    internal sealed partial class GetCachedPackagesRequest : Request<CachedPackageInfo[]>
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private GetCachedPackagesRequest()
            : base()
        {
        }

        internal GetCachedPackagesRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override CachedPackageInfo[] GetResult()
        {
            return GetOperationData(Id);
        }
    }
}
