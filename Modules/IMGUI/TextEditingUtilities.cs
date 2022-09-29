// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{

    enum TextEditOp
    {
        MoveLeft, MoveRight, MoveUp, MoveDown, MoveLineStart, MoveLineEnd, MoveTextStart, MoveTextEnd, MovePageUp, MovePageDown,
        MoveGraphicalLineStart, MoveGraphicalLineEnd, MoveWordLeft, MoveWordRight,
        MoveParagraphForward, MoveParagraphBackward,  MoveToStartOfNextWord, MoveToEndOfPreviousWord,
        SelectLeft, SelectRight, SelectUp, SelectDown, SelectTextStart, SelectTextEnd, SelectPageUp, SelectPageDown,
        ExpandSelectGraphicalLineStart, ExpandSelectGraphicalLineEnd, SelectGraphicalLineStart, SelectGraphicalLineEnd,
        SelectWordLeft, SelectWordRight, SelectToEndOfPreviousWord, SelectToStartOfNextWord,
        SelectParagraphBackward, SelectParagraphForward,
        Delete, Backspace, DeleteWordBack, DeleteWordForward, DeleteLineBack,
        Cut, Copy, Paste, SelectAll, SelectNone,
        ScrollStart, ScrollEnd, ScrollPageUp, ScrollPageDown
    }

    internal class TextEditingUtilities
    {
        private TextSelectingUtilities m_TextSelectingUtility;
        TextHandle m_TextHandle;
        private bool hasSelection => m_TextSelectingUtility.hasSelection;
        private string SelectedText => m_TextSelectingUtility.selectedText;
        private int m_iAltCursorPos => m_TextSelectingUtility.iAltCursorPos;
        int m_CursorIndexSavedState = -1;
        bool m_IsCompositionActive;
        bool m_UpdateImeWindowPosition;

        internal bool revealCursor
        {
            get { return m_TextSelectingUtility.revealCursor; }
            set { m_TextSelectingUtility.revealCursor = value; }
        }
        private int cursorIndex
        {
            get { return m_TextSelectingUtility.cursorIndex; }
            set { m_TextSelectingUtility.cursorIndex = value; }
        }
        private int selectIndex
        {
            get { return m_TextSelectingUtility.selectIndex; }
            set { m_TextSelectingUtility.selectIndex = value; }
        }
        internal string text
        {
            get { return m_TextSelectingUtility.text; }
            set { m_TextSelectingUtility.text = value; }
        }
        public TextEditingUtilities(TextSelectingUtilities selectingUtilities, TextHandle textHandle)
        {
            m_TextSelectingUtility = selectingUtilities;
            m_TextHandle = textHandle;
        }

        /// <summary>
        /// Checks if IME is active and updates the internal states. This should be run each update.
        /// </summary>
        public bool UpdateImeState()
        {
            if (GUIUtility.compositionString.Length > 0)
            {
                if (!m_IsCompositionActive)
                {
                    m_UpdateImeWindowPosition = true;
                    ReplaceSelection(string.Empty);
                }

                m_IsCompositionActive = true;
            }
            else
            {
                m_IsCompositionActive = false;
            }

            return m_IsCompositionActive;
        }

        public bool ShouldUpdateImeWindowPosition()
        {
            return m_UpdateImeWindowPosition;
        }

        public void SetImeWindowPosition(Vector2 worldPosition)
        {
            var cursorPos = m_TextHandle.GetCursorPositionFromStringIndexUsingCharacterHeight(cursorIndex, true);
            GUIUtility.compositionCursorPos = worldPosition + cursorPos;
        }

        public string GeneratePreviewString(bool richText)
        {
            RestoreCursorState();
            var compositionString = GUIUtility.compositionString;
            if (m_IsCompositionActive)
            {
                return richText ? text.Insert(cursorIndex, $"<u>{compositionString}</u>") : text.Insert(cursorIndex, compositionString);
            }
            return text;
        }

        /// <summary>
        /// Sets the cursor position to be at the end of the current IME item being previewing in the text.
        /// When generateVisualContent is invoked this cursor position will be returned. Any changes to the text will revert the cursor back to the original position.
        /// </summary>
        /// <param name="cursor"></param>
        public void EnableCursorPreviewState()
        {
            if (m_CursorIndexSavedState != -1)
                return;

            m_CursorIndexSavedState = m_TextSelectingUtility.cursorIndex;
            m_TextSelectingUtility.SetCursorNoCheck(m_CursorIndexSavedState + GUIUtility.compositionString.Length);
        }

        /// <summary>
        /// Restores the cursor back to its original position after previewing the IME string cursor position with <see cref="SetCursorPreviewState(int)"/>
        /// </summary>
        public void RestoreCursorState()
        {
            if (m_CursorIndexSavedState == -1)
                return;

            m_TextSelectingUtility.SetCursorNoCheck(m_CursorIndexSavedState);
            m_CursorIndexSavedState = -1;
        }

        [VisibleToOtherModules]
        internal bool HandleKeyEvent(Event e, bool textIsReadOnly)
        {
            RestoreCursorState();
            InitKeyActions();
            EventModifiers m = e.modifiers;
            e.modifiers &= ~EventModifiers.CapsLock;
            if (s_Keyactions.ContainsKey(e))
            {
                TextEditOp op = (TextEditOp)s_Keyactions[e];
                PerformOperation(op, textIsReadOnly);
                e.modifiers = m;
                return true;
            }
            e.modifiers = m;
            return false;
        }

        bool PerformOperation(TextEditOp operation, bool textIsReadOnly)
        {
            revealCursor = true;

            switch (operation)
            {
                // NOTE the TODOs below:
                case TextEditOp.MoveLeft:           m_TextSelectingUtility.MoveLeft(); break;
                case TextEditOp.MoveRight:          m_TextSelectingUtility.MoveRight(); break;
                case TextEditOp.MoveUp:             m_TextSelectingUtility.MoveUp(); break;
                case TextEditOp.MoveDown:           m_TextSelectingUtility.MoveDown(); break;
                case TextEditOp.MoveLineStart:      m_TextSelectingUtility.MoveLineStart(); break;
                case TextEditOp.MoveLineEnd:        m_TextSelectingUtility.MoveLineEnd(); break;
                case TextEditOp.MoveWordRight:      m_TextSelectingUtility.MoveWordRight(); break;
                case TextEditOp.MoveToStartOfNextWord:      m_TextSelectingUtility.MoveToStartOfNextWord(); break;
                case TextEditOp.MoveToEndOfPreviousWord:        m_TextSelectingUtility.MoveToEndOfPreviousWord(); break;
                case TextEditOp.MoveWordLeft:       m_TextSelectingUtility.MoveWordLeft(); break;
                case TextEditOp.MoveTextStart:      m_TextSelectingUtility.MoveTextStart(); break;
                case TextEditOp.MoveTextEnd:        m_TextSelectingUtility.MoveTextEnd(); break;
                case TextEditOp.MoveParagraphForward:   m_TextSelectingUtility.MoveParagraphForward(); break;
                case TextEditOp.MoveParagraphBackward:  m_TextSelectingUtility.MoveParagraphBackward(); break;
                //      case TextEditOp.MovePageUp:     return MovePageUp (); break;
                //      case TextEditOp.MovePageDown:       return MovePageDown (); break;
                case TextEditOp.MoveGraphicalLineStart: m_TextSelectingUtility.MoveGraphicalLineStart(); break;
                case TextEditOp.MoveGraphicalLineEnd: m_TextSelectingUtility.MoveGraphicalLineEnd(); break;
                case TextEditOp.SelectLeft:         m_TextSelectingUtility.SelectLeft(); break;
                case TextEditOp.SelectRight:            m_TextSelectingUtility.SelectRight(); break;
                case TextEditOp.SelectUp:           m_TextSelectingUtility.SelectUp(); break;
                case TextEditOp.SelectDown:         m_TextSelectingUtility.SelectDown(); break;
                case TextEditOp.SelectWordRight:        m_TextSelectingUtility.SelectWordRight(); break;
                case TextEditOp.SelectWordLeft:     m_TextSelectingUtility.SelectWordLeft(); break;
                case TextEditOp.SelectToEndOfPreviousWord:  m_TextSelectingUtility.SelectToEndOfPreviousWord(); break;
                case TextEditOp.SelectToStartOfNextWord:    m_TextSelectingUtility.SelectToStartOfNextWord(); break;

                case TextEditOp.SelectTextStart:        m_TextSelectingUtility.SelectTextStart(); break;
                case TextEditOp.SelectTextEnd:      m_TextSelectingUtility.SelectTextEnd(); break;
                case TextEditOp.ExpandSelectGraphicalLineStart: m_TextSelectingUtility.ExpandSelectGraphicalLineStart(); break;
                case TextEditOp.ExpandSelectGraphicalLineEnd: m_TextSelectingUtility.ExpandSelectGraphicalLineEnd(); break;
                case TextEditOp.SelectParagraphForward:     m_TextSelectingUtility.SelectParagraphForward(); break;
                case TextEditOp.SelectParagraphBackward:    m_TextSelectingUtility.SelectParagraphBackward(); break;
                case TextEditOp.SelectGraphicalLineStart: m_TextSelectingUtility.SelectGraphicalLineStart(); break;
                case TextEditOp.SelectGraphicalLineEnd: m_TextSelectingUtility.SelectGraphicalLineEnd(); break;
                //      case TextEditOp.SelectPageUp:                   return SelectPageUp (); break;
                //      case TextEditOp.SelectPageDown:             return SelectPageDown (); break;
                case TextEditOp.Delete:
                    if (textIsReadOnly) return false;
                    else return Delete();
                case TextEditOp.Backspace:
                    if (textIsReadOnly) return false;
                    else return Backspace();
                case TextEditOp.Cut:
                    if (textIsReadOnly) return false;
                    else return Cut();
                case TextEditOp.Copy:
                    m_TextSelectingUtility.Copy(); break;
                case TextEditOp.Paste:
                    if (textIsReadOnly) return false;
                    else return Paste();
                case TextEditOp.SelectAll:                          m_TextSelectingUtility.SelectAll(); break;
                case TextEditOp.SelectNone:                     m_TextSelectingUtility.SelectNone(); break;
                //      case TextEditOp.ScrollStart:            return ScrollStart (); break;
                //      case TextEditOp.ScrollEnd:          return ScrollEnd (); break;
                //      case TextEditOp.ScrollPageUp:       return ScrollPageUp (); break;
                //      case TextEditOp.ScrollPageDown:     return ScrollPageDown (); break;
                case TextEditOp.DeleteWordBack:
                    if (textIsReadOnly) return false;
                    else return DeleteWordBack();
                case TextEditOp.DeleteLineBack:
                    if (textIsReadOnly) return false;
                    else return DeleteLineBack();
                case TextEditOp.DeleteWordForward:
                    if (textIsReadOnly) return false;
                    else return DeleteWordForward();
                default:
                    Debug.Log("Unimplemented: " + operation);
                    break;
            }

            return false;
        }


        static void MapKey(string key, TextEditOp action)
        {
            s_Keyactions[Event.KeyboardEvent(key)] = action;
        }

        static Dictionary<Event, TextEditOp> s_Keyactions;
        /// Set up a platform independent keyboard->Edit action map. This varies depending on whether we are on mac or windows.
        void InitKeyActions()
        {
            if (s_Keyactions != null)
                return;
            s_Keyactions = new Dictionary<Event, TextEditOp>();

            // key mappings shared by the platforms
            MapKey("left", TextEditOp.MoveLeft);
            MapKey("right", TextEditOp.MoveRight);
            MapKey("up", TextEditOp.MoveUp);
            MapKey("down", TextEditOp.MoveDown);

            MapKey("#left", TextEditOp.SelectLeft);
            MapKey("#right", TextEditOp.SelectRight);
            MapKey("#up", TextEditOp.SelectUp);
            MapKey("#down", TextEditOp.SelectDown);

            MapKey("delete", TextEditOp.Delete);
            MapKey("backspace", TextEditOp.Backspace);
            MapKey("#backspace", TextEditOp.Backspace);

            // OSX is the special case for input shortcuts
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                // Keyboard mappings for mac
                // TODO     MapKey ("home", TextEditOp.ScrollStart);
                // TODO     MapKey ("end", TextEditOp.ScrollEnd);
                // TODO     MapKey ("page up", TextEditOp.ScrollPageUp);
                // TODO     MapKey ("page down", TextEditOp.ScrollPageDown);

                MapKey("^left", TextEditOp.MoveGraphicalLineStart);
                MapKey("^right", TextEditOp.MoveGraphicalLineEnd);
                // TODO     MapKey ("^up", TextEditOp.ScrollPageUp);
                // TODO     MapKey ("^down", TextEditOp.ScrollPageDown);

                MapKey("&left", TextEditOp.MoveWordLeft);
                MapKey("&right", TextEditOp.MoveWordRight);
                MapKey("&up", TextEditOp.MoveParagraphBackward);
                MapKey("&down", TextEditOp.MoveParagraphForward);

                MapKey("%left", TextEditOp.MoveGraphicalLineStart);
                MapKey("%right", TextEditOp.MoveGraphicalLineEnd);
                MapKey("%up", TextEditOp.MoveTextStart);
                MapKey("%down", TextEditOp.MoveTextEnd);

                MapKey("#home", TextEditOp.SelectTextStart);
                MapKey("#end", TextEditOp.SelectTextEnd);
                // TODO         MapKey ("#page up", TextEditOp.SelectPageUp);
                // TODO         MapKey ("#page down", TextEditOp.SelectPageDown);

                MapKey("#^left", TextEditOp.ExpandSelectGraphicalLineStart);
                MapKey("#^right", TextEditOp.ExpandSelectGraphicalLineEnd);
                MapKey("#^up", TextEditOp.SelectParagraphBackward);
                MapKey("#^down", TextEditOp.SelectParagraphForward);

                MapKey("#&left", TextEditOp.SelectWordLeft);
                MapKey("#&right", TextEditOp.SelectWordRight);
                MapKey("#&up", TextEditOp.SelectParagraphBackward);
                MapKey("#&down", TextEditOp.SelectParagraphForward);

                MapKey("#%left", TextEditOp.ExpandSelectGraphicalLineStart);
                MapKey("#%right", TextEditOp.ExpandSelectGraphicalLineEnd);
                MapKey("#%up", TextEditOp.SelectTextStart);
                MapKey("#%down", TextEditOp.SelectTextEnd);

                MapKey("%a", TextEditOp.SelectAll);
                MapKey("%x", TextEditOp.Cut);
                MapKey("%c", TextEditOp.Copy);
                MapKey("%v", TextEditOp.Paste);

                // emacs-like keybindings
                MapKey("^d", TextEditOp.Delete);
                MapKey("^h", TextEditOp.Backspace);
                MapKey("^b", TextEditOp.MoveLeft);
                MapKey("^f", TextEditOp.MoveRight);
                MapKey("^a", TextEditOp.MoveLineStart);
                MapKey("^e", TextEditOp.MoveLineEnd);

                MapKey("&delete", TextEditOp.DeleteWordForward);
                MapKey("&backspace", TextEditOp.DeleteWordBack);
                MapKey("%backspace", TextEditOp.DeleteLineBack);
            }
            else
            {
                // Windows/Linux keymappings
                MapKey("home", TextEditOp.MoveGraphicalLineStart);
                MapKey("end", TextEditOp.MoveGraphicalLineEnd);
                // TODO     MapKey ("page up", TextEditOp.MovePageUp);
                // TODO     MapKey ("page down", TextEditOp.MovePageDown);

                MapKey("%left", TextEditOp.MoveWordLeft);
                MapKey("%right", TextEditOp.MoveWordRight);
                MapKey("%up", TextEditOp.MoveParagraphBackward);
                MapKey("%down", TextEditOp.MoveParagraphForward);

                MapKey("^left", TextEditOp.MoveToEndOfPreviousWord);
                MapKey("^right", TextEditOp.MoveToStartOfNextWord);
                MapKey("^up", TextEditOp.MoveParagraphBackward);
                MapKey("^down", TextEditOp.MoveParagraphForward);

                MapKey("#^left", TextEditOp.SelectToEndOfPreviousWord);
                MapKey("#^right", TextEditOp.SelectToStartOfNextWord);
                MapKey("#^up", TextEditOp.SelectParagraphBackward);
                MapKey("#^down", TextEditOp.SelectParagraphForward);

                MapKey("#home", TextEditOp.SelectGraphicalLineStart);
                MapKey("#end", TextEditOp.SelectGraphicalLineEnd);
                // TODO         MapKey ("#page up", TextEditOp.SelectPageUp);
                // TODO         MapKey ("#page down", TextEditOp.SelectPageDown);

                MapKey("^delete", TextEditOp.DeleteWordForward);
                MapKey("^backspace", TextEditOp.DeleteWordBack);
                MapKey("%backspace", TextEditOp.DeleteLineBack);

                MapKey("^a", TextEditOp.SelectAll);
                MapKey("^x", TextEditOp.Cut);
                MapKey("^c", TextEditOp.Copy);
                MapKey("^v", TextEditOp.Paste);
                MapKey("#delete", TextEditOp.Cut);
                MapKey("^insert", TextEditOp.Copy);
                MapKey("#insert", TextEditOp.Paste);
            }
        }


        // Deletes previous text on the line
        public bool DeleteLineBack()
        {
            RestoreCursorState();

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            int p = cursorIndex;
            int i = p;
            while (i-- != 0)
                if (text[i] == '\n')
                {
                    p = i + 1;
                    break;
                }
            if (i == -1)
                p = 0;
            if (cursorIndex != p)
            {
                text = text.Remove(p, cursorIndex - p);
                m_TextSelectingUtility.selectIndex = cursorIndex = p;
                return true;
            }
            return false;
        }

        // Deletes the previous word
        public bool DeleteWordBack()
        {
            RestoreCursorState();

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }

            int prevWordEnd = m_TextSelectingUtility.FindEndOfPreviousWord(cursorIndex);
            if (cursorIndex != prevWordEnd)
            {
                text = text.Remove(prevWordEnd, cursorIndex - prevWordEnd);
                selectIndex = cursorIndex = prevWordEnd;
                return true;
            }
            return false;
        }

        // Deletes the following word
        public bool DeleteWordForward()
        {
            RestoreCursorState();

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }

            int nextWordStart = m_TextSelectingUtility.FindStartOfNextWord(cursorIndex);
            if (cursorIndex < text.Length)
            {
                text = text.Remove(cursorIndex, nextWordStart - cursorIndex);
                return true;
            }
            return false;
        }

        // perform a right-delete
        public bool Delete()
        {
            RestoreCursorState();

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            else if (cursorIndex < text.Length)
            {
                text = text.Remove(cursorIndex, m_TextSelectingUtility.NextCodePointIndex(cursorIndex) - cursorIndex);
                return true;
            }
            return false;
        }

        // Perform a left-delete
        public bool Backspace()
        {
            RestoreCursorState();

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            else if (cursorIndex > 0)
            {
                var startIndex = m_TextSelectingUtility.PreviousCodePointIndex(cursorIndex);
                text = text.Remove(startIndex, cursorIndex - startIndex);
                selectIndex = cursorIndex = startIndex;
                m_TextSelectingUtility.ClearCursorPos();
                return true;
            }
            return false;
        }

        /// Delete the current selection. If there is no selection, this function does not do anything...
        public bool DeleteSelection()
        {
            if (cursorIndex == selectIndex)
                return false;
            if (cursorIndex < selectIndex)
            {
                text = text.Substring(0, cursorIndex) + text.Substring(selectIndex, text.Length - selectIndex);
                m_TextSelectingUtility.SetSelectIndexWithoutNotify(cursorIndex);
            }
            else
            {
                text = text.Substring(0, selectIndex) + text.Substring(cursorIndex, text.Length - cursorIndex);
                m_TextSelectingUtility.SetCursorIndexWithoutNotify(selectIndex);
            }
            m_TextSelectingUtility.ClearCursorPos();

            return true;
        }

        /// Replace the selection with /replace/. If there is no selection, /replace/ is inserted at the current cursor point.
        public void ReplaceSelection(string replace)
        {
            RestoreCursorState();
            DeleteSelection();
            text = text.Insert(cursorIndex, replace);
            m_TextSelectingUtility.SetCursorIndexWithoutNotify(cursorIndex + replace.Length);
            m_TextSelectingUtility.SetSelectIndexWithoutNotify(selectIndex + replace.Length);
            m_TextSelectingUtility.ClearCursorPos();
        }

        /// Replacted the selection with /c/
        public void Insert(char c)
        {
            ReplaceSelection(c.ToString());
        }

        /// Move selection to alt cursor /position/
        public void MoveSelectionToAltCursor()
        {
            RestoreCursorState();
            if (m_iAltCursorPos == -1)
                return;
            int p = m_iAltCursorPos;
            string tmp = SelectedText;
            text = text.Insert(p, tmp);

            if (p < cursorIndex)
            {
                cursorIndex += tmp.Length;
                selectIndex += tmp.Length;
            }

            DeleteSelection();

            selectIndex = cursorIndex = p;
            m_TextSelectingUtility.ClearCursorPos();
        }

        public bool CanPaste()
        {
            return GUIUtility.systemCopyBuffer.Length != 0;
        }

        public bool Cut()
        {
            m_TextSelectingUtility.Copy();
            return DeleteSelection();
        }
        public bool Paste()
        {
            RestoreCursorState();
            string pasteval = GUIUtility.systemCopyBuffer;
            if (pasteval != "")
            {
                if (!m_TextSelectingUtility.multiline)
                    pasteval = ReplaceNewlinesWithSpaces(pasteval);
                ReplaceSelection(pasteval);
                return true;
            }
            return false;
        }

        static string ReplaceNewlinesWithSpaces(string value)
        {
            // First get rid of Windows style new lines and then *nix so we don't leave '\r' around.
            value = value.Replace("\r\n", " ");
            value = value.Replace('\n', ' ');
            // This probably won't happen, but just in case...
            value = value.Replace('\r', ' ');
            return value;
        }

        internal void OnBlur()
        {
            revealCursor = false;
            m_TextSelectingUtility.SelectNone();
        }
    }
}
