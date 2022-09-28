// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
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
        private PackageInfo m_PackageInfo;
        public override PackageInfo packageInfo => m_PackageInfo;

        public override string category => m_PackageInfo.category;

        public override bool isDirectDependency => isFullyFetched && m_PackageInfo.isDirectDependency;

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
        private bool m_IsUnityPackage;
        public override bool isUnityPackage => m_IsUnityPackage;

        public override bool isRegistryPackage => m_PackageInfo?.source == PackageSource.Registry;
        public override bool isFromScopedRegistry => isRegistryPackage && m_PackageInfo?.registry?.isDefault == false;
        public override RegistryInfo registry => isRegistryPackage ? m_PackageInfo.registry : null;

        [SerializeField]
        private bool m_IsFullyFetched;
        public override bool isFullyFetched => m_IsFullyFetched;

        public string sourcePath
        {
            get
            {
                if (m_PackageInfo.source == PackageSource.Local || m_PackageInfo.source == PackageSource.LocalTarball)
                    return m_PackageInfo.packageId.Substring(m_PackageInfo.packageId.IndexOf("@file:") + 6);
                if (m_PackageInfo.source == PackageSource.Git)
                    return m_PackageInfo.packageId.Split(new[] {'@'}, 2)[1];
                return null;
            }
        }

        [SerializeField]
        private bool m_IsInstalled;
        public override bool isInstalled => m_IsInstalled;

        public bool installedFromPath => HasTag(PackageTag.Local | PackageTag.Custom | PackageTag.Git);

        public override bool isAvailableOnDisk => m_IsFullyFetched && !string.IsNullOrEmpty(m_PackageInfo.resolvedPath);

        public string shortVersionId => FormatPackageId(name, version?.ShortVersion());

        public string documentationUrl => packageInfo?.documentationUrl;

        public string changelogUrl => packageInfo?.changelogUrl;

        public string licensesUrl => packageInfo?.licensesUrl;

        public override string localPath => packageInfo?.resolvedPath;

        public override string versionString => m_Version.ToString();

        public override long versionId => 0;


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

        internal void UpdatePackageInfo(PackageInfo newPackageInfo, bool isUnityPackage)
        {
            m_IsFullyFetched = m_Version?.ToString() == newPackageInfo.version;
            m_PackageInfo = newPackageInfo;
            m_IsUnityPackage = isUnityPackage;

            RefreshTags();

            // For core packages, or packages that are bundled with Unity without being published, use Unity's build date
            m_PublishedDateTicks = 0;
            if (HasTag(PackageTag.Bundled) && m_PackageInfo.datePublished == null)
                m_PublishedDateTicks = new DateTime(1970, 1, 1).Ticks + InternalEditorUtility.GetUnityVersionDate() * TimeSpan.TicksPerSecond;

            m_Author = this.isUnityPackage ? k_UnityAuthor : m_PackageInfo.author?.name ?? string.Empty;

            if (HasTag(PackageTag.BuiltIn))
                m_Description = UpmPackageDocs.FetchBuiltinDescription(this);

            if (m_IsFullyFetched)
            {
                m_DisplayName = GetDisplayName(m_PackageInfo);
                m_PackageId = m_PackageInfo.packageId;
                if (installedFromPath)
                    m_PackageId = m_PackageId.Replace("\\", "/");

                ProcessErrors(newPackageInfo);
            }
            else
            {
                m_PackageId = FormatPackageId(name, version.ToString());
            }
        }

        public void SetInstalled(bool value)
        {
            m_IsInstalled = value;
            RefreshTags();
        }

        private void RefreshTags()
        {
            m_Tag = PackageTag.UpmFormat;

            // in the case of git/local packages, we always assume that the non-installed versions are from the registry
            var source = m_PackageInfo.source == PackageSource.BuiltIn || m_IsInstalled ? m_PackageInfo.source : PackageSource.Registry;
            switch (source)
            {
                case PackageSource.BuiltIn:
                    m_Tag |= PackageTag.Bundled | PackageTag.VersionLocked;
                    if (m_PackageInfo.type == "module")
                        m_Tag |= PackageTag.BuiltIn;
                    else if (m_PackageInfo.type == "feature")
                        m_Tag |= PackageTag.Feature;
                    break;

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

                case PackageSource.Unknown:
                case PackageSource.Registry:
                default:
                    break;
            }

            if (isFromScopedRegistry)
                m_Tag |= PackageTag.ScopedRegistry;

            // The following logic means that if we see a Unity package on a scoped registry, we will consider it NOT from the scoped registry
            // We'll also mark any non unity packages from any registry as `main not unity`
            if (isRegistryPackage)
            {
                if (isUnityPackage)
                    m_Tag &= ~PackageTag.ScopedRegistry;
                else
                    m_Tag |= PackageTag.MainNotUnity;
            }

            if (!isUnityPackage)
                return;

            m_Tag |= PackageTag.Unity;

            const string previewTagString = "Preview";
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
            else if (isLifecycleVersionValid && m_Version.Value.IsEqualOrPatchOf(lifecycleVersionParsed))
            {
                m_Tag |= PackageTag.Release;
            }
            else if ((version?.Major == 0 && string.IsNullOrEmpty(version?.Prerelease)) ||
                        m_Version?.IsExperimental() == true ||
                        previewTagString.Equals(version?.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Experimental;
        }

        public override string GetDescriptor(bool isFirstLetterCapitalized = false)
        {
            if (HasTag(PackageTag.Feature))
                return isFirstLetterCapitalized? L10n.Tr("Feature") : L10n.Tr("feature");
            if (HasTag(PackageTag.BuiltIn))
                return isFirstLetterCapitalized ? L10n.Tr("Built-in package") : L10n.Tr("built-in package");
            return isFirstLetterCapitalized ? L10n.Tr("Package") : L10n.Tr("package");
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
