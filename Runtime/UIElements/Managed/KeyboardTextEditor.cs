// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal class KeyboardTextEditor : TextEditor
    {
        internal bool m_Changed;

        // Drag
        bool m_Dragged;
        bool m_DragToPosition = true;
        bool m_PostPoneMove;
        bool m_SelectAllOnMouseUp = true;

        string m_PreDrawCursorText;

        public KeyboardTextEditor(TextField textField)
            : base(textField)
        {
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            SyncTextEditor();
            m_Changed = false;

            EventPropagation result = EventPropagation.Continue;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    result = DoMouseDown(evt);
                    break;

                case EventType.MouseDrag:
                    result = DoMouseDrag(evt);
                    break;

                case EventType.MouseUp:
                    result = DoMouseUp(evt);
                    break;

                case EventType.KeyDown:
                    result = DoKeyDown(evt);
                    break;

                case EventType.ExecuteCommand:
                    result = DoExecuteCommand(evt);
                    break;

                case EventType.ValidateCommand:
                    result = DoValidateCommand(evt);
                    break;
            }

            if (m_Changed)
            {
                // Pre-cull string to maxLength.
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                textField.text = text;
                result = EventPropagation.Stop;
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();

            return result;
        }

        EventPropagation DoMouseDown(Event evt)
        {
            this.TakeCapture();

            if (!m_HasFocus)
            {
                m_HasFocus = true;

                MoveCursorToPosition_Internal(evt.mousePosition, evt.shift);

                return EventPropagation.Stop;
            }

            if (evt.clickCount == 2 && doubleClickSelectsWord)
            {
                SelectCurrentWord();
                DblClickSnap(DblClickSnapping.WORDS);
                MouseDragSelectsWholeWords(true);
                m_DragToPosition = false;
            }
            else if (evt.clickCount == 3 && tripleClickSelectsLine)
            {
                SelectCurrentParagraph();
                MouseDragSelectsWholeWords(true);
                DblClickSnap(DblClickSnapping.PARAGRAPHS);
                m_DragToPosition = false;
            }
            else
            {
                MoveCursorToPosition_Internal(evt.mousePosition, evt.shift);
                m_SelectAllOnMouseUp = false;
            }

            return EventPropagation.Stop;
        }

        EventPropagation DoMouseUp(Event evt)
        {
            if (!this.HasCapture())
                return EventPropagation.Continue;

            if (m_Dragged && m_DragToPosition)
            {
                MoveSelectionToAltCursor();
            }
            else if (m_PostPoneMove)
            {
                MoveCursorToPosition_Internal(evt.mousePosition, evt.shift);
            }
            else if (m_SelectAllOnMouseUp)
            {
                m_SelectAllOnMouseUp = false;
            }

            MouseDragSelectsWholeWords(false);

            this.ReleaseCapture();

            m_DragToPosition = true;
            m_Dragged = false;
            m_PostPoneMove = false;

            return EventPropagation.Stop;
        }

        EventPropagation DoMouseDrag(Event evt)
        {
            if (!this.HasCapture())
                return EventPropagation.Continue;

            if (!evt.shift && hasSelection && m_DragToPosition)
            {
                MoveAltCursorToPosition(Event.current.mousePosition);
            }
            else
            {
                if (evt.shift)
                {
                    MoveCursorToPosition_Internal(evt.mousePosition, evt.shift);
                }
                else
                {
                    SelectToPosition(evt.mousePosition);
                }

                m_DragToPosition = false;
                m_SelectAllOnMouseUp = !hasSelection;
            }
            m_Dragged = true;

            return EventPropagation.Stop;
        }

        EventPropagation DoKeyDown(Event evt)
        {
            if (!textField.hasFocus)
                return EventPropagation.Continue;

            if (HandleKeyEvent(evt))
            {
                m_Changed = true;
                textField.text = text;
                return EventPropagation.Stop;
            }

            // Ignore tab & shift-tab in text fields
            if (evt.keyCode == KeyCode.Tab || evt.character == '\t')
                return EventPropagation.Continue;

            char c = evt.character;

            if (c == '\n' && !multiline && !evt.alt)
                return EventPropagation.Continue;

            // Simplest test: only allow the character if the display font supports it.
            if ((textField.font != null && textField.font.HasCharacter(c)) || c == '\n')
            {
                Insert(c);
                m_Changed = true;
                return EventPropagation.Continue;
            }

            // On windows, key presses also send events with keycode but no character. Eat them up here.
            if (c == 0)
            {
                // if we have a composition string, make sure we clear the previous selection.
                if (!string.IsNullOrEmpty(Input.compositionString))
                {
                    ReplaceSelection("");
                    m_Changed = true;
                }
                return EventPropagation.Stop;
            }
            return EventPropagation.Continue;
        }

        EventPropagation DoValidateCommand(Event evt)
        {
            if (!textField.hasFocus)
                return EventPropagation.Continue;

            switch (evt.commandName)
            {
                case "Cut":
                case "Copy":
                    if (!hasSelection)
                        return EventPropagation.Continue;
                    break;
                case "Paste":
                    if (!CanPaste())
                        return EventPropagation.Continue;
                    break;
                case "SelectAll":
                case "Delete":
                    break;
                case "UndoRedoPerformed":
                    // TODO: ????? editor.text = text; --> see EditorGUI's DoTextField
                    break;
            }

            return EventPropagation.Stop;
        }

        EventPropagation DoExecuteCommand(Event evt)
        {
            bool mayHaveChanged = false;
            string oldText = text;

            if (!textField.hasFocus)
                return EventPropagation.Continue;

            switch (evt.commandName)
            {
                case "OnLostFocus":
                    return EventPropagation.Stop;
                case "Cut":
                    Cut();
                    mayHaveChanged = true;
                    break;
                case "Copy":
                    Copy();
                    return EventPropagation.Stop;
                case "Paste":
                    Paste();
                    mayHaveChanged = true;
                    break;
                case "SelectAll":
                    SelectAll();
                    return EventPropagation.Stop;
                case "Delete":
                    // This "Delete" command stems from a Shift-Delete in the text
                    // On Windows, Shift-Delete in text does a cut whereas on Mac, it does a delete.
                    if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                        Delete();
                    else
                        Cut();
                    mayHaveChanged = true;
                    break;
            }

            if (mayHaveChanged)
            {
                if (oldText != text)
                    m_Changed = true;
                return EventPropagation.Stop;
            }

            return EventPropagation.Continue;
        }

        public void PreDrawCursor(string newText)
        {
            SyncTextEditor();

            m_PreDrawCursorText = text;

            int cursorPos = cursorIndex;

            if (!string.IsNullOrEmpty(Input.compositionString))
            {
                text = newText.Substring(0, cursorIndex) + Input.compositionString + newText.Substring(selectIndex);
                cursorPos += Input.compositionString.Length;
            }
            else
            {
                text = newText;
            }

            if (maxLength >= 0 && text != null && text.Length > maxLength)
            {
                text = text.Substring(0, maxLength);
                cursorPos = Math.Min(cursorPos, maxLength - 1);
            }

            graphicalCursorPos = style.GetCursorPixelPosition(localPosition, new GUIContent(text), cursorPos);
        }

        public void PostDrawCursor()
        {
            text = m_PreDrawCursorText;
        }
    }
}
