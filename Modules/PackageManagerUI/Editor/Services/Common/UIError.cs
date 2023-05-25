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
        private static readonly string k_EntitlementErrorMessage = L10n.Tr("This package is not available to use because there is no license registered for your user. If you believe you have permission to use this package, refresh your license in the license management window of Unity Hub. Otherwise, contact your administrator.");
        public static readonly UIError k_EntitlementError = new UIError(UIErrorCode.UpmError_Forbidden, k_EntitlementErrorMessage, Attribute.IsDetailInConsole);
        public static readonly UIError k_EntitlementWarning = new UIError(UIErrorCode.UpmError_Forbidden, k_EntitlementErrorMessage, Attribute.IsDetailInConsole | Attribute.IsWarning);
        [SerializeField]
        private UIErrorCode m_UIErrorCode;

        [SerializeField]
        private Attribute m_Attribute;

        [SerializeField]
        private int m_OperationErrorCode;

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

        public UIError(UIErrorCode errorCode, string message, Attribute attribute = Attribute.None, int operationErrorCode = -1) : base(NativeErrorCode.Unknown, message)
        {
            m_UIErrorCode = errorCode;
            m_Attribute = attribute;
            m_OperationErrorCode = operationErrorCode;
        }
    }
}
