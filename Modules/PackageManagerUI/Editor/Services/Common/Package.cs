// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class Package : IPackage, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_Name;
        public string name => m_Name;

        [SerializeField]
        private Product m_Product;
        public IProduct product => m_Product?.id > 0 ? m_Product : null;

        [SerializeField]
        private string m_UniqueId;
        public string uniqueId => m_UniqueId;

        [SerializeField]
        private bool m_IsDiscoverable;
        public bool isDiscoverable => m_IsDiscoverable || versions.installed?.isDirectDependency == true;

        public string displayName => !string.IsNullOrEmpty(m_Product?.displayName) ? m_Product?.displayName : versions.FirstOrDefault()?.displayName ?? string.Empty;
        public PackageState state
        {
            get
            {
                if (progress != PackageProgress.None)
                    return PackageState.InProgress;

                var numErrors = 0;
                var numWarnings = 0;
                foreach (var error in GetAllErrorsInPackageAndVersions())
                {
                    // We don't count HiddenFromUI errors when calculating the package state because the state is tightly coupled to the UI.
                    // The icon to display in the package list is directly mapped to the package state using a USS class name.
                    // For that reason, we need to disregard HiddenFromUI errors to avoid showing the error or warning icons.
                    if (error.HasAttribute(UIError.Attribute.HiddenFromUI))
                        continue;

                    ++numErrors;
                    if (error.HasAttribute(UIError.Attribute.Warning))
                        ++numWarnings;
                    else if (!error.HasAttribute(UIError.Attribute.Clearable))
                        return PackageState.Error;
                }

                var primary = versions.primary;
                if (primary.HasTag(PackageTag.Deprecated) && primary.isInstalled)
                    return PackageState.Error;

                if (numErrors > 0 && numWarnings == numErrors || isDeprecated)
                    return PackageState.Warning;

                if (primary.HasTag(PackageTag.Custom))
                    return PackageState.InDevelopment;

                if (isLocked)
                    return PackageState.Locked;

                if (primary.isInstalled && !primary.isDirectDependency)
                    return PackageState.InstalledAsDependency;

                var recommended = versions.recommended;
                var latestKeyVersion = versions.key.LastOrDefault();
                if (recommended != null && primary != recommended && ((primary.isInstalled && primary != latestKeyVersion) || primary.HasTag(PackageTag.LegacyFormat)) && !primary.HasTag(PackageTag.Local))
                    return PackageState.UpdateAvailable;

                if (primary.importedAssets?.Any() == true)
                    return PackageState.Imported;

                if (versions.importAvailable != null)
                    return PackageState.ImportAvailable;

                if (versions.installed != null)
                    return PackageState.Installed;

                if (primary.HasTag(PackageTag.LegacyFormat))
                    return PackageState.DownloadAvailable;

                return PackageState.None;
            }
        }

        [SerializeField]
        private PackageProgress m_Progress;
        public virtual PackageProgress progress => m_Progress;

        [SerializeField]
        private string m_DeprecationMessage;
        public virtual string deprecationMessage => m_DeprecationMessage;

        [SerializeField]
        private bool m_IsDeprecated;
        public virtual bool isDeprecated => m_IsDeprecated;

        [SerializeField]
        private bool m_IsLocked;
        public virtual bool isLocked => m_IsLocked;

        // errors on the package level (not just about a particular version)
        [SerializeField]
        private List<UIError> m_Errors;
        public IEnumerable<UIError> errors => m_Errors;

        private IEnumerable<UIError> GetAllErrorsInPackageAndVersions()
        {
            if (m_Errors != null)
                foreach (var error in m_Errors)
                    yield return error;
            foreach (var version in versions.Where(v => v.errors != null))
                foreach (var versionError in version.errors)
                    yield return versionError;
        }

        public bool hasEntitlements => versions.Any(v => v.HasTag(PackageTag.Unity) && v.hasEntitlements);

        public bool hasEntitlementsError => versions.Any(v => v.hasEntitlementsError);

        [SerializeReference]
        private IVersionList m_VersionList;
        public IVersionList versions => m_VersionList;

        IEnumerable<UI.IPackageVersion> UI.IPackage.versions => versions;

        private void LinkPackageAndVersions()
        {
            foreach (var version in versions)
                version.package = this;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            LinkPackageAndVersions();
        }

        private Package(string name, IVersionList versionList, Product product = null, bool isDiscoverable = true, bool isDeprecated = false, string deprecationMessage = null, bool isLocked = false)
        {
            m_Name = name;
            m_VersionList = versionList;
            m_Product = product;

            m_UniqueId = m_Product?.id > 0 ? product.id.ToString() : name;

            m_IsDiscoverable = isDiscoverable;
            m_Errors = new List<UIError>();
            m_Progress = PackageProgress.None;

            m_IsDeprecated = versionList.primary?.HasTag(PackageTag.InstalledFromPath) == false && isDeprecated;
            m_DeprecationMessage = deprecationMessage;

            m_IsLocked = isLocked;

            LinkPackageAndVersions();
        }

        // We are making package factories inherit from a sub class of Package to make sure that we can keep all the package modifying code
        // private and that only Packages themselves and factories can actually modify packages. This way there won't be any accidental
        // package modifications that's not caught by the package change events.
        internal class Factory : BaseService
        {
            public Package CreatePackage(string name, IVersionList versionList, Product product = null, bool isDiscoverable = true, bool isDeprecated = false, string deprecationMessage = null, bool isLocked = false)
            {
                return new Package(name, versionList, product, isDiscoverable, isDeprecated, deprecationMessage, isLocked);
            }

            public void AddError(Package package, UIError error)
            {
                package.m_Errors.Add(error);
            }

            public int ClearErrors(Package package, Predicate<UIError> match = null)
            {
                if (match != null)
                    return package.m_Errors.RemoveAll(match);
                var numErrors = package.m_Errors.Count;
                package.m_Errors.Clear();
                return numErrors;
            }

            public void SetProgress(Package package, PackageProgress progress)
            {
                package.m_Progress = progress;
            }

            public override Type registrationType => null;
        }
    }
}
