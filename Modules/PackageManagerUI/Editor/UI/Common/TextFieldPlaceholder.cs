// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class TextFieldPlaceholder : Label
    {
        private TextField m_ParentTextField;

        public TextFieldPlaceholder(TextField textField, string placeholderText = null)
        {
            if (textField == null)
                throw new ArgumentNullException("textField");

            AddToClassList("placeholder");
            text = placeholderText ?? string.Empty;
            textField.Add(this);
            pickingMode = PickingMode.Ignore;
            UIUtils.SetElementDisplay(this, string.IsNullOrEmpty(textField.value));
            textField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            m_ParentTextField = textField;
        }

        private void OnTextFieldChange(ChangeEvent<string> evt)
        {
            UIUtils.SetElementDisplay(this, string.IsNullOrEmpty(evt.newValue));
        }

        public void OnDisable()
        {
            m_ParentTextField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            m_ParentTextField.Remove(this);
        }
    }
}
