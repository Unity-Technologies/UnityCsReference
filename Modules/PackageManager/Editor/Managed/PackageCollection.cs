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
    public class PackageCollection : IEnumerable<PackageInfo>
    {
        [SerializeField]
        private PackageInfo[] m_PackageList;

        private PackageCollection() {}

        internal PackageCollection(IEnumerable<PackageInfo> packages)
        {
            m_PackageList = (packages ?? new PackageInfo[] {}).ToArray();
        }

        IEnumerator<PackageInfo> IEnumerable<PackageInfo>.GetEnumerator()
        {
            return ((IEnumerable<PackageInfo>)m_PackageList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_PackageList.GetEnumerator();
        }
    }
}

