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

        private const float k_PositionRightOriginal = 5.0f;
        private const float k_PositionRightWithScroll = 12.0f;

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
            var message = ApplicationUtil.instance.GetTranslationForText("An error occurred.");
            if (error != null)
                message = error.message ?? string.Format(ApplicationUtil.instance.GetTranslationForText("An error occurred ({0})"), error.errorCode.ToString());

            alertMessage.text = message;
            UIUtils.SetElementDisplay(this, true);
        }

        public void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            alertMessage.text = "";
            onCloseError = null;
        }

        private VisualElementCache cache { get; set; }

        private Label alertMessage { get { return cache.Get<Label>("alertMessage"); } }

        private Button closeButton { get { return cache.Get<Button>("close"); } }
    }
}
