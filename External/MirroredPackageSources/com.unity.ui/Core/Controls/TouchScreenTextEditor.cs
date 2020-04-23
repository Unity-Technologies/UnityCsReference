namespace UnityEngine.UIElements
{
    internal class TouchScreenTextEditorEventHandler : TextEditorEventHandler
    {
        private IVisualElementScheduledItem m_TouchKeyboardPoller = null;

        public TouchScreenTextEditorEventHandler(TextEditorEngine editorEngine, ITextInputField textInputField)
            : base(editorEngine, textInputField)
        {
        }

        void PollTouchScreenKeyboard()
        {
            if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed)
            {
                if (m_TouchKeyboardPoller == null)
                {
                    m_TouchKeyboardPoller = (textInputField as VisualElement)?.schedule.Execute(DoPollTouchScreenKeyboard).Every(100);
                }
                else
                {
                    m_TouchKeyboardPoller.Resume();
                }
            }
        }

        void DoPollTouchScreenKeyboard()
        {
            if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed)
            {
                if (textInputField.editorEngine.keyboardOnScreen != null)
                {
                    textInputField.UpdateText(textInputField.CullString(textInputField.editorEngine.keyboardOnScreen.text));

                    if (!textInputField.isDelayed)
                    {
                        textInputField.UpdateValueFromText();
                    }

                    if (textInputField.editorEngine.keyboardOnScreen.status != TouchScreenKeyboard.Status.Visible)
                    {
                        textInputField.editorEngine.keyboardOnScreen = null;
                        m_TouchKeyboardPoller.Pause();

                        if (textInputField.isDelayed)
                        {
                            textInputField.UpdateValueFromText();
                        }
                    }
                }
            }
        }

        public override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            long mouseEventType = MouseDownEvent.TypeId();

            if (!textInputField.isReadOnly && evt.eventTypeId == mouseEventType && editorEngine.keyboardOnScreen == null)
            {
                textInputField.SyncTextEngine();
                textInputField.UpdateText(editorEngine.text);

                editorEngine.keyboardOnScreen = TouchScreenKeyboard.Open(textInputField.text,
                    TouchScreenKeyboardType.Default,
                    true,     // autocorrection
                    editorEngine.multiline,
                    textInputField.isPasswordField);

                if (editorEngine.keyboardOnScreen != null)
                {
                    PollTouchScreenKeyboard();
                }

                // Scroll offset might need to be updated
                editorEngine.UpdateScrollOffset();
                evt.StopPropagation();
            }
        }
    }
}
