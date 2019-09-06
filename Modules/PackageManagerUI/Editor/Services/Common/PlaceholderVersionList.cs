// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PlaceholderVersionList : IVersionList
    {
        private PlaceholderPackageVersion[] m_Versions;

        public IEnumerable<IPackageVersion> all => m_Versions;

        public IEnumerable<IPackageVersion> key => m_Versions;

        public IPackageVersion installed => null;

        public IPackageVersion latest => m_Versions[0];

        public IPackageVersion latestPatch => m_Versions[0];

        public IPackageVersion importAvailable => null;

        public IPackageVersion recommended => m_Versions[0];

        public IPackageVersion primary => m_Versions[0];

        public PlaceholderVersionList(PlaceholderPackageVersion version)
        {
            m_Versions = new[] { version };
        }
    }
}
