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
        private string m_ProductId;

        public string name => string.Empty;
        public string uniqueId => m_ProductId;

        public string displayName => m_Versions.FirstOrDefault()?.displayName;

        [SerializeField]
        private List<AssetStorePackageVersion> m_Versions;

        internal AssetStorePackageVersion m_InstalledVersion => m_Versions.FirstOrDefault(pv => pv.isAvailableOnDisk);
        internal AssetStorePackageVersion m_FetchedVersion => m_Versions.FirstOrDefault();
        internal AssetStorePackageVersion m_LocalVersion => m_Versions.LastOrDefault();

        public IEnumerable<IPackageVersion> versions => m_Versions.Cast<IPackageVersion>();
        public IEnumerable<IPackageVersion> keyVersions => m_Versions.Cast<IPackageVersion>();
        public IPackageVersion installedVersion => m_InstalledVersion;
        public IPackageVersion latestVersion => m_FetchedVersion;
        public IPackageVersion latestPatch => latestVersion;
        public IPackageVersion recommendedVersion => latestVersion;
        public IPackageVersion primaryVersion => installedVersion ?? latestVersion;

        [SerializeField]
        private PackageState m_State;
        public PackageState state => m_State;

        public void SetState(PackageState state)
        {
            m_State = state;
        }

        public bool isDiscoverable => true;

        [SerializeField]
        private List<Error> m_Errors;
        public IEnumerable<Error> errors => m_Errors;

        public void AddError(Error error)
        {
            m_Errors?.Add(error);
        }

        public void ClearErrors()
        {
            m_Errors?.Clear();
        }

        public void AddVersion(AssetStorePackageVersion version)
        {
            m_Versions.Add(version);
        }

        public void RemoveVersion(AssetStorePackageVersion version)
        {
            m_Versions.Remove(version);
        }

        public AssetStorePackage(string productId, Error error)
        {
            m_Errors = new List<Error> { error };
            m_State = PackageState.Error;
            m_ProductId = productId;
            m_Versions = new List<AssetStorePackageVersion>();
        }

        public AssetStorePackage(string productId, IDictionary<string, object> productDetail)
        {
            m_Errors = new List<Error>();
            m_State = PackageState.UpToDate;
            m_ProductId = productId;
            m_Versions = new List<AssetStorePackageVersion>();
            try
            {
                m_Versions.Add(new AssetStorePackageVersion(productId, productDetail));
            }
            catch (Exception e)
            {
                m_Errors.Add(new Error(NativeErrorCode.Unknown, e.Message));
                m_State = PackageState.Error;
            }
        }

        public IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
