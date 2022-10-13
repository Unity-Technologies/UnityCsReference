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
    internal class AssetStoreVersionList : IVersionList
    {
        [SerializeField]
        private List<AssetStorePackageVersion> m_Versions;

        public IEnumerable<IPackageVersion> key => m_Versions.Cast<IPackageVersion>();

        public IPackageVersion installed => null;

        public IPackageVersion latest => m_Versions.LastOrDefault();

        public IPackageVersion importAvailable => m_Versions.FirstOrDefault(v => v.isAvailableOnDisk);

        public IPackageVersion recommended => latest;

        public IPackageVersion primary => importAvailable ?? latest;

        public IPackageVersion lifecycleVersion => null;

        public bool isNonLifecycleVersionInstalled => false;

        public bool hasLifecycleVersion => false;

        public int numUnloadedVersions => 0;

        public IPackageVersion GetUpdateTarget(IPackageVersion version)
        {
            return recommended;
        }

        public AssetStoreVersionList()
        {
            m_Versions = new List<AssetStorePackageVersion>();
        }

        public AssetStoreVersionList(IOProxy ioProxy, AssetStoreProductInfo productInfo, AssetStoreLocalInfo localInfo = null, AssetStoreUpdateInfo updateInfo = null)
        {
            m_Versions = new List<AssetStorePackageVersion>();

            if (productInfo == null || productInfo.productId <= 0 || productInfo.versionId <= 0)
                return;

            // The version we get from the product info the latest on the server
            // The version we get from the localInfo is the version publisher set when uploading the .unitypackage file
            // The publisher could update the version on the server but NOT upload a new .unitypackage file, that will
            // result in a case where localInfo and productInfo have different version numbers but no update is available
            // Because of this, we prefer showing version from the server (even when localInfo version is different)
            // and we only want to show the localInfo version when `localInfo.canUpdate` is set to true
            var latestVersion = new AssetStorePackageVersion(ioProxy, productInfo);
            if (localInfo != null)
            {
                if (updateInfo?.canUpdate == true)
                    m_Versions.Add(new AssetStorePackageVersion(ioProxy, productInfo, localInfo));
                else
                {
                    latestVersion.SetLocalPath(ioProxy, localInfo.packagePath);
                    latestVersion.AddDowngradeWarningIfApplicable(localInfo, updateInfo);
                }
            }
            m_Versions.Add(latestVersion);
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
