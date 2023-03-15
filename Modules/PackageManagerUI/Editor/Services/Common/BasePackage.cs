// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class BasePackage : IPackage
    {
        [SerializeField]
        protected string m_Name;
        public string name => m_Name;

        public string displayName => versions.FirstOrDefault()?.displayName ?? string.Empty;
        public PackageState state
        {
            get
            {
                if (progress != PackageProgress.None)
                    return PackageState.InProgress;

                if (errors.Any())
                {
                    if (errors.All(error => error.HasAttribute(UIError.Attribute.IsWarning)))
                        return PackageState.Warning;
                    else if (errors.Any(error => !error.HasAttribute(UIError.Attribute.IsClearable)))
                        return PackageState.Error;
                }

                var primary = versions.primary;
                var recommended = versions.recommended;
                var latestKeyVersion = versions.key.LastOrDefault();
                if (primary.HasTag(PackageTag.Custom))
                    return PackageState.InDevelopment;

                if (primary.isInstalled && !primary.isDirectDependency)
                    return PackageState.InstalledAsDependency;

                if (primary != recommended && ((primary.isInstalled && primary != latestKeyVersion) || primary is AssetStorePackageVersion))
                    return PackageState.UpdateAvailable;

                if (versions.importAvailable != null)
                    return PackageState.ImportAvailable;

                if (versions.installed != null)
                    return PackageState.Installed;

                if (primary.HasTag(PackageTag.Downloadable))
                    return PackageState.DownloadAvailable;

                return PackageState.None;
            }
        }

        [SerializeField]
        protected PackageProgress m_Progress;
        public PackageProgress progress
        {
            get { return m_Progress; }
            set { m_Progress = value; }
        }

        // errors on the package level (not just about a particular version)
        [SerializeField]
        protected List<UIError> m_Errors;

        // Combined errors for this package or any version.
        // Stop lookup after first error encountered on a version to save time not looking up redundant errors.
        public IEnumerable<UIError> errors
        {
            get
            {
                var versionErrors = versions.Select(v => v.errors).FirstOrDefault(e => e?.Any() ?? false) ?? Enumerable.Empty<UIError>();
                return m_Errors == null ? versionErrors : m_Errors.Concat(versionErrors);
            }
        }

        public bool hasEntitlements => versions.Any(v => v.availableRegistry == RegistryType.UnityRegistry && v.hasEntitlements);

        public bool hasEntitlementsError => m_Errors.Any(error => error.errorCode == UIErrorCode.Forbidden) || versions.Any(version => version.hasEntitlementsError);

        public void AddError(UIError error)
        {
            if (error.errorCode == UIErrorCode.Forbidden)
            {
                m_Errors.Add(versions?.primary.isInstalled == true ? UIError.k_EntitlementError : UIError.k_EntitlementWarning);
                return;
            }

            m_Errors.Add(error);
        }

        public void ClearErrors(Predicate<UIError> match = null)
        {
            if (match == null)
                m_Errors.Clear();
            else
                m_Errors.RemoveAll(match);
        }

        [SerializeField]
        protected PackageType m_Type;
        public bool Is(PackageType type)
        {
            return (m_Type & type) != 0;
        }

        [SerializeField]
        protected long m_FirstPublishedDateTicks;
        public DateTime? firstPublishedDate => m_FirstPublishedDateTicks == 0 ? null : (DateTime?)new DateTime(m_FirstPublishedDateTicks, DateTimeKind.Utc);

        public virtual bool isDiscoverable => true;

        public virtual IEnumerable<PackageImage> images => Enumerable.Empty<PackageImage>();
        public virtual IEnumerable<PackageLink> links => Enumerable.Empty<PackageLink>();

        public virtual DateTime? purchasedTime => null;
        public virtual IEnumerable<string> labels => Enumerable.Empty<string>();

        public abstract string uniqueId { get; }
        public abstract IVersionList versions { get; }

        IEnumerable<UI.IPackageVersion> UI.IPackage.versions => versions.Cast<UI.IPackageVersion>();

        public abstract IPackage Clone();

        protected virtual string descriptor => L10n.Tr("package");

        public string GetDescriptor(bool isFirstLetterCapitalized = false)
        {
            return isFirstLetterCapitalized ? descriptor.First().ToString().ToUpper() + descriptor.Substring(1) : descriptor;
        }
    }
}
