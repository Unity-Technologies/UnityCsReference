// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class EmbedRequest : Request<PackageInfo>
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private EmbedRequest()
            : base()
        {
        }

        internal EmbedRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override PackageInfo GetResult()
        {
            return GetOperationData(Id);
        }
    }
}
