// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.ShortcutManagement
{
    class PromptWindow : EditorWindow
    {
        static readonly Color k_InvalidColorLightSkin = new Color(1f, 0.69f, 0.73f);
        static readonly Color k_InvalidColorDarkSkin = new Color(1f, 0.87f, 0.89f);

        TextField m_TextField;
        Button m_SubmitButton;
        private TextElement m_Header;
        private TextElement m_Message;
        private TextElement m_ValueLabel;
        Predicate<string> m_Validator;
        Action<string> m_Action;
        bool m_IsValid;

        public static void Show(string title, string headerText, string messageText, string valueLabel, string initialValue, string acceptButtonText, Predicate<string> validator, Action<string> action)
        {
            var promptWindow = GetWindow<PromptWindow>(true, title, true);

            promptWindow.m_TextField.value = initialValue;
            promptWindow.m_SubmitButton.text = acceptButtonText;
            promptWindow.m_Validator = validator;
            promptWindow.m_Action = action;
            promptWindow.m_Header.text = headerText;
            promptWindow.m_Message.text = messageText;
            promptWindow.m_ValueLabel.text = valueLabel;

            promptWindow.m_TextField.SelectAll();
            promptWindow.m_TextField.Focus();
            promptWindow.UpdateValidation();

            promptWindow.ShowModal();
        }

        void OnEnable()
        {
            var root = new VisualElement();
            root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = 10;

            m_Header = new TextElement();
            StyleUtility.NormalTextColor(m_Header);
            m_Header.style.fontStyleAndWeight = StyleValue<FontStyle>.Create(FontStyle.Bold);
            m_Header.style.paddingBottom = m_Header.style.paddingTop = 10;

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;

            m_Message = new TextElement();
            StyleUtility.NormalTextColor(m_Message);
            m_Message.style.flexGrow = 1;

            var textContainer = new VisualElement();
            StyleUtility.StyleRow(textContainer);
            textContainer.style.paddingBottom = textContainer.style.paddingTop = 30;

            m_ValueLabel = new TextElement();
            StyleUtility.NormalTextColor(m_ValueLabel);

            m_TextField = new TextField();
            m_TextField.style.flexGrow = 1;
            m_TextField.style.height = 16;
            m_TextField.OnValueChanged(OnTextFieldValueChanged);
            m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);

            textContainer.Add(m_ValueLabel);
            textContainer.Add(m_TextField);

            var buttonSpacer = new VisualElement();
            buttonSpacer.style.flexGrow = 1;
            buttonSpacer.style.height = 20;

            var buttonSpacer2 = new VisualElement();
            buttonSpacer2.style.flexGrow = 1;
            buttonSpacer2.style.height = 20;

            var buttons = new VisualElement();
            StyleUtility.StyleRow(buttons);

            m_SubmitButton = new Button(Submit);
            m_SubmitButton.style.flexGrow = 1;
            m_SubmitButton.style.height = 20;

            var cancelButton = new Button(Close) { text = L10n.Tr("Cancel") };
            cancelButton.style.flexGrow = 1;
            cancelButton.style.height = 20;

            buttons.Add(buttonSpacer);
            buttons.Add(buttonSpacer2);
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                buttons.Add(cancelButton);
                buttons.Add(m_SubmitButton);
            }
            else
            {
                buttons.Add(m_SubmitButton);
                buttons.Add(cancelButton);
            }

            root.Add(m_Header);
            root.Add(spacer);
            root.Add(m_Message);
            root.Add(textContainer);
            root.Add(buttons);

            rootVisualContainer.Add(root);
        }

        void UpdateValidation()
        {
            m_IsValid = m_Validator == null || m_Validator(m_TextField.value);
            if (m_IsValid)
            {
                m_SubmitButton.SetEnabled(true);
                m_TextField.style.backgroundColor = Color.white;
            }
            else
            {
                m_SubmitButton.SetEnabled(false);
                m_TextField.style.backgroundColor = EditorGUIUtility.isProSkin ? k_InvalidColorDarkSkin : k_InvalidColorLightSkin;
            }
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
