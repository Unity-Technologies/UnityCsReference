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
        public override string uniqueId => m_PackageId;

        [SerializeField]
        private string m_Author;
        public override string author => m_Author;

        [SerializeField]
        private bool m_IsUnityPackage;
        public override bool isUnityPackage => m_IsUnityPackage;

        [SerializeField]
        private PackageSource m_Source;
        public override bool isRegistryPackage => m_Source == PackageSource.Registry;

        [SerializeField]
        private bool m_IsFromScopedRegistry;
        public override bool isFromScopedRegistry => isRegistryPackage && m_IsFromScopedRegistry;

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
            m_IsUnityPackage = isUnityPackage;
            m_Source = packageInfo.source;
            m_Category = packageInfo.category;
            m_IsDirectDependency = packageInfo.isDirectDependency;
            m_IsFromScopedRegistry = packageInfo.registry?.isDefault == false;
            m_Name = packageInfo.name;
            m_VersionInManifest = packageInfo.projectDependenciesEntry;

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

                ProcessErrors(packageInfo);

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

                m_HasErrorWithEntitlementMessage = false;
                m_Errors.Clear();
                m_Dependencies = new DependencyInfo[0];
                m_ResolvedDependencies = new DependencyInfo[0];
                m_Entitlements = new EntitlementsInfo();
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
                else if (packageInfo.type == "feature")
                    m_Tag |= PackageTag.Feature;
            }
            else
                RefreshTagsForLocalAndGit(packageInfo.source);

            m_Tag |= PackageTag.Installable | PackageTag.Removable;
            if (isInstalled && isDirectDependency && !installedFromPath && !HasTag(PackageTag.BuiltIn))
                m_Tag |= PackageTag.Embeddable;

            // lifecycle tags should not apply to scoped registry packages
            if (isUnityPackage)
            {
                var previewTagString = "Preview";
                SemVersion? lifecycleVersionParsed;
                var isLifecycleVersionValid = SemVersionParser.TryParse(packageInfo.unityLifecycle?.version, out lifecycleVersionParsed);

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
                         previewTagString.Equals(version?.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
                    m_Tag |= PackageTag.Experimental;
                else if (isLifecycleVersionValid && m_Version?.IsEqualOrPatchOf(lifecycleVersionParsed) == true)
                {
                    m_Tag |= PackageTag.Release;
                }
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

        private void ProcessErrors(PackageInfo info)
        {
            m_HasErrorWithEntitlementMessage = info.errors.Any(error
                => error.errorCode == ErrorCode.Forbidden
                || error.message.IndexOf(EntitlementsErrorChecker.k_NoSubscriptionUpmErrorMessage, StringComparison.InvariantCultureIgnoreCase) >= 0);

            m_Errors.Clear();
            if (hasEntitlementsError)
                m_Errors.Add(UIError.k_EntitlementError);

            foreach (var error in info.errors)
            {
                if (error.message.Contains(EntitlementsErrorChecker.k_NotAcquiredUpmErrorMessage))
                    m_Errors.Add(new UIError(UIErrorCode.UpmError_NotAcquired, error.message));
                else if (error.message.Contains(EntitlementsErrorChecker.k_NotSignedInUpmErrorMessage))
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
