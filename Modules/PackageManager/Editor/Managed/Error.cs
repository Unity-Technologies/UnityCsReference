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
    public class Error
    {
        [SerializeField]
        private ErrorCode m_ErrorCode;
        [SerializeField]
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

