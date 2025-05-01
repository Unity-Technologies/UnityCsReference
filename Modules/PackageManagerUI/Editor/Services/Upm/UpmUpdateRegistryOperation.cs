// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmUpdateRegistryOperation : UpmBaseOperation<UpdateScopedRegistryRequest>
    {
        [SerializeField]
        protected string m_RegistryName = null;
        public string registryName => m_RegistryName;
        [SerializeField]
        protected string m_NewRegistryName = null;
        public string newRegistryName => m_NewRegistryName;
        [SerializeField]
        protected string m_RegistryUrl = null;
        public string registryUrl => m_RegistryUrl;
        [SerializeField]
        protected string[] m_Scopes = null;
        public string[] scopes => m_Scopes;
        [SerializeField]
        protected bool m_DryRun = false;
        public bool dryRun => m_DryRun;

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        public void Update(string oldName, string newName, string url, string[] scopes, bool dryRun = false)
        {
            m_RegistryName = oldName;
            m_NewRegistryName = newName;
            m_RegistryUrl = url;
            m_Scopes = scopes;
            m_DryRun = dryRun;
            Start();
        }

        protected override UpdateScopedRegistryRequest CreateRequest()
        {
            var updatedregistryName = string.IsNullOrEmpty(newRegistryName) ? registryName : newRegistryName;
            return m_ClientProxy.UpdateScopedRegistry(registryName,  new UpdateScopedRegistryOptions(updatedregistryName, registryUrl, scopes), dryRun);
        }
    }
}
