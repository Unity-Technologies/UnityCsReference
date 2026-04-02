// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;
using static UnityEngine.TextEditingUtilities;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal class TextSelectingUtilities
    {
        public enum DblClickSnapping : byte { WORDS, PARAGRAPHS }
        DblClickSnapping m_DblClickSnap = DblClickSnapping.WORDS;
        int m_IAltCursorPos = -1;
        bool m_HasHorizontalCursorPos = false;

        private bool m_bJustSelected = false;
        private bool m_MouseDragSelectsWholeWords = false;
        private int m_DblClickInitPosStart = 0;
        private int m_DblClickInitPosEnd = 0;

        public DblClickSnapping dblClickSnap
        {
            get
            {
                if (useAdvancedText)
                    return (DblClickSnapping)TextSelectionService.GetDblClickSnap(tgi);
                return m_DblClickSnap;
            }
            set
            {
                if (useAdvancedText)
                    TextSelectionService.SetDblClickSnap(tgi, (int)value);
                m_DblClickSnap = value;
            }
        }

        public int iAltCursorPos
        {
            get
            {
                if (useAdvancedText)
                    return TextSelectionService.GetIAltCursorPos(tgi);
                return m_IAltCursorPos;
            }
            set
            {
                if (useAdvancedText)
                    TextSelectionService.SetIAltCursorPos(tgi, value);
                m_IAltCursorPos = value;
            }
        }

        public bool hasHorizontalCursorPos
        {
            get
            {
                if (useAdvancedText)
                    return TextSelectionService.GetHasHorizontalCursorPos(tgi);
                return m_HasHorizontalCursorPos;
            }
            set
            {
                if (useAdvancedText)
                    TextSelectionService.SetHasHorizontalCursorPos(tgi, value);
                m_HasHorizontalCursorPos = value;
            }
        }
        public TextHandle textHandle;
        private const int kMoveDownHeight = 5;
        private const char kNewLineChar = '\n';

        bool useAdvancedText => textHandle.useAdvancedText;
        IntPtr tgi => textHandle.textGenerationInfo;

        /// Does this text field has a selection
        public bool hasSelection => cursorIndex != selectIndex;

        bool m_RevealCursor;
        public bool revealCursor
        {
            get
            {
                if (useAdvancedText)
                    return TextSelectionService.GetRevealCursor(tgi);
                return m_RevealCursor;
            }
            set
            {
                if (useAdvancedText)
                {
                    if (TextSelectionService.SetRevealCursor(tgi, value))
                    {
                        m_RevealCursor = value;
                        OnRevealCursorChange?.Invoke();
                    }  
                    return;
                }
                if (m_RevealCursor != value)
                {
                    m_RevealCursor = value;
                    OnRevealCursorChange?.Invoke();
                }
            }
        }

        int m_CharacterCount => textHandle.characterCount;
        // For TextCore we need to substract 1 if the last character is a zero width space
        int characterCount => (!useAdvancedText && m_CharacterCount > 0 && textHandle.textInfo.textElementInfo[m_CharacterCount - 1].character == 0x200B) ? m_CharacterCount - 1 : m_CharacterCount;
        TextElementInfo[] m_TextElementInfos => textHandle.textInfo.textElementInfo;

        int m_CursorIndex = 0;
        public int cursorIndex
        {
            get
            {
                if (textHandle.IsPlaceholder) return 0;
                if (useAdvancedText)
                    return TextSelectionService.GetCursorIndex(tgi);
                return ClampTextIndex(m_CursorIndex);
            }
            set
            {
                if (useAdvancedText)
                {
                    if (TextSelectionService.SetCursorIndex(tgi, value))
                    {
                        m_CursorIndex = value;
                        OnCursorIndexChange?.Invoke();
                    }
                        
                    return;
                }
                if (m_CursorIndex != value)
                {
                    m_CursorIndex = value;
                    OnCursorIndexChange?.Invoke();
                }
            }
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal int cursorIndexNoValidation
        {
            get
            {
                if (useAdvancedText)
                    return TextSelectionService.GetCursorIndexNoValidation(tgi);
                return m_CursorIndex;
            }
            set
            {
                if (useAdvancedText)
                {
                    if (TextSelectionService.SetCursorIndex(tgi, value))
                        OnCursorIndexChange?.Invoke();
                    return;
                }
                if (m_CursorIndex != value)
                {
                    SetCursorIndexWithoutNotify(value);
                    OnCursorIndexChange?.Invoke();
                }
            }
        }

        internal void SetCursorIndexWithoutNotify(int index)
        {
            if (useAdvancedText)
            {

                TextSelectionService.SetCursorIndex(tgi, index);
                m_CursorIndex = index;
            }
            else
                m_CursorIndex = index;
        }

        internal int m_SelectIndex = 0;
        public int selectIndex
        {
            get
            {
                if (textHandle.IsPlaceholder) return 0;
                if (useAdvancedText)
                    return TextSelectionService.GetSelectIndex(tgi);
                return ClampTextIndex(m_SelectIndex);
            }
            set
            {
                if (useAdvancedText)
                {
                    if (TextSelectionService.SetSelectIndex(tgi, value))
                    {
                        m_SelectIndex = value;
                        OnSelectIndexChange?.Invoke();
                    }
                        
                    return;
                }
                if (m_SelectIndex != value)
                {
                    SetSelectIndexWithoutNotify(value);
                    OnSelectIndexChange?.Invoke();
                }
            }
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal int selectIndexNoValidation
        {
            get
            {
                if (useAdvancedText)
                    return TextSelectionService.GetSelectIndexNoValidation(tgi);
                return m_SelectIndex;
            }
            set
            {
                if (useAdvancedText)
                {
                    if (TextSelectionService.SetSelectIndex(tgi, value))
                        OnSelectIndexChange?.Invoke();
                    return;
                }
                if (m_SelectIndex != value)
                {
                    SetSelectIndexWithoutNotify(value);
                    OnSelectIndexChange?.Invoke();
                }
            }
        }

        internal void SetSelectIndexWithoutNotify(int index)
        {
            if (useAdvancedText)
            {
                TextSelectionService.SetSelectIndex(tgi, index);
                m_SelectIndex = index;
            }
            else
                m_SelectIndex = index;
        }

        /// Returns the selected text
        public string selectedText
        {
            get
            {
                if (useAdvancedText)
                    return TextSelectionService.GetSelectedText(tgi);

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

        public bool HandleKeyEvent(KeyCode key, EventModifiers modifiers)
        {
            var op = TextSelectOpFromEnum(key, modifiers, (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX));
            if (op.HasValue)
            {
                PerformOperation(op.Value);
                return true;
            }
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

        //Used for tests
        internal static readonly List<(KeyEvent keyEvent, TextSelectOp operation)> s_GlobalKeyMappings = new()
        {
            (new KeyEvent(KeyCode.LeftArrow, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectLeft),
            (new KeyEvent(KeyCode.RightArrow, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectRight),
            (new KeyEvent(KeyCode.UpArrow, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectUp),
            (new KeyEvent(KeyCode.DownArrow, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectDown),
        };

        //Used for tests
        internal static readonly List<(KeyEvent keyEvent, TextSelectOp operation)> s_MacKeyMappings = new()
        {
            (new (KeyCode.Home, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectTextStart),
            (new (KeyCode.End, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectTextEnd),

            (new (KeyCode.LeftArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.ExpandSelectGraphicalLineStart),
            (new (KeyCode.RightArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.ExpandSelectGraphicalLineEnd),
            (new (KeyCode.UpArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.SelectParagraphBackward),
            (new (KeyCode.DownArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.SelectParagraphForward),

            (new (KeyCode.LeftArrow, EventModifiers.Shift | EventModifiers.Alt | EventModifiers.FunctionKey), TextSelectOp.SelectWordLeft),
            (new (KeyCode.RightArrow, EventModifiers.Shift | EventModifiers.Alt | EventModifiers.FunctionKey), TextSelectOp.SelectWordRight),
            (new (KeyCode.UpArrow, EventModifiers.Shift | EventModifiers.Alt | EventModifiers.FunctionKey), TextSelectOp.SelectParagraphBackward),
            (new (KeyCode.DownArrow, EventModifiers.Shift | EventModifiers.Alt | EventModifiers.FunctionKey), TextSelectOp.SelectParagraphForward),

            (new (KeyCode.LeftArrow, EventModifiers.Command | EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.ExpandSelectGraphicalLineStart),
            (new (KeyCode.RightArrow, EventModifiers.Command | EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.ExpandSelectGraphicalLineEnd),
            (new (KeyCode.UpArrow, EventModifiers.Command | EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectTextStart),
            (new (KeyCode.DownArrow, EventModifiers.Command | EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectTextEnd),

            (new (KeyCode.A, EventModifiers.Command), TextSelectOp.SelectAll),
            (new (KeyCode.C, EventModifiers.Command), TextSelectOp.Copy),
        };

        //Used for tests
        internal static readonly List<(KeyEvent keyEvent, TextSelectOp operation)> s_WindowsLinuxKeyMappings = new()
        {
            (new(KeyCode.LeftArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.SelectToEndOfPreviousWord),
            (new(KeyCode.RightArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.SelectToStartOfNextWord),
            (new(KeyCode.UpArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.SelectParagraphBackward),
            (new(KeyCode.DownArrow, EventModifiers.Shift | EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.SelectParagraphForward),

            (new(KeyCode.Home, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectGraphicalLineStart),
            (new(KeyCode.End, EventModifiers.Shift | EventModifiers.FunctionKey), TextSelectOp.SelectGraphicalLineEnd),

            (new(KeyCode.A, EventModifiers.Control), TextSelectOp.SelectAll),
            (new(KeyCode.C, EventModifiers.Control), TextSelectOp.Copy),
            (new(KeyCode.Insert, EventModifiers.Control | EventModifiers.FunctionKey), TextSelectOp.Copy),
        };

        internal static TextSelectOp? TextSelectOpFromEnum(KeyCode key, EventModifiers modifiers, bool IsMacOsFamily)
        {
            //Capslock is always ignored for actions
            modifiers &= ~EventModifiers.CapsLock;

            var keyEvent = new KeyEvent(key, modifiers);
            foreach (var mapping in s_GlobalKeyMappings)
            {
                if (mapping.keyEvent == keyEvent)
                    return mapping.operation;
            }

            foreach (var mapping in IsMacOsFamily ? s_MacKeyMappings : s_WindowsLinuxKeyMappings)
            {
                if (mapping.keyEvent == keyEvent)
                    return mapping.operation;
            }

            return null;
        }

        internal void NotifyFromFlags(int flags)
        {
            if ((flags & (int)EditingEventFlags.CursorIndexChanged) != 0)
            {
                OnCursorIndexChange?.Invoke();
                m_CursorIndex = cursorIndex;
            }
            if ((flags & (int)EditingEventFlags.SelectIndexChanged) != 0)
            {
                OnSelectIndexChange?.Invoke();
                m_SelectIndex = selectIndex;
            }
            if ((flags & (int)EditingEventFlags.RevealCursorChanged) != 0)
            {
                OnRevealCursorChange?.Invoke();
                m_RevealCursor = revealCursor;
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void SyncStateToNative()
        {
            if (tgi != IntPtr.Zero)
            {
                TextSelectionService.SetCursorIndex(tgi, m_CursorIndex);
                TextSelectionService.SetSelectIndex(tgi, m_SelectIndex);
                TextSelectionService.SetRevealCursor(tgi, m_RevealCursor);
            }
        }

        public void ClearCursorPos()
        {
            if (useAdvancedText)
            {
                TextSelectionService.ClearCursorPos(tgi);
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectAll(tgi));
                return;
            }
            cursorIndex = 0; selectIndex = Int32.MaxValue;
            ClearCursorPos();
        }

        /// Select none of the text
        public void SelectNone()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectNone(tgi));
                return;
            }
            selectIndex = cursorIndex;
            ClearCursorPos();
        }

        /// Expand the selection to the left
        public void SelectLeft()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectLeft(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectRight(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectUp(tgi));
                return;
            }
            cursorIndex = textHandle.LineUpCharacterPosition(cursorIndex);
        }

        public void SelectDown()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectDown(tgi));
                return;
            }
            cursorIndex = textHandle.LineDownCharacterPosition(cursorIndex);
        }

        /// Select to the end of the text
        public void SelectTextEnd()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectTextEnd(tgi));
                return;
            }
            // This is not quite like the mac - there, when you select to end of text, the position of the cursor becomes somewhat i'll defined
            // Hard to explain. In textedit, try: CMD-SHIFT-down, SHIFT-LEFT for case 1. then do CMD-SHIFT-down, SHIFT-RIGHT, SHIFT-LEFT for case 2.
            // Anyways, it's wrong so we won't do that
            cursorIndex = characterCount;
        }

        /// Select to the start of the text
        public void SelectTextStart()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectTextStart(tgi));
                return;
            }
            // Same thing as SelectTextEnd...
            cursorIndex = 0;
        }

        public void SelectToStartOfNextWord()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectToStartOfNextWord(tgi));
                return;
            }
            ClearCursorPos();
            cursorIndex = FindStartOfNextWord(cursorIndex);
        }

        public void SelectToEndOfPreviousWord()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectToEndOfPreviousWord(tgi));
                return;
            }
            ClearCursorPos();
            cursorIndex = FindEndOfPreviousWord(cursorIndex);
        }

        public void SelectWordRight()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectWordRight(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectWordLeft(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectGraphicalLineStart(tgi));
                return;
            }
            ClearCursorPos();
            cursorIndex = GetGraphicalLineStart(cursorIndex);
        }

        /// Expand the selection to the end of the line
        /// Used on a mac for SHIFT-End
        public void SelectGraphicalLineEnd()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectGraphicalLineEnd(tgi));
                return;
            }
            ClearCursorPos();
            cursorIndex = GetGraphicalLineEnd(cursorIndex);
        }

        public void SelectParagraphForward()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectParagraphForward(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectParagraphBackward(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectCurrentWord(tgi));
                return;
            }

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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectCurrentParagraph(tgi));
                return;
            }

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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveRight(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveLeft(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveUp(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveDown(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveLineStart(tgi));
                return;
            }

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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveLineEnd(tgi));
                return;
            }

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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveGraphicalLineStart(tgi));
                return;
            }
            cursorIndex = selectIndex = GetGraphicalLineStart(cursorIndex < selectIndex ? cursorIndex : selectIndex);
        }

        /// Move to the end of the current graphical line. This takes word-wrapping into consideration.
        public void MoveGraphicalLineEnd()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveGraphicalLineEnd(tgi));
                return;
            }
            cursorIndex = selectIndex = GetGraphicalLineEnd(cursorIndex > selectIndex ? cursorIndex : selectIndex);
        }

        /// Moves the cursor to the beginning of the text
        public void MoveTextStart()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveTextStart(tgi));
                return;
            }
            selectIndex = cursorIndex = 0;
        }

        /// Moves the cursor to the end of the text
        public void MoveTextEnd()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveTextEnd(tgi));
                return;
            }
            selectIndex = cursorIndex = characterCount;
        }

        /// Move to the next paragraph
        public void MoveParagraphForward()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveParagraphForward(tgi));
                return;
            }

            cursorIndex = cursorIndex > selectIndex ? cursorIndex : selectIndex;
            if (cursorIndex < characterCount)
            {
                selectIndex = cursorIndex = IndexOfEndOfLine(cursorIndex + 1);
            }
        }

        /// Move to the previous paragraph
        public void MoveParagraphBackward()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveParagraphBackward(tgi));
                return;
            }

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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveWordRight(tgi));
                return;
            }
            cursorIndex = cursorIndex > selectIndex ? cursorIndex : selectIndex;
            cursorIndex = selectIndex = FindNextSeperator(cursorIndex);
            ClearCursorPos();
        }

        public void MoveToStartOfNextWord()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveToStartOfNextWord(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveToEndOfPreviousWord(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveWordLeft(tgi));
                return;
            }
            cursorIndex = cursorIndex < selectIndex ? cursorIndex : selectIndex;
            cursorIndex = FindPrevSeperator(cursorIndex);
            selectIndex = cursorIndex;
        }

        /// sets whether the text selection is done by dbl click or not
        public void MouseDragSelectsWholeWords(bool on)
        {
            if (useAdvancedText)
            {
                TextSelectionService.MouseDragSelectsWholeWords(tgi, on);
                return;
            }
            m_MouseDragSelectsWholeWords = on;
            m_DblClickInitPosStart = cursorIndex < selectIndex ? cursorIndex : selectIndex;
            m_DblClickInitPosEnd = cursorIndex < selectIndex ? selectIndex : cursorIndex;
        }

        /// Expand the selection to the start of the line
        /// Used on a mac for CMD-SHIFT-LEFT
        public void ExpandSelectGraphicalLineStart()
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.ExpandSelectGraphicalLineStart(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.ExpandSelectGraphicalLineEnd(tgi));
                return;
            }
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
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.MoveCursorToPosition(tgi, textHandle.PointsToPixels(cursorPosition), shift));
                return;
            }
            selectIndex = textHandle.GetCursorIndexFromPosition(cursorPosition);

            if (!shift)
            {
                cursorIndex = selectIndex;
            }
        }

        protected internal void MoveAltCursorToPosition(Vector2 cursorPosition)
        {
            if (useAdvancedText)
            {
                TextSelectionService.MoveAltCursorToPosition(tgi, textHandle.PointsToPixels(cursorPosition));
                return;
            }
            // This action is invalid if the entire text is selected
            if (cursorIndex == 0 && selectIndex == characterCount)
            {
                iAltCursorPos = -1;
                return;
            }
            int index = textHandle.GetCursorIndexFromPosition(cursorPosition);
            iAltCursorPos = Mathf.Min(characterCount, index);
        }

        protected internal bool IsOverSelection(Vector2 cursorPosition)
        {
            if (useAdvancedText)
                return TextSelectionService.IsOverSelection(tgi, textHandle.PointsToPixels(cursorPosition));

            int p = textHandle.GetCursorIndexFromPosition(cursorPosition);
            return ((p < Mathf.Max(cursorIndex, selectIndex)) && (p > Mathf.Min(cursorIndex, selectIndex)));
        }

        // Do a drag selection. Used to expand the selection in MouseDrag events.
        public void SelectToPosition(Vector2 cursorPosition)
        {
            if (useAdvancedText)
            {
                NotifyFromFlags(TextSelectionService.SelectToPosition(tgi, textHandle.PointsToPixels(cursorPosition)));
                return;
            }

            if (characterCount == 0)
                return;
            if (!m_MouseDragSelectsWholeWords)
                cursorIndex = textHandle.GetCursorIndexFromPosition(cursorPosition);
            else // snap to words/paragraphs
            {
                int p = textHandle.GetCursorIndexFromPosition(cursorPosition);

                if (dblClickSnap == DblClickSnapping.WORDS)
                {
                    if (p <= m_DblClickInitPosStart)
                    {
                        cursorIndex = FindEndOfClassification(p, Direction.Backward);
                        selectIndex = FindEndOfClassification(m_DblClickInitPosEnd - 1, Direction.Forward);
                    }
                    else if (p >= m_DblClickInitPosEnd)
                    {
                        cursorIndex = FindEndOfClassification(p - 1, Direction.Forward);
                        selectIndex = FindEndOfClassification(m_DblClickInitPosStart + 1, Direction.Backward);
                    }
                    else
                    {
                        cursorIndex = m_DblClickInitPosStart;
                        selectIndex = m_DblClickInitPosEnd;
                    }
                }
                else // paragraph
                {
                    if (p <= m_DblClickInitPosStart)
                    {
                        if (p > 0)
                            cursorIndex = textHandle.LastIndexOf(kNewLineChar, Mathf.Max(0, p - 1)) + 1;
                        else
                            cursorIndex = 0;

                        selectIndex = textHandle.LastIndexOf(kNewLineChar, Mathf.Min(characterCount - 1, m_DblClickInitPosEnd + 1));
                    }
                    else if (p >= m_DblClickInitPosEnd)
                    {
                        if (p < characterCount)
                        {
                            cursorIndex = IndexOfEndOfLine(p);
                        }
                        else
                            cursorIndex = characterCount;

                        selectIndex = textHandle.LastIndexOf(kNewLineChar, Mathf.Max(0, m_DblClickInitPosEnd - 2)) + 1;
                    }
                    else
                    {
                        cursorIndex = m_DblClickInitPosStart;
                        selectIndex = m_DblClickInitPosEnd;
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
            if (useAdvancedText)
                return TextSelectionService.GetStartOfNextWord(tgi, p);

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
            if (useAdvancedText)
                return TextSelectionService.GetEndOfPreviousWord(tgi, p);

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

        public Action OnCursorIndexChange;
        public Action OnSelectIndexChange;
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal Action OnRevealCursorChange;

        int ClampTextIndex(int index)
        {
            // When there is no cached Standard textInfo for this element (e.g., after
            // switching from Advanced to Standard text generator), characterCount comes
            // from a shared instance and is unreliable. Avoid clamping to a stale value.
            if (!useAdvancedText && textHandle.TextInfoNode == null)
                return Mathf.Max(0, index);
            return Mathf.Clamp(index, 0, characterCount);
        }

        int IndexOfEndOfLine(int startIndex)
        {
            int index = textHandle.IndexOf(kNewLineChar, startIndex);
            return (index != -1 ? index : characterCount);
        }

        public int PreviousCodePointIndex(int index)
        {
            if (useAdvancedText)
                return TextSelectionService.PreviousCodePointIndex(tgi, index);

            if (index > 0)
                index--;

            return index;
        }

        public int NextCodePointIndex(int index)
        {
            if (useAdvancedText)
                return TextSelectionService.NextCodePointIndex(tgi, index);

            if (index < characterCount)
                index++;

            return index;
        }

        int GetGraphicalLineStart(int p)
        {
            return textHandle.GetFirstCharacterIndexOnLine(p);
        }

        int GetGraphicalLineEnd(int p)
        {
            return textHandle.GetLastCharacterIndexOnLine(p);
        }

        public void Copy()
        {
            if (useAdvancedText)
            {
                string selected = TextSelectionService.GetSelectedText(tgi);
                if (selected.Length > 0)
                    StytemCopyBuffer.systemCopyBuffer = selected;
                return;
            }
            if (selectIndex == cursorIndex)
                return;

            StytemCopyBuffer.systemCopyBuffer = selectedText;
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
