// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{

    enum TextEditOp
    {
        MoveLeft, MoveRight, MoveUp, MoveDown, MoveLineStart, MoveLineEnd, MoveTextStart, MoveTextEnd, MovePageUp,
        MovePageDown, MoveGraphicalLineStart, MoveGraphicalLineEnd, MoveWordLeft, MoveWordRight, MoveParagraphForward,
        MoveParagraphBackward,  MoveToStartOfNextWord, MoveToEndOfPreviousWord, Delete, Backspace, DeleteWordBack,
        DeleteWordForward, DeleteLineBack, Cut, Paste, ScrollStart, ScrollEnd, ScrollPageUp, ScrollPageDown
    }

    enum TextSelectOp
    {
        SelectLeft, SelectRight, SelectUp, SelectDown, SelectTextStart, SelectTextEnd, SelectPageUp, SelectPageDown,
        ExpandSelectGraphicalLineStart, ExpandSelectGraphicalLineEnd, SelectGraphicalLineStart, SelectGraphicalLineEnd,
        SelectWordLeft, SelectWordRight, SelectToEndOfPreviousWord, SelectToStartOfNextWord, SelectParagraphBackward,
        SelectParagraphForward, Copy, SelectAll, SelectNone
    }

    internal class TextEditingUtilities
    {
        private TextSelectingUtilities m_TextSelectingUtility;
        internal TextHandle textHandle;
        private bool hasSelection => m_TextSelectingUtility.hasSelection;
        private string SelectedText => m_TextSelectingUtility.selectedText;
        private int m_iAltCursorPos => m_TextSelectingUtility.iAltCursorPos;
        int m_CursorIndexSavedState = -1;
        internal bool isCompositionActive;
        bool m_UpdateImeWindowPosition;
        internal Action OnTextChanged;

        public bool multiline = false;
        internal bool revealCursor
        {
            get => m_TextSelectingUtility.revealCursor;
            set => m_TextSelectingUtility.revealCursor = value;
        }

        //Used by automated tests
        internal int stringCursorIndex
        {
            get => textHandle.GetCorrespondingStringIndex(cursorIndex);
            set => cursorIndex = textHandle.GetCorrespondingCodePointIndex(value);
        }

        private int cursorIndex
        {
            get => m_TextSelectingUtility.cursorIndex;
            set => m_TextSelectingUtility.cursorIndex = value;
        }

        //Used by automated tests
        internal int stringSelectIndex
        {
            get => textHandle.GetCorrespondingStringIndex(selectIndex);
            set => selectIndex = textHandle.GetCorrespondingCodePointIndex(value);
        }

        private int selectIndex
        {
            get => m_TextSelectingUtility.selectIndex;
            set => m_TextSelectingUtility.selectIndex = value;
        }

        string m_Text;
        public string text
        {
            get => m_Text;
            set
            {
                if (value == m_Text)
                    return;

                m_Text = value ?? string.Empty;
                OnTextChanged?.Invoke();
            }
        }

        internal void SetTextWithoutNotify(string value)
        {
            m_Text = value;
        }

        public TextEditingUtilities(TextSelectingUtilities selectingUtilities, TextHandle textHandle, string text)
        {
            m_TextSelectingUtility = selectingUtilities;
            this.textHandle = textHandle;
            m_Text = text;
        }

        /// <summary>
        /// Checks if IME is active and updates the internal states. This should be run each update.
        /// </summary>
        public bool UpdateImeState()
        {
            if (GUIUtility.compositionString.Length > 0)
            {
                if (!isCompositionActive)
                {
                    m_UpdateImeWindowPosition = true;
                    ReplaceSelection(string.Empty);
                }

                isCompositionActive = true;
            }
            else
            {
                isCompositionActive = false;
            }

            return isCompositionActive;
        }

        public bool ShouldUpdateImeWindowPosition()
        {
            return m_UpdateImeWindowPosition;
        }

        public void SetImeWindowPosition(Vector2 worldPosition)
        {
            var cursorPos = textHandle.GetCursorPositionFromStringIndexUsingCharacterHeight(cursorIndex, true);
            GUIUtility.compositionCursorPos = worldPosition + cursorPos;
        }

        public string GeneratePreviewString(bool richText)
        {
            RestoreCursorState();
            var compositionString = GUIUtility.compositionString;
            if (isCompositionActive)
            {
                return richText ? text.Insert(stringCursorIndex, $"<u>{compositionString}</u>") : text.Insert(stringCursorIndex, compositionString);
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
            cursorIndex = selectIndex = m_CursorIndexSavedState + GUIUtility.compositionString.Length;
        }

        /// <summary>
        /// Restores the cursor back to its original position after previewing the IME string cursor position with <see cref="SetCursorPreviewState(int)"/>
        /// </summary>
        public void RestoreCursorState()
        {
            if (m_CursorIndexSavedState == -1)
                return;

            cursorIndex = selectIndex = m_CursorIndexSavedState;
            m_CursorIndexSavedState = -1;
        }

        [VisibleToOtherModules]
        internal bool HandleKeyEvent(Event e)
        {
            RestoreCursorState();
            InitKeyActions();
            EventModifiers m = e.modifiers;
            e.modifiers &= ~EventModifiers.CapsLock;
            if (s_KeyEditOps.ContainsKey(e))
            {
                TextEditOp op = (TextEditOp)s_KeyEditOps[e];
                PerformOperation(op);
                e.modifiers = m;
                return true;
            }
            e.modifiers = m;
            return false;
        }

        void PerformOperation(TextEditOp operation)
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
                case TextEditOp.Delete: Delete(); break;
                case TextEditOp.Backspace: Backspace(); break;
                case TextEditOp.Cut: Cut(); break;
                case TextEditOp.Paste: Paste(); break;
                //      case TextEditOp.ScrollStart:            return ScrollStart (); break;
                //      case TextEditOp.ScrollEnd:          return ScrollEnd (); break;
                //      case TextEditOp.ScrollPageUp:       return ScrollPageUp (); break;
                //      case TextEditOp.ScrollPageDown:     return ScrollPageDown (); break;
                case TextEditOp.DeleteWordBack: DeleteWordBack(); break;
                case TextEditOp.DeleteLineBack: DeleteLineBack(); break;
                case TextEditOp.DeleteWordForward: DeleteWordForward(); break;
                default:
                    Debug.Log("Unimplemented: " + operation);
                    break;
            }
        }


        static void MapKey(string key, TextEditOp action)
        {
            s_KeyEditOps[Event.KeyboardEvent(key)] = action;
        }

        static Dictionary<Event, TextEditOp> s_KeyEditOps;
        /// Set up a platform independent keyboard->Edit action map. This varies depending on whether we are on mac or windows.
        void InitKeyActions()
        {
            if (s_KeyEditOps != null)
                return;
            s_KeyEditOps = new Dictionary<Event, TextEditOp>();

            // key mappings shared by the platforms
            MapKey("left", TextEditOp.MoveLeft);
            MapKey("right", TextEditOp.MoveRight);
            MapKey("up", TextEditOp.MoveUp);
            MapKey("down", TextEditOp.MoveDown);

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

                MapKey("%x", TextEditOp.Cut);
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

                // TODO         MapKey ("#page up", TextEditOp.SelectPageUp);
                // TODO         MapKey ("#page down", TextEditOp.SelectPageDown);

                MapKey("^delete", TextEditOp.DeleteWordForward);
                MapKey("^backspace", TextEditOp.DeleteWordBack);
                MapKey("%backspace", TextEditOp.DeleteLineBack);

                MapKey("^x", TextEditOp.Cut);
                MapKey("^v", TextEditOp.Paste);
                MapKey("#delete", TextEditOp.Cut);
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

            var currentLineInfo = textHandle.GetLineInfoFromCharacterIndex(cursorIndex);
            var startIndex = currentLineInfo.firstCharacterIndex;
            var stringStartIndex = textHandle.GetCorrespondingStringIndex(startIndex);

            if (startIndex != cursorIndex)
            {
                text = text.Remove(stringStartIndex, stringCursorIndex - stringStartIndex);
                cursorIndex = selectIndex = startIndex;
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
                int prevWordEndString = textHandle.GetCorrespondingStringIndex(prevWordEnd);
                text = text.Remove(prevWordEndString, stringCursorIndex - prevWordEndString);
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
                int nextWordStartString = textHandle.GetCorrespondingStringIndex(nextWordStart);
                text = text.Remove(stringCursorIndex, nextWordStartString - stringCursorIndex);
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
            else if (stringCursorIndex < text.Length)
            {
                var count = textHandle.textInfo.textElementInfo[cursorIndex].stringLength;
                text = text.Remove(stringCursorIndex, count);
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
                var count = textHandle.textInfo.textElementInfo[cursorIndex - 1].stringLength;
                text = text.Remove(stringCursorIndex - count, count);
                cursorIndex = startIndex;
                selectIndex = startIndex;
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
                text = text.Substring(0, stringCursorIndex) + text.Substring(stringSelectIndex, text.Length - stringSelectIndex);
                selectIndex = cursorIndex;
            }
            else
            {
                text = text.Substring(0, stringSelectIndex) + text.Substring(stringCursorIndex, text.Length - stringCursorIndex);
                cursorIndex = selectIndex;
            }
            m_TextSelectingUtility.ClearCursorPos();

            return true;
        }

        /// Replace the selection with /replace/. If there is no selection, /replace/ is inserted at the current cursor point.
        public void ReplaceSelection(string replace)
        {
            RestoreCursorState();
            DeleteSelection();
            text = text.Insert(stringCursorIndex, replace);

            var newIndex = cursorIndex + new StringInfo(replace).LengthInTextElements;
            cursorIndex = newIndex;
            selectIndex = newIndex;
            m_TextSelectingUtility.ClearCursorPos();
        }

        /// Replaced the selection with /c/
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
                if (!multiline)
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

        // Returns true if the TouchScreenKeyboard should be used. On Android and Chrome OS, we only want to use the
        // TouchScreenKeyboard if in-place editing is not allowed (i.e. when we do not have a hardware keyboard available).
        internal bool TouchScreenKeyboardShouldBeUsed()
        {
            RuntimePlatform platform = Application.platform;
            switch (platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.WebGLPlayer:
                    return !TouchScreenKeyboard.isInPlaceEditingAllowed;
                default:
                    return TouchScreenKeyboard.isSupported;
            }
        }
    }
}
