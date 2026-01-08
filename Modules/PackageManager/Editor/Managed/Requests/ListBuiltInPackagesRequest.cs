// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    internal sealed partial class ListBuiltInPackagesRequest : Request<PackageInfo[]>
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private ListBuiltInPackagesRequest()
            : base()
        {
        }

        internal ListBuiltInPackagesRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override PackageInfo[] GetResult()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetOperationData(Id).Where(p => p.type != ShimPackageType).ToArray();
#pragma warning restore RS0030
        }
    }
}
