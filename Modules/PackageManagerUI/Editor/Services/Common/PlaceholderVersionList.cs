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
    internal class PlaceholderVersionList : BaseVersionList
    {
        [SerializeField]
        private PlaceholderPackageVersion[] m_Versions;

        public override IPackageVersion latest => m_Versions[0];

        public override IPackageVersion recommended => m_Versions[0];

        public override IPackageVersion primary => m_Versions[0];

        public PlaceholderVersionList(PlaceholderPackageVersion version)
        {
            m_Versions = new[] { version };
        }

        public override IEnumerator<IPackageVersion> GetEnumerator()
        {
            return m_Versions.Cast<IPackageVersion>().GetEnumerator();
        }
    }
}
