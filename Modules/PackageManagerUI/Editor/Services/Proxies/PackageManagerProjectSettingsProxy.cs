// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerProjectSettingsProxy
    {
        public event Action<bool> onEnablePreviewPackagesChanged = delegate {};
        public event Action<bool> onEnablePackageDependenciesChanged = delegate {};
        public event Action<bool> onAdvancedSettingsExpanded = delegate {};

        public virtual bool enablePreviewPackages
        {
            get => PackageManagerProjectSettings.instance.enablePreviewPackages;
            set => PackageManagerProjectSettings.instance.enablePreviewPackages = value;
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

        public virtual bool oneTimeWarningShown
        {
            get => PackageManagerProjectSettings.instance.oneTimeWarningShown;
            set => PackageManagerProjectSettings.instance.oneTimeWarningShown = value;
        }

        public void OnEnable()
        {
            PackageManagerProjectSettings.instance.onEnablePreviewPackagesChanged += OnEnablePreviewPackagesChanged;
            PackageManagerProjectSettings.instance.onEnablePackageDependenciesChanged += OnEnablePackageDependenciesChanged;
            PackageManagerProjectSettings.instance.onAdvancedSettingsExpanded += OnAdvancedSettingsExpanded;
        }

        public void OnDisable()
        {
            PackageManagerProjectSettings.instance.onEnablePreviewPackagesChanged -= OnEnablePreviewPackagesChanged;
            PackageManagerProjectSettings.instance.onEnablePackageDependenciesChanged -= OnEnablePackageDependenciesChanged;
            PackageManagerProjectSettings.instance.onAdvancedSettingsExpanded -= OnAdvancedSettingsExpanded;
        }

        public virtual void Save()
        {
            PackageManagerProjectSettings.instance.Save();
        }

        private void OnEnablePreviewPackagesChanged(bool enablePreviewPackages)
        {
            onEnablePreviewPackagesChanged?.Invoke(enablePreviewPackages);
        }

        private void OnEnablePackageDependenciesChanged(bool enablePackageDependencies)
        {
            onEnablePackageDependenciesChanged?.Invoke(enablePackageDependencies);
        }

        private void OnAdvancedSettingsExpanded(bool advancedSettingsExpanded)
        {
            onAdvancedSettingsExpanded?.Invoke(advancedSettingsExpanded);
        }
    }
}
