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
        public bool oneTimeWarningShown;

        public void Save()
        {
            Save(true);
        }
    }
}
