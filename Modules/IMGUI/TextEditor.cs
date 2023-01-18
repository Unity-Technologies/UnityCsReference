// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    public class TextEditor
    {
        private readonly GUIContent m_Content = new GUIContent();
        private TextSelectingUtilities m_TextSelecting;

        //Used in automated tests
        internal TextEditingUtilities m_TextEditing;

        //Used in automated tests
        internal IMGUITextHandle m_TextHandle;

        public TouchScreenKeyboard keyboardOnScreen = null;
        public int controlID = 0;

        public GUIStyle style;

        [Obsolete("'multiline' has been deprecated. Changes to this member will not be observed. Use 'isMultiline' instead.", true)]
        public bool multiline;
        public bool isMultiline
        { get => m_TextEditing.multiline; set => m_TextEditing.multiline = value; }

        [Obsolete("'hasHorizontalCursorPos' has been deprecated. Changes to this member will not be observed. Use 'hasHorizontalCursor' instead.", true)]
        public bool hasHorizontalCursorPos = false;
        public bool hasHorizontalCursor
        {
            get => m_TextSelecting.hasHorizontalCursorPos;
            set => m_TextSelecting.hasHorizontalCursorPos = value;
        }

        public bool isPasswordField = false;

        // The text field can have a scroll offset in order to display its contents
        public Vector2 scrollOffset;

        [Obsolete("'revealCursor' has been deprecated. Changes to this member will not be observed. Use 'showCursor' instead.", true)]
        public bool revealCursor;

        public bool showCursor
        {
            get => m_TextSelecting.revealCursor;
            set => m_TextSelecting.revealCursor = value;
        }

        private bool focus;
        internal bool m_HasFocus { get { return focus; } set { focus = value; } }

        [Obsolete("Please use 'text' instead of 'content'", true)]
        public GUIContent content
        {
            get => throw new NotImplementedException("Please use 'text' instead of 'content'");
            set => throw new NotImplementedException("Please use 'text' instead of 'content'");
        }

        public string text
        {
            get => m_TextEditing.text;
            set
            {
                if (m_TextEditing.text == value)
                    return;
                m_TextEditing.SetTextWithoutNotify(value);
                m_Content.SetTextWithoutNotify(value);
                textWithWhitespace = value;
                // TODO: Change this call to only do the parsing of the text, to update the characterCount properly.
                UpdateTextHandle();
            }
        }

        string m_TextWithWhitespace;
        internal string textWithWhitespace
        {
            get => string.IsNullOrEmpty(m_TextWithWhitespace) ? GUIContent.k_ZeroWidthSpace : m_TextWithWhitespace;
            set =>
                //The NoWidthSpace unicode is added at the end of the string to make sure LineFeeds update the layout of the text.
                m_TextWithWhitespace = value + GUIContent.k_ZeroWidthSpace;
        }

        public Rect position { get; set; }

        internal virtual Rect localPosition
        {
            get => style.padding.Remove(position);
        }

        public int cursorIndex
        {
            get => m_TextSelecting.cursorIndex;
            set => m_TextSelecting.cursorIndex = value;
        }

        internal int stringCursorIndex
        {
            get => m_TextEditing.stringCursorIndex;
            set => m_TextEditing.stringCursorIndex = value;
        }

        public int selectIndex
        {
            get => m_TextSelecting.selectIndex;
            set => m_TextSelecting.selectIndex = value;
        }

        internal int stringSelectIndex
        {
            get => m_TextEditing.stringSelectIndex;
            set => m_TextEditing.stringSelectIndex = value;
        }

        // are we up/downing?
        public Vector2 graphicalCursorPos;

        public Vector2 graphicalSelectCursorPos;

        public DblClickSnapping doubleClickSnapping
        {
            get => m_TextSelecting.dblClickSnap;
            set => m_TextSelecting.dblClickSnap = value;
        }

        public int altCursorPosition
        {
            get => m_TextSelecting.iAltCursorPos;
            set => m_TextSelecting.iAltCursorPos = value;
        }

        public enum DblClickSnapping : byte { WORDS, PARAGRAPHS }

        [RequiredByNativeCode]
        public TextEditor()
        {
            var style = GUIStyle.none;
            m_TextHandle = IMGUITextHandle.GetTextHandle(style, position, textWithWhitespace, Color.white, true);
            m_TextSelecting = new TextSelectingUtilities(m_TextHandle);
            m_TextEditing = new TextEditingUtilities(m_TextSelecting, m_TextHandle, m_Content.text);
            m_Content.OnTextChanged += OnContentTextChangedHandle;
            m_TextEditing.OnTextChanged += OnTextChangedHandle;
            this.style = style;

            m_TextSelecting.OnCursorIndexChange += OnCursorIndexChange;
            m_TextSelecting.OnSelectIndexChange += OnSelectIndexChange;
        }

        private void OnTextChangedHandle()
        {
            m_Content.SetTextWithoutNotify(text);
            textWithWhitespace = text;
        }

        private void OnContentTextChangedHandle()
        {
            text = m_Content.text;
            textWithWhitespace = text;
        }

        public void OnFocus()
        {
            m_HasFocus = true;
            m_TextSelecting.OnFocus();
        }

        public void OnLostFocus()
        {
            m_HasFocus = false;
        }

        public bool HasClickedOnLink(Vector2 mousePosition, out string linkData)
        {
            linkData = "";
            var intersectingLink = m_TextHandle.FindIntersectingLink(mousePosition - new Vector2(position.x, position.y));
            if (intersectingLink < 0)
                return false;

            var link = m_TextHandle.textInfo.linkInfo[intersectingLink];
            if (link.linkId != null && link.linkIdLength > 0)
            {
                linkData = new string(link.linkId);
                return true;
            }
            return false;
        }

        public bool HasClickedOnHREF(Vector2 mousePosition, out string href)
        {
            href = "";
            var intersectingLink = m_TextHandle.FindIntersectingLink(mousePosition - new Vector2(position.x, position.y));
            if (intersectingLink < 0)
                return false;

            var link = m_TextHandle.textInfo.linkInfo[intersectingLink];
            if (link.hashCode == (int)MarkupTag.HREF)
            {
                if (link.linkId != null && link.linkIdLength > 0)
                {
                    href = new string(link.linkId);
                    if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Handle a key event.
        // Looks up the platform-dependent key-action table & performs the event
        // return true if the event was recognized.
        public bool HandleKeyEvent(Event e)
        {
            return m_TextEditing.HandleKeyEvent(e) || m_TextSelecting.HandleKeyEvent(e);
        }

        // Deletes previous text on the line
        public bool DeleteLineBack() => m_TextEditing.DeleteLineBack();

        // Deletes the previous word
        public bool DeleteWordBack() => m_TextEditing.DeleteWordBack();

        // Deletes the following word
        public bool DeleteWordForward() => m_TextEditing.DeleteWordForward();

        // perform a right-delete
        public bool Delete() => m_TextEditing.Delete();

        public bool CanPaste() => m_TextEditing.CanPaste();

        // Perform a left-delete
        public bool Backspace() => m_TextEditing.Backspace();

        /// Select all the text
        public void SelectAll() => m_TextSelecting.SelectAll();

        /// Select none of the text
        public void SelectNone() => m_TextSelecting.SelectNone();


        /// Does this text field has a selection
        public bool hasSelection => m_TextSelecting.hasSelection;

        /// Returns the selected text
        public string SelectedText => m_TextSelecting.selectedText;

        /// Delete the current selection. If there is no selection, this function does not do anything...
        public bool DeleteSelection() => m_TextEditing.DeleteSelection();

        /// Replace the selection with /replace/. If there is no selection, /replace/ is inserted at the current cursor point.
        public void ReplaceSelection(string replace) => m_TextEditing.ReplaceSelection(replace);

        /// Replaced the selection with /c/
        public void Insert(char c)
        {
            m_TextEditing.Insert(c);
            UpdateTextHandle();
        }

        /// Move selection to alt cursor /position/
        public void MoveSelectionToAltCursor() => m_TextEditing.MoveSelectionToAltCursor();

        /// Move the cursor one character to the right and deselect.
        public void MoveRight() => m_TextSelecting.MoveRight();

        /// Move the cursor one character to the left and deselect.
        public void MoveLeft() => m_TextSelecting.MoveLeft();

        /// Move the cursor up and deselects.
        public void MoveUp() => m_TextSelecting.MoveUp();

        /// Move the cursor down and deselects.
        public void MoveDown() => m_TextSelecting.MoveDown();

        /// Moves the cursor to the start of the current line.
        public void MoveLineStart() => m_TextSelecting.MoveLineStart();

        /// Moves the selection to the end of the current line
        public void MoveLineEnd() => m_TextSelecting.MoveLineEnd();

        /// Move to the start of the current graphical line. This takes word-wrapping into consideration.
        public void MoveGraphicalLineStart() => m_TextSelecting.MoveGraphicalLineStart();

        /// Move to the end of the current graphical line. This takes word-wrapping into consideration.
        public void MoveGraphicalLineEnd() => m_TextSelecting.MoveGraphicalLineEnd();

        /// Moves the cursor to the beginning of the text
        public void MoveTextStart() => m_TextSelecting.MoveTextStart();

        /// Moves the cursor to the end of the text
        public void MoveTextEnd() => m_TextSelecting.MoveTextEnd();

        /// Move to the next paragraph
        public void MoveParagraphForward() => m_TextSelecting.MoveParagraphForward();

        /// Move to the previous paragraph
        public void MoveParagraphBackward() => m_TextSelecting.MoveParagraphBackward();

        // Move the cursor to a graphical position. Used for moving the cursor on MouseDown events.
        public void MoveCursorToPosition(Vector2 cursorPosition)
        {
            MoveCursorToPosition_Internal(cursorPosition, Event.current.shift);
        }

        // Move the cursor to a graphical position. Used for moving the cursor on MouseDown events.
        protected internal void MoveCursorToPosition_Internal(Vector2 cursorPosition, bool shift) =>  m_TextSelecting.MoveCursorToPosition_Internal(GetLocalCursorPosition(cursorPosition), shift);

        public void MoveAltCursorToPosition(Vector2 cursorPosition) => m_TextSelecting.MoveAltCursorToPosition(GetLocalCursorPosition(cursorPosition));

        public bool IsOverSelection(Vector2 cursorPosition)=> m_TextSelecting.IsOverSelection(GetLocalCursorPosition(cursorPosition));

        // Do a drag selection. Used to expand the selection in MouseDrag events.
        public void SelectToPosition(Vector2 cursorPosition) => m_TextSelecting.SelectToPosition(GetLocalCursorPosition(cursorPosition));

        private Vector2 GetLocalCursorPosition(Vector2 cursorPosition)
        {
            return cursorPosition - style.Internal_GetTextRectOffset(position, m_Content, new Vector2(m_TextHandle.preferredSize.x, m_TextHandle.preferredSize.y > 0 ? m_TextHandle.preferredSize.y : style.lineHeight)) + scrollOffset;
        }

        /// Expand the selection to the left
        public void SelectLeft() => m_TextSelecting.SelectLeft();

        public void SelectRight() => m_TextSelecting.SelectRight();

        public void SelectUp() => m_TextSelecting.SelectUp();

        public void SelectDown() => m_TextSelecting.SelectDown();

        /// Select to the end of the text
        public void SelectTextEnd() => m_TextSelecting.SelectTextEnd();

        /// Select to the start of the text
        public void SelectTextStart() => m_TextSelecting.SelectTextStart();

        /// sets whether the text selection is done by dbl click or not
        public void MouseDragSelectsWholeWords(bool on) => m_TextSelecting.MouseDragSelectsWholeWords(on);

        public void DblClickSnap(DblClickSnapping snapping) => m_TextSelecting.DblClickSnap(snapping);

        /// Move to the end of the word.
        /// If the cursor is over some space characters, these are skipped
        /// Then, the cursor moves to the end of the following word.
        /// This corresponds to Alt-RightArrow on a Mac
        public void MoveWordRight() => m_TextSelecting.MoveWordRight();

        public void MoveToStartOfNextWord() => m_TextSelecting.MoveToStartOfNextWord();

        public void MoveToEndOfPreviousWord() => m_TextSelecting.MoveToEndOfPreviousWord();

        public void SelectToStartOfNextWord() => m_TextSelecting.SelectToStartOfNextWord();

        public void SelectToEndOfPreviousWord() => m_TextSelecting.SelectToEndOfPreviousWord();

        /// Move to start of next word.
        /// This corresponds to Ctrl-RightArrow on Windows
        /// If the cursor is over a whitespace, it's moved forwards ''till the first non-whitespace character
        /// If the cursor is over an alphanumeric character, it''s moved forward 'till it encounters space or a punctuation mark.
        /// If the stopping character is a space, this is skipped as well.
        /// If the cursor is over an punctuation mark, it's moved forward ''till it a letter or a space of a punctuation mark. If the stopping character is a space, this is skipped as well
        public int FindStartOfNextWord(int p) => m_TextSelecting.FindStartOfNextWord(p);

        public void MoveWordLeft() => m_TextSelecting.MoveWordLeft();

        public void SelectWordRight() => m_TextSelecting.SelectWordRight();

        public void SelectWordLeft() => m_TextSelecting.SelectWordLeft();

        /// Expand the selection to the start of the line
        /// Used on a mac for CMD-SHIFT-LEFT
        public void ExpandSelectGraphicalLineStart() => m_TextSelecting.ExpandSelectGraphicalLineStart();

        /// Expand the selection to the end of the line
        /// Used on a mac for CMD-SHIFT-RIGHT
        public void ExpandSelectGraphicalLineEnd() => m_TextSelecting.ExpandSelectGraphicalLineEnd();

        /// Move the selection point to the start of the line
        /// Used on a Windows for SHIFT-Home
        public void SelectGraphicalLineStart() => m_TextSelecting.SelectGraphicalLineStart();

        /// Expand the selection to the end of the line
        /// Used on a mac for SHIFT-End
        public void SelectGraphicalLineEnd() => m_TextSelecting.SelectGraphicalLineEnd();

        public void SelectParagraphForward() => m_TextSelecting.SelectParagraphForward();

        public void SelectParagraphBackward() => m_TextSelecting.SelectParagraphBackward();

        /// Select the word under the cursor
        public void SelectCurrentWord() => m_TextSelecting.SelectCurrentWord();

        // Select the entire paragraph the cursor is on (separated by \n)
        public void SelectCurrentParagraph() => m_TextSelecting.SelectCurrentParagraph();

        public void UpdateScrollOffsetIfNeeded(Event evt)
        {
            if (evt.type != EventType.Repaint && evt.type != EventType.Layout)
            {
                UpdateScrollOffset();
            }
        }

        internal void UpdateTextHandle()
        {
            m_TextHandle = IMGUITextHandle.GetTextHandle(style, style.padding.Remove(position), textWithWhitespace, Color.white, true);
            m_TextEditing.textHandle = m_TextHandle;
            m_TextSelecting.textHandle = m_TextHandle;
        }

        Vector2 lastCursorPos = Vector2.zero;
        Vector2 previousContentSize = Vector2.zero;
        [VisibleToOtherModules]
        internal void UpdateScrollOffset()
        {
            const int kCursorWidth = 1;
            const float epsilon = 0.05f;

            var newXOffset = scrollOffset.x;
            var newYOffset = scrollOffset.y;

            graphicalCursorPos = style.GetCursorPixelPosition(new Rect(0, 0, position.width, position.height), m_Content, m_TextSelecting.cursorIndexNoValidation);

            // The rectangle inside which the text is displayed.
            Rect viewRect = style.padding.Remove(position);

            // Position of the cursor in the viewRect coordinate system.
            var localGraphicalCursorPos = graphicalCursorPos;
            localGraphicalCursorPos.x -= style.padding.left;
            localGraphicalCursorPos.y -= style.padding.top;

            // The size of the text, without any padding.
            Vector2 contentSize = previousContentSize = style.GetPreferredSize(m_Content.textWithWhitespace, position);

            // If there is plenty of room, simply show entire string
            if (contentSize.x < viewRect.width)
            {
                newXOffset = 0;
            }
            else if (showCursor)
            {
                //go right
                if (localGraphicalCursorPos.x > scrollOffset.x + viewRect.width - kCursorWidth)
                    newXOffset = localGraphicalCursorPos.x - viewRect.width + kCursorWidth;
                //go left
                else if (localGraphicalCursorPos.x < scrollOffset.x)
                    newXOffset = Mathf.Max(localGraphicalCursorPos.x, 0);
                //go left - applies when deleting from the string
                else if (previousContentSize.x != contentSize.x && localGraphicalCursorPos.x < viewRect.x + Math.Abs(contentSize.x + kCursorWidth - viewRect.width))
                    newXOffset = Mathf.Max(viewRect.width - localGraphicalCursorPos.x, 0);
            }

            // ... and height/y as well
            // If there is plenty of room, simply show entire string
            if (Mathf.Round(contentSize.y) <= Mathf.Round(viewRect.height) || viewRect.height == 0)
                newYOffset = 0;
            else if (showCursor && Math.Abs(lastCursorPos.y - localGraphicalCursorPos.y) > epsilon)
            {
                //go down
                if (localGraphicalCursorPos.y + style.lineHeight > scrollOffset.y + viewRect.height)
                    newYOffset = localGraphicalCursorPos.y - viewRect.height + style.lineHeight;
                //go up
                else if (localGraphicalCursorPos.y < style.lineHeight + scrollOffset.y)
                    newYOffset = localGraphicalCursorPos.y - style.lineHeight;
            }

            if (scrollOffset.x != newXOffset || scrollOffset.y != newYOffset)
                scrollOffset = new Vector2(newXOffset, newYOffset < 0 ? 0 : newYOffset);

            showCursor = false;
            lastCursorPos = localGraphicalCursorPos;
        }

        // TODO: get the height from the font
        public void DrawCursor(string newText)
        {
            string realText = text;
            int cursorPos = cursorIndex;
            if (GUIUtility.compositionString.Length > 0)
            {
                m_Content.text = newText.Substring(0, cursorIndex) + GUIUtility.compositionString + newText.Substring(selectIndex);
                cursorPos += GUIUtility.compositionString.Length;
            }
            else
                m_Content.text = newText;

            graphicalCursorPos = style.GetCursorPixelPosition(position, m_Content, cursorPos);

            Vector2 originalContentOffset = style.contentOffset;
            style.contentOffset -= scrollOffset;
            style.Internal_clipOffset = scrollOffset;

            GUIUtility.compositionCursorPos = GUIClip.UnclipToWindow(graphicalCursorPos + new Vector2(position.x, position.y + style.lineHeight) - scrollOffset);

            if (GUIUtility.compositionString.Length > 0)
                style.DrawWithTextSelection(position, m_Content, controlID, cursorIndex, cursorIndex + GUIUtility.compositionString.Length, true);
            else
                style.DrawWithTextSelection(position, m_Content, controlID, cursorIndex, selectIndex);

            if (m_TextSelecting.iAltCursorPos != -1)
                style.DrawCursor(position, m_Content, controlID, m_TextSelecting.iAltCursorPos);

            // reset
            style.contentOffset = originalContentOffset;
            style.Internal_clipOffset = Vector2.zero;

            m_Content.text = realText;
        }

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
            if (isPasswordField)
                return false;

            return m_TextEditing.Cut();
        }

        public void Copy()
        {
            if (isPasswordField)
                return;

            m_TextSelecting.Copy();
        }

        internal Rect[] GetHyperlinksRect()
        {
            return style.GetHyperlinkRects(m_TextHandle, localPosition);
        }

        public bool Paste()
        {
            return m_TextEditing.Paste();
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

        internal virtual void OnCursorIndexChange()
        {
            UpdateScrollOffset();
        }

        internal virtual void OnSelectIndexChange()
        {
            UpdateScrollOffset();
        }
    }
} // namespace
