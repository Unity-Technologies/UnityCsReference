// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    internal sealed partial class GetRegistriesRequest : Request<RegistryInfo[]>
    {
        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private GetRegistriesRequest()
            : base()
        {
        }

        internal GetRegistriesRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }

        protected override RegistryInfo[] GetResult()
        {
            return GetOperationData(Id);
        }
    }
}
