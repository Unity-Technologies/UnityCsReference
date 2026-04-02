// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [FilePath("ProjectSettings/PackageManagerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class PackageManagerProjectSettings : ScriptableSingleton<PackageManagerProjectSettings>
    {
        public event Action<bool> onEnablePreReleasePackagesChanged = delegate {};
        public event Action<bool> onAdvancedSettingsFoldoutChanged = delegate {};
        public event Action<bool> onScopedRegistriesSettingsFoldoutChanged = delegate {};
        public event Action<bool> onSeeAllVersionsChanged = delegate {};
        public event Action<long> onLoadAssetsChanged = delegate {};
        public event Action onInitializationFinished = delegate {};

        [SerializeField]
        private bool m_EnablePreReleasePackages;

        public bool enablePreReleasePackages
        {
            get => m_EnablePreReleasePackages;
            set
            {
                if (value == m_EnablePreReleasePackages)
                    return;
                m_EnablePreReleasePackages = value;
                onEnablePreReleasePackagesChanged?.Invoke(m_EnablePreReleasePackages);
            }
        }

        [SerializeField]
        private bool m_AdvancedSettingsExpanded = true;
        public virtual bool advancedSettingsExpanded
        {
            get => m_AdvancedSettingsExpanded;
            set
            {
                if (value == m_AdvancedSettingsExpanded)
                    return;
                m_AdvancedSettingsExpanded = value;
                onAdvancedSettingsFoldoutChanged?.Invoke(m_AdvancedSettingsExpanded);
            }
        }

        [SerializeField]
        private bool m_ScopedRegistriesSettingsExpanded = true;
        public virtual bool scopedRegistriesSettingsExpanded
        {
            get => m_ScopedRegistriesSettingsExpanded;
            set
            {
                if (value == m_ScopedRegistriesSettingsExpanded)
                    return;
                m_ScopedRegistriesSettingsExpanded = value;
                onScopedRegistriesSettingsFoldoutChanged?.Invoke(m_ScopedRegistriesSettingsExpanded);
            }
        }

        [SerializeField]
        private bool m_SeeAllPackageVersions;
        public virtual bool seeAllPackageVersions
        {
            get => m_SeeAllPackageVersions;
            set
            {
                if (value == m_SeeAllPackageVersions)
                    return;
                m_SeeAllPackageVersions = value;
                onSeeAllVersionsChanged?.Invoke(m_SeeAllPackageVersions);
            }
        }

        [SerializeField]
        private bool m_DismissPreviewPackagesInUse;
        public virtual bool dismissPreviewPackagesInUse
        {
            get => m_DismissPreviewPackagesInUse;
            set => m_DismissPreviewPackagesInUse = value;
        }

        [SerializeField]
        public bool oneTimeWarningShown;

        [SerializeField]
        public bool oneTimePackageErrorsPopUpShown;

        [SerializeField]
        private RegistryInfo m_MainRegistry = new ();
        public RegistryInfo mainRegistry => m_MainRegistry;

        [SerializeField]
        private List<RegistryInfo> m_ScopedRegistries = new ();
        public IReadOnlyList<RegistryInfo> scopedRegistries => m_ScopedRegistries;

        // `m_UserSelectedRegistryName` and `m_UserAddingNewScopedRegistry` only reflect what scoped registry the user selected
        // by interacting with the settings window. registryInfoDraft.original is what's actually selected and displayed to the user
        // When the user opens the settings window for the first time, `m_UserAddingNewScopedRegistry` would be false and
        // `m_UserSelectedRegistryName` is empty, but registryInfoDraft.original would be the first scoped registry since that's the default value.
        [SerializeField]
        private string m_UserSelectedRegistryName;
        [SerializeField]
        private bool m_UserAddingNewScopedRegistry;
        public bool isUserAddingNewScopedRegistry
        {
            get => m_UserAddingNewScopedRegistry;
            set => m_UserAddingNewScopedRegistry = value;
        }

        [SerializeField]
        private RegistryInfoDraft m_RegistryInfoDraft = new();
        public RegistryInfoDraft registryInfoDraft => m_RegistryInfoDraft;

        public void SelectRegistry(string name)
        {
            var registry = string.IsNullOrEmpty(name) ? null : scopedRegistries.FirstMatch(r => r.name == name);
            m_UserSelectedRegistryName = name;
            m_RegistryInfoDraft.SetOriginalRegistryInfo(registry);
        }

        public void SetRegistries(RegistryInfo[] newRegistries)
        {
            if (newRegistries == null || newRegistries.Length == 0)
                return;

            m_MainRegistry = newRegistries[0];

            var oldSelectionIndex = Math.Max(0, m_ScopedRegistries.FindIndex(r => r.name == m_UserSelectedRegistryName));
            m_ScopedRegistries.Clear();
            if (newRegistries.Length > 1)
                m_ScopedRegistries.AddRange(new ArraySegment<RegistryInfo>(newRegistries, 1, newRegistries.Length - 1));
            RefreshSelection(oldSelectionIndex);
        }

        public bool AddRegistry(RegistryInfo registry)
        {
            if (!IsValidRegistryInfo(registry))
                return false;

            m_ScopedRegistries.Add(registry);
            if (m_RegistryInfoDraft.original is null && m_RegistryInfoDraft.name == registry.name)
            {
                m_UserSelectedRegistryName = registry.name;
                isUserAddingNewScopedRegistry = false;
                m_RegistryInfoDraft.SetOriginalRegistryInfo(registry);
            }

            return true;
        }

        public bool UpdateRegistry(string oldName, RegistryInfo newRegistry)
        {
            if (!IsValidRegistryInfo(newRegistry) || string.IsNullOrEmpty(oldName))
                return false;

            for (var i = 0; i < m_ScopedRegistries.Count; i++)
            {
                var registry = m_ScopedRegistries[i];
                if (registry.name != oldName)
                    continue;
                m_ScopedRegistries[i] = newRegistry;

                if (m_UserSelectedRegistryName == oldName)
                    m_UserSelectedRegistryName = newRegistry.name;
                if (m_RegistryInfoDraft.original?.name == oldName)
                    m_RegistryInfoDraft.SetOriginalRegistryInfo(newRegistry);

                return true;
            }

            return false;
        }

        public bool RemoveRegistry(string name)
        {
            var registryIndex = m_ScopedRegistries.FindIndex(r => r.name == name);
            if (registryIndex >= 0)
            {
                m_ScopedRegistries.RemoveAt(registryIndex);
                if (m_RegistryInfoDraft.original?.name == name)
                    RefreshSelection(registryIndex);
                return true;
            }
            return false;
        }

        private void RefreshSelection(int oldSelectionIndex = 0)
        {
            if (!string.IsNullOrEmpty(m_UserSelectedRegistryName))
            {
                var selectedRegistry = scopedRegistries.FirstMatch(r => r.name == m_UserSelectedRegistryName);
                if (selectedRegistry == null)
                {
                    if (oldSelectionIndex >= 0 && oldSelectionIndex < m_ScopedRegistries.Count)
                        selectedRegistry = m_ScopedRegistries[oldSelectionIndex];
                    else if (m_ScopedRegistries.Count > 0)
                        selectedRegistry = m_ScopedRegistries[m_ScopedRegistries.Count -  1];
                }
                m_UserSelectedRegistryName = selectedRegistry?.name ?? string.Empty;
                m_RegistryInfoDraft.SetOriginalRegistryInfo(selectedRegistry);
            }
            else
            {
                var selectedRegistry = m_UserAddingNewScopedRegistry || scopedRegistries.Count == 0 ? null : scopedRegistries[0];
                m_RegistryInfoDraft.SetOriginalRegistryInfo(selectedRegistry);
            }
        }

        private static bool IsValidRegistryInfo(RegistryInfo info)
        {
            if (info?.scopes == null)
                return false;

            return !string.IsNullOrWhiteSpace(info.id) && !string.IsNullOrWhiteSpace(info.name)
                && !string.IsNullOrWhiteSpace(info.url) && info.scopes.Length > 0
                && !Array.Exists(info.scopes, string.IsNullOrWhiteSpace);
        }

        void OnEnable()
        {
            m_RegistryInfoDraft.OnEnable();

            if (m_RegistryInfoDraft.original is null && !m_RegistryInfoDraft.hasUnsavedChanges && !isUserAddingNewScopedRegistry && scopedRegistries.Count > 0)
                m_RegistryInfoDraft.SetOriginalRegistryInfo(scopedRegistries[0]);

            onInitializationFinished.Invoke();
        }

        public void Save()
        {
            Save(true);
        }

        [SerializeField]
        private long m_LoadAssets;

        public long loadAssets
        {
            get => m_LoadAssets == 0  ? (long)PackageLoadBar.AssetsToLoad.Min : m_LoadAssets;
            set
            {
                if (value == m_LoadAssets)
                    return;
                m_LoadAssets = value;
                onLoadAssetsChanged?.Invoke(m_LoadAssets);
            }
        }
    }
}
