// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmPackageVersion : IPackageVersion
    {
        static readonly string k_UnityPrefix = "com.unity.";

        private PackageInfo m_PackageInfo;
        public PackageInfo packageInfo { get { return m_PackageInfo; } }

        public string name { get { return m_PackageInfo.name; } }
        public string type { get { return m_PackageInfo.type; } }
        public string category { get { return m_PackageInfo.category; } }
        public IEnumerable<Error> errors { get { return m_PackageInfo.errors; } }
        public bool isDirectDependency { get { return isFullyFetched && m_PackageInfo.isDirectDependency; } }

        public DependencyInfo[] dependencies { get { return m_PackageInfo.dependencies; } }
        public DependencyInfo[] resolvedDependencies { get { return m_PackageInfo.resolvedDependencies; } }

        private string m_PackageId;
        public string uniqueId { get { return m_PackageId; } }

        public string packageUniqueId { get { return name; } }

        private PackageSource m_Source;
        public PackageSource source { get { return m_Source; } }

        private string m_Author;
        public string author { get { return m_Author; } }

        private string m_DisplayName;
        public string displayName { get { return m_DisplayName; } }

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

                m_Samples = GetSamplesFromPackageInfo(m_PackageInfo) ?? new List<Sample>();
                m_SamplesParsed = true;
                return m_Samples;
            }
        }

        private static List<Sample> GetSamplesFromPackageInfo(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.resolvedPath))
                return null;

            var jsonPath = Path.Combine(packageInfo.resolvedPath, "package.json");
            if (!File.Exists(jsonPath))
                return null;

            try
            {
                var packageJson = Json.Deserialize(File.ReadAllText(jsonPath)) as Dictionary<string, object>;
                var samples = packageJson["samples"] as List<object>;
                return samples?.Select(s =>
                {
                    var sample = s as Dictionary<string, object>;

                    object temp;
                    var displayName = sample.TryGetValue("displayName", out temp) ? temp as string : string.Empty;
                    var path = sample.TryGetValue("path", out temp) ? temp as string : string.Empty;
                    var description = sample.TryGetValue("description", out temp) ? temp as string : string.Empty;
                    var interactiveImport = sample.TryGetValue("interactiveImport", out temp) ? (bool)temp : false;

                    var resolvedSamplePath = Path.Combine(packageInfo.resolvedPath, path);
                    var importPath = IOUtils.CombinePaths(
                        Application.dataPath,
                        "Samples",
                        IOUtils.SanitizeFileName(packageInfo.displayName),
                        packageInfo.version,
                        IOUtils.SanitizeFileName(displayName)
                    );
                    return new Sample(displayName, description, resolvedSamplePath, importPath, interactiveImport);
                }).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool m_IsInstalled;
        public bool isInstalled
        {
            get { return m_IsInstalled; }
            set
            {
                m_IsInstalled = value;
                m_Source = m_PackageInfo.source == PackageSource.BuiltIn || m_IsInstalled ? m_PackageInfo.source : PackageSource.Registry;
            }
        }

        public bool isUserVisible { get { return isInstalled || HasTag(PackageTag.Release | PackageTag.Preview | PackageTag.Verified | PackageTag.Core); } }

        private string m_Description;
        public string description { get { return !string.IsNullOrEmpty(m_Description) ? m_Description :  m_PackageInfo.description; } }

        private PackageTag m_Tag;

        public bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) != 0;
        }

        public bool isVersionLocked
        {
            get { return source == PackageSource.Embedded || source == PackageSource.Git || source == PackageSource.BuiltIn; }
        }

        public bool canBeRemoved
        {
            get { return source != PackageSource.Unknown; }
        }

        public bool canBeEmbedded
        {
            get
            {
                return isInstalled && isDirectDependency && (source == PackageSource.Registry || HasTag(PackageTag.Core));
            }
        }

        private bool hasPathInId
        {
            get { return source == PackageSource.Local || source == PackageSource.Embedded || source == PackageSource.LocalTarball; }
        }

        public bool isAvailableOnDisk
        {
            get { return m_IsFullyFetched && !string.IsNullOrEmpty(m_PackageInfo.resolvedPath); }
        }

        public string shortVersionId { get { return FormatPackageId(name, version.ShortVersion()); } }

        public DateTime? publishedDate { get { return m_PackageInfo.datePublished; } }

        public string publisherId => m_Author;

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

        public IEnumerable<PackageImage> images => Enumerable.Empty<PackageImage>();

        public IEnumerable<PackageSizeInfo> sizes => Enumerable.Empty<PackageSizeInfo>();

        public IEnumerable<PackageLink> links => Enumerable.Empty<PackageLink>();

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, SemVersion version, string displayName)
        {
            m_Version = version;
            m_DisplayName = displayName;
            m_IsInstalled = isInstalled;

            UpdatePackageInfo(packageInfo);
        }

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled)
            : this(packageInfo, isInstalled, SemVersion.Parse(packageInfo.version), packageInfo.displayName)
        {
        }

        internal void UpdatePackageInfo(PackageInfo newPackageInfo)
        {
            m_IsFullyFetched = m_Version == newPackageInfo.version;
            m_PackageInfo = newPackageInfo;
            m_Source = m_PackageInfo.source == PackageSource.BuiltIn || m_IsInstalled ? m_PackageInfo.source : PackageSource.Registry;

            RefreshTags();

            m_Author = string.IsNullOrEmpty(m_PackageInfo.author.name) &&
                m_PackageInfo.name.StartsWith(k_UnityPrefix) ? "Unity Technologies Inc." : m_PackageInfo.author.name;

            if (m_Source == PackageSource.BuiltIn)
                m_Description = UpmPackageDocs.SplitBuiltinDescription(this)[0];

            // reset sample parse status on package info update, such that the sample list gets regenerated
            m_SamplesParsed = false;

            if (m_IsFullyFetched)
            {
                m_DisplayName = GetDisplayName(m_PackageInfo);
                m_PackageId = m_PackageInfo.packageId;
                if (hasPathInId)
                    m_PackageId = m_PackageId.Replace("\\", "/");
            }
            else
            {
                m_PackageId = FormatPackageId(name, version.ToString());
            }
        }

        private void RefreshTags()
        {
            switch (m_Source)
            {
                case PackageSource.BuiltIn:
                    m_Tag = type.Equals("module") ? PackageTag.BuiltIn : PackageTag.Core;
                    break;

                case PackageSource.Embedded:
                    m_Tag = PackageTag.InDevelopment;
                    break;

                case PackageSource.Local:
                    m_Tag = PackageTag.Local;
                    break;

                case PackageSource.Git:
                    m_Tag = PackageTag.Git;
                    break;

                case PackageSource.Unknown:
                case PackageSource.Registry:
                default:
                    m_Tag = PackageTag.None;
                    break;
            }

            if (m_Version.IsRelease())
            {
                m_Tag |= PackageTag.Release;
                if (m_Version == m_PackageInfo.versions.verified && !HasTag(PackageTag.InDevelopment | PackageTag.Local | PackageTag.Git))
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
