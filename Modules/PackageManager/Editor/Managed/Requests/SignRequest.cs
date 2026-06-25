// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    internal sealed partial class SignRequest : Request
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private SignRequest()
            : base()
        {
        }

        internal SignRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }
    }
}
