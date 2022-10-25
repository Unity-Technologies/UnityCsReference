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
    internal class PlaceholderVersionList : IVersionList
    {
        [SerializeField]
        private PlaceholderPackageVersion[] m_Versions;

        public IEnumerable<IPackageVersion> key => m_Versions;

        public IPackageVersion installed => null;

        public IPackageVersion latest => m_Versions[0];

        public IPackageVersion latestPatch => m_Versions[0];

        public IPackageVersion importAvailable => null;

        public IPackageVersion recommended => m_Versions[0];

        public IPackageVersion primary => m_Versions[0];

        public IPackageVersion lifecycleVersion => null;

        public bool isNonLifecycleVersionInstalled => false;

        public bool hasLifecycleVersion => false;

        public int numUnloadedVersions => 0;

        public PlaceholderVersionList(PlaceholderPackageVersion version)
        {
            m_Versions = new[] { version };
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
