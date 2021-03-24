// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class GenericInputDropdown : DropdownContent
    {
        public static readonly string k_DefaultSubmitButtonText = L10n.Tr("Submit");
        internal override Vector2 windowSize => new Vector2(320, 50);

        public Action<string> submitClicked { get; set; }

        private TextFieldPlaceholder m_InputPlaceholder;
        private EditorWindow m_AnchorWindow;

        private ResourceLoader m_ResourceLoader;
        private void ResolveDependencies(ResourceLoader resourceLoader)
        {
            m_ResourceLoader = resourceLoader;
        }

        public GenericInputDropdown(ResourceLoader resourceLoader, EditorWindow anchorWindow, InputDropdownArgs args)
        {
            ResolveDependencies(resourceLoader);

            styleSheets.Add(m_ResourceLoader.inputDropdownStyleSheet);

            var root = m_ResourceLoader.GetTemplate("GenericInputDropdown.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            Init(anchorWindow, args);
        }

        internal override void OnDropdownShown()
        {
            inputTextField.visualInput.Focus();
            m_AnchorWindow?.rootVisualElement?.SetEnabled(false);
        }

        internal override void OnDropdownClosed()
        {
            m_InputPlaceholder.OnDisable();

            inputTextField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            inputTextField.visualInput.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            submitButton.clickable.clicked -= SubmitClicked;

            if (m_AnchorWindow != null)
            {
                m_AnchorWindow.rootVisualElement.SetEnabled(true);
                m_AnchorWindow = null;
            }

            inputTextField.value = string.Empty;
            submitButton.text = string.Empty;
            submitClicked = null;
        }

        private void Init(EditorWindow anchorWindow, InputDropdownArgs args)
        {
            m_AnchorWindow = anchorWindow;

            inputTextField.value = args.defaultValue ?? string.Empty;
            inputTextField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            inputTextField.visualInput.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            m_InputPlaceholder = new TextFieldPlaceholder(inputTextField);
            m_InputPlaceholder.text = args.placeholderText ?? string.Empty;

            mainTitle.text = args.title ?? string.Empty;
            UIUtils.SetElementDisplay(mainTitle, !string.IsNullOrEmpty(mainTitle.text));

            var showIcon = false;
            if (!string.IsNullOrEmpty(args.iconUssClass))
            {
                showIcon = true;
                icon.AddToClassList(args.iconUssClass);
            }
            else if (args.icon != null)
            {
                showIcon = true;
                icon.style.backgroundImage = new StyleBackground((Background)args.icon);
            }
            UIUtils.SetElementDisplay(icon, showIcon);

            submitButton.clickable.clicked += SubmitClicked;
            submitButton.SetEnabled(!string.IsNullOrEmpty(inputTextField.value));
            submitButton.text = !string.IsNullOrEmpty(args.submitButtonText) ? args.submitButtonText : k_DefaultSubmitButtonText;
            if (args.onInputSubmitted != null)
                submitClicked = args.onInputSubmitted;
        }

        private void SubmitClicked()
        {
            if (string.IsNullOrEmpty(inputTextField.value))
                return;

            submitClicked?.Invoke(inputTextField.value);
            Close();
        }

        private void OnTextFieldChange(ChangeEvent<string> evt)
        {
            submitButton.SetEnabled(!string.IsNullOrEmpty(inputTextField.value));
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SubmitClicked();
                    break;
            }
        }

        private VisualElementCache cache { get; set; }
        private VisualElement icon => cache.Get<VisualElement>("icon");
        private Label mainTitle => cache.Get<Label>("mainTitle");
        private TextField inputTextField => cache.Get<TextField>("inputTextField");
        private Button submitButton => cache.Get<Button>("submitButton");
    }
}
