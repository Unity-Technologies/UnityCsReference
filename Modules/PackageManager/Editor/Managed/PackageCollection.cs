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

        [SerializeField]
        private Error m_Error;

        /// <summary>
        /// This allows <see cref="error" /> to return null
        /// after de-serialization
        /// </summary>
        [SerializeField]
        private bool m_HasError;

        private PackageCollection() {}

        internal PackageCollection(
            IEnumerable<UpmPackageInfo> packages,
            Error error)
        {
            m_PackageList = (packages ?? new UpmPackageInfo[] {}).ToArray();
            m_Error = error;
            m_HasError = (m_Error != null);
        }

        IEnumerator<UpmPackageInfo> IEnumerable<UpmPackageInfo>.GetEnumerator()
        {
            return ((IEnumerable<UpmPackageInfo>)m_PackageList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_PackageList.GetEnumerator();
        }

        public Error error { get { return m_HasError ? m_Error : null; } }
    }
}

