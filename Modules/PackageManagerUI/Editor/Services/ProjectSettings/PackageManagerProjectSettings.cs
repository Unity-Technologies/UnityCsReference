// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [FilePath("ProjectSettings/PackageManagerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class PackageManagerProjectSettings : ScriptableSingleton<PackageManagerProjectSettings>
    {
        public event Action<bool> onEnablePreviewPackagesChanged = delegate {};
        public event Action<bool> onEnablePackageDependenciesChanged = delegate {};
        public event Action<bool> onAdvancedSettingsExpanded = delegate {};

        [SerializeField]
        private bool m_EnablePreviewPackages;

        public bool enablePreviewPackages
        {
            get { return m_EnablePreviewPackages; }
            set
            {
                if (value != m_EnablePreviewPackages)
                {
                    m_EnablePreviewPackages = value;
                    onEnablePreviewPackagesChanged?.Invoke(m_EnablePreviewPackages);
                }
            }
        }

        [SerializeField]
        private bool m_EnablePackageDependencies;

        public bool enablePackageDependencies
        {
            get { return m_EnablePackageDependencies; }
            set
            {
                if (value != m_EnablePackageDependencies)
                {
                    m_EnablePackageDependencies = value;
                    onEnablePackageDependenciesChanged?.Invoke(m_EnablePackageDependencies);
                }
            }
        }

        [SerializeField]
        private bool m_AdvancedSettingsExpanded = true;
        public virtual bool advancedSettingsExpanded
        {
            get { return m_AdvancedSettingsExpanded; }
            set
            {
                if (value != m_AdvancedSettingsExpanded)
                {
                    m_AdvancedSettingsExpanded = value;
                    onEnablePackageDependenciesChanged?.Invoke(m_AdvancedSettingsExpanded);
                }
            }
        }

        [SerializeField]
        public bool oneTimeWarningShown;

        public void Save()
        {
            Save(true);
        }
    }
}
