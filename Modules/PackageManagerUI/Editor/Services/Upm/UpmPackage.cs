// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
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

        public UpmPackage(string name, bool isDiscoverable, PackageType type = PackageType.None)
        {
            m_Progress = PackageProgress.None;
            m_Name = name;
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = new UpmVersionList();
            m_Errors = new List<UIError>();
            m_Type = type;
        }

        public UpmPackage(PackageInfo info, bool isInstalled, bool isDiscoverable)
        {
            m_Progress = PackageProgress.None;
            m_Name = info.name;
            m_Errors = new List<UIError>();
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = new UpmVersionList(info, isInstalled);
            m_Type = versions.primary.HasTag(PackageTag.BuiltIn) ? PackageType.BuiltIn : PackageType.Installable;
        }

        internal void UpdateVersions(IEnumerable<UpmPackageVersion> updatedVersions)
        {
            m_VersionList = new UpmVersionList(updatedVersions);
            ClearErrors();
        }

        // This function is only used to update the object, not to actually perform the add operation
        public void AddInstalledVersion(UpmPackageVersion newVersion)
        {
            m_VersionList.AddInstalledVersion(newVersion);
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
