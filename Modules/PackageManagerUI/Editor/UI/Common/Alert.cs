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
        }

        public void SetError(UIError error)
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

            alertMessage.text = message;
            UIUtils.SetElementDisplay(this, true);
        }

        public void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            alertMessage.text = string.Empty;
        }

        private VisualElementCache cache { get; set; }

        private Label alertMessage { get { return cache.Get<Label>("alertMessage"); } }
        private VisualElement alertContainer { get { return cache.Get<VisualElement>("alertContainer"); } }
    }
}
