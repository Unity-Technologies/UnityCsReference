// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class ClearCacheRequest : Request
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private ClearCacheRequest()
            : base()
        {
        }

        internal ClearCacheRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }
    }
}
