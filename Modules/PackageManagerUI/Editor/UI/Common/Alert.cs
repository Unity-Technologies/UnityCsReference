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

        private readonly VisualElement root;
        private const float originalPositionRight = 5.0f;
        private const float positionRightWithScroll = 12.0f;

        public Action OnCloseError;

        public Alert()
        {
            UIUtils.SetElementDisplay(this, false);

            root = Resources.GetTemplate("Alert.uxml");
            Add(root);
            root.StretchToParentSize();

            CloseButton.clickable.clicked += () =>
            {
                if (null != OnCloseError)
                    OnCloseError();
                ClearError();
            };
        }

        public void SetError(Error error)
        {
            var message = "An error occurred.";
            if (error != null)
                message = error.message ?? string.Format("An error occurred ({0})", error.errorCode.ToString());

            AlertMessage.text = message;
            UIUtils.SetElementDisplay(this, true);
        }

        public void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            AdjustSize(false);
            AlertMessage.text = "";
            OnCloseError = null;
        }

        public void AdjustSize(bool verticalScrollerVisible)
        {
            if (verticalScrollerVisible)
                style.right = originalPositionRight + positionRightWithScroll;
            else
                style.right = originalPositionRight;
        }

        private Label _alertMessage;
        private Label AlertMessage { get { return _alertMessage ?? (_alertMessage = root.Q<Label>("alertMessage")); } }

        private Button _closeButton;
        private Button CloseButton { get { return _closeButton ?? (_closeButton = root.Q<Button>("close")); } }
    }
}
