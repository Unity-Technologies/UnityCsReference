// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search
{
    static class SearchField
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

        private static TextEditor s_ActiveEditor;
        private static bool s_ActuallyEditing = false; // internal so we can save this state.
        private static IMECompositionMode s_IMECompositionModeBackup;

        private static bool s_DragToPosition = true;
        private static bool s_Dragged = false;
        private static bool s_PostPoneMove = false;
        private static bool s_SelectAllOnMouseUp = true;
        private static double s_NextBlinkTime = 0;
        private static bool s_CursorBlinking;
        private static int s_RecentSearchIndex = -1;
        private static string m_CycledSearch;
        private static string m_LastSearch;

        public static int controlID { get; private set; } = -1;

        public static string Draw(Rect position, string text, GUIStyle style)
        {
            using (new BlinkCursorScope(s_CursorBlinking, new Color(0, 0, 0, 0.01f)))
            {
                bool selectAll = false;
                if (!String.IsNullOrEmpty(m_CycledSearch) && (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
                {
                    text = m_CycledSearch;
                    m_CycledSearch = null;
                    selectAll = GUI.changed = true;
                }

                text = Draw(position, text, false, false, null, style ?? Styles.searchField);

                if (selectAll)
                    GetTextEditor().SelectAll();

                return text;
            }
        }

        public static string Draw(Rect position, string text, bool multiline, bool passwordField, string allowedletters, GUIStyle style)
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
                    if (editor.IsEditingControl(id))
                    {
                        editor.position = position;
                        editor.style = style;
                        editor.controlID = id;
                        editor.multiline = multiline;
                        editor.isPasswordField = passwordField;
                        editor.DetectFocusChange();
                    }
                    else if (EditorGUIUtility.editingTextField || (eventType == EventType.ExecuteCommand && evt.commandName == CommandName.NewKeyboardFocus))
                    {
                        editor.BeginEditing(id, text, position, style, multiline, passwordField);
                        if (eventType == EventType.ExecuteCommand)
                            evt.Use();
                    }
                }
            }

            if (editor.controlID == id && GUIUtility.keyboardControl != id)
                editor.EndEditing();

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
                                {
                                    evt.Use();
                                }
                                break;
                            case CommandName.Paste:
                                if (editor.CanPaste())
                                {
                                    evt.Use();
                                }
                                break;
                            case CommandName.SelectAll:
                            case CommandName.Delete:
                                evt.Use();
                                break;
                            case CommandName.UndoRedoPerformed:
                                editor.text = text;
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
                                s_ActiveEditor?.EndEditing();
                                evt.Use();
                                break;
                            case CommandName.Cut:
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
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
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
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
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
                                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                                {
                                    editor.Delete();
                                }
                                else
                                {
                                    editor.Cut();
                                }
                                mayHaveChanged = true;
                                evt.Use();
                                break;
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        if (s_Dragged && s_DragToPosition)
                        {
                            //GUIUtility.keyboardControl = id;
                            //editor.BeginEditing (id, text, position, style, multiline, passwordField);
                            editor.MoveSelectionToAltCursor();
                            mayHaveChanged = true;
                        }
                        else if (s_PostPoneMove)
                        {
                            editor.MoveCursorToPosition(evt.mousePosition);
                        }
                        else if (s_SelectAllOnMouseUp)
                        {
                            // If cursor is invisible, it's a selectable label, and we don't want to select all automatically
                            if (GUI.skin.settings.cursorColor.a > 0)
                            {
                                editor.SelectAll();
                            }
                            s_SelectAllOnMouseUp = false;
                        }

                        editor.MouseDragSelectsWholeWords(false);
                        s_DragToPosition = true;
                        s_Dragged = false;
                        s_PostPoneMove = false;
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
                        if (editor.IsEditingControl(id))
                        { // if so, process the event normally
                            if (evt.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                                editor.SelectCurrentWord();
                                editor.MouseDragSelectsWholeWords(true);
                                editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                                s_DragToPosition = false;
                            }
                            else if (evt.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                                editor.SelectCurrentParagraph();
                                editor.MouseDragSelectsWholeWords(true);
                                editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                                s_DragToPosition = false;
                            }
                            else
                            {
                                editor.MoveCursorToPosition(evt.mousePosition);
                                s_SelectAllOnMouseUp = false;
                            }
                        }
                        else
                        { // Otherwise, mark this as initial click and begin editing
                            GUIUtility.keyboardControl = id;
                            editor.BeginEditing(id, text, position, style, multiline, passwordField);
                            editor.MoveCursorToPosition(evt.mousePosition);
                            // If cursor is invisible, it's a selectable label, and we don't want to select all automatically
                            if (GUI.skin.settings.cursorColor.a > 0)
                            {
                                s_SelectAllOnMouseUp = true;
                            }
                        }

                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!evt.shift && editor.hasSelection && s_DragToPosition)
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

                            s_DragToPosition = false;
                            s_SelectAllOnMouseUp = !editor.hasSelection;
                        }
                        s_Dragged = true;
                        evt.Use();
                    }
                    break;
                case EventType.ContextClick:
                    if (position.Contains(evt.mousePosition))
                    {
                        if (!editor.IsEditingControl(id))
                        { // First click: focus before showing popup
                            GUIUtility.keyboardControl = id;
                            if (wasEnabled)
                            {
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
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
                        if (editor.IsEditingControl(id) && editor.HandleKeyEvent(evt))
                        {
                            evt.Use();
                            mayHaveChanged = true;
                            break;
                        }

                        if (evt.keyCode == KeyCode.Escape)
                        {
                            if (editor.IsEditingControl(id))
                            {
                                editor.text = String.Empty;
                                editor.EndEditing();
                                mayHaveChanged = true;
                            }
                        }
                        else if (c == '\n' || c == 3)
                        {
                            if (!editor.IsEditingControl(id))
                            {
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
                                editor.SelectAll();
                            }
                            else
                            {
                                if (!multiline || (evt.alt || evt.shift || evt.control))
                                {
                                    editor.EndEditing();
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
                            if (multiline && editor.IsEditingControl(id))
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
                        else if (editor.IsEditingControl(id))
                        {
                            bool validCharacter = (allowedletters == null || allowedletters.IndexOf(c) != -1) && IsPrintableChar(c);
                            if (validCharacter)
                            {
                                editor.Insert(c);
                                mayHaveChanged = true;
                            }
                            else
                            {
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
                        if (editor.IsEditingControl(id) && MightBePrintableKey(evt, multiline) && !nonPrintableTab)
                            evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (GUIUtility.hotControl == 0)
                        EditorGUIUtility.AddCursorRect(position, MouseCursor.Text);

                    string drawText = editor.IsEditingControl(id) ? editor.text : text;
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

        public static TextEditor GetTextEditor()
        {
            return (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), controlID);
        }

        public static void MoveCursor(TextCursorPlacement moveCursor)
        {
            var te = GetTextEditor();
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

        private static void MoveAutoComplete(TextEditor te)
        {
            while (te.cursorIndex < te.text.Length && !char.IsWhiteSpace(te.text[te.cursorIndex]))
                te.MoveRight();

            // If there is a space at the end of the text, move through it.
            if (te.cursorIndex == te.text.Length - 1 && char.IsWhiteSpace(te.text[te.cursorIndex]))
                te.MoveRight();
        }

        public static void Focus()
        {
            EditorGUI.FocusTextInControl(k_QuickSearchBoxName);
        }

        public static bool UpdateBlinkCursorState(double time)
        {
            if (time >= s_NextBlinkTime)
            {
                s_NextBlinkTime = time + 0.5;
                s_CursorBlinking = !s_CursorBlinking;
                return true;
            }

            return false;
        }

        public static bool HandleKeyEvent(Event evt)
        {
            if (evt.type != EventType.KeyDown)
                return false;

            if (evt.modifiers.HasFlag(EventModifiers.Alt))
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

        private static bool IsEditingControl(this TextEditor self, int id)
        {
            return GUIUtility.keyboardControl == id && self.controlID == id && s_ActuallyEditing && HasCurrentWindowKeyFocus();
        }

        private static void BeginEditing(this TextEditor self, int id, string newText, Rect _position, GUIStyle _style, bool _multiline, bool passwordField)
        {
            if (self.IsEditingControl(id))
                return;

            s_ActiveEditor?.EndEditing();
            s_ActiveEditor = self;
            self.controlID = id;
            self.text = newText;
            self.multiline = _multiline;
            self.style = _style;
            self.position = _position;
            self.isPasswordField = passwordField;
            self.scrollOffset = Vector2.zero;
            s_ActuallyEditing = true;
            Undo.IncrementCurrentGroup();

            s_IMECompositionModeBackup = Input.imeCompositionMode;
            Input.imeCompositionMode = IMECompositionMode.On;
        }

        private static void EndEditing(this TextEditor self)
        {
            if (s_ActiveEditor == self)
                s_ActiveEditor = null;

            self.controlID = 0;
            s_ActuallyEditing = false;
            Undo.IncrementCurrentGroup();

            Input.imeCompositionMode = s_IMECompositionModeBackup;
        }

        private static bool HasKeyboardFocus(int controlID)
        {
            // Every EditorWindow has its own keyboardControl state so we also need to
            // check if the current OS view has focus to determine if the control has actual key focus (gets the input)
            // and not just being a focused control in an unfocused window.
            return GUIUtility.keyboardControl == controlID && HasCurrentWindowKeyFocus();
        }

        private static EventType GetEventTypeForControlAllowDisabledContextMenuPaste(Event evt, int id)
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

        private static void ShowTextEditorPopupMenu(TextEditor editor)
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

        private static bool MightBePrintableKey(Event evt, bool multiline)
        {
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

        private static bool IsPrintableChar(char c)
        {
            if (c < 32)
            {
                return false;
            }
            return true;
        }

        private static bool HasCurrentWindowKeyFocus()
        {
            return EditorGUIUtility.HasCurrentWindowKeyFocus();
        }

        private static string CyclePreviousSearch(int shift)
        {
            if (SearchSettings.recentSearches.Count == 0)
                return m_LastSearch;

            s_RecentSearchIndex = Utils.Wrap(s_RecentSearchIndex + shift, SearchSettings.recentSearches.Count);

            return SearchSettings.recentSearches[s_RecentSearchIndex];
        }

        public static void UpdateLastSearchText(string value)
        {
            if (value.Equals(m_LastSearch))
                return;
            m_LastSearch = value;
            if (string.IsNullOrEmpty(value))
                return;
            s_RecentSearchIndex = 0;
            SearchSettings.AddRecentSearch(value);
        }
    }
}
