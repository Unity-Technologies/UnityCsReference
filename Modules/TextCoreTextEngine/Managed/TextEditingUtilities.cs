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

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal class TextEditingUtilities
    {
        private TextSelectingUtilities m_TextSelectingUtility;

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal TextHandle textHandle;
        private bool hasSelection => m_TextSelectingUtility.hasSelection;
        private string SelectedText => m_TextSelectingUtility.selectedText;
        private int m_iAltCursorPos => m_TextSelectingUtility.iAltCursorPos;
        int m_CursorIndexSavedState = -1;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal bool isCompositionActive;

        bool m_UpdateImeWindowPosition;

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal Action OnTextChanged;

        public bool multiline = false;
        internal bool revealCursor
        {
            get => m_TextSelectingUtility.revealCursor;
            set => m_TextSelectingUtility.revealCursor = value;
        }

        //Used by automated tests
        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
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

        private int cursorIndexNoValidation {
            get => m_TextSelectingUtility.cursorIndexNoValidation;
            set => m_TextSelectingUtility.cursorIndexNoValidation = value;
        }
        private int selectIndexNoValidation {
            get => m_TextSelectingUtility.selectIndexNoValidation;
            set => m_TextSelectingUtility.selectIndexNoValidation = value;
        }

        private int stringCursorIndexNoValidation {
            get => textHandle.GetCorrespondingStringIndex(m_TextSelectingUtility.cursorIndexNoValidation);
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
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

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
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
            if (Input.compositionString.Length > 0)
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
            Input.compositionCursorPos = worldPosition + cursorPos;
        }

        public string GeneratePreviewString(bool richText)
        {
            RestoreCursorState();
            var compositionString = Input.compositionString;
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

            m_CursorIndexSavedState = m_TextSelectingUtility.cursorIndexNoValidation;
            cursorIndexNoValidation = selectIndexNoValidation = m_CursorIndexSavedState + Input.compositionString.Length;
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

        public bool HandleKeyEvent(KeyCode key, EventModifiers modifiers)
        {
            var op = TextEditOpFromEnum(key, modifiers, (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX));
            if (op.HasValue)
            {
                PerformOperation(op.Value);
                return true;
            }
            return false;
        }

        //Used for tests
        internal record struct KeyEvent(KeyCode key, EventModifiers modifiers);

        //Used for tests
        internal static readonly List<(KeyEvent keyEvent, TextEditOp operation)> s_GlobalKeyMappings = new()
        {
            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.FunctionKey), TextEditOp.MoveLeft),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.FunctionKey), TextEditOp.MoveRight),
            (new KeyEvent(KeyCode.UpArrow, EventModifiers.FunctionKey), TextEditOp.MoveUp),
            (new KeyEvent(KeyCode.DownArrow, EventModifiers.FunctionKey), TextEditOp.MoveDown),
            (new KeyEvent(KeyCode.Delete, EventModifiers.FunctionKey), TextEditOp.Delete),
            (new KeyEvent(KeyCode.Backspace, EventModifiers.FunctionKey), TextEditOp.Backspace),
            (new KeyEvent(KeyCode.Backspace, EventModifiers.Shift | EventModifiers.FunctionKey), TextEditOp.Backspace)
        };

        //Used for tests
        internal static readonly List<(KeyEvent keyEvent, TextEditOp operation)> s_MacKeyMappings = new()
        {
            // Keyboard mappings for mac
            // TODO     MapKey ("home"):return TextEditOp.
            // ;
            // TODO     MapKey ("end"):return TextEditOp.ScrollEnd;
            // TODO     MapKey ("page up"):return TextEditOp.ScrollPageUp;
            // TODO     MapKey ("page down"):return TextEditOp.ScrollPageDown;
            // TODO     MapKey ("page up"):return TextEditOp.ScrollPageUp;
            // TODO     MapKey ("page down"):return TextEditOp.ScrollPageDown;
            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.MoveGraphicalLineStart),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.MoveGraphicalLineEnd),
            // TODO     MapKey ("^up"):return TextEditOp.ScrollPageUp;
            // TODO     MapKey ("^down"):return TextEditOp.ScrollPageDown;
            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.Alt | EventModifiers.FunctionKey), TextEditOp.MoveWordLeft),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.Alt | EventModifiers.FunctionKey), TextEditOp.MoveWordRight),
            (new KeyEvent(KeyCode.UpArrow, EventModifiers.Alt | EventModifiers.FunctionKey), TextEditOp.MoveParagraphBackward),
            (new KeyEvent(KeyCode.DownArrow, EventModifiers.Alt | EventModifiers.FunctionKey), TextEditOp.MoveParagraphForward),

            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveGraphicalLineStart),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveGraphicalLineEnd),
            (new KeyEvent(KeyCode.UpArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveTextStart),
            (new KeyEvent(KeyCode.DownArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveTextEnd),

            (new KeyEvent(KeyCode.X, EventModifiers.Command), TextEditOp.Cut),
            (new KeyEvent(KeyCode.V, EventModifiers.Command), TextEditOp.Paste),

            // emacs-like keybindings
            (new KeyEvent(KeyCode.D, EventModifiers.Control), TextEditOp.Delete),
            (new KeyEvent(KeyCode.H, EventModifiers.Control), TextEditOp.Backspace),
            (new KeyEvent(KeyCode.B, EventModifiers.Control), TextEditOp.MoveLeft),
            (new KeyEvent(KeyCode.F, EventModifiers.Control), TextEditOp.MoveRight),
            (new KeyEvent(KeyCode.A, EventModifiers.Control), TextEditOp.MoveLineStart),
            (new KeyEvent(KeyCode.E, EventModifiers.Control), TextEditOp.MoveLineEnd),

            (new KeyEvent(KeyCode.Delete, EventModifiers.Alt | EventModifiers.FunctionKey), TextEditOp.DeleteWordForward),
            (new KeyEvent(KeyCode.Backspace, EventModifiers.Alt | EventModifiers.FunctionKey), TextEditOp.DeleteWordBack),
            (new KeyEvent(KeyCode.Backspace, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.DeleteLineBack)
        };

        internal static readonly List<(KeyEvent keyEvent, TextEditOp operation)> s_WindowsLinuxKeyMappings = new()
        {
            (new KeyEvent(KeyCode.Home, EventModifiers.FunctionKey), TextEditOp.MoveGraphicalLineStart),
            (new KeyEvent(KeyCode.End, EventModifiers.FunctionKey), TextEditOp.MoveGraphicalLineEnd),
            // TODO     MapKey ("page up"):return TextEditOp.MovePageUp;
            // TODO     MapKey ("page down"):return TextEditOp.MovePageDown;
            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveWordLeft),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveWordRight),
            (new KeyEvent(KeyCode.UpArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveParagraphBackward),
            (new KeyEvent(KeyCode.DownArrow, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.MoveParagraphForward),

            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.MoveToEndOfPreviousWord),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.MoveToStartOfNextWord),
            (new KeyEvent(KeyCode.UpArrow, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.MoveParagraphBackward),
            (new KeyEvent(KeyCode.DownArrow, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.MoveParagraphForward),

            // TODO         MapKey ("#page up"):return TextEditOp.SelectPageUp;
            // TODO         MapKey ("#page down"):return TextEditOp.SelectPageDown;

            (new KeyEvent(KeyCode.Delete, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.DeleteWordForward),
            (new KeyEvent(KeyCode.Backspace, EventModifiers.Control | EventModifiers.FunctionKey), TextEditOp.DeleteWordBack),
            (new KeyEvent(KeyCode.Backspace, EventModifiers.Command | EventModifiers.FunctionKey), TextEditOp.DeleteLineBack),

            (new KeyEvent(KeyCode.X, EventModifiers.Control), TextEditOp.Cut),
            (new KeyEvent(KeyCode.V, EventModifiers.Control), TextEditOp.Paste),
            (new KeyEvent(KeyCode.Delete, EventModifiers.Shift | EventModifiers.FunctionKey), TextEditOp.Cut),
            (new KeyEvent(KeyCode.Insert, EventModifiers.Shift | EventModifiers.FunctionKey), TextEditOp.Paste)

        };

        //Used for tests
        internal static TextEditOp? TextEditOpFromEnum(KeyCode key, EventModifiers modifiers, bool IsMacOsFamily)
        {
            //Capslock is always ignored for actions
            modifiers &= ~EventModifiers.CapsLock;

            var keyEvent = new KeyEvent(key, modifiers);
            foreach (var mapping in s_GlobalKeyMappings)
            {
                if (mapping.keyEvent == keyEvent) 
                    return mapping.operation;
            }

            foreach (var mapping in IsMacOsFamily? s_MacKeyMappings : s_WindowsLinuxKeyMappings)
            {
                if (mapping.keyEvent == keyEvent)
                    return mapping.operation;
            }

            return null;
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

        // Deletes previous text on the line
        public bool DeleteLineBack()
        {
            RestoreCursorState();

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }

            if (textHandle.useAdvancedText)
            {
                var start = textHandle.GetFirstCharacterIndexOnLine(cursorIndex);
                if (start != cursorIndex)
                {
                    text = text.Remove(start, stringCursorIndex - start);
                    cursorIndex = selectIndex = start;
                    return true;
                }
                return false;
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
            else if (textHandle.useAdvancedText && stringCursorIndex < text.Length)
            {
                int startIndex = 0;

                if (cursorIndex == 0 && textHandle.IsMainDirectionRTL())
                    startIndex = m_TextSelectingUtility.PreviousCodePointIndex(cursorIndex);
                else
                    startIndex = m_TextSelectingUtility.NextCodePointIndex(cursorIndex);

                int count = Mathf.Abs(startIndex - cursorIndex);
                text = text.Remove(stringCursorIndex, count);
                return true;
            }
            else if (stringCursorIndex < text.Length)
            {
                int count = textHandle.textInfo.textElementInfo[cursorIndex].stringLength;
                text = text.Remove(stringCursorIndex, count);
                return true;
            }
            return false;
        }

        // Perform a left-delete
        public bool Backspace()
        {
            RestoreCursorState();
            int prevCursorIndex = cursorIndex;
            int prevSelectIndex = selectIndex;

            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            else if (textHandle.useAdvancedText && cursorIndex > 0)
            {
                int startIndex = 0;

                if (cursorIndex == text.Length && textHandle.IsMainDirectionRTL())
                    startIndex = m_TextSelectingUtility.NextCodePointIndex(cursorIndex);
                else
                    startIndex  = m_TextSelectingUtility.PreviousCodePointIndex(cursorIndex);

                int count = Mathf.Abs(cursorIndex - startIndex);
                text = text.Remove(stringCursorIndex - count, count);
                cursorIndex = Math.Max(0, prevCursorIndex - count) ;
                selectIndex = Math.Max(0, prevSelectIndex - count);
                m_TextSelectingUtility.ClearCursorPos();
                return true;
            }
            else if (cursorIndex > 0)
            {
                var startIndex = m_TextSelectingUtility.PreviousCodePointIndex(cursorIndex);
                int count = textHandle.textInfo.textElementInfo[cursorIndex - 1].stringLength;

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

            int length = textHandle.useAdvancedText ? replace.Length : new StringInfo(replace).LengthInTextElements;
            var newIndex = cursorIndexNoValidation + length;
            cursorIndexNoValidation = newIndex;
            selectIndexNoValidation = newIndex;
            m_TextSelectingUtility.ClearCursorPos();
        }

        private char m_HighSurrogate;

        /// Replaced the selection with /c/
        public bool Insert(char c)
        {
            if (char.IsHighSurrogate(c))
            {
                m_HighSurrogate = c;
                return false;
            }
            else if (char.IsLowSurrogate(c))
            {
                var lowSurrogate = c;
                string combinedString = new string(new char[] { m_HighSurrogate, lowSurrogate });
                ReplaceSelection(combinedString.ToString());
                return true;
            }
            ReplaceSelection(c.ToString());
            return true;
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
            return StytemCopyBuffer.systemCopyBuffer.Length != 0;
        }

        public bool Cut()
        {
            m_TextSelectingUtility.Copy();
            return DeleteSelection();
        }

        public bool Paste()
        {
            RestoreCursorState();
            string pasteval = StytemCopyBuffer.systemCopyBuffer;
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

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void OnBlur()
        {
            revealCursor = false;
            isCompositionActive = false;
            RestoreCursorState();
            m_TextSelectingUtility.SelectNone();
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal bool TouchScreenKeyboardCanBeUsed()
        {
            return TouchScreenKeyboard.isSupported;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal bool PhysicalKeyboardCanBeUsed()
        {
            return TouchScreenKeyboard.isSupported ? TouchScreenKeyboard.isInPlaceEditingAllowed : true;
        }
    }
}
