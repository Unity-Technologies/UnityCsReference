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
        private const string k_UnityPrefix = "com.unity.";
        private const string k_UnityAuthor = "Unity Technologies";
        private static readonly string k_EntitlementsErrorMessage =
            L10n.Tr("This package is not available to use because there is no license registered for your user. " +
                    "If you believe you have permission to use this package, " +
                    "refresh your license in the license management window of Unity Hub. " +
                    "Otherwise, contact your administrator.");

        [SerializeField]
        private string m_Category;
        public override string category => m_Category;

        [SerializeField]
        private Error[] m_UpmErrors = new Error[0];

        private UIError entitlementsError => !entitlements.isAllowed && isInstalled ? new UIError(UIErrorCode.UpmError, k_EntitlementsErrorMessage) : null;
        public override IEnumerable<UIError> errors =>
            m_UpmErrors.Select(e => new UIError((UIErrorCode)e.errorCode, e.message, UIError.Attribute.None)).Concat(entitlementsError != null ? new List<UIError> { entitlementsError } : new List<UIError>());

        [SerializeField]
        private bool m_IsFullyFetched;
        public override bool isFullyFetched => m_IsFullyFetched;

        [SerializeField]
        private bool m_IsDirectDependency;
        public override bool isDirectDependency => isFullyFetched && m_IsDirectDependency;

        [SerializeField]
        private string m_PackageId;
        public override string uniqueId => m_PackageId;

        [SerializeField]
        private string m_Author;
        public override string author => m_Author;

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
        private PackageSource m_Source;
        public bool isRegistryPackage => m_Source == PackageSource.Registry;

        [SerializeField]
        private bool m_IsFromScopedRegistry;
        public bool isFromScopedRegistry => isRegistryPackage && m_IsFromScopedRegistry;

        [SerializeField]
        private DependencyInfo[] m_Dependencies;
        public override DependencyInfo[] dependencies => m_Dependencies;
        [SerializeField]
        private DependencyInfo[] m_ResolvedDependencies;
        public override DependencyInfo[] resolvedDependencies => m_ResolvedDependencies;
        [SerializeField]
        private EntitlementsInfo m_Entitlements;
        public override EntitlementsInfo entitlements => m_Entitlements;

        public string sourcePath
        {
            get
            {
                if (HasTag(PackageTag.Local))
                    return m_PackageId.Substring(m_PackageId.IndexOf("@file:") + 6);
                if (HasTag(PackageTag.Git))
                    return m_PackageId.Split(new[] {'@'}, 2)[1];
                return null;
            }
        }

        [SerializeField]
        private bool m_IsInstalled;
        public override bool isInstalled
        {
            get { return m_IsInstalled; }
        }

        public bool installedFromPath => HasTag(PackageTag.Local | PackageTag.Custom | PackageTag.Git);

        public override bool isAvailableOnDisk => m_IsFullyFetched && !string.IsNullOrEmpty(m_ResolvedPath);

        [SerializeField]
        private string m_ResolvedPath;
        public override string localPath => m_ResolvedPath;

        public override string versionString => m_Version.ToString();

        public override string versionId => m_Version.ToString();

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, SemVersion? version, string displayName, bool isUnityPackage)
        {
            m_Version = version;
            m_VersionString = m_Version?.ToString();
            m_DisplayName = displayName;
            m_IsInstalled = isInstalled;

            UpdatePackageInfo(packageInfo, isUnityPackage);
        }

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, bool isUnityPackage)
        {
            SemVersionParser.TryParse(packageInfo.version, out m_Version);
            m_VersionString = m_Version?.ToString();
            m_DisplayName = packageInfo.displayName;
            m_IsInstalled = isInstalled;

            UpdatePackageInfo(packageInfo, isUnityPackage);
        }

        internal void UpdatePackageInfo(PackageInfo packageInfo, bool isUnityPackage)
        {
            m_IsFullyFetched = m_Version?.ToString() == packageInfo.version;
            m_PackageUniqueId = packageInfo.name;
            m_IsUnityPackage = isUnityPackage;
            m_Source = packageInfo.source;
            m_Category = packageInfo.category;
            m_IsDirectDependency = packageInfo.isDirectDependency;
            m_IsFromScopedRegistry = packageInfo.registry?.isDefault == false;
            m_Name = packageInfo.name;

            RefreshTags(packageInfo);

            // For core packages, or packages that are bundled with Unity without being published, use Unity's build date
            m_PublishedDateTicks = packageInfo.datePublished?.Ticks ?? 0;
            if (HasTag(PackageTag.Bundled) && packageInfo.datePublished == null)
                m_PublishedDateTicks = new DateTime(1970, 1, 1).Ticks + InternalEditorUtility.GetUnityVersionDate() * TimeSpan.TicksPerSecond;

            m_Author = this.isUnityPackage ? k_UnityAuthor : packageInfo.author?.name ?? string.Empty;

            if (m_IsFullyFetched)
            {
                m_DisplayName = GetDisplayName(packageInfo);
                m_PackageId = packageInfo.packageId;
                if (installedFromPath)
                    m_PackageId = m_PackageId.Replace("\\", "/");

                m_UpmErrors = packageInfo.errors;
                m_Dependencies = packageInfo.dependencies;
                m_ResolvedDependencies = packageInfo.resolvedDependencies;
                m_Entitlements = packageInfo.entitlements;
                m_ResolvedPath = packageInfo.resolvedPath;

                if (HasTag(PackageTag.BuiltIn))
                    m_Description = UpmPackageDocs.FetchBuiltinDescription(packageInfo);
                else
                    m_Description = packageInfo.description;
            }
            else
            {
                m_PackageId = FormatPackageId(name, version.ToString());

                m_UpmErrors = new Error[0];
                m_Dependencies = new DependencyInfo[0];
                m_ResolvedDependencies = new DependencyInfo[0];
                m_Entitlements = new EntitlementsInfo();
                m_ResolvedPath = string.Empty;
                m_Description = string.Empty;
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
            RefreshTagsForLocalAndGit(m_Source);
        }

        private void RefreshTagsForLocalAndGit(PackageSource source)
        {
            if (source == PackageSource.BuiltIn || source == PackageSource.Registry)
                return;

            m_Tag &= ~(PackageTag.Custom | PackageTag.VersionLocked | PackageTag.Local | PackageTag.Git);
            if (!m_IsInstalled)
                return;

            switch (source)
            {
                case PackageSource.Embedded:
                    m_Tag |= PackageTag.Custom | PackageTag.VersionLocked;
                    break;
                case PackageSource.Local:
                case PackageSource.LocalTarball:
                    m_Tag |= PackageTag.Local;
                    break;
                case PackageSource.Git:
                    m_Tag |= PackageTag.Git | PackageTag.VersionLocked;
                    break;
                default:
                    break;
            }
        }

        private void RefreshTags(PackageInfo packageInfo)
        {
            // in the case of git/local packages, we always assume that the non-installed versions are from the registry
            var source = packageInfo.source == PackageSource.BuiltIn || m_IsInstalled ? packageInfo.source : PackageSource.Registry;
            m_Tag = PackageTag.None;
            if (source == PackageSource.BuiltIn)
            {
                m_Tag = PackageTag.Bundled | PackageTag.VersionLocked;
                if (packageInfo.type == "module")
                    m_Tag |= PackageTag.BuiltIn;
            }
            else
                RefreshTagsForLocalAndGit(packageInfo.source);

            m_Tag |= PackageTag.Installable | PackageTag.Removable;
            if (isInstalled && isDirectDependency && !installedFromPath && !HasTag(PackageTag.BuiltIn))
                m_Tag |= PackageTag.Embeddable;

            if (isUnityPackage)
            {
                if (m_Version?.IsRelease() == true)
                {
                    m_Tag |= PackageTag.Release;
                    SemVersion? verified;
                    bool isVerifiedParsed = SemVersionParser.TryParse(packageInfo.versions.verified, out verified);

                    if (isVerifiedParsed && m_Version == verified && !installedFromPath)
                        m_Tag |= PackageTag.Verified;
                }
                else
                {
                    if ((version?.Major == 0 && string.IsNullOrEmpty(version?.Prerelease)) ||
                        IsVersionTagPreview(version))
                        m_Tag |= PackageTag.Preview;
                }
            }
        }

        private bool IsVersionTagPreview(SemVersion? version)
        {
            var versionTag = version?.Prerelease.Split('.')[0];

            return !string.IsNullOrEmpty(versionTag);
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
