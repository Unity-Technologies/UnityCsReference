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
        [NativeName("isLegacy")]
        private bool m_IsLegacy;

        [SerializeField]
        [NativeName("errorCode")]
        private string m_ErrorCode;

        [SerializeField]
        [NativeName("attestation")]
        private Attestation m_Attestation;

        public SignatureStatus status => m_Status;
        public string reason => m_Reason;
        public bool isLegacy => m_IsLegacy;
        public string errorCode => m_ErrorCode;
        public Attestation attestation => m_Attestation;

        internal SignatureInfo() : this(SignatureStatus.Unchecked, "", false, null, "") {}

        internal SignatureInfo(SignatureStatus status, string reason, bool isLegacy, Attestation attestation, string errorCode)
        {
            m_Status = status;
            m_Reason = reason;
            m_IsLegacy = isLegacy;
            m_Attestation = attestation;
            m_ErrorCode = errorCode;
        }
    }
}
