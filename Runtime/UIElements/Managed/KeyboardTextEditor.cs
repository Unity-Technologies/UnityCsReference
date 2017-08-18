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

        protected override void RegisterCallbacksOnTarget()
        {
            base.RegisterCallbacksOnTarget();

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            base.UnregisterCallbacksFromTarget();

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            SyncTextEditor();
            m_Changed = false;

            target.TakeCapture();

            if (!m_HasFocus)
            {
                m_HasFocus = true;

                MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
                evt.StopPropagation();
            }
            else
            {
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
                    MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
                    m_SelectAllOnMouseUp = false;
                }

                evt.StopPropagation();
            }

            if (m_Changed)
            {
                // Pre-cull string to maxLength.
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                textField.text = text;
                textField.TextFieldChanged();
                evt.StopPropagation();
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!target.HasCapture())
                return;

            SyncTextEditor();
            m_Changed = false;

            if (m_Dragged && m_DragToPosition)
            {
                MoveSelectionToAltCursor();
            }
            else if (m_PostPoneMove)
            {
                MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
            }
            else if (m_SelectAllOnMouseUp)
            {
                m_SelectAllOnMouseUp = false;
            }

            MouseDragSelectsWholeWords(false);

            target.ReleaseCapture();

            m_DragToPosition = true;
            m_Dragged = false;
            m_PostPoneMove = false;

            evt.StopPropagation();

            if (m_Changed)
            {
                // Pre-cull string to maxLength.
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                textField.text = text;
                textField.TextFieldChanged();
                evt.StopPropagation();
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!target.HasCapture())
                return;

            SyncTextEditor();
            m_Changed = false;

            // FIXME: presing shift while dragging will change start of selection (alt cursor).
            // Also, adding to selection (with shift click) after a drag-select does not work: it clears the previous selection.
            if (!evt.shiftKey && hasSelection && m_DragToPosition)
            {
                MoveAltCursorToPosition(evt.localMousePosition);
            }
            else
            {
                if (evt.shiftKey)
                {
                    MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
                }
                else
                {
                    SelectToPosition(evt.localMousePosition);
                }

                m_DragToPosition = false;
                m_SelectAllOnMouseUp = !hasSelection;
            }
            m_Dragged = true;

            evt.StopPropagation();

            if (m_Changed)
            {
                // Pre-cull string to maxLength.
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                textField.text = text;
                textField.TextFieldChanged();
                evt.StopPropagation();
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!textField.hasFocus)
                return;

            SyncTextEditor();
            m_Changed = false;

            if (HandleKeyEvent(evt.imguiEvent))
            {
                m_Changed = true;
                textField.text = text;
                evt.StopPropagation();
            }
            else
            {
                // Ignore tab & shift-tab in text fields
                if (evt.keyCode == KeyCode.Tab || evt.character == '\t')
                    return;

                char c = evt.character;

                if (c == '\n' && !multiline && !evt.altKey)
                {
                    textField.TextFieldChangeValidated();
                    return;
                }

                // Simplest test: only allow the character if the display font supports it.
                Font font = textField.editor.style.font;
                if ((font != null && font.HasCharacter(c)) || c == '\n')
                {
                    Insert(c);
                    m_Changed = true;
                }
                // On windows, key presses also send events with keycode but no character. Eat them up here.
                else if (c == 0)
                {
                    // if we have a composition string, make sure we clear the previous selection.
                    if (!string.IsNullOrEmpty(Input.compositionString))
                    {
                        ReplaceSelection("");
                        m_Changed = true;
                    }
                    evt.StopPropagation();
                }
            }

            if (m_Changed)
            {
                // Pre-cull string to maxLength.
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                textField.text = text;
                textField.TextFieldChanged();
                evt.StopPropagation();
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();
        }

        void OnIMGUIEvent(IMGUIEvent evt)
        {
            if (!textField.hasFocus)
                return;

            SyncTextEditor();
            m_Changed = false;

            switch (evt.imguiEvent.type)
            {
                case EventType.ValidateCommand:
                    switch (evt.imguiEvent.commandName)
                    {
                        case "Cut":
                        case "Copy":
                            if (!hasSelection)
                                return;
                            break;
                        case "Paste":
                            if (!CanPaste())
                                return;
                            break;
                        case "SelectAll":
                        case "Delete":
                            break;
                        case "UndoRedoPerformed":
                            // TODO: ????? editor.text = text; --> see EditorGUI's DoTextField
                            break;
                    }
                    evt.StopPropagation();
                    break;

                case EventType.ExecuteCommand:
                    bool mayHaveChanged = false;
                    string oldText = text;

                    if (!textField.hasFocus)
                        return;

                    switch (evt.imguiEvent.commandName)
                    {
                        case "OnLostFocus":
                            evt.StopPropagation();
                            return;
                        case "Cut":
                            Cut();
                            mayHaveChanged = true;
                            break;
                        case "Copy":
                            Copy();
                            evt.StopPropagation();
                            return;
                        case "Paste":
                            Paste();
                            mayHaveChanged = true;
                            break;
                        case "SelectAll":
                            SelectAll();
                            evt.StopPropagation();
                            return;
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

                        evt.StopPropagation();
                    }
                    break;
            }

            if (m_Changed)
            {
                // Pre-cull string to maxLength.
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                textField.text = text;
                textField.TextFieldChanged();
                evt.StopPropagation();
            }

            // Scroll offset might need to be updated
            UpdateScrollOffset();
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
