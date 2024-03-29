// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using CachePathConfig = UnityEditorInternal.AssetStoreCachePathManager.CachePathConfig;
using ConfigStatus = UnityEditorInternal.AssetStoreCachePathManager.ConfigStatus;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAssetStoreCachePathProxy : IService
    {
        event Action<CachePathConfig> onConfigChanged;

        CachePathConfig GetDefaultConfig();
        CachePathConfig GetConfig();
        ConfigStatus SetConfig(string path);
        ConfigStatus ResetConfig();
    }

    [Serializable]
    internal class AssetStoreCachePathProxy : BaseService<IAssetStoreCachePathProxy>, IAssetStoreCachePathProxy
    {
        public event Action<CachePathConfig> onConfigChanged = delegate {};

        [SerializeField]
        private CachePathConfig m_CurrentConfig;

        public CachePathConfig GetDefaultConfig() => AssetStoreCachePathManager.GetDefaultConfig();

        public CachePathConfig GetConfig()
        {
            var config = AssetStoreCachePathManager.GetConfig();
            if (m_CurrentConfig == null ||
                string.Compare(config.path, m_CurrentConfig.path, StringComparison.Ordinal) != 0 ||
                config.source != m_CurrentConfig.source ||
                config.status != m_CurrentConfig.status)
            {
                m_CurrentConfig = config;
                onConfigChanged?.Invoke(m_CurrentConfig);
            }

            return m_CurrentConfig;
        }

        public ConfigStatus SetConfig(string path)
        {
            var status = AssetStoreCachePathManager.SetConfig(path);
            if (status == ConfigStatus.Success)
                GetConfig();
            return status;
        }

        public ConfigStatus ResetConfig()
        {
            var status = AssetStoreCachePathManager.ResetConfig();
            if (status == ConfigStatus.Success)
                GetConfig();
            return status;
        }
    }
}
