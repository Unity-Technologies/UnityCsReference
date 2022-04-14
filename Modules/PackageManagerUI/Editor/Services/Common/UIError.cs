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
        private static readonly string k_EntitlementErrorMessage = L10n.Tr("An error occurred: This package isn't available because its license isn't registered to your user account. If you're licensed to use this package, go to Unity Hub > Preferences > Licenses and click Refresh. Otherwise, contact your administrator.");
        internal static readonly string k_InvalidSignatureWarningMessage = L10n.Tr("This package version doesn't have a valid signature. For your security, install a different version or report a bug to Unity.");
        internal static readonly string k_UnsignedUnityPackageWarningMessage = L10n.Tr("This package version has no signature. For your security, install a different version or review your scoped registry and load the package from the Unity registry.");
        internal static readonly string k_ReadMoreDocsUrl = "https://docs.unity3d.com/Manual/upm-errors.html";
        public static readonly UIError k_EntitlementError = new UIError(UIErrorCode.Forbidden, k_EntitlementErrorMessage);
        public static readonly UIError k_EntitlementWarning = new UIError(UIErrorCode.Forbidden, k_EntitlementErrorMessage, Attribute.IsWarning);
        internal static readonly UIError k_InvalidSignatureWarning = new UIError(
            UIErrorCode.UpmError_InvalidSignature,
            k_InvalidSignatureWarningMessage,
            Attribute.IsWarning,
            readMoreUrl: k_ReadMoreDocsUrl);

        internal static readonly UIError k_UnsignedUnityPackageWarning = new UIError(
            UIErrorCode.UpmError_UnsignedUnityPackage,
            k_UnsignedUnityPackageWarningMessage,
            Attribute.IsWarning,
            readMoreUrl: k_ReadMoreDocsUrl);

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
            None                = 0,
            IsDetailInConsole   = 1 << 0,
            IsWarning           = 1 << 1,
            IsClearable         = 1 << 2
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
