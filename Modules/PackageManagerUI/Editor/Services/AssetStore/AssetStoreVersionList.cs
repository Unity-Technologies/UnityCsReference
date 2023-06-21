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
    internal class AssetStoreVersionList : BaseVersionList
    {
        [SerializeField]
        private List<AssetStorePackageVersion> m_Versions;

        public override IPackageVersion latest => m_Versions.Count > 0 ? m_Versions[^1] : null;

        [SerializeField]
        private int m_ImportAvailableIndex;
        public override IPackageVersion importAvailable => m_ImportAvailableIndex < 0 ? null : m_Versions[m_ImportAvailableIndex];

        [SerializeField]
        private int m_ImportedIndex;
        public override IPackageVersion imported => m_ImportedIndex < 0 ? null : m_Versions[m_ImportedIndex];

        [SerializeField]
        private int m_RecommendedIndex = -1;
        public override IPackageVersion recommended => m_RecommendedIndex < 0 ? null : m_Versions[m_RecommendedIndex];

        public override IPackageVersion primary => imported ?? importAvailable ?? latest;

        public AssetStoreVersionList(AssetStoreProductInfo productInfo, AssetStoreLocalInfo localInfo = null, AssetStoreImportedPackage importedPackage = null, AssetStoreUpdateInfo updateInfo = null)
        {
            m_Versions = new List<AssetStorePackageVersion>();

            CreateAndAddToSortedVersions(productInfo, localInfo, importedPackage, localInfo?.uploadId);
            CreateAndAddToSortedVersions(productInfo, localInfo, importedPackage, importedPackage?.uploadId);
            CreateAndAddToSortedVersions(productInfo, localInfo, importedPackage, updateInfo?.recommendedUploadId);

            if (m_Versions.Count == 0)
                m_Versions.Add(new AssetStorePackageVersion(productInfo));

            m_ImportAvailableIndex = localInfo == null ? -1 : m_Versions.FindIndex(v => v.uploadId == localInfo.uploadId);
            m_ImportedIndex = importedPackage == null ? -1 : m_Versions.FindIndex(v => v.uploadId == importedPackage.uploadId);
            m_RecommendedIndex = updateInfo == null ? -1 : m_Versions.FindIndex(v => v.uploadId == updateInfo.recommendedUploadId);
        }

        private void CreateAndAddToSortedVersions(AssetStoreProductInfo productInfo, AssetStoreLocalInfo localInfo, AssetStoreImportedPackage importedPackage, long? uploadId)
        {
            if (uploadId == null)
                return;

            var insertIndex = m_Versions.Count;
            for (var i = 0; i < m_Versions.Count; i++)
            {
                var version = m_Versions[i];
                // We need to check duplicates here because it's possible that for localInfo, importedPackage and updateInfo to have the same uploadId
                if (version.uploadId == uploadId)
                    return;

                if (version.uploadId < uploadId)
                    continue;

                insertIndex = i;
                break;
            }

            m_Versions.Insert(insertIndex, new AssetStorePackageVersion
            (
                productInfo,
                uploadId.Value,
                localInfo: uploadId == localInfo?.uploadId ? localInfo : null,
                importedPackage: uploadId == importedPackage?.uploadId ? importedPackage : null
            ));
        }

        public override IEnumerator<IPackageVersion> GetEnumerator()
        {
            return m_Versions.Cast<IPackageVersion>().GetEnumerator();
        }
    }
}
