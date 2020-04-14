// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackagesAction : VisualElement
    {
        public Action<string> actionClicked { get; set; }
        private readonly string m_PlaceHolderText;

        private ResourceLoader m_ResourceLoader;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
        }

        public PackagesAction(string actionButtonText, string defaultText = "")
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackagesAction.uxml");
            Add(root);

            m_PlaceHolderText = defaultText;

            Cache = new VisualElementCache(root);

            ActionButton.clickable.clicked += ActionClick;

            RegisterCallback<MouseDownEvent>(evt => Hide());
            PackagesActionContainer.RegisterCallback<MouseDownEvent>(evt =>
            {
                EditorApplication.delayCall += () => { ParamTextField.visualInput.Focus(); };
                evt.StopPropagation();
            });

            ParamTextField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            ActionButton.text = actionButtonText;
        }

        private void ActionClick()
        {
            if (string.IsNullOrEmpty(ParamTextField.value))
                return;

            actionClicked?.Invoke(ParamTextField.value);
        }

        private void OnTextFieldChange(ChangeEvent<string> evt)
        {
            ActionButton.SetEnabled(!string.IsNullOrEmpty(ParamTextField.value));
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
                    ActionClick();
                    break;
            }
        }

        internal void Show()
        {
            if (parent == null)
                return;

            ParamTextField.value = m_PlaceHolderText;
            ParamTextField.visualInput.Focus();
            ParamTextField.visualInput.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            ActionButton.SetEnabled(!string.IsNullOrEmpty(m_PlaceHolderText));

            foreach (var element in parent.Children())
                element.SetEnabled(element == this);
        }

        internal void Hide()
        {
            if (parent == null)
                return;

            ParamTextField.visualInput.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            foreach (var element in parent.Children())
                element.SetEnabled(true);
            parent.Remove(this);
        }

        private VisualElementCache Cache { get; }

        private VisualElement PackagesActionContainer { get { return Cache.Get<VisualElement>("packagesActionContainer"); } }
        private TextField ParamTextField { get { return Cache.Get<TextField>("paramTextField"); } }
        private Button ActionButton { get { return Cache.Get<Button>("actionButton"); } }
    }
}
