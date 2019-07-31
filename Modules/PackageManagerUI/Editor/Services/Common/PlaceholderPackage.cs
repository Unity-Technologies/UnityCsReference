// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PlaceholderPackage : IPackage
    {
        [SerializeField]
        private string m_UniqueId;

        public string name => string.Empty;
        public string uniqueId => m_UniqueId;

        public string displayName => m_Version.displayName;

        [SerializeField]
        private PlaceholderPackageVersion m_Version;

        public IEnumerable<IPackageVersion> versions => new[] { m_Version };
        public IEnumerable<IPackageVersion> keyVersions => new[] { m_Version };
        public IPackageVersion installedVersion => null;
        public IPackageVersion latestVersion => m_Version;
        public IPackageVersion latestPatch => m_Version;
        public IPackageVersion recommendedVersion => m_Version;
        public IPackageVersion primaryVersion => m_Version;

        [SerializeField]
        private PackageState m_State;
        public PackageState state => m_State;

        public bool isDiscoverable => true;

        public IEnumerable<Error> errors => Enumerable.Empty<Error>();

        public void AddError(Error error)
        {
        }

        public void ClearErrors()
        {
        }

        public PlaceholderPackage(string uniqueId, PackageTag tag = PackageTag.None, PackageSource source = PackageSource.Unknown, PackageState state = PackageState.InProgress)
        {
            m_UniqueId = uniqueId;
            m_State = state;
            m_Version = new PlaceholderPackageVersion(uniqueId, uniqueId, tag, source);
        }

        public IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
