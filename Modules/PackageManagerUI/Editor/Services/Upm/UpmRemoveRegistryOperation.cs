// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmRemoveRegistryOperation : UpmBaseOperation<RemoveScopedRegistryRequest>
    {
        [SerializeField]
        protected string m_RegistryName = null;
        public string registryName => m_RegistryName;

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        public void Remove(string name)
        {
            m_RegistryName = name;
            Start();
        }

        protected override RemoveScopedRegistryRequest CreateRequest()
        {
            return Client.RemoveScopedRegistry(registryName);
        }
    }
}
