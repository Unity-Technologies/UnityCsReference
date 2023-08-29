// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IProjectSettingsProxy : IService
    {
        event Action<bool> onEnablePreReleasePackagesChanged;
        event Action<bool> onAdvancedSettingsFoldoutChanged;
        event Action<bool> onScopedRegistriesSettingsFoldoutChanged;
        event Action<bool> onSeeAllVersionsChanged;
        event Action<long> onLoadAssetsChanged;
        event Action onInitializationFinished;

        bool dismissPreviewPackagesInUse { get; set; }
        bool enablePreReleasePackages { get; set; }
        bool advancedSettingsExpanded { get; set; }
        bool scopedRegistriesSettingsExpanded { get; set; }
        bool seeAllPackageVersions { get; set; }
        long loadAssets { get; set; }
        bool oneTimeWarningShown { get; set; }
        bool oneTimeDeprecatedPopUpShown { get; set; }
        bool isUserAddingNewScopedRegistry { get; set; }

        IList<RegistryInfo> registries { get; }
        IEnumerable<RegistryInfo> scopedRegistries { get; }
        RegistryInfoDraft registryInfoDraft { get; }

        void SetRegistries(RegistryInfo[] registries);
        bool AddRegistry(RegistryInfo registry);
        bool UpdateRegistry(string oldName, RegistryInfo newRegistry);
        bool RemoveRegistry(string name);
        void SelectRegistry(string name);

        void Save();
    }

    [ExcludeFromCodeCoverage]
    internal class PackageManagerProjectSettingsProxy : BaseService<IProjectSettingsProxy>, IProjectSettingsProxy
    {
        public event Action<bool> onEnablePreReleasePackagesChanged = delegate {};
        public event Action<bool> onAdvancedSettingsFoldoutChanged = delegate {};
        public event Action<bool> onScopedRegistriesSettingsFoldoutChanged = delegate {};
        public event Action<bool> onSeeAllVersionsChanged = delegate {};
        public event Action<long> onLoadAssetsChanged = delegate {};
        public event Action onInitializationFinished = delegate {};

        public bool dismissPreviewPackagesInUse
        {
            get => PackageManagerProjectSettings.instance.dismissPreviewPackagesInUse;
            set => PackageManagerProjectSettings.instance.dismissPreviewPackagesInUse = value;
        }

        public bool enablePreReleasePackages
        {
            get => PackageManagerProjectSettings.instance.enablePreReleasePackages;
            set => PackageManagerProjectSettings.instance.enablePreReleasePackages = value;
        }

        public bool advancedSettingsExpanded
        {
            get => PackageManagerProjectSettings.instance.advancedSettingsExpanded;
            set => PackageManagerProjectSettings.instance.advancedSettingsExpanded = value;
        }

        public bool scopedRegistriesSettingsExpanded
        {
            get => PackageManagerProjectSettings.instance.scopedRegistriesSettingsExpanded;
            set => PackageManagerProjectSettings.instance.scopedRegistriesSettingsExpanded = value;
        }

        public bool seeAllPackageVersions
        {
            get => PackageManagerProjectSettings.instance.seeAllPackageVersions;
            set => PackageManagerProjectSettings.instance.seeAllPackageVersions = value;
        }

        public long loadAssets
        {
            get => PackageManagerProjectSettings.instance.loadAssets;
            set => PackageManagerProjectSettings.instance.loadAssets = value;
        }

        public bool oneTimeWarningShown
        {
            get => PackageManagerProjectSettings.instance.oneTimeWarningShown;
            set => PackageManagerProjectSettings.instance.oneTimeWarningShown = value;
        }

        public bool oneTimeDeprecatedPopUpShown
        {
            get => PackageManagerProjectSettings.instance.oneTimeDeprecatedPopUpShown;
            set => PackageManagerProjectSettings.instance.oneTimeDeprecatedPopUpShown = value;
        }

        public bool isUserAddingNewScopedRegistry
        {
            get => PackageManagerProjectSettings.instance.isUserAddingNewScopedRegistry;
            set => PackageManagerProjectSettings.instance.isUserAddingNewScopedRegistry = value;
        }

        public IList<RegistryInfo> registries => PackageManagerProjectSettings.instance.registries;

        public void SetRegistries(RegistryInfo[] registries)
        {
            PackageManagerProjectSettings.instance.SetRegistries(registries);
        }

        public bool AddRegistry(RegistryInfo registry)
        {
            return PackageManagerProjectSettings.instance.AddRegistry(registry);
        }

        public bool UpdateRegistry(string oldName, RegistryInfo newRegistry)
        {
            return PackageManagerProjectSettings.instance.UpdateRegistry(oldName, newRegistry);
        }

        public bool RemoveRegistry(string name)
        {
            return PackageManagerProjectSettings.instance.RemoveRegistry(name);
        }

        public void SelectRegistry(string name)
        {
            PackageManagerProjectSettings.instance.SelectRegistry(name);
        }

        public IEnumerable<RegistryInfo> scopedRegistries => PackageManagerProjectSettings.instance.scopedRegistries;

        public RegistryInfoDraft registryInfoDraft => PackageManagerProjectSettings.instance.registryInfoDraft;

        public override void OnEnable()
        {
            PackageManagerProjectSettings.instance.onEnablePreReleasePackagesChanged += OnEnablePreReleasePackagesChanged;
            PackageManagerProjectSettings.instance.onAdvancedSettingsFoldoutChanged += OnAdvancedSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onScopedRegistriesSettingsFoldoutChanged += OnScopedRegistriesSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onSeeAllVersionsChanged += OnSeeAllPackageVersionsChanged;
            PackageManagerProjectSettings.instance.onLoadAssetsChanged += OnLoadAssetsChanged;
            PackageManagerProjectSettings.instance.onInitializationFinished += OnInitializationFinished;
        }

        public override void OnDisable()
        {
            PackageManagerProjectSettings.instance.onEnablePreReleasePackagesChanged -= OnEnablePreReleasePackagesChanged;
            PackageManagerProjectSettings.instance.onAdvancedSettingsFoldoutChanged -= OnAdvancedSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onScopedRegistriesSettingsFoldoutChanged -= OnScopedRegistriesSettingsFoldoutChanged;
            PackageManagerProjectSettings.instance.onSeeAllVersionsChanged -= OnSeeAllPackageVersionsChanged;
            PackageManagerProjectSettings.instance.onLoadAssetsChanged -= OnLoadAssetsChanged;
            PackageManagerProjectSettings.instance.onInitializationFinished -= OnInitializationFinished;
        }

        public void Save()
        {
            PackageManagerProjectSettings.instance.Save();
        }

        private void OnInitializationFinished()
        {
            onInitializationFinished.Invoke();
        }

        private void OnEnablePreReleasePackagesChanged(bool enablePreReleasePackages)
        {
            onEnablePreReleasePackagesChanged?.Invoke(enablePreReleasePackages);
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
