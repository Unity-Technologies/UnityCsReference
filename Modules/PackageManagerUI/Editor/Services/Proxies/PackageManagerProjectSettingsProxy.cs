// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerProjectSettingsProxy
    {
        public event Action<bool> onEnablePreReleasePackagesChanged = delegate {};
        public event Action<bool> onEnablePackageDependenciesChanged = delegate {};
        public event Action<bool> onAdvancedSettingsFoldoutChanged = delegate {};
        public event Action<bool> onScopedRegistriesSettingsFoldoutChanged = delegate {};
        public event Action<bool> onSeeAllVersionsChanged = delegate {};
        public event Action<long> onLoadAssetsChanged = delegate {};

        public virtual bool enablePreReleasePackages
        {
            get => PackageManagerProjectSettings.instance.enablePreReleasePackages;
            set => PackageManagerProjectSettings.instance.enablePreReleasePackages = value;
        }

        public virtual bool enablePackageDependencies
        {
            get => PackageManagerProjectSettings.instance.enablePackageDependencies;
            set => PackageManagerProjectSettings.instance.enablePackageDependencies = value;
        }

        public virtual bool advancedSettingsExpanded
        {
            get => PackageManagerProjectSettings.instance.advancedSettingsExpanded;
            set => PackageManagerProjectSettings.instance.advancedSettingsExpanded = value;
        }

        public virtual bool scopedRegistriesSettingsExpanded
        {
            get => PackageManagerProjectSettings.instance.scopedRegistriesSettingsExpanded;
            set => PackageManagerProjectSettings.instance.scopedRegistriesSettingsExpanded = value;
        }

        public virtual bool seeAllPackageVersions
        {
            get => PackageManagerProjectSettings.instance.seeAllPackageVersions;
            set => PackageManagerProjectSettings.instance.seeAllPackageVersions = value;
        }

        public virtual long loadAssets
        {
            get => PackageManagerProjectSettings.instance.loadAssets;
            set => PackageManagerProjectSettings.instance.loadAssets = value;
        }

        public virtual bool oneTimeWarningShown
        {
            get => PackageManagerProjectSettings.instance.oneTimeWarningShown;
            set => PackageManagerProjectSettings.instance.oneTimeWarningShown = value;
        }

        public virtual bool isUserAddingNewScopedRegistry
        {
            get => PackageManagerProjectSettings.instance.isUserAddingNewScopedRegistry;
            set => PackageManagerProjectSettings.instance.isUserAddingNewScopedRegistry = value;
        }

        public virtual IList<RegistryInfo> registries => PackageManagerProjectSettings.instance.registries;

        public virtual void SetRegistries(RegistryInfo[] registries)
        {
            PackageManagerProjectSettings.instance.SetRegistries(registries);
        }

        public virtual bool AddRegistry(RegistryInfo registry)
        {
            return PackageManagerProjectSettings.instance.AddRegistry(registry);
        }

        public virtual bool UpdateRegistry(string oldName, RegistryInfo newRegistry)
        {
            return PackageManagerProjectSettings.instance.UpdateRegistry(oldName, newRegistry);
        }

        public virtual bool RemoveRegistry(string name)
        {
            return PackageManagerProjectSettings.instance.RemoveRegistry(name);
        }

        public virtual void SelectRegistry(string name)
        {
            PackageManagerProjectSettings.instance.SelectRegistry(name);
        }

        public virtual IEnumerable<RegistryInfo> scopedRegistries => PackageManagerProjectSettings.instance.scopedRegistries;

        public virtual RegistryInfoDraft registryInfoDraft => PackageManagerProjectSettings.instance.registryInfoDraft;

        public void OnEnable()
        {
            PackageManagerProjectSettings.instance.onEnablePreReleasePackagesChanged += OnEnablePreReleasePackagesChanged;
            PackageManagerProjectSettings.instance.onEnablePackageDependenciesChanged += OnEnablePackageDependenciesChanged;
            PackageManagerProjectSettings.instance.onAdvancedSettingsFoldoutChanged += OnAdvancedSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onScopedRegistriesSettingsFoldoutChanged += OnScopedRegistriesSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onSeeAllVersionsChanged += OnSeeAllPackageVersionsChanged;
            PackageManagerProjectSettings.instance.onLoadAssetsChanged += OnLoadAssetsChanged;
        }

        public void OnDisable()
        {
            PackageManagerProjectSettings.instance.onEnablePreReleasePackagesChanged -= OnEnablePreReleasePackagesChanged;
            PackageManagerProjectSettings.instance.onEnablePackageDependenciesChanged -= OnEnablePackageDependenciesChanged;
            PackageManagerProjectSettings.instance.onAdvancedSettingsFoldoutChanged -= OnAdvancedSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onScopedRegistriesSettingsFoldoutChanged -= OnScopedRegistriesSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onSeeAllVersionsChanged -= OnSeeAllPackageVersionsChanged;
            PackageManagerProjectSettings.instance.onLoadAssetsChanged -= OnLoadAssetsChanged;
        }

        public virtual void Save()
        {
            PackageManagerProjectSettings.instance.Save();
        }

        private void OnEnablePreReleasePackagesChanged(bool enablePreReleasePackages)
        {
            onEnablePreReleasePackagesChanged?.Invoke(enablePreReleasePackages);
        }

        private void OnEnablePackageDependenciesChanged(bool enablePackageDependencies)
        {
            onEnablePackageDependenciesChanged?.Invoke(enablePackageDependencies);
        }

        private void OnAdvancedSettingsFoldoutChanged(bool advancedSettingsExpanded)
        {
            onAdvancedSettingsFoldoutChanged?.Invoke(advancedSettingsExpanded);
        }

        private void OnScopedRegistriesSettingsFoldoutChanged(bool scopedRegistriesSettingsExpanded)
        {
            onScopedRegistriesSettingsFoldoutChanged?.Invoke(scopedRegistriesSettingsExpanded);
        }

        private void OnSeeAllPackageVersionsChanged(bool seeAllPackageVersions)
        {
            onSeeAllVersionsChanged?.Invoke(seeAllPackageVersions);
        }

        private void OnLoadAssetsChanged(long loadAssets)
        {
            onLoadAssetsChanged?.Invoke(loadAssets);
        }
    }
}
