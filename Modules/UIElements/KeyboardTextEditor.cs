// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal class KeyboardTextEditorEventHandler : TextEditorEventHandler
    {
        // used in tests
        internal bool m_Changed;

        // Drag
        bool m_Dragged;
        bool m_DragToPosition = true;
        bool m_PostponeMove;
        bool m_SelectAllOnMouseUp = true;

        string m_PreDrawCursorText;

        public KeyboardTextEditorEventHandler(TextEditorEngine editorEngine, TextInputFieldBase textInputField)
            : base(editorEngine, textInputField)
        {
        }

        public override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
            {
                OnMouseDown(evt as MouseDownEvent);
            }
            else if (evt.GetEventTypeId() == MouseUpEvent.TypeId())
            {
                OnMouseUp(evt as MouseUpEvent);
            }
            else if (evt.GetEventTypeId() == MouseMoveEvent.TypeId())
            {
                OnMouseMove(evt as MouseMoveEvent);
            }
            else if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                OnKeyDown(evt as KeyDownEvent);
            }
            else if (evt.GetEventTypeId() == IMGUIEvent.TypeId())
            {
                OnIMGUIEvent(evt as IMGUIEvent);
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            textInputField.SyncTextEngine();
            m_Changed = false;

            if (!textInputField.hasFocus)
            {
                editorEngine.m_HasFocus = true;

                editorEngine.MoveCursorToPosition_Internal(evt.localMousePosition, evt.button == (int)MouseButton.LeftMouse && evt.shiftKey);

                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    textInputField.TakeMouseCapture();
                }

                evt.StopPropagation();
            }
            else if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (evt.clickCount == 2 && textInputField.doubleClickSelectsWord)
                {
                    editorEngine.SelectCurrentWord();
                    editorEngine.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                    editorEngine.MouseDragSelectsWholeWords(true);
                    m_DragToPosition = false;
                }
                else if (evt.clickCount == 3 && textInputField.tripleClickSelectsLine)
                {
                    editorEngine.SelectCurrentParagraph();
                    editorEngine.MouseDragSelectsWholeWords(true);
                    editorEngine.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                    m_DragToPosition = false;
                }
                else
                {
                    editorEngine.MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
                    m_SelectAllOnMouseUp = false;
                }

                textInputField.TakeMouseCapture();
                evt.StopPropagation();
            }
            else if (evt.button == (int)MouseButton.RightMouse)
            {
                if (editorEngine.cursorIndex == editorEngine.selectIndex)
                {
                    editorEngine.MoveCursorToPosition_Internal(evt.localMousePosition, false);
                }
                m_SelectAllOnMouseUp = false;
                m_DragToPosition = false;
            }

            // Scroll offset might need to be updated
            editorEngine.UpdateScrollOffset();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (!textInputField.HasMouseCapture())
            {
                return;
            }

            textInputField.SyncTextEngine();
            m_Changed = false;

            if (m_Dragged && m_DragToPosition)
            {
                editorEngine.MoveSelectionToAltCursor();
            }
            else if (m_PostponeMove)
            {
                editorEngine.MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
            }
            else if (m_SelectAllOnMouseUp)
            {
                m_SelectAllOnMouseUp = false;
            }

            editorEngine.MouseDragSelectsWholeWords(false);

            textInputField.ReleaseMouseCapture();

            m_DragToPosition = true;
            m_Dragged = false;
            m_PostponeMove = false;

            evt.StopPropagation();

            // Scroll offset might need to be updated
            editorEngine.UpdateScrollOffset();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (!textInputField.HasMouseCapture())
            {
                return;
            }

            textInputField.SyncTextEngine();
            m_Changed = false;

            // FIXME: presing shift while dragging will change start of selection (alt cursor).
            // Also, adding to selection (with shift click) after a drag-select does not work: it clears the previous selection.
            if (!evt.shiftKey && editorEngine.hasSelection && m_DragToPosition)
            {
                editorEngine.MoveAltCursorToPosition(evt.localMousePosition);
            }
            else
            {
                if (evt.shiftKey)
                {
                    editorEngine.MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);
                }
                else
                {
                    editorEngine.SelectToPosition(evt.localMousePosition);
                }

                m_DragToPosition = false;
                m_SelectAllOnMouseUp = !editorEngine.hasSelection;
            }
            m_Dragged = true;

            evt.StopPropagation();

            // Scroll offset might need to be updated
            editorEngine.UpdateScrollOffset();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!textInputField.hasFocus)
                return;

            textInputField.SyncTextEngine();
            m_Changed = false;

            // Check for action keys.
            if (editorEngine.HandleKeyEvent(evt.imguiEvent))
            {
                if (textInputField.text != editorEngine.text)
                {
                    m_Changed = true;
                }
                evt.StopPropagation();
            }
            else
            {
                // Ignore tab & shift-tab in text fields
                if (evt.keyCode == KeyCode.Tab || evt.character == '\t')
                    return;

                evt.StopPropagation();

                char c = evt.character;

                if (c == '\n' && !editorEngine.multiline && !evt.altKey)
                {
                    return;
                }

                if (!textInputField.AcceptCharacter(c))
                {
                    return;
                }

                // Simplest test: only allow the character if the display font supports it.
                Font font = editorEngine.style.font;
                if ((font != null && font.HasCharacter(c)) || c == '\n')
                {
                    // Input event
                    editorEngine.Insert(c);
                    m_Changed = true;
                }
                // On windows, key presses also send events with keycode but no character. Eat them up here.
                else if (c == 0)
                {
                    // if we have a composition string, make sure we clear the previous selection.
                    if (!string.IsNullOrEmpty(Input.compositionString))
                    {
                        editorEngine.ReplaceSelection("");
                        m_Changed = true;
                    }
                }
            }

            if (m_Changed)
            {
                editorEngine.text = textInputField.CullString(editorEngine.text);
                textInputField.UpdateText(editorEngine.text);
            }

            // Scroll offset might need to be updated
            editorEngine.UpdateScrollOffset();
        }

        void OnIMGUIEvent(IMGUIEvent evt)
        {
            if (!textInputField.hasFocus)
                return;

            textInputField.SyncTextEngine();
            m_Changed = false;

            switch (evt.imguiEvent.type)
            {
                case EventType.ValidateCommand:
                    switch (evt.imguiEvent.commandName)
                    {
                        case EventCommandNames.Cut:
                        case EventCommandNames.Copy:
                            if (!editorEngine.hasSelection)
                                return;
                            break;
                        case EventCommandNames.Paste:
                            if (!editorEngine.CanPaste())
                                return;
                            break;
                        case EventCommandNames.SelectAll:
                        case EventCommandNames.Delete:
                            break;
                        case EventCommandNames.UndoRedoPerformed:
                            // TODO: ????? editor.text = text; --> see EditorGUI's DoTextField
                            break;
                    }
                    evt.StopPropagation();
                    break;

                case EventType.ExecuteCommand:
                    bool mayHaveChanged = false;
                    string oldText = editorEngine.text;

                    if (!textInputField.hasFocus)
                        return;

                    switch (evt.imguiEvent.commandName)
                    {
                        case EventCommandNames.OnLostFocus:
                            evt.StopPropagation();
                            return;
                        case EventCommandNames.Cut:
                            editorEngine.Cut();
                            mayHaveChanged = true;
                            break;
                        case EventCommandNames.Copy:
                            editorEngine.Copy();
                            evt.StopPropagation();
                            return;
                        case EventCommandNames.Paste:
                            editorEngine.Paste();
                            mayHaveChanged = true;
                            break;
                        case EventCommandNames.SelectAll:
                            editorEngine.SelectAll();
                            evt.StopPropagation();
                            return;
                        case EventCommandNames.Delete:
                            // This "Delete" command stems from a Shift-Delete in the text
                            // On Windows, Shift-Delete in text does a cut whereas on Mac, it does a delete.
                            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                                editorEngine.Delete();
                            else
                                editorEngine.Cut();
                            mayHaveChanged = true;
                            break;
                    }

                    if (mayHaveChanged)
                    {
                        if (oldText != editorEngine.text)
                            m_Changed = true;

                        evt.StopPropagation();
                    }
                    break;
            }

            if (m_Changed)
            {
                editorEngine.text = textInputField.CullString(editorEngine.text);
                textInputField.UpdateText(editorEngine.text);
                evt.StopPropagation();
            }

            // Scroll offset might need to be updated
            editorEngine.UpdateScrollOffset();
        }

        public void PreDrawCursor(string newText)
        {
            textInputField.SyncTextEngine();

            m_PreDrawCursorText = editorEngine.text;

            int cursorPos = editorEngine.cursorIndex;

            if (!string.IsNullOrEmpty(Input.compositionString))
            {
                editorEngine.text = newText.Substring(0, editorEngine.cursorIndex) + Input.compositionString + newText.Substring(editorEngine.selectIndex);
                cursorPos += Input.compositionString.Length;
            }
            else
            {
                editorEngine.text = newText;
            }

            editorEngine.text = textInputField.CullString(editorEngine.text);
            cursorPos = Math.Min(cursorPos, editorEngine.text.Length);

            editorEngine.graphicalCursorPos = editorEngine.style.GetCursorPixelPosition(editorEngine.localPosition, new GUIContent(editorEngine.text), cursorPos);
        }

        public void PostDrawCursor()
        {
            editorEngine.text = m_PreDrawCursorText;
        }
    }
}
