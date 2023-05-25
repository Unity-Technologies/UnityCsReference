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

        public UpmPackage(string name, bool isDiscoverable, UpmVersionList versionList)
        {
            m_Progress = PackageProgress.None;
            m_Name = name;
            m_Type = PackageType.Upm;
            m_Errors = new List<UIError>();
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = versionList;

            RefreshPackageTypeFromVersions();
            LinkPackageAndVersions();
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
