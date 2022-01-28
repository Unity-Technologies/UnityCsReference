// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmPackageVersion : IPackageVersion
    {
        private const string k_UnityPrefix = "com.unity.";
        private const string k_UnityAuthor = "Unity Technologies";

        private PackageInfo m_PackageInfo;
        public PackageInfo packageInfo { get { return m_PackageInfo; } }

        public string name { get { return m_PackageInfo.name; } }
        public string type { get { return m_PackageInfo.type; } }
        public string category { get { return m_PackageInfo.category; } }

        public IDictionary<string, string> categoryLinks => null;

        public IEnumerable<Error> errors => m_PackageInfo.errors.Concat(entitlementsError != null ? new List<Error> { entitlementsError } : new List<Error>());
        public bool isDirectDependency { get { return isFullyFetched && m_PackageInfo.isDirectDependency; } }

        public DependencyInfo[] dependencies { get { return m_PackageInfo.dependencies; } }
        public DependencyInfo[] resolvedDependencies { get { return m_PackageInfo.resolvedDependencies; } }
        public EntitlementsInfo entitlements => m_PackageInfo.entitlements;
        Error entitlementsError => !entitlements.isAllowed && isInstalled ? new Error(NativeErrorCode.Unknown, L10n.Tr("You do not have entitlements for this package.")) : null;

        [SerializeField]
        private bool m_IsUnityPackage;
        public bool isUnityPackage
        {
            get
            {
                if (HasTag(PackageTag.Bundled))
                    return true;
                if (HasTag(PackageTag.Git | PackageTag.Local | PackageTag.Custom))
                    return false;
                return m_IsUnityPackage;
            }
        }

        [SerializeField]
        private string m_PackageId;
        public string uniqueId { get { return m_PackageId; } }

        [SerializeField]
        private string m_PackageUniqueId;
        public string packageUniqueId => m_PackageUniqueId;

        [SerializeField]
        private string m_Author;
        public string author => m_Author;

        public string authorLink => string.Empty;

        private string m_DisplayName;
        public string displayName { get { return m_DisplayName; } }

        [SerializeField]
        private string m_VersionString;

        private SemVersion m_Version;
        public SemVersion version { get { return m_Version; } }

        private bool m_IsFullyFetched;
        public bool isFullyFetched { get { return m_IsFullyFetched; } }

        private bool m_SamplesParsed;
        private List<Sample> m_Samples;
        public IEnumerable<Sample> samples
        {
            get
            {
                if (m_SamplesParsed)
                    return m_Samples;

                if (!isFullyFetched)
                    return new List<Sample>();

                m_Samples = Sample.FindByPackage(m_PackageInfo)?.ToList() ?? new List<Sample>();
                m_SamplesParsed = true;
                return m_Samples;
            }
        }

        private bool m_IsInstalled;
        public bool isInstalled
        {
            get { return m_IsInstalled; }
            set
            {
                m_IsInstalled = value;
                RefreshTags();
            }
        }

        private string m_Description;
        public string description { get { return !string.IsNullOrEmpty(m_Description) ? m_Description :  m_PackageInfo.description; } }

        private PackageTag m_Tag;

        public bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) != 0;
        }

        public bool isVersionLocked => HasTag(PackageTag.VersionLocked);

        public bool canBeRemoved => HasTag(PackageTag.Removable);

        public bool canBeEmbedded => HasTag(PackageTag.Embeddable);

        public bool installedFromPath => HasTag(PackageTag.Local | PackageTag.Custom | PackageTag.Git);

        public bool isAvailableOnDisk
        {
            get { return m_IsFullyFetched && !string.IsNullOrEmpty(m_PackageInfo.resolvedPath); }
        }

        public string shortVersionId { get { return FormatPackageId(name, version.ShortVersion()); } }

        private long m_PublishedDateTicks;
        public DateTime? publishedDate => m_PublishedDateTicks == 0 ? m_PackageInfo.datePublished : new DateTime(m_PublishedDateTicks, DateTimeKind.Utc);

        public string publisherId => author;

        public string localPath
        {
            get
            {
                var packageInfoResolvedPath = packageInfo?.resolvedPath;
                return packageInfoResolvedPath;
            }
        }

        public string versionString => m_Version?.ToString();

        public string versionId => m_Version?.ToString();

        public SemVersion supportedVersion => null;

        public IEnumerable<SemVersion> supportedVersions => Enumerable.Empty<SemVersion>();

        public IEnumerable<PackageSizeInfo> sizes => Enumerable.Empty<PackageSizeInfo>();

        public bool isFromScopedRegistry => m_PackageInfo?.registry?.isDefault == false;

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, SemVersion version, string displayName, bool isUnityPackage)
        {
            m_Version = version;
            m_DisplayName = displayName;
            m_IsInstalled = isInstalled;
            m_PackageUniqueId = packageInfo.name;

            UpdatePackageInfo(packageInfo, isUnityPackage);
        }

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, bool isUnityPackage)
        {
            SemVersion.TryParse(packageInfo.version, out m_Version);
            m_VersionString = m_Version?.ToString();
            m_DisplayName = packageInfo.displayName;
            m_IsInstalled = isInstalled;
            m_PackageUniqueId = packageInfo.name;

            UpdatePackageInfo(packageInfo, isUnityPackage);
        }

        internal void UpdatePackageInfo(PackageInfo newPackageInfo, bool isUnityPackage)
        {
            m_IsFullyFetched = m_Version == newPackageInfo.version;
            m_PackageInfo = newPackageInfo;
            m_PackageUniqueId = m_PackageInfo.name;
            m_IsUnityPackage = isUnityPackage;

            RefreshTags();

            // For core packages, or packages that are bundled with Unity without being published, use Unity's build date
            m_PublishedDateTicks = 0;
            if (HasTag(PackageTag.Bundled) && m_PackageInfo.datePublished == null)
                m_PublishedDateTicks = new DateTime(1970, 1, 1).Ticks + InternalEditorUtility.GetUnityVersionDate() * TimeSpan.TicksPerSecond;

            m_Author = this.isUnityPackage ? k_UnityAuthor : m_PackageInfo.author?.name ?? string.Empty;

            if (HasTag(PackageTag.BuiltIn))
                m_Description = UpmPackageDocs.FetchBuiltinDescription(this);

            // reset sample parse status on package info update, such that the sample list gets regenerated
            m_SamplesParsed = false;

            if (m_IsFullyFetched)
            {
                m_DisplayName = GetDisplayName(m_PackageInfo);
                m_PackageId = m_PackageInfo.packageId;
                if (installedFromPath)
                    m_PackageId = m_PackageId.Replace("\\", "/");
            }
            else
            {
                m_PackageId = FormatPackageId(name, version.ToString());
            }
        }

        internal void UpdateFetchedInfo(AssetStore.FetchedInfo fetchedInfo)
        {
            m_PackageUniqueId = fetchedInfo.id;

            // override version info with product info
            m_DisplayName = fetchedInfo.displayName;
            m_Description = fetchedInfo.description;
        }

        private void RefreshTags()
        {
            // in the case of git/local packages, we always assume that the non-installed versions are from the registry
            var source = m_PackageInfo.source == PackageSource.BuiltIn || m_IsInstalled ? m_PackageInfo.source : PackageSource.Registry;
            switch (source)
            {
                case PackageSource.BuiltIn:
                    m_Tag = PackageTag.Bundled | PackageTag.VersionLocked;
                    if (m_PackageInfo.type == "module")
                        m_Tag |= PackageTag.BuiltIn;
                    break;

                case PackageSource.Embedded:
                    m_Tag = PackageTag.Custom | PackageTag.VersionLocked;
                    break;

                case PackageSource.Local:
                case PackageSource.LocalTarball:
                    m_Tag = PackageTag.Local;
                    break;

                case PackageSource.Git:
                    m_Tag = PackageTag.Git | PackageTag.VersionLocked;
                    break;

                case PackageSource.Unknown:
                case PackageSource.Registry:
                default:
                    m_Tag = PackageTag.None;
                    break;
            }

            m_Tag |= PackageTag.Installable | PackageTag.Removable;
            if (isInstalled && isDirectDependency && !installedFromPath && !HasTag(PackageTag.BuiltIn))
                m_Tag |= PackageTag.Embeddable;

            if (m_Version.IsRelease())
            {
                m_Tag |= PackageTag.Release;
                if (m_Version == m_PackageInfo.versions.verified && !installedFromPath)
                    m_Tag |= PackageTag.Verified;
            }
            else
            {
                if ((version.Major == 0 && string.IsNullOrEmpty(version.Prerelease)) ||
                    PackageTag.Preview.ToString().Equals(version.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
                    m_Tag |= PackageTag.Preview;
            }
        }

        private static string GetDisplayName(PackageInfo info)
        {
            if (!string.IsNullOrEmpty(info.displayName))
                return info.displayName;
            return ExtractDisplayName(info.name);
        }

        public static string ExtractDisplayName(string packageName)
        {
            if (packageName.StartsWith(k_UnityPrefix))
            {
                var displayName = packageName.Substring(k_UnityPrefix.Length).Replace("modules.", "");
                displayName = string.Join(" ", displayName.Split('.'));
                return new CultureInfo("en-US").TextInfo.ToTitleCase(displayName);
            }
            return packageName;
        }

        public static string FormatPackageId(string name, string version)
        {
            return $"{name.ToLower()}@{version}";
        }
    }
}
