// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed class ResetToEditorDefaultsRequest : Request
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private ResetToEditorDefaultsRequest()
        {
        }

        internal ResetToEditorDefaultsRequest(long operationId, NativeClient.StatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }
    }
}

