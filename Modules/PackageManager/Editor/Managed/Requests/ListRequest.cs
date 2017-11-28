// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Linq;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed class ListRequest : Request<PackageCollection>
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private ListRequest()
            : base()
        {
        }

        internal ListRequest(long operationId, NativeClient.StatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override PackageCollection GetResult()
        {
            var operationStatus = NativeClient.GetListOperationData(Id);
            var packageList = operationStatus.packageList.Select(p => (PackageInfo)p);
            return new PackageCollection(packageList);
        }
    }
}

