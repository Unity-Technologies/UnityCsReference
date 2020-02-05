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

        public Alert()
        {
            UIUtils.SetElementDisplay(this, false);

            var root = Resources.GetTemplate("Alert.uxml");
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
            var message = string.IsNullOrEmpty(error.message) ?
                ApplicationUtil.instance.GetTranslationForText("An error occurred. See console for more details.") :
                string.Format(ApplicationUtil.instance.GetTranslationForText("An error occurred ({0}). See console for more details."), error.message);

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
