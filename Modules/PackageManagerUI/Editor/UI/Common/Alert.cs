// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAlert
    {
        void SetError(UIError error);
        void ClearError();
    }
}

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class Alert : VisualElement, IAlert
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
            var state = error.HasAttribute(UIError.Attribute.IsWarning) ? PackageState.Warning : PackageState.Error;
            var message = state == PackageState.Warning ? L10n.Tr("A warning occurred") : L10n.Tr("An error occurred");

            if (!string.IsNullOrEmpty(error.message))
                message = string.Format("{0}: {1}", message, error.message);
            if (error.HasAttribute(UIError.Attribute.IsDetailInConsole))
                message = string.Format(L10n.Tr("{0} See console for more details."), message);

            var addClass = state == PackageState.Warning ? "warning" : "error";
            var removeFromClass = state == PackageState.Warning ? "error" : "warning";

            alertContainer.RemoveFromClassList(removeFromClass);
            alertContainer.AddClasses(addClass);

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
