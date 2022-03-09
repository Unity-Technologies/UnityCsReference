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
        private Action m_ReadMoreLinkAction;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
        }

        public Alert()
        {
            ResolveDependencies();

            UIUtils.SetElementDisplay(this, false);

            var root = m_ResourceLoader.GetTemplate("Alert.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            alertReadMoreLink.clicked += ReadMoreLinkClick;
        }

        private void ReadMoreLinkClick()
        {
            m_ReadMoreLinkAction?.Invoke();
        }

        public void SetError(UIError error, IPackageVersion packageVersion = null)
        {
            var message = string.Empty;
            if (!string.IsNullOrEmpty(error.message))
                message = error.message;
            if (error.HasAttribute(UIError.Attribute.IsDetailInConsole))
                message = string.Format(L10n.Tr("{0} See console for more details."), message);

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

            if (!string.IsNullOrEmpty(error.readMoreURL))
            {
                ShowReadMoreLink(error, packageVersion);
            }
            else
            {
                HideReadMoreLink();
            }

            alertMessage.text = message;
            UIUtils.SetElementDisplay(this, true);
        }

        public void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            alertMessage.text = string.Empty;
            HideReadMoreLink();
        }

        private void ShowReadMoreLink(UIError error, IPackageVersion packageVersion)
        {
            m_ReadMoreLinkAction = () =>
            {
                PackageManagerWindowAnalytics.SendEvent($"alertreadmore_{error.errorCode.ToString()}", packageVersion?.uniqueId);
                Application.OpenURL(error.readMoreURL);
            };
            UIUtils.SetElementDisplay(alertReadMoreLink, true);
        }

        private void HideReadMoreLink()
        {
            UIUtils.SetElementDisplay(alertReadMoreLink, false);
            m_ReadMoreLinkAction = null;
        }

        private VisualElementCache cache { get; set; }

        private Label alertMessage { get { return cache.Get<Label>("alertMessage"); } }
        private VisualElement alertContainer { get { return cache.Get<VisualElement>("alertContainer"); } }
        private Button alertReadMoreLink { get { return cache.Get<Button>("alertReadMoreLink"); } }
    }
}
