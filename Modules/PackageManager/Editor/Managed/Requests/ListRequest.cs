// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class ListRequest : Request<PackageCollection>
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private ListRequest()
            : base()
        {
        }

        internal ListRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override PackageCollection GetResult()
        {
            var operationStatus = GetOperationData(Id);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var packageList = operationStatus.packageList.Where(p => p.type != ShimPackageType);
#pragma warning restore RS0030
            return new PackageCollection(packageList, operationStatus.error);
        }
    }
}
