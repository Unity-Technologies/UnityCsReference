// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [FilePath("ProjectSettings/PackageManagerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class PackageManagerProjectSettings : ScriptableSingleton<PackageManagerProjectSettings>
    {
        public event Action<bool> onScopedRegistriesSettingsFoldoutChanged = delegate {};

        [SerializeField]
        private bool m_ScopedRegistriesSettingsExpanded = true;
        public virtual bool scopedRegistriesSettingsExpanded
        {
            get { return m_ScopedRegistriesSettingsExpanded; }
            set
            {
                if (value != m_ScopedRegistriesSettingsExpanded)
                {
                    m_ScopedRegistriesSettingsExpanded = value;
                    onScopedRegistriesSettingsFoldoutChanged?.Invoke(m_ScopedRegistriesSettingsExpanded);
                }
            }
        }

        [SerializeField]
        public bool oneTimeWarningShown;

        [SerializeField]
        private List<RegistryInfo> m_Registries = new List<RegistryInfo>();

        public IList<RegistryInfo> registries => m_Registries;

        public IEnumerable<RegistryInfo> scopedRegistries => m_Registries.Skip(1);

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
            get { return m_UserAddingNewScopedRegistry; }
            set { m_UserAddingNewScopedRegistry = value; }
        }
        [SerializeField]
        private RegistryInfoDraft m_RegistryInfoDraft = new RegistryInfoDraft();
        public RegistryInfoDraft registryInfoDraft => m_RegistryInfoDraft;

        public void SelectRegistry(string name)
        {
            var registry = string.IsNullOrEmpty(name) ? null : scopedRegistries.FirstOrDefault(r => r.name == name);
            m_UserSelectedRegistryName = name;
            m_RegistryInfoDraft.SetOriginalRegistryInfo(registry);
        }

        public void SetRegistries(RegistryInfo[] newRegistries)
        {
            var sanitizedNewRegistries = newRegistries?.Where(r => r != null).ToArray();

            var oldSelectionIndex = Math.Max(0, m_Registries.FindIndex(r => r.name == m_UserSelectedRegistryName));
            m_Registries.Clear();
            if (sanitizedNewRegistries?.Length > 0)
                m_Registries.AddRange(sanitizedNewRegistries);
            RefreshSelection(oldSelectionIndex);
        }

        public bool AddRegistry(RegistryInfo registry)
        {
            if (!IsValidRegistryInfo(registry))
                return false;

            m_Registries.Add(registry);
            if (m_RegistryInfoDraft.original == null && m_RegistryInfoDraft.name == registry.name)
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

            for (var i = 0; i < m_Registries.Count; i++)
            {
                var registry = m_Registries[i];
                if (registry.name != oldName)
                    continue;
                m_Registries[i] = newRegistry;

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
            var registryIndex = m_Registries.FindIndex(r => r.name == name);
            if (registryIndex >= 0)
            {
                m_Registries.RemoveAt(registryIndex);
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
                var selectedRegistry = scopedRegistries.FirstOrDefault(r => r.name == m_UserSelectedRegistryName);
                if (selectedRegistry == null)
                {
                    // We use `m_Registries.Count - 2` because m_Registries always contains the main registry
                    // and scopedRegistries is always one element less than m_Registries
                    var newSelectionIndex = Math.Max(0, Math.Min(oldSelectionIndex, m_Registries.Count - 2));
                    selectedRegistry = scopedRegistries.Skip(newSelectionIndex).FirstOrDefault();
                }
                m_UserSelectedRegistryName = selectedRegistry?.name ?? string.Empty;
                m_RegistryInfoDraft.SetOriginalRegistryInfo(selectedRegistry);
            }
            else
            {
                var selectedRegistry = m_UserAddingNewScopedRegistry ? null : scopedRegistries.FirstOrDefault();
                m_RegistryInfoDraft.SetOriginalRegistryInfo(selectedRegistry);
            }
        }

        private static bool IsValidRegistryInfo(RegistryInfo info)
        {
            if (info?.scopes == null)
                return false;

            return !string.IsNullOrEmpty(info.id?.Trim()) && !string.IsNullOrEmpty(info.name?.Trim())
                && !string.IsNullOrEmpty(info.url?.Trim()) && info.scopes.Any()
                && info.scopes.All(s => !string.IsNullOrEmpty(s.Trim()));
        }

        void OnEnable()
        {
            if (m_RegistryInfoDraft.original == null && !m_RegistryInfoDraft.hasUnsavedChanges && !isUserAddingNewScopedRegistry)
            {
                var firstScopedRegistry = scopedRegistries.FirstOrDefault();
                if (firstScopedRegistry != null)
                    m_RegistryInfoDraft.SetOriginalRegistryInfo(firstScopedRegistry);
            }
        }

        public void Save()
        {
            Save(true);
        }
    }
}
