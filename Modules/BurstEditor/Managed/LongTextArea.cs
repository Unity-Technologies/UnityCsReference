// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

[assembly: InternalsVisibleTo("Unity.Burst.Editor.Tests")]

namespace Unity.Burst.Editor
{
    internal class LongTextArea
    {
        internal const float naturalEnhancedPad = 20f;
        private const int kMaxFragment = 2048;

        internal struct Fragment
        {
            public int lineCount;
            public string text;
        }
        public enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }
        internal struct Branch
        {
            public BurstDisassembler.AsmEdge Edge;

            public Rect StartHorizontal;
            public Rect VerticalLine;
            public Rect EndHorizontal;

            public Rect UpperLine;
            public Rect LowerLine;
            public float UpperAngle;
            public float LowerAngle;

            public Branch(BurstDisassembler.AsmEdge edge, Rect startHorizontal, Rect verticalLine, Rect endHorizontal, Rect upperLine, Rect lowerLine, float angle1, float angle2)
            {
                Edge = edge;

                StartHorizontal = startHorizontal;
                VerticalLine = verticalLine;
                EndHorizontal = endHorizontal;

                UpperLine = upperLine;
                LowerLine = lowerLine;
                UpperAngle = angle1;
                LowerAngle = angle2;
            }
        }

        internal float fontHeight = 0.0f;
        internal float fontWidth = 0.0f;

        public string GetText => m_Text;

        private string m_Text = "";
        private int _mTextLines = 0;
        private List<Fragment> m_Fragments = null;
        private bool invalidated = true;
        internal float finalAreaSizeXRenderSize = 0.0f;
        internal int finalAreaSizeYInLines = 0;

        private static readonly Texture2D backgroundTexture = Texture2D.whiteTexture;
        private static readonly GUIStyle textureStyle = new GUIStyle { normal = new GUIStyleState { background = backgroundTexture } };

        internal float horizontalPad = 50.0f;

        internal int[] lineDepth = null;
        internal bool[] _folded = null;
        internal int[] blockLine = null;
        private List<Fragment>[] _blocksFragments = null;

        private Fragment[] _blocksFragmentsStart = null;

        private Fragment GetFragmentStart(int blockIdx)
        {
            AddFoldedString(blockIdx);

            return _blocksFragmentsStart[blockIdx];
        }

        // Need two separate caches for start of blocks fragments, as we possibly differentiate between
        // rendered line and copied.
        private Fragment[] _blocksFragmentsStartPlain = null;
        private Fragment GetFragmentStartPlain(int blockIdx)
        {
            AddFoldedStringColorless(blockIdx);

            return _blocksFragmentsStartPlain[blockIdx];
        }

        // Used for searching when text is colored:
        internal List<Fragment>[] blocksFragmentsPlain = null;

        internal List<Fragment> GetPlainFragments(int blockIdx)
        {
            blocksFragmentsPlain[blockIdx] ??= RecomputeFragmentsFromBlock(blockIdx, false);

            return blocksFragmentsPlain[blockIdx];
        }

        private BurstDisassembler _disassembler;

        private int _selectBlockStart = -1;
        private float _selectStartY = -1f;
        private int _selectBlockEnd = -1;
        private float _selectEndY = -1f;

        private float _renderStartY = 0.0f;
        private int _renderBlockStart = -1;
        internal int _renderBlockEnd = -1;
        private int _initialLineCount = -1;

        private bool _mouseDown = false;
        private bool _mouseOutsideBounds = true;
        internal Vector2 selectPos = Vector2.zero;
        internal Vector2 selectDragPos = Vector2.zero;

        public bool HasSelection { get; private set; }

        private Color _selectionColor;
        private readonly Color _selectionColorDarkmode = new Color(0f, .6f, .9f, .5f);
        private readonly Color _selectionColorLightmode = new Color(0f, 0f, .9f, .2f);

        private readonly Color _lineHighlightColor = new Color(.56f, .56f, .56f, 0.2f);

        private readonly Color[] _regsColourWheel =
        {
            new Color(0f, 0f, .9f, .2f),
            new Color(0f, .9f, 0f, .2f),
            new Color(.9f, 0f, 0f, .2f)
        };

        private Color _hoverBoxColor;
        private Color _hoverTextColor;
        private readonly Color _hoverBoxColorDarkMode = new Color(.33f, .33f, .33f);
        private readonly Color _hoverBoxColorLightMode = new Color(.88f, .88f, .88f);

        // Current active hit should just be selection color
        private readonly Color _searchHitColor = new Color(.5f, .2f, .2f, .5f);

        private SearchCriteria _prevCriteria;
        internal int _activeSearchHitIdx = -1;
        internal readonly Dictionary<int, List<(int startIdx, int endIdx, int nr)>> searchHits = new Dictionary<int, List<(int startIdx, int endIdx, int nr)>>();

        private int _nrSearchHits = 0;

        public int NrSearchHits => _nrSearchHits;
        public int ActiveSearchNr => _activeSearchHitIdx;

        private readonly Color[] _colourWheel = new Color[]
            {Color.blue, Color.cyan, Color.green, Color.magenta, Color.red, Color.yellow, Color.white, Color.black};

        internal (int idx, int length) textSelectionIdx = (0, 0);
        private bool _textSelectionIdxValid = true;

        internal (int blockIdx, int textIdx) enhancedTextSelectionIdxStart = (0, 0);
        internal (int blockIdx, int textIdx) enhancedTextSelectionIdxEnd = (0, 0);

        internal bool CopyColorTags { get; private set; } = true;

        private bool _oldShowBranchMarkers = false;

        internal const int regLineThickness = 2;
        private const int highlightLineThickness = 3;
        private float _verticalPad = 0;

        internal Branch hoveredBranch;
        private BurstDisassembler.AsmEdge _prevHoveredEdge;

        private string _jobName;

        // Have this field, as NextHit needs the actual horizontal padding
        // in order to have correct horizontal scroll.
        private float _actualHorizontalPad = 0;

        internal int MaxLineDepth;

        /// <returns>
        /// (idx in regards to whole <see cref="str"/> , where color tags are removed from this line, idx from this line with color tags removed)
        /// </returns>
        internal readonly Func<string, int, (int total, int relative)> GetEndIndexOfColoredLine = BurstStringSearch.GetEndIndexOfColoredLine;


        internal readonly Func<string, int, (int total, int relative)> GetEndIndexOfPlainLine = BurstStringSearch.GetEndIndexOfPlainLine;


        internal bool IsTextSet(string jobName)
        {
            return _jobName == jobName;
        }

        public void SetText(string jobName, string textToRender, bool isDarkMode, BurstDisassembler disassembler, bool useDisassembler)
        {
            selectDragPos = Vector2.zero;
            StopSearching();
            StopSelection();
            ClearLinePress();

            _jobName = jobName;


            if (!useDisassembler)
            {
                m_Text = textToRender;
                _disassembler = null;
                m_Fragments = RecomputeFragments(m_Text);
                horizontalPad = 0.0f;
            }
            else
            {
                m_Fragments = null;
                SetDisassembler(disassembler);

                (_selectionColor, _hoverBoxColor, _hoverTextColor) = isDarkMode
                    ? (_selectionColorDarkmode, _hoverBoxColorDarkMode, _hoverBoxColorLightMode)
                    : (_selectionColorLightmode, _hoverBoxColorLightMode, _hoverBoxColorDarkMode);
            }

            invalidated = true;
        }

        public void ExpandAllBlocks()
        {
            StopSelection();
            ClearLinePress();

            int blockIdx = 0;
            foreach (var block in _disassembler.Blocks)
            {
                var changed = _folded[blockIdx];
                var blockLongEnoughForFold = block.Length > 1;

                _folded[blockIdx] = false;
                if (changed && blockLongEnoughForFold)
                {
                    finalAreaSizeYInLines += Math.Max(block.Length - 1, 1);
                }
                blockIdx++;
            }
        }

        private void ClearLinePress()
        {
            _lineRegCache.Clear();
            _pressedLine = -1;
        }

        public void FocusCodeBlocks()
        {
            StopSelection();
            ClearLinePress();

            var blockIdx = 0;
            foreach (var block in _disassembler.Blocks)
            {
                bool changed = false;
                switch (block.Kind)
                {
                    case BurstDisassembler.AsmBlockKind.None:
                    case BurstDisassembler.AsmBlockKind.Directive:
                    case BurstDisassembler.AsmBlockKind.Block:
                    case BurstDisassembler.AsmBlockKind.Data:
                        if (!_folded[blockIdx])
                        {
                            changed = true;
                        }
                        _folded[blockIdx] = true;
                        break;
                    case BurstDisassembler.AsmBlockKind.Code:
                        if (_folded[blockIdx])
                        {
                            changed = true;
                        }
                        _folded[blockIdx] = false;
                        break;
                }

                if (changed)
                {
                    if (_folded[blockIdx])
                    {
                        finalAreaSizeYInLines -= Math.Max(block.Length - 1, 1);
                    }
                    else
                    {
                        finalAreaSizeYInLines += Math.Max(block.Length - 1, 1);
                    }

                }
                blockIdx++;
            }
        }

        private void ComputeInitialLineCount()
        {
            var blockIdx = 0;
            _initialLineCount = 0;
            foreach (var block in _disassembler.Blocks)
            {
                switch (block.Kind)
                {
                    case BurstDisassembler.AsmBlockKind.None:
                    case BurstDisassembler.AsmBlockKind.Directive:
                    case BurstDisassembler.AsmBlockKind.Block:
                    case BurstDisassembler.AsmBlockKind.Data:
                        _folded[blockIdx] = true;
                        break;
                    case BurstDisassembler.AsmBlockKind.Code:
                        _folded[blockIdx] = false;
                        break;
                }
                var blockLongEnoughForFold = block.Length > 1;
                _folded[blockIdx] = _folded[blockIdx] && blockLongEnoughForFold;

                _initialLineCount += _folded[blockIdx] ? 1 : block.Length;
                blockIdx++;
            }
        }

        public void SetDisassembler(BurstDisassembler disassembler)
        {
            _disassembler = disassembler;
            if (disassembler == null)
            {
                return;
            }

            var numBlocks = _disassembler.Blocks.Count;
            var numLinesFromBlock = new int[numBlocks];
            lineDepth = new int[numBlocks];
            _folded = new bool[numBlocks];
            blockLine = new int[numBlocks];
            _blocksFragments = new List<Fragment>[numBlocks];
            _blocksFragmentsStart = new Fragment[numBlocks];
            _blocksFragmentsStartPlain = new Fragment[numBlocks];
            blocksFragmentsPlain = new List<Fragment>[numBlocks];

            ComputeInitialLineCount();

            // Count edges
            var edgeCount = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < numBlocks; i++)
            {
                sb.Append(_disassembler.GetOrRenderBlockToTextUncached(i, false));

                var block = _disassembler.Blocks[i];
                if (block.Edges != null)
                {
                    foreach (var edge in block.Edges)
                    {
                        if (edge.Kind == BurstDisassembler.AsmEdgeKind.OutBound)
                        {
                            edgeCount++;
                        }
                    }
                }
            }
            m_Text = sb.ToString();

            var edgeArray = new BurstDisassembler.AsmEdge[edgeCount];
            var edgeIndex = 0;
            foreach (var block in _disassembler.Blocks)
            {
                if (block.Edges != null)
                {
                    foreach (var edge in block.Edges)
                    {
                        if (edge.Kind == BurstDisassembler.AsmEdgeKind.OutBound)
                        {
                            edgeArray[edgeIndex++] = edge;
                        }
                    }
                }
            }

            Array.Sort(edgeArray, (a, b) =>
            {
                var src1BlockIdx = a.OriginRef.BlockIndex;
                var src1Line = _disassembler.Blocks[src1BlockIdx].LineIndex;
                src1Line += a.OriginRef.LineIndex;
                var dst1BlockIdx = a.LineRef.BlockIndex;
                var dst1Line = _disassembler.Blocks[dst1BlockIdx].LineIndex;
                dst1Line += a.LineRef.LineIndex;
                var Len1 = Math.Abs(src1Line - dst1Line);
                var src2BlockIdx = b.OriginRef.BlockIndex;
                var src2Line = _disassembler.Blocks[src2BlockIdx].LineIndex;
                src2Line += b.OriginRef.LineIndex;
                var dst2BlockIdx = b.LineRef.BlockIndex;
                var dst2Line = _disassembler.Blocks[dst2BlockIdx].LineIndex;
                dst2Line += b.LineRef.LineIndex;
                var Len2 = Math.Abs(src2Line - dst2Line);
                return Len1 - Len2;
            });

            // Iterate through the blocks to precompute the widths for branches
            var maxLine = 0;
            foreach (var edge in edgeArray)
            {
                if (edge.Kind != BurstDisassembler.AsmEdgeKind.OutBound) continue;

                var s = edge.OriginRef.BlockIndex;
                var e = edge.LineRef.BlockIndex;
                if (e == s + 1)
                {
                    continue;   // don't render if its pointing to next line
                }

                var m = 0;

                var l = s;
                var le = e;
                if (e < s)
                {
                    l = e;
                    le = s;
                }

                for (; l <= le; l++)
                {
                    numLinesFromBlock[l]++;
                    if (m < numLinesFromBlock[l])
                    {
                        m = numLinesFromBlock[l];
                    }
                    if (maxLine < m)
                    {
                        maxLine = m;
                    }
                }

                lineDepth[s] = m;
            }

            MaxLineDepth = maxLine;

            horizontalPad = naturalEnhancedPad + maxLine * 10;
        }

        // Changing the font size doesn't update the text field, so added this to force a recalculation
        public void Invalidate()
        {
            // Assumed to only happen on altering font size (or similar actions not leading to altered number of
            // assembly lines in view).
            // Hence we only have to clear the cached rects for highlighting, and not the actual line number _linePressed,
            // as that will stay constant.
            _lineRegCache.Clear();
            invalidated = true;
        }

        private struct HoverBox
        {
            public Rect box;
            public string info;
            /// <summary>
            /// Indicates start and end column of hovered token.
            /// </summary>
            public (int start, int end) columnRange;
            public int lineNumber;
            public bool valid;
        }

        private HoverBox hover;

        public void Interact(Rect workingArea, EventType eventT)
        {
            if (_disassembler == null)
            {
                return;
            }

            var pos = GetMousePosForHover();
            if (!workingArea.Contains(pos))
            {
                // Mouse position outside of assembly view
                return;
            }

            // lineNumber (absolute)
            var lineNumber = GetLineNumber(pos);

            // Mouse just pressed this line.
            if (eventT == EventType.MouseDown)
            {
                var lineStr = GetLineString(_disassembler.Lines[lineNumber]);
                var lineLen = (lineStr.Length) * fontWidth;

                var blockIdx = 0;
                var len = _disassembler.Blocks.Count;
                for (; blockIdx < len-1; blockIdx++)
                {
                    if (_disassembler.Blocks[blockIdx+1].LineIndex > lineNumber)
                    {
                        break;
                    }
                }

                if (_folded[blockIdx])
                {
                    lineLen += 4*fontWidth;
                }
                if (pos.x < _actualHorizontalPad || pos.x > _actualHorizontalPad + lineLen)
                {
                    _pressedLine = -1;
                }
                else
                {
                    _pressedLine = lineNumber;
                }
            }

            var column = Mathf.FloorToInt((pos.x - horizontalPad) / fontWidth);

            var sameToken = hover.lineNumber == lineNumber &&
                            BurstMath.WithinRange(hover.columnRange.start, hover.columnRange.end, column);
            if (hover.valid && (sameToken || hover.box.Contains(pos)))
            {
                // Use the cached info text.
                return;
            }
            // Cached instruction info text is not valid. So try query new.
            hover.valid = false;

            if (column < 0 || lineNumber < 0 || lineNumber >= _disassembler.Lines.Count)
            {
                return;
            }

            int tokIdx;
            try
            {
                // block 0 is queried as the absolut line number is given to the function.
                // This will give us the correct assembly token, as the function queries
                // assembly line based on offset from block start => line lineNumber is queried.
                tokIdx = _disassembler.GetTokenIndexFromColumn(0, lineNumber, column, out _);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            if (tokIdx < 0)
            {
                return;
            }
            // Found match

            var token = _disassembler.GetToken(tokIdx);
            var tokenString = _disassembler.GetTokenAsText(token);

            if (token.Kind != BurstDisassembler.AsmTokenKind.Instruction &&
                token.Kind != BurstDisassembler.AsmTokenKind.BranchInstruction &&
                token.Kind != BurstDisassembler.AsmTokenKind.CallInstruction &&
                token.Kind != BurstDisassembler.AsmTokenKind.FunctionBegin &&
                token.Kind != BurstDisassembler.AsmTokenKind.FunctionEnd &&
                token.Kind != BurstDisassembler.AsmTokenKind.JumpInstruction &&
                token.Kind != BurstDisassembler.AsmTokenKind.ReturnInstruction &&
                token.Kind != BurstDisassembler.AsmTokenKind.InstructionSIMD)
            {
                return;
            }
            if (_disassembler.GetInstructionInformation(tokenString, out var info))
            {
                hover.valid = true;
                // Don't know the actual size needed yet.
                hover.box = new Rect(pos, Vector2.zero);
                hover.info = info;
                hover.lineNumber = lineNumber;

                // Find column range of instruction:
                var line = _disassembler.Lines[lineNumber];
                var idx = line.ColumnIndex + (tokIdx - line.TokenIndex - 1);
                var colStart = _disassembler.ColumnIndices[idx];
                var colEnd = colStart + token.Length - 1;
                hover.columnRange = (colStart, colEnd);
            }
        }

        /// Not clipped, so use only if you know the line will be within the UI element
        private static void DrawLine(Vector2 start, Vector2 end, int width)
        {
            var matrix = GUI.matrix;
            Vector2 distance = end - start;
            float angle = Mathf.Rad2Deg * Mathf.Atan(distance.y / distance.x);
            if (distance.x < 0)
            {
                angle += 180;
            }

            int width2 = (int)Mathf.Ceil(width / 2);

            Rect pos = new Rect(start.x, start.y - width2, distance.magnitude, width);

            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(pos, backgroundTexture);
            GUI.matrix = matrix; // restore initial matrix to avoid floating point inaccuracies with RotateAroundPivot(...)
        }

        private void DrawLine(Rect line, float angle)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(line.x, line.center.y));
            GUI.DrawTexture(line, backgroundTexture);
            GUI.matrix = matrix; // restore initial matrix to avoid floating point inaccuracies with RotateAroundPivot(...)
        }

        private int CalculateNumberOfLines(int width, string text)
        {
            var lineCount = 1;
            var col = 0;
            var wordCharCount = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (char.IsWhiteSpace(ch) || ch == '-')
                {
                    if (col + wordCharCount < width)
                    {
                        col++; // space
                    }
                    else
                    {
                        lineCount++;
                        col = 0;
                    }

                    col += wordCharCount;
                    wordCharCount = 0;
                }
                else
                {
                    wordCharCount++;
                }
            }

            if (wordCharCount + col >= width)
            {
                lineCount++;
            }

            return lineCount;
        }

        private void DrawHover(GUIStyle style, Rect workingArea)
        {
            if (!hover.valid)
            {
                return;
            }
            if (hover.box.size == Vector2.zero)
            {
                // Hover size has not been initialized yet.
                var sizex = (hover.info.Length + 2.0f) * fontWidth;
                var sizey = 2.0f * fontHeight;
                var posx = hover.box.x + 10f;
                var posy = hover.box.y;
                if (posx + sizex > workingArea.width)
                {
                    // Box exceeds rightmost bound so cramp it in
                    var availableSpace = workingArea.xMax - posx;
                    var possiblyCharacters = Mathf.FloorToInt(availableSpace / fontWidth);
                    var neededLines = CalculateNumberOfLines(possiblyCharacters - 1, hover.info);

                    var availableSpacey = workingArea.yMax - posy;
                    var possibleLines = Mathf.FloorToInt(availableSpacey / fontHeight);

                    var diff = neededLines - possibleLines;
                    if (diff > 0)
                    {
                        posy -= diff * fontHeight;
                    }

                    sizey = neededLines * fontHeight + fontHeight;
                    sizex = availableSpace;
                }
                hover.box.size = new Vector2(sizex, sizey);
                hover.box.position = new Vector2(posx, posy);
            }
            var col = GUI.color;
            GUI.color = _hoverBoxColor;

            var pos = hover.box.position;
            var size = hover.box.size;
            GUI.Box(hover.box, "", textureStyle);

            GUI.color = _hoverTextColor;
            var hoverStyle = style;
            hoverStyle.wordWrap = true;
            GUI.Label(
                new Rect(pos.x + fontWidth * 0.5f,
                    pos.y + fontHeight * 0.5f,
                    size.x - fontWidth/2f + 1.0f,
                    size.y - fontHeight + 1.0f),
                hover.info, hoverStyle);

            GUI.color = col;
        }

        private void MakeBranchThin(ref Branch branch)
        {
            int lineThickness = regLineThickness;

            branch.StartHorizontal.height = lineThickness;
            branch.VerticalLine.width = lineThickness;
            branch.EndHorizontal.height = lineThickness;

            branch.UpperLine.height = lineThickness;
            branch.LowerLine.height = lineThickness;

            // Adjusting position for arrow tip for thicker lines.
            branch.UpperLine.y += (highlightLineThickness - regLineThickness);

            branch.UpperLine.position -= new Vector2(.5f, .5f);
            branch.LowerLine.position -= new Vector2(.5f, -.5f);

            // Make end part of arrow expand upwards.
            branch.EndHorizontal.y += (highlightLineThickness - regLineThickness);
        }

        /// <summary>
        /// Use this for hover, as there is a slight visual mis-balance
        /// between cursor position and visual cursor.
        /// </summary>
        private Vector2 GetMousePosForHover()
        {
            Vector2 mousePos = Event.current.mousePosition;
            mousePos.y -= 0.5f;
            mousePos.x += 0.5f;
            return mousePos;
        }

        /// <summary>
        /// Calculate the position and size of an edge, and return it as a branch.
        /// </summary>
        /// <param name="edge"> The edge to base branch on. </param>
        /// <param name="x"> Start x position of branch. </param>
        /// <param name="y1"> Start y position of branch. </param>
        /// <param name="y2"> End y position of branch. </param>
        /// <param name="w"> Depth of branch. </param>
        private Branch CalculateBranch(BurstDisassembler.AsmEdge edge, float x, float y1, float y2, int w)
        {
            bool isEdgeHovered = edge.Equals(_prevHoveredEdge);

            int lineThickness = isEdgeHovered
                ? highlightLineThickness
                : regLineThickness;

            // Calculate rectangles for branch arrows:
            var start = new Vector2(x, y1 + _verticalPad);
            var end = start + new Vector2(-(w * 10), 0);

            var branchStartPos = new Rect(end.x - lineThickness / 2, start.y - 1, start.x - end.x + lineThickness / 2, lineThickness);

            start = end;
            end = start + new Vector2(0, y2 - y1);

            var branchVerticalPartPos = end.y < start.y
                ? new Rect(start.x - 1, end.y, lineThickness, start.y - end.y)
                : new Rect(start.x - 1, start.y, lineThickness, end.y - start.y);

            start = end;
            end = start + new Vector2(w * 10, 0);

            var branchEndPos = new Rect(start.x - lineThickness / 2, start.y - 1, end.x - start.x, lineThickness);

            // Calculate rectangles for arrow tip.
            Vector2 lowerArrowTipStart = end;
            Vector2 upperArrowtipStart = end;

            //   Moving the arrow tips closer together.
            upperArrowtipStart += new Vector2(0, 0.5f);
            lowerArrowTipStart -= new Vector2(0, 0.5f);

            //   Upper arrow tip.
            var upperArrowTipEnd = upperArrowtipStart + new Vector2(-5, -5);

            var upperLine = new Rect(upperArrowtipStart.x, upperArrowtipStart.y - (int)Mathf.Ceil(lineThickness / 2), (upperArrowTipEnd - upperArrowtipStart).magnitude, lineThickness);

            //   Lower arrow tip.
            var lowerArrowtipEnd = lowerArrowTipStart + new Vector2(-5, 5);
            var lowerLine = new Rect(lowerArrowTipStart.x, lowerArrowTipStart.y - (int)Mathf.Ceil(lineThickness / 2), (lowerArrowtipEnd - lowerArrowTipStart).magnitude, lineThickness);

            if (isEdgeHovered)
            {
                // Adjusting position for arrow tip for thicker lines.
                upperArrowtipStart.y -= (highlightLineThickness - regLineThickness);
                upperArrowTipEnd.y -= (highlightLineThickness - regLineThickness);
                upperLine.y -= (highlightLineThickness - regLineThickness);

                upperLine.position += new Vector2(.5f, .5f);
                lowerLine.position += new Vector2(.5f, -.5f);

                // Make end part of arrow expand upwards.
                branchEndPos.y -= (highlightLineThickness - regLineThickness);
            }

            var branch = new Branch(edge, branchStartPos, branchVerticalPartPos, branchEndPos, upperLine, lowerLine,
                BurstMath.CalculateAngle(upperArrowtipStart, upperArrowTipEnd), BurstMath.CalculateAngle(lowerArrowTipStart, lowerArrowtipEnd));

            // Handling whether mouse is hovering over edge.
            Vector2 mousePos = GetMousePosForHover();

            // Rotate mousePos so it seems like lower arrow tip is not rotated.
            Vector2 lowerArrowTipPivot = lowerArrowTipStart;
            lowerArrowTipPivot.y -= (int)Mathf.Ceil(lineThickness / 2);
            Vector2 angledMouseLower = BurstMath.AnglePoint(BurstMath.CalculateAngle(lowerArrowTipPivot, lowerArrowtipEnd), mousePos, new Vector2(lowerLine.x, lowerLine.center.y));
            angledMouseLower.y -= (int)Mathf.Ceil(lineThickness / 2);

            Vector2 upperArrowTipPivot = upperArrowtipStart;
            upperArrowTipPivot.y += (int)Mathf.Ceil(lineThickness / 2);
            Vector2 angleMouseUpper = BurstMath.AnglePoint(BurstMath.CalculateAngle(upperArrowTipPivot, upperArrowTipEnd) - 360, mousePos, new Vector2(upperLine.x, upperLine.center.y));
            angleMouseUpper.y += (int)Mathf.Ceil(lineThickness / 2);

            if (BurstMath.AdjustedContains(branchStartPos, mousePos) || BurstMath.AdjustedContains(branchVerticalPartPos, mousePos) || BurstMath.AdjustedContains(branchEndPos, mousePos)
                || BurstMath.AdjustedContains(lowerLine, angledMouseLower) || BurstMath.AdjustedContains(upperLine, angleMouseUpper))
            {
                // Handling whether another branch was already made thick is done in DrawBranch(...).
                if (!hoveredBranch.Edge.Equals(default(BurstDisassembler.AsmEdge)) && hoveredBranch.Edge.Equals(_prevHoveredEdge))
                {
                    return branch;
                }
                hoveredBranch = branch;
            }
            return branch;
        }



        /// <summary>
        /// Draws a branch as a jump-able set of boxes.
        /// </summary>
        /// <param name="branch"> The branch to draw. </param>
        /// <param name="w"> Depth of the branch. </param>
        /// <param name="colourWheel"> Array of possible colors for branches. </param>
        /// <param name="workingArea"> Current view in inspector. </param>
        private void DrawBranch(Branch branch, int w, Rect workingArea)
        {
            bool isBranchHovered = branch.Edge.Equals(hoveredBranch.Edge);
            Vector2 scrollPos = workingArea.position;

            int lineThickness = isBranchHovered
                ? highlightLineThickness
                : regLineThickness;

            GUI.color = _colourWheel[w % _colourWheel.Length];

            // Check if hovered but not made thick yet:
            if (isBranchHovered && !branch.Edge.Equals(_prevHoveredEdge))
            {
                // alter thickness as edge is hovered over.
                branch.StartHorizontal.height = highlightLineThickness;
                branch.VerticalLine.width = highlightLineThickness;
                branch.EndHorizontal.height = highlightLineThickness;

                branch.UpperLine.height = highlightLineThickness;
                branch.LowerLine.height = highlightLineThickness;

                // Adjusting position for arrow tip for thicker lines.
                branch.UpperLine.y -= (highlightLineThickness - regLineThickness);

                branch.UpperLine.position += new Vector2(.5f, .5f);
                branch.LowerLine.position += new Vector2(.5f, -.5f);

                // Make end part of arrow expand upwards.
                branch.EndHorizontal.y -= (highlightLineThickness - regLineThickness);
            }
            else if (branch.EndHorizontal.height == highlightLineThickness && !isBranchHovered)
            {
                // the branch was previously hovered, but is now hidden behind
                // another branch, that is the one being visually hovered.
                MakeBranchThin(ref branch);
            }

            // Render actual arrows:
            GUI.Box(branch.StartHorizontal, "", textureStyle);
            GUI.Box(branch.VerticalLine, "", textureStyle);

            float yy = branch.EndHorizontal.y - scrollPos.y;
            if (yy >= 0 && yy < workingArea.height)
            {
                GUI.Box(branch.EndHorizontal, "", textureStyle);
                DrawLine(branch.UpperLine, branch.UpperAngle);
                DrawLine(branch.LowerLine, branch.LowerAngle);
            }

            // Use below instead of buttons, to make the currently hovered edge the jump-able,
            // and not the edge rendered first i.e. when two edges overlap.
            if (Event.current.type == EventType.MouseDown && isBranchHovered)
            {
                Vector2 mousePos = GetMousePosForHover();

                // Rotate mousePos so it seems like lower arrow tip is not rotated.
                Vector2 lowerLineEnd = branch.LowerLine.position;
                lowerLineEnd.y += (int)Mathf.Ceil(lineThickness / 2);
                lowerLineEnd += new Vector2(-5, 5);

                Vector2 angledMouseLower = BurstMath.AnglePoint(BurstMath.CalculateAngle(branch.LowerLine.position, lowerLineEnd),
                    mousePos, new Vector2(branch.LowerLine.x, branch.LowerLine.center.y));
                angledMouseLower.y -= (int)Mathf.Ceil(lineThickness / 2);

                // Rotate mousePos so it seems like upper arrow tip isn't rotated.
                Vector2 upperArrowTipPivot = branch.UpperLine.position;
                upperArrowTipPivot.y += 2 * (int)Mathf.Ceil(lineThickness / 2);

                Vector2 upperLineEnd = branch.UpperLine.position;
                upperLineEnd.y += (int)Mathf.Ceil(lineThickness / 2);
                upperLineEnd += new Vector2(-5, -5);

                Vector2 angleMouseUpper = BurstMath.AnglePoint(BurstMath.CalculateAngle(upperArrowTipPivot, upperLineEnd) - 360,
                    Event.current.mousePosition, new Vector2(branch.UpperLine.x, branch.UpperLine.center.y));
                angleMouseUpper.y += (int)Mathf.Ceil(lineThickness / 2);

                // Se if a jump should be made and jump.
                if (BurstMath.AdjustedContains(branch.StartHorizontal, mousePos))
                {
                    // make end arrow be at mouse cursor.
                    var target = branch.EndHorizontal;
                    target.y += branch.StartHorizontal.y < branch.EndHorizontal.y
                        ? (workingArea.yMax - mousePos.y)
                        : (workingArea.yMin - mousePos.y + highlightLineThickness / 2f);
                    GUI.ScrollTo(target);
                    Event.current.Use();
                }
                else if (BurstMath.AdjustedContains(branch.EndHorizontal, mousePos) || BurstMath.AdjustedContains(branch.LowerLine, angledMouseLower)
                    || BurstMath.AdjustedContains(branch.UpperLine, angleMouseUpper) || BurstMath.AdjustedContains(branch.VerticalLine, mousePos))
                {
                    var target = branch.StartHorizontal;
                    target.y += branch.StartHorizontal.y < branch.EndHorizontal.y
                        ? workingArea.yMin - mousePos.y + highlightLineThickness / 2
                        : workingArea.yMax - mousePos.y;
                    GUI.ScrollTo(target);
                    Event.current.Use();
                }
            }
        }

        private bool DrawFold(float x, float y, bool state, BurstDisassembler.AsmBlockKind kind)
        {
            var currentBg = GUI.backgroundColor;
            switch (kind)
            {
                case BurstDisassembler.AsmBlockKind.None:
                case BurstDisassembler.AsmBlockKind.Directive:
                case BurstDisassembler.AsmBlockKind.Block:
                    GUI.backgroundColor = Color.grey;
                    break;
                case BurstDisassembler.AsmBlockKind.Code:
                    GUI.backgroundColor = Color.green;
                    break;
                case BurstDisassembler.AsmBlockKind.Data:
                    GUI.backgroundColor = Color.magenta;
                    break;
            }

            var pressed = false;
            if (state)
            {
                pressed = GUI.Button(new Rect(x - fontWidth, y, fontWidth, fontHeight), "+");
            }
            else
            {
                pressed = GUI.Button(new Rect(x - fontWidth, y, fontWidth, fontHeight), "-");
            }

            GUI.backgroundColor = currentBg;

            return pressed;
        }

        internal void Layout(GUIStyle style, float hPad)
        {
            var oldFontHeight = fontHeight;
            var oldFontWidth = fontWidth;

            var cacheWidth0 = style.CalcSize(new GUIContent("W"));
            var cacheWidth1 = style.CalcSize(new GUIContent("WW"));

            var cacheHeight0 = style.CalcSize(new GUIContent("\n"));
            var cacheHeight1 = style.CalcSize(new GUIContent("\n\n"));

            fontHeight = cacheHeight1.y - cacheHeight0.y;
            fontWidth = cacheWidth1.x - cacheWidth0.x;

            _verticalPad = fontHeight * 0.5f;

            // oldFontWidth == 0 means we picked the first target after opening inspector.
            var diffX = oldFontWidth != 0 ? fontWidth / oldFontWidth : 0.0f;

            if (HasSelection && (oldFontWidth != fontWidth || oldFontHeight != fontHeight))
            {
                float diffY = fontHeight / oldFontHeight;

                // We only have to take padding into account for x-axis, as it's the only one with it.
                selectPos = new Vector2(((selectPos.x - hPad) * diffX) + hPad, diffY * selectPos.y);
                selectDragPos = new Vector2(((selectDragPos.x - hPad) * diffX) + hPad, diffY * selectDragPos.y);
            }

            invalidated = false;
            var oldFinalAreaSizeX = finalAreaSizeXRenderSize;
            finalAreaSizeXRenderSize = 0.0f;
            finalAreaSizeYInLines = 0;

            if (_disassembler == null)
            {
                LayoutPlain(style);
            }
            else
            {
                finalAreaSizeYInLines = _initialLineCount;
                finalAreaSizeXRenderSize = oldFinalAreaSizeX * diffX;
            }
        }

        /// <summary>
        /// Find accumulated fragment number the block start at.
        /// </summary>
        /// <param name="start">Block index to start search at. Should match <see cref="acc"/>.</param>
        /// <param name="acc">Accumulated fragment number that block <see cref="start"/> start at.</param>
        internal int GetFragNrFromBlockIdx(int blockIdx, int start, int acc, ref float positionY)
        {
            int fragNr = acc;
            int lPos = 0;

            for (int b = start; b < blockIdx; b++)
            {
                if (_folded[b])
                {
                    fragNr += GetPlainFragments(b).Count;
                    lPos++;
                }
                else
                {
                    foreach (var frag in GetPlainFragments(b))
                    {
                        fragNr++;
                        lPos += frag.lineCount;
                    }
                }
            }

            positionY += lPos * fontHeight;
            return fragNr;
        }

        internal bool SearchText(SearchCriteria searchCriteria, Regex regx, ref Rect workingArea, bool stopFirstMatch = false, bool focusTop = false) =>
            _disassembler == null
                ? SearchTextPlain(searchCriteria, regx, ref workingArea, stopFirstMatch, focusTop)
                : SearchTextEnhanced(searchCriteria, regx, ref workingArea, stopFirstMatch, focusTop);

        /// <summary>
        /// Searches <see cref="m_Text"/> for every match, and saves each match in <see cref="searchHits"/>.
        /// </summary>
        /// <param name="criteria">Search options.</param>
        /// <param name="regx">Used if <see cref="criteria"/> specifies a regex search.</param>
        /// <param name="workingArea">Inspector View.</param>
        /// <param name="stopFirstMatch">Whether search should stop at first match.</param>
        /// <param name="focusTop">Whether match focus should be at the top of view.</param>
        private bool SearchTextEnhanced(SearchCriteria criteria, Regex regx, ref Rect workingArea, bool stopFirstMatch = false, bool focusTop = false)
        {
            searchHits.Clear();
            _nrSearchHits = 0;
            _activeSearchHitIdx = -1;

            var matches = stopFirstMatch
                ? new List<(int, int)> { BurstStringSearch.FindMatch(m_Text, criteria, regx, 0) }
                : BurstStringSearch.FindAllMatches(m_Text, criteria, regx);

            var renderStartY = workingArea.y;

            bool doRepaint = false;

            float positionY = 0f;
            int accFragNr = 0;
            int accBlockNr = 0;
            int accIdx;
            foreach (var (idx, len) in matches)
            {
                if (len <= 0)
                {
                    continue;
                }
                int idxEnd = idx + len;

                // Find block + fragment match starts in:
                var blockStartInfo = _disassembler.GetBlockIdxFromTextIdx(idx, accBlockNr);
                // search hit within a folded block => unfold block.
                if (_folded[blockStartInfo.idx]) FoldUnfoldBlock(blockStartInfo.idx);
                // Finds the accumulated fragment number of the first fragment in block
                accFragNr = GetFragNrFromBlockIdx(blockStartInfo.idx, accBlockNr, accFragNr, ref positionY);

                int fragNrStart;
                float fragPositionY;
                // Finds actual accumulated fragment we just reached and it's index
                (fragNrStart, accIdx, fragPositionY) = GetFragNrFromEnhancedTextIdx(idx, blockStartInfo.idx, accFragNr, blockStartInfo.l, positionY);

                var frag1Text = GetPlainFragments(blockStartInfo.idx)[fragNrStart - accFragNr].text;

                // Make idx relative to fragment:
                int idxStartInFrag1 = idx - accIdx;
                int idxEndInFrag1  = Math.Min(idxStartInFrag1 + len, frag1Text.Length);

                // positionY should be stationed at top of fragment we have hit in.
                // Need to push it down to the specific line the hit is in. But keep this in a temporary variable so as to not fuck up the property of positionY!!!
                int exactLineNr = BurstStringSearch.FindLineNr(frag1Text, idxStartInFrag1);
                float exactPositionY = fragPositionY + exactLineNr * fontHeight;

                // Find block + fragment match ends in:
                var blockEndInfo = _disassembler.GetBlockIdxFromTextIdx(idxEnd, blockStartInfo.idx);
                // Finds the accumulated fragment number of match's last fragment
                accFragNr = GetFragNrFromBlockIdx(blockEndInfo.idx, blockStartInfo.idx, accFragNr, ref positionY);
                int fragNrEnd;
                // Finds the actual accumulated fragment number of match last fragment
                (fragNrEnd, accIdx, _) = GetFragNrFromEnhancedTextIdx(idxEnd, blockEndInfo.idx, accFragNr, blockEndInfo.l, positionY);

                accBlockNr = blockEndInfo.idx;

                if (!searchHits.ContainsKey(fragNrStart)) searchHits.Add(fragNrStart, new List<(int startIdx, int endIdx, int nr)>());
                searchHits[fragNrStart].Add((idxStartInFrag1, idxEndInFrag1, _nrSearchHits));

                if (fragNrStart < fragNrEnd)
                {
                    // match span multiple fragments
                    fragNrStart++; // Already captured this fragment

                    // For every fragment in-between first and last we capture the whole fragment
                    for (; fragNrStart < fragNrEnd; fragNrStart++)
                    {
                        if (!searchHits.ContainsKey(fragNrStart)) searchHits.Add(fragNrStart, new List<(int startIdx, int endIdx, int nr)>());
                        searchHits[fragNrStart].Add((0, int.MaxValue, _nrSearchHits)); // Max value to indicate it's the entire fragment
                    }
                    int idxStartInFragN = accIdx;
                    int idxEndInFragN = idxEnd - idxStartInFragN;

                    if (!searchHits.ContainsKey(fragNrStart)) searchHits.Add(fragNrStart, new List<(int startIdx, int endIdx, int nr)>());
                    searchHits[fragNrStart].Add((0, idxEndInFragN, _nrSearchHits));
                }

                if (_activeSearchHitIdx < 0 && exactPositionY >= renderStartY)
                {
                    _activeSearchHitIdx = _nrSearchHits;
                    doRepaint = SetScrollOnView(ref workingArea, frag1Text, exactLineNr, idxStartInFrag1, exactPositionY);
                }
                _nrSearchHits++;
            }
            return doRepaint;
        }

        private bool SetScrollOnView(ref Rect workingArea, string text, int lineNr, int matchIdx, float goToPosY)
        {
            // Find x position of search word:
            var startOfLine = lineNr > 0 ? GetEndIndexOfPlainLine(text, lineNr - 1).total + 1 : 0;
            float goToPosX = (matchIdx - startOfLine) * fontWidth + _actualHorizontalPad;

            // Set x position of view if search outside:
            bool leftOfView = workingArea.xMin > goToPosX;
            bool rightOfView = goToPosX > workingArea.xMax;
            workingArea.x = leftOfView
                ? goToPosX - 5 * fontWidth
                : rightOfView
                    ? goToPosX - workingArea.width + 5 * fontWidth
                    : workingArea.x;

            // Set y position of view if search outside:
            bool aboveView = workingArea.yMin > goToPosY;
            bool belowView = goToPosY > workingArea.yMax;
            workingArea.y = aboveView
                ? goToPosY - 5 * fontHeight
                : belowView
                    ? goToPosY - workingArea.height + 5*fontHeight
                    : workingArea.y;

            return aboveView || belowView || leftOfView || rightOfView;
        }

        /// <remarks>Works on non-colored text.</remarks>
        /// <param name="goal">Index we want fragment number from.</param>
        /// <param name="blockNrStart">Block to start search in.</param>
        /// <param name="fragNrStart">Accumulated fragment number of <see cref="blockNrStart"/>.</param>
        /// <param name="start">Start index of <see cref="blockNrStart"/>.</param>
        /// <returns>(accumulated fragment number, total index for returning fragment number)</returns>
        internal (int fragNr, int idx, float positionY) GetFragNrFromEnhancedTextIdx(int goal, int blockNrStart, int fragNrStart, int start, float positionY)
        {
            bool goThroughfrags(int goal, int blockIdx, ref int start, ref int f, ref float positionY)
            {
                var frags = GetPlainFragments(blockIdx);
                var lPos = 0;
                for (int i = 0; i < frags.Count; i++)
                {
                    Fragment frag = frags[i];
                    int len = frag.text.Length;
                    if (start + len >= goal)
                    {
                        positionY += lPos * fontHeight;
                        return true;
                    }
                    start += len + 1; // +1 because of newline
                    f++;
                    lPos+= frag.lineCount;
                }
                positionY += lPos * fontHeight;
                return false;
            }

            int f = fragNrStart;

            for (int b = blockNrStart; b < blocksFragmentsPlain.Length; b++)
            {
                if (goThroughfrags(goal, b, ref start, ref f, ref positionY)) break;
            }

            return (f, start, positionY);
        }

        /// <returns>(accumulated fragment number, total index for returning fragment number)</returns>
        private (int fragNr, int idx) GetFragNrFromPlainTextIdx(int goal, int fragNrStart, int start, ref float positionY)
        {
            int i = fragNrStart;
            var lPos = 0;
            for (; i < m_Fragments.Count; i++)
            {
                Fragment frag = m_Fragments[i];

                int len = frag.text.Length;
                if (start + len >= goal)
                {
                    break;
                }
                start += len + 1; // +1 because of newline
                lPos+=frag.lineCount;
            }

            positionY += lPos * fontHeight;
            return (i, start);
        }

        private bool SearchTextPlain(SearchCriteria criteria, Regex regx, ref Rect workingArea, bool stopFirstMatch = false, bool focusTop = false)
        {
            _nrSearchHits = 0;
            searchHits.Clear();
            _activeSearchHitIdx = -1;

            var matches = stopFirstMatch
                ? new List<(int, int)> { BurstStringSearch.FindMatch(m_Text, criteria, regx, 0) }
                : BurstStringSearch.FindAllMatches(m_Text, criteria, regx);

            bool doRepaint = false;

            float positionY = 0f;
            int accFragNr = 0;
            int accFragIdx = 0;
            var renderStartY = workingArea.y;
            foreach (var (idx, len) in matches)
            {
                if (len <= 0)
                {
                    continue;
                }
                int idxEnd = idx + len;

                // Find fragment match start in:
                (accFragNr, accFragIdx) = GetFragNrFromPlainTextIdx(idx, accFragNr, accFragIdx, ref positionY);
                int fragNrStart = accFragNr;

                string frag1Text = m_Fragments[fragNrStart].text;

                // Get relative index in fragment:
                int idxStartInFrag1 = idx - accFragIdx;
                int idxEndInFrag1 = Math.Min(idxStartInFrag1 + len, frag1Text.Length);

                int exactLineNr = BurstStringSearch.FindLineNr(frag1Text, idxStartInFrag1);
                float exactPositionY = positionY + exactLineNr * fontHeight;

                // Find fragment match end in:
                (accFragNr, accFragIdx) = GetFragNrFromPlainTextIdx(idxEnd, accFragNr, accFragIdx, ref positionY);
                int fragNrEnd = accFragNr;


                if (!searchHits.ContainsKey(fragNrStart)) searchHits.Add(fragNrStart, new List<(int startIdx, int endIdx, int nr)>());
                searchHits[fragNrStart].Add((idxStartInFrag1, idxEndInFrag1, _nrSearchHits));

                if (fragNrStart < fragNrEnd)
                {
                    // They must directly follow each other
                    int fragNr = fragNrStart + 1;
                    for (; fragNr < fragNrEnd; fragNr++)
                    {
                        if (!searchHits.ContainsKey(fragNr)) searchHits.Add(fragNr, new List<(int startIdx, int endIdx, int nr)>());
                        searchHits[fragNr].Add((0, m_Fragments[fragNr].text.Length, _nrSearchHits));
                    }

                    int idxStartInFrag2 = idxEnd - accFragIdx;
                    int idxEndInFrag2 = idxEnd - accFragIdx;

                    if (!searchHits.ContainsKey(fragNr)) searchHits.Add(fragNr, new List<(int startIdx, int endIdx, int nr)>());
                    searchHits[fragNr].Add((idxStartInFrag2, idxEndInFrag2, _nrSearchHits));
                }

                if (_activeSearchHitIdx < 0 && exactPositionY >= renderStartY)
                {
                    _activeSearchHitIdx = _nrSearchHits;
                    doRepaint = SetScrollOnView(ref workingArea, frag1Text, exactLineNr, idxStartInFrag1, exactPositionY);
                }
                _nrSearchHits++;
            }
            return doRepaint;
        }

        private void LayoutPlain(GUIStyle style)
        {
            foreach (var frag in m_Fragments)
            {
                // Calculate the size as we have hidden control codes in the string
                var size = style.CalcSize(new GUIContent(frag.text));
                finalAreaSizeXRenderSize = Math.Max(finalAreaSizeXRenderSize, size.x);
                finalAreaSizeYInLines += frag.lineCount;
            }
        }

        private void AddFoldedStringColorless(int blockIdx)
        {
            if (_blocksFragmentsStartPlain[blockIdx].lineCount != 0) return;

            string text = null;
            if (!_disassembler.IsColored)
            {
                _blocksFragments[blockIdx] ??= RecomputeFragmentsFromBlock(blockIdx);
                text = _blocksFragments[blockIdx][0].text;
            }
            else
            {
                text = GetPlainFragments(blockIdx)[0].text;
            }
            var endOfFirstLineIdx = text.IndexOf('\n');
            if (endOfFirstLineIdx == -1) endOfFirstLineIdx = text.Length;

            _blocksFragmentsStartPlain[blockIdx] = new Fragment() { lineCount = 1, text = text.Substring(0, endOfFirstLineIdx) + " ..." };
        }

        private void AddFoldedString(int blockIdx)
        {
            if (_blocksFragmentsStart[blockIdx].lineCount != 0) return;

            // precomputing every block to avoid having runtime check at every access later on
            _blocksFragments[blockIdx] ??= RecomputeFragmentsFromBlock(blockIdx);

            // precompute string to present for folded block.
            var text = _blocksFragments[blockIdx][0].text;

            var endOfFirstLineIdx = text.IndexOf('\n');
            if (endOfFirstLineIdx == -1) endOfFirstLineIdx = text.Length;

            _blocksFragmentsStart[blockIdx] = new Fragment() { lineCount = 1, text = text.Substring(0, endOfFirstLineIdx) + " ..." };
        }

        internal void LayoutEnhanced(GUIStyle style, Rect workingArea, bool showBranchMarkers)
        {
            Vector2 scrollPos = workingArea.position;
            if (showBranchMarkers != _oldShowBranchMarkers)
            {
                // need to alter selection according to padding on x-axis.
                if (showBranchMarkers)
                {
                    selectPos.x += (horizontalPad - naturalEnhancedPad);
                    selectDragPos.x += (horizontalPad - naturalEnhancedPad);
                }
                else
                {
                    selectPos.x -= (horizontalPad - naturalEnhancedPad);
                    selectDragPos.x -= (horizontalPad - naturalEnhancedPad);
                }
                // register cache needs to be cleared:
                _lineRegCache.Clear();
            }
            _oldShowBranchMarkers = showBranchMarkers;

            // Also computes the first and last blocks to render this time and ensures
            int lNum = 0;
            _renderBlockStart = -1;
            _renderBlockEnd = -1;

            _selectBlockStart = -1;
            _selectStartY = -1f;
            _selectBlockEnd = -1;
            _selectEndY = -1f;

            for (int idx = 0; idx<_disassembler.Blocks.Count; idx++)
            {
                var block = _disassembler.Blocks[idx];
                var lHeight = block.Length;

                if (_folded[idx])
                {
                    lHeight = 1;
                }

                blockLine[idx] = lNum;

                var blockHeight = lHeight * fontHeight;
                var lNumFontHeight = lNum * fontHeight;
                if (_selectBlockStart == -1 && selectPos.y - blockHeight <= lNumFontHeight)
                {
                    _selectBlockStart = idx;
                    _selectStartY = lNumFontHeight;
                }
                if (_selectBlockEnd == -1 && (selectDragPos.y - blockHeight <= lNumFontHeight || idx == _disassembler.Blocks.Count - 1))
                {
                    _selectBlockEnd = idx;
                    _selectEndY = lNumFontHeight;
                }

                // Whole block is above view, or block starts below view.
                // If at last block and _renderBlockStart == -1, we must have had all block above our scrollPos.
                // Happens when Scroll view is at bottom and fontSize is decreased. As this forces the scroll view upwards,
                // we simply set the last block as the one being rendered (avoids an indexOutOfBounds exception).
                if (((lNumFontHeight - scrollPos.y + blockHeight) < 0 || (lNumFontHeight - scrollPos.y) > workingArea.size.y)
                    && !(idx == _disassembler.Blocks.Count - 1 && _renderBlockStart == -1))
                {
                    lNum += lHeight;
                    continue;
                }

                if (_renderBlockStart == -1)
                {
                    _renderBlockStart = idx;
                    _renderStartY = lNumFontHeight;
                }

                _renderBlockEnd = idx;

                if (_blocksFragments[idx] == null)
                {
                    AddFoldedString(idx);

                    foreach (var fragment in _blocksFragments[idx])
                    {
                        style.CalcMinMaxWidth(new GUIContent(fragment.text), out _, out var maxWidth);
                        if (finalAreaSizeXRenderSize < maxWidth)
                        {
                            finalAreaSizeXRenderSize = maxWidth;
                        }
                    }
                }

                lNum += lHeight;
            }
            // selection is below last line of assembly.
            if (_selectBlockStart == -1)
            {
                _selectBlockStart = _disassembler.Blocks.Count - 1;
            }
        }

        internal string GetStartingColorTag(int blockIdx, int textIdx)
        {
            if (!CopyColorTags || !(_disassembler?.IsColored ?? false)) return "";

            const int colorTagLength = 15;

            var text = _folded[blockIdx]
                ? GetFragmentStart(blockIdx).text// _blocksFragmentsStart[blockIdx].text
                : _disassembler.GetOrRenderBlockToText(blockIdx);

            var colorTagIdx = text.LastIndexOf("<color=", textIdx);
            if (colorTagIdx == -1) return "";

            // Checking that found tas is actually for this idx
            return text.IndexOf("</color>", colorTagIdx, textIdx - colorTagIdx) == -1
                ? text.Substring(colorTagIdx, colorTagLength)
                : "";
        }

        internal string GetEndingColorTag(int blockIdx, int textIdx)
        {
            if (!CopyColorTags || !(_disassembler?.IsColored ?? false)) return "";

            var text = _folded[blockIdx]
                ? GetFragmentStart(blockIdx).text// _blocksFragmentsStart[blockIdx].text
                : _disassembler.GetOrRenderBlockToText(blockIdx);

            var endColorTagIdx = text.IndexOf("</color>", textIdx);
            if (endColorTagIdx == -1) return "";

            // Check that tag actually belongs for thid idx
            return text.IndexOf("<color=", textIdx, endColorTagIdx - textIdx) == -1
                ? "</color>"
                : "";
        }

        internal void ChangeCopyMode()
        {
            CopyColorTags = !CopyColorTags;

            // Need to update text selection fields as it should now ignore/use color tags
            _textSelectionIdxValid = false;
            UpdateEnhancedSelectTextIdx(_oldShowBranchMarkers ? horizontalPad : naturalEnhancedPad);
        }

        internal void DoSelectionCopy()
        {
            // textIdxE = -1 to indicate we want till the end of the block of text.
            string GetStringFromBlock(int blockIdx, int textIdxS, int textIdxE = -1)
            {
                string text;
                if (_folded[blockIdx])
                {
                    text = (CopyColorTags
                        ? GetFragmentStart(blockIdx).text
                        : GetFragmentStartPlain(blockIdx).text)
                        + '\n';

                    return textIdxE == -1
                        ? text.Substring(textIdxS, text.Length - textIdxS)
                        : text.Substring(textIdxS, textIdxE - textIdxS);
                }

                if (CopyColorTags)
                {
                    text = _disassembler.GetOrRenderBlockToText(blockIdx);
                    if (textIdxE == -1) textIdxE = text.Length;
                }
                else
                {
                    text = m_Text;

                    // Transform textIdxS and textIdxE to absolute index into m_Text
                    var (blockStart, blockEnd) = _disassembler.BlockIdxs[blockIdx];
                    textIdxS += blockStart;
                    if (textIdxE == -1) textIdxE = blockEnd + 1; // +1 to take newline as well
                    else                textIdxE += blockStart;
                }

                return text.Substring(textIdxS, textIdxE - textIdxS);
            }

            if (!HasSelection) return;

            var sb = new StringBuilder();

            if (_disassembler != null && enhancedTextSelectionIdxStart != enhancedTextSelectionIdxEnd)
            {
                if (enhancedTextSelectionIdxStart.blockIdx < enhancedTextSelectionIdxEnd.blockIdx)
                {
                    // Multiple block selection
                    var blockIdx = enhancedTextSelectionIdxStart.blockIdx;

                    sb.Append(
                        GetStartingColorTag(enhancedTextSelectionIdxStart.blockIdx, enhancedTextSelectionIdxStart.textIdx) +
                        GetStringFromBlock(blockIdx++, enhancedTextSelectionIdxStart.textIdx));

                    for (; blockIdx < enhancedTextSelectionIdxEnd.blockIdx; blockIdx++)
                    {
                        sb.Append(GetStringFromBlock(blockIdx, 0));
                    }

                    sb.Append(
                        GetStringFromBlock(blockIdx, 0, enhancedTextSelectionIdxEnd.textIdx) +
                        GetEndingColorTag(enhancedTextSelectionIdxEnd.blockIdx, enhancedTextSelectionIdxEnd.textIdx));
                }
                else
                {
                    // Single block selection
                    sb.Append(
                        GetStartingColorTag(enhancedTextSelectionIdxStart.blockIdx, enhancedTextSelectionIdxStart.textIdx) +
                        GetStringFromBlock(
                            enhancedTextSelectionIdxStart.blockIdx,
                            enhancedTextSelectionIdxStart.textIdx,
                            enhancedTextSelectionIdxEnd.textIdx) +
                        GetEndingColorTag(enhancedTextSelectionIdxEnd.blockIdx, enhancedTextSelectionIdxEnd.textIdx));
                }
            }
            else
            {
                sb.Append(m_Text.Substring(textSelectionIdx.idx, textSelectionIdx.length));
            }
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }

        internal void SelectAll()
        {
            HasSelection = true;
            selectPos = Vector2.zero;
            selectDragPos = new Vector2(finalAreaSizeXRenderSize, finalAreaSizeYInLines * fontHeight);

            if (_disassembler != null)
            {
                enhancedTextSelectionIdxStart = (0, 0);
                var blockIdx = _disassembler.Blocks.Count - 1;

                enhancedTextSelectionIdxEnd.blockIdx = blockIdx;
                // Make sure selection works on appropriate text:
                enhancedTextSelectionIdxEnd.textIdx = _folded[blockIdx]
                    ? CopyColorTags
                        ? GetFragmentStart(blockIdx).text.Length
                        : GetFragmentStartPlain(blockIdx).text.Length
                    : CopyColorTags
                        ? _disassembler.GetOrRenderBlockToText(blockIdx).Length
                        : _disassembler.BlockIdxs[blockIdx].endIdx - _disassembler.BlockIdxs[blockIdx].startIdx;

                _selectBlockStart = 0;
                _selectBlockEnd = blockIdx;
                _selectEndY = (finalAreaSizeYInLines * fontHeight) -
                              (_folded[blockIdx] ? fontHeight : _disassembler.Blocks[blockIdx].Length * fontHeight);
            }
            else
            {
                textSelectionIdx = (0, m_Text.Length);
            }
            _textSelectionIdxValid = true;
        }

        private void ScrollDownToSelection(Rect workingArea)
        {
            if (!workingArea.Contains(selectDragPos + new Vector2(fontWidth, fontHeight / 2)))
            {
                GUI.ScrollTo(new Rect(selectDragPos + new Vector2(fontWidth, fontHeight), Vector2.zero));
            }

            _textSelectionIdxValid = false;
        }

        private void ScrollUpToSelection(Rect workingArea)
        {
            if (!workingArea.Contains(selectDragPos - new Vector2(0, fontHeight / 2)))
            {
                GUI.ScrollTo(new Rect(selectDragPos - new Vector2(0, fontHeight), Vector2.zero));
            }

            _textSelectionIdxValid = false;
        }


        internal void MoveSelectionLeft(Rect workingArea, bool showBranchMarkers)
        {
            HasSelection = true;
            if (_disassembler != null)
            {
                float hPad = showBranchMarkers ? horizontalPad : naturalEnhancedPad;
                string text;
                int prevLineIdx = Mathf.FloorToInt((selectDragPos.y - _selectEndY) / fontHeight) - 1;

                if (selectDragPos.x < hPad + fontWidth)
                {
                    if (prevLineIdx < 0 && _selectBlockEnd == 0)
                    {
                        // we are at the beginning of the text.
                        return;
                    }

                    if (prevLineIdx < 0 && _selectBlockEnd > 0)
                    {
                        // get text from previous block, and do calculations for that.
                        _selectBlockEnd--;
                        _selectEndY -= _folded[_selectBlockEnd]
                            ? fontHeight
                            : _disassembler.Blocks[_selectBlockEnd].Length * fontHeight;

                        (text, prevLineIdx) = !_folded[_selectBlockEnd]
                            ? (_disassembler.GetOrRenderBlockToText(_selectBlockEnd), _disassembler.Blocks[_selectBlockEnd].Length - 1)
                            : (GetFragmentStart(_selectBlockEnd).text + '\n', 0);
                    }
                    else
                    {
                        text = _folded[_selectBlockEnd]
                            ? GetFragmentStart(_selectBlockEnd).text + '\n'
                            : _disassembler.GetOrRenderBlockToText(_selectBlockEnd);
                    }
                    var charsInLine = GetEndIndexOfColoredLine(text, prevLineIdx).relative;

                    selectDragPos.x = charsInLine * fontWidth + hPad + fontWidth / 2;
                    selectDragPos.y -= fontHeight;
                }
                else
                {
                    // simply move selection left.
                    text = _folded[_selectBlockEnd]
                        ? GetFragmentStart(_selectBlockEnd).text + '\n'
                        : _disassembler.GetOrRenderBlockToText(_selectBlockEnd);
                    var charsInLine = GetEndIndexOfColoredLine(text, prevLineIdx + 1).relative;

                    if (selectDragPos.x > charsInLine * fontWidth + hPad)
                        selectDragPos.x = hPad + charsInLine * fontWidth + fontWidth / 2;

                    selectDragPos.x -= fontWidth;
                }
            }
            else
            {
                int prevLineIdx = Mathf.FloorToInt((selectDragPos.y) / fontHeight) - 1;

                if (selectDragPos.x < fontWidth)
                {
                    if (prevLineIdx < 0)
                    {
                        // we are at beginning of the text
                        return;
                    }

                    int charsInLine = GetEndIndexOfPlainLine(m_Text, prevLineIdx).relative;

                    selectDragPos.x = charsInLine * fontWidth + fontWidth / 2;
                    selectDragPos.y -= fontHeight;
                }
                else
                {
                    // simply move selection left.
                    int charsInLine = GetEndIndexOfPlainLine(m_Text, prevLineIdx+1).relative;
                    if (selectDragPos.x > charsInLine * fontWidth)
                    {
                        selectDragPos.x = charsInLine * fontWidth + fontWidth / 2;
                    }

                    selectDragPos.x -= fontWidth;
                }
            }

            // check if we moved outside of view and scroll if true.
            ScrollUpToSelection(workingArea);
        }

        internal void MoveSelectionRight(Rect workingArea, bool showBranchMarkers)
        {
            HasSelection = true;
            if (_disassembler != null)
            {
                var hPad = showBranchMarkers ? horizontalPad : naturalEnhancedPad;

                var text     = _folded[_selectBlockEnd]
                    ? GetFragmentStart(_selectBlockEnd).text + '\n'
                    : _disassembler.GetOrRenderBlockToText(_selectBlockEnd);

                var thisLine    = Mathf.FloorToInt((selectDragPos.y - _selectEndY) / fontHeight);
                var charsInLine = GetEndIndexOfColoredLine(text, thisLine).relative;

                if (selectDragPos.x >= hPad + charsInLine * fontWidth)
                {
                    // move down a line:
                    thisLine++;

                    int lineCount = _folded[_selectBlockEnd] ? 1 : _disassembler.Blocks[_selectBlockEnd].Length;

                    if (thisLine > lineCount && _selectBlockEnd == _disassembler.Blocks.Count - 1)
                    {
                        // We are at the end of the text
                        return;
                    }

                    if (thisLine > lineCount)
                    {
                        // selected into next block.
                        _selectBlockEnd++;
                        _selectEndY += _folded[_selectBlockEnd - 1]
                            ? fontHeight
                            : _disassembler.Blocks[_selectBlockEnd - 1].Length * fontHeight;
                    }

                    selectDragPos.x = hPad + fontWidth/2;
                    selectDragPos.y += fontHeight;
                }
                else
                {
                    // simply move selection right.
                    if (selectDragPos.x < hPad)
                    {
                        selectDragPos.x = hPad + fontWidth / 2;
                    }

                    selectDragPos.x += fontWidth;
                }
            }
            else
            {
                int thisLine = Mathf.FloorToInt((selectDragPos.y) / fontHeight);

                int charsInLine = GetEndIndexOfColoredLine(m_Text, thisLine).relative;

                if (selectDragPos.x >= charsInLine*fontWidth)
                {
                    thisLine++;

                    if (thisLine >= _mTextLines)
                    {
                        // we are at end of the text
                        return;
                    }

                    selectDragPos.x = 0f;
                    selectDragPos.y += fontHeight;

                }
                else
                {
                    // simply move selection right.
                    selectDragPos.x += fontWidth;
                }
            }
            // check if we moved outside of view and scroll if true.
            ScrollDownToSelection(workingArea);
        }

        internal void MoveSelectionUp(Rect workingArea, bool showBranchMarkers)
        {
            HasSelection = true;
            if (_disassembler != null)
            {
                int thisLine = Mathf.FloorToInt((selectDragPos.y - _selectEndY) / fontHeight);

                // At the first line
                if (thisLine == 0 && _selectBlockEnd == 0) return;

                if (thisLine == 0)
                {
                    // We are moving on to a block above
                    _selectBlockEnd--;
                    _selectEndY -= _folded[_selectBlockEnd]
                        ? fontHeight
                        : _disassembler.Blocks[_selectBlockEnd].Length * fontHeight;
                }
            }
            else
            {
                int thisLine = Mathf.FloorToInt((selectDragPos.y) / fontHeight) - 1;

                if (thisLine < 0) return;
            }

            selectDragPos.y -= fontHeight;

            // check if we moved outside of view and scroll if true.
            ScrollUpToSelection(workingArea);
        }

        internal void MoveSelectionDown(Rect workingArea, bool showBranchMarkers)
        {
            HasSelection = true;
            if (_disassembler != null)
            {
                int thisLine = Mathf.FloorToInt((selectDragPos.y - _selectEndY) / fontHeight) + 1;

                int lineCount = _folded[_selectBlockEnd] ? 0 : _disassembler.Blocks[_selectBlockEnd].Length - 1;

                if (thisLine > lineCount)
                {
                    // At the last line
                    if (_selectBlockEnd == _disassembler.Blocks.Count - 1) { return; }

                    // Moved into next block
                    _selectBlockEnd++;
                    _selectEndY += _folded[_selectBlockEnd - 1]
                        ? fontHeight
                        : _disassembler.Blocks[_selectBlockEnd - 1].Length * fontHeight;
                }
            }
            else
            {
                int thisLine = Mathf.FloorToInt(selectDragPos.y / fontHeight) + 1;

                if (thisLine > _mTextLines) return;
            }

            selectDragPos.y += fontHeight;

            // check if we moved outside of view and scroll if true.
            ScrollDownToSelection(workingArea);
        }

        internal bool MouseOutsideView(Rect workingArea, Vector2 mousePos, int controlID)
        {
            if (!workingArea.Contains(mousePos))
            {
                if (GUIUtility.hotControl == controlID && Event.current.rawType == EventType.MouseUp)
                {
                    _mouseDown = false;
                }

                _mouseOutsideBounds = true;
                return true;
            }

            _mouseOutsideBounds = false;
            return false;
        }

        private Vector2 MouseClickAndClamp(bool showBranchMarkers, Vector2 mousePos)
        {
            HasSelection = true;
            if (_disassembler != null)
            {
                float hPad = showBranchMarkers ? horizontalPad : naturalEnhancedPad;

                // Make sure we cannot set cursor in the horizontal padding:
                if (mousePos.x < hPad) mousePos.x = hPad;
            }
            return mousePos;
        }

        private int GetLineNumber(Vector2 mousePos)
        {
            var lineNumber = Mathf.FloorToInt(mousePos.y / fontHeight);
            var len = _disassembler.Blocks.Count;
            for (var idx = 0; idx < len; idx++)
            {
                var block = _disassembler.Blocks[idx];
                if (lineNumber <= block.LineIndex)
                {
                    break;
                }
                if (_folded[idx])
                {
                    lineNumber += block.Length - 1;
                }
            }
            return lineNumber;
        }
        internal void MouseClicked(bool showBranchMarkers, bool withShift, Vector2 mousePos, int controlID)
        {
            if (_mouseOutsideBounds || mousePos.y > (finalAreaSizeYInLines * fontHeight)) { return; }

            GUIUtility.hotControl = controlID;
            // FocusControl is to take keyboard focus away from the TreeView.
            GUI.FocusControl("long text");
            if (withShift)
            {
                mousePos = MouseClickAndClamp(showBranchMarkers, mousePos);

                selectDragPos = mousePos;
                _textSelectionIdxValid = false;
            }
            else
            {
                selectDragPos = mousePos;
                StopSelection();
            }
            _mouseDown = true;
        }


        internal void DragMouse(Vector2 mousePos, bool showBranchMarkers)
        {
            if (!_mouseDown || mousePos.y > (finalAreaSizeYInLines * fontHeight)) { return; }

            mousePos = MouseClickAndClamp(showBranchMarkers, mousePos);

            selectDragPos = mousePos;
            _textSelectionIdxValid = false;
        }

        internal void MouseReleased()
        {
            GUIUtility.hotControl = 0;
            _mouseDown = false;
        }

        internal void DoScroll(Rect workingArea, float mouseRelMoveY)
        {
            if (!_mouseDown) { return; }

            var movingDown = mouseRelMoveY > 0;

            var room = Mathf.Max(0, movingDown
                ? (finalAreaSizeYInLines * fontHeight) - workingArea.yMax
                : workingArea.yMin);

            // naturalEnhancedPad magic number taken from unity engine (GUI.cs under EndScrollView).
            var dist = Mathf.Min(Mathf.Abs(mouseRelMoveY * naturalEnhancedPad), room);
            selectDragPos.y += movingDown ? dist : -dist;

            _textSelectionIdxValid = false;
        }

        public void MoveView(Direction dir, Rect workingArea)
        {
            switch (dir)
            {
                case Direction.Left:
                    GUI.ScrollTo(new Rect(workingArea.xMin - 2*fontWidth, workingArea.y, 0, 0));
                    break;

                case Direction.Right:
                    GUI.ScrollTo(new Rect(workingArea.xMax + 2*fontWidth, workingArea.y, 0, 0));
                    break;

                case Direction.Up:
                    GUI.ScrollTo(new Rect(workingArea.x, workingArea.yMin - 2*fontWidth, 0, 0));
                    break;

                case Direction.Down:
                    GUI.ScrollTo(new Rect(workingArea.x, workingArea.yMax + 2*fontWidth, 0, 0));
                    break;
            }
        }

        public void StopSelection()
        {
            selectPos = selectDragPos;
            textSelectionIdx = (0, 0);
            _textSelectionIdxValid = true;
            HasSelection = false;
        }



        /// <summary>
        /// Renders a blue box relative to text at (positionX, positionY) from start idx to end idx.
        /// </summary>
        private void RenderLineSelection(float positionX, float positionY, int start, int end, bool IsSelection = true)
        {
            const int alignmentPad = 2;
            var oldColor = GUI.color;
            GUI.color = IsSelection ? _selectionColor : _searchHitColor;
            GUI.Box(new Rect(positionX + alignmentPad + start*fontWidth, positionY, (end - start)*fontWidth, fontHeight), "", textureStyle);
            GUI.color = oldColor;
        }

        internal struct FragmentSelectionInfo
        {
            public float startY;
            public float lastY;
            public float botY;

            public int startLineEndIdxRel;
            public int startLine;
            public int lastLine;

            public int charsIn;
            public int charsInDrag;
        }

        internal FragmentSelectionInfo PrepareInfoForSelection(float positionX, float positionY, float fragHeight, Fragment frag, Func<string, int, (int total, int relative)> GetEndIndexOfLine)
        {
            const float distanceFromBorder = 0.001f;
            var text = frag.text;

            var top = selectPos;
            var bot = selectDragPos;
            if (selectPos.y > selectDragPos.y)
            {
                top = selectDragPos;
                bot = selectPos;
            }
            // fixing so we only look at things within this current fragment.
            var start = top.y < positionY ? new Vector2(positionX, positionY) : top;
            // `y = ... - adjustBottomSlightly` so it is within current fragments outer "border".
            var last = bot.y < positionY + fragHeight ? bot : new Vector2(finalAreaSizeXRenderSize, positionY + fragHeight - distanceFromBorder);

            // Make sure this cannot go beyond number of lines in fragment (zero indexed).
            var startLine = Math.Min(Mathf.FloorToInt((start.y - positionY) / fontHeight), frag.lineCount-1);
            var lastLine = Math.Min(Mathf.FloorToInt((last.y - positionY) / fontHeight), frag.lineCount-1);

            if (startLine == lastLine && start.x > last.x)
                (start, last) = (last, start);

            // Used for making sure charsIn and charsInDrag does not exceed line length.
            var (_, startLineEndIdxRel) = GetEndIndexOfLine(text, startLine);
            var (_, lastLineEndIdxRel) = GetEndIndexOfLine(text, lastLine);

            // Each fragment does not end on a newline, so we need to add one to idx.
            if (startLine == frag.lineCount - 1) startLineEndIdxRel++;
            if (lastLine == frag.lineCount - 1) lastLineEndIdxRel++;

            var charsIn = Math.Min(Mathf.FloorToInt((start.x - positionX) / fontWidth), startLineEndIdxRel);
            var charsInDrag = Math.Min(Mathf.FloorToInt((last.x - positionX) / fontWidth), lastLineEndIdxRel);
            charsIn = charsIn < 0 ? 0 : charsIn;
            charsInDrag = charsInDrag < 0 ? 0 : charsInDrag;

            return new FragmentSelectionInfo() {startY = start.y, lastY = last.y, botY = bot.y,
                startLineEndIdxRel = startLineEndIdxRel, startLine = startLine, lastLine = lastLine,
                charsIn = charsIn, charsInDrag = charsInDrag};
        }

        private void SelectText(float positionX, float positionY, float fragHeight, Fragment frag, Func<string, int, (int total, int relative)> GetEndIndexOfLine)
        {
            if (!HasSelection || !(BurstMath.WithinRange(selectPos.y, selectDragPos.y, positionY) ||
                                 BurstMath.WithinRange(selectPos.y, selectDragPos.y, positionY + fragHeight)
                                 || BurstMath.WithinRange(positionY, positionY + fragHeight, selectPos.y)))
            {
                return;
            }

            var inf = PrepareInfoForSelection(positionX, positionY, fragHeight, frag, GetEndIndexOfLine);
            var text = frag.text;

            if (BurstMath.RoundDownToNearest(inf.lastY, fontHeight) > BurstMath.RoundDownToNearest(inf.startY, fontHeight))
            {
                // Multi line selection in this text.
                int lineEndIdx = inf.startLineEndIdxRel;
                if (inf.startY != positionY)
                {
                    // Selection started in this fragment.
                    RenderLineSelection(positionX, positionY + (inf.startLine * fontHeight), inf.charsIn, lineEndIdx + 1);
                    inf.startLine++;
                }

                for (; inf.startLine < inf.lastLine; inf.startLine++)
                {
                    lineEndIdx = GetEndIndexOfLine(text, inf.startLine).relative;
                    RenderLineSelection(positionX, positionY + (inf.startLine * fontHeight), 0, lineEndIdx + 1);
                }

                if (positionY + fragHeight < inf.botY)
                {
                    // select going into next fragment
                    inf.charsInDrag++;
                }

                RenderLineSelection(positionX, positionY + (inf.startLine * fontHeight), 0, inf.charsInDrag);
            }
            else
            {
                // Single line selection in this text segment.
                int startIdx = inf.charsIn;
                int endIdx = inf.charsInDrag;
                if (inf.startY == positionY)
                {
                    // Selection started in text segment above.
                    startIdx = 0;
                }

                if (positionY + fragHeight < inf.botY)
                {
                    // Selection going into next text segment.
                    endIdx = inf.startLineEndIdxRel + 1;
                }

                RenderLineSelection(positionX, positionY + (inf.startLine * fontHeight), startIdx, endIdx);
            }
        }

        /// <summary>
        /// Updates _textSelectionIdx based on the position of _selectPos and _selectDragPos.
        /// </summary>
        /// <param name="GetEndIndexOfLine"> either  GetEndIndexOfPlainLine or GetEndIndexOfColoredLine</param>
        private void UpdateSelectTextIdx(Func<string, int, (int total, int relative)> GetEndIndexOfLine)
        {
            if (!_textSelectionIdxValid && HasSelection)
            {
                var start = selectPos;
                var last = selectDragPos;
                if (last.y < start.y)
                {
                    start = selectDragPos;
                    last = selectPos;
                }
                int startLine   = Mathf.FloorToInt(start.y / fontHeight);
                int lastLine    = Mathf.FloorToInt(last.y / fontHeight);

                if (startLine == lastLine && start.x > last.x)
                {
                    (start, last) = (last, start);
                }

                var (startLineEndIdxTotal, startLineEndIdxRel) = GetEndIndexOfLine(m_Text, startLine);
                var (lastLineEndIdxTotal, lastLineEndIdxRel) = GetEndIndexOfLine(m_Text, lastLine);

                int charsIn = Math.Min(Mathf.FloorToInt(start.x / fontWidth), startLineEndIdxRel);
                int charsInDrag = Math.Min(Mathf.FloorToInt(last.x / fontWidth), lastLineEndIdxRel);
                charsIn = charsIn < 0 ? 0 : charsIn;
                charsInDrag = charsInDrag < 0 ? 0 : charsInDrag;

                int selectStartIdx = startLineEndIdxTotal - (startLineEndIdxRel - charsIn);
                textSelectionIdx = (selectStartIdx, lastLineEndIdxTotal - (lastLineEndIdxRel - charsInDrag) - selectStartIdx);

                _textSelectionIdxValid = true;
            }
        }

        public void NextSearchHit(bool shift, Rect workingArea)
        {
            if (_nrSearchHits <= 0) return;

            // If shift is held down: Go backwards through active search
            _activeSearchHitIdx = shift
                ? (Math.Max(_activeSearchHitIdx, 0) - 1 + _nrSearchHits) % _nrSearchHits
                : (_activeSearchHitIdx + 1) % _nrSearchHits;

            // Find fragment number of _activeSearchHit
            var fragIdx = 0;
            int prevFragLastHitNr = -1;
            var activeSearchIdx = _activeSearchHitIdx;
            foreach (var key in searchHits.Keys)
            {
                var hits = searchHits[key];

                var nrHits = hits.Count;
                var hitsLastNr = hits[nrHits - 1].nr;

                // If last hit continues into this fragment we subtracted too much
                if (hits[0].nr == prevFragLastHitNr) { activeSearchIdx++; }

                if (hitsLastNr >= _activeSearchHitIdx)
                {
                    fragIdx = key;
                    break;
                }
                activeSearchIdx -= nrHits;
                prevFragLastHitNr = hitsLastNr;
            }

            var hit = searchHits[fragIdx][activeSearchIdx];

            // Find y position of _activeSearchHit.
            int exactLineNumber;
            string text;
            var lPos = 0;
            if (_disassembler == null)
            {
                int i = 0;
                for (; i < fragIdx; i++)
                {
                    lPos += m_Fragments[i].lineCount;
                }
                text = m_Fragments[i].text;
                exactLineNumber = BurstStringSearch.FindLineNr(text, hit.startIdx);
                lPos+= exactLineNumber;
            }
            else
            {
                int blockIdx = 0;
                int innerFragIdx = 0;
                int tmpFragIdx = 0;

                for (; tmpFragIdx < fragIdx; blockIdx++)
                {
                    var frags = GetPlainFragments(blockIdx);

                    if (_folded[blockIdx])
                    {
                        lPos++;
                        tmpFragIdx += frags.Count;
                    }
                    else
                    {
                        for (innerFragIdx = 0; ; innerFragIdx++, tmpFragIdx++)
                        {
                            if (innerFragIdx >= frags.Count)
                            {
                                innerFragIdx = 0;
                                break;
                            }
                            if (tmpFragIdx >= fragIdx) break;
                            lPos+= frags[innerFragIdx].lineCount;
                        }
                    }
                    if (innerFragIdx != 0) break;
                }
                text = GetPlainFragments(blockIdx)[innerFragIdx].text;
                exactLineNumber = BurstStringSearch.FindLineNr(text, hit.startIdx);
                lPos+= exactLineNumber;
            }

            int startOfLine = exactLineNumber > 0 ? GetEndIndexOfPlainLine(text, exactLineNumber-1).total + 1 : 0;
            int hitLength = hit.endIdx - hit.startIdx;

            float positionX = hit.startIdx - startOfLine;
            if (positionX * fontWidth > workingArea.x) positionX += hitLength + 3 * fontWidth;
            else positionX -= 3 * fontWidth;

            var positionY = lPos * fontHeight;
            GUI.ScrollTo(new Rect(positionX * fontWidth + _actualHorizontalPad, positionY < workingArea.yMin ? positionY - 5 * fontHeight : positionY + 5 * fontHeight, 0, 0));
        }

        public void StopSearching()
        {
            _nrSearchHits = 0;
            searchHits.Clear();
            _activeSearchHitIdx = -1;
            _prevCriteria.filter = String.Empty;
        }

        private void RenderMultipleLinesSelection(string text, float positionX, float positionY, int linePosition, int startIdx, int endIdx, bool IsSelection = true)
        {
            int lineStart = BurstStringSearch.FindLineNr(text, startIdx);
            int lineEnd   = BurstStringSearch.FindLineNr(text, endIdx);

            // Make idx relative to line
            int startOfLine = lineStart == 0 ? 0 : GetEndIndexOfPlainLine(text, lineStart-1).total + 1;
            startIdx        = startIdx - startOfLine;

            startOfLine = lineEnd == 0 ? 0 : GetEndIndexOfPlainLine(text, lineEnd-1).total + 1;
            endIdx      = endIdx - startOfLine;

            linePosition += lineStart;

            if (lineStart == lineEnd)
            {
                RenderLineSelection(positionX, positionY + linePosition*fontHeight, startIdx, endIdx, IsSelection);
            }
            else
            {
                int endOfLine = GetEndIndexOfPlainLine(text, lineStart).relative;
                RenderLineSelection(positionX, positionY + linePosition*fontHeight, startIdx, endOfLine, IsSelection);
                lineStart++;
                linePosition++;

                for (; lineStart < lineEnd; lineStart++)
                {
                    endOfLine = GetEndIndexOfPlainLine(text, lineStart).relative;
                    RenderLineSelection(positionX, positionY + linePosition * fontHeight, 0, endOfLine, IsSelection);
                    linePosition++;
                }

                RenderLineSelection(positionX, positionY + linePosition * fontHeight, 0, endIdx, IsSelection);
            }
        }

        private void RenderPlainOldFragments(GUIStyle style, Rect workingArea, bool doSearch)
        {
            Vector2 scrollPos = workingArea.position;
            int lNum = 0;
            for (int i = 0; i < m_Fragments.Count; i++)
            {
                // if fragment is below view
                var pYCheck= (lNum * fontHeight) - scrollPos.y;
                if (pYCheck > workingArea.size.y) break;

                var fragment = m_Fragments[i];

                float fragHeight = fragment.lineCount * fontHeight;

                // if whole fragment is above view
                if ((pYCheck + fragHeight) < 0)
                {
                    lNum += fragment.lineCount;
                    continue;
                }

                if (doSearch && searchHits.TryGetValue(i, out var lst))
                {
                    foreach (var (startI, endI, nr) in lst)
                    {
                        bool isActive = nr == _activeSearchHitIdx;
                        RenderMultipleLinesSelection(fragment.text, horizontalPad, 0.0f, lNum, startI, endI, isActive);
                    }
                }
                // we append \n here, as we want it for the selection but not the rendering.
                SelectText(horizontalPad, lNum*fontHeight, fragHeight, fragment, GetEndIndexOfPlainLine);

                GUI.Label(new Rect(horizontalPad, lNum*fontHeight, finalAreaSizeXRenderSize, fragHeight + fontHeight*0.5f), fragment.text, style);
                lNum += fragment.lineCount;
            }
            UpdateSelectTextIdx(GetEndIndexOfPlainLine);
        }

        /// <returns>Whether the GUI should be forced to repaint during this frame.</returns>
        public bool Render(GUIStyle style, Rect workingArea, bool showBranchMarkers, bool doSearch, SearchCriteria searchCriteria, Regex regx)
        {
            _actualHorizontalPad = showBranchMarkers ? horizontalPad : naturalEnhancedPad;
            style.richText = true;

            if (invalidated)
            {
                Layout(style, _actualHorizontalPad);
            }

            if (_disassembler != null)
            {
                // We always need to call this as its sets up the correct horizontal bar and block rendering
                LayoutEnhanced(style, workingArea, showBranchMarkers);
            }

            if (Event.current.type == EventType.Layout)
            {
                // working area will be valid only during repaint, for the layout event we don't draw the labels
                // Add correct size Rect to GUILayoutUtillity.topLevel for retrieval with next GetRect(.) call.
                GUILayoutUtility.GetRect(finalAreaSizeXRenderSize + (showBranchMarkers ? horizontalPad : 20.0f), finalAreaSizeYInLines*fontHeight);
                return false;
            }

            var newSearch = doSearch && (searchCriteria.filter != "" && !searchCriteria.Equals(_prevCriteria));
            _prevCriteria = searchCriteria;

            var doRepaint = false;
            if (newSearch)
            {
                doRepaint = SearchText(searchCriteria, regx, ref workingArea);
                if (doRepaint)
                {
                    // Search found at least one match, so move to the first
                    GUI.ScrollTo(workingArea);
                }
            }

            if (_disassembler == null)
            {
                RenderPlainOldFragments(style, workingArea, doSearch);
            }
            else
            {
                RenderEnhanced(style, workingArea, showBranchMarkers, _actualHorizontalPad, doSearch);
                RenderRegisterHighlight(workingArea, _actualHorizontalPad);
                RenderLineHighlight(_actualHorizontalPad);
            }

            DrawHover(style, workingArea);
            return doRepaint;
        }

        internal int GetLinesBlockIdx(int pressedLine)
        {
            // Find block that was pressed:
            var b = _renderBlockStart;
            var numBlocks = _blocksFragments.Length;
            for (; b <= _renderBlockEnd && b < numBlocks-1; b++)
            {
                var block = _disassembler.Blocks[b];
                if (block.LineIndex <= pressedLine && block.LineIndex + block.Length > pressedLine)
                {
                    break;
                }
            }

            return b;
        }

        internal Rect GetLineHighlight(ref LineRegRectsCache cache, float hPad, int pressedLine)
        {
            // Find block that was pressed:
            var b = GetLinesBlockIdx(pressedLine);

            if (cache.IsLineHighlightCached(pressedLine, _folded[b]))
            {
                // Same old line
                return cache.lineHighlight;
            }

            var pressedLineRel = pressedLine - _disassembler.Blocks[b].LineIndex;
            var positionY =
                    _renderStartY + (blockLine[b] - blockLine[_renderBlockStart] + pressedLineRel) * fontHeight;
            const float lineHeight = 2f;

            // Cannot use tokens in disassembler, as it's not updated with the proper output created by
            // RenderLine(.).
            var line = _disassembler.Lines[pressedLine];
            var lineStr = _folded[b] ? GetFragmentStartPlain(b).text : GetLineString(line);
            var xEnd = lineStr.Length * fontWidth;
            var xStart = hPad;
            var yPos = positionY + fontHeight;
            var rect = new Rect(xStart, yPos, xEnd, lineHeight);
            cache.UpdateLineHighlight(pressedLine, rect, _folded[b]);
            return rect;
        }

        private void RenderLineHighlight(float hPad)
        {
            if (_pressedLine == -1)
            {
                return;
            }
            var firstRenderedLine = _disassembler.Blocks[_renderBlockStart].LineIndex;
            var tmp = _disassembler.Blocks[_renderBlockEnd];
            var lastRenderedLine = tmp.LineIndex + tmp.Length;
            if (!BurstMath.WithinRange(firstRenderedLine, lastRenderedLine, _pressedLine))
            {
                return;
            }

            var col = GUI.color;
            GUI.color = _lineHighlightColor;
            GUI.Box(GetLineHighlight(ref _lineRegCache, hPad, _pressedLine), "", textureStyle);
            GUI.color = col;
        }

        internal LineRegRectsCache _lineRegCache;
        internal int _pressedLine = -1;

        private void RenderRegisterHighlight(Rect workingArea, float hPad)
        {
            if (_pressedLine == -1)
            {
                return;
            }

            if (_disassembler.LineUsesRegs(_pressedLine, out var usedRegs) && _disassembler.Lines[_pressedLine].Kind != BurstDisassembler.AsmLineKind.Directive)
            {
                var oldColor = GUI.color;
                var regRects = GetRegisterRects(hPad, ref _lineRegCache, _pressedLine, usedRegs);

                var i = 0;
                foreach (var rects in regRects)
                {
                    GUI.color = _regsColourWheel[i % _regsColourWheel.Length];
                    foreach (var rect in rects)
                    {
                        if (!workingArea.Contains(rect.position))
                        {
                            continue;
                        }
                        GUI.Box(rect, "", textureStyle);
                    }

                    i++;
                }
                GUI.color = oldColor;
            }
            else if (_pressedLine != _lineRegCache.chosenLine)
            {
                // We need to clear the cache, as we pressed somewhere new.
                // Alternatively we only need to do this if a fold changed, or the view changed!
                _lineRegCache.Clear();
            }
        }

        internal string GetLineString(BurstDisassembler.AsmLine line)
        {
            _disassembler._output.Clear();

            _disassembler.RenderLine(ref line, false);

            var str = _disassembler._output.ToString();
            _disassembler._output.Length = 0;
            return str;
        }

        private (int start, int end) FindBlockIdx(int startBlock, int firstLine, int lastLine)
        {
            var blockIdx = startBlock;
            for (; blockIdx > 0; blockIdx--)
            {
                if (blockLine[blockIdx] <= firstLine)
                {
                    break;
                }
            }

            var blockIdxEnd = _renderBlockEnd;
            for (; blockIdxEnd < _blocksFragments.Length - 1; blockIdxEnd++)
            {
                var bLen = _folded[blockIdxEnd]
                    ? 1
                    : _disassembler.Blocks[blockIdxEnd].Length;
                if (blockLine[blockIdxEnd] + bLen >= lastLine)
                {
                    break;
                }
            }

            return (blockIdx, blockIdxEnd);
        }

        /// <summary>
        /// Finds all usages of <see cref="regs"/> within view, and returns a list of <see cref="Rect"/>
        /// presenting these usages.
        /// </summary>
        /// <param name="hPad">Horizontal padding in text area.</param>
        /// <param name="cache">Cache handling saving register rects.</param>
        /// <param name="pressedLine">The currently selected line.</param>
        /// <param name="regs">Registers to match against.</param>
        /// <remarks>
        /// This will return representation of ALL matching registers within <see cref="workingArea"/>.
        ///
        /// Depends on:
        /// <list type="bullet">
        /// <item>
        /// <see cref="_burstDisassember"/>.
        /// </item>
        /// <item>
        /// <see cref="LayoutEnhanced"/> been called at least once before this.
        /// </item>
        /// </list>
        /// </remarks>
        internal List<Rect>[] GetRegisterRects(float hPad, ref LineRegRectsCache cache, int pressedLine, List<string> regs)
        {
            const int extraLines = 5;
            const float horizontalAlignmentPad = 2f;

            var firstLine = Math.Max(blockLine[_renderBlockStart] - extraLines, 0);
            var lastLine = blockLine[_renderBlockEnd] + (_folded[_renderBlockEnd]
                ? 1
                : _disassembler.Blocks[_renderBlockEnd].Length-1) + extraLines;

            var (blockIdx, blockIdxEnd) = FindBlockIdx(_renderBlockStart, firstLine, lastLine);

            int lNum = Math.Max(Mathf.FloorToInt(_renderStartY/fontHeight) - extraLines,0);
            // Convert lineIdx to absolut indexes. Needed for _disassembler.Lines[...]
            for (var i = 0; i < blockIdxEnd; i++)
            {
                if (_folded[i])
                {
                    var len = _disassembler.Blocks[i].Length - 1;
                    if (i < blockIdx) { firstLine += len; }
                    lastLine += len;
                }
            }
            // Make sure LastLine does not exceed number of lines
            lastLine = Math.Min(lastLine, _disassembler.Lines.Count - 1);

            if (cache.IsRegisterCachedOrClear(pressedLine, firstLine, lastLine))
            {
                return cache.rects;
            }

            // Clean up regs, so e.g. "rcx" and "ecx" only counts as one:
            regs = _disassembler.CleanRegs(regs);

            var currentLine = firstLine;
            var lineInBlock = firstLine - _disassembler.Blocks[blockIdx].LineIndex;

            var rects = cache.rects;
            if (rects == null)
            {
                rects = new List<Rect>[regs.Count];
                for (var i = 0; i < regs.Count; i++)
                {
                    rects[i] = new List<Rect>();
                }
            }

            while (blockIdx < _blocksFragments.Length && currentLine <= lastLine)
            {
                var lineNotCached = !cache.LinesRegsCached(currentLine);
                var line = _disassembler.Lines[currentLine];
                if (lineNotCached && line.Kind != BurstDisassembler.AsmLineKind.Directive)
                {
                    // Loop body
                    var lineStart = _disassembler.Tokens[line.TokenIndex].AlignedPosition;
                    var i = 0;
                    foreach (var reg in regs)
                    {
                        var regUsedNr = _disassembler.LineUsedReg(currentLine, reg);
                        if (regUsedNr > 0)
                        {
                            var tmpIdx = 0;
                            for (var r = 0; r < regUsedNr; r++)
                            {
                                // Find position and width/height of reg:
                                var width = reg.Length * fontWidth;

                                var tokenRegIdx = _disassembler.GetRegisterTokenIndex(line, reg, tmpIdx);
                                if (tokenRegIdx == -1)
                                {
                                    throw new Exception($"Could not find token index of \"{reg}\" on line {currentLine}.");
                                }

                                var regIdx = _disassembler.Tokens[tokenRegIdx].AlignedPosition - lineStart;
                                var regPos = regIdx * fontWidth;
                                var x = hPad + regPos + horizontalAlignmentPad;
                                rects[i].Add(new Rect(x, lNum*fontHeight, width, fontHeight));

                                tmpIdx = tokenRegIdx + 1;
                            }
                        }
                        i++;
                    }
                }

                // Loop end statements
                if (lineInBlock >= _disassembler.Blocks[blockIdx].Length)
                {
                    // Moved into next block
                    blockIdx++;
                    lineInBlock = 0;
                }

                if (_folded[blockIdx])
                {
                    currentLine += _disassembler.Blocks[blockIdx].Length;
                    blockIdx++;
                    lineInBlock = 0;
                }
                else
                {
                    currentLine++;
                    lineInBlock++;
                }
                lNum++;
            }
            cache.UpdateRegisters(firstLine, lastLine, pressedLine, rects);

            return cache.rects;
        }

        internal struct LineRegRectsCache
        {
            [Flags]
            private enum CachedItem
            {
                None = 0,
                Line = 1,
                Registers = 2
            }

            private CachedItem _isCached;
            private bool _folded;
            /// <summary>
            /// First line we have cached up till.
            /// </summary>
            public int startLine;
            /// <summary>
            /// Last line we have cached down till.
            /// </summary>
            public int endLine;
            /// <summary>
            /// Line we have in focus.
            /// </summary>
            public int chosenLine;

            public Rect lineHighlight;
            /// <summary>
            /// Cached rects. Each row represent one register, and each column is a rect for that register.
            /// </summary>
            public List<Rect>[] rects;

            public bool LinesRegsCached(int pressedLine)
            {
                return (_isCached & CachedItem.Registers) > 0 && BurstMath.WithinRange(startLine, endLine, pressedLine);
            }

            public bool IsRegisterCachedOrClear(int pressedLine, int firstLine, int lastLine)
            {
                var isCached = false;
                if (!IsRegistersCached(pressedLine))
                {
                    // New line was pressed!
                    Clear();
                }
                else if (!ShouldUpdateCache(firstLine, lastLine))
                {
                    isCached = true;
                }

                return isCached;
            }

            public bool IsLineHighlightCached(int linePressed, bool folded) =>
                folded == _folded && (_isCached & CachedItem.Line) > 0 && linePressed == chosenLine;
            public bool IsRegistersCached(int linePressed) => (_isCached & CachedItem.Registers) > 0 && linePressed == chosenLine;

            private bool ShouldUpdateCache(int viewLineStart, int viewLineEnd) => viewLineStart < startLine || viewLineEnd > endLine;

            public void UpdateRegisters(int firstLine, int lastLine, int pressedLine, List<Rect>[] RegRects)
            {
                if (chosenLine != pressedLine && (_isCached & CachedItem.Line) > 0)
                {
                    _isCached ^= CachedItem.Line;
                }
                startLine = Math.Min(firstLine, startLine);
                endLine = Math.Max(lastLine, endLine);
                chosenLine = pressedLine;
                rects = RegRects;
                _isCached |= CachedItem.Registers;
            }

            public void UpdateLineHighlight(int pressedLine, Rect rect, bool folded)
            {
                _isCached |= CachedItem.Line;
                if (chosenLine != pressedLine && (_isCached & CachedItem.Registers) > 0)
                {
                    // line just changed so register cache is no longer valid.
                    _isCached ^= CachedItem.Registers;
                }

                lineHighlight = rect;
                chosenLine = pressedLine;
                _folded = folded;
            }

            public void Clear()
            {
                rects = null;
                chosenLine = -1;
                startLine = int.MaxValue;
                endLine = -1;
                _isCached = CachedItem.None;
            }
        }

        private void TestSelUnderscore(GUIStyle style, Vector2 scrollPos, Rect workingArea)
        {
            var current = GUI.color;

            // Selection
            GUI.color = Color.blue;
            GUI.Box(
                new Rect(horizontalPad + style.padding.left + fontWidth * 8, style.padding.top + fontHeight * 19,
                    3 * fontWidth, 1 * fontHeight), "", textureStyle);

            // Underscored
            Vector2 start = new Vector2(horizontalPad + style.padding.left + fontWidth * 8,
                style.padding.top + fontHeight * 20 - 2);
            Vector2 end = start + new Vector2((3 + 22) * fontWidth, 0 * fontHeight);

            GUI.color = Color.red;
            DrawLine(start, end, 2);

            GUI.color = current;
        }

        internal void RenderBranches(Rect workingArea)
        {
            var color = GUI.color;
            List<Branch> branches = new List<Branch>();
            hoveredBranch = default;
            for (int idx = 0;idx<_disassembler.Blocks.Count; idx++)
            {
                var block = _disassembler.Blocks[idx];
                if (block.Edges != null)
                {
                    foreach (var edge in block.Edges)
                    {
                        if (edge.Kind == BurstDisassembler.AsmEdgeKind.OutBound)
                        {
                            var srcLine = blockLine[idx];
                            if (!_folded[idx])
                            {
                                srcLine += edge.OriginRef.LineIndex;
                            }
                            var dstBlockIdx = edge.LineRef.BlockIndex;
                            var dstLine = blockLine[dstBlockIdx];
                            if (!_folded[dstBlockIdx])
                            {
                                dstLine += edge.LineRef.LineIndex;
                            }

                            int arrowMinY = srcLine;
                            int arrowMaxY = dstLine;
                            if (srcLine > dstLine)
                            {
                                (arrowMinY, arrowMaxY) = (dstLine, srcLine);
                            }

                            if ((dstBlockIdx == idx + 1 && edge.LineRef.LineIndex == 0) // pointing to next line
                                || !(workingArea.yMin <= arrowMaxY * fontHeight &&      // Arrow not inside view.
                                    workingArea.yMax >= arrowMinY * fontHeight))
                            {
                                continue;
                            }
                            branches.Add(CalculateBranch(edge, horizontalPad - (4 + fontWidth), srcLine * fontHeight,
                                dstLine * fontHeight, lineDepth[idx]));
                        }
                    }
                }
            }

            // Drawing branches while making sure the hovered is rendered at top.
            foreach (var branch in branches)
            {
                if (!branch.Edge.Equals(hoveredBranch.Edge))
                {
                    DrawBranch(branch, lineDepth[branch.Edge.OriginRef.BlockIndex], workingArea);
                }
            }
            if (!hoveredBranch.Edge.Equals(default(BurstDisassembler.AsmEdge)))
            {
                DrawBranch(hoveredBranch, lineDepth[hoveredBranch.Edge.OriginRef.BlockIndex], workingArea);
            }

            _prevHoveredEdge = hoveredBranch.Edge;
            GUI.color = color;
        }

        internal int BumpSelectionXByColorTag(string text, int lineIdxTotal, int charsIn)
        {
            bool lastWasStart = true;
            int colorTagStart = text.IndexOf("<color=", lineIdxTotal);

            while (colorTagStart != -1 && colorTagStart - lineIdxTotal < charsIn)
            {
                int colorTagEnd = text.IndexOf('>', colorTagStart + 1);
                // +1 as the index calculation is zero based.
                charsIn += colorTagEnd - colorTagStart + 1;

                if (lastWasStart)
                {
                    colorTagStart = text.IndexOf("</color>", colorTagEnd + 1);
                    lastWasStart = false;
                }
                else
                {
                    colorTagStart = text.IndexOf("<color=", colorTagEnd + 1);
                    lastWasStart = true;
                }
            }
            return charsIn;
        }

        internal void UpdateEnhancedSelectTextIdx(float hPad)
        {
            if (_textSelectionIdxValid || !HasSelection) return;

            int blockIdxStart = _selectBlockStart;
            int blockIdxEnd = _selectBlockEnd;
            float blockStartPosY = _selectStartY;
            float blockEndPosY = _selectEndY;

            var start = selectPos;
            var last = selectDragPos;
            if (last.y < start.y)
            {
                // we selected upwards.
                (start, last) = (last, start);
                (blockIdxStart, blockIdxEnd, blockStartPosY, blockEndPosY) = (blockIdxEnd, blockIdxStart, blockEndPosY, blockStartPosY);
            }

            // Math.Min to make sure line number does not exceed number of line in block (zero indexed).
            var blockStartline = Math.Min(Mathf.FloorToInt((start.y - blockStartPosY) / fontHeight),
                _folded[blockIdxStart]
                    ? 0
                    : _disassembler.Blocks[blockIdxStart].Length-1);
            var blockEndLine = Math.Min(Mathf.FloorToInt((last.y - blockEndPosY) / fontHeight),
                _folded[blockIdxEnd]
                    ? 0
                    : _disassembler.Blocks[blockIdxEnd].Length-1);

            if (blockStartline == blockEndLine && blockIdxStart == blockIdxEnd && start.x > last.x)
            {
                // _selectDragPos was above and behind _selectPos on same line.
                (start, last) = (last, start);
            }

            var text = _folded[blockIdxStart]
                ? CopyColorTags
                    ? GetFragmentStart(blockIdxStart).text + '\n'
                    : GetFragmentStartPlain(blockIdxStart).text + '\n'
                : CopyColorTags
                    ? _disassembler.GetOrRenderBlockToText(blockIdxStart)
                    : _disassembler.GetOrRenderBlockToTextUncached(blockIdxStart, false);

            var (startLineEndIdxTotal, startLineEndIdxRel) = GetEndIndexOfColoredLine(text, blockStartline);
            int startOfLineIdx = startLineEndIdxTotal - startLineEndIdxRel;

            int charsIn = Math.Min(Mathf.FloorToInt((start.x - hPad) / fontWidth), startLineEndIdxRel);
            charsIn = charsIn < 0 ? 0 : charsIn;

            // Adjust charsIn so it takes color tags into considerations.
            charsIn = BumpSelectionXByColorTag(text, startOfLineIdx, charsIn + 1) - 1; // +1 -1 to not bump charsIn when selecting char just after color tag.

            enhancedTextSelectionIdxStart = (blockIdxStart, startOfLineIdx + charsIn);

            if (blockIdxStart < blockIdxEnd)
            {
                text = _folded[blockIdxEnd]
                    ? CopyColorTags
                        ? GetFragmentStart(blockIdxEnd).text + '\n'
                        : GetFragmentStartPlain(blockIdxEnd).text + '\n'
                    : CopyColorTags
                        ? _disassembler.GetOrRenderBlockToText(blockIdxEnd)
                        : _disassembler.GetOrRenderBlockToTextUncached(blockIdxEnd, false);
            }

            var (lastLineEndIdxTotal, lastLineEndIdxRel) = GetEndIndexOfColoredLine(text, blockEndLine);
            startOfLineIdx = lastLineEndIdxTotal - lastLineEndIdxRel;

            int charsInDrag = Math.Min(Mathf.FloorToInt((last.x - hPad) / fontWidth), lastLineEndIdxRel);
            charsInDrag = charsInDrag < 0 ? 0 : charsInDrag;

            // Adjust charsInDrag so it takes color tags into considerations.
            charsInDrag = BumpSelectionXByColorTag(text, startOfLineIdx, charsInDrag);

            enhancedTextSelectionIdxEnd = (blockIdxEnd, startOfLineIdx + charsInDrag);

            _textSelectionIdxValid = true;
        }

        /// <summary>
        /// Updates selection based on whether <see cref="blockIdx"/> was folded or unfolded.
        /// </summary>
        /// <param name="hPad">horizontal padding.</param>
        /// <param name="blockIdx">Block idx of folded/unfolded block.</param>
        /// <param name="block">folded/unfolded block.</param>
        /// <param name="positionY">Blocks y-position in textarea.</param>
        private void UpdateSelectionByFolding(float hPad, int blockIdx, BurstDisassembler.AsmBlock block, float positionY)
        {
            // cursor start and end position of selection
            var start = selectPos;
            var end = selectDragPos;

            // selection block start and end
            var blockStart = _selectBlockStart;
            var blockEnd = _selectBlockEnd;

            // top y-coordinate of selections end block
            var blockEndY = _selectEndY;

            var rightWaySelect = true;
            if (start.y > end.y)
            {
                (start, end) = (end, start);
                (blockStart, blockEnd) = (blockEnd, blockStart);
                blockEndY = _selectStartY;
                rightWaySelect = false;
            }

            if (BurstMath.RoundDownToNearest(end.y, fontHeight) - BurstMath.RoundDownToNearest(start.y, fontHeight) < fontHeight
                && end.x < start.x)
            {
                (start, end) = (end, start);
                rightWaySelect = !rightWaySelect;
            }

            var changeY = _folded[blockIdx]
                ? -Math.Max(block.Length - 1, 1) * fontHeight
                : Math.Max(block.Length - 1, 1) * fontHeight;

            var charsInLine = GetEndIndexOfColoredLine(
                _folded[blockIdx]
                    ? GetFragmentStart(blockIdx).text + '\n'
                    : _disassembler.GetOrRenderBlockToText(blockIdx),
                0).relative;

            var thisLineSelect = Mathf.FloorToInt((start.y - positionY) / fontHeight);
            var thisLineDrag = Mathf.FloorToInt((end.y - positionY) / fontHeight);

            var selectPosCharsIn = Mathf.FloorToInt((start.x - hPad) / fontWidth);
            var selectDragPosCharsIn = Mathf.FloorToInt((end.x - hPad) / fontWidth);

            if (blockStart < blockIdx)
            {
                if (blockEnd == blockIdx)
                {
                    if (thisLineDrag > 0 || (end.x - hPad) / fontWidth > charsInLine)
                    {
                        /* selection starts above touched block but ends in the middle of it.
                         *
                         *  b-1 _xxx        b-1 _xxx
                         *  b   xxx_   =>   b   xxx
                         *  b+1 ____        b+1 ____
                         */

                        // Move selectDragPos onto end of this line.
                        end.y -= thisLineDrag * fontHeight;
                        end.x = charsInLine * fontWidth + hPad + fontWidth / 2;
                    }
                }
                else if (blockEnd > blockIdx)
                {
                    /* selection starts above touched block and ends below it.
                     *
                     *  b-1 _xxx
                     *  b   xxxx
                     *  b+1 xx__
                     */
                    end.y += changeY;
                    blockEndY += changeY;
                }
            }
            else if (blockStart == blockIdx)
            {
                if (blockEnd == blockIdx)
                {
                    if (thisLineSelect > 0 || selectPosCharsIn > charsInLine)
                    {
                        /* selection starts and ends in the middle of the touched block
                         *
                         *  b-1 ____        b-1 ____
                         *  b   _xx_    =>  b   _
                         *  b+1 ____        b+1 ____
                         */
                        // Make cursor go to the end of line
                        end.y -= thisLineDrag * fontHeight;
                        end.x = charsInLine * fontWidth + hPad + fontWidth / 2;

                        StopSelection();
                    }
                    else if (thisLineDrag > 0 || selectDragPosCharsIn > charsInLine)
                    {
                        /* selection starts at first line and ends within
                         *
                         *  b-1 ____        b-1 ____
                         *  b   _xx_    =>  b   x
                         *      xxx_
                         *  b+1 ____        b+1 ____
                         */
                        // Move selectDragPos onto end of this line.
                        end.y -= thisLineDrag * fontHeight;
                        end.x = charsInLine * fontWidth + hPad + fontWidth / 2;
                    }
                }
                else if (blockEnd > blockIdx)
                {
                    // selection starts either in first line or middle of touched block and ends below it
                    end.y += changeY;

                    if (thisLineSelect > 0 || selectPosCharsIn > charsInLine)
                    {
                        /* selection starts in middle of touched block and ends below it
                         *
                         *  b-1 ____        b-1 ____
                         *  b   ____    =>  b   ___x
                         *      x___
                         *  b+1 xx__        b+1 xx__
                         */
                        // Move selectPos onto end of this line.
                        start.y -= thisLineSelect * fontHeight;
                        start.x = charsInLine * fontWidth + hPad + fontWidth / 2;
                    }
                }
            }
            else
            {
                start.y += changeY;
                end.y += changeY;
            }

            // Write back changes made during folding/unfolding
            if (rightWaySelect)
            {
                selectPos = start;
                selectDragPos = end;
                _selectEndY = blockEndY;
            }
            else
            {
                selectPos = end;
                selectDragPos = start;
                _selectStartY = blockEndY;
            }

            if (!HasSelection) selectPos = selectDragPos;
            else _textSelectionIdxValid = false;
        }

        private void FoldUnfoldBlock(int blockIdx)
        {
            var block = _disassembler.Blocks[blockIdx];

            _folded[blockIdx] = !_folded[blockIdx];
            if (_folded[blockIdx])
            {
                finalAreaSizeYInLines -= Math.Max(block.Length - 1, 1);
                AddFoldedString(blockIdx);
            }
            else
            {
                finalAreaSizeYInLines += Math.Max(block.Length - 1, 1);
            }
        }

        private void RenderEnhanced(GUIStyle style, Rect workingArea, bool showBranchMarkers, float hPad, bool doSearch)
        {
            //TestSelUnderscore();
            if (showBranchMarkers)
            {
                RenderBranches(workingArea);
            }

            int lOffs = Mathf.FloorToInt(_renderStartY / fontHeight);
            var fragNr = 0;
            for (var i = 0; i <= _renderBlockEnd; i++)
            {
                if (i < _renderBlockStart)
                {
                    if (doSearch)
                    {
                        foreach (var frag in GetPlainFragments(i))
                        {
                            fragNr++;
                        }
                    }

                    continue;
                }

                var block = _disassembler.Blocks[i];

                var blockLongEnoughForFold = block.Length > 1;
                if (blockLongEnoughForFold)
                {
                    var pressed = DrawFold(hPad - 2, lOffs*fontHeight, _folded[i], block.Kind);
                    if (pressed)
                    {
                        FoldUnfoldBlock(i);

                        // Make sure cursor and selection is updated according to the changed folding.
                        UpdateSelectionByFolding(hPad, i, block, lOffs*fontHeight);
                    }
                }

                if (doSearch)
                {
                    var lNum = 0;
                    for (var tmp = 0; tmp < GetPlainFragments(i).Count; fragNr++, tmp++)
                    {
                        if (searchHits.TryGetValue(fragNr, out var hits))
                        {
                            if (_folded[i]) FoldUnfoldBlock(i);

                            foreach (var (startIdx, endIdx, nr) in hits)
                            {
                                var isActive = nr == _activeSearchHitIdx;
                                var realEndIdx = endIdx;

                                if (endIdx == int.MaxValue)
                                {
                                    var text = GetPlainFragments(i)[tmp].text;
                                    realEndIdx = text.Length;
                                    RenderMultipleLinesSelection(text, horizontalPad, lOffs*fontHeight, + lNum, startIdx,
                                        realEndIdx, isActive);
                                }
                                else
                                {
                                    RenderMultipleLinesSelection(
                                        GetPlainFragments(i)[tmp].text,
                                        horizontalPad,
                                        lOffs*fontHeight,
                                        lNum,
                                        startIdx,
                                        realEndIdx,
                                        isActive);
                                }
                            }
                        }
                        lNum += GetPlainFragments(i)[tmp].lineCount;
                    }
                }

                if (!_folded[i] || !blockLongEnoughForFold)
                {
                    for (var j = 0; j < _blocksFragments[i].Count; j++)
                    {
                        var fragment = _blocksFragments[i][j];

                        var fragLineCount = fragment.lineCount;
                        var fragHeight = fragLineCount * fontHeight;

                        SelectText(hPad, lOffs*fontHeight, fragHeight, fragment,
                            _disassembler.IsColored
                                ? GetEndIndexOfColoredLine
                                : GetEndIndexOfPlainLine);

                        GUI.Label(new Rect(hPad, lOffs*fontHeight, finalAreaSizeXRenderSize, fragHeight + fontHeight*0.5f),
                            fragment.text, style);
                        lOffs+= fragLineCount;
                    }
                }
                else
                {
                    var frag = GetFragmentStart(i);

                    SelectText(hPad, lOffs*fontHeight, fontHeight, frag,
                        _disassembler.IsColored
                            ? GetEndIndexOfColoredLine
                            : GetEndIndexOfPlainLine);

                    float fragHeight = fontHeight;

                    GUI.Label(new Rect(hPad, lOffs*fontHeight, finalAreaSizeXRenderSize, fragHeight*1.5f), frag.text, style);
                    lOffs++;
                }
            }
            UpdateEnhancedSelectTextIdx(hPad);
        }

        private List<Fragment> RecomputeFragments(string text)
        {
            List<Fragment> result = new List<Fragment>();

            string[] pieces = text.Split('\n');
            _mTextLines = pieces.Length;

            StringBuilder b = new StringBuilder();

            int lineCount = 0;
            for (int a = 0; a < pieces.Length; a++)
            {
                if (b.Length >= kMaxFragment)
                {
                    b.Remove(b.Length - 1, 1);
                    AddFragment(b, lineCount, result);
                    lineCount = 0;
                }

                b.Append(pieces[a]);
                b.Append('\n');
                lineCount++;
            }

            if (b.Length > 0)
            {
                b.Remove(b.Length - 1, 1);
                AddFragment(b, lineCount, result);
            }

            return result;
        }

        private List<Fragment> RecomputeFragmentsFromBlock(int blockIdx, bool colored)
        {
            var text = _disassembler.GetOrRenderBlockToTextUncached(blockIdx, colored).TrimEnd('\n');

            return RecomputeFragments(text);
        }

        internal List<Fragment> RecomputeFragmentsFromBlock(int blockIdx)
        {
            var text = _disassembler.GetOrRenderBlockToText(blockIdx).TrimEnd('\n');

            return RecomputeFragments(text);
        }

        private static void AddFragment(StringBuilder b, int lineCount, List<Fragment> result)
        {
            result.Add(new Fragment() { text = b.ToString(), lineCount = lineCount });
            b.Length = 0;
        }
    }
}
