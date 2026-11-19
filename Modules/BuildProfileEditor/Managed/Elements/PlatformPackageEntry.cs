// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.PackageManager.UI.Internal;
using UnityEngine;
using PlatformPackageInfo = UnityEditor.BuildTargetDiscovery.PlatformPackageInfo;
using PlatformPackageIdentifier = UnityEditor.BuildTargetDiscovery.PlatformPackageIdentifier;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// <see cref="PlatformPackageItem"/> entry state.
    /// </summary>
    internal class PlatformPackageEntry
    {
        /// <summary>
        /// Covers Asset Store packages whose product info names no license of its own.
        /// </summary>
        internal const string k_UnityEulaUrl = "https://unity.com/legal/as-terms";

        /// <summary>
        /// Covers every other package whose product info names no license of its own. The Asset Store
        /// terms do not apply outside the Asset Store.
        /// </summary>
        internal const string k_UnityTermsOfServiceUrl = "https://unity.com/legal/terms-of-service";

        /// <summary>
        /// Package identifier.
        /// </summary>
        public string qualifiedName { get; private set; }

        /// <summary>
        /// Optional pinned package version from the platform catalog. Empty when the registry
        /// should resolve the latest version.
        /// </summary>
        public string version { get; private set; }

        /// <summary>
        /// Package display name.
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// Package description.
        /// </summary>
        public string description { get; private set; }

        /// <summary>
        /// Package publisher name.
        /// </summary>
        public string publisher { get; private set; }

        /// <summary>
        /// Package thumbnail image.
        /// </summary>
        public Texture thumbnail { get; private set; }

        /// <summary>
        /// Indicates whether the package has a thumbnail and should show a
        /// placeholder if the thumbnail is not available.
        /// </summary>
        public bool hasThumbnail { get; private set; }

        /// <summary>
        /// Required package for a build profile targeting the current platform.
        /// Will automatically be installed when a build profile is created.
        /// </summary>
        public bool required => m_RequiredByPlatform || m_RequiredByVariant;

        /// <summary>
        /// True when Package Manager marks this package deprecated. Falls back to the catalog deprecation flag
        /// when PM has no <see cref="PackageManager.PackageInfo"/> for this package.
        /// </summary>
        public bool deprecated { get; private set; }

        /// <summary>
        /// Deprecation message from Package Manager. Falls back to the catalog deprecation message
        /// when PM has no <see cref="PackageManager.PackageInfo"/> for this package.
        /// </summary>
        public string deprecationTooltip { get; private set; }

        /// <summary>
        /// True when Package Manager reports access to this package as granted by an entitlement.
        /// The catalog carries no entitlement data, so this stays false when PM has no
        /// <see cref="PackageManager.PackageInfo"/> for this package.
        /// </summary>
        public bool entitled { get; private set; }

        /// <summary>
        /// Set when <see cref="qualifiedName"/> is installed.
        /// </summary>
        public bool isInstalled { get; set; }

        /// <summary>
        /// Set when a recommended package should be installed.
        /// </summary>
        public bool shouldInstalled { get; set; }

        /// <summary>
        /// URL of the license this package is distributed under. Packages whose product info names
        /// no license fall back to Unity's EULA.
        /// </summary>
        public string licenseUrl { get; set; }

        /// <summary>
        /// Short name to label <see cref="licenseUrl"/> with.
        /// </summary>
        public string licenseName { get; set; }

        bool m_RequiredByPlatform;
        bool m_RequiredByVariant;
        bool m_UserSelectionBeforeRequiredByVariant;

        public PlatformPackageEntry()
        {
            qualifiedName = string.Empty;
            version = string.Empty;
            displayName = string.Empty;
            description = string.Empty;
            publisher = string.Empty;
            thumbnail = null;
            hasThumbnail = false;
            shouldInstalled = false;
            deprecated = false;
            deprecationTooltip = string.Empty;
            entitled = false;
            isInstalled = false;
            licenseUrl = k_UnityTermsOfServiceUrl;
            licenseName = TrText.unityTermsOfServiceName;
        }

        /// <param name="name"><see cref="qualifiedName"/></param>
        /// <param name="selected">Set when package is toggled on for installation.</param>
        /// <param name="requiredByPlatform">Set when the platform requires the package, regardless of which preconfigured settings variant is selected.</param>
        /// <param name="isInstalled"><see cref="isInstalled"/></param>
        public PlatformPackageEntry(PlatformPackageInfo platformPackageInfo, bool selected, bool requiredByPlatform, bool isInstalled)
        {
            qualifiedName = platformPackageInfo.qualifiedName;
            version = platformPackageInfo.version;
            var serviceInfoProvider = BuildProfileContext.packageServiceInfoProvider;
            var packageInfo = serviceInfoProvider.GetPackageInfo(qualifiedName);
            displayName = packageInfo != null ? packageInfo.displayName : platformPackageInfo.displayName;
            description = packageInfo != null ? packageInfo.description : platformPackageInfo.description;

            var thumbnail = serviceInfoProvider.GetThumbnail(qualifiedName);
            this.thumbnail = thumbnail != null ? thumbnail : null;
            hasThumbnail = platformPackageInfo.hasThumbnail;
  
            publisher = platformPackageInfo.publisher;
            (licenseUrl, licenseName) = ResolveLicense(
                serviceInfoProvider.GetLicenseUrl(qualifiedName),
                serviceInfoProvider.GetLicenseName(qualifiedName),
                serviceInfoProvider.IsAssetStorePackage(qualifiedName));

            shouldInstalled = requiredByPlatform || selected;
            m_RequiredByPlatform = requiredByPlatform;
            this.isInstalled = isInstalled;

            if (packageInfo != null)
            {
                deprecated = packageInfo.isDeprecated || packageInfo.IsPackageLifeCycleDeprecated();
                deprecationTooltip = deprecated ? packageInfo.GetDeprecationMessageForBuildProfile() : string.Empty;
                entitled = serviceInfoProvider.IsEnterprisePackage(qualifiedName);
            }
            else
            {
                deprecated = platformPackageInfo.deprecated;
                deprecationTooltip = deprecated && !string.IsNullOrEmpty(platformPackageInfo.deprecationMessage)
                    ? platformPackageInfo.deprecationMessage
                    : string.Empty;
            }
        }

        /// <summary>
        /// Marks this package as required by the selected preconfigured settings variants. Releasing the requirement
        /// restores the install selection the user had before.
        /// </summary>
        public void SetRequiredByVariant(bool value)
        {
            if (m_RequiredByPlatform || value == m_RequiredByVariant)
                return;

            if (value)
            {
                m_UserSelectionBeforeRequiredByVariant = shouldInstalled;
                shouldInstalled = true;
            }
            else
                shouldInstalled = m_UserSelectionBeforeRequiredByVariant;

            m_RequiredByVariant = value;
        }

        /// <summary>
        /// Picks the license to show for a package. A product that names its own license keeps it; one that
        /// does not falls back to the Unity license covering how it was obtained.
        /// </summary>
        internal static (string url, string name) ResolveLicense(
            string resolvedUrl, string resolvedName, bool isAssetStorePackage)
        {
            if (string.IsNullOrEmpty(resolvedUrl))
            {
                return isAssetStorePackage
                    ? (k_UnityEulaUrl, TrText.unityEulaName)
                    : (k_UnityTermsOfServiceUrl, TrText.unityTermsOfServiceName);
            }

            return (resolvedUrl, string.IsNullOrEmpty(resolvedName) ? TrText.licenseFallbackName : resolvedName);
        }

        /// <summary>
        /// Identifies this package for installation, pairing <see cref="qualifiedName"/> with
        /// <see cref="version"/>.
        /// </summary>
        public PlatformPackageIdentifier GetPackageIdentifier() =>
            new PlatformPackageIdentifier(qualifiedName, version);
    }
}
