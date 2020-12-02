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
    internal class PlaceholderPackageVersion : IPackageVersion
    {
        [SerializeField]
        private string m_PackageUniqueId;
        public string packageUniqueId => m_PackageUniqueId;

        [SerializeField]
        private string m_UniqueId;
        public string uniqueId => m_UniqueId;
        public string name => string.Empty;

        [SerializeField]
        private string m_DisplayName;
        public string displayName => m_DisplayName;

        public string author => string.Empty;

        public string authorLink => string.Empty;

        public string description => string.Empty;

        public string category => string.Empty;

        public IDictionary<string, string> categoryLinks => null;

        public IEnumerable<Error> errors => Enumerable.Empty<Error>();

        public IEnumerable<Sample> samples => Enumerable.Empty<Sample>();

        public SemVersion version => new SemVersion(0);

        public EntitlementsInfo entitlements => null;

        public DateTime? publishedDate => null;

        public string publisherId => null;

        public DependencyInfo[] dependencies => null;

        public DependencyInfo[] resolvedDependencies => null;

        public PackageInfo packageInfo => null;

        public bool isInstalled => false;

        public bool isFullyFetched => true;

        public bool isAvailableOnDisk => false;

        public bool isVersionLocked => true;

        public bool canBeRemoved => false;

        public bool canBeEmbedded => false;

        public bool isDirectDependency => true;

        public string localPath => string.Empty;

        public string versionString => string.Empty;

        public string versionId => string.Empty;

        public IEnumerable<SemVersion> supportedVersions => Enumerable.Empty<SemVersion>();

        public IEnumerable<PackageSizeInfo> sizes => Enumerable.Empty<PackageSizeInfo>();

        public SemVersion supportedVersion => null;

        [SerializeField]
        private PackageTag m_Tag;
        public bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) != 0;
        }

        public PlaceholderPackageVersion(string packageUniqueId, string uniqueId, string displayName, PackageTag tag = PackageTag.None, PackageSource source = PackageSource.Unknown)
        {
            m_PackageUniqueId = packageUniqueId;
            m_UniqueId = uniqueId;
            m_DisplayName = displayName;
            m_Tag = tag;
        }
    }
}
