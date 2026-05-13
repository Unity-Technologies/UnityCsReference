// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.AssetPackage
{
    using SignatureInfo = UnityEditor.PackageManager.SignatureInfo;
    using TrustLevel = UnityEditor.PackageManager.TrustLevel;

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class AssetPackageInfo
    {
        [SerializeField]
        [NativeName("errorCode")]
        private AssetPackageErrorCode m_ErrorCode;

        [SerializeField]
        [NativeName("errorMessage")]
        private string m_ErrorMessage;

        [SerializeField]
        [NativeName("signature")]
        private SignatureInfo m_Signature;

        [SerializeField]
        [NativeName("trustLevel")]
        private TrustLevel m_TrustLevel;

        public AssetPackageErrorCode errorCode => m_ErrorCode;
        public string errorMessage => m_ErrorMessage;
        public SignatureInfo signature => m_Signature;
        public TrustLevel trustLevel => m_TrustLevel;

        internal AssetPackageInfo() : this(AssetPackageErrorCode.None, "", new SignatureInfo(), TrustLevel.Unchecked) {}

        internal AssetPackageInfo(AssetPackageErrorCode errorCode, string errorMessage, SignatureInfo signature, TrustLevel trustLevel)
        {
            m_ErrorCode = errorCode;
            m_ErrorMessage = errorMessage;
            m_Signature = signature;
            m_TrustLevel = trustLevel;
        }
    }
}
