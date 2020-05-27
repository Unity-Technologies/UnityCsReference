// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmPackageVersion : BasePackageVersion
    {
        static readonly string k_UnityPrefix = "com.unity.";

        [SerializeField]
        private PackageInfo m_PackageInfo;
        public override PackageInfo packageInfo => m_PackageInfo;

        public override string category => m_PackageInfo.category;

        UIError entitlementsError => !entitlements.isAllowed && isInstalled ?
        new UIError(UIErrorCode.UpmError, L10n.Tr("You do not have entitlements for this package.")) : null;
        public override IEnumerable<UIError> errors =>
            m_PackageInfo.errors.Select(e => new UIError((UIErrorCode)e.errorCode, e.message)).Concat(entitlementsError != null ? new List<UIError> { entitlementsError } : new List<UIError>());
        public override bool isDirectDependency => isFullyFetched && m_PackageInfo.isDirectDependency;

        [SerializeField]
        private string m_PackageId;
        public override string uniqueId => m_PackageId;

        [SerializeField]
        private string m_Author;
        public override string author => m_Author;

        [SerializeField]
        private bool m_IsFullyFetched;
        public override bool isFullyFetched => m_IsFullyFetched;

        public string sourcePath
        {
            get
            {
                if (m_PackageInfo.source == PackageSource.Local || m_PackageInfo.source == PackageSource.LocalTarball)
                {
                    return m_PackageInfo.packageId.Substring(m_PackageInfo.packageId.IndexOf("@file:") + 6);
                }
                else
                    return null;
            }
        }

        [SerializeField]
        private bool m_IsInstalled;
        public override bool isInstalled
        {
            get { return m_IsInstalled; }
        }

        public bool installedFromPath => HasTag(PackageTag.Local | PackageTag.InDevelopment | PackageTag.Git);

        public override bool isAvailableOnDisk => m_IsFullyFetched && !string.IsNullOrEmpty(m_PackageInfo.resolvedPath);

        public string shortVersionId => FormatPackageId(name, version?.ShortVersion());

        public string documentationUrl => packageInfo?.documentationUrl;

        public string changelogUrl => packageInfo?.changelogUrl;

        public string licensesUrl => packageInfo?.licensesUrl;

        public override string localPath => packageInfo?.resolvedPath;

        public override string versionString => m_Version.ToString();

        public override string versionId => m_Version.ToString();

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, SemVersion? version, string displayName)
        {
            m_Version = version;
            m_VersionString = m_Version?.ToString();
            m_DisplayName = displayName;
            m_IsInstalled = isInstalled;
            m_PackageUniqueId = packageInfo.name;

            UpdatePackageInfo(packageInfo);
        }

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled)
        {
            SemVersionParser.TryParse(packageInfo.version, out m_Version);
            m_VersionString = m_Version?.ToString();
            m_DisplayName = packageInfo.displayName;
            m_IsInstalled = isInstalled;
            m_PackageUniqueId = packageInfo.name;

            UpdatePackageInfo(packageInfo);
        }

        internal void UpdatePackageInfo(PackageInfo newPackageInfo)
        {
            m_IsFullyFetched = m_Version?.ToString() == newPackageInfo.version;
            m_PackageInfo = newPackageInfo;
            m_PackageUniqueId = m_PackageInfo.name;

            RefreshTags();

            // For core packages, or packages that are bundled with Unity without being published, use Unity's build date
            m_PublishedDateTicks = 0;
            if (HasTag(PackageTag.Bundled) && m_PackageInfo.datePublished == null)
                m_PublishedDateTicks = new DateTime(1970, 1, 1).Ticks + InternalEditorUtility.GetUnityVersionDate() * TimeSpan.TicksPerSecond;

            m_Author = string.IsNullOrEmpty(m_PackageInfo.author.name) &&
                m_PackageInfo.name.StartsWith(k_UnityPrefix) ? "Unity Technologies Inc." : m_PackageInfo.author.name;

            if (HasTag(PackageTag.BuiltIn))
                m_Description = UpmPackageDocs.SplitBuiltinDescription(this)[0];

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

        internal void UpdateProductInfo(AssetStoreProductInfo productInfo)
        {
            m_PackageUniqueId = productInfo.id;
            m_PublishNotes = productInfo.publishNotes;

            // override version info with product info
            m_DisplayName = productInfo.displayName;
            m_Description = productInfo.description;
        }

        public void SetInstalled(bool value)
        {
            m_IsInstalled = value;
            RefreshTags();
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
                    m_Tag = PackageTag.InDevelopment | PackageTag.VersionLocked;
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

            if (m_Version?.IsRelease() == true)
            {
                m_Tag |= PackageTag.Release;
                SemVersion? verified;
                bool isVerifiedParsed = SemVersionParser.TryParse(m_PackageInfo.versions.verified, out verified);

                if (isVerifiedParsed && m_Version == verified && !installedFromPath)
                    m_Tag |= PackageTag.Verified;
            }
            else
            {
                if ((version?.Major == 0 && string.IsNullOrEmpty(version?.Prerelease)) ||
                    PackageTag.Preview.ToString().Equals(version?.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
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
