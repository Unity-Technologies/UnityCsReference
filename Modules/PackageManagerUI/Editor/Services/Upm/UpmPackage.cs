// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmPackage : BasePackage
    {
        public override string uniqueId => name;

        [SerializeField]
        private bool m_IsDiscoverable;
        public override bool isDiscoverable => m_IsDiscoverable;

        [SerializeField]
        private UpmVersionList m_VersionList;

        public override IVersionList versions => m_VersionList;

        protected override string descriptor
        {
            get
            {
                if (Is(PackageType.Feature))
                    return L10n.Tr("feature");
                if (Is(PackageType.BuiltIn))
                    return L10n.Tr("built-in package");
                return L10n.Tr("package");
            }
        }

        public UpmPackage(string name, bool isDiscoverable, PackageType type)
        {
            m_Progress = PackageProgress.None;
            m_Name = name;
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = new UpmVersionList();
            m_Errors = new List<UIError>();
            m_Type = type;
            RefreshUnityType();
        }

        public UpmPackage(PackageInfo info, bool isInstalled, bool isDiscoverable, bool isUnityPackage)
        {
            m_Progress = PackageProgress.None;
            m_Name = info.name;
            m_Errors = new List<UIError>();
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = new UpmVersionList(info, isInstalled, isUnityPackage);
            m_Type = versions.primary.HasTag(PackageTag.BuiltIn) ? PackageType.BuiltIn : PackageType.Installable;
            _ = info.type == "feature" ? m_Type |= PackageType.Feature : m_Type &= ~PackageType.Feature;

            RefreshUnityType();
        }

        internal void UpdateVersions(IEnumerable<UpmPackageVersion> updatedVersions, int numUnloadedVersions)
        {
            m_VersionList = new UpmVersionList(updatedVersions, m_VersionList.lifecycleVersionString, m_VersionList.lifecycleNextVersion, numUnloadedVersions);
            RefreshUnityType();
            ClearErrors();
        }

        // This function is only used to update the object, not to actually perform the add operation
        public void AddInstalledVersion(UpmPackageVersion newVersion)
        {
            m_VersionList.AddInstalledVersion(newVersion);
            RefreshUnityType();
        }

        private void RefreshUnityType()
        {
            var primaryUpmVersion = versions?.primary as UpmPackageVersion;
            _ = primaryUpmVersion?.isUnityPackage ?? false ? m_Type |= PackageType.Unity : m_Type &= ~PackageType.Unity;
            _ = primaryUpmVersion?.isFromScopedRegistry ?? false ? m_Type |= PackageType.ScopedRegistry : m_Type &= ~PackageType.ScopedRegistry;
            _ = primaryUpmVersion?.isRegistryPackage ?? false ? (!primaryUpmVersion?.isUnityPackage ?? false ?
                m_Type |= PackageType.MainNotUnity : m_Type &= ~PackageType.ScopedRegistry) : m_Type &= ~PackageType.MainNotUnity;
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
