// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class Alert : ExtendedHelpBox
    {
        public Alert() : this(ServicesContainer.instance.Resolve<IApplicationProxy>())
        {
        }

        public Alert(IApplicationProxy application) : base(application)
        {
        }

        public void RefreshError(UIError error, IPackageVersion packageVersion = null)
        {
            var showHelpBox = error != null;
            UIUtils.SetElementDisplay(this, showHelpBox);
            if (!showHelpBox)
                return;

            var message = error.message ?? string.Empty;
            if (error.HasAttribute(UIError.Attribute.DetailInConsole))
                message = string.Format(L10n.Tr("{0} See console for more details."), message);
            text = message;

            messageType = error.HasAttribute(UIError.Attribute.Warning) ? HelpBoxMessageType.Warning : HelpBoxMessageType.Error;

            RefreshActionButton(error, packageVersion);
        }

        private void RefreshActionButton(UIError error, IPackageVersion packageVersion)
        {
            Action buttonAction;
            if (error.errorCode is UIErrorCode.UpmError_NotAcquired)
            {
                var productUrl = packageVersion?.package?.product?.productUrl;
                if (string.IsNullOrEmpty(productUrl))
                    return;
                buttonAction = () =>
                {
                    m_Application.OpenURL(productUrl);
                    PackageManagerWindowAnalytics.SendEvent("viewProductInAssetStoreFromAlertHelpBox", packageVersion.uniqueId);
                };
                SetCustomLinkButton(L10n.Tr("View in Asset Store"), buttonAction, productUrl);
            }
            else if (!string.IsNullOrEmpty(error.readMoreURL))
            {
                buttonAction = () =>
                {
                    PackageManagerWindowAnalytics.SendEvent($"alertreadmore_{error.errorCode}", packageVersion?.uniqueId);
                    m_Application.OpenURL(error.readMoreURL);
                };
                SetCustomLinkButton(L10n.Tr("Learn More"), buttonAction, error.readMoreURL);
            }
            else
                SetCustomLinkButton(string.Empty, null, string.Empty);
        }
    }
}
