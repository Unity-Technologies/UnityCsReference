// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class Alert : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<Alert> {}

        public Action onCloseError;

        private ResourceLoader m_ResourceLoader;
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

            closeButton.clickable.clicked += () =>
            {
                onCloseError?.Invoke();
                ClearError();
            };
        }

        public void SetError(UIError error)
        {
            var message = string.IsNullOrEmpty(error.message) ? L10n.Tr("An error occurred.")
                : string.Format(L10n.Tr("An error occurred: {0}"), error.message);

            if ((UIError.Attribute.IsDetailInConsole & error.attribute) != 0)
            {
                message = string.Format(L10n.Tr("{0} See console for more details."), message);
            }

            alertMessage.text = message;
            UIUtils.SetElementDisplay(this, true);
        }

        public void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            alertMessage.text = string.Empty;
            onCloseError = null;
        }

        private VisualElementCache cache { get; set; }

        private Label alertMessage { get { return cache.Get<Label>("alertMessage"); } }

        private Button closeButton { get { return cache.Get<Button>("close"); } }
    }
}
