// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal class TextEditorEngine : TextEditor
    {
        public TextEditorEngine(TextInputFieldBase field)
        {
            textInputField = field;
        }

        TextInputFieldBase textInputField { get; }

        internal override Rect localPosition
        {
            get { return new Rect(0, 0, position.width, position.height); }
        }

        internal override void OnDetectFocusChange()
        {
            if (m_HasFocus && !textInputField.hasFocus)
                OnFocus();
            if (!m_HasFocus && textInputField.hasFocus)
                OnLostFocus();
        }
    }
}
