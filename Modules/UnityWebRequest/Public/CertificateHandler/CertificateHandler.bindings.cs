// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Networking
{
    ///<summary>Responsible for rejecting or accepting certificates received on https requests.</summary>
    ///<remarks>**Note**: Custom certificate validation is currently implemented for Android, iOS, tvOS, visionOS, and desktop platforms only.
    ///To trust certificates on iOS, tvOS, and visionOS platforms, enable arbitrary loads either by enabling unsecured HTTP in Player Settings or explicitly in Info.plist file. For more information, refer to &lt;a href="https://developer.apple.com/documentation/bundleresources/information_property_list/nsapptransportsecurity/nsallowsarbitraryloads?language=objc"&gt;Apple documentation&lt;/a&gt;.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/CertificateHandler/CertificateHandlerScript.h")]
    public class CertificateHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        extern private static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] CertificateHandler obj);

        [NativeMethod(IsThreadSafe = true)]
        extern private void ReleaseFromScripting();

        protected CertificateHandler()
        {
            m_Ptr = Create(this);
        }

#pragma warning disable UAC0045 // Releases the native handler; ReleaseFromScripting is thread-safe and public handlers are often left undisposed.
        ~CertificateHandler()
        {
            Dispose();
        }
#pragma warning restore UAC0045

        ///<summary>Callback, invoked for each leaf certificate sent by the remote server.</summary>
        ///<remarks>Override this to implement a custom certificate validation scheme.</remarks>
        ///<param name="certificateData">Certificate data in PEM or DER format. If certificate data contains multiple certificates, the first one is the leaf certificate.</param>
        ///<returns>
        ///  <c>true</c> if the certificate should be accepted, <c>false</c> if not.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Networking;
        ///using System.Security.Cryptography.X509Certificates;
        ///
        /// // Based on https://www.owasp.org/index.php/Certificate_and_Public_Key_Pinning#.Net
        ///class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
        ///{
        ///    // Encoded RSAPublicKey
        ///    private static string PUB_KEY = "30818902818100C4A06B7B52F8D17DC1CCB47362" +
        ///        "C64AB799AAE19E245A7559E9CEEC7D8AA4DF07CB0B21FDFD763C63A313A668FE9D764E" +
        ///        "D913C51A676788DB62AF624F422C2F112C1316922AA5D37823CD9F43D1FC54513D14B2" +
        ///        "9E36991F08A042C42EAAEEE5FE8E2CB10167174A359CEBF6FACC2C9CA933AD403137EE" +
        ///        "2C3F4CBED9460129C72B0203010001";
        ///
        ///    protected override bool ValidateCertificate(byte[] certificateData)
        ///    {
        ///        X509Certificate2 certificate = new X509Certificate2(certificateData);
        ///        string pk = certificate.GetPublicKeyString();
        ///        if (pk.Equals(PUB_KEY))
        ///            return true;
        ///
        ///        // Bad dog
        ///        return false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        protected virtual bool ValidateCertificate(byte[] certificateData)
        {
            return false;
        }

        [RequiredByNativeCode]
        internal bool ValidateCertificateNative(byte[] certificateData)
        {
            return ValidateCertificate(certificateData);
        }

        [VisibleToOtherModules]
        internal bool ValidateCertificateExternal(byte[] certificateData)
        {
            return ValidateCertificate(certificateData);
        }

        ///<summary>Signals that this [CertificateHandler] is no longer being used, and should clean up any resources it is using.</summary>
        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                ReleaseFromScripting();
                m_Ptr = IntPtr.Zero;
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(CertificateHandler handler) => handler.m_Ptr;
        }

    }
}
