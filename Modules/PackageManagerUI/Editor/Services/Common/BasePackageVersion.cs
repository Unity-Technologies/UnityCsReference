// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal abstract class BasePackageVersion : IPackageVersion
    {
        public string name => packageInfo?.name ?? string.Empty;

        protected string m_DisplayName;
        public string displayName => m_DisplayName;

        protected string m_Description;
        public string description => !string.IsNullOrEmpty(m_Description) ? m_Description : (packageInfo?.description ?? string.Empty);

        protected string m_PackageUniqueId;
        public string packageUniqueId => m_PackageUniqueId;

        public virtual string authorLink => string.Empty;

        protected SemVersion m_Version;
        public SemVersion version => m_Version;

        protected long m_PublishedDateTicks;
        public DateTime? publishedDate => m_PublishedDateTicks == 0 ? packageInfo?.datePublished : new DateTime(m_PublishedDateTicks, DateTimeKind.Utc);

        public DependencyInfo[] dependencies => packageInfo?.dependencies;
        public DependencyInfo[] resolvedDependencies => packageInfo?.resolvedDependencies;
        public EntitlementsInfo entitlements => packageInfo?.entitlements;

        protected PackageTag m_Tag;
        public bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) != 0;
        }

        public virtual PackageInfo packageInfo => null;
        public virtual IDictionary<string, string> categoryLinks => null;
        public virtual IEnumerable<Error> errors => Enumerable.Empty<Error>();
        public virtual IEnumerable<Sample> samples => Enumerable.Empty<Sample>();
        public virtual IEnumerable<PackageSizeInfo> sizes => Enumerable.Empty<PackageSizeInfo>();
        public virtual IEnumerable<SemVersion> supportedVersions => Enumerable.Empty<SemVersion>();
        public virtual SemVersion supportedVersion => null;

        public abstract string uniqueId { get; }
        public abstract string category { get; }
        public abstract string author { get; }
        public abstract bool isInstalled { get; }
        public abstract bool isFullyFetched { get; }
        public abstract bool isAvailableOnDisk { get; }
        public abstract bool isDirectDependency { get; }
        public abstract string localPath { get; }
        public abstract string versionString { get; }
        public abstract string versionId { get; }
    }
}
