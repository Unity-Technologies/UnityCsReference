// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageAddFromUrlField : VisualElement
    {
        private static PackageAddFromUrlField instance;

        public static void Show(VisualElement root)
        {
            if (instance == null)
                instance = new PackageAddFromUrlField();
            if (instance.parent == null)
                root.Add(instance);

            instance.Show();
        }

        public PackageAddFromUrlField()
        {
            var root = Resources.GetTemplate("PackageAddFromUrlField.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            AddButton.clickable.clicked += OnAddButtonClick;

            RegisterCallback<MouseDownEvent>(evt => Hide());
            AddFromUrlFieldContainer.RegisterCallback<MouseDownEvent>(evt =>
            {
                EditorApplication.delayCall += () => { UrlTextField.visualInput.Focus(); };
                evt.StopPropagation();
            });

            UrlTextField.RegisterCallback<ChangeEvent<string>>(OnUrlTextFieldChange);
        }

        private void OnUrlTextFieldChange(ChangeEvent<string> evt)
        {
            AddButton.SetEnabled(!string.IsNullOrEmpty(UrlTextField.value));
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
            var urlText = UrlTextField.value;
            if (!string.IsNullOrEmpty(urlText) && !Package.AddRemoveOperationInProgress)
            {
                Package.AddFromUrl(urlText);
                Hide();
            }
        }

        internal void Show()
        {
            if (parent == null)
                return;

            UrlTextField.value = string.Empty;
            UrlTextField.visualInput.Focus();
            UrlTextField.visualInput.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            AddButton.SetEnabled(false);

            foreach (var element in parent.Children())
                if (element != this)
                    element.SetEnabled(false);
        }

        internal void Hide()
        {
            if (parent == null)
                return;

            UrlTextField.visualInput.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            foreach (var element in parent.Children())
                element.SetEnabled(true);
            parent.Remove(this);
        }

        private VisualElementCache Cache { get; set; }

        private VisualElement AddFromUrlFieldContainer { get { return Cache.Get<VisualElement>("addFromUrlFieldContainer"); } }
        private TextField UrlTextField { get { return Cache.Get<TextField>("urlTextField"); } }
        private Button AddButton { get { return Cache.Get<Button>("addFromUrlButton"); } }
    }
}
