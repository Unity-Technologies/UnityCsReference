// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageAddFromUrlField : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageAddFromUrlField> {}
        private string urlText;

        private readonly VisualElement root;

        public PackageAddFromUrlField()
        {
            root = Resources.GetTemplate("PackageAddFromUrlField.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            UrlTextField.value = urlText;

            AddButton.SetEnabled(!string.IsNullOrEmpty(urlText));
            AddButton.clickable.clicked += OnAddButtonClick;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        private void OnUrlTextFieldChange(ChangeEvent<string> evt)
        {
            urlText = evt.newValue;
            AddButton.SetEnabled(!string.IsNullOrEmpty(urlText));
        }

        private void OnUrlTextFieldFocus(FocusEvent evt)
        {
            Show();
        }

        private void OnUrlTextFieldFocusOut(FocusOutEvent evt)
        {
            Hide();
        }

        private void OnContainerFocus(FocusEvent evt)
        {
            UrlTextField.Focus();
        }

        private void OnContainerFocusOut(FocusOutEvent evt)
        {
            Hide();
        }

        private void OnEnterPanel(AttachToPanelEvent evt)
        {
            AddFromUrlFieldContainer.RegisterCallback<FocusEvent>(OnContainerFocus);
            AddFromUrlFieldContainer.RegisterCallback<FocusOutEvent>(OnContainerFocusOut);
            UrlTextField.Q("unity-text-input").RegisterCallback<FocusEvent>(OnUrlTextFieldFocus);
            UrlTextField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(OnUrlTextFieldFocusOut);
            UrlTextField.RegisterCallback<ChangeEvent<string>>(OnUrlTextFieldChange);
            UrlTextField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            Hide();
        }

        private void OnLeavePanel(DetachFromPanelEvent evt)
        {
            AddFromUrlFieldContainer.UnregisterCallback<FocusEvent>(OnContainerFocus);
            AddFromUrlFieldContainer.UnregisterCallback<FocusOutEvent>(OnContainerFocusOut);
            UrlTextField.Q("unity-text-input").UnregisterCallback<FocusEvent>(OnUrlTextFieldFocus);
            UrlTextField.Q("unity-text-input").UnregisterCallback<FocusOutEvent>(OnUrlTextFieldFocusOut);
            UrlTextField.UnregisterCallback<ChangeEvent<string>>(OnUrlTextFieldChange);
            UrlTextField.Q("unity-text-input").UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Hide();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnAddButtonClick();
                    break;
            }
        }

        private void OnAddButtonClick()
        {
            var path = urlText;
            if (!string.IsNullOrEmpty(path) && !Package.AddRemoveOperationInProgress)
            {
                Package.AddFromLocalDisk(path);
                Hide();
            }
        }

        internal void Hide()
        {
            UIUtils.SetElementDisplay(this, false);
        }

        internal void Show(bool reset = false)
        {
            if (reset)
                Reset();
            UIUtils.SetElementDisplay(this, true);
        }

        private void Reset()
        {
            UrlTextField.value = string.Empty;
            urlText = string.Empty;
            AddButton.SetEnabled(false);
            UrlTextField.Focus();
        }

        private VisualElementCache Cache { get; set; }

        private VisualElement AddFromUrlFieldContainer { get { return Cache.Get<VisualElement>("addFromUrlFieldContainer"); } }
        private TextField UrlTextField { get { return Cache.Get<TextField>("urlTextField"); } }
        private Button AddButton { get { return Cache.Get<Button>("addFromUrlButton"); } }
    }
}
