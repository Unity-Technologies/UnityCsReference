// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UIError : Error
    {
        private static readonly string k_EntitlementErrorMessage = L10n.Tr("This package is not available to use because there is no license registered for your user. Please sign in with a licensed account. If the problem persists, please contact your administrator.");
        internal static readonly string k_InvalidSignatureWarningMessage = L10n.Tr("This package version doesn't have a valid signature. For your security, install a different version or report a bug to Unity.");
        internal static readonly string k_UnsignedUnityPackageWarningMessage = L10n.Tr("This package version has no signature. For your security, install a different version or review your scoped registry and load the package from the Unity registry.");
        internal static readonly string k_readMoreDocsUrl = "https://docs.unity3d.com/Manual/upm-errors.html";
        public static readonly UIError k_EntitlementError = new UIError(UIErrorCode.Forbidden, k_EntitlementErrorMessage);
        public static readonly UIError k_EntitlementWarning = new UIError(UIErrorCode.Forbidden, k_EntitlementErrorMessage, Attribute.IsWarning);
        internal static readonly UIError k_InvalidSignatureWarning = new UIError(
            UIErrorCode.UpmError_InvalidSignature,
            k_InvalidSignatureWarningMessage,
            Attribute.IsWarning,
            readMoreUrl: k_readMoreDocsUrl);

        internal static readonly UIError k_UnsignedUnityPackageWarning = new UIError(
            UIErrorCode.UpmError_UnsignedUnityPackage,
            k_UnsignedUnityPackageWarningMessage,
            Attribute.IsWarning,
            readMoreUrl: k_readMoreDocsUrl);

        [SerializeField]
        private UIErrorCode m_UIErrorCode;

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

        public new UIErrorCode errorCode => m_UIErrorCode;

        public int operationErrorCode => m_OperationErrorCode;

        public Attribute attribute => m_Attribute;

        public bool HasAttribute(Attribute attribute)
        {
            return (m_Attribute & attribute) != 0;
        }

        public UIError(UIErrorCode errorCode, string message, int operationErrorCode) : this(errorCode, message, Attribute.None, operationErrorCode) {}

        public UIError(UIErrorCode errorCode, string message, Attribute attribute = Attribute.None, int operationErrorCode = -1, string readMoreUrl = "") : base(NativeErrorCode.Unknown, message)
        {
            m_UIErrorCode = errorCode;
            m_Attribute = attribute;
            m_OperationErrorCode = operationErrorCode;
            m_ReadMoreUrl = readMoreUrl;
        }
    }
}
