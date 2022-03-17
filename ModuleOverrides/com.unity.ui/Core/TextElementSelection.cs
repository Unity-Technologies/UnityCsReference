// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.UIR;

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
        /// Color of the cursor.
        /// </summary>
        public Color cursorColor { get; set; }

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

        /// <summary>
        /// Background color of selected text.
        /// </summary>
        public Color selectionColor { get; set; }

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
        internal bool selectAllOnFocus { get; set; }

        /// <summary>
        /// Controls whether the element's content is selected when you mouse up for the first time.
        /// </summary>
        internal bool selectAllOnMouseUp { get; set; }

        /// <summary>
        /// The position of the text cursor inside the element"/>.
        /// </summary>
        public Vector2 cursorPosition { get; }
        internal float cursorLineHeight  { get; }
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

        internal TextSelectingManipulator m_SelectingManipulator;

        bool m_IsSelectable;
        /// <summary>
        /// Controls whether the element's content is selectable.  Note that selectable TextElement are required to be focusable.
        /// </summary>
        public bool isSelectable
        {
            get => m_IsSelectable;
            set
            {
                if (value == m_IsSelectable)
                    return;

                focusable = value;
                m_SelectingManipulator = value ? new TextSelectingManipulator(this) : null;
                m_IsSelectable = value;
            }
        }

        int ITextSelection.cursorIndex
        {
            get => m_SelectingManipulator?.cursorIndex ?? -1;
            set
            {
                if (isSelectable)
                    m_SelectingManipulator.cursorIndex = value;
            }
        }


        int ITextSelection.selectIndex
        {
            get => m_SelectingManipulator?.selectIndex ?? -1;
            set
            {
                if (isSelectable)
                    m_SelectingManipulator.selectIndex = value;
            }
        }

        void ITextSelection.SelectAll()
        {
            m_SelectingManipulator?.m_SelectingUtilities.SelectAll();
        }

        void ITextSelection.SelectNone()
        {
            m_SelectingManipulator?.m_SelectingUtilities.SelectNone();
        }

        void ITextSelection.SelectRange(int cursorIndex, int selectionIndex)
        {
            if (isSelectable)
            {
                m_SelectingManipulator.m_SelectingUtilities.cursorIndex = cursorIndex;
                m_SelectingManipulator.m_SelectingUtilities.selectIndex = selectionIndex;
            }
        }

        bool ITextSelection.HasSelection()
        {
            return isSelectable && m_SelectingManipulator.HasSelection();
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


        Vector2 ITextSelection.cursorPosition => uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(selection.cursorIndex);
        float ITextSelection.cursorLineHeight => uitkTextHandle.GetLineHeightFromCharacterIndex(selection.cursorIndex);

        void ITextSelection.MoveTextEnd()
        {
            if (isSelectable)
                m_SelectingManipulator.m_SelectingUtilities.MoveTextEnd();
        }

        Color ITextSelection.selectionColor { get; set; } = new Color(0.239f, 0.502f, 0.875f, 0.65f);


        Color ITextSelection.cursorColor { get; set; } = new Color(0.706f, 0.706f, 0.706f, 1.0f);

        float ITextSelection.cursorWidth { get; set; } = 1.0f;

        void DrawHighlighting(MeshGenerationContext mgc)
        {
            var playmodeTintColor = panel.contextType == ContextType.Editor
                ? UIElementsUtility.editorPlayModeTintColor
                : Color.white;

            var startIndex = Math.Min(selection.cursorIndex, selection.selectIndex);
            var startPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(startIndex);
            var endIndex = Math.Max(selection.cursorIndex, selection.selectIndex);
            var endPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(endIndex);

            var firstLineIndex = uitkTextHandle.textHandle.GetLineNumber(startIndex);
            var lastLineIndex = uitkTextHandle.textHandle.GetLineNumber(endIndex);
            var lineHeight = uitkTextHandle.GetLineHeight(firstLineIndex);

            // Single line
            if (firstLineIndex == lastLineIndex)
            {
                mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
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
                        lastCharacterOnLine = uitkTextHandle.textHandle.textInfo.lineInfo[lineIndex].lastCharacterIndex;
                        endPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(lastCharacterOnLine);
                    }
                    else if (lineIndex == lastLineIndex)
                    {
                        firstCharacterOnLine =
                            uitkTextHandle.textHandle.textInfo.lineInfo[lineIndex].firstCharacterIndex;
                        startPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(firstCharacterOnLine);

                        endPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(endIndex);
                    }
                    else if (lineIndex != firstLineIndex && lineIndex != lastLineIndex)
                    {
                        firstCharacterOnLine =
                            uitkTextHandle.textHandle.textInfo.lineInfo[lineIndex].firstCharacterIndex;
                        startPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(firstCharacterOnLine);

                        lastCharacterOnLine = uitkTextHandle.textHandle.textInfo.lineInfo[lineIndex].lastCharacterIndex;
                        endPos = uitkTextHandle.GetCursorPositionFromIndexUsingLineHeight(lastCharacterOnLine);
                    }

                    mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
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
            var width = AlignmentUtils.FloorToPixelGrid(selection.cursorWidth, scaledPixelsPerPoint);

            mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams
            {
                rect = new Rect(selection.cursorPosition.x, selection.cursorPosition.y - characterHeight, width, characterHeight),
                color = selection.cursorColor,
                playmodeTintColor = playmodeTintColor
            });
        }
    }
}
