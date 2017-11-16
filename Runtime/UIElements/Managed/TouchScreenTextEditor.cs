// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal class TouchScreenTextEditorEventHandler : TextEditorEventHandler
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

        public TouchScreenTextEditorEventHandler(TextEditorEngine editorEngine, TextInputFieldBase textInputField)
            : base(editorEngine, textInputField)
        {
            secureText = string.Empty;
        }

        public override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            long mouseEventType = MouseDownEvent.TypeId();

            if (evt.GetEventTypeId() == mouseEventType)
            {
                textInputField.SyncTextEngine();
                textInputField.UpdateText(editorEngine.text);
                textInputField.TakeMouseCapture();

                editorEngine.keyboardOnScreen = TouchScreenKeyboard.Open(!string.IsNullOrEmpty(secureText) ? secureText : textInputField.text,
                        TouchScreenKeyboardType.Default,
                        true, // autocorrection
                        editorEngine.multiline,
                        !string.IsNullOrEmpty(secureText));

                // Scroll offset might need to be updated
                editorEngine.UpdateScrollOffset();
                evt.StopPropagation();
            }
        }
    }
}
