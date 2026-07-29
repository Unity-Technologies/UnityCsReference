// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class Alert : ExtendedHelpBox
    {
        [Serializable]
        public new class UxmlSerializedData : ExtendedHelpBox.UxmlSerializedData
        {
            public override object CreateInstance() => new Alert();
        }

        private readonly IApplicationProxy m_Application;
        private readonly IUnityConnectProxy m_UnityConnectProxy;

        public Alert()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<IApplicationProxy>();
            m_UnityConnectProxy = container.Resolve<IUnityConnectProxy>();

            UIUtils.SetElementDisplay(this, false);
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
            if (error.errorCode is UIErrorCode.UpmError_NotSignedIn)
            {
                SetCustomLinkButton(L10n.Tr("Sign in"), () => m_UnityConnectProxy.ShowLogin());
                return;
            }

            if (error.errorCode is UIErrorCode.UpmError_NotAcquired)
            {
                var productUrl = packageVersion?.package?.product?.productUrl;
                if (!string.IsNullOrEmpty(productUrl))
                {
                    SetCustomLinkButton(L10n.Tr("View in Asset Store"), () =>
                    {
                        m_Application.OpenURL(productUrl);
                        PackageManagerWindowAnalytics.SendEvent("viewProductInAssetStoreFromAlertHelpBox", packageVersion.uniqueId);
                    }, productUrl);
                    return;
                }
                SetCustomLinkButton(string.Empty, null);
                return;
            }

            if (!string.IsNullOrEmpty(error.readMoreURL))
            {
                SetCustomLinkButton(L10n.Tr("Learn More"), () =>
                {
                    PackageManagerWindowAnalytics.SendEvent($"alertreadmore_{error.errorCode}", packageVersion?.uniqueId);
                    m_Application.OpenURL(error.readMoreURL);
                }, error.readMoreURL);
                return;
            }

            SetCustomLinkButton(string.Empty, null);
        }
    }
}
