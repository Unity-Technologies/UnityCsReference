// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/CertificateHandler/CertificateHandlerScript.h")]
    public class CertificateHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        extern private static IntPtr Create(CertificateHandler obj);

        [NativeMethod(IsThreadSafe = true)]
        extern private void Release();

        protected CertificateHandler()
        {
            m_Ptr = Create(this);
        }

        ~CertificateHandler()
        {
            Dispose();
        }

        protected virtual bool ValidateCertificate(byte[] certificateData)
        {
            return false;
        }

        [RequiredByNativeCode]
        internal bool ValidateCertificateNative(byte[] certificateData)
        {
            return ValidateCertificate(certificateData);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Release();
                m_Ptr = IntPtr.Zero;
            }
        }

    }
}
