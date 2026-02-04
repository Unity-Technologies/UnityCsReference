// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreImportedPackage : IReadOnlyCollection<Asset>
    {
        public long productId => latestAssetOrigin?.productId ?? 0;
        public string displayName => latestAssetOrigin?.packageName ?? "";
        public string versionString => latestAssetOrigin?.packageVersion ?? "";
        public long uploadId => latestAssetOrigin?.uploadId ?? 0;

        // Used in UI. Highest/latest asset origin found amongst importedAssets. This is set
        // as part of the List function
        [SerializeField]
        private AssetOrigin m_LatestAssetOrigin;

        public AssetOrigin latestAssetOrigin => m_LatestAssetOrigin;

        public int Count => m_ImportedAssets.Count;

        [SerializeField]
        private List<Asset> m_ImportedAssets;

        public AssetStoreImportedPackage(params Asset[] importedAssets)
        {
            m_ImportedAssets = new List<Asset>();
            m_LatestAssetOrigin = null;

            foreach (var asset in importedAssets)
                AddImportedAsset(asset);
        }

        public void AddImportedAsset(Asset importedAsset)
        {
            if (importedAsset.origin == null)
                return;

            m_ImportedAssets.Add(importedAsset);
            if (m_LatestAssetOrigin == null || importedAsset.origin.uploadId > m_LatestAssetOrigin.uploadId)
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
