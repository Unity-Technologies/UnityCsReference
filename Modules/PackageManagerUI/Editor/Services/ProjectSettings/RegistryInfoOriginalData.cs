// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class RegistryInfoOriginalData : ScriptableObject
    {
        [SerializeField]
        private RegistryInfo m_RegistryInfo;

        public string id => m_RegistryInfo?.id;

        public SearchCapabilities capabilities => m_RegistryInfo?.capabilities ?? SearchCapabilities.None;

        public bool isDefault => m_RegistryInfo?.isDefault ?? false;

        public new string name => m_RegistryInfo?.name;

        public string url => m_RegistryInfo?.url;

        public string[] scopes => m_RegistryInfo?.scopes;

        public ConfigSource configSource => m_RegistryInfo?.configSource ?? ConfigSource.Unknown;

        public void SetRegistryInfo(RegistryInfo registryInfo)
        {
            // make a copy to prevent alteration of underlying scoped registries
            //  list when undoing selection
            m_RegistryInfo = CopyOriginal(registryInfo);
        }

        private RegistryInfo CopyOriginal(RegistryInfo original)
        {
            return original != null ? new RegistryInfo(original.id, original.name, original.url, original.scopes?.Clone() as string[], original.isDefault, original.capabilities, original.configSource)
                : null;
        }

        public bool IsEqualTo(RegistryInfo registryInfo)
        {
            var comparer = new UpmRegistryClient.RegistryInfoComparer();

            return comparer.Equals(m_RegistryInfo, registryInfo);
        }
    }
}
