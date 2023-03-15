// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Experimental.Licensing;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class BasePackageVersion : IPackageVersion, ISerializationCallbackReceiver
    {
        [SerializeField]
        protected string m_Name;
        public virtual string name => m_Name ?? string.Empty;

        [SerializeField]
        protected string m_DisplayName;
        public virtual string displayName => m_DisplayName;

        [SerializeField]
        protected string m_Description;
        public string description => m_Description ?? string.Empty;

        [SerializeField]
        protected string m_VersionString;
        protected SemVersion? m_Version;
        public virtual SemVersion? version => m_Version;

        public virtual string versionInManifest => null;
        public virtual bool isInvalidSemVerInManifest => false;

        [SerializeField]
        protected long m_PublishedDateTicks;
        public DateTime? publishedDate => m_PublishedDateTicks == 0 ? null : new DateTime(m_PublishedDateTicks, DateTimeKind.Utc);

        [SerializeField]
        protected string m_PublishNotes;
        public string localReleaseNotes => m_PublishNotes;

        public virtual DependencyInfo[] dependencies => null;
        public virtual DependencyInfo[] resolvedDependencies => null;
        public virtual EntitlementsInfo entitlements => null;

        public virtual IEnumerable<Asset> importedAssets => null;

        [NonSerialized]
        private IPackage m_Package;
        public UI.IPackage package => m_Package;
        IPackage IPackageVersion.package
        {
            get => m_Package;
            set { m_Package = value; }
        }

        [SerializeField]
        protected PackageTag m_Tag;
        public virtual bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) != 0;
        }

        // Analytics tags are different from the package tags we use.
        // We need them to identify different situations that we don't necessarily have tags for
        public string GetAnalyticsTags()
        {
            var tags = new List<string>();
            if (m_Tag != PackageTag.None)
                tags.Add(m_Tag.ToString());
            if (m_Package.isDeprecated)
                tags.Add("PackageDeprecation");
            return string.Join(", ", tags);
        }

        public virtual RegistryType availableRegistry => RegistryType.None;

        // Currently we don't consider Upm Packages with EntitlementLicensingModel.AssetStore as having entitlements
        // and it is only used right now to check if a package is from Asset Store. This is also to be consistent with
        // other Asset Store packages (as in, if Upm Packages on Asset Store are considered with entitlements, then every
        // package from Asset Store should be considered to have entitlements).
        public bool hasEntitlements => entitlements != null &&
                (entitlements.licensingModel == EntitlementLicensingModel.Enterprise
                || entitlements.status == EntitlementStatus.NotGranted
                || entitlements.status == EntitlementStatus.Granted);

        public virtual bool hasEntitlementsError => false;

        public virtual IEnumerable<UIError> errors => Enumerable.Empty<UIError>();
        public virtual IEnumerable<PackageSizeInfo> sizes => Enumerable.Empty<PackageSizeInfo>();
        public virtual IEnumerable<SemVersion> supportedVersions => Enumerable.Empty<SemVersion>();
        public virtual SemVersion? supportedVersion => null;
        public virtual string deprecationMessage => null;

        public abstract string uniqueId { get; }
        public abstract string packageId { get; }
        public abstract string category { get; }
        public abstract string author { get; }
        public abstract bool isInstalled { get; }
        public abstract bool isFullyFetched { get; }
        public abstract bool isAvailableOnDisk { get; }
        public abstract bool isDirectDependency { get; }
        public abstract string localPath { get; }
        public abstract string versionString { get; }
        public abstract long versionId { get; }

        public bool IsDifferentVersionThanRequested
            => !string.IsNullOrEmpty(versionInManifest) && !HasTag(PackageTag.Git | PackageTag.Local | PackageTag.Custom) &&
                versionInManifest != versionString;

        public bool IsRequestedButOverriddenVersion
            => !string.IsNullOrEmpty(versionString) && !isInstalled &&
                versionString == m_Package?.versions.primary.versionInManifest;

        public virtual string GetDescriptor(bool isFirstLetterCapitalized = false)
        {
            return isFirstLetterCapitalized ? L10n.Tr("Package") : L10n.Tr("package");
        }

        public virtual void OnBeforeSerialize()
        {
            // Do nothing
        }

        public virtual void OnAfterDeserialize()
        {
            SemVersionParser.TryParse(m_VersionString, out m_Version);
        }

        public virtual bool MatchesSearchText(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            if (name.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (!string.IsNullOrEmpty(displayName) && displayName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            var prerelease = searchText.StartsWith("-") ? searchText.Substring(1) : searchText;
            if (version != null && ((SemVersion)version).Prerelease.IndexOf(prerelease, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            // searching for pre-release if search text matches with search term 'pre', case insensitive
            const string prereleaseSearchText = "Pre";
            if (HasTag(PackageTag.PreRelease) && prereleaseSearchText.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            // searching for experimental if search text matches with search term 'experimental', case insensitive
            const string experimentalSearchText = "Experimental";
            if (HasTag(PackageTag.Experimental) && experimentalSearchText.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (HasTag(PackageTag.Release) && PackageTag.Release.ToString().IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version?.StripTag().StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase) == true)
                return true;

            if (!string.IsNullOrEmpty(category))
            {
                var words = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var categories =  category.Split('/');
                if (words.All(word => word.Length >= 2 && categories.Any(category => category.StartsWith(word, StringComparison.CurrentCultureIgnoreCase))))
                    return true;
            }

            return false;
        }
    }
}
