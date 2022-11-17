// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface to access a TextElement selection and cursor information
    /// This interface is not meant to be implemented explicitly
    /// as its declaration might change in the future.
    /// </summary>
    public interface ITextSelection
    {
        /// <summary>
        /// Returns true if the field is selectable.
        /// </summary>
        public bool isSelectable { get; set; }

        /// <summary>
        /// Color of the cursor.
        /// </summary>
        public Color cursorColor { get; set; }

        /// <summary>
        /// Background color of selected text.
        /// </summary>
        public Color selectionColor { get; set; }

        /// <summary>
        /// This is the cursor index in the text presented.
        /// </summary>
        public int cursorIndex { get; set; }

        ///// <summary>
        ///// Controls whether double clicking selects the word under the mouse pointer or not.
        ///// </summary>
        public bool doubleClickSelectsWord { get; set; }

        /// <summary>
        /// This is the selection index in the text presented.
        /// </summary>
        public int selectIndex { get; set; }

        ///// <summary>
        ///// Controls whether triple clicking selects the entire line under the mouse pointer or not.
        ///// </summary>
        public bool tripleClickSelectsLine { get; set; }

        /// <summary>
        /// Return true is the TextElement has a selection.
        /// </summary>
        public bool HasSelection();

        /// <summary>
        /// Selects all the text contained in the field.
        /// </summary>
        public void SelectAll();

        /// <summary>
        /// Remove selection
        /// </summary>
        public void SelectNone();

        /// <summary>
        /// Select text between cursorIndex and selectIndex.
        /// </summary>
        public void SelectRange(int cursorIndex, int selectionIndex);

        // Internal members

        /// <summary>
        /// Controls whether the element's content is selected upon receiving focus.
        /// </summary>
        public bool selectAllOnFocus { get; set; }

        /// <summary>
        /// Controls whether the element's content is selected when you mouse up for the first time.
        /// </summary>
        public bool selectAllOnMouseUp { get; set; }

        /// <summary>
        /// The position of the text cursor inside the element.
        /// </summary>
        public Vector2 cursorPosition { get; }
        internal float lineHeightAtCursorPosition  { get; }
        internal float cursorWidth { get; set; }

        internal void MoveTextEnd();
    }

    // Text editing and selection management implementation
    public partial class TextElement : ITextSelection
    {
        /// <summary>
        /// Retrieves this TextElement's ITextSelection
        /// </summary>
        public ITextSelection selection => this;

        TextSelectingManipulator m_SelectingManipulator;

        bool m_IsSelectable;
        /// <summary>
        /// Controls whether the element's content is selectable.  Note that selectable TextElement are required to be focusable.
        /// </summary>
        bool ITextSelection.isSelectable
        {
            get => m_IsSelectable && focusable;
            set
            {
                focusable = value;
                m_IsSelectable = value;
            }
        }

        int ITextSelection.cursorIndex
        {
            get => selection.isSelectable ? selectingManipulator.cursorIndex : -1;
            set
            {
                if (selection.isSelectable)
                    selectingManipulator.cursorIndex = value;
            }
        }


        int ITextSelection.selectIndex
        {
            get => selection.isSelectable ? selectingManipulator.selectIndex : -1;
            set
            {
                if (selection.isSelectable)
                    selectingManipulator.selectIndex = value;
            }
        }

        void ITextSelection.SelectAll()
        {
            if (selection.isSelectable)
                selectingManipulator.m_SelectingUtilities.SelectAll();
        }

        void ITextSelection.SelectNone()
        {
            if (selection.isSelectable)
                selectingManipulator.m_SelectingUtilities.SelectNone();
        }

        void ITextSelection.SelectRange(int cursorIndex, int selectionIndex)
        {
            if (selection.isSelectable)
            {
                selectingManipulator.m_SelectingUtilities.cursorIndex = cursorIndex;
                selectingManipulator.m_SelectingUtilities.selectIndex = selectionIndex;
            }
        }

        bool ITextSelection.HasSelection()
        {
            return selection.isSelectable && selectingManipulator.HasSelection();
        }

        bool ITextSelection.doubleClickSelectsWord { get; set; } = true;

        bool ITextSelection.tripleClickSelectsLine { get; set; } = true;

        /// <summary>
        /// Controls whether the element's content is selected upon receiving focus.
        /// </summary>
        bool ITextSelection.selectAllOnFocus { get; set; } = false;

        /// <summary>
        /// Controls whether the element's content is selected when you mouse up for the first time.
        /// </summary>
        bool ITextSelection.selectAllOnMouseUp { get; set; } = false;

        Vector2 ITextSelection.cursorPosition => uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(selection.cursorIndex) + contentRect.min;

        float ITextSelection.lineHeightAtCursorPosition => uitkTextHandle.GetLineHeightFromCharacterIndex(selection.cursorIndex);

        void ITextSelection.MoveTextEnd()
        {
            if (selection.isSelectable)
                selectingManipulator.m_SelectingUtilities.MoveTextEnd();
        }

        Color ITextSelection.selectionColor { get; set; } = new Color(0.239f, 0.502f, 0.875f, 0.65f);


        Color ITextSelection.cursorColor { get; set; } = new Color(0.706f, 0.706f, 0.706f, 1.0f);

        float ITextSelection.cursorWidth { get; set; } = 1.0f;

        // Always return a valid selecting manipulator and rely on isSelectable to use it/not
        internal TextSelectingManipulator selectingManipulator =>
            m_SelectingManipulator ??= new TextSelectingManipulator(this);

        private void DrawHighlighting(MeshGenerationContext mgc)
        {
            var playmodeTintColor = panel.contextType == ContextType.Editor
                ? UIElementsUtility.editorPlayModeTintColor
                : Color.white;

            var startIndex = Math.Min(selection.cursorIndex, selection.selectIndex);
            var endIndex = Math.Max(selection.cursorIndex, selection.selectIndex);

            var startPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(startIndex);
            var endPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(endIndex);

            var firstLineIndex = uitkTextHandle.GetLineNumber(startIndex);
            var lastLineIndex = uitkTextHandle.GetLineNumber(endIndex);
            var lineHeight = uitkTextHandle.GetLineHeight(firstLineIndex);

            // We must take the padding, margin and border into account
            var layoutOffset = contentRect.min;

            if (m_TouchScreenKeyboard != null && m_HideMobileInput)
            {
                var textInfo = uitkTextHandle.textInfo;
                var stringPosition = selection.selectIndex < selection.cursorIndex ? textInfo.textElementInfo[selection.selectIndex].index : textInfo.textElementInfo[selection.cursorIndex].index;
                var length = selection.selectIndex < selection.cursorIndex ? selection.cursorIndex - stringPosition : selection.selectIndex - stringPosition;
                m_TouchScreenKeyboard.selection = new RangeInt(stringPosition, length);
            }

            // Single line
            if (firstLineIndex == lastLineIndex)
            {
                startPos += layoutOffset;
                endPos += layoutOffset;

                mgc.meshGenerator.DrawRectangle(new UIR.MeshGenerator.RectangleParams
                {
                    rect = new Rect(startPos.x, startPos.y - lineHeight, endPos.x - startPos.x, lineHeight),
                    color = selection.selectionColor,
                    playmodeTintColor = playmodeTintColor
                });
            }
            // Multiline
            else
            {
                int firstCharacterOnLine;
                int lastCharacterOnLine;
                // RichText could cause each line to have a different height. This is why they need to be drawn separately.
                for (int lineIndex = firstLineIndex; lineIndex <= lastLineIndex; lineIndex++)
                {
                    if (lineIndex == firstLineIndex)
                    {
                        lastCharacterOnLine = uitkTextHandle.textInfo.lineInfo[lineIndex].lastCharacterIndex;
                        endPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(lastCharacterOnLine, true);
                    }
                    else if (lineIndex == lastLineIndex)
                    {
                        firstCharacterOnLine =
                            uitkTextHandle.textInfo.lineInfo[lineIndex].firstCharacterIndex;
                        startPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(firstCharacterOnLine);

                        endPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(endIndex, true);
                    }
                    else if (lineIndex != firstLineIndex && lineIndex != lastLineIndex)
                    {
                        firstCharacterOnLine =
                            uitkTextHandle.textInfo.lineInfo[lineIndex].firstCharacterIndex;
                        startPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(firstCharacterOnLine);

                        lastCharacterOnLine = uitkTextHandle.textInfo.lineInfo[lineIndex].lastCharacterIndex;
                        endPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(lastCharacterOnLine, true);
                    }

                    startPos += layoutOffset;
                    endPos += layoutOffset;

                    mgc.meshGenerator.DrawRectangle(new UIR.MeshGenerator.RectangleParams
                    {
                        rect = new Rect(startPos.x, startPos.y - lineHeight, endPos.x - startPos.x, lineHeight),
                        color = selection.selectionColor,
                        playmodeTintColor = playmodeTintColor
                    });
                }
            }
        }

        // used by unit tests
        internal void DrawCaret(MeshGenerationContext mgc)
        {
            var playmodeTintColor = panel.contextType == ContextType.Editor
                ? UIElementsUtility.editorPlayModeTintColor
                : Color.white;

            var characterHeight = uitkTextHandle.GetCharacterHeightFromIndex(selection.cursorIndex);
            var width = AlignmentUtils.CeilToPixelGrid(selection.cursorWidth, scaledPixelsPerPoint);

            mgc.meshGenerator.DrawRectangle(new UIR.MeshGenerator.RectangleParams
            {
                rect = new Rect(selection.cursorPosition.x, selection.cursorPosition.y - characterHeight, width, characterHeight),
                color = selection.cursorColor,
                playmodeTintColor = playmodeTintColor
            });
        }
    }
}
