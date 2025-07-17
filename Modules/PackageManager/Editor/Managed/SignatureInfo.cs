// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class SignatureInfo
    {
        [SerializeField]
        [NativeName("status")]
        private SignatureStatus m_Status;

        [SerializeField]
        [NativeName("reason")]
        private string m_Reason;

        [SerializeField]
        [NativeName("attestation")]
        private Attestation m_Attestation;

        public SignatureStatus status => m_Status;
        public string reason => m_Reason;
        public Attestation attestation => m_Attestation;

        internal SignatureInfo() : this(SignatureStatus.Unchecked, "", null) {}

        internal SignatureInfo(SignatureStatus status, string reason, Attestation attestation)
        {
            m_Status = status;
            m_Reason = reason;
            m_Attestation = attestation;
        }
    }
}
