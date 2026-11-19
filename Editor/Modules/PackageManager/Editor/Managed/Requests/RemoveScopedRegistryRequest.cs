// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    internal sealed class RemoveScopedRegistryRequest : Request
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private RemoveScopedRegistryRequest()
        {
        }

        internal RemoveScopedRegistryRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }
    }
}
