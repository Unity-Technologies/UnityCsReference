// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    [Serializable]
    internal class AssetStorePackage : IPackage
    {
        [SerializeField]
        private string m_Name;
        public string name => m_Name;

        [SerializeField]
        private string m_ProductId;
        public string uniqueId => m_ProductId;

        public string displayName => versions.FirstOrDefault()?.displayName;

        [SerializeField]
        private AssetStoreVersionList m_VersionList;

        [SerializeField]
        private UpmVersionList m_UpmVersionList;

        public IVersionList versionList => string.IsNullOrEmpty(name) ? m_VersionList as IVersionList : m_UpmVersionList as IVersionList;

        [SerializeField]
        private PackageProgress m_Progress;
        public PackageProgress progress
        {
            get { return m_Progress; }
            set { m_Progress = value; }
        }

        public PackageState state => PackageExtensions.GetState(this);

        public bool isDiscoverable => true;

        [SerializeField]
        private List<Error> m_Errors;
        // Combined errors for this package or any version.
        // Stop lookup after first error encountered on a version to save time not looking up redundant errors.
        public IEnumerable<Error> errors => (versions.Select(v => v.errors).FirstOrDefault(e => e?.Any() ?? false) ?? new List<Error>()).Concat(m_Errors);

        [SerializeField]
        private PackageType m_Type;
        public bool Is(PackageType type)
        {
            return (m_Type & type) != 0;
        }

        [SerializeField]
        private List<PackageImage> m_Images;
        [SerializeField]
        private List<PackageLink> m_Links;

        public IEnumerable<PackageImage> images => m_Images;

        public IEnumerable<PackageLink> links => m_Links;

        public IEnumerable<IPackageVersion> versions => versionList?.all;

        public IEnumerable<IPackageVersion> keyVersions => versionList?.key;

        public IPackageVersion installedVersion => versionList?.installed;

        public IPackageVersion latestVersion => versionList?.latest;

        public IPackageVersion latestPatch => versionList?.latestPatch;

        public IPackageVersion recommendedVersion => versionList?.recommended;

        public IPackageVersion primaryVersion => versionList?.primary;

        public void AddError(Error error)
        {
            m_Errors?.Add(error);
        }

        public void ClearErrors()
        {
            m_Errors?.Clear();
        }

        public AssetStorePackage(string productId, Error error)
        {
            m_Errors = new List<Error> { error };
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = string.Empty;
            m_ProductId = productId;

            m_Images = new List<PackageImage>();
            m_Links = new List<PackageLink>();
            m_VersionList = new AssetStoreVersionList();
            m_UpmVersionList = new UpmVersionList();
        }

        public AssetStorePackage(FetchedInfo fetchedInfo, LocalInfo localInfo = null)
        {
            m_Errors = new List<Error>();
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = string.Empty;
            m_ProductId = fetchedInfo?.id.ToString();
            m_Images = fetchedInfo?.images ?? new List<PackageImage>();
            m_Links = fetchedInfo?.links ?? new List<PackageLink>();
            m_VersionList = new AssetStoreVersionList();
            m_UpmVersionList = new UpmVersionList();

            if (string.IsNullOrEmpty(fetchedInfo?.id) || string.IsNullOrEmpty(fetchedInfo?.versionId))
            {
                AddError(new Error(NativeErrorCode.Unknown, "Invalid product details."));
            }
            else if (localInfo == null)
            {
                m_VersionList.AddVersion(new AssetStorePackageVersion(fetchedInfo));
            }
            else
            {
                m_VersionList.AddVersion(new AssetStorePackageVersion(fetchedInfo, localInfo));
                if (localInfo.canUpdate && (localInfo.versionId != fetchedInfo.versionId || localInfo.versionString != fetchedInfo.versionString))
                    m_VersionList.AddVersion(new AssetStorePackageVersion(fetchedInfo));
            }
        }

        public AssetStorePackage(FetchedInfo fetchedInfo, UpmPackage package)
        {
            m_Errors = new List<Error>();
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = package?.name ?? string.Empty;
            m_ProductId = fetchedInfo?.id.ToString();

            m_Images = fetchedInfo?.images ?? new List<PackageImage>();
            m_Links = fetchedInfo?.links ?? new List<PackageLink>();
            m_VersionList = new AssetStoreVersionList();

            m_UpmVersionList = package?.versionList as UpmVersionList ?? new UpmVersionList();
            foreach (var version in m_UpmVersionList.all.Cast<UpmPackageVersion>())
                version.UpdateFetchedInfo(fetchedInfo);

            if (string.IsNullOrEmpty(fetchedInfo?.id) || string.IsNullOrEmpty(fetchedInfo?.versionId))
                AddError(new Error(NativeErrorCode.Unknown, "Invalid product details."));
            else if (string.IsNullOrEmpty(package?.name))
                AddError(new Error(NativeErrorCode.Unknown, "Invalid package info."));
        }

        public IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
