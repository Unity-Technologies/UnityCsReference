// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using TextElement = UnityEngine.UIElements.TextElement;

namespace UnityEditor.UIElements.Text
{
    internal class TextInfoOverlay 
    {
        [Flags]
        internal enum DisplayOption
        {
            None = 0,
            Characters = 1,
            Words = 2,
            Lines = 4,
            Links = 8
        }

        DisplayOption m_DisplayOption;

        internal DisplayOption displayOption
        {
            set
            {
                m_DisplayOption = value;
                DrawTextInfo();
            }
            get => m_DisplayOption;
        }

        private TextElement m_TargetTextElement;
        private bool wasInPermanentCache = false;
        internal TextElement targetTextElement
        {
            get => m_TargetTextElement;
            set
            {
                if(m_TargetTextElement == value) return;

                if(m_TargetTextElement != null && wasInPermanentCache)
                {
                    m_TargetTextElement.uitkTextHandle.RemoveFromPermanentCache();
                    wasInPermanentCache = false;
                }


                m_TargetTextElement = value;


                if (m_TargetTextElement != null)
                {
                    wasInPermanentCache = m_TargetTextElement.uitkTextHandle.IsCachedPermanent;
                    if(!wasInPermanentCache)
                        m_TargetTextElement.uitkTextHandle.AddToPermanentCacheAndGenerateMesh();
                }

                DrawTextInfo();
            }
        }

        private const float k_SmallVisibleSize = 10;
        private const float k_LabelFontSize = 10;

        /// <summary>True when handle returns metrics already in content Y-down (e.g. ATG after normalization).</summary>
        private bool MetricsAreContentYDown => m_TargetTextElement != null && m_TargetTextElement.computedStyle.unityTextGenerator == TextGeneratorType.Advanced;

        /// <summary>Content Y for a metric: use as-is when MetricsAreContentYDown, else contentHeight - metric (TextCore Y-up).</summary>
        private float ContentY(float metricY, float contentHeight)
        {
            return MetricsAreContentYDown ? metricY : contentHeight - metricY;
        }

        private Vector2 ContentPoint(Vector2 v, float contentHeight)
        {
            return MetricsAreContentYDown ? new Vector2(v.x, v.y) : new Vector2(v.x, contentHeight - v.y);
        }

        readonly FontAsset m_RobotoMonoRegular;
        Color m_LabelColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);

        DebuggerSelection m_DebuggerSelection;

        internal TextInfoOverlay(DebuggerSelection debuggerSelection)
        {
            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onSelectedElementChanged += element => targetTextElement = element as TextElement;
            m_TargetTextElement = null;

            m_RobotoMonoRegular = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular SDF.asset") as FontAsset;
        }

        internal void RemoveOverlay()
        {
            displayOption = DisplayOption.None;
            targetTextElement = null;
        }

        internal void DrawTextInfo()
        {
            m_TargetTextElement?.MarkDirtyRepaint();
        }

        public void Draw(MeshGenerationContext mgc, DisplayOption displayOption)
        {
            m_DisplayOption = displayOption;

            if (m_TargetTextElement == null)
                return;

            Rect r = m_TargetTextElement.LocalToWorld(m_TargetTextElement.contentRect);
            if (r.width < 0.01f || r.height < 0.01f)
                return; // Skip rendering when too small.

            if (displayOption.HasFlag(DisplayOption.Characters))
                DrawCharacters(mgc);

            if (displayOption.HasFlag(DisplayOption.Words))
                DrawWords(mgc);

            if (displayOption.HasFlag(DisplayOption.Links))
                DrawLinks(mgc);

            if (displayOption.HasFlag(DisplayOption.Lines))
                DrawLines(mgc);
        }

        private void DrawCharacters(MeshGenerationContext mgc)
        {
            var handle = m_TargetTextElement.uitkTextHandle;
            int characterCount = handle.GetTextElementCount();
            var content = m_TargetTextElement.contentRect;
            float contentHeight = content.height;

            for (int i = 0; i < characterCount; i++)
            {
                var currentChar = handle.GetScaledCharacterMetrics(i);

                // Skip invisible characters (whitespace, control chars, newlines).
                // They carry no meaningful glyph information for the overlay and would only add noise.
                if (!currentChar.isVisible)
                    continue;

                float origin = currentChar.origin;
                float advance = currentChar.xAdvance;
                float ascentline = currentChar.ascentline;
                float baseline = currentChar.baseline;
                float descentline = currentChar.descentline;

                // Draw character bounds (content Y-down: top = min y, bottom = max y)
                DrawDottedRectangle(mgc,
                    ContentPoint(currentChar.topLeft, contentHeight),
                    ContentPoint(currentChar.topRight, contentHeight),
                    ContentPoint(currentChar.bottomRight, contentHeight),
                    ContentPoint(currentChar.bottomLeft, contentHeight),
                    Color.green);

                float ascY = ContentY(ascentline, contentHeight);
                float descY = ContentY(descentline, contentHeight);
                float baseY = ContentY(baseline, contentHeight);
                Vector2 asc_start = new Vector2(currentChar.topLeft.x, ascY);
                Vector2 asc_end = new Vector2(currentChar.topRight.x, ascY);
                DrawDottedLine(mgc, asc_start, asc_end, Color.yellow);
                Vector2 desc_start = new Vector2(currentChar.topLeft.x, descY);
                Vector2 desc_end = new Vector2(currentChar.topRight.x, descY);
                DrawDottedLine(mgc, desc_start, desc_end, Color.yellow);
                Vector2 baseline_start = new Vector2(currentChar.topLeft.x, baseY);
                Vector2 baseline_end = new Vector2(currentChar.topRight.x, baseY);
                DrawDottedLine(mgc, baseline_start, baseline_end, Color.yellow);

                float capline = baseline + currentChar.fontCapLine * currentChar.scale;
                float meanline = baseline + currentChar.fontMeanLine * currentChar.scale;
                Vector2 cap_start = new Vector2(currentChar.topLeft.x, ContentY(capline, contentHeight));
                Vector2 cap_end = new Vector2(currentChar.topRight.x, ContentY(capline, contentHeight));
                DrawDottedLine(mgc, cap_start, cap_end, Color.cyan);
                Vector2 mean_start = new Vector2(currentChar.topLeft.x, ContentY(meanline, contentHeight));
                Vector2 mean_end = new Vector2(currentChar.topRight.x, ContentY(meanline, contentHeight));
                DrawDottedLine(mgc, mean_start, mean_end, Color.cyan);

                Vector2 origin_position = new Vector2(origin, ContentY(baseline, contentHeight));
                DrawCrossHair(mgc, origin_position, k_SmallVisibleSize / 2, Color.cyan);
                Vector2 advance_position = new Vector2(advance, ContentY(baseline, contentHeight));
                DrawCrossHair(mgc, advance_position, k_SmallVisibleSize / 2, Color.yellow);
            }
        }

        // Eight visually distinct colors cycled per word so every word and each of its
        // line-segments (when character-level wrapping splits a word across lines) share one color.
        static readonly Color[] k_WordOutlineColors =
        {
            new Color(0.20f, 0.82f, 0.20f, 1f), // green
            new Color(0.22f, 0.60f, 1.00f, 1f), // blue
            new Color(1.00f, 0.60f, 0.20f, 1f), // orange
            new Color(0.82f, 0.20f, 0.82f, 1f), // purple
            new Color(0.20f, 0.82f, 0.82f, 1f), // teal
            new Color(1.00f, 0.80f, 0.20f, 1f), // yellow
            new Color(1.00f, 0.38f, 0.38f, 1f), // red
            new Color(0.40f, 1.00f, 0.60f, 1f), // lime
        };

        private void DrawWords(MeshGenerationContext mgc)
        {
            var handle = m_TargetTextElement.uitkTextHandle;
            int wordCount = handle.GetWordCount();
            float contentHeight = m_TargetTextElement.contentRect.height;
            int totalCharCount = handle.GetTextElementCount();

            for (int i = 0; i < wordCount; i++)
            {
                var currWord = handle.GetWordInfo(i);
                if (currWord.characterCount <= 0)
                    continue;

                // All line-segments of the same logical word share one color so you can tell
                // them apart visually from neighbouring words even when a word wraps.
                var wordColor = k_WordOutlineColors[i % k_WordOutlineColors.Length];

                float segmentTop = float.MaxValue;
                float segmentBottom = float.MinValue;
                float segmentLeft = float.MaxValue;
                float segmentRight = float.MinValue;

                for (int j = 0; j < currWord.characterCount; j++)
                {
                    int characterIndex = currWord.firstCharacterIndex + j;
                    if (characterIndex >= totalCharCount)
                        break;

                    var currCharInfo = handle.GetScaledCharacterMetrics(characterIndex);

                    // Only visible glyphs contribute to the segment bounds.  Whitespace (spaces,
                    // tabs, newlines) is excluded so trailing/leading whitespace is never folded
                    // into a word rectangle.
                    if (currCharInfo.isVisible)
                    {
                        float topY = ContentY(currCharInfo.ascentline, contentHeight);
                        float bottomY = ContentY(currCharInfo.descentline, contentHeight);
                        if (topY < bottomY)
                        {
                            segmentTop = Mathf.Min(segmentTop, topY);
                            segmentBottom = Mathf.Max(segmentBottom, bottomY);
                        }

                        // RTL glyphs can have topLeft.x > topRight.x; take the true X extremes.
                        float charLeft = Mathf.Min(currCharInfo.topLeft.x, currCharInfo.topRight.x);
                        float charRight = Mathf.Max(currCharInfo.topLeft.x, currCharInfo.topRight.x);
                        segmentLeft = Mathf.Min(segmentLeft, charLeft);
                        segmentRight = Mathf.Max(segmentRight, charRight);
                    }

                    bool isLastChar = j == currWord.characterCount - 1;
                    // Detect a line change within this word — happens when character-level wrapping
                    // splits a single ICU word across multiple lines.
                    bool lineChanged = !isLastChar
                        && characterIndex + 1 < totalCharCount
                        && currCharInfo.lineNumber != handle.GetScaledCharacterMetrics(characterIndex + 1).lineNumber;

                    if (isLastChar || lineChanged)
                    {
                        // Draw one rectangle per line-segment. A word split across N lines yields
                        // N rectangles, all the same color, making the wrapping easy to spot.
                        if (segmentLeft < segmentRight && segmentTop < segmentBottom)
                        {
                            float h = Mathf.Max(0.001f, segmentBottom - segmentTop);
                            float w = Mathf.Max(0.001f, segmentRight - segmentLeft);
                            DrawRectangle(mgc, new Rect(segmentLeft, segmentTop, w, h), wordColor);
                        }

                        // Reset segment accumulators for the next line-segment of this word.
                        segmentTop = float.MaxValue;
                        segmentBottom = float.MinValue;
                        segmentLeft = float.MaxValue;
                        segmentRight = float.MinValue;
                    }
                }
            }
        }

        private void DrawLinks(MeshGenerationContext mgc)
        {
            var handle = m_TargetTextElement.uitkTextHandle;
            int linkCount = handle.GetLinkCount();
            if (linkCount == 0)
                return;

            var rects = handle.GetLinkRects();
            // Handle returns link rects in element content space: Y-down, origin top-left (UITK convention).
            for (int i = 0; i < rects.Length; i++)
            {
                DrawRectangle(mgc, rects[i], Color.cyan);
            }
        }

        private const float k_LabelOffsetAbove = -2f;
        private const float k_LabelOffsetBelow = 4f;

        private void DrawLines(MeshGenerationContext mgc)
        {
            var handle = m_TargetTextElement.uitkTextHandle;
            int lineCount = handle.GetLineCount();
            float contentHeight = m_TargetTextElement.contentRect.height;

            for (int i = 0; i < lineCount; i++)
            {
                var lineInfo = handle.GetLineInfo(i);

                // TextCore LineInfo is in pixel space; ATG GetLineInfo already returns
                // point-space values. Scale TextCore values so the overlay draws correctly.
                if (!MetricsAreContentYDown)
                {
                    float invPPP = 1f / m_TargetTextElement.scaledPixelsPerPoint;
                    lineInfo.ascender *= invPPP;
                    lineInfo.baseline *= invPPP;
                    lineInfo.descender *= invPPP;
                    lineInfo.lineExtents = new Extents(
                        lineInfo.lineExtents.min * invPPP,
                        lineInfo.lineExtents.max * invPPP);
                    lineInfo.length *= invPPP;
                }

                var firstCharacterInfo = handle.GetScaledCharacterMetrics(lineInfo.firstCharacterIndex);

                float lineLeft = lineInfo.lineExtents.min.x;
                float lineRight = lineInfo.lineExtents.max.x;

                float ascY = ContentY(lineInfo.ascender, contentHeight);
                float baseY = ContentY(lineInfo.baseline, contentHeight);
                float descY = ContentY(lineInfo.descender, contentHeight);

                float lineRectTop = MetricsAreContentYDown
                    ? lineInfo.lineExtents.min.y
                    : contentHeight - lineInfo.lineExtents.max.y;
                float lineRectHeight = MetricsAreContentYDown
                    ? lineInfo.lineExtents.max.y - lineInfo.lineExtents.min.y
                    : Math.Abs(lineInfo.lineExtents.max.y - lineInfo.lineExtents.min.y);

                Rect currentRectangle = new Rect(lineInfo.lineExtents.min.x, lineRectTop,
                    lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x,
                    Mathf.Max(0.001f, lineRectHeight));
                DrawDottedRectangle(mgc, currentRectangle, Color.green);

                Vector2 asc_start = new Vector2(lineLeft, ascY);
                Vector2 asc_end = new Vector2(lineRight, ascY);
                DrawDottedLine(mgc, asc_start, asc_end, Color.yellow);
                Vector2 desc_start = new Vector2(lineLeft, descY);
                Vector2 desc_end = new Vector2(lineRight, descY);
                DrawDottedLine(mgc, desc_start, desc_end, Color.yellow);
                Vector2 baseline_start = new Vector2(lineLeft, baseY);
                Vector2 baseline_end = new Vector2(lineRight, baseY);
                DrawDottedLine(mgc, baseline_start, baseline_end, Color.yellow);

                bool hasCapMeanData = firstCharacterInfo.fontCapLine != 0f || firstCharacterInfo.fontMeanLine != 0f;
                Vector2 cap_start = default, cap_end = default;
                Vector2 mean_start = default, mean_end = default;

                if (hasCapMeanData)
                {
                    float capline = lineInfo.baseline + firstCharacterInfo.fontCapLine * firstCharacterInfo.scale;
                    float meanline = lineInfo.baseline + firstCharacterInfo.fontMeanLine * firstCharacterInfo.scale;
                    cap_start = new Vector2(lineLeft, ContentY(capline, contentHeight));
                    cap_end = new Vector2(lineRight, ContentY(capline, contentHeight));
                    mean_start = new Vector2(lineLeft, ContentY(meanline, contentHeight));
                    mean_end = new Vector2(lineRight, ContentY(meanline, contentHeight));
                    DrawDottedLine(mgc, cap_start, cap_end, Color.cyan);
                    DrawDottedLine(mgc, mean_start, mean_end, Color.cyan);
                }

                if (m_TargetTextElement.worldTransform.lossyScale.x > 2)
                {
                    DrawLabelForLine(mgc, "Ascent Line", asc_start, k_LabelOffsetAbove);
                    DrawLabelForLine(mgc, "Base Line", baseline_start, k_LabelOffsetAbove);
                    DrawLabelForLine(mgc, "Descent Line", desc_start, k_LabelOffsetBelow);

                    if (hasCapMeanData)
                    {
                        DrawLabelForLine(mgc, "Cap Line", cap_start, k_LabelOffsetAbove);
                        DrawLabelForLine(mgc, "Mean Line", mean_start, k_LabelOffsetAbove);
                    }
                }
            }
        }

        private Vector2 GetOffset()
        {
            // Map content space (0,0 = top-left of content) to element local space.
            var content = m_TargetTextElement.contentRect;
            return new Vector2(content.x, content.y);
        }

        private void DrawDottedRectangle(MeshGenerationContext mgc, Rect rectangle, Color color)
        {
            //TODO dotted...
            DrawRectangle(mgc, rectangle, color);
        }

        private void DrawDottedRectangle(MeshGenerationContext mgc, Vector2 top_left, Vector2 top_right, Vector2 bottom_right, Vector2 bottom_left, Color color)
        {
            //TODO dotted...
            DrawRectangle(mgc, top_left, top_right, bottom_right, bottom_left, color);
        }

        private void DrawDottedLine(MeshGenerationContext mgc, Vector2 start, Vector2 end, Color color)
        {
            var offset = GetOffset();
            start = m_TargetTextElement.LocalToWorld(start + offset);
            end = m_TargetTextElement.LocalToWorld(end + offset);

            var painter = mgc.painter2D;
            painter.strokeColor = color;
            painter.lineWidth = 1;
            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(end);
            painter.ClosePath();
            painter.Stroke();
        }

        private void DrawCrossHair(MeshGenerationContext mgc, Vector2 pos, float size, Color color)
        {
            pos = m_TargetTextElement.LocalToWorld(pos+ GetOffset());
            const float lineThickness = 1.0f;

            Rect verticalRect = new Rect(pos.x - lineThickness / 2, pos.y - size / 2, lineThickness, size);
            Rect horizontalRect = new Rect(pos.x - size/2, pos.y - lineThickness / 2, size, lineThickness);

            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var verticalLine = MeshGenerator.RectangleParams.MakeSolid(verticalRect, color, playModeTintColor);
            var horizontalLine = MeshGenerator.RectangleParams.MakeSolid(horizontalRect, color, playModeTintColor);

            mgc.meshGenerator.DrawRectangle(verticalLine);
            mgc.meshGenerator.DrawRectangle(horizontalLine);
        }

        private void DrawRectangle(MeshGenerationContext mgc, Rect rectangle, Color color)
        {
            Vector2 top_left = new Vector2(rectangle.xMin, rectangle.yMin);
            Vector2 top_right = new Vector2(rectangle.xMax, rectangle.yMin);
            Vector2 bottom_right = new Vector2(rectangle.xMax, rectangle.yMax);
            Vector2 bottom_left = new Vector2(rectangle.xMin, rectangle.yMax);

            DrawRectangle(mgc, top_left, top_right, bottom_right, bottom_left, color);
        }
        private void DrawRectangle(MeshGenerationContext mgc, Vector2 top_left, Vector2 top_right, Vector2 bottom_right, Vector2 bottom_left, Color color)
        {
            // This method expect the coordinates to be in the contentRect of the m_TargetTextElement
            // It will apply the transformation so that the resulting geometry is in world space (no need for separate offset for paddind + border)

            var offset = GetOffset();


            top_left = m_TargetTextElement.LocalToWorld(top_left + offset);
            top_right = m_TargetTextElement.LocalToWorld(top_right + offset);
            bottom_right = m_TargetTextElement.LocalToWorld(bottom_right + offset);
            bottom_left = m_TargetTextElement.LocalToWorld(bottom_left + offset);

            var painter = mgc.painter2D;
            painter.strokeColor = color;
            painter.lineWidth = 1;
            painter.BeginPath();
            painter.MoveTo(top_left);
            painter.LineTo(top_right);
            painter.LineTo(bottom_right);
            painter.LineTo(bottom_left);
            painter.ClosePath();
            painter.Stroke();
        }

        // Draw label with the same style as in TMP
        private void DrawLabelForLine(MeshGenerationContext mgc, String name, Vector2 contentPos, float screenYOffset)
        {
            var worldPos = m_TargetTextElement.LocalToWorld(contentPos + GetOffset());
            worldPos.y += screenYOffset;
            mgc.DrawText(name, worldPos, k_LabelFontSize, m_LabelColor, m_RobotoMonoRegular);
        }
    }
}
