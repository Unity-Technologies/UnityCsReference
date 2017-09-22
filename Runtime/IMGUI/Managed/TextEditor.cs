// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class TextEditor
    {
        public TouchScreenKeyboard keyboardOnScreen = null;
        public int controlID = 0;
        public GUIStyle style = GUIStyle.none;
        public bool multiline = false;
        public bool hasHorizontalCursorPos = false;
        public bool isPasswordField = false;
        internal bool m_HasFocus;
        public Vector2 scrollOffset = Vector2.zero; // The text field can have a scroll offset in order to display its contents

        private GUIContent m_Content = new GUIContent();
        private Rect m_Position;
        private int m_CursorIndex = 0;
        private int m_SelectIndex = 0;
        private bool m_RevealCursor = false;

        [Obsolete("Please use 'text' instead of 'content'", false)]
        public GUIContent content
        {
            get { return m_Content; }
            set { m_Content = value; }
        }

        public string text
        {
            get { return m_Content.text; }
            set
            {
                m_Content.text = value ?? string.Empty;

                ClampTextIndex(ref m_CursorIndex);
                ClampTextIndex(ref m_SelectIndex);
            }
        }

        public Rect position
        {
            get { return m_Position; }
            set
            {
                if (m_Position == value)
                    return;

                m_Position = value;

                UpdateScrollOffset();
            }
        }

        internal virtual Rect localPosition { get { return position; } }

        public int cursorIndex
        {
            get { return m_CursorIndex; }
            set
            {
                int oldCursorIndex = m_CursorIndex;
                m_CursorIndex = value;
                ClampTextIndex(ref m_CursorIndex);
                if (m_CursorIndex != oldCursorIndex)
                    m_RevealCursor = true;
            }
        }

        public int selectIndex
        {
            get { return m_SelectIndex; }
            set
            {
                m_SelectIndex = value;
                ClampTextIndex(ref m_SelectIndex);
            }
        }

        // are we up/downing?
        public Vector2 graphicalCursorPos;
        public Vector2 graphicalSelectCursorPos;

        // Clear the cursor position for vertical movement...
        void ClearCursorPos() {hasHorizontalCursorPos = false; m_iAltCursorPos = -1; }

        // selection
        bool m_MouseDragSelectsWholeWords = false;
        int m_DblClickInitPos = 0;
        DblClickSnapping m_DblClickSnap = DblClickSnapping.WORDS;
        public DblClickSnapping doubleClickSnapping { get { return m_DblClickSnap; } set { m_DblClickSnap = value; } }
        bool m_bJustSelected = false;

        int m_iAltCursorPos = -1;
        public int altCursorPosition { get { return m_iAltCursorPos; } set { m_iAltCursorPos = value; } }

        public enum DblClickSnapping : byte { WORDS, PARAGRAPHS };

        [RequiredByNativeCode]
        public TextEditor()
        {
        }

        public void OnFocus()
        {
            if (multiline)
                cursorIndex = selectIndex = 0;
            else
                SelectAll();
            m_HasFocus = true;
        }

        public void OnLostFocus()
        {
            m_HasFocus = false;
            scrollOffset = Vector2.zero;
        }

        void GrabGraphicalCursorPos()
        {
            if (!hasHorizontalCursorPos)
            {
                graphicalCursorPos = style.GetCursorPixelPosition(localPosition, m_Content, cursorIndex);
                graphicalSelectCursorPos = style.GetCursorPixelPosition(localPosition, m_Content, selectIndex);
                hasHorizontalCursorPos = false;
            }
        }

        // Handle a key event.
        // Looks up the platform-dependent key-action table & performs the event
        // return true if the event was recognized.
        public bool HandleKeyEvent(Event e)
        {
            InitKeyActions();
            EventModifiers m = e.modifiers;
            e.modifiers &= ~EventModifiers.CapsLock;
            if (s_Keyactions.ContainsKey(e))
            {
                TextEditOp op = (TextEditOp)s_Keyactions[e];
                PerformOperation(op);
                e.modifiers = m;
                return true;
            }
            e.modifiers = m;
            return false;
        }

        // Deletes previous text on the line
        public bool DeleteLineBack()
        {
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
                m_Content.text = text.Remove(p, cursorIndex - p);
                selectIndex = cursorIndex = p;
                return true;
            }
            return false;
        }

        // Deletes the previous word
        public bool DeleteWordBack()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }

            int prevWordEnd = FindEndOfPreviousWord(cursorIndex);
            if (cursorIndex != prevWordEnd)
            {
                m_Content.text = text.Remove(prevWordEnd, cursorIndex - prevWordEnd);
                selectIndex = cursorIndex = prevWordEnd;
                return true;
            }
            return false;
        }

        // Deletes the following word
        public bool DeleteWordForward()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }

            int nextWordStart = FindStartOfNextWord(cursorIndex);
            if (cursorIndex < text.Length)
            {
                m_Content.text = text.Remove(cursorIndex, nextWordStart - cursorIndex);
                return true;
            }
            return false;
        }

        // perform a right-delete
        public bool Delete()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            else if (cursorIndex < text.Length)
            {
                m_Content.text = text.Remove(cursorIndex, 1);
                return true;
            }
            return false;
        }

        public bool CanPaste()
        {
            return GUIUtility.systemCopyBuffer.Length != 0;
        }

        // Perform a left-delete
        public bool Backspace()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            else if (cursorIndex > 0)
            {
                m_Content.text = text.Remove(cursorIndex - 1, 1);
                selectIndex = cursorIndex = cursorIndex - 1;
                ClearCursorPos();
                return true;
            }
            return false;
        }

        /// Select all the text
        public void SelectAll()
        {
            cursorIndex = 0; selectIndex = text.Length;
            ClearCursorPos();
        }

        /// Select none of the text
        public void SelectNone()
        {
            selectIndex = cursorIndex;
            ClearCursorPos();
        }

        /// Does this text field has a selection
        public bool hasSelection { get { return cursorIndex != selectIndex; } }

        /// Returns the selected text
        public string SelectedText
        {
            get
            {
                if (cursorIndex == selectIndex)
                    return "";
                if (cursorIndex < selectIndex)
                    return text.Substring(cursorIndex, selectIndex - cursorIndex);
                else
                    return text.Substring(selectIndex, cursorIndex - selectIndex);
            }
        }

        /// Delete the current selection. If there is no selection, this function does not do anything...
        public bool DeleteSelection()
        {
            if (cursorIndex == selectIndex)
                return false;
            if (cursorIndex < selectIndex)
            {
                m_Content.text = text.Substring(0, cursorIndex) + text.Substring(selectIndex, text.Length - selectIndex);
                selectIndex = cursorIndex;
            }
            else
            {
                m_Content.text = text.Substring(0, selectIndex) + text.Substring(cursorIndex, text.Length - cursorIndex);
                cursorIndex = selectIndex;
            }
            ClearCursorPos();

            return true;
        }

        /// Replace the selection with /replace/. If there is no selection, /replace/ is inserted at the current cursor point.
        public void ReplaceSelection(string replace)
        {
            DeleteSelection();
            m_Content.text = text.Insert(cursorIndex, replace);
            selectIndex = cursorIndex += replace.Length;
            ClearCursorPos();
        }

        /// Replacted the selection with /c/
        public void Insert(char c)
        {
            ReplaceSelection(c.ToString());
        }

        /// Move selection to alt cursor /position/
        public void MoveSelectionToAltCursor()
        {
            if (m_iAltCursorPos == -1)
                return;
            int p = m_iAltCursorPos;
            string tmp = SelectedText;
            m_Content.text = text.Insert(p, tmp);

            if (p < cursorIndex)
            {
                cursorIndex += tmp.Length;
                selectIndex += tmp.Length;
            }

            DeleteSelection();

            selectIndex = cursorIndex = p;
            ClearCursorPos();
        }

        /// Move the cursor one character to the right and deselect.
        public void MoveRight()
        {
            ClearCursorPos();
            if (selectIndex == cursorIndex)
            {
                cursorIndex++;
                DetectFocusChange(); // TODO: Is this necessary?
                selectIndex = cursorIndex;
            }
            else
            {
                if (selectIndex > cursorIndex)
                    cursorIndex = selectIndex;
                else
                    selectIndex = cursorIndex;
            }
        }

        /// Move the cursor one character to the left and deselect.
        public void MoveLeft()
        {
            if (selectIndex == cursorIndex)
            {
                cursorIndex--;
                selectIndex = cursorIndex;
            }
            else
            {
                if (selectIndex > cursorIndex)
                    selectIndex = cursorIndex;
                else
                    cursorIndex = selectIndex;
            }
            ClearCursorPos();
        }

        /// Move the cursor up and deselects.
        public void MoveUp()
        {
            if (selectIndex < cursorIndex)
                selectIndex = cursorIndex;
            else
                cursorIndex = selectIndex;
            GrabGraphicalCursorPos();
            graphicalCursorPos.y -=  1;
            cursorIndex = selectIndex = style.GetCursorStringIndex(localPosition, m_Content, graphicalCursorPos);
            if (cursorIndex <= 0)
                ClearCursorPos();
        }

        /// Move the cursor down and deselects.
        public void MoveDown()
        {
            if (selectIndex > cursorIndex)
                selectIndex = cursorIndex;
            else
                cursorIndex = selectIndex;
            GrabGraphicalCursorPos();
            graphicalCursorPos.y += style.lineHeight + 5;
            cursorIndex = selectIndex = style.GetCursorStringIndex(localPosition, m_Content, graphicalCursorPos);
            if (cursorIndex == text.Length)
                ClearCursorPos();
        }

        /// Moves the cursor to the start of the current line.
        public void MoveLineStart()
        {
            // we start from the left-most selected character
            int p = selectIndex < cursorIndex ? selectIndex : cursorIndex;
            // then we scan back to find the first newline
            int i = p;
            while (i-- != 0)
                if (text[i] == '\n')
                {
                    selectIndex = cursorIndex = i + 1;
                    return;
                }
            selectIndex = cursorIndex = 0;
        }

        /// Moves the selection to the end of the current line
        public void MoveLineEnd()
        {
            // we start from the right-most selected character
            int p = selectIndex > cursorIndex ? selectIndex : cursorIndex;
            // then we scan forward to find the first newline
            int i = p;
            int strlen = text.Length;
            while (i < strlen)
            {
                if (text[i] == '\n')
                {
                    selectIndex = cursorIndex = i;
                    return;
                }
                i++;
            }
            selectIndex = cursorIndex = strlen;
        }

        /// Move to the start of the current graphical line. This takes word-wrapping into consideration.
        public void MoveGraphicalLineStart()
        {
            cursorIndex = selectIndex = GetGraphicalLineStart(cursorIndex < selectIndex ? cursorIndex : selectIndex);
        }

        /// Move to the end of the current graphical line. This takes word-wrapping into consideration.
        public void MoveGraphicalLineEnd()
        {
            cursorIndex = selectIndex = GetGraphicalLineEnd(cursorIndex > selectIndex ? cursorIndex : selectIndex);
        }

        /// Moves the cursor to the beginning of the text
        public void MoveTextStart()
        {
            selectIndex = cursorIndex = 0;
        }

        /// Moves the cursor to the end of the text
        public void MoveTextEnd()
        {
            selectIndex = cursorIndex = text.Length;
        }

        private int IndexOfEndOfLine(int startIndex)
        {
            int index = text.IndexOf('\n', startIndex);
            return (index != -1 ? index : text.Length);
        }

        /// Move to the next paragraph
        public void MoveParagraphForward()
        {
            cursorIndex = cursorIndex > selectIndex ? cursorIndex : selectIndex;
            if (cursorIndex < text.Length)
            {
                selectIndex = cursorIndex = IndexOfEndOfLine(cursorIndex + 1);
            }
        }

        /// Move to the previous paragraph
        public void MoveParagraphBackward()
        {
            cursorIndex = cursorIndex < selectIndex ? cursorIndex : selectIndex;
            if (cursorIndex > 1)
            {
                selectIndex = cursorIndex = text.LastIndexOf('\n', cursorIndex - 2) + 1;
            }
            else
                selectIndex = cursorIndex =  0;
        }

        //

        // Move the cursor to a graphical position. Used for moving the cursor on MouseDown events.
        public void MoveCursorToPosition(Vector2 cursorPosition)
        {
            MoveCursorToPosition_Internal(cursorPosition, Event.current.shift);
        }

        // Move the cursor to a graphical position. Used for moving the cursor on MouseDown events.
        protected internal void MoveCursorToPosition_Internal(Vector2 cursorPosition, bool shift)
        {
            selectIndex = style.GetCursorStringIndex(localPosition, m_Content, cursorPosition + scrollOffset);

            if (!shift)
            {
                cursorIndex = selectIndex;
            }

            DetectFocusChange(); // TODO: Is this necessary?
        }

        public void MoveAltCursorToPosition(Vector2 cursorPosition)
        {
            int index = style.GetCursorStringIndex(localPosition, m_Content, cursorPosition + scrollOffset);
            m_iAltCursorPos = Mathf.Min(text.Length, index);
            DetectFocusChange(); // TODO: Is this necessary?
        }

        public bool IsOverSelection(Vector2 cursorPosition)
        {
            int p = style.GetCursorStringIndex(localPosition, m_Content, cursorPosition + scrollOffset);
            return ((p < Mathf.Max(cursorIndex, selectIndex)) && (p > Mathf.Min(cursorIndex, selectIndex)));
        }

        // Do a drag selection. Used to expand the selection in MouseDrag events.
        public void SelectToPosition(Vector2 cursorPosition)
        {
            if (!m_MouseDragSelectsWholeWords)
                cursorIndex = style.GetCursorStringIndex(localPosition, m_Content, cursorPosition + scrollOffset);
            else // snap to words/paragraphs
            {
                int p = style.GetCursorStringIndex(localPosition, m_Content, cursorPosition + scrollOffset);


                if (m_DblClickSnap == DblClickSnapping.WORDS)
                {
                    if (p < m_DblClickInitPos)
                    {
                        cursorIndex = FindEndOfClassification(p, -1);
                        selectIndex = FindEndOfClassification(m_DblClickInitPos, +1);
                    }
                    else
                    {
                        if (p >= text.Length)
                            p = text.Length - 1;
                        cursorIndex = FindEndOfClassification(p, +1);
                        selectIndex = FindEndOfClassification(m_DblClickInitPos - 1, -1);
                    }
                } // paragraph
                else
                {
                    if (p < m_DblClickInitPos)
                    {
                        if (p > 0)
                            cursorIndex = text.LastIndexOf('\n', Mathf.Max(0, p - 2)) + 1;
                        else
                            cursorIndex = 0;

                        selectIndex = text.LastIndexOf('\n', m_DblClickInitPos);
                    }
                    else
                    {
                        if (p < text.Length)
                        {
                            cursorIndex = IndexOfEndOfLine(p);
                        }
                        else
                            cursorIndex = text.Length;

                        selectIndex = text.LastIndexOf('\n', Mathf.Max(0, m_DblClickInitPos - 2)) + 1;
                    }
                }
            }
        }

        /// Expand the selection to the left
        public void SelectLeft()
        {
            if (m_bJustSelected)
                if (cursorIndex > selectIndex)
                { // swap
                    int tmp = cursorIndex;
                    cursorIndex = selectIndex;
                    selectIndex = tmp;
                }
            m_bJustSelected = false;

            cursorIndex--;
        }

        public void SelectRight()
        {
            if (m_bJustSelected)
                if (cursorIndex < selectIndex)
                { // swap
                    int tmp = cursorIndex;
                    cursorIndex = selectIndex;
                    selectIndex = tmp;
                }
            m_bJustSelected = false;

            cursorIndex++;
        }

        public void SelectUp()
        {
            GrabGraphicalCursorPos();
            graphicalCursorPos.y -= 1;
            cursorIndex = style.GetCursorStringIndex(localPosition, m_Content, graphicalCursorPos);
        }

        public void SelectDown()
        {
            GrabGraphicalCursorPos();
            graphicalCursorPos.y += style.lineHeight + 5;
            cursorIndex = style.GetCursorStringIndex(localPosition, m_Content, graphicalCursorPos);
        }

        /// Select to the end of the text
        public void SelectTextEnd()
        {
            // This is not quite like the mac - there, when you select to end of text, the position of the cursor becomes somewhat i'll defined
            // Hard to explain. In textedit, try: CMD-SHIFT-down, SHIFT-LEFT for case 1. then do CMD-SHIFT-down, SHIFT-RIGHT, SHIFT-LEFT for case 2.
            // Anyways, it's wrong so we won't do that
            cursorIndex = text.Length;
        }

        /// Select to the start of the text
        public void SelectTextStart()
        {
            // Same thing as SelectTextEnd...
            cursorIndex = 0;
        }

        /// sets whether the text selection is done by dbl click or not
        public void MouseDragSelectsWholeWords(bool on)
        {
            m_MouseDragSelectsWholeWords = on;
            m_DblClickInitPos = cursorIndex;
        }

        public void DblClickSnap(DblClickSnapping snapping)
        {
            m_DblClickSnap = snapping;
        }

        int GetGraphicalLineStart(int p)
        {
            Vector2 point = style.GetCursorPixelPosition(localPosition, m_Content, p);
            point.x = 0;
            return style.GetCursorStringIndex(localPosition, m_Content, point);
        }

        int GetGraphicalLineEnd(int p)
        {
            Vector2 point = style.GetCursorPixelPosition(localPosition, m_Content, p);
            point.x += 5000;
            return style.GetCursorStringIndex(localPosition, m_Content, point);
        }

        int FindNextSeperator(int startPos)
        {
            int textLen = text.Length;
            while (startPos < textLen && !isLetterLikeChar(text[startPos]))
                startPos++;
            while (startPos < textLen && isLetterLikeChar(text[startPos]))
                startPos++;
            return startPos;
        }

        static bool isLetterLikeChar(char c)
        {
            return System.Char.IsLetterOrDigit(c) || c == '\'';
        }

        int FindPrevSeperator(int startPos)
        {
            startPos--;
            while (startPos > 0 && !isLetterLikeChar(text[startPos]))
                startPos--;

            while (startPos >= 0 && isLetterLikeChar(text[startPos]))
                startPos--;
            return startPos + 1;
        }

        /// Move to the end of the word.
        /// If the cursor is over some space characters, these are skipped
        /// Then, the cursor moves to the end of the following word.
        /// This corresponds to Alt-RightArrow on a Mac
        public void MoveWordRight()
        {
            cursorIndex = cursorIndex > selectIndex ? cursorIndex : selectIndex;
            cursorIndex = selectIndex = FindNextSeperator(cursorIndex);
            ClearCursorPos();
        }

        public void MoveToStartOfNextWord()
        {
            ClearCursorPos();
            if (cursorIndex != selectIndex)
            {
                MoveRight();
                return;
            }
            cursorIndex = selectIndex = FindStartOfNextWord(cursorIndex);
        }

        public void MoveToEndOfPreviousWord()
        {
            ClearCursorPos();
            if (cursorIndex != selectIndex)
            {
                MoveLeft();
                return;
            }
            cursorIndex = selectIndex = FindEndOfPreviousWord(cursorIndex);
        }

        public void SelectToStartOfNextWord()
        {
            ClearCursorPos();
            cursorIndex = FindStartOfNextWord(cursorIndex);
        }

        public void SelectToEndOfPreviousWord()
        {
            ClearCursorPos();
            cursorIndex = FindEndOfPreviousWord(cursorIndex);
        }

        enum CharacterType
        {
            LetterLike,
            Symbol, Symbol2,
            WhiteSpace
        }

        CharacterType ClassifyChar(char c)
        {
            if (System.Char.IsWhiteSpace(c))
                return CharacterType.WhiteSpace;
            if (System.Char.IsLetterOrDigit(c) || c == '\'')
                return CharacterType.LetterLike;
            return CharacterType.Symbol;
        }

        /// Move to start of next word.
        /// This corresponds to Ctrl-RightArrow on Windows
        /// If the cursor is over a whitespace, it's moved forwards ''till the first non-whitespace character
        /// If the cursor is over an alphanumeric character, it''s moved forward 'till it encounters space or a punctuation mark.
        /// If the stopping character is a space, this is skipped as well.
        /// If the cursor is over an punctuation mark, it's moved forward ''till it a letter or a space of a punctuation mark. If the stopping character is a space, this is skipped as well
        public int FindStartOfNextWord(int p)
        {
            int textLen = text.Length;
            if (p == textLen)
                return p;

            // Find out which char type we're at...
            char c = text[p];
            CharacterType t = ClassifyChar(c);
            if (t != CharacterType.WhiteSpace)
            {
                p++;
                while (p < textLen && ClassifyChar(text[p]) == t)
                    p++;
            }
            else
            {
                if (c == '\t' || c == '\n')
                    return p + 1;
            }

            if (p == textLen)
                return p;

            // Skip spaces
            c = text[p];
            if (c == ' ')                       // If we're at a space, skip over any number of spaces
            {
                while (p < textLen && System.Char.IsWhiteSpace(text[p]))
                    p++;
            }
            else if (c == '\t' || c == '\n')            // If we're at a tab or a newline, just step one char ahead
            {
                return p;
            }
            return p;
        }

        int FindEndOfPreviousWord(int p)
        {
            if (p == 0)
                return p;
            p--;

            // Skip spaces
            while (p > 0 && text[p] == ' ')
                p--;

            CharacterType t = ClassifyChar(text[p]);
            if (t != CharacterType.WhiteSpace)
            {
                while (p >  0 && ClassifyChar(text[p - 1]) == t)
                    p--;
            }
            return p;
        }

        public void MoveWordLeft()
        {
            cursorIndex = cursorIndex < selectIndex ? cursorIndex : selectIndex;
            cursorIndex = FindPrevSeperator(cursorIndex);
            selectIndex = cursorIndex;
        }

        public void SelectWordRight()
        {
            ClearCursorPos();
            int cachedPos = selectIndex;
            if (cursorIndex < selectIndex)
            {
                selectIndex = cursorIndex;
                MoveWordRight();
                selectIndex = cachedPos;
                cursorIndex = cursorIndex < selectIndex ? cursorIndex : selectIndex;
                return;
            }
            selectIndex = cursorIndex;
            MoveWordRight();
            selectIndex = cachedPos;
        }

        public void SelectWordLeft()
        {
            ClearCursorPos();
            int cachedPos = selectIndex;
            if (cursorIndex > selectIndex)
            {
                selectIndex = cursorIndex;
                MoveWordLeft();
                selectIndex = cachedPos;
                cursorIndex = cursorIndex > selectIndex ? cursorIndex : selectIndex;
                return;
            }
            selectIndex = cursorIndex;
            MoveWordLeft();
            selectIndex = cachedPos;
        }

        /// Expand the selection to the start of the line
        /// Used on a mac for CMD-SHIFT-LEFT
        public void ExpandSelectGraphicalLineStart()
        {
            ClearCursorPos();
            if (cursorIndex < selectIndex)
                cursorIndex = GetGraphicalLineStart(cursorIndex);
            else
            {
                int temp = cursorIndex;
                cursorIndex = GetGraphicalLineStart(selectIndex);
                selectIndex = temp;
            }
        }

        /// Expand the selection to the end of the line
        /// Used on a mac for CMD-SHIFT-RIGHT
        public void ExpandSelectGraphicalLineEnd()
        {
            ClearCursorPos();
            if (cursorIndex > selectIndex)
                cursorIndex = GetGraphicalLineEnd(cursorIndex);
            else
            {
                int temp = cursorIndex;
                cursorIndex = GetGraphicalLineEnd(selectIndex);
                selectIndex = temp;
            }
        }

        /// Move the selection point to the start of the line
        /// Used on a Windows for SHIFT-Home
        public void SelectGraphicalLineStart()
        {
            ClearCursorPos();
            cursorIndex = GetGraphicalLineStart(cursorIndex);
        }

        /// Expand the selection to the end of the line
        /// Used on a mac for SHIFT-End
        public void SelectGraphicalLineEnd()
        {
            ClearCursorPos();
            cursorIndex = GetGraphicalLineEnd(cursorIndex);
        }

        public void SelectParagraphForward()
        {
            ClearCursorPos();
            bool wasBehind = cursorIndex < selectIndex;
            if (cursorIndex < text.Length)
            {
                cursorIndex = IndexOfEndOfLine(cursorIndex + 1);
                if (wasBehind && cursorIndex > selectIndex)
                    cursorIndex = selectIndex;
            }
        }

        public void SelectParagraphBackward()
        {
            ClearCursorPos();
            bool wasInFront = cursorIndex > selectIndex;
            if (cursorIndex > 1)
            {
                cursorIndex = text.LastIndexOf('\n', cursorIndex - 2) + 1;
                if (wasInFront && cursorIndex < selectIndex)
                    cursorIndex = selectIndex;
            }
            else
                selectIndex = cursorIndex =  0;
        }

        /// Select the word under the cursor
        public void SelectCurrentWord()
        {
            ClearCursorPos();

            int textLen = text.Length;
            selectIndex = cursorIndex;

            // Handle that the text box can be empty
            if (textLen == 0)
                return;

            if (cursorIndex >= textLen)
                cursorIndex = textLen - 1;
            if (selectIndex >= textLen)
                selectIndex--;

            if (cursorIndex < selectIndex)
            {
                cursorIndex = FindEndOfClassification(cursorIndex, -1);
                selectIndex = FindEndOfClassification(selectIndex, +1);
            }
            else
            {
                cursorIndex = FindEndOfClassification(cursorIndex, +1);
                selectIndex = FindEndOfClassification(selectIndex, -1);
            }

            m_bJustSelected = true;
        }

        int FindEndOfClassification(int p, int dir)
        {
            int textLen = text.Length;

            if (p >= textLen || p < 0)
                return p;

            CharacterType t = ClassifyChar(text[p]);
            do
            {
                p += dir;
                if (p < 0)
                    return 0;
                if (p >= textLen)
                    return textLen;
            }
            while (ClassifyChar(text[p]) == t);
            if (dir == 1)
                return p;
            return p + 1;
        }

        // Select the entire paragraph the cursor is on (separated by \n)
        public void SelectCurrentParagraph()
        {
            ClearCursorPos();
            int textLen = text.Length;

            if (cursorIndex < textLen)
            {
                cursorIndex = IndexOfEndOfLine(cursorIndex) + 1;
            }
            if (selectIndex != 0)
                selectIndex = text.LastIndexOf('\n', selectIndex - 1) + 1;
        }

        public void UpdateScrollOffsetIfNeeded(Event evt)
        {
            if (evt.type != EventType.Repaint && evt.type != EventType.Layout)
            {
                UpdateScrollOffset();
            }
        }

        protected void UpdateScrollOffset()
        {
            int cursorPos = cursorIndex;
            graphicalCursorPos = style.GetCursorPixelPosition(new Rect(0, 0, position.width, position.height), m_Content, cursorPos);

            //Debug.Log("Cursor pos: " + graphicalCursorPos);

            Rect r = style.padding.Remove(position);

            Vector2 contentSize = new Vector2(style.CalcSize(m_Content).x, style.CalcHeight(m_Content, position.width));

            // If there is plenty of room, simply show entire string
            if (contentSize.x < position.width)
            {
                scrollOffset.x = 0;
            }
            else if (m_RevealCursor)
            {
                //go right
                if (graphicalCursorPos.x + 1 > scrollOffset.x + r.width)
                    // do we want html or apple behavior? this is html behavior
                    scrollOffset.x = graphicalCursorPos.x - r.width;
                //go left
                if (graphicalCursorPos.x < scrollOffset.x + style.padding.left)
                    scrollOffset.x = graphicalCursorPos.x - style.padding.left;
            }
            // ... and height/y as well
            // If there is plenty of room, simply show entire string
            // Debug.Log(contentSize.y + " < R : " + r);
            if (contentSize.y < r.height)
            {
                scrollOffset.y = 0;
            }
            else if (m_RevealCursor)
            {
                //go down
                if (graphicalCursorPos.y + style.lineHeight > scrollOffset.y + r.height + style.padding.top)
                    scrollOffset.y = graphicalCursorPos.y - r.height - style.padding.top + style.lineHeight;
                //go up
                if (graphicalCursorPos.y < scrollOffset.y + style.padding.top)
                    scrollOffset.y = graphicalCursorPos.y - style.padding.top;
            }

            // This case takes many words to explain:
            // 1. Text field has more text than it can fit vertically, and the cursor is at the very bottom (text field is scrolled down)
            // 2. user e.g. deletes some lines of text at the bottom (backspace or select+delete)
            // 3. now suddenly we have space at the bottom of text field, that is now not filled with any content
            // 4. scroll text field up to fill in that space (this is what other text editors do)
            if (scrollOffset.y > 0 && contentSize.y - scrollOffset.y < r.height)
                scrollOffset.y = contentSize.y - r.height - style.padding.top - style.padding.bottom;

            scrollOffset.y = scrollOffset.y < 0 ? 0 : scrollOffset.y;

            m_RevealCursor = false;
        }

        // TODO: get the height from the font

        public void DrawCursor(string newText)
        {
            string realText = text;
            int cursorPos = cursorIndex;
            if (Input.compositionString.Length > 0)
            {
                m_Content.text = newText.Substring(0, cursorIndex) + Input.compositionString + newText.Substring(selectIndex);
                cursorPos += Input.compositionString.Length;
            }
            else
                m_Content.text = newText;

            graphicalCursorPos = style.GetCursorPixelPosition(new Rect(0, 0, position.width, position.height), m_Content, cursorPos);

            //Debug.Log("Cursor pos: " + graphicalCursorPos);

            Vector2 originalContentOffset = style.contentOffset;
            style.contentOffset -= scrollOffset;
            style.Internal_clipOffset = scrollOffset;

            // Debug.Log ("ScrollOffset : " + scrollOffset);

            Input.compositionCursorPos = graphicalCursorPos + new Vector2(position.x, position.y + style.lineHeight) - scrollOffset;

            if (Input.compositionString.Length > 0)
                style.DrawWithTextSelection(position, m_Content, controlID, cursorIndex, cursorIndex + Input.compositionString.Length, true);
            else
                style.DrawWithTextSelection(position, m_Content, controlID, cursorIndex, selectIndex);

            if (m_iAltCursorPos != -1)
                style.DrawCursor(position, m_Content, controlID, m_iAltCursorPos);

            // reset
            style.contentOffset = originalContentOffset;
            style.Internal_clipOffset = Vector2.zero;

            m_Content.text = realText;
        }

        bool PerformOperation(TextEditOp operation)
        {
            m_RevealCursor = true;

            switch (operation)
            {
                // NOTE the TODOs below:
                case TextEditOp.MoveLeft:           MoveLeft(); break;
                case TextEditOp.MoveRight:          MoveRight(); break;
                case TextEditOp.MoveUp:             MoveUp(); break;
                case TextEditOp.MoveDown:           MoveDown(); break;
                case TextEditOp.MoveLineStart:      MoveLineStart(); break;
                case TextEditOp.MoveLineEnd:        MoveLineEnd(); break;
                case TextEditOp.MoveWordRight:      MoveWordRight(); break;
                case TextEditOp.MoveToStartOfNextWord:      MoveToStartOfNextWord(); break;
                case TextEditOp.MoveToEndOfPreviousWord:        MoveToEndOfPreviousWord(); break;
                case TextEditOp.MoveWordLeft:       MoveWordLeft(); break;
                case TextEditOp.MoveTextStart:      MoveTextStart(); break;
                case TextEditOp.MoveTextEnd:        MoveTextEnd(); break;
                case TextEditOp.MoveParagraphForward:   MoveParagraphForward(); break;
                case TextEditOp.MoveParagraphBackward:  MoveParagraphBackward(); break;
                //      case TextEditOp.MovePageUp:     return MovePageUp (); break;
                //      case TextEditOp.MovePageDown:       return MovePageDown (); break;
                case TextEditOp.MoveGraphicalLineStart: MoveGraphicalLineStart(); break;
                case TextEditOp.MoveGraphicalLineEnd: MoveGraphicalLineEnd(); break;
                case TextEditOp.SelectLeft:         SelectLeft(); break;
                case TextEditOp.SelectRight:            SelectRight(); break;
                case TextEditOp.SelectUp:           SelectUp(); break;
                case TextEditOp.SelectDown:         SelectDown(); break;
                case TextEditOp.SelectWordRight:        SelectWordRight(); break;
                case TextEditOp.SelectWordLeft:     SelectWordLeft(); break;
                case TextEditOp.SelectToEndOfPreviousWord:  SelectToEndOfPreviousWord(); break;
                case TextEditOp.SelectToStartOfNextWord:    SelectToStartOfNextWord(); break;

                case TextEditOp.SelectTextStart:        SelectTextStart(); break;
                case TextEditOp.SelectTextEnd:      SelectTextEnd(); break;
                case TextEditOp.ExpandSelectGraphicalLineStart: ExpandSelectGraphicalLineStart(); break;
                case TextEditOp.ExpandSelectGraphicalLineEnd: ExpandSelectGraphicalLineEnd(); break;
                case TextEditOp.SelectParagraphForward:     SelectParagraphForward(); break;
                case TextEditOp.SelectParagraphBackward:    SelectParagraphBackward(); break;
                case TextEditOp.SelectGraphicalLineStart: SelectGraphicalLineStart(); break;
                case TextEditOp.SelectGraphicalLineEnd: SelectGraphicalLineEnd(); break;
                //      case TextEditOp.SelectPageUp:                   return SelectPageUp (); break;
                //      case TextEditOp.SelectPageDown:             return SelectPageDown (); break;
                case TextEditOp.Delete:                             return Delete();
                case TextEditOp.Backspace:                      return Backspace();
                case TextEditOp.Cut:                                    return Cut();
                case TextEditOp.Copy:                               Copy(); break;
                case TextEditOp.Paste:                              return Paste();
                case TextEditOp.SelectAll:                          SelectAll(); break;
                case TextEditOp.SelectNone:                     SelectNone(); break;
                //      case TextEditOp.ScrollStart:            return ScrollStart (); break;
                //      case TextEditOp.ScrollEnd:          return ScrollEnd (); break;
                //      case TextEditOp.ScrollPageUp:       return ScrollPageUp (); break;
                //      case TextEditOp.ScrollPageDown:     return ScrollPageDown (); break;
                case TextEditOp.DeleteWordBack: return DeleteWordBack(); // break; // The uncoditional return makes the "break;" issue a warning about unreachable code
                case TextEditOp.DeleteLineBack: return DeleteLineBack();
                case TextEditOp.DeleteWordForward: return DeleteWordForward(); // break; // The uncoditional return makes the "break;" issue a warning about unreachable code
                default:
                    Debug.Log("Unimplemented: " + operation);
                    break;
            }

            return false;
        }

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
        };

        string oldText;
        int oldPos, oldSelectPos;

        public void SaveBackup()
        {
            oldText = text;
            oldPos = cursorIndex;
            oldSelectPos = selectIndex;
        }

        public void Undo()
        {
            m_Content.text = oldText;
            cursorIndex = oldPos;
            selectIndex = oldSelectPos;
        }

        public bool Cut()
        {
            //Debug.Log ("Cut");
            if (isPasswordField)
                return false;
            Copy();
            return DeleteSelection();
        }

        public void Copy()
        {
            //Debug.Log ("Copy");
            if (selectIndex == cursorIndex)
                return;

            if (isPasswordField)
                return;

            string copyStr;
            if (cursorIndex < selectIndex)
                copyStr = text.Substring(cursorIndex, selectIndex - cursorIndex);
            else
                copyStr = text.Substring(selectIndex, cursorIndex - selectIndex);

            GUIUtility.systemCopyBuffer = copyStr;
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

        public bool Paste()
        {
            //Debug.Log ("Paste");
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

        static void MapKey(string key, TextEditOp action)
        {
            s_Keyactions[Event.KeyboardEvent(key)] = action;
        }

        static Dictionary<Event, TextEditOp> s_Keyactions;
        /// Set up a platform independant keyboard->Edit action map. This varies depending on whether we are on mac or windows.
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

        public void DetectFocusChange()
        {
            OnDetectFocusChange();
        }

        internal virtual void OnDetectFocusChange()
        {
            if (m_HasFocus == true && controlID != GUIUtility.keyboardControl)
                OnLostFocus();
            if (m_HasFocus == false && controlID == GUIUtility.keyboardControl)
                OnFocus();
        }

        private void ClampTextIndex(ref int index)
        {
            index = Mathf.Clamp(index, 0, text.Length);
        }
    }
} // namespace
