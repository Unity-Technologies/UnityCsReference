// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UIError
    {
        [SerializeField]
        private UIErrorCode m_ErrorCode;

        [SerializeField]
        private string m_Message;

        public UIError(UIErrorCode errorCode, string message)
        {
            m_ErrorCode = errorCode;
            m_Message = message;
        }

        public UIErrorCode errorCode => m_ErrorCode;

        public string message => m_Message;

        public static explicit operator UIError(Error v)
        {
            return new UIError((UIErrorCode)v.errorCode, v.message);
        }
    }
}
