// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    class OperationStatus
    {
        [NativeName("status")]
        private NativeStatusCode m_Status;
        [NativeName("id")]
        private string m_Id;
        [NativeName("type")]
        private string m_Type;
        [NativeName("packageList")]
        private PackageInfo[] m_PackageList;
        [NativeName("progress")]
        private float m_Progress;
        [NativeName("error")]
        private Error m_Error;

        private OperationStatus() {}

        public string id { get { return m_Id;  } }
        public NativeStatusCode status { get { return m_Status;  } }
        public string type { get { return m_Type;  } }
        public PackageInfo[] packageList { get { return m_PackageList; } }
        public float progress { get { return m_Progress; } }
        public Error error
        {
            get
            {
                if (m_Error != null && m_Error.errorCode == ErrorCode.Unknown && m_Error.message == "")
                {
                    // Since the native error field is an Error instance (rather than a Error pointer), it is always instanciated
                    //  by the binding layer, even when there is no error. Therefore we check whether it's an "empty" error.
                    return null;
                }
                return m_Error;
            }
        }
    }
}

