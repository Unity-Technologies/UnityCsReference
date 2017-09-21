// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    class OutdatedPackage
    {
        [SerializeField]
        private UpmPackageInfo m_Current;
        [SerializeField]
        private UpmPackageInfo m_Latest;

        private OutdatedPackage() {}

        public OutdatedPackage(UpmPackageInfo current, UpmPackageInfo latest)
        {
            m_Current = current;
            m_Latest = latest;
        }

        public UpmPackageInfo current { get { return m_Current;  } }
        public UpmPackageInfo latest { get { return m_Latest;  } }
    }
}

