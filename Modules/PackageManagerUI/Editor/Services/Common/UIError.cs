// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UIError
    {
        private static readonly string k_CantValidateSignatureErrorMessage = L10n.Tr("Package signature could not be validated.");
        public static readonly UIError k_CantValidateSignatureError = new UIError(UIErrorCode.UpmError_Unknown, k_CantValidateSignatureErrorMessage);

        [SerializeField]
        private UIErrorCode m_ErrorCode;

        [SerializeField]
        private string m_Message;

        [SerializeField]
        private Attribute m_Attribute;

        [SerializeField]
        private int m_OperationErrorCode;

        public string readMoreURL => m_ReadMoreUrl;

        [SerializeField]
        private string m_ReadMoreUrl;

        [Flags]
        internal enum Attribute
        {
            None              = 0,
            DetailInConsole   = 1 << 0,
            Warning           = 1 << 1,
            Clearable         = 1 << 2,
            HiddenFromUI      = 1 << 3
        }

        public UIErrorCode errorCode => m_ErrorCode;

        public string message => m_Message;

        public int operationErrorCode => m_OperationErrorCode;

        public Attribute attribute
        {
            get => m_Attribute;
            set => m_Attribute = value;
        }

        public bool HasAttribute(Attribute attribute)
        {
            return (m_Attribute & attribute) != 0;
        }

        public UIError(Error error, Attribute attribute = Attribute.None) : this((UIErrorCode)error.errorCode, error.message, attribute)
        {
            // Currently the `Error` object we got from the Upm Client does not contain the status code (to be done in https://jira.unity3d.com/browse/PAK-2312)
            // For now we want to extract it from the error message string as a workaround
            const string errorMessageWithStatusCode = "failed with status code";
            var index = message.IndexOf(errorMessageWithStatusCode);
            if (index >= 0)
            {
                try
                {
                    var errorCodeString = Regex.Match(message.Substring(index), @"\d+").Value;
                    m_OperationErrorCode = int.Parse(errorCodeString);
                }
                catch (Exception)
                {
                    m_OperationErrorCode = -1;
                }
            }
        }

        public UIError(UIErrorCode errorCode, string message, Attribute attribute = Attribute.None, int operationErrorCode = -1, string readMoreUrl = "")
        {
            m_ErrorCode = errorCode;
            m_Message = message;
            m_Attribute = attribute;
            m_OperationErrorCode = operationErrorCode;
            m_ReadMoreUrl = readMoreUrl;
        }
    }
}
