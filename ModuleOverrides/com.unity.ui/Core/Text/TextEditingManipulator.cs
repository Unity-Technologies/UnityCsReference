// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal class TextEditingManipulator
    {
        TextElement m_TextElement;
        internal TextEditorEventHandler editingEventHandler;
        internal TextEditingUtilities editingUtilities;

        private bool m_TouchScreenTextFieldInitialized;
        private bool touchScreenTextFieldChanged
        {
            get { return m_TouchScreenTextFieldInitialized != touchScreenTextField; }
        }
        internal bool touchScreenTextField
        {
            get { return TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed; }
        }

        private IVisualElementScheduledItem m_HardwareKeyboardPoller = null;

        public TextEditingManipulator(TextElement textElement)
        {
            m_TextElement = textElement;
            editingUtilities = new TextEditingUtilities(textElement.selectingManipulator.m_SelectingUtilities, textElement.uitkTextHandle.textHandle);
            InitTextEditorEventHandler();
        }

        private void InitTextEditorEventHandler()
        {
            m_TouchScreenTextFieldInitialized = touchScreenTextField;
            if (m_TouchScreenTextFieldInitialized)
            {
                editingEventHandler = new TouchScreenTextEditorEventHandler(m_TextElement, editingUtilities);
            }
            else
            {
                editingEventHandler = new KeyboardTextEditorEventHandler(m_TextElement, editingUtilities);
            }
        }

        internal void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            if (m_TextElement.edition.isReadOnly)
                return;

            switch (evt)
            {
                case FocusInEvent fie:
                    OnFocusInEvent(fie);
                    break;
                case FocusOutEvent foe:
                    OnFocusOutEvent(foe);
                    break;
            }

            editingEventHandler?.ExecuteDefaultActionAtTarget(evt);
        }

        void OnFocusInEvent(FocusInEvent evt)
        {
            m_TextElement.edition.SaveValueAndText();
            // Update the selectedTextElement when an InputField takes focus.
            m_TextElement.focusController.selectedTextElement = m_TextElement;

            // When this input field receives focus, make sure the correct text editor is initialized
            // (i.e. hardware keyboard or touchscreen keyboard).
            if (touchScreenTextFieldChanged)
                InitTextEditorEventHandler();

            // When focused and the keyboard availability changes, make sure the correct text editor is
            // initialized under these new conditions and un-focus this input field.
            if (m_HardwareKeyboardPoller == null)
            {
                m_HardwareKeyboardPoller = m_TextElement.schedule.Execute(() =>
                {
                    if (touchScreenTextFieldChanged)
                    {
                        InitTextEditorEventHandler();
                        m_TextElement.Blur();
                    }
                }).Every(250);
            }
            else
            {
                m_HardwareKeyboardPoller.Resume();
            }
        }

        void OnFocusOutEvent(FocusOutEvent evt)
        {
            m_HardwareKeyboardPoller?.Pause();

            editingUtilities.OnBlur();
        }
    }
}
