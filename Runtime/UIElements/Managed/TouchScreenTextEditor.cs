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

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            SyncTextEditor();

            EventPropagation result = EventPropagation.Continue;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    result = DoMouseDown();
                    break;
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();

            return result;
        }

        EventPropagation DoMouseDown()
        {
            textField.TakeCapture();

            keyboardOnScreen = TouchScreenKeyboard.Open(!string.IsNullOrEmpty(secureText) ? secureText : textField.text,
                    TouchScreenKeyboardType.Default,
                    true, // autocorrection
                    multiline,
                    !string.IsNullOrEmpty(secureText));

            return EventPropagation.Stop;
        }
    }
}
