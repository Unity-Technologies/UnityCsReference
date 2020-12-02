using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class KeyboardTextEditorEventHandler : TextEditorEventHandler
    {
        // used in tests
        internal bool m_Changed;

        // Drag
        bool m_Dragged;
        bool m_DragToPosition;
        bool m_PostponeMove;
        bool m_SelectAllOnMouseUp = false;

        string m_PreDrawCursorText;

        public KeyboardTextEditorEventHandler(TextEditorEngine editorEngine, ITextInputField textInputField)
            : base(editorEngine, textInputField)
        {
        }

        public override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.eventTypeId == FocusEvent.TypeId())
            {
                OnFocus(evt as FocusEvent);
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                OnBlur(evt as BlurEvent);
            }
            else if (evt.eventTypeId == MouseDownEvent.TypeId())
            {
                OnMouseDown(evt as MouseDownEvent);
            }
            else if (evt.eventTypeId == MouseUpEvent.TypeId())
            {
                OnMouseUp(evt as MouseUpEvent);
            }
            else if (evt.eventTypeId == MouseMoveEvent.TypeId())
            {
                OnMouseMove(evt as MouseMoveEvent);
            }
            else if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                OnKeyDown(evt as KeyDownEvent);
            }
            else if (evt.eventTypeId == ValidateCommandEvent.TypeId())
            {
                OnValidateCommandEvent(evt as ValidateCommandEvent);
            }
            else if (evt.eventTypeId == ExecuteCommandEvent.TypeId())
            {
                OnExecuteCommandEvent(evt as ExecuteCommandEvent);
            }
        }

        void OnFocus(FocusEvent _)
        {
            GUIUtility.imeCompositionMode = IMECompositionMode.On;
            m_DragToPosition = false;

            // If focus was given to this element from a mouse click or a Panel.Focus call, allow select on mouse up.
            if (PointerDeviceState.GetPressedButtons(PointerId.mousePointerId) != 0 ||
                (textInputField as VisualElement)?.panel.contextType == ContextType.Editor && Event.current == null)
                m_SelectAllOnMouseUp = true;
        }

        void OnBlur(BlurEvent _)
        {
            GUIUtility.imeCompositionMode = IMECompositionMode.Auto;
        }

        //TODO: replace by PointerDownEvent
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
                    textInputField.CaptureMouse();
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
                }

                textInputField.CaptureMouse();
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
                editorEngine.SelectAll();
                m_SelectAllOnMouseUp = false;
            }

            editorEngine.MouseDragSelectsWholeWords(false);

            textInputField.ReleaseMouse();

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

        private readonly Event m_ImguiEvent = new Event();
        void OnKeyDown(KeyDownEvent evt)
        {
            if (!textInputField.hasFocus)
                return;

            textInputField.SyncTextEngine();
            m_Changed = false;

            evt.GetEquivalentImguiEvent(m_ImguiEvent);

            // Check for action keys.
            if (editorEngine.HandleKeyEvent(m_ImguiEvent, textInputField.isReadOnly))
            {
                if (textInputField.text != editorEngine.text)
                {
                    m_Changed = true;
                }
                evt.StopPropagation();
            }
            else
            {
                char c = evt.character;

                // Ignore tab & shift-tab in single-line text fields
                if (!editorEngine.multiline && (evt.keyCode == KeyCode.Tab || c == '\t'))
                    return;

                // Ignore modifier+tab in multiline text fields
                if (editorEngine.multiline && (evt.keyCode == KeyCode.Tab || c == '\t') && evt.modifiers != EventModifiers.None)
                {
                    return;
                }

                // Ignore command and control keys, but not AltGr characters
                if (evt.actionKey && !(evt.altKey && c != '\0'))
                    return;

                evt.StopPropagation();

                if (c == '\n' && !editorEngine.multiline && !evt.altKey)
                {
                    return;
                }

                // When the newline character is sent, we have to check if the shift key is down also...
                // In the multiline case, this is like a return on a single line
                if (c == '\n' && editorEngine.multiline && evt.shiftKey)
                {
                    return;
                }

                if (!textInputField.AcceptCharacter(c))
                {
                    return;
                }

                // Simplest test: only allow the character if the display font supports it.
                Font font = editorEngine.style.font;
                if (font != null && font.HasCharacter(c) || c == '\n' || c == '\t')
                {
                    // Input event
                    editorEngine.Insert(c);
                    m_Changed = true;
                }
                // On windows, key presses also send events with keycode but no character. Eat them up here.
                else if (c == 0)
                {
                    // if we have a composition string, make sure we clear the previous selection.
                    if (!string.IsNullOrEmpty(GUIUtility.compositionString))
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

        void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (!textInputField.hasFocus)
                return;

            textInputField.SyncTextEngine();
            m_Changed = false;

            switch (evt.commandName)
            {
                case EventCommandNames.Cut:
                    if (!editorEngine.hasSelection || textInputField.isReadOnly)
                        return;
                    break;
                case EventCommandNames.Copy:
                    if (!editorEngine.hasSelection)
                        return;
                    break;
                case EventCommandNames.Paste:
                    if (!editorEngine.CanPaste() || textInputField.isReadOnly)
                        return;
                    break;
                case EventCommandNames.SelectAll:
                    break;
                case EventCommandNames.Delete:
                    if (textInputField.isReadOnly)
                        return;
                    break;
                case EventCommandNames.UndoRedoPerformed:
                    // TODO: ????? editor.text = text; --> see EditorGUI's DoTextField
                    break;
            }
            evt.StopPropagation();
        }

        void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            if (!textInputField.hasFocus)
                return;

            textInputField.SyncTextEngine();
            m_Changed = false;

            bool mayHaveChanged = false;
            string oldText = editorEngine.text;

            if (!textInputField.hasFocus)
                return;

            switch (evt.commandName)
            {
                case EventCommandNames.OnLostFocus:
                    evt.StopPropagation();
                    return;
                case EventCommandNames.Cut:
                    if (!textInputField.isReadOnly)
                    {
                        editorEngine.Cut();
                        mayHaveChanged = true;
                    }
                    break;
                case EventCommandNames.Copy:
                    editorEngine.Copy();
                    evt.StopPropagation();
                    return;
                case EventCommandNames.Paste:
                    if (!textInputField.isReadOnly)
                    {
                        editorEngine.Paste();
                        mayHaveChanged = true;
                    }
                    break;
                case EventCommandNames.SelectAll:
                    editorEngine.SelectAll();
                    evt.StopPropagation();
                    return;
                case EventCommandNames.Delete:
                    if (!textInputField.isReadOnly)
                    {
                        // This "Delete" command stems from a Shift-Delete in the text
                        // On Windows, Shift-Delete in text does a cut whereas on Mac, it does a delete.
                        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                            editorEngine.Delete();
                        else
                            editorEngine.Cut();
                        mayHaveChanged = true;
                    }
                    break;
            }

            if (mayHaveChanged)
            {
                if (oldText != editorEngine.text)
                    m_Changed = true;

                evt.StopPropagation();
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

            if (!string.IsNullOrEmpty(GUIUtility.compositionString))
            {
                editorEngine.text = newText.Substring(0, editorEngine.cursorIndex) + GUIUtility.compositionString + newText.Substring(editorEngine.selectIndex);
                cursorPos += GUIUtility.compositionString.Length;
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
