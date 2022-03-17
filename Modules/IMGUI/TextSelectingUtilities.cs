// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;
using static UnityEngine.TextEditor;

namespace UnityEngine
{
    internal class TextSelectingUtilities
    {
        public DblClickSnapping dblClickSnap = DblClickSnapping.WORDS;
        public int iAltCursorPos = -1;
        public event Action OnTextChanged;
        public bool multiline = false;
        public bool hasHorizontalCursorPos = false;

        private int m_CursorIndex = 0;
        private int m_SelectIndex = 0;
        private string m_Text;
        private bool m_bJustSelected = false;
        private bool m_MouseDragSelectsWholeWords = false;
        private int m_DblClickInitPos = 0;
        TextHandle m_TextHandle;
        private const int kMoveDownHeight = 5;
        private const char kNewLineChar = '\n';

        /// Does this text field has a selection
        public bool hasSelection { get { return cursorIndex != selectIndex; } }

        bool m_RevealCursor;
        public bool revealCursor
        {
            get => m_RevealCursor;
            set
            {
                if (value != m_RevealCursor)
                {
                    m_RevealCursor = value;
                    OnCursorIndexChange?.Invoke();
                }
            }
        }

        public string text
        {
            get { return m_Text; }
            set
            {
                if (value == m_Text)
                    return;
                m_Text = value ?? string.Empty;
                EnsureValidCodePointIndex(ref m_CursorIndex);
                EnsureValidCodePointIndex(ref m_SelectIndex);
                OnTextChanged?.Invoke();
            }
        }

        public int cursorIndex
        {
            get { return m_CursorIndex; }
            set
            {
                int oldCursorIndex = m_CursorIndex;
                m_CursorIndex = value;
                EnsureValidCodePointIndex(ref m_CursorIndex);

                if (m_CursorIndex != oldCursorIndex)
                {
                    revealCursor = true;
                    OnCursorIndexChange?.Invoke();
                }
            }
        }

        public int selectIndex
        {
            get { return m_SelectIndex; }
            set
            {
                int oldSelectIndex = m_SelectIndex;
                m_SelectIndex = value;
                EnsureValidCodePointIndex(ref m_SelectIndex);

                if (m_SelectIndex != oldSelectIndex)
                    OnSelectIndexChange?.Invoke();
            }
        }

        /// Returns the selected text
        public string selectedText
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

        public TextSelectingUtilities(TextHandle textHandle)
        {
            m_TextHandle = textHandle;
            m_Text = String.Empty;
        }

        internal void SetCursorNoCheck(int cursor)
        {
            m_CursorIndex = cursor;
            m_SelectIndex = cursor;
        }

        public void ClearCursorPos()
        {
            hasHorizontalCursorPos = false;
            iAltCursorPos = -1;
        }

        public void OnFocus(bool selectAll = true)
        {
            if (selectAll)
                SelectAll();
            else
                cursorIndex = selectIndex = 0;
            revealCursor = true;
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

            cursorIndex = PreviousCodePointIndex(cursorIndex);
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

            cursorIndex = NextCodePointIndex(cursorIndex);
        }

        public void SelectUp()
        {
            cursorIndex = m_TextHandle.LineUpCharacterPosition(cursorIndex);
        }

        public void SelectDown()
        {
            cursorIndex = m_TextHandle.LineDownCharacterPosition(cursorIndex);
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
                cursorIndex = text.LastIndexOf(kNewLineChar, cursorIndex - 2) + 1;
                if (wasInFront && cursorIndex < selectIndex)
                    cursorIndex = selectIndex;
            }
            else
                selectIndex = cursorIndex = 0;
        }

        /// Select the word under the cursor
        public void SelectCurrentWord()
        {
            var index = cursorIndex;
            if (cursorIndex < selectIndex)
            {
                cursorIndex = FindEndOfClassification(index, Direction.Backward);
                selectIndex = FindEndOfClassification(index, Direction.Forward);
            }
            else
            {
                cursorIndex = FindEndOfClassification(index, Direction.Forward);
                selectIndex = FindEndOfClassification(index, Direction.Backward);
            }

            ClearCursorPos();
            m_bJustSelected = true;
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
                selectIndex = text.LastIndexOf(kNewLineChar, selectIndex - 1) + 1;
        }

        /// Move the cursor one character to the right and deselect.
        public void MoveRight()
        {
            ClearCursorPos();
            if (selectIndex == cursorIndex)
            {
                cursorIndex = NextCodePointIndex(cursorIndex);
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
                cursorIndex = PreviousCodePointIndex(cursorIndex);
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
            cursorIndex = selectIndex = m_TextHandle.LineUpCharacterPosition(cursorIndex);
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
            cursorIndex = selectIndex = m_TextHandle.LineDownCharacterPosition(cursorIndex);
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
                if (text[i] == kNewLineChar)
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
                if (text[i] == kNewLineChar)
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
                selectIndex = cursorIndex = text.LastIndexOf(kNewLineChar, cursorIndex - 2) + 1;
            }
            else
                selectIndex = cursorIndex = 0;
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

        public void MoveWordLeft()
        {
            cursorIndex = cursorIndex < selectIndex ? cursorIndex : selectIndex;
            cursorIndex = FindPrevSeperator(cursorIndex);
            selectIndex = cursorIndex;
        }

        /// sets whether the text selection is done by dbl click or not
        public void MouseDragSelectsWholeWords(bool on)
        {
            m_MouseDragSelectsWholeWords = on;
            m_DblClickInitPos = cursorIndex;
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

        public void DblClickSnap(DblClickSnapping snapping)
        {
            dblClickSnap = snapping;
        }

        protected internal void MoveCursorToPosition_Internal(Vector2 cursorPosition, bool shift)
        {
            selectIndex = m_TextHandle.GetCursorIndexFromPosition(cursorPosition);

            if (!shift)
            {
                cursorIndex = selectIndex;
            }
        }

        // Do a drag selection. Used to expand the selection in MouseDrag events.
        public void SelectToPosition(Vector2 cursorPosition)
        {
            if (!m_MouseDragSelectsWholeWords)
                cursorIndex = m_TextHandle.GetCursorIndexFromPosition(cursorPosition);
            else // snap to words/paragraphs
            {
                int p = m_TextHandle.GetCursorIndexFromPosition(cursorPosition);

                EnsureValidCodePointIndex(ref p);
                EnsureValidCodePointIndex(ref m_DblClickInitPos);

                if (dblClickSnap == DblClickSnapping.WORDS)
                {
                    if (p < m_DblClickInitPos)
                    {
                        cursorIndex = FindEndOfClassification(p, Direction.Backward);
                        selectIndex = FindEndOfClassification(m_DblClickInitPos, Direction.Forward);
                    }
                    else
                    {
                        cursorIndex = FindEndOfClassification(p, Direction.Forward);
                        selectIndex = FindEndOfClassification(m_DblClickInitPos, Direction.Backward);
                    }
                } // paragraph
                else
                {
                    if (p < m_DblClickInitPos)
                    {
                        if (p > 0)
                            cursorIndex = text.LastIndexOf(kNewLineChar, Mathf.Max(0, p - 2)) + 1;
                        else
                            cursorIndex = 0;

                        selectIndex = text.LastIndexOf(kNewLineChar, Mathf.Min(text.Length - 1, m_DblClickInitPos));
                    }
                    else
                    {
                        if (p < text.Length)
                        {
                            cursorIndex = IndexOfEndOfLine(p);
                        }
                        else
                            cursorIndex = text.Length;

                        selectIndex = text.LastIndexOf(kNewLineChar, Mathf.Max(0, m_DblClickInitPos - 2)) + 1;
                    }
                }
            }
        }

        int FindNextSeperator(int startPos)
        {
            int textLen = text.Length;
            while (startPos < textLen && ClassifyChar(startPos) != CharacterType.LetterLike)
                startPos = NextCodePointIndex(startPos);
            while (startPos < textLen && ClassifyChar(startPos) == CharacterType.LetterLike)
                startPos = NextCodePointIndex(startPos);
            return startPos;
        }

        int FindPrevSeperator(int startPos)
        {
            startPos = PreviousCodePointIndex(startPos);
            while (startPos > 0 && ClassifyChar(startPos) != CharacterType.LetterLike)
                startPos = PreviousCodePointIndex(startPos);

            if (startPos == 0)
                return 0;

            while (startPos > 0 && ClassifyChar(startPos) == CharacterType.LetterLike)
                startPos = PreviousCodePointIndex(startPos);

            if (ClassifyChar(startPos) == CharacterType.LetterLike)
                return startPos;
            return NextCodePointIndex(startPos);
        }

        public int FindStartOfNextWord(int p)
        {
            int textLen = text.Length;
            if (p == textLen)
                return p;

            // Find out which char type we're at...
            CharacterType t = ClassifyChar(p);
            if (t != CharacterType.WhiteSpace)
            {
                p = NextCodePointIndex(p);
                while (p < textLen && ClassifyChar(p) == t)
                    p = NextCodePointIndex(p);
            }
            else
            {
                if (text[p] == '\t' || text[p] == kNewLineChar)
                    return NextCodePointIndex(p);
            }

            if (p == textLen)
                return p;

            // Skip spaces
            if (text[p] == ' ') // If we're at a space, skip over any number of spaces
            {
                while (p < textLen && ClassifyChar(p) == CharacterType.WhiteSpace)
                    p = NextCodePointIndex(p);
            }
            else if (text[p] == '\t' || text[p] == kNewLineChar) // If we're at a tab or a newline, just step one char ahead
            {
                return p;
            }
            return p;
        }

        public int FindEndOfPreviousWord(int p)
        {
            if (p == 0)
                return p;
            p = PreviousCodePointIndex(p);

            // Skip spaces
            while (p > 0 && text[p] == ' ')
                p = PreviousCodePointIndex(p);

            CharacterType t = ClassifyChar(p);
            if (t != CharacterType.WhiteSpace)
            {
                while (p > 0 && ClassifyChar(PreviousCodePointIndex(p)) == t)
                    p = PreviousCodePointIndex(p);
            }
            return p;
        }

        int FindEndOfClassification(int p, Direction dir)
        {
            if (text.Length == 0)
                return 0;

            if (p == text.Length)
                p = PreviousCodePointIndex(p);

            var t = ClassifyChar(p);
            if (t == CharacterType.NewLine)
                return p;
            do
            {
                switch (dir)
                {
                    case Direction.Backward:
                        p = PreviousCodePointIndex(p);
                        if (p == 0)
                            return ClassifyChar(0) == t ? 0 : NextCodePointIndex(0);
                        break;

                    case Direction.Forward:
                        p = NextCodePointIndex(p);
                        if (p == text.Length)
                            return text.Length;
                        break;
                }
            }
            while (ClassifyChar(p) == t);
            if (dir == Direction.Forward)
                return p;
            return NextCodePointIndex(p);
        }

        internal Action OnCursorIndexChange;

        internal Action OnSelectIndexChange;

        void ClampTextIndex(ref int index)
        {
            index = Mathf.Clamp(index, 0, text.Length);
        }

        void EnsureValidCodePointIndex(ref int index)
        {
            ClampTextIndex(ref index);
            if (!IsValidCodePointIndex(index))
                index = NextCodePointIndex(index);
        }

        bool IsValidCodePointIndex(int index)
        {
            if (index < 0 || index > text.Length)
                return false;
            if (index == 0 || index == text.Length)
                return true;
            return !char.IsLowSurrogate(text[index]);
        }

        int IndexOfEndOfLine(int startIndex)
        {
            int index = text.IndexOf(kNewLineChar, startIndex);
            return (index != -1 ? index : text.Length);
        }

        public int PreviousCodePointIndex(int index)
        {
            if (index > 0)
                index--;
            while (index > 0 && char.IsLowSurrogate(text[index]))
                index--;
            return index;
        }

        public int NextCodePointIndex(int index)
        {
            if (index < text.Length)
                index++;
            while (index < text.Length && char.IsLowSurrogate(text[index]))
                index++;
            return index;
        }

        int GetGraphicalLineStart(int p)
        {
            Vector2 point = m_TextHandle.GetCursorPositionFromStringIndexUsingLineHeight(p);
            point.y -= 1.0f / GUIUtility.pixelsPerPoint; // we make sure no floating point errors can make us land on another line
            point.x = 0;
            return m_TextHandle.GetCursorIndexFromPosition(point);
        }

        int GetGraphicalLineEnd(int p)
        {
            Vector2 point = m_TextHandle.GetCursorPositionFromStringIndexUsingLineHeight(p);
            point.y -= 1.0f / GUIUtility.pixelsPerPoint; // we make sure no floating point errors can make us land on another line
            point.x += 5000;
            return m_TextHandle.GetCursorIndexFromPosition(point);
        }

        public void Copy()
        {
            if (selectIndex == cursorIndex)
                return;

            GUIUtility.systemCopyBuffer = selectedText;
        }

        enum CharacterType
        {
            LetterLike,
            Symbol, Symbol2,
            WhiteSpace,
            NewLine,
        }

        enum Direction
        {
            Forward,
            Backward,
        }

        CharacterType ClassifyChar(int index)
        {
            if (text[index] == kNewLineChar)
                return CharacterType.NewLine;
            if (char.IsWhiteSpace(text, index))
                return CharacterType.WhiteSpace;
            if (char.IsLetterOrDigit(text, index) || text[index] == '\'')
                return CharacterType.LetterLike;
            return CharacterType.Symbol;
        }
    }
}
