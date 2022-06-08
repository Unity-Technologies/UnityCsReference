// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class Alert : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<Alert> {}

        private ResourceLoader m_ResourceLoader;
        private UnityConnectProxy m_UnityConnectProxy;

        private Action m_OnActionButtonClicked;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_UnityConnectProxy = container.Resolve<UnityConnectProxy>();
        }

        public Alert()
        {
            ResolveDependencies();

            UIUtils.SetElementDisplay(this, false);

            var root = m_ResourceLoader.GetTemplate("Alert.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            alertActionButton.clicked += () => m_OnActionButtonClicked?.Invoke();
        }

        public void RefreshError(UIError error, IPackageVersion packageVersion = null)
        {
            if (error == null)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }
            UIUtils.SetElementDisplay(this, true);

            var message = error.message ?? string.Empty;
            if (error.HasAttribute(UIError.Attribute.IsDetailInConsole))
                message = string.Format(L10n.Tr("{0} See console for more details."), message);
            alertMessage.text = message;

            if (error.HasAttribute(UIError.Attribute.IsWarning))
            {
                alertContainer.RemoveFromClassList("error");
                alertContainer.AddClasses("warning");
            }
            else
            {
                alertContainer.RemoveFromClassList("warning");
                alertContainer.AddClasses("error");
            }

            RefreshActionButton(error, packageVersion);
        }

        private void RefreshActionButton(UIError error, IPackageVersion packageVersion)
        {
            var buttonText = string.Empty;
            Action buttonAction = null;

            if (error.errorCode is UIErrorCode.UpmError_NotSignedIn)
            {
                buttonText = L10n.Tr("Sign in");
                buttonAction = () => m_UnityConnectProxy.ShowLogin();
            }
            else if (error.errorCode is UIErrorCode.UpmError_NotAcquired)
            {
                var assetStoreLink = (packageVersion?.package as AssetStorePackage)?.assetStoreLink;
                if (!string.IsNullOrEmpty(assetStoreLink))
                {
                    buttonText = L10n.Tr("View in Asset Store");
                    buttonAction = () => Application.OpenURL(assetStoreLink);
                }
            }
            else if (!string.IsNullOrEmpty(error.readMoreURL))
            {
                buttonText = L10n.Tr("Read more");
                buttonAction = () =>
                {
                    PackageManagerWindowAnalytics.SendEvent($"alertreadmore_{error.errorCode.ToString()}", packageVersion?.uniqueId);
                    Application.OpenURL(error.readMoreURL);
                };
            }

            alertActionButton.text = buttonText;
            m_OnActionButtonClicked = buttonAction;
            UIUtils.SetElementDisplay(alertActionButton, buttonAction != null);
        }

        private VisualElementCache cache { get; }

        private Label alertMessage => cache.Get<Label>("alertMessage");
        private VisualElement alertContainer => cache.Get<VisualElement>("alertContainer");
        private Button alertActionButton => cache.Get<Button>("alertActionButton");
    }
}
