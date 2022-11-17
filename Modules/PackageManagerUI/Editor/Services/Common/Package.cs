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
        public bool isDiscoverable => m_IsDiscoverable;

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
                    ++numErrors;
                    if (error.HasAttribute(UIError.Attribute.IsWarning))
                        ++numWarnings;
                    else if (!error.HasAttribute(UIError.Attribute.IsClearable))
                        return PackageState.Error;
                }

                var primary = versions.primary;
                if (primary.HasTag(PackageTag.Deprecated) && primary.isInstalled)
                    return PackageState.Error;

                if (numErrors > 0 && numWarnings == numErrors || isDeprecated)
                    return PackageState.Warning;

                var recommended = versions.recommended;
                var latestKeyVersion = versions.key.LastOrDefault();
                if (primary.HasTag(PackageTag.Custom))
                    return PackageState.InDevelopment;

                if (primary.isInstalled && !primary.isDirectDependency)
                    return PackageState.InstalledAsDependency;

                if (primary != recommended && ((primary.isInstalled && primary != latestKeyVersion) || primary.HasTag(PackageTag.LegacyFormat)) && !primary.HasTag(PackageTag.Local))
                    return PackageState.UpdateAvailable;

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

        public bool hasEntitlements => versions.Any(version => version.HasTag(PackageTag.Unity) && version.hasEntitlements);

        public bool hasEntitlementsError => m_Errors.Any(error => error.errorCode == UIErrorCode.UpmError_Forbidden) || versions.Any(version => version.hasEntitlementsError);

        private IVersionList m_VersionList;
        public IVersionList versions => m_VersionList;

        [SerializeField]
        private AssetStoreVersionList m_SerializedAssetStoreVersionList;
        [SerializeField]
        private UpmVersionList m_SerializedUpmVersionList;
        [SerializeField]
        private PlaceholderVersionList m_SerializedPlaceholderVersionList;

        IEnumerable<UI.IPackageVersion> UI.IPackage.versions => versions.Cast<UI.IPackageVersion>();

        private void LinkPackageAndVersions()
        {
            foreach (var version in versions)
                version.package = this;
        }

        public void OnBeforeSerialize()
        {
            if (m_VersionList is UpmVersionList)
                m_SerializedUpmVersionList = m_VersionList as UpmVersionList;
            else if (m_VersionList is AssetStoreVersionList)
                m_SerializedAssetStoreVersionList = m_VersionList as AssetStoreVersionList;
            else
                m_SerializedPlaceholderVersionList = m_VersionList as PlaceholderVersionList;
        }

        public void OnAfterDeserialize()
        {
            if (m_SerializedPlaceholderVersionList?.Any() == true)
                m_VersionList = m_SerializedPlaceholderVersionList;
            else if (string.IsNullOrEmpty(name))
                m_VersionList = m_SerializedAssetStoreVersionList;
            else
                m_VersionList = m_SerializedUpmVersionList;
            LinkPackageAndVersions();
        }

        private Package(string name, IVersionList versionList, Product product = null, bool isDiscoverable = true, bool isDeprecated = false, string deprecationMessage = null)
        {
            m_Name = name;
            m_VersionList = versionList;
            m_Product = product;

            m_UniqueId = m_Product?.id > 0 ? product.id.ToString() : name;

            m_IsDiscoverable = isDiscoverable;
            m_Errors = new List<UIError>();
            m_Progress = PackageProgress.None;

            m_IsDeprecated = isDeprecated;
            m_DeprecationMessage = deprecationMessage;

            LinkPackageAndVersions();
        }

        public virtual bool IsInTab(PackageFilterTab tab)
        {
            var version = versions.primary;
            switch (tab)
            {
                case PackageFilterTab.BuiltIn:
                    return version.HasTag(PackageTag.BuiltIn);
                case PackageFilterTab.UnityRegistry:
                    return !version.HasTag(PackageTag.BuiltIn) && version.HasTag(PackageTag.UpmFormat) && version.HasTag(PackageTag.Unity) && (isDiscoverable || (versions.installed?.isDirectDependency ?? false));
                case PackageFilterTab.MyRegistries:
                    return version.HasTag(PackageTag.UpmFormat) && version.HasTag(PackageTag.ScopedRegistry | PackageTag.MainNotUnity) && (isDiscoverable || (versions.installed?.isDirectDependency ?? false));
                case PackageFilterTab.InProject:
                    return !version.HasTag(PackageTag.BuiltIn) && (progress == PackageProgress.Installing || versions.installed != null);
                case PackageFilterTab.AssetStore:
                    return product != null;
                default:
                    return false;
            }
        }

        // We are making package factories inherit from a sub class of Package to make sure that we can keep all the package modifying code
        // private and that only Packages themselves and factories can actually modify packages. This way there won't be any accidental
        // package modifications that's not caught by the package change events.
        internal class Factory
        {
            public Package CreatePackage(string name, IVersionList versionList, Product product = null, bool isDiscoverable = true, bool isDeprecated = false, string deprecationMessage = null)
            {
                return new Package(name, versionList, product, isDiscoverable, isDeprecated, deprecationMessage);
            }

            public void AddError(Package package, UIError error)
            {
                if (error.errorCode == UIErrorCode.UpmError_Forbidden)
                {
                    package.m_Errors.Add(package.versions?.primary.isInstalled == true ? UIError.k_EntitlementError : UIError.k_EntitlementWarning);
                    return;
                }
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
        }
    }
}
