// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmPackage : IPackage
    {
        private string m_Name;
        public string name { get { return m_Name; } }
        public string uniqueId { get { return m_Name; } }

        public string displayName { get { return versions.First().displayName; } }

        private bool m_IsDiscoverable;
        public bool isDiscoverable { get { return m_IsDiscoverable; } }

        private UpmVersionList m_VersionList;
        public IVersionList versionList => m_VersionList;

        public IEnumerable<IPackageVersion> keyVersions => versionList?.key;

        public IPackageVersion installedVersion => versionList?.installed;

        public IPackageVersion latestPatch => versionList?.latestPatch;

        public IPackageVersion latestVersion => versionList?.latest;

        public IPackageVersion recommendedVersion => versionList?.recommended;

        public IPackageVersion primaryVersion => versionList?.primary;

        // errors on the package level (not just about a particular version)
        List<Error> m_UpmErrors;

        // Combined errors for this package or any version.
        // Stop lookup after first error encountered on a version to save time not looking up redundant errors.
        public IEnumerable<Error> errors => (versions.Select(v => v.errors).FirstOrDefault(e => e?.Any() ?? false) ?? new List<Error>()).Concat(m_UpmErrors);

        public IEnumerable<PackageImage> images => Enumerable.Empty<PackageImage>();

        public IEnumerable<PackageLink> links => Enumerable.Empty<PackageLink>();

        public IEnumerable<IPackageVersion> versions => versionList?.all;

        private PackageProgress m_Progress;
        public PackageProgress progress
        {
            get { return m_Progress; }
            set { m_Progress = value; }
        }

        public PackageState state => PackageExtensions.GetState(this);

        private PackageType m_Type;
        public bool Is(PackageType type)
        {
            return (m_Type & type) != 0;
        }

        public UpmPackage(string name, bool isDiscoverable, PackageType type = PackageType.None)
        {
            m_Progress = PackageProgress.None;
            m_Name = name;
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = new UpmVersionList();
            m_UpmErrors = new List<Error>();
            m_Type = type;
        }

        public UpmPackage(PackageInfo info, bool isInstalled, bool isDiscoverable)
        {
            m_Progress = PackageProgress.None;
            m_Name = info.name;
            m_UpmErrors = new List<Error>();
            m_IsDiscoverable = isDiscoverable;
            m_VersionList = new UpmVersionList(info, isInstalled);
            m_Type = primaryVersion.HasTag(PackageTag.BuiltIn) ? PackageType.BuiltIn : PackageType.Installable;
        }

        internal void UpdateVersions(IEnumerable<UpmPackageVersion> updatedVersions)
        {
            m_VersionList = new UpmVersionList(updatedVersions);
            m_UpmErrors.Clear();
        }

        // This function is only used to update the object, not to actually perform the add operation
        public void AddInstalledVersion(UpmPackageVersion newVersion)
        {
            m_VersionList.AddInstalledVersion(newVersion);
        }

        public void AddError(Error error)
        {
            m_UpmErrors.Add(error);
        }

        public void ClearErrors()
        {
            m_UpmErrors.Clear();
        }

        public IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
