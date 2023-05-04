// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShortcutManagement
{
    class PromptWindow : EditorWindow
    {
        Func<string, string> m_Validator;
        Action<string> m_Action;
        bool m_IsValid;

        TextElement m_HeaderTextElement;
        TextElement m_MessageTextElement;
        TextElement m_LabelTextElement;
        TextField m_TextField;
        Label m_WarningText;
        Button m_SubmitButton;

        public static void Show(string title, string headerText, string messageText, string valueLabel, string initialValue,
            string acceptButtonText, Func<string, string> validator, Action<string> action)
        {
            var promptWindow = GetWindow<PromptWindow>(true, title, true);

            // TODO: Ideally the window size should be fixed according to its contents
            promptWindow.minSize = promptWindow.maxSize = new Vector2(380, 160);

            promptWindow.m_HeaderTextElement.text = headerText;
            promptWindow.m_MessageTextElement.text = messageText;
            promptWindow.m_LabelTextElement.text = valueLabel;
            promptWindow.m_TextField.value = initialValue;
            promptWindow.m_SubmitButton.text = acceptButtonText;

            promptWindow.m_Validator = validator;
            promptWindow.m_Action = action;

            promptWindow.m_TextField.SelectAll();
            promptWindow.m_TextField.Focus();
            promptWindow.UpdateValidation();

            promptWindow.ShowModal();
        }

        void OnEnable()
        {
            // Load elements
            var root = new VisualElement { name = "root-container" };
            var visualTreeAsset = (VisualTreeAsset)EditorResources.Load<UnityEngine.Object>("UXML/ShortcutManager/PromptWindow.uxml");
            visualTreeAsset.CloneTree(root);
            rootVisualElement.Add(root);

            // Load styles
            if (EditorGUIUtility.isProSkin)
                root.AddToClassList("isProSkin");

            // Find elements
            m_HeaderTextElement = root.Q<TextElement>("header");
            m_MessageTextElement = root.Q<TextElement>("message");
            var labelAndTextField = root.Q("label-and-text-field");
            m_LabelTextElement = labelAndTextField.Q<TextElement>();
            m_TextField = labelAndTextField.Q<TextField>();
            m_WarningText = labelAndTextField.Q<Label>("warning-text");
            var buttons = root.Q("buttons");
            m_SubmitButton = root.Q<Button>("submit");
            var cancelButton = root.Q<Button>("cancel");

            // Set localized text
            cancelButton.text = L10n.Tr("Cancel");

            // Set up event handlers
            m_TextField.RegisterValueChangedCallback(OnTextFieldValueChanged);
            m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
            m_SubmitButton.clickable.clicked += Submit;
            cancelButton.clickable.clicked += Close;

            // Flip submit and cancel buttons on macOS
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                var parent = m_SubmitButton.parent;
                parent.Remove(m_SubmitButton);
                parent.Add(m_SubmitButton);
            }

            // Mark last button with class
            buttons.Children().Last().AddToClassList("last");
        }

        void UpdateValidation()
        {
            var validationErrorMessage = m_Validator?.Invoke(m_TextField.value);

            m_WarningText.text = validationErrorMessage;
            m_IsValid = validationErrorMessage == null;

            m_SubmitButton.SetEnabled(m_IsValid);
            m_WarningText.visible = !m_IsValid;
        }

        void Submit()
        {
            m_Action(m_TextField.text);
            Close();
        }

        void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            UpdateValidation();
        }

        void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (m_IsValid)
                    {
                        Submit();
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.Escape:
                    Close();
                    evt.StopPropagation();
                    break;
            }
        }
    }
}
