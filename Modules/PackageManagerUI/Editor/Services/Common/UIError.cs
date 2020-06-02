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

        [SerializeField]
        private Attribute m_Attribute;

        [Flags]
        internal enum Attribute
        {
            None = 0,
            IsDetailInConsole = 1 << 0,
            IsWarning = 1 << 1,
            IsClearable = 1 << 2
        }

        public UIError(UIErrorCode errorCode, string message, Attribute attribute = Attribute.None)
        {
            m_ErrorCode = errorCode;
            m_Message = message;
            m_Attribute = attribute;
        }

        public UIErrorCode errorCode => m_ErrorCode;

        public string message => m_Message;

        public Attribute attribute => m_Attribute;
    }
}
