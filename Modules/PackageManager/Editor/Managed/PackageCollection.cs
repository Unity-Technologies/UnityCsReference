// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager
{
    [Serializable]
    public class PackageCollection : IEnumerable<UpmPackageInfo>
    {
        [SerializeField]
        private UpmPackageInfo[] m_PackageList;

        private PackageCollection() {}

        internal PackageCollection(IEnumerable<UpmPackageInfo> packages)
        {
            m_PackageList = (packages ?? new UpmPackageInfo[] {}).ToArray();
        }

        IEnumerator<UpmPackageInfo> IEnumerable<UpmPackageInfo>.GetEnumerator()
        {
            return ((IEnumerable<UpmPackageInfo>)m_PackageList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_PackageList.GetEnumerator();
        }
    }
}

