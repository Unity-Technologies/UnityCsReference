// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    public class Error
    {
        [SerializeField]
        [NativeName("errorCode")]
        private ErrorCode m_ErrorCode;

        [SerializeField]
        [NativeName("message")]
        private string m_Message;

        private Error() {}

        internal Error(ErrorCode errorCode, string message)
        {
            m_ErrorCode = errorCode;
            m_Message = message;
        }

        public ErrorCode errorCode { get { return m_ErrorCode;  } }
        public string message { get { return m_Message;  } }
    }
}

