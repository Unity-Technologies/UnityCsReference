// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreVersionList : IVersionList
    {
        [SerializeField]
        private List<AssetStorePackageVersion> m_Versions;

        public IEnumerable<IPackageVersion> key => m_Versions.Cast<IPackageVersion>();

        public IPackageVersion installed => null;

        public IPackageVersion latest => m_Versions.LastOrDefault();

        public IPackageVersion latestPatch => latest;

        public IPackageVersion importAvailable => m_Versions.FirstOrDefault(v => v.isAvailableOnDisk);

        public IPackageVersion recommended => latest;

        public IPackageVersion primary => importAvailable ?? latest;

        public AssetStoreVersionList(AssetStoreUtils assetStoreUtils, IOProxy ioProxy)
        {
            ResolveDependencies(assetStoreUtils, ioProxy);

            m_Versions = new List<AssetStorePackageVersion>();
        }

        public void ResolveDependencies(AssetStoreUtils assetStoreUtils, IOProxy ioProxy)
        {
            if (m_Versions == null)
                return;
            foreach (var version in m_Versions)
                version.ResolveDependencies(assetStoreUtils, ioProxy);
        }

        public void AddVersion(AssetStorePackageVersion version)
        {
            m_Versions.Add(version);
        }

        public void RemoveVersion(AssetStorePackageVersion version)
        {
            m_Versions.Remove(version);
        }

        public IEnumerator<IPackageVersion> GetEnumerator()
        {
            return m_Versions.Cast<IPackageVersion>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Versions.GetEnumerator();
        }
    }
}
