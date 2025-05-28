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
        public override string category => m_Category ?? string.Empty;

        [SerializeField]
        private bool m_IsFullyFetched;
        public override bool isFullyFetched => m_IsFullyFetched;

        [SerializeField]
        private bool m_IsDirectDependency;
        public override bool isDirectDependency => isFullyFetched && m_IsDirectDependency;

        [SerializeField]
        private List<UIError> m_Errors = new();
        public override IEnumerable<UIError> errors => m_Errors ?? Enumerable.Empty<UIError>();

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
        private DependencyInfo[] m_Dependencies;
        public override DependencyInfo[] dependencies => m_Dependencies ?? Array.Empty<DependencyInfo>();
        [SerializeField]
        private DependencyInfo[] m_ResolvedDependencies;
        public override DependencyInfo[] resolvedDependencies => m_ResolvedDependencies ?? Array.Empty<DependencyInfo>();
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

        [SerializeField]
        private string m_ResolvedPath;
        public override string localPath => m_ResolvedPath ?? string.Empty;

        [SerializeField]
        private string m_VersionInManifest;
        public override string versionInManifest => m_VersionInManifest ?? string.Empty;

        public override string versionString => isInvalidSemVerInManifest ? versionInManifest : m_VersionString;

        // When packages are installed from path (git, local, custom) versionInManifest behaves differently so we don't consider them to have invalid SemVer
        public override bool isInvalidSemVerInManifest => !string.IsNullOrEmpty(versionInManifest) && !HasTag(PackageTag.InstalledFromPath) &&
                                                          (!SemVersionParser.TryParse(versionInManifest, out var semVersion) || semVersion?.ToString() != versionInManifest);

        public override long uploadId => 0;

        [SerializeField]
        private string m_DeprecationMessage;
        public override string deprecationMessage => m_DeprecationMessage ?? string.Empty;

        private UpmPackageVersion(string name, string versionString, RegistryType availableRegistry)
        {
            m_Name = name;
            m_AvailableRegistry = availableRegistry;

            SemVersionParser.TryParse(versionString, out m_Version);
            m_VersionString = m_Version?.ToString();
        }

        public static UpmPackageVersion CreateWithIncompleteInfo(IUpmPackageData packageData, string versionString)
        {
            var packageVersion = new UpmPackageVersion(packageData.name, versionString, packageData.availableRegistryType)
            {
                m_IsFullyFetched = false,
                m_IsInstalled =  false,
                m_IsDirectDependency = false,
                m_DisplayName = !string.IsNullOrEmpty(packageData.displayName) ? packageData.displayName : ExtractDisplayName(packageData.name)
            };
            packageVersion.UpdateTags(packageData);
            packageVersion.m_PackageId = FormatPackageId(packageVersion.m_Name, packageVersion.m_VersionString);
            packageVersion.m_Author = packageVersion.HasTag(PackageTag.Unity) ? k_UnityAuthor : string.Empty;
            return packageVersion;
        }

        public static UpmPackageVersion CreateWithCompleteInfo(IUpmPackageData packageData, PackageInfo packageInfo, bool isInstalled)
        {
            var packageVersion = new UpmPackageVersion(packageInfo.name, packageInfo.version, packageData.availableRegistryType)
            {
                m_IsFullyFetched = true,
                m_IsInstalled = isInstalled,
                m_IsDirectDependency = packageInfo.isDirectDependency,
                m_VersionInManifest = isInstalled ? packageInfo.projectDependenciesEntry : string.Empty,
                m_DisplayName = !string.IsNullOrEmpty(packageInfo.displayName) ? packageInfo.displayName : ExtractDisplayName(packageInfo.name),
                m_Category = packageInfo.category,
                m_Entitlements = packageInfo.entitlements,
                m_Dependencies = packageInfo.dependencies,
                m_ResolvedDependencies = packageInfo.resolvedDependencies,
                m_ResolvedPath = packageInfo.resolvedPath,
                m_DeprecationMessage = packageInfo.deprecationMessage,
                m_Description = packageInfo.ExtractBuiltinDescription(out var result) ? result : packageInfo.description,
                m_PublishedDateTicks = GetPublishDateTicks(packageInfo)
            };
            packageVersion.UpdateTags(packageData, packageInfo);
            packageVersion.m_PackageId = packageVersion.HasTag(PackageTag.InstalledFromPath) ? packageInfo.packageId.Replace("\\", "/") : packageInfo.packageId;
            packageVersion.m_Author = packageVersion.HasTag(PackageTag.Unity) ? k_UnityAuthor : packageInfo.author?.name ?? string.Empty;
            packageVersion.ProcessErrors(packageInfo);
            return packageVersion;
        }

        private void UpdateTags(IUpmPackageData packageData, PackageInfo packageInfo = null)
        {
            m_Tag = GetTagsFromPackageInfo(packageInfo);
            m_Tag |= PackageTag.UpmFormat;
            if (m_Version != null && !isInvalidSemVerInManifest)
                m_Tag |= m_Version.Value.GetExpOrPreOrReleaseTag();

            if (HasTag(PackageTag.InstalledFromPath))
                return;

            // We only tag a package as `Unity` when it's directly installed from registry or built in. A package available on Unity registry can be installed
            // through git or local file system, but in those cases it is not considered a `Unity` package.
            if (m_AvailableRegistry == RegistryType.UnityRegistry && packageInfo?.source != PackageSource.Unknown)
                m_Tag |= PackageTag.Unity;

            // We use the logic below instead packageInfo.isDeprecated, since we don't do an extra fetch when we want to tag deprecated version in version history
            // We want to know if a version is deprecated before we do the extra fetch
            if (packageData.IsVersionDeprecated(m_VersionString))
                m_Tag |= PackageTag.Deprecated;
        }

        private static PackageTag GetTagsFromPackageInfo(PackageInfo packageInfo)
        {
            switch (packageInfo?.source)
            {
                case PackageSource.Embedded:
                    return PackageTag.Custom;
                case PackageSource.Local:
                case PackageSource.LocalTarball:
                    return PackageTag.Local;
                case PackageSource.Git:
                    return PackageTag.Git;
                case PackageSource.BuiltIn:
                    if (packageInfo.type == "module")
                        return PackageTag.BuiltIn;
                    if (packageInfo.type == "feature")
                        return PackageTag.Feature;
                    break;
            }
            return PackageTag.None;
        }

        public override string GetDescriptor(bool isFirstLetterCapitalized = false)
        {
            if (HasTag(PackageTag.Feature))
                return isFirstLetterCapitalized ? L10n.Tr("Feature") : L10n.Tr("feature");
            if (HasTag(PackageTag.BuiltIn))
                return isFirstLetterCapitalized ? L10n.Tr("Built-in package") : L10n.Tr("built-in package");
            return isFirstLetterCapitalized ? L10n.Tr("Package") : L10n.Tr("package");
        }

        private static long GetPublishDateTicks(PackageInfo info)
        {
            // For core packages, or packages that are bundled with Unity without being published, use Unity's build date
            var result = info.datePublished?.Ticks ?? 0;
            if (result == 0 && info.source == PackageSource.BuiltIn)
                result = new DateTime(1970, 1, 1).Ticks + InternalEditorUtility.GetUnityVersionDate() * TimeSpan.TicksPerSecond;
            return result;
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
                && error.message.IndexOf(EntitlementsErrorAndDeprecationChecker.k_NoSubscriptionUpmErrorMessage, StringComparison.InvariantCultureIgnoreCase) >= 0);

            m_Errors.Clear();

            if (hasEntitlementsError)
                m_Errors.Add(isInstalled ? UIError.k_EntitlementError : UIError.k_EntitlementWarning);

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
