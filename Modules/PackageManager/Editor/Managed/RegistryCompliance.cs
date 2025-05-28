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
    internal class RegistryCompliance
    {
        [SerializeField]
        [NativeName("status")]
        private RegistryComplianceStatus m_Status = RegistryComplianceStatus.Compliant;

        [SerializeField]
        [NativeName("violations")]
        private Violation[] m_Violations = Array.Empty<Violation>();

        internal RegistryCompliance() : this(RegistryComplianceStatus.Compliant, null) {}

        internal RegistryCompliance(RegistryComplianceStatus status = RegistryComplianceStatus.Compliant, Violation[] violations = null)
        {
            m_Status = status;
            m_Violations = violations ?? Array.Empty<Violation>();
        }

        internal RegistryComplianceStatus status { get { return m_Status; } }
        internal Violation[] violations { get { return m_Violations; } }
    }
}
