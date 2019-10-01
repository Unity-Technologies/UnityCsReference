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
    internal class AssetStorePackage : BasePackage
    {
        [SerializeField]
        private string m_ProductId;
        public override string uniqueId => m_ProductId;

        [SerializeField]
        private AssetStoreVersionList m_VersionList;

        [SerializeField]
        private UpmVersionList m_UpmVersionList;

        public override IVersionList versions => string.IsNullOrEmpty(name) ? m_VersionList as IVersionList : m_UpmVersionList as IVersionList;

        [SerializeField]
        private List<PackageImage> m_Images;
        [SerializeField]
        private List<PackageLink> m_Links;

        public override IEnumerable<PackageImage> images => m_Images;

        public override IEnumerable<PackageLink> links => m_Links;

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

            m_UpmVersionList = package?.versions as UpmVersionList ?? new UpmVersionList();
            foreach (var version in m_UpmVersionList.Cast<UpmPackageVersion>())
                version.UpdateFetchedInfo(fetchedInfo);

            if (string.IsNullOrEmpty(fetchedInfo?.id) || string.IsNullOrEmpty(fetchedInfo?.versionId))
                AddError(new Error(NativeErrorCode.Unknown, "Invalid product details."));
            else if (string.IsNullOrEmpty(package?.name))
                AddError(new Error(NativeErrorCode.Unknown, "Invalid package info."));
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
