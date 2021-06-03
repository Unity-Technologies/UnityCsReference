// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchField
    {
        static class CommandName
        {
            public const string Cut = "Cut";
            public const string Copy = "Copy";
            public const string Paste = "Paste";
            public const string SelectAll = "SelectAll";
            public const string Delete = "Delete";
            public const string UndoRedoPerformed = "UndoRedoPerformed";
            public const string OnLostFocus = "OnLostFocus";
            public const string NewKeyboardFocus = "NewKeyboardFocus";
        }

        private const string k_QuickSearchBoxName = "QuickSearchBox";
        private static readonly int s_SearchFieldHash = "QuickSearchField".GetHashCode();

        private TextEditor m_ActiveEditor;
        private bool m_ActuallyEditing = false; // internal so we can save this state.
        private IMECompositionMode m_IMECompositionModeBackup;

        private bool m_DragToPosition = true;
        private bool m_Dragged = false;
        private bool m_PostPoneMove = false;
        private double m_NextBlinkTime = 0;
        private bool m_CursorBlinking;
        private int m_RecentSearchIndex = -1;
        private string m_CycledSearch;
        private string m_LastSearch;

        private int m_UndoIndex = -1;
        private TextUndoInfo[] m_UndoStack;
        private double m_UndoLastTime = 0f;

        public const float cancelButtonWidth = 20f;
        public int controlID { get; private set; } = -1;

        public bool IsMultiline(float height)
        {
            return height > 30f;
        }

        readonly struct TextUndoInfo
        {
            readonly public string text;
            readonly public int cursorPos;
            readonly public int selectPos;

            public TextUndoInfo(in TextEditor te)
            {
                this.text = te.text;
                this.cursorPos = te.cursorIndex;
                this.selectPos = te.selectIndex;
            }

            public TextUndoInfo(string text, int cursorPos, int selectPos)
            {
                this.text = text;
                this.cursorPos = cursorPos;
                this.selectPos = selectPos;
            }

            public override string ToString()
            {
                return $"{text} ({cursorPos}/{selectPos})";
            }
        }

        public string Draw(Rect position, string text, GUIStyle style)
        {
            if (m_UndoStack == null)
            {
                m_UndoStack = new TextUndoInfo[10];
                if (!string.IsNullOrEmpty(text))
                {
                    m_UndoStack[0] = new TextUndoInfo(text, text.Length, text.Length);
                    m_UndoIndex = 0;
                }
                SaveUndo(text);
            }

            var evt = Event.current;
            using (new BlinkCursorScope(m_CursorBlinking && HasKeyboardFocus(controlID), new Color(0, 0, 0, 0.01f)))
            {
                bool selectAll = false;
                if (!String.IsNullOrEmpty(m_CycledSearch) && (evt.type == EventType.Repaint || evt.type == EventType.Layout))
                {
                    text = m_CycledSearch;
                    m_CycledSearch = null;
                    selectAll = GUI.changed = true;
                }

                text = Draw(position, text, IsMultiline(position.height), false, null, style ?? Styles.searchField);
                TrackUndoRedo(text);

                if (selectAll)
                    GetTextEditor().SelectAll();

                return text;
            }
        }

        private void TrackUndoRedo(string text)
        {
            var now = EditorApplication.timeSinceStartup;
            if (GUI.changed)
                m_UndoLastTime = now;
            else if (m_UndoLastTime != 0f && now - m_UndoLastTime >= 0.5f)
                SaveUndo(text);
        }

        private void SaveUndo(string text)
        {
            if (string.IsNullOrEmpty(text) || m_UndoIndex != -1 && m_UndoStack[m_UndoIndex].text == text)
                return;
            m_UndoIndex = Utils.Wrap(m_UndoIndex + 1, m_UndoStack.Length);
            m_UndoStack[m_UndoIndex] = new TextUndoInfo(GetTextEditor());
        }

        private bool UndoEdit(TextEditor editor, int direction = 1)
        {
            if (m_UndoStack == null)
                return false;

            // Save current state in case it has changed
            SaveUndo(editor.text);

            var nextIndex = Utils.Wrap(m_UndoIndex - direction, m_UndoStack.Length);
            if (string.IsNullOrEmpty(m_UndoStack[nextIndex].text))
                return false;

            m_UndoIndex = nextIndex;
            editor.text = m_UndoStack[m_UndoIndex].text;
            editor.cursorIndex = m_UndoStack[m_UndoIndex].selectPos;
            editor.selectIndex = m_UndoStack[m_UndoIndex].cursorPos;
            return true;
        }

        private bool RedoEdit(TextEditor editor)
        {
            return UndoEdit(editor, -1);
        }

        public string Draw(Rect position, string text, bool multiline, bool passwordField, string allowedletters, GUIStyle style)
        {
            GUI.SetNextControlName(k_QuickSearchBoxName);

            var evt = Event.current;
            var id = GUIUtility.GetControlID(s_SearchFieldHash, FocusType.Keyboard, position);

            if (text == null)
                text = string.Empty;

            var eventType = evt.GetTypeForControl(id);
            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), id);
            controlID = id;

            if (HasKeyboardFocus(id))
            {
                editor.text = text;
                if (Event.current.type != EventType.Layout)
                {
                    if (IsEditingControl(editor, id))
                    {
                        editor.position = position;
                        editor.style = style;
                        editor.controlID = id;
                        editor.multiline = multiline;
                        editor.isPasswordField = passwordField;
                    }
                    else if (EditorGUIUtility.editingTextField || (eventType == EventType.ExecuteCommand && evt.commandName == CommandName.NewKeyboardFocus))
                    {
                        BeginEditing(editor, id, text, position, style, multiline, passwordField);
                        if (eventType == EventType.ExecuteCommand)
                            evt.Use();
                    }
                }
            }

            if (editor.controlID == id && GUIUtility.keyboardControl != id)
                EndEditing(editor);

            var mayHaveChanged = false;
            var textBeforeKey = editor.text;
            var wasEnabled = GUI.enabled;

            switch (GetEventTypeForControlAllowDisabledContextMenuPaste(evt, id))
            {
                case EventType.ValidateCommand:
                    if (GUIUtility.keyboardControl == id)
                    {
                        switch (evt.commandName)
                        {
                            case CommandName.Cut:
                            case CommandName.Copy:
                                if (editor.hasSelection)
                                    evt.Use();
                                break;
                            case CommandName.Paste:
                                if (editor.CanPaste())
                                    evt.Use();
                                break;
                            case CommandName.SelectAll:
                            case CommandName.Delete:
                                evt.Use();
                                break;
                            case CommandName.UndoRedoPerformed:
                                evt.Use();
                                break;
                        }
                    }
                    break;
                case EventType.ExecuteCommand:
                    if (GUIUtility.keyboardControl == id)
                    {
                        switch (evt.commandName)
                        {
                            case CommandName.OnLostFocus:
                                EndEditing(m_ActiveEditor);
                                evt.Use();
                                break;
                            case CommandName.Cut:
                                BeginEditing(editor, id, text, position, style, multiline, passwordField);
                                editor.Cut();
                                mayHaveChanged = true;
                                break;
                            case CommandName.Copy:
                                if (wasEnabled)
                                    editor.Copy();
                                else if (!passwordField)
                                    GUIUtility.systemCopyBuffer = text;
                                evt.Use();
                                break;
                            case CommandName.Paste:
                                BeginEditing(editor, id, text, position, style, multiline, passwordField);
                                editor.Paste();
                                mayHaveChanged = true;
                                break;
                            case CommandName.SelectAll:
                                editor.SelectAll();
                                evt.Use();
                                break;
                            case CommandName.Delete:
                                // This "Delete" command stems from a Shift-Delete in the text editor.
                                // On Windows, Shift-Delete in text does a cut whereas on Mac, it does a delete.
                                BeginEditing(editor, id, text, position, style, multiline, passwordField);
                                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                                    editor.Delete();
                                else
                                    editor.Cut();
                                mayHaveChanged = true;
                                evt.Use();
                                break;
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        if (m_Dragged && m_DragToPosition)
                        {
                            editor.MoveSelectionToAltCursor();
                            mayHaveChanged = true;
                        }
                        else if (m_PostPoneMove)
                        {
                            editor.MoveCursorToPosition(evt.mousePosition);
                        }

                        editor.MouseDragSelectsWholeWords(false);
                        m_DragToPosition = true;
                        m_Dragged = false;
                        m_PostPoneMove = false;
                        if (evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        // Does this text field already have focus?
                        if (IsEditingControl(editor, id))
                        { // if so, process the event normally
                            Utils.SetTextEditorHasFocus(editor, true); // We do not want the TextEditor to SelectAll
                            if (evt.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                                editor.SelectCurrentWord();
                                editor.MouseDragSelectsWholeWords(true);
                                editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                                m_DragToPosition = false;
                            }
                            else if (evt.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                                editor.SelectCurrentParagraph();
                                editor.MouseDragSelectsWholeWords(true);
                                editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                                m_DragToPosition = false;
                            }
                            else
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                            }
                        }
                        else
                        { // Otherwise, mark this as initial click and begin editing
                            GUIUtility.keyboardControl = id;
                            BeginEditing(editor, id, text, position, style, multiline, passwordField);
                            editor.MoveCursorToPosition(evt.mousePosition);
                        }

                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!evt.shift && editor.hasSelection && m_DragToPosition)
                        {
                            editor.MoveAltCursorToPosition(evt.mousePosition);
                        }
                        else
                        {
                            if (evt.shift)
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                            }
                            else
                            {
                                editor.SelectToPosition(evt.mousePosition);
                            }

                            m_DragToPosition = false;
                        }
                        m_Dragged = true;
                        evt.Use();
                    }
                    break;
                case EventType.ContextClick:
                    if (position.Contains(evt.mousePosition))
                    {
                        if (!IsEditingControl(editor, id))
                        { // First click: focus before showing popup
                            GUIUtility.keyboardControl = id;
                            if (wasEnabled)
                            {
                                BeginEditing(editor, id, text, position, style, multiline, passwordField);
                                editor.MoveCursorToPosition(evt.mousePosition);
                            }
                        }
                        ShowTextEditorPopupMenu(editor);
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    var nonPrintableTab = false;
                    if (GUIUtility.keyboardControl == id)
                    {
                        char c = evt.character;

                        // Let the editor handle all cursor keys, etc...
                        if (IsEditingControl(editor, id) && editor.HandleKeyEvent(evt))
                        {
                            evt.Use();
                            mayHaveChanged = true;
                            break;
                        }

                        if (m_UndoIndex != -1 && evt.keyCode == KeyCode.Z && (evt.command || evt.control))
                        {
                            if (IsEditingControl(editor, id) && UndoEdit(editor))
                            {
                                evt.Use();
                                mayHaveChanged = true;
                            }
                        }
                        else if (m_UndoIndex != -1 && evt.keyCode == KeyCode.Y && (evt.command || evt.control))
                        {
                            if (IsEditingControl(editor, id) && RedoEdit(editor))
                            {
                                evt.Use();
                                mayHaveChanged = true;
                            }
                        }
                        else if (evt.keyCode == KeyCode.Escape)
                        {
                            if (IsEditingControl(editor, id))
                            {
                                editor.text = String.Empty;
                                EndEditing(editor);
                                mayHaveChanged = true;
                            }
                        }
                        else if (c == '\n' || c == 3)
                        {
                            if (!IsEditingControl(editor, id))
                            {
                                BeginEditing(editor, id, text, position, style, multiline, passwordField);
                                editor.SelectAll();
                            }
                            else
                            {
                                if (!multiline || (evt.alt || evt.shift || evt.control))
                                {
                                    EndEditing(editor);
                                }
                                else
                                {
                                    editor.Insert(c);
                                    mayHaveChanged = true;
                                    break;
                                }
                            }
                        }
                        else if (c == '\t' || evt.keyCode == KeyCode.Tab)
                        {
                            // Only insert tabs if multiline
                            if (multiline && IsEditingControl(editor, id))
                            {
                                bool validTabCharacter = (allowedletters == null || allowedletters.IndexOf(c) != -1);
                                bool validTabEvent = !(evt.alt || evt.shift || evt.control) && c == '\t';
                                if (validTabEvent && validTabCharacter)
                                {
                                    editor.Insert(c);
                                    mayHaveChanged = true;
                                }
                            }
                            else
                            {
                                nonPrintableTab = true;
                            }
                        }
                        else if (c == 25 || c == 27)
                        {
                            // Note, OS X send characters for the following keys that we need to eat:
                            // ASCII 25: "End Of Medium" on pressing shift tab
                            // ASCII 27: "Escape" on pressing ESC
                            nonPrintableTab = true;
                        }
                        else if (IsEditingControl(editor, id))
                        {
                            bool validCharacter = (allowedletters == null || allowedletters.IndexOf(c) != -1) && IsPrintableChar(c);
                            if (validCharacter)
                            {
                                editor.Insert(c);
                                mayHaveChanged = true;
                            }
                            else
                            {
                                if (IsLineBreak(evt))
                                {
                                    editor.Insert('\n');
                                    mayHaveChanged = true;
                                }

                                // If the composition string is not empty, then it's likely that even though we didn't add a printable
                                // character to the string, we should refresh the GUI, to update the composition string.
                                if (Input.compositionString != "")
                                {
                                    editor.ReplaceSelection("");
                                    mayHaveChanged = true;
                                }
                            }
                        }
                        // consume Keycode events that might result in a printable key so they aren't passed on to other controls or shortcut manager later
                        if (IsEditingControl(editor, id) && MightBePrintableKey(evt, multiline) && !nonPrintableTab)
                            evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (GUIUtility.hotControl == 0)
                    {
                        var cursorRect = position;
                        if (!String.IsNullOrEmpty(text))
                        {
                            cursorRect = Styles.searchFieldBtn.margin.Remove(cursorRect);
                            cursorRect.width -= cancelButtonWidth;
                        }
                        EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.Text);
                    }

                    string drawText = IsEditingControl(editor, id) ? editor.text : text;
                    editor.position = position;
                    editor.style = style;
                    editor.DrawCursor(drawText);
                    break;
            }

            // Scroll offset might need to be updated
            editor.UpdateScrollOffsetIfNeeded(evt);

            GUI.changed = false;
            if (mayHaveChanged)
            {
                // If some action happened that could change the text AND
                // the text actually changed, then set changed to true.
                // Don't just compare the text only, since it also changes when changing text field.
                // Don't leave out comparing the text though, since it will result in false positives.
                GUI.changed = (textBeforeKey != editor.text);
                evt.Use();
            }
            if (GUI.changed)
            {
                GUI.changed = true;
                return editor.text;
            }

            return text;
        }

        public TextEditor GetTextEditor()
        {
            return (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), controlID);
        }

        public void MoveCursor(TextCursorPlacement moveCursor)
        {
            MoveCursor(moveCursor, -1);
        }

        public void MoveCursor(TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            var te = GetTextEditor();

            if (cursorInsertPosition >= 0)
            {
                te.selectIndex = te.cursorIndex = cursorInsertPosition;
            }
            else
            {
                switch (moveCursor)
                {
                    case TextCursorPlacement.MoveLineEnd: te.MoveLineEnd(); break;
                    case TextCursorPlacement.MoveLineStart: te.MoveLineStart(); break;
                    case TextCursorPlacement.MoveToEndOfPreviousWord: te.MoveToEndOfPreviousWord(); break;
                    case TextCursorPlacement.MoveToStartOfNextWord: te.MoveToStartOfNextWord(); break;
                    case TextCursorPlacement.MoveWordLeft: te.MoveWordLeft(); break;
                    case TextCursorPlacement.MoveWordRight: te.MoveWordRight(); break;
                    case TextCursorPlacement.MoveAutoComplete: MoveAutoComplete(te); break;
                }
            }
        }

        private void MoveAutoComplete(TextEditor te)
        {
            while (te.cursorIndex < te.text.Length && !char.IsWhiteSpace(te.text[te.cursorIndex]))
                te.MoveRight();

            // If there is a space at the end of the text, move through it.
            if (te.cursorIndex == te.text.Length - 1 && char.IsWhiteSpace(te.text[te.cursorIndex]))
                te.MoveRight();
        }

        public void Focus()
        {
            if (GUIUtility.keyboardControl != controlID)
                EditorGUI.FocusTextInControl(k_QuickSearchBoxName);
        }

        public bool UpdateBlinkCursorState(double time)
        {
            if (time >= m_NextBlinkTime)
            {
                m_NextBlinkTime = time + 0.5;
                m_CursorBlinking = !m_CursorBlinking;
                return true;
            }

            return false;
        }

        public bool HandleKeyEvent(Event evt)
        {
            if (evt.type != EventType.KeyDown)
                return false;

            if (IsLineBreak(evt))
                return true;

            if (evt.modifiers.HasAny(EventModifiers.Alt))
            {
                if (evt.keyCode == KeyCode.DownArrow)
                {
                    m_CycledSearch = CyclePreviousSearch(-1);
                    evt.Use();
                    return true;
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    m_CycledSearch = CyclePreviousSearch(+1);
                    evt.Use();
                    return true;
                }
            }

            return false;
        }

        private bool IsEditingControl(TextEditor self, int id)
        {
            return GUIUtility.keyboardControl == id && self.controlID == id && m_ActuallyEditing && Utils.HasCurrentWindowKeyFocus();
        }

        private void BeginEditing(TextEditor self, int id, string newText, Rect _position, GUIStyle _style, bool _multiline, bool passwordField)
        {
            if (IsEditingControl(self, id))
                return;

            if (m_ActiveEditor != null)
                EndEditing(m_ActiveEditor);
            m_ActiveEditor = self;
            self.controlID = id;
            self.text = newText;
            self.multiline = _multiline;
            self.style = _style;
            self.position = _position;
            self.isPasswordField = passwordField;
            self.scrollOffset = Vector2.zero;
            m_ActuallyEditing = true;
            Undo.IncrementCurrentGroup();

            m_IMECompositionModeBackup = Input.imeCompositionMode;
            Input.imeCompositionMode = IMECompositionMode.On;
        }

        private void EndEditing(TextEditor self)
        {
            if (m_ActiveEditor == self)
                m_ActiveEditor = null;

            self.controlID = 0;
            m_ActuallyEditing = false;
            Undo.IncrementCurrentGroup();

            Input.imeCompositionMode = m_IMECompositionModeBackup;
        }

        private bool HasKeyboardFocus(int controlID)
        {
            // Every EditorWindow has its own keyboardControl state so we also need to
            // check if the current OS view has focus to determine if the control has actual key focus (gets the input)
            // and not just being a focused control in an unfocused window.
            return GUIUtility.keyboardControl == controlID && Utils.HasCurrentWindowKeyFocus();
        }

        private EventType GetEventTypeForControlAllowDisabledContextMenuPaste(Event evt, int id)
        {
            // UI is enabled: regular code path
            var wasEnabled = GUI.enabled;
            if (wasEnabled)
                return evt.GetTypeForControl(id);

            // UI is disabled: get type as if it was enabled
            GUI.enabled = true;
            var type = evt.GetTypeForControl(id);
            GUI.enabled = false;

            // these events are always processed, no matter the enabled/disabled state (IMGUI::GetEventType)
            if (type == EventType.Repaint || type == EventType.Layout || type == EventType.Used)
                return type;

            // allow context / right click, and "Copy" commands
            if (type == EventType.ContextClick)
                return type;
            if (type == EventType.MouseDown && evt.button == 1)
                return type;
            if ((type == EventType.ValidateCommand || type == EventType.ExecuteCommand) && evt.commandName == CommandName.Copy)
                return type;

            // ignore all other events for disabled controls
            return EventType.Ignore;
        }

        private void ShowTextEditorPopupMenu(TextEditor editor)
        {
            GenericMenu pm = new GenericMenu();
            var enabled = GUI.enabled;
            var receiver = EditorWindow.focusedWindow;

            // Cut
            if (editor.hasSelection && !editor.isPasswordField && enabled)
                pm.AddItem(EditorGUIUtility.TrTextContent("Cut"), false, () => receiver.SendEvent(EditorGUIUtility.CommandEvent(CommandName.Cut)));
            else
                pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Cut"));

            // Copy -- when GUI is disabled, allow Copy even with no selection (will copy everything)
            if ((editor.hasSelection || !enabled) && !editor.isPasswordField)
                pm.AddItem(EditorGUIUtility.TrTextContent("Copy"), false, () => receiver.SendEvent(EditorGUIUtility.CommandEvent(CommandName.Copy)));
            else
                pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Copy"));

            // Paste
            if (editor.CanPaste() && enabled)
                pm.AddItem(EditorGUIUtility.TrTextContent("Paste"), false, () => receiver.SendEvent(EditorGUIUtility.CommandEvent(CommandName.Paste)));

            pm.ShowAsContext();
        }

        private bool IsLineBreak(Event evt)
        {
            if ((evt.control || evt.shift) && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                return true;
            return false;
        }

        private bool MightBePrintableKey(Event evt, bool multiline)
        {
            if (IsLineBreak(evt))
                return true;
            if (evt.command || evt.control)
                return false;
            if (evt.keyCode >= KeyCode.Mouse0 && evt.keyCode <= KeyCode.Mouse6)
                return false;
            if (evt.keyCode >= KeyCode.JoystickButton0 && evt.keyCode <= KeyCode.Joystick8Button19)
                return false;
            if (evt.keyCode >= KeyCode.F1 && evt.keyCode <= KeyCode.F15)
                return false;
            switch (evt.keyCode)
            {
                case KeyCode.AltGr:
                case KeyCode.Backspace:
                case KeyCode.CapsLock:
                case KeyCode.Clear:
                case KeyCode.Delete:
                case KeyCode.DownArrow:
                case KeyCode.End:
                case KeyCode.Escape:
                case KeyCode.Help:
                case KeyCode.Home:
                case KeyCode.Insert:
                case KeyCode.LeftAlt:
                case KeyCode.LeftArrow:
                case KeyCode.LeftCommand: // same as LeftApple
                case KeyCode.LeftControl:
                case KeyCode.LeftShift:
                case KeyCode.LeftWindows:
                case KeyCode.Menu:
                case KeyCode.Numlock:
                case KeyCode.PageDown:
                case KeyCode.PageUp:
                case KeyCode.Pause:
                case KeyCode.Print:
                case KeyCode.RightAlt:
                case KeyCode.RightArrow:
                case KeyCode.RightCommand: // same as RightApple
                case KeyCode.RightControl:
                case KeyCode.RightShift:
                case KeyCode.RightWindows:
                case KeyCode.ScrollLock:
                case KeyCode.SysReq:
                case KeyCode.UpArrow:
                    return false;
                case KeyCode.Return:
                    return multiline;
                case KeyCode.None:
                    return IsPrintableChar(evt.character);
                default:
                    return true;
            }
        }

        private bool IsPrintableChar(char c)
        {
            if (c < 32)
            {
                return false;
            }
            return true;
        }

        private string CyclePreviousSearch(int shift)
        {
            if (SearchSettings.recentSearches.Count == 0)
                return m_LastSearch;

            m_RecentSearchIndex = Utils.Wrap(m_RecentSearchIndex + shift, SearchSettings.recentSearches.Count);

            return SearchSettings.recentSearches[m_RecentSearchIndex];
        }

        public void UpdateLastSearchText(string value)
        {
            if (value.Equals(m_LastSearch))
                return;
            m_LastSearch = value;
            if (string.IsNullOrEmpty(value))
                return;
            m_RecentSearchIndex = 0;
            SearchSettings.AddRecentSearch(value);
        }

        public void DrawError(int errorIndex, int errorLength, string errorTooltip)
        {
            DrawLineWithTooltip(errorIndex, errorIndex + errorLength, errorTooltip, Styles.Wiggle.wiggle, Styles.Wiggle.wiggleTooltip);
        }

        public void DrawWarning(int errorIndex, int errorLength, string errorTooltip)
        {
            DrawLineWithTooltip(errorIndex, errorIndex + errorLength, errorTooltip, Styles.Wiggle.wiggleWarning, Styles.Wiggle.wiggleTooltip);
        }

        const float textTopBottomPadding = 4f;
        const float minSinglelineTextHeight = 20f;
        public const float searchFieldSingleLineHeight = minSinglelineTextHeight + textTopBottomPadding;

        private void DrawLineWithTooltip(int lineStartIndex, int lineEndIndex, string tooltip, GUIStyle lineStyle, GUIStyle tooltipStyle)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var te = GetTextEditor();

            var content = new GUIContent(te.text);
            var startPosition = te.style.GetCursorPixelPosition(te.position, content, lineStartIndex);
            var endPosition = te.style.GetCursorPixelPosition(te.position, content, lineEndIndex);

            var visibleRect = te.style.padding.Remove(te.position);
            startPosition.x -= te.scrollOffset.x;
            endPosition.x -= te.scrollOffset.x;
            if (startPosition.x < visibleRect.x && endPosition.x < visibleRect.x)
                return;

            startPosition.x = Mathf.Max(startPosition.x, visibleRect.x);
            var lineRect = new Rect(te.position) { xMin = startPosition.x, xMax = endPosition.x };
            lineRect.yMin = startPosition.y + minSinglelineTextHeight;
            lineRect.yMax = lineRect.yMin + lineStyle.fixedHeight;

            lineStyle.Draw(lineRect, GUIContent.none, controlID);

            var tooltipRect = new Rect(lineRect);
            tooltipRect.yMin -= textTopBottomPadding;
            tooltipRect.yMax += textTopBottomPadding;
            tooltipStyle.Draw(tooltipRect, Utils.GUIContentTemp(string.Empty, tooltip), controlID);
            EditorGUIUtility.AddCursorRect(tooltipRect, MouseCursor.Arrow);
        }

        internal Rect GetRect(string text, float width, float padding)
        {
            var fieldWidth = width - padding;
            var fieldHeight = Mathf.Max(minSinglelineTextHeight, Styles.searchField.CalcHeight(Utils.GUIContentTemp(text), fieldWidth));
            return GUILayoutUtility.GetRect(fieldWidth, fieldHeight + textTopBottomPadding, Styles.searchField);
        }
    }
}
