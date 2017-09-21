// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed class RemoveRequest : Request
    {
        [SerializeField]
        private string m_PackageIdOrName;

        /// <summary>
        /// Id or name of the package to be removed
        /// </summary>
        public string PackageIdOrName
        {
            get
            {
                return m_PackageIdOrName;
            }
        }

        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private RemoveRequest()
        {
        }

        internal RemoveRequest(long operationId, NativeClient.StatusCode initialStatus, string packageName)
            : base(operationId, initialStatus)
        {
            m_PackageIdOrName = packageName;
        }
    }
}

