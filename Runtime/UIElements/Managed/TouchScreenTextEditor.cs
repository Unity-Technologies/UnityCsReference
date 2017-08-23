// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal class TouchScreenTextEditor : TextEditor
    {
        private string m_SecureText;
        public string secureText
        {
            get { return m_SecureText; }
            set
            {
                string temp = value ?? string.Empty;
                if (temp != m_SecureText)
                {
                    m_SecureText = temp;
                }
            }
        }

        public TouchScreenTextEditor(TextField textField)
            : base(textField)
        {
            secureText = string.Empty;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseUpDownEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseUpDownEvent);
        }

        void OnMouseUpDownEvent(MouseDownEvent evt)
        {
            SyncTextEditor();
            textField.TakeCapture();

            keyboardOnScreen = TouchScreenKeyboard.Open(!string.IsNullOrEmpty(secureText) ? secureText : textField.text,
                    TouchScreenKeyboardType.Default,
                    true, // autocorrection
                    multiline,
                    !string.IsNullOrEmpty(secureText));

            // Scroll offset might need to be updated
            UpdateScrollOffset();
            evt.StopPropagation();
        }
    }
}
