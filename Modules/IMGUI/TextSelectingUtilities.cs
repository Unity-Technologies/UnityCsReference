// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;
using static UnityEngine.TextEditor;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class TextSelectingUtilities
    {
        public DblClickSnapping dblClickSnap = DblClickSnapping.WORDS;
        public int iAltCursorPos = -1;
        public bool hasHorizontalCursorPos = false;

        private bool m_bJustSelected = false;
        private bool m_MouseDragSelectsWholeWords = false;
        private int m_DblClickInitPos = 0;
        public TextHandle textHandle;
        private const int kMoveDownHeight = 5;
        private const char kNewLineChar = '\n';

        /// Does this text field has a selection
        public bool hasSelection => cursorIndex != selectIndex;

        bool m_RevealCursor;
        public bool revealCursor
        {
            get => m_RevealCursor;
            set
            {
                if (m_RevealCursor != value)
                {
                    m_RevealCursor = value;
                    OnRevealCursorChange?.Invoke();
                }
            }
        }

        int m_CharacterCount => textHandle.textInfo.characterCount;
        int characterCount => (m_CharacterCount > 0 && textHandle.textInfo.textElementInfo[m_CharacterCount - 1].character == 0x200B) ? m_CharacterCount - 1 : m_CharacterCount;
        TextElementInfo[] m_TextElementInfos => textHandle.textInfo.textElementInfo;

        int m_CursorIndex = 0;
        public int cursorIndex
        {
            get => textHandle.IsPlaceholder ? 0 : ClampTextIndex(m_CursorIndex);
            set
            {
                if (m_CursorIndex != value)
                {
                    m_CursorIndex = value;
                    OnCursorIndexChange?.Invoke();
                }
            }
        }
        internal int cursorIndexNoValidation
        {
            get { return m_CursorIndex; }
            set
            {
                if (m_CursorIndex != value)
                {
                    SetCursorIndexWithoutNotify(value);
                    OnCursorIndexChange?.Invoke();
                }
            }
        }

        internal void SetCursorIndexWithoutNotify(int index)
        {
            m_CursorIndex = index;
        }

        internal int m_SelectIndex = 0;
        public int selectIndex
        {
            get => textHandle.IsPlaceholder ? 0 : ClampTextIndex(m_SelectIndex);
            set
            {

                if (m_SelectIndex != value)
                {
                    SetSelectIndexWithoutNotify(value);
                    OnSelectIndexChange?.Invoke();
                }
            }
        }
        internal void SetSelectIndexWithoutNotify(int index)
        {
            m_SelectIndex = index;
        }

        /// Returns the selected text
        public string selectedText
        {
            get
            {
                if (cursorIndex == selectIndex)
                    return "";

                if (cursorIndex < selectIndex)
                    return textHandle.Substring(cursorIndex, selectIndex - cursorIndex);
                else
                    return textHandle.Substring(selectIndex, cursorIndex - selectIndex);
            }
        }


        public TextSelectingUtilities(TextHandle textHandle)
        {
            this.textHandle = textHandle;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal bool HandleKeyEvent(Event e)
        {
            InitKeyActions();
            EventModifiers m = e.modifiers;
            e.modifiers &= ~EventModifiers.CapsLock;
            if (s_KeySelectOps.ContainsKey(e))
            {
                var op = (TextSelectOp)s_KeySelectOps[e];
                PerformOperation(op);
                e.modifiers = m;
                return true;
            }
            e.modifiers = m;
            return false;
        }

        bool PerformOperation(TextSelectOp operation)
        {
            switch (operation)
            {
                case TextSelectOp.SelectLeft: SelectLeft(); break;
                case TextSelectOp.SelectRight: SelectRight(); break;
                case TextSelectOp.SelectUp: SelectUp(); break;
                case TextSelectOp.SelectDown: SelectDown(); break;
                case TextSelectOp.SelectWordRight: SelectWordRight(); break;
                case TextSelectOp.SelectWordLeft: SelectWordLeft(); break;
                case TextSelectOp.SelectToEndOfPreviousWord: SelectToEndOfPreviousWord(); break;
                case TextSelectOp.SelectToStartOfNextWord: SelectToStartOfNextWord(); break;
                case TextSelectOp.SelectTextStart: SelectTextStart(); break;
                case TextSelectOp.SelectTextEnd: SelectTextEnd(); break;
                case TextSelectOp.ExpandSelectGraphicalLineStart: ExpandSelectGraphicalLineStart(); break;
                case TextSelectOp.ExpandSelectGraphicalLineEnd: ExpandSelectGraphicalLineEnd(); break;
                case TextSelectOp.SelectParagraphForward: SelectParagraphForward(); break;
                case TextSelectOp.SelectParagraphBackward: SelectParagraphBackward(); break;
                case TextSelectOp.SelectGraphicalLineStart: SelectGraphicalLineStart(); break;
                case TextSelectOp.SelectGraphicalLineEnd: SelectGraphicalLineEnd(); break;
                case TextSelectOp.Copy: Copy(); break;
                case TextSelectOp.SelectAll: SelectAll(); break;
                case TextSelectOp.SelectNone: SelectNone(); break;
                 default:
                     Debug.Log("Unimplemented: " + operation);
                     break;
            }

            return false;
        }

        static Dictionary<Event, TextSelectOp> s_KeySelectOps;
        static void MapKey(string key, TextSelectOp action)
        {
            s_KeySelectOps[Event.KeyboardEvent(key)] = action;
        }

        void InitKeyActions()
        {
            if (s_KeySelectOps != null)
                return;
            s_KeySelectOps = new Dictionary<Event, TextSelectOp>();

            // key mappings shared by the platforms
            MapKey("#left", TextSelectOp.SelectLeft);
            MapKey("#right", TextSelectOp.SelectRight);
            MapKey("#up", TextSelectOp.SelectUp);
            MapKey("#down", TextSelectOp.SelectDown);

            // OSX is the special case for input shortcuts
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                // Keyboard mappings for mac
                MapKey("#home", TextSelectOp.SelectTextStart);
                MapKey("#end", TextSelectOp.SelectTextEnd);

                MapKey("#^left", TextSelectOp.ExpandSelectGraphicalLineStart);
                MapKey("#^right", TextSelectOp.ExpandSelectGraphicalLineEnd);
                MapKey("#^up", TextSelectOp.SelectParagraphBackward);
                MapKey("#^down", TextSelectOp.SelectParagraphForward);

                MapKey("#&left", TextSelectOp.SelectWordLeft);
                MapKey("#&right", TextSelectOp.SelectWordRight);
                MapKey("#&up", TextSelectOp.SelectParagraphBackward);
                MapKey("#&down", TextSelectOp.SelectParagraphForward);

                MapKey("#%left", TextSelectOp.ExpandSelectGraphicalLineStart);
                MapKey("#%right", TextSelectOp.ExpandSelectGraphicalLineEnd);
                MapKey("#%up", TextSelectOp.SelectTextStart);
                MapKey("#%down", TextSelectOp.SelectTextEnd);

                MapKey("%a", TextSelectOp.SelectAll);
                MapKey("%c", TextSelectOp.Copy);
            }
            else
            {
                // Windows/Linux keymappings
                MapKey("#^left", TextSelectOp.SelectToEndOfPreviousWord);
                MapKey("#^right", TextSelectOp.SelectToStartOfNextWord);
                MapKey("#^up", TextSelectOp.SelectParagraphBackward);
                MapKey("#^down", TextSelectOp.SelectParagraphForward);

                MapKey("#home", TextSelectOp.SelectGraphicalLineStart);
                MapKey("#end", TextSelectOp.SelectGraphicalLineEnd);

                MapKey("^a", TextSelectOp.SelectAll);
                MapKey("^c", TextSelectOp.Copy);
                MapKey("^insert", TextSelectOp.Copy);
            }
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

            revealCursor = true;
        }

        /// Select all the text
        public void SelectAll()
        {
            cursorIndex = 0; selectIndex = Int32.MaxValue;
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
            cursorIndex = textHandle.LineUpCharacterPosition(cursorIndex);
        }

        public void SelectDown()
        {
            cursorIndex = textHandle.LineDownCharacterPosition(cursorIndex);
        }

        /// Select to the end of the text
        public void SelectTextEnd()
        {
            // This is not quite like the mac - there, when you select to end of text, the position of the cursor becomes somewhat i'll defined
            // Hard to explain. In textedit, try: CMD-SHIFT-down, SHIFT-LEFT for case 1. then do CMD-SHIFT-down, SHIFT-RIGHT, SHIFT-LEFT for case 2.
            // Anyways, it's wrong so we won't do that
            cursorIndex = characterCount;
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
            if (cursorIndex < characterCount)
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
                cursorIndex = textHandle.LastIndexOf(kNewLineChar, cursorIndex - 2) + 1;
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
            int textLen = characterCount;

            if (cursorIndex < textLen)
                cursorIndex = IndexOfEndOfLine(cursorIndex);
            if (selectIndex != 0)
                selectIndex = textHandle.LastIndexOf(kNewLineChar, selectIndex - 1) + 1;
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
            cursorIndex = selectIndex = textHandle.LineUpCharacterPosition(cursorIndex);
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
            cursorIndex = selectIndex = textHandle.LineDownCharacterPosition(cursorIndex);
            if (cursorIndex == characterCount)
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
                if (m_TextElementInfos[i].character == kNewLineChar)
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
            int strlen = characterCount;
            while (i < strlen)
            {
                if (m_TextElementInfos[i].character == kNewLineChar)
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
            selectIndex = cursorIndex = characterCount;
        }

        /// Move to the next paragraph
        public void MoveParagraphForward()
        {
            cursorIndex = cursorIndex > selectIndex ? cursorIndex : selectIndex;
            if (cursorIndex < characterCount)
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
                selectIndex = cursorIndex = textHandle.LastIndexOf(kNewLineChar, cursorIndex - 2) + 1;
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
            selectIndex = textHandle.GetCursorIndexFromPosition(cursorPosition);

            if (!shift)
            {
                cursorIndex = selectIndex;
            }
        }

        protected internal void MoveAltCursorToPosition(Vector2 cursorPosition)
        {
            int index = textHandle.GetCursorIndexFromPosition(cursorPosition);
            iAltCursorPos = Mathf.Min(characterCount, index);
        }

        protected internal bool IsOverSelection(Vector2 cursorPosition)
        {
            int p = textHandle.GetCursorIndexFromPosition(cursorPosition);
            return ((p < Mathf.Max(cursorIndex, selectIndex)) && (p > Mathf.Min(cursorIndex, selectIndex)));
        }

        // Do a drag selection. Used to expand the selection in MouseDrag events.
        public void SelectToPosition(Vector2 cursorPosition)
        {
            if (!m_MouseDragSelectsWholeWords)
                cursorIndex = textHandle.GetCursorIndexFromPosition(cursorPosition);
            else // snap to words/paragraphs
            {
                int p = textHandle.GetCursorIndexFromPosition(cursorPosition);

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
                            cursorIndex = textHandle.LastIndexOf(kNewLineChar, Mathf.Max(0, p - 2)) + 1;
                        else
                            cursorIndex = 0;

                        selectIndex = textHandle.LastIndexOf(kNewLineChar, Mathf.Min(characterCount - 1, m_DblClickInitPos));
                    }
                    else
                    {
                        if (p < characterCount)
                        {
                            cursorIndex = IndexOfEndOfLine(p);
                        }
                        else
                            cursorIndex = characterCount;

                        selectIndex = textHandle.LastIndexOf(kNewLineChar, Mathf.Max(0, m_DblClickInitPos - 2)) + 1;
                    }
                }
            }
        }

        int FindNextSeperator(int startPos)
        {
            int textLen = characterCount;
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
            int textLen = characterCount;
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
                if (m_TextElementInfos[p].character == '\t' || m_TextElementInfos[p].character == kNewLineChar)
                    return NextCodePointIndex(p);
            }

            if (p == textLen)
                return p;

            // Skip spaces
            if (m_TextElementInfos[p].character == ' ') // If we're at a space, skip over any number of spaces
            {
                while (p < textLen && ClassifyChar(p) == CharacterType.WhiteSpace)
                    p = NextCodePointIndex(p);
            }
            else if (m_TextElementInfos[p].character == '\t' || m_TextElementInfos[p].character == kNewLineChar) // If we're at a tab or a newline, just step one char ahead
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
            while (p > 0 && m_TextElementInfos[p].character == ' ')
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
            if (characterCount == 0)
                return 0;

            if (p >= characterCount)
                p = characterCount - 1;

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
                        if (p >= characterCount)
                            return characterCount;
                        break;
                }
            }
            while (ClassifyChar(p) == t);
            if (dir == Direction.Forward)
                return p;
            return NextCodePointIndex(p);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal Action OnCursorIndexChange;
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal Action OnSelectIndexChange;
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal Action OnRevealCursorChange;

        int ClampTextIndex(int index)
        {
            return Mathf.Clamp(index, 0, characterCount);
        }

        int IndexOfEndOfLine(int startIndex)
        {
            int index = textHandle.IndexOf(kNewLineChar, startIndex);
            return (index != -1 ? index : characterCount);
        }

        public int PreviousCodePointIndex(int index)
        {
            if (index > 0)
                index--;

            return index;
        }

        public int NextCodePointIndex(int index)
        {
            if (index < characterCount)
                index++;

            return index;
        }

        int GetGraphicalLineStart(int p)
        {
            Vector2 point = textHandle.GetCursorPositionFromStringIndexUsingLineHeight(p);
            point.y -= 1.0f / GUIUtility.pixelsPerPoint; // we make sure no floating point errors can make us land on another line
            point.x = 0;
            return textHandle.GetCursorIndexFromPosition(point);
        }

        int GetGraphicalLineEnd(int p)
        {
            Vector2 point = textHandle.GetCursorPositionFromStringIndexUsingLineHeight(p);
            point.y -= 1.0f / GUIUtility.pixelsPerPoint; // we make sure no floating point errors can make us land on another line
            point.x += 5000;
            return textHandle.GetCursorIndexFromPosition(point);
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
            char c = (char)m_TextElementInfos[index].character;
            if (c == kNewLineChar)
                return CharacterType.NewLine;
            if (char.IsWhiteSpace(c))
                return CharacterType.WhiteSpace;
            if (char.IsLetterOrDigit(c) || m_TextElementInfos[index].character == '\'')
                return CharacterType.LetterLike;
            return CharacterType.Symbol;
        }
    }
}
