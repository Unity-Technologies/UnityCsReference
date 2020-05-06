// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class PackRequest : Request<PackOperationResult>
    {
        private PackRequest()
        {
        }

        internal PackRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override PackOperationResult GetResult()
        {
            return GetOperationData(Id);
        }
    }
}
