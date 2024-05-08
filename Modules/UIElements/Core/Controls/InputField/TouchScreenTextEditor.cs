// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal class TouchScreenTextEditorEventHandler : TextEditorEventHandler
    {
        private IVisualElementScheduledItem m_TouchKeyboardPoller = null;
        private bool m_TouchKeyboardAllowsInPlaceEditing = false;
        private bool m_IsClicking = false;

        // For UI Test Framework.
        internal static long Frame { get; private set; }
        // For UI Test Framework.
        internal static TouchScreenKeyboard activeTouchScreenKeyboard { get; private set; }

        public TouchScreenTextEditorEventHandler(TextElement textElement, TextEditingUtilities editingUtilities)
            : base(textElement, editingUtilities) {}

        void PollTouchScreenKeyboard()
        {
            m_TouchKeyboardAllowsInPlaceEditing = TouchScreenKeyboard.isInPlaceEditingAllowed;

            if (TouchScreenKeyboard.isSupported && !m_TouchKeyboardAllowsInPlaceEditing)
            {
                if (m_TouchKeyboardPoller == null)
                    m_TouchKeyboardPoller = textElement?.schedule.Execute(DoPollTouchScreenKeyboard).Every(100);
                else
                    m_TouchKeyboardPoller.Resume();
            }
        }

        void DoPollTouchScreenKeyboard()
        {
            ++Frame;

            if (editingUtilities.TouchScreenKeyboardShouldBeUsed())
            {
                if (textElement.m_TouchScreenKeyboard == null)
                    return;

                var edition = textElement.edition;
                var touchKeyboard = textElement.m_TouchScreenKeyboard;
                var touchKeyboardText = touchKeyboard.text;

                if (touchKeyboard.status != TouchScreenKeyboard.Status.Visible)
                {
                    if (touchKeyboard.status == TouchScreenKeyboard.Status.Canceled)
                    {
                        edition.RestoreValueAndText();
                    }
                    else
                    {
                        //Ensure that text is updated after closing the keyboard as some platforms only send input after it is closed
                        touchKeyboardText = touchKeyboard.text;
                        if (editingUtilities.text != touchKeyboardText)
                        {
                            edition.UpdateText(touchKeyboardText);
                            textElement.uitkTextHandle.Update();
                        }
                    }

                    CloseTouchScreenKeyboard();

                    if (edition.isDelayed)
                        edition.UpdateValueFromText?.Invoke();

                    edition.UpdateTextFromValue?.Invoke();
                    textElement.Blur();

                    return;
                }

                // Stop processing if nothing has changed - UpdateText affects performance, even if we have not
                // modified the string therefore we try to use it sparingly
                if (editingUtilities.text == touchKeyboardText)
                    return;

                // When we are utilizing our own input field, we need to do some extra work such as making
                // sure the caret is at the right position, characters being entered are valid, any extra
                // validation should be in place, etc.
                if (edition.hideMobileInput)
                {
                    if (editingUtilities.text != touchKeyboardText)
                    {
                        var changed = false;
                        editingUtilities.text = "";

                        foreach (var character in touchKeyboardText)
                        {
                            if (!edition.AcceptCharacter(character))
                                return;

                            if (character != 0)
                            {
                                editingUtilities.text += character;
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            UpdateStringPositionFromKeyboard();
                        }

                        edition.UpdateText(editingUtilities.text);
                        // UpdateScrollOffset needs the new geometry of the text to compute the new scrollOffset.
                        textElement.uitkTextHandle.ComputeSettingsAndUpdate();
                    }
                    else if (!m_IsClicking && touchKeyboard != null && touchKeyboard.canGetSelection)
                    {
                        UpdateStringPositionFromKeyboard();
                    }
                }
                // When using the native OS's input field to update the input field value, we just want to sync the
                // soft keyboard's value with our text value.
                else {
                    edition.UpdateText(touchKeyboardText);

                    // UpdateScrollOffset needs the new geometry of the text to compute the new scrollOffset.
                    textElement.uitkTextHandle.ComputeSettingsAndUpdate();
                }

                textElement.edition.UpdateScrollOffset?.Invoke(false);
            }
            else
            {
                // TouchScreenKeyboard should no longer be used, presumably because a hardware keyboard is now available.
                CloseTouchScreenKeyboard();
            }
        }

        private void UpdateStringPositionFromKeyboard()
        {
            if (textElement.m_TouchScreenKeyboard == null)
                return;

            var selectionRange = textElement.m_TouchScreenKeyboard.selection;
            var selectionStart = selectionRange.start;
            var selectionEnd = selectionRange.end;

            if (textElement.selection.selectIndex != selectionStart)
                textElement.selection.selectIndex = selectionStart;

            if (textElement.selection.cursorIndex != selectionEnd)
                textElement.selection.cursorIndex = selectionEnd;
        }

        private void CloseTouchScreenKeyboard()
        {
            if (textElement.m_TouchScreenKeyboard != null)
            {
                textElement.m_TouchScreenKeyboard.active = false;
                textElement.m_TouchScreenKeyboard = null;
                m_TouchKeyboardPoller?.Pause();
                TouchScreenKeyboard.hideInput = true;
            }
            activeTouchScreenKeyboard = null;
        }

        private void OpenTouchScreenKeyboard()
        {
            var edition = textElement.edition;
            TouchScreenKeyboard.hideInput = edition.hideMobileInput;

            textElement.m_TouchScreenKeyboard = TouchScreenKeyboard.Open(
                textElement.text,
                edition.keyboardType,
                !edition.isPassword && edition.autoCorrection,
                edition.multiline,
                edition.isPassword
            );

            // Highlights the soft keyboard's text (if applicable) when using inPlaceEditing mode
            if (edition.hideMobileInput)
            {
                var selectIndex = textElement.selection.selectIndex;
                var cursorIndex = textElement.selection.cursorIndex;
                var length = selectIndex < cursorIndex ? cursorIndex - selectIndex : selectIndex - cursorIndex;
                var start = selectIndex < cursorIndex ? selectIndex : cursorIndex;
                textElement.m_TouchScreenKeyboard.selection = new RangeInt(start, length);
            }
            // Otherwise place the cursor at the end of the string
            else
            {
                textElement.m_TouchScreenKeyboard.selection = new RangeInt(textElement.m_TouchScreenKeyboard.text?.Length ?? 0, 0);
            }

            activeTouchScreenKeyboard = textElement.m_TouchScreenKeyboard;
        }

        public override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (!editingUtilities.TouchScreenKeyboardShouldBeUsed() || textElement.edition.isReadOnly)
                return;

            switch (evt)
            {
                case PointerDownEvent:
                    OnPointerDownEvent();
                    break;
                case PointerUpEvent pue:
                    OnPointerUpEvent(pue);
                    break;
                case FocusInEvent:
                    OnFocusInEvent();
                    break;
                case FocusOutEvent foe:
                    OnFocusOutEvent(foe);
                    break;
            }
        }

        private void OnPointerDownEvent()
        {
            m_IsClicking = true;

            // Update the soft keyboard's input field's cursor with the selection's cursor;
            if (textElement.m_TouchScreenKeyboard != null && textElement.edition.hideMobileInput)
            {
                var selectionStart = textElement.selection.cursorIndex;
                var softKeyboardStringLength = textElement.m_TouchScreenKeyboard.text?.Length ?? 0;

                if (selectionStart < 0)
                    selectionStart = 0;

                if (selectionStart > softKeyboardStringLength)
                    selectionStart = softKeyboardStringLength;

                textElement.m_TouchScreenKeyboard.selection = new RangeInt(selectionStart, 0);
            }
        }

        private void OnPointerUpEvent(PointerUpEvent evt)
        {
            m_IsClicking = false;
            evt.StopPropagation();
        }

        private void OnFocusInEvent()
        {
            if (textElement.m_TouchScreenKeyboard != null)
                return;

            OpenTouchScreenKeyboard();

            if (textElement.m_TouchScreenKeyboard != null)
                PollTouchScreenKeyboard();

            textElement.edition.SaveValueAndText();
            textElement.edition.UpdateScrollOffset?.Invoke(false);
        }

        private void OnFocusOutEvent(FocusOutEvent evt)
        {
            var currentFocusedTextElement = (TextElement)evt.target;

            // Expected to be NULL when losing focus
            var pendingFocusedTextElement =
                currentFocusedTextElement.focusController.m_LastPendingFocusedElement as TextElement;

            // Adds the ability to keep the soft keyboard open when clicking into another input field only if,
            // the keyboard type, multiline or hide mobile input are not the same - otherwise we need to
            // re-instantiate the keyboard (by closing and reopening it), if not, it's going to start behaving
            // as it never changed.
            if (pendingFocusedTextElement == currentFocusedTextElement ||
                pendingFocusedTextElement == null ||
                pendingFocusedTextElement.edition.keyboardType != currentFocusedTextElement.edition.keyboardType ||
                pendingFocusedTextElement.edition.multiline != currentFocusedTextElement.edition.multiline ||
                pendingFocusedTextElement.edition.hideMobileInput != currentFocusedTextElement.edition.hideMobileInput)
            {
                CloseTouchScreenKeyboard();
            }
            else
            {
                // Partially dismiss the keyboard
                textElement.m_TouchScreenKeyboard = null;
                m_TouchKeyboardPoller?.Pause();
            }

            if (textElement.edition.isDelayed)
                textElement.edition.UpdateValueFromText?.Invoke();

            textElement.edition.UpdateTextFromValue?.Invoke();
        }

    }
}
