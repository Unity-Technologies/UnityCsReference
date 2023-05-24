// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmPackageVersion : BasePackageVersion
    {
        private const string k_UnityPrefix = "com.unity.";
        private const string k_UnityAuthor = "Unity Technologies";

        [SerializeField]
        private string m_Category;
        public override string category => m_Category;

        [SerializeField]
        private bool m_IsFullyFetched;
        public override bool isFullyFetched => m_IsFullyFetched;

        [SerializeField]
        private bool m_IsDirectDependency;
        public override bool isDirectDependency => isFullyFetched && m_IsDirectDependency;

        [SerializeField]
        private List<UIError> m_Errors = new();
        public override IEnumerable<UIError> errors => m_Errors;

        [SerializeField]
        private string m_PackageId;
        public override string packageId => m_PackageId;
        public override string uniqueId => m_PackageId;

        [SerializeField]
        private string m_Author;
        public override string author => m_Author;

        [SerializeField]
        private RegistryType m_AvailableRegistry;
        public override RegistryType availableRegistry => m_AvailableRegistry;

        [SerializeField]
        private PackageSource m_Source;

        [SerializeField]
        private DependencyInfo[] m_Dependencies;
        public override DependencyInfo[] dependencies => m_Dependencies;
        [SerializeField]
        private DependencyInfo[] m_ResolvedDependencies;
        public override DependencyInfo[] resolvedDependencies => m_ResolvedDependencies;
        [SerializeField]
        private EntitlementsInfo m_Entitlements;
        public override EntitlementsInfo entitlements => m_Entitlements;

        [SerializeField]
        private bool m_HasErrorWithEntitlementMessage;
        public override bool hasEntitlementsError => (hasEntitlements && !entitlements.isAllowed) || m_HasErrorWithEntitlementMessage;

        public string sourcePath
        {
            get
            {
                if (HasTag(PackageTag.Local))
                    return m_PackageId.Substring(m_PackageId.IndexOf("@file:") + 6);
                if (HasTag(PackageTag.Git))
                    return m_PackageId.Split(new[] { '@' }, 2)[1];
                return null;
            }
        }

        [SerializeField]
        private bool m_IsInstalled;
        public override bool isInstalled => m_IsInstalled;

        public bool installedFromPath => HasTag(PackageTag.Local | PackageTag.Custom | PackageTag.Git);

        public override bool isAvailableOnDisk => m_IsFullyFetched && !string.IsNullOrEmpty(m_ResolvedPath);

        [SerializeField]
        private string m_ResolvedPath;
        public override string localPath => m_ResolvedPath;

        [SerializeField]
        private string m_VersionInManifest;
        public override string versionInManifest => m_VersionInManifest;


        public override string versionString => isInvalidSemVerInManifest ? versionInManifest : m_VersionString;

        // When packages are installed from path (git, local, custom) versionInManifest behaves differently so we don't consider them to have invalid SemVer
        public override bool isInvalidSemVerInManifest => !string.IsNullOrEmpty(versionInManifest) && !installedFromPath &&
                                                          (!SemVersionParser.TryParse(versionInManifest, out var semVersion) || semVersion?.ToString() != versionInManifest);

        public override long versionId => 0;

        [SerializeField]
        private string m_DeprecationMessage;
        public override string deprecationMessage => m_DeprecationMessage;


        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, SemVersion? version, string displayName, RegistryType availableRegistry)
        {
            m_Version = version;
            m_VersionString = m_Version?.ToString();
            m_DisplayName = displayName;
            m_IsInstalled = isInstalled;

            UpdatePackageInfo(packageInfo, availableRegistry);
        }

        public UpmPackageVersion(PackageInfo packageInfo, bool isInstalled, RegistryType availableRegistry)
        {
            SemVersionParser.TryParse(packageInfo.version, out m_Version);
            m_VersionString = m_Version?.ToString();
            m_DisplayName = packageInfo.displayName;
            m_IsInstalled = isInstalled;

            UpdatePackageInfo(packageInfo, availableRegistry);
        }

        internal void UpdatePackageInfo(PackageInfo packageInfo, RegistryType availableRegistry)
        {
            m_IsFullyFetched = m_Version?.ToString() == packageInfo.version;
            m_AvailableRegistry = availableRegistry;
            m_Source = packageInfo.source;
            m_Category = packageInfo.category;
            m_IsDirectDependency = packageInfo.isDirectDependency;
            m_Name = packageInfo.name;
            m_VersionInManifest = packageInfo.projectDependenciesEntry;
            m_Entitlements = packageInfo.entitlements;

            RefreshTags(packageInfo);

            // For core packages, or packages that are bundled with Unity without being published, use Unity's build date
            m_PublishedDateTicks = packageInfo.datePublished?.Ticks ?? 0;
            if (m_PublishedDateTicks == 0 && packageInfo.source == PackageSource.BuiltIn)
                m_PublishedDateTicks = new DateTime(1970, 1, 1).Ticks + InternalEditorUtility.GetUnityVersionDate() * TimeSpan.TicksPerSecond;

            m_Author = HasTag(PackageTag.Unity) ? k_UnityAuthor : packageInfo.author?.name ?? string.Empty;

            if (m_IsFullyFetched)
            {
                m_DisplayName = GetDisplayName(packageInfo);
                m_PackageId = packageInfo.packageId;
                if (installedFromPath)
                    m_PackageId = m_PackageId.Replace("\\", "/");

                ProcessErrors(packageInfo);

                m_Dependencies = packageInfo.dependencies;
                m_ResolvedDependencies = packageInfo.resolvedDependencies;
                m_ResolvedPath = packageInfo.resolvedPath;
                m_DeprecationMessage = packageInfo.deprecationMessage;

                if (packageInfo.ExtractBuiltinDescription(out var result))
                    m_Description = result;
                else
                    m_Description = packageInfo.description;
            }
            else
            {
                m_PackageId = FormatPackageId(name, version.ToString());

                m_HasErrorWithEntitlementMessage = false;
                m_Errors.Clear();
                m_Dependencies = Array.Empty<DependencyInfo>();
                m_ResolvedDependencies = Array.Empty<DependencyInfo>();
                m_ResolvedPath = string.Empty;
                m_Description = string.Empty;
            }
        }

        public void SetInstalled(bool value)
        {
            m_IsInstalled = value;
            RefreshTagsForLocalAndGit(m_Source);
        }

        private void RefreshTagsForLocalAndGit(PackageSource source)
        {
            m_Tag &= ~(PackageTag.Custom | PackageTag.VersionLocked | PackageTag.Local | PackageTag.Git);
            if (!m_IsInstalled || source is PackageSource.BuiltIn or PackageSource.Registry)
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
            }
        }

        private void RefreshTags(PackageInfo packageInfo)
        {
            m_Tag = PackageTag.UpmFormat;

            // in the case of git/local packages, we always assume that the non-installed versions are from the registry
            if (packageInfo.source == PackageSource.BuiltIn)
            {
                m_Tag |= PackageTag.Unity | PackageTag.VersionLocked;
                switch (packageInfo.type)
                {
                    case "module":
                        m_Tag |= PackageTag.BuiltIn;
                        break;
                    case "feature":
                        m_Tag |= PackageTag.Feature;
                        break;
                }
            }
            else
                RefreshTagsForLocalAndGit(packageInfo.source);

            // We only tag a package as `Unity` when it's directly installed from registry. A package available on Unity registry can be installed
            // through git or local file system but in those cases it is not considered a `Unity` package.
            if (m_Source == PackageSource.Registry && m_AvailableRegistry == RegistryType.UnityRegistry)
                m_Tag |= PackageTag.Unity;

            // We use the logic below instead packageInfo.isDeprecated, since we don't do an extra fetch when we want to tag deprecated version in version history
            // We want to know if a version is deprecated before we do the extra fetch
            if (packageInfo.versions.deprecated.Contains(m_VersionString))
                m_Tag |= PackageTag.Deprecated;

            if (!HasTag(PackageTag.Unity) || HasTag(PackageTag.Deprecated) || isInvalidSemVerInManifest)
                return;

            var isLifecycleVersionValid = SemVersionParser.TryParse(packageInfo.unityLifecycle?.version, out var lifecycleVersionParsed);
            if (m_Version?.HasPreReleaseVersionTag() == true)
            {
                // must match exactly to be release candidate
                if (m_VersionString == packageInfo.unityLifecycle?.version)
                    m_Tag |= PackageTag.ReleaseCandidate;
                else
                    m_Tag |= PackageTag.PreRelease;
            }
            else if ((version?.Major == 0 && string.IsNullOrEmpty(version?.Prerelease)) ||
                        m_Version?.IsExperimental() == true ||
                        "Preview".Equals(version?.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Experimental;
            else if (isLifecycleVersionValid && m_Version?.IsEqualOrPatchOf(lifecycleVersionParsed) == true)
            {
                m_Tag |= PackageTag.Release;
            }
        }

        public override string GetDescriptor(bool isFirstLetterCapitalized = false)
        {
            if (HasTag(PackageTag.Feature))
                return isFirstLetterCapitalized ? L10n.Tr("Feature") : L10n.Tr("feature");
            if (HasTag(PackageTag.BuiltIn))
                return isFirstLetterCapitalized ? L10n.Tr("Built-in package") : L10n.Tr("built-in package");
            return isFirstLetterCapitalized ? L10n.Tr("Package") : L10n.Tr("package");
        }

        private static string GetDisplayName(PackageInfo info)
        {
            return !string.IsNullOrEmpty(info.displayName) ? info.displayName : ExtractDisplayName(info.name);
        }

        public static string ExtractDisplayName(string packageName)
        {
            if (!packageName.StartsWith(k_UnityPrefix))
                return packageName;
            var displayName = packageName.Substring(k_UnityPrefix.Length).Replace("modules.", "");
            displayName = string.Join(" ", displayName.Split('.'));
            return new CultureInfo("en-US").TextInfo.ToTitleCase(displayName);
        }

        public static string FormatPackageId(string name, string version)
        {
            return $"{name.ToLower()}@{version}";
        }

        private void ProcessErrors(PackageInfo info)
        {
            m_HasErrorWithEntitlementMessage = info.errors.Any(error
                => error.errorCode == ErrorCode.Forbidden
                || error.message.IndexOf(EntitlementsErrorAndDeprecationChecker.k_NoSubscriptionUpmErrorMessage, StringComparison.InvariantCultureIgnoreCase) >= 0);

            m_Errors.Clear();

            if (hasEntitlementsError)
                m_Errors.Add(UIError.k_EntitlementError);

            foreach (var error in info.errors)
            {
                if (error.message.Contains(EntitlementsErrorAndDeprecationChecker.k_NotAcquiredUpmErrorMessage))
                    m_Errors.Add(new UIError(UIErrorCode.UpmError_NotAcquired, error.message));
                else if (error.message.Contains(EntitlementsErrorAndDeprecationChecker.k_NotSignedInUpmErrorMessage))
                    m_Errors.Add(new UIError(UIErrorCode.UpmError_NotSignedIn, error.message));
                else
                    m_Errors.Add(new UIError(error));
            }

            if (info.signature.status == SignatureStatus.Invalid)
                m_Errors.Add(UIError.k_InvalidSignatureWarning);
            else if (info.signature.status == SignatureStatus.Unsigned && name.StartsWith(k_UnityPrefix) &&
                     (info.source == PackageSource.LocalTarball ||
                      info.source == PackageSource.Registry && !info.registry.isDefault))
                // Flag Unsigned packages on a non-default registry and local tarballs
                // when the name starts with "com.unity." to prevent dependency confusion
                m_Errors.Add(UIError.k_UnsignedUnityPackageWarning);
        }
    }
}
