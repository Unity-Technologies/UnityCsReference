// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine.Bindings;

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
        [Obsolete("cursorColor is deprecated. Please use the corresponding USS property (--unity-cursor-color) instead.")]
        public Color cursorColor { get; set; }

        /// <summary>
        /// Background color of selected text.
        /// </summary>
        [Obsolete("selectionColor is deprecated. Please use the corresponding USS property (--unity-selection-color) instead.")]
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
        internal static readonly BindingId isSelectableProperty = nameof(isSelectable);
        internal static readonly BindingId cursorIndexProperty = nameof(cursorIndex);
        internal static readonly BindingId selectIndexProperty = nameof(selectIndex);
        internal static readonly BindingId doubleClickSelectsWordProperty = nameof(doubleClickSelectsWord);
        internal static readonly BindingId tripleClickSelectsLineProperty = nameof(tripleClickSelectsLine);
        internal static readonly BindingId cursorPositionProperty = nameof(cursorPosition);
        internal static readonly BindingId selectAllOnFocusProperty = nameof(selectAllOnFocus);
        internal static readonly BindingId selectAllOnMouseUpProperty = nameof(selectAllOnMouseUp);
        internal static readonly BindingId selectionProperty = nameof(selection);

        /// <summary>
        /// Retrieves this TextElement's ITextSelection
        /// </summary>
        [CreateProperty(ReadOnly = true)]
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
                if (value == m_IsSelectable)
                    return;

                focusable = value;
                m_IsSelectable = value;
                EnableInClassList(selectableUssClassName, value);
                NotifyPropertyChanged(isSelectableProperty);
            }
        }

        [CreateProperty]
        internal bool isSelectable
        {
            get => selection.isSelectable;
            set => selection.isSelectable = value;
        }

        int ITextSelection.cursorIndex
        {
            get => selection.isSelectable ? selectingManipulator.cursorIndex : -1;
            set
            {
                var current = selection.cursorIndex;
                if (selection.isSelectable)
                    selectingManipulator.cursorIndex = value;

                if (current != selection.cursorIndex)
                    NotifyPropertyChanged(cursorIndexProperty);
            }
        }

        [CreateProperty]
        private int cursorIndex
        {
            get => selection.cursorIndex;
            set => selection.cursorIndex = value;
        }

        int ITextSelection.selectIndex
        {
            get => selection.isSelectable ? selectingManipulator.selectIndex : -1;
            set
            {
                var current = selection.selectIndex;
                if (selection.isSelectable)
                    selectingManipulator.selectIndex = value;
                if (current != selection.selectIndex)
                    NotifyPropertyChanged(selectIndexProperty);
            }
        }

        [CreateProperty]
        private int selectIndex
        {
            get => selection.selectIndex;
            set => selection.selectIndex = value;
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

        private bool m_DoubleClickSelectsWord = true;

        bool ITextSelection.doubleClickSelectsWord
        {
            get => m_DoubleClickSelectsWord;
            set
            {
                if (m_DoubleClickSelectsWord == value)
                    return;
                m_DoubleClickSelectsWord = value;
                NotifyPropertyChanged(doubleClickSelectsWordProperty);
            }
        }

        [CreateProperty]
        internal bool doubleClickSelectsWord
        {
            get => selection.doubleClickSelectsWord;
            set => selection.doubleClickSelectsWord = value;
        }

        private bool m_TripleClickSelectsLine = true;

        bool ITextSelection.tripleClickSelectsLine
        {
            get => m_TripleClickSelectsLine;
            set
            {
                if (m_TripleClickSelectsLine == value)
                    return;
                m_TripleClickSelectsLine = value;
                NotifyPropertyChanged(tripleClickSelectsLineProperty);
            }
        }

        [CreateProperty]
        internal bool tripleClickSelectsLine
        {
            get => selection.tripleClickSelectsLine;
            set => selection.tripleClickSelectsLine = value;
        }

        private bool m_SelectAllOnFocus = false;

        /// <summary>
        /// Controls whether the element's content is selected upon receiving focus.
        /// </summary>
        bool ITextSelection.selectAllOnFocus
        {
            get => m_SelectAllOnFocus;
            set
            {
                if (m_SelectAllOnFocus == value)
                    return;
                m_SelectAllOnFocus = value;
                NotifyPropertyChanged(selectAllOnFocusProperty);
            }
        }

        [CreateProperty]
        private bool selectAllOnFocus
        {
            get => selection.selectAllOnFocus;
            set => selection.selectAllOnFocus = value;
        }

        private bool m_SelectAllOnMouseUp = false;

        /// <summary>
        /// Controls whether the element's content is selected when you mouse up for the first time.
        /// </summary>
        bool ITextSelection.selectAllOnMouseUp
        {
            get => m_SelectAllOnMouseUp;
            set
            {
                if (m_SelectAllOnMouseUp == value)
                    return;
                m_SelectAllOnMouseUp = value;
                NotifyPropertyChanged(selectAllOnMouseUpProperty);
            }
        }

        [CreateProperty]
        private bool selectAllOnMouseUp
        {
            get => selection.selectAllOnMouseUp;
            set => selection.selectAllOnMouseUp = value;
        }

        Vector2 ITextSelection.cursorPosition
        {
            get
            {
                uitkTextHandle.AddTextInfoToPermanentCache();
                return uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(selection.cursorIndex) + contentRect.min;
            }
        }

        [CreateProperty(ReadOnly = true)]
        private Vector2 cursorPosition => selection.cursorPosition;

        float ITextSelection.lineHeightAtCursorPosition
        {
            get
            {
                uitkTextHandle.AddTextInfoToPermanentCache();
                return uitkTextHandle.GetLineHeightFromCharacterIndex(selection.cursorIndex);
            }
        }

        void ITextSelection.MoveTextEnd()
        {
            if (selection.isSelectable)
                selectingManipulator.m_SelectingUtilities.MoveTextEnd();
        }

        Color m_SelectionColor = new Color(0.239f, 0.502f, 0.875f, 0.65f);
        Color ITextSelection.selectionColor
        {
            get => m_SelectionColor;
            set
            {
                if (m_SelectionColor == value)
                    return;
                m_SelectionColor = value;
                MarkDirtyRepaint();
            }
        }

        internal Color selectionColor
        {
            get => m_SelectionColor;
            set
            {
                if (m_SelectionColor == value)
                    return;
                m_SelectionColor = value;
                MarkDirtyRepaint();
            }
        }

        Color m_CursorColor = new Color(0.706f, 0.706f, 0.706f, 1.0f);
        Color ITextSelection.cursorColor {
            get => m_CursorColor;
            set
            {
                if (m_CursorColor == value)
                    return;
                m_CursorColor = value;
                MarkDirtyRepaint();
            }
        }

        internal Color cursorColor
        {
            get => m_CursorColor;
            set
            {
                if (m_CursorColor == value)
                    return;
                m_CursorColor = value;
                MarkDirtyRepaint();
            }
        }

        private float m_CursorWidth = 1.0f;

        float ITextSelection.cursorWidth
        {
            get => m_CursorWidth;
            set
            {
                if (Mathf.Approximately(m_CursorWidth, value))
                    return;
                m_CursorWidth = value;
                MarkDirtyRepaint();
            }
        }

        // Always return a valid selecting manipulator and rely on isSelectable to use it/not
        internal TextSelectingManipulator selectingManipulator
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_SelectingManipulator ??= new TextSelectingManipulator(this);
        }

        private void DrawHighlighting(MeshGenerationContext mgc)
        {
            var playmodeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var startIndex = Math.Min(selection.cursorIndex, selection.selectIndex);
            var endIndex = Math.Max(selection.cursorIndex, selection.selectIndex);

            var startPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(startIndex);
            var endPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(endIndex);

            var firstLineIndex = uitkTextHandle.GetLineNumber(startIndex);
            var lastLineIndex = uitkTextHandle.GetLineNumber(endIndex);
            var lineHeight = uitkTextHandle.GetLineHeight(firstLineIndex);

            // We must take the padding, margin and border into account
            var layoutOffset = contentRect.min;

            if (m_TouchScreenKeyboard != null && hideMobileInput)
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
                    color = selectionColor,
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
                        lastCharacterOnLine = GetLastCharacterAt(lineIndex);
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

                        lastCharacterOnLine = GetLastCharacterAt(lineIndex);
                        endPos = uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(lastCharacterOnLine, true);
                    }

                    startPos += layoutOffset;
                    endPos += layoutOffset;

                    mgc.meshGenerator.DrawRectangle(new UIR.MeshGenerator.RectangleParams
                    {
                        rect = new Rect(startPos.x, startPos.y - lineHeight, endPos.x - startPos.x, lineHeight),
                        color = selectionColor,
                        playmodeTintColor = playmodeTintColor
                    });
                }
            }
        }

        private void DrawNativeHighlighting(MeshGenerationContext mgc)
        {
            var playmodeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var startIndex = Math.Min(selection.cursorIndex, selection.selectIndex);
            var endIndex = Math.Max(selection.cursorIndex, selection.selectIndex);
            var rectangles = uitkTextHandle.GetHighlightRectangles(startIndex, endIndex);

            for (int i = 0; i < rectangles.Length; i++)
            {
                mgc.meshGenerator.DrawRectangle(new UIR.MeshGenerator.RectangleParams
                {
                    rect = new Rect(rectangles[i].position + contentRect.min, rectangles[i].size),
                    color = selectionColor,
                    playmodeTintColor = playmodeTintColor
                });
            }
        }

        // used by unit tests
        internal void DrawCaret(MeshGenerationContext mgc)
        {
            var playmodeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var characterHeight = uitkTextHandle.GetCharacterHeightFromIndex(selection.cursorIndex);
            var width = AlignmentUtils.CeilToPixelGrid(selection.cursorWidth, scaledPixelsPerPoint);

            mgc.meshGenerator.DrawRectangle(new UIR.MeshGenerator.RectangleParams
            {
                rect = new Rect(selection.cursorPosition.x, selection.cursorPosition.y - characterHeight, width, characterHeight),
                color = cursorColor,
                playmodeTintColor = playmodeTintColor
            });
        }

        int GetLastCharacterAt(int lineIndex)
        {
            var lastCharacterIndex = uitkTextHandle.textInfo.lineInfo[lineIndex].lastCharacterIndex;
            var firstCharacterIndex = uitkTextHandle.textInfo.lineInfo[lineIndex].firstCharacterIndex;
            var lastCharacter = uitkTextHandle.textInfo.textElementInfo[lastCharacterIndex];

            // Select the last character that is not a \n or a \r
            while (lastCharacter.character is '\n' or '\r' && lastCharacterIndex > firstCharacterIndex)
                lastCharacter = uitkTextHandle.textInfo.textElementInfo[--lastCharacterIndex];

            return lastCharacterIndex;
        }
    }
}
