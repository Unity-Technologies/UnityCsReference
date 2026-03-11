// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal class TextEditingManipulator
    {
        private readonly TextElement m_TextElement;

        private TextEditorEventHandler m_TouchScreenEditingEventHandler;
        internal TextEditorEventHandler touchScreenEditingEventHandler
        {
            get => m_TouchScreenEditingEventHandler;
            set
            {
                if (m_TouchScreenEditingEventHandler == value)
                    return;

                m_TouchScreenEditingEventHandler?.UnregisterCallbacksFromTarget(m_TextElement);
                m_TouchScreenEditingEventHandler = value;
                m_TouchScreenEditingEventHandler?.RegisterCallbacksOnTarget(m_TextElement);
            }
        }

        private TextEditorEventHandler m_KeyboardEditingEventHandler;
        internal TextEditorEventHandler keyboardEditingEventHandler
        {
            get => m_KeyboardEditingEventHandler;
            set
            {
                if (m_KeyboardEditingEventHandler == value)
                    return;

                m_KeyboardEditingEventHandler?.UnregisterCallbacksFromTarget(m_TextElement);
                m_KeyboardEditingEventHandler = value;
                m_KeyboardEditingEventHandler?.RegisterCallbacksOnTarget(m_TextElement);
            }
        }

        internal TextEditingUtilities editingUtilities;

        private IVisualElementScheduledItem m_HardwareKeyboardPoller = null;

        public TextEditingManipulator(TextElement textElement)
        {
            m_TextElement = textElement;
            editingUtilities = new TextEditingUtilities(textElement.selectingManipulator.m_SelectingUtilities, textElement.uitkTextHandle, textElement.text);
            UpdateTextEditorEventHandler();
        }

        public void Reset()
        {
            touchScreenEditingEventHandler = null;
            keyboardEditingEventHandler = null;
        }

        private bool touchScreenTextFieldChanged
        {
            get
            {
                if (touchScreenCanBeUsed != (touchScreenEditingEventHandler != null) || keyboardCanBeUsed != (keyboardEditingEventHandler != null))
                {
                    return true;
                }
                return false;
            }
        }

        private bool touchScreenCanBeUsed => (editingUtilities?.TouchScreenKeyboardCanBeUsed() ?? false) && !m_TextElement.edition.hideSoftKeyboard;
        private bool keyboardCanBeUsed => m_TextElement.edition.hideSoftKeyboard || (editingUtilities?.PhysicalKeyboardCanBeUsed() ?? true);

        private void UpdateTextEditorEventHandler()
        {
            if (touchScreenCanBeUsed)
            {
                touchScreenEditingEventHandler = new TouchScreenTextEditorEventHandler(m_TextElement, editingUtilities);
            }
            else
            {
                touchScreenEditingEventHandler = null;
            }
            if (keyboardCanBeUsed)
            {
                keyboardEditingEventHandler = new KeyboardTextEditorEventHandler(m_TextElement, editingUtilities);
            }
            else
            {
                keyboardEditingEventHandler = null;
            }
        }

        internal void HandleEventBubbleUp(EventBase evt)
        {
            if (m_TextElement.edition.isReadOnly)
                return;

            if (evt is BlurEvent)
            {
                m_TextElement.uitkTextHandle.RemoveFromPermanentCache();
            }
            else if ((evt is not PointerMoveEvent && evt is not MouseMoveEvent) || m_TextElement.selectingManipulator.isClicking)
            {
                m_TextElement.uitkTextHandle.AddToPermanentCacheAndGenerateMesh();
            }

            switch (evt)
            {
                case FocusInEvent:
                    OnFocusInEvent();
                    break;
                case FocusOutEvent:
                    OnFocusOutEvent();
                    break;
            }

            keyboardEditingEventHandler?.HandleEventBubbleUp(evt);
            touchScreenEditingEventHandler?.HandleEventBubbleUp(evt);
        }

        void OnFocusInEvent()
        {
            m_TextElement.edition.SaveValueAndText();
            // Update the selectedTextElement when an InputField takes focus.
            m_TextElement.focusController.selectedTextElement = m_TextElement;

            // When this input field receives focus, make sure the correct text editor is initialized
            // (i.e. hardware keyboard or touchscreen keyboard).
            if (touchScreenTextFieldChanged)
                UpdateTextEditorEventHandler();

            // When focused and the keyboard availability changes, make sure the correct text editor is
            // initialized under these new conditions and un-focus this input field.
            if (m_HardwareKeyboardPoller == null)
            {
                m_HardwareKeyboardPoller = m_TextElement.schedule.Execute(() =>
                {
                    if (touchScreenTextFieldChanged)
                    {
                        UpdateTextEditorEventHandler();
                        m_TextElement.Blur();
                    }
                }).Every(250);
            }
            else
            {
                m_HardwareKeyboardPoller.Resume();
            }
        }

        void OnFocusOutEvent()
        {
            m_HardwareKeyboardPoller?.Pause();
            editingUtilities.OnBlur();
        }
    }
}
