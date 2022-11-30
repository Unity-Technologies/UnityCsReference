// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreImportedPackage : IEnumerable<Asset>
    {
        public long productId => latestAssetOrigin?.productId ?? 0;
        public string displayName => latestAssetOrigin?.packageName ?? "";
        public string versionString => latestAssetOrigin?.packageVersion ?? "";
        public long uploadId => latestAssetOrigin?.uploadId ?? 0;

        // Used in UI. Highest/latest asset origin found amongst importedAssets. This is set
        // as part of the List function
        [SerializeField]
        private AssetOrigin m_LatestAssetOrigin;
        public AssetOrigin latestAssetOrigin
        {
            get
            {
                if (m_LatestAssetOrigin == null)
                    CalculateLatestAssetOrigin();
                return m_LatestAssetOrigin;
            }
        }

        [SerializeField]
        private List<Asset> m_ImportedAssets;

        public AssetStoreImportedPackage(List<Asset> importedAssets)
        {
            m_ImportedAssets = importedAssets ?? new List<Asset>();
            CalculateLatestAssetOrigin();
        }

        private void CalculateLatestAssetOrigin()
        {
            m_LatestAssetOrigin = m_ImportedAssets.OrderByDescending(x => x.origin.uploadId).FirstOrDefault().origin;
        }

        public void AddImportedAsset(Asset importedAsset)
        {
            if (importedAsset.origin == null)
                return;

            m_ImportedAssets.Add(importedAsset);
            if (importedAsset.origin.uploadId > m_LatestAssetOrigin.uploadId)
                m_LatestAssetOrigin = importedAsset.origin;
        }

        public IEnumerator<Asset> GetEnumerator()
        {
            return m_ImportedAssets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_ImportedAssets.GetEnumerator();
        }
    }
}
