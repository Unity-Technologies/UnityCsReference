// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Serialization;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class PackageCompliance
    {
        [SerializeField]
        [NativeName("status")]
        private PackageComplianceStatus m_Status;

        [SerializeField]
        [NativeName("violation")]
        private Violation m_Violation;

        internal PackageCompliance() : this(PackageComplianceStatus.Compliant, null) {}

        internal PackageCompliance(PackageComplianceStatus status = PackageComplianceStatus.Compliant, Violation violation = null)
        {
            m_Status = status;
            m_Violation = violation;
        }

        internal PackageComplianceStatus status { get { return m_Status; } }
        internal Violation violation { get { return m_Violation; } }
    }
}
