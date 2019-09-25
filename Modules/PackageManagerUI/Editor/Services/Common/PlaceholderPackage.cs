// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PlaceholderPackage : IPackage
    {
        [SerializeField]
        private string m_UniqueId;

        public string name => string.Empty;
        public string uniqueId => m_UniqueId;

        public string displayName => versions.FirstOrDefault().displayName;

        [SerializeField]
        private PlaceholderVersionList m_VersionList;

        public IVersionList versionList => m_VersionList;

        [SerializeField]
        private PackageProgress m_Progress;
        public PackageProgress progress
        {
            get { return m_Progress; }
            set { m_Progress = value; }
        }

        public PackageState state => PackageExtensions.GetState(this);

        [SerializeField]
        private PackageType m_Type;

        public bool isDiscoverable => true;

        public IEnumerable<Error> errors => Enumerable.Empty<Error>();

        public IEnumerable<PackageImage> images => Enumerable.Empty<PackageImage>();

        public IEnumerable<PackageLink> links => Enumerable.Empty<PackageLink>();

        public IEnumerable<IPackageVersion> versions => versionList?.all;

        public IEnumerable<IPackageVersion> keyVersions => versionList?.key;

        public IPackageVersion installedVersion => versionList?.installed;

        public IPackageVersion latestVersion => versionList?.latest;

        public IPackageVersion latestPatch => versionList?.latestPatch;

        public IPackageVersion recommendedVersion => versionList?.recommended;

        public IPackageVersion primaryVersion => versionList?.primary;

        public void AddError(Error error)
        {
        }

        public void ClearErrors()
        {
        }

        public bool Is(PackageType type)
        {
            return (m_Type & type) != 0;
        }

        public PlaceholderPackage(string uniqueId, PackageType type = PackageType.None, PackageTag tag = PackageTag.None, PackageProgress progress = PackageProgress.None)
        {
            m_Type = type;
            m_UniqueId = uniqueId;
            m_Progress = progress;
            m_VersionList = new PlaceholderVersionList(new PlaceholderPackageVersion(uniqueId, uniqueId, tag));
        }

        public IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
