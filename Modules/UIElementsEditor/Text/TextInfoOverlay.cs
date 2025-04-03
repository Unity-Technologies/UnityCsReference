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
    internal class TextInfoOverlay : VisualElement
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
        internal TextElement targetTextElement
        {
            get => m_TargetTextElement;
            set
            {
                // Set the new target label
                if (m_TargetTextElement != null)
                {
                    m_TargetTextElement.generateVisualContent -= OnGenerateVisualContent;
                    m_TargetTextElement.MarkDirtyRepaint();
                }

                m_TargetTextElement = value;

                if (value != null)
                {
                    m_TargetTextElement.generateVisualContent += OnGenerateVisualContent;
                    m_TargetTextElement.MarkDirtyRepaint();
                }

                DrawTextInfo();
            }
        }

        private float m_TextElementFontSize => m_TargetTextElement.resolvedStyle.fontSize;
        private float m_LineThickness => m_TextElementFontSize / 150;
        private float m_DottedRectSpaceSize => m_TextElementFontSize / 10;
        private float m_DottedLineSpaceSize => m_TextElementFontSize / 10;
        private float m_LabelFontSize => m_TextElementFontSize / 20;

        FontAsset m_RobotoMonoRegular;
        Color m_LabelColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);

        DebuggerSelection m_DebuggerSelection;

        internal TextInfoOverlay(DebuggerSelection debuggerSelection)
        {
            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onSelectedElementChanged += element => targetTextElement = element as TextElement;
            m_TargetTextElement = null;

            m_RobotoMonoRegular = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular SDF.asset") as FontAsset;
            style.unityFontDefinition = FontDefinition.FromSDFFont(m_RobotoMonoRegular);
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

        internal void ToggleCharacters(bool showCharacters)
        {
            if (showCharacters)
            {
                displayOption |= DisplayOption.Characters;
                DrawTextInfo();
            }
            else
                displayOption &= ~DisplayOption.Characters;
        }

        internal void ToggleWords(bool showWords)
        {
            if (showWords)
            {
                displayOption |= DisplayOption.Words;
                DrawTextInfo();
            }
            else
                displayOption &= ~DisplayOption.Words;
        }

        internal void ToggleLinks(bool showLinks)
        {
            if (showLinks)
            {
                displayOption |= DisplayOption.Links;
                DrawTextInfo();
            }
            else
                displayOption &= ~DisplayOption.Links;
        }

        internal void ToggleLines(bool showLines)
        {
            if (showLines)
            {
                displayOption |= DisplayOption.Lines;
                DrawTextInfo();
            }
            else
                displayOption &= ~DisplayOption.Lines;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Rect r = contentRect;
            if (r.width < 0.01f || r.height < 0.01f || m_TextElementFontSize < 1)
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
            //TODO add scaling here
            var textInfo = m_TargetTextElement.uitkTextHandle.textInfo;
            int characterCount = textInfo.characterCount;

            for (int i = 0; i < characterCount; i++)
            {
                var currentChar = textInfo.textElementInfo[i];

                //TODO: check if visible: maxVisibleChar, maxVisibleLines, firstVisibleChar (not currently implemented in Text Element)

                float origin = currentChar.origin;
                float advance = currentChar.xAdvance;
                float ascentline = currentChar.ascender;
                float baseline = currentChar.baseLine;
                float descentline = currentChar.descender;

                // Draw character bounds
                if (currentChar.isVisible)
                {
                    Rect currentRect = new Rect(currentChar.topLeft.x, m_TargetTextElement.layout.height - currentChar.topLeft.y,
                        currentChar.topRight.x - currentChar.topLeft.x,
                        Math.Abs((float)(currentChar.bottomLeft.y - currentChar.topLeft.y)));
                    currentRect = ApplyOffset(currentRect);
                    DrawDottedRectangle(mgc, currentRect, m_DottedRectSpaceSize, Color.green);

                    // Draw ascent line
                    Vector2 asc_start = new Vector2(currentChar.topLeft.x, m_TargetTextElement.layout.height - ascentline);
                    Vector2 asc_end = new Vector2(currentChar.topRight.x, m_TargetTextElement.layout.height - ascentline);
                    asc_start = ApplyOffset(asc_start);
                    asc_end = ApplyOffset(asc_end);

                    Rect ascentLine = new Rect(asc_start.x, asc_start.y,
                        asc_end.x - asc_start.x, 1);
                    DrawHorizontalDottedLine(mgc, ascentLine, m_DottedLineSpaceSize, Color.yellow);

                    // Draw descent line
                    Vector2 desc_start = new Vector2(currentChar.topLeft.x, m_TargetTextElement.layout.height - descentline);
                    Vector2 desc_end = new Vector2(currentChar.topRight.x, m_TargetTextElement.layout.height - descentline);
                    desc_start = ApplyOffset(desc_start);
                    desc_end = ApplyOffset(desc_end);

                    Rect descentLine = new Rect(desc_start.x, desc_start.y - 1,
                        desc_end.x - desc_start.x, 1);
                    DrawHorizontalDottedLine(mgc, descentLine, m_DottedLineSpaceSize, Color.yellow);

                    // Draw baseline
                    Vector2 baseline_start = new Vector2(currentChar.topLeft.x, m_TargetTextElement.layout.height - baseline);
                    Vector2 baseline_end = new Vector2(currentChar.topRight.x, m_TargetTextElement.layout.height - baseline);
                    baseline_start = ApplyOffset(baseline_start);
                    baseline_end = ApplyOffset(baseline_end);

                    Rect baseLine = new Rect(baseline_start.x, baseline_end.y,
                        baseline_end.x - baseline_start.x, 1);
                    DrawHorizontalDottedLine(mgc, baseLine, m_DottedLineSpaceSize, Color.yellow);

                    float capline = currentChar.fontAsset == null
                        ? 0
                        : baseline + currentChar.fontAsset.faceInfo.capLine * currentChar.scale;
                    float meanline = currentChar.fontAsset == null
                        ? 0
                        : baseline + currentChar.fontAsset.faceInfo.meanLine * currentChar.scale;

                    Vector2 cap_start = new Vector2(currentChar.topLeft.x, m_TargetTextElement.layout.height - capline);
                    Vector2 cap_end = new Vector2(currentChar.topRight.x, m_TargetTextElement.layout.height - capline);

                    Vector2 mean_start = new Vector2(currentChar.topLeft.x, m_TargetTextElement.layout.height - meanline);
                    Vector2 mean_end = new Vector2(currentChar.topRight.x, m_TargetTextElement.layout.height - meanline);

                    // Draw cap line
                    cap_start = ApplyOffset(cap_start);
                    cap_end = ApplyOffset(cap_end);
                    Rect capLine = new Rect(cap_start.x, cap_start.y, cap_end.x - cap_start.x, 1);

                    DrawHorizontalDottedLine(mgc, capLine, m_DottedLineSpaceSize, Color.cyan);

                    // Draw mean line
                    mean_start = ApplyOffset(mean_start);
                    mean_end = ApplyOffset(mean_end);
                    Rect meanLine = new Rect(mean_start.x, mean_start.y,
                        mean_end.x - mean_start.x, 1);
                    DrawHorizontalDottedLine(mgc, meanLine, m_DottedLineSpaceSize, Color.cyan);

                    // Draw labels if zoomed in to a certain scale
                    if (m_TargetTextElement.parent.resolvedStyle.scale.value.x > 2)
                    {
                        DrawLabelForLine(mgc, "Ascent Line", asc_start);
                        DrawLabelForLine(mgc, "Descent Line", desc_start);
                        DrawLabelForLine(mgc, "Base Line", baseline_start);
                        DrawLabelForLine(mgc, "Cap Line", cap_start);
                        DrawLabelForLine(mgc, "Mean Line", mean_start);
                    }
                }

                // Draw white space bounds
                else
                {
                    float whiteSpaceAdvance = Math.Abs(origin - advance) > 0.01f
                        ? advance
                        : origin + (ascentline - descentline) * 0.03f;
                    Rect currentRect = new Rect(origin, m_TargetTextElement.layout.height - ascentline,
                        Math.Abs(whiteSpaceAdvance - origin), Math.Abs(ascentline - descentline));
                    currentRect = ApplyOffset(currentRect);
                    DrawDottedRectangle(mgc, currentRect, m_DottedRectSpaceSize, Color.gray);
                }

                // Draw origin
                Vector2 origin_position = new Vector2(origin, m_TargetTextElement.layout.height - baseline);
                origin_position = ApplyOffset(origin_position);
                DrawCrossHair(mgc, origin_position.x, origin_position.y, m_DottedRectSpaceSize / 2, Color.cyan);

                // Draw horizontal advance
                Vector2 advance_position = new Vector2(advance, m_TargetTextElement.layout.height - baseline);
                advance_position = ApplyOffset(advance_position);
                DrawCrossHair(mgc, advance_position.x, advance_position.y, m_DottedRectSpaceSize / 2, Color.yellow);

            }
        }

        private void DrawWords(MeshGenerationContext mgc)
        {
            var textInfo = m_TargetTextElement.uitkTextHandle.textInfo;
            int wordCount = textInfo.wordCount;

            for (int i = 0; i < wordCount; i++)
            {
                var currWord = textInfo.wordInfo[i];

                bool isBeginRegion = false;
                float maxAscender = -Mathf.Infinity;
                float minDescender = Mathf.Infinity;

                float wordLeft = 0;

                for (int j = 0; j < currWord.characterCount; j++)
                {
                    int characterIndex = currWord.firstCharacterIndex + j;
                    var currCharInfo = textInfo.textElementInfo[characterIndex];
                    int currentLine = currCharInfo.lineNumber;

                    maxAscender = Mathf.Max(maxAscender, currCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currCharInfo.descender);

                    // Draw word bounds

                    // If reached the first character of the word
                    if (isBeginRegion == false)
                    {
                        isBeginRegion = true;

                        wordLeft = currCharInfo.topLeft.x;

                        // If the word is one character
                        if (currWord.characterCount == 1)
                        {
                            isBeginRegion = false;

                            Rect currentRect = new Rect(wordLeft, m_TargetTextElement.layout.height - maxAscender,
                                currCharInfo.topRight.x - wordLeft, Math.Abs(maxAscender - minDescender));
                            currentRect = ApplyOffset(currentRect);
                            DrawRectangle(mgc, currentRect, Color.green);
                        }
                    }

                    // If reached last character of the word
                    if (isBeginRegion && j == currWord.characterCount - 1)
                    {
                        isBeginRegion = false;

                        Rect currentRect = new Rect(wordLeft, m_TargetTextElement.layout.height - maxAscender,
                            currCharInfo.topRight.x - wordLeft, Math.Abs(maxAscender - minDescender));
                        currentRect = ApplyOffset(currentRect);
                        DrawRectangle(mgc, currentRect, Color.green);
                    }

                    // If the word is split and will continue on the next line
                    else if (isBeginRegion && currentLine != textInfo.textElementInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        Rect currentRect = new Rect(wordLeft, m_TargetTextElement.layout.height - maxAscender,
                            currCharInfo.topRight.x - wordLeft, Math.Abs(maxAscender - minDescender));
                        currentRect = ApplyOffset(currentRect);
                        DrawRectangle(mgc, currentRect, Color.green);

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                    }
                }
            }
        }

        private void DrawLinks(MeshGenerationContext mgc)
        {
            var textInfo = m_TargetTextElement.uitkTextHandle.textInfo;
            int linkCount = textInfo.linkCount;

            for (int i = 0; i < linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];
                bool isBeginRegion = false;

                float maxAscender = -Mathf.Infinity;
                float minDescender = Mathf.Infinity;

                var linkleft = 0f;

                for (int j = 0; j < linkInfo.linkTextLength; j++)
                {
                    int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    var currentCharInfo = textInfo.textElementInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    // Draw link bounds

                    // If reached the first character of the link
                    if (isBeginRegion == false)
                    {
                        isBeginRegion = true;

                        linkleft = currentCharInfo.bottomLeft.x;

                        // If the link is one character
                        if (linkInfo.linkTextLength == 1)
                        {
                            isBeginRegion = false;

                            Rect currentRectangle = new Rect(linkleft, m_TargetTextElement.layout.height - maxAscender,
                                currentCharInfo.topRight.x - linkleft, Math.Abs(maxAscender - minDescender));
                            currentRectangle = ApplyOffset(currentRectangle);
                            DrawRectangle(mgc, currentRectangle, Color.cyan);
                        }
                    }

                    // If reached last character of the link
                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        Rect currentRectangle = new Rect(linkleft, m_TargetTextElement.layout.height - maxAscender,
                            currentCharInfo.topRight.x - linkleft, Math.Abs(maxAscender - minDescender));
                        currentRectangle = ApplyOffset(currentRectangle);
                        DrawRectangle(mgc, currentRectangle, Color.cyan);
                    }

                    // If the link is split on more than one line
                    else if (isBeginRegion && currentLine != textInfo.textElementInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        Rect currentRectangle = new Rect(linkleft, m_TargetTextElement.layout.height - maxAscender,
                            currentCharInfo.topRight.x - linkleft, Math.Abs(maxAscender - minDescender));
                        currentRectangle = ApplyOffset(currentRectangle);
                        DrawRectangle(mgc, currentRectangle, Color.cyan);

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                    }
                }
            }
        }

        private void DrawLines(MeshGenerationContext mgc)
        {
            var textInfo = m_TargetTextElement.uitkTextHandle.textInfo;
            int lineCount = textInfo.lineCount;

            for (int i = 0; i < lineCount; i++)
            {
                var lineInfo = textInfo.lineInfo[i];
                var firstCharacterInfo = textInfo.textElementInfo[lineInfo.firstCharacterIndex];
                var lastCharacterInfo = textInfo.textElementInfo[lineInfo.lastCharacterIndex - 1];

                float lineLeft = firstCharacterInfo.bottomLeft.x;
                float lineRight = lastCharacterInfo.topRight.x;

                float ascentline = lineInfo.ascender;
                float baseline = lineInfo.baseline;
                float descentline = lineInfo.descender;

                // Draw line bounds
                Rect currentRectangle = new Rect(lineInfo.lineExtents.min.x,
                    m_TargetTextElement.layout.height - lineInfo.lineExtents.max.y,
                    (lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x),
                    Math.Abs((float)(lineInfo.lineExtents.max.y - lineInfo.lineExtents.min.y)));

                currentRectangle = ApplyOffset(currentRectangle);
                DrawDottedRectangle(mgc, currentRectangle, m_DottedRectSpaceSize, Color.green);

                // Draw ascent line
                Vector2 asc_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - ascentline);
                Vector2 asc_end = new Vector2(lineRight, m_TargetTextElement.layout.height - ascentline);
                asc_start = ApplyOffset(asc_start);
                asc_end = ApplyOffset(asc_end);

                Rect ascentLine = new Rect(asc_start.x, asc_start.y,
                    asc_end.x - asc_start.x, 1);
                DrawHorizontalDottedLine(mgc, ascentLine, m_DottedLineSpaceSize, Color.yellow);


                // Draw descent line
                Vector2 desc_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - descentline);
                Vector2 desc_end = new Vector2(lineRight, m_TargetTextElement.layout.height - descentline);
                desc_start = ApplyOffset(desc_start);
                desc_end = ApplyOffset(desc_end);

                Rect descentLine = new Rect(desc_start.x, desc_start.y,
                    desc_end.x - desc_start.x, 1);
                DrawHorizontalDottedLine(mgc, descentLine, m_DottedLineSpaceSize, Color.yellow);

                // Draw baseline
                Vector2 baseline_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - baseline);
                Vector2 baseline_end = new Vector2(lineRight, m_TargetTextElement.layout.height - baseline);
                baseline_start = ApplyOffset(baseline_start);
                baseline_end = ApplyOffset(baseline_end);

                Rect baseLine = new Rect(baseline_start.x, baseline_end.y,
                    baseline_end.x - baseline_start.x, 1);
                DrawHorizontalDottedLine(mgc, baseLine, m_DottedLineSpaceSize, Color.yellow);

                                // FaceInfo faceInfo = currentChar.fontAsset.faceInfo;
                float capline = firstCharacterInfo.fontAsset == null
                    ? 0
                    : baseline + firstCharacterInfo.fontAsset.faceInfo.capLine * firstCharacterInfo.scale;
                float meanline = firstCharacterInfo.fontAsset == null
                    ? 0
                    : baseline + firstCharacterInfo.fontAsset.faceInfo.meanLine * firstCharacterInfo.scale;

                Vector2 cap_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - capline);
                Vector2 cap_end = new Vector2(lineRight, m_TargetTextElement.layout.height - capline);

                Vector2 mean_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - meanline);
                Vector2 mean_end = new Vector2(lineRight, m_TargetTextElement.layout.height - meanline);

                // Draw cap line
                cap_start = ApplyOffset(cap_start);
                cap_end = ApplyOffset(cap_end);
                Rect capLine = new Rect(cap_start.x, cap_start.y, cap_end.x - cap_start.x, 1);

                DrawHorizontalDottedLine(mgc, capLine, m_DottedLineSpaceSize, Color.cyan);

                // Draw mean line
                mean_start = ApplyOffset(mean_start);
                mean_end = ApplyOffset(mean_end);
                Rect meanLine = new Rect(mean_start.x, mean_start.y,
                    mean_end.x - mean_start.x, 1);
                DrawHorizontalDottedLine(mgc, meanLine, m_DottedLineSpaceSize, Color.cyan);

                // Draw labels if zoomed in to a certain scale
                if (m_TargetTextElement.parent.resolvedStyle.scale.value.x > 2)
                {
                    DrawLabelForLine(mgc, "Ascent Line", asc_start);
                    DrawLabelForLine(mgc, "Descent Line", desc_start);
                    DrawLabelForLine(mgc, "Base Line", baseline_start);
                    DrawLabelForLine(mgc, "Cap Line", cap_start);
                    DrawLabelForLine(mgc, "Mean Line", mean_start);
                }
            }
        }

        private Vector2 ApplyOffset(Vector2 point)
        {
            var style = m_TargetTextElement.resolvedStyle;
            var x_offset = style.paddingLeft + style.borderLeftWidth;
            var y_offset = style.paddingBottom + style.borderBottomWidth;

            point.x += x_offset;
            point.y -= y_offset;

            return point;
        }

        // This is to transform a rectangle to the coordinate system of text info overlay
        private Rect ApplyOffset(Rect rectangle)
        {
            var style = m_TargetTextElement.resolvedStyle;
            var x_offset = style.paddingLeft + style.borderLeftWidth;
            var y_offset = style.paddingBottom + style.borderBottomWidth;

            rectangle.x += x_offset;
            rectangle.y -= y_offset;

            return rectangle;
        }

        private void DrawDottedRectangle(MeshGenerationContext mgc, Rect rectangle, float size, Color color)
        {
            Rect top = new Rect(rectangle.xMin, rectangle.yMax,
                rectangle.width, m_LineThickness);
            Rect bottom = new Rect(rectangle.xMin, rectangle.yMin,
                rectangle.width, m_LineThickness);
            Rect left = new Rect(rectangle.xMin, rectangle.yMin,
                m_LineThickness, rectangle.height);
            Rect right = new Rect(rectangle.xMax, rectangle.yMin,
                m_LineThickness, rectangle.height);

            DrawHorizontalDottedLine(mgc, top, size, color);
            DrawHorizontalDottedLine(mgc, bottom, size, color);
            DrawVerticalDottedLine(mgc, left, size, color);
            DrawVerticalDottedLine(mgc, right, size, color);
        }

        private void DrawHorizontalDottedLine(MeshGenerationContext mgc, Rect rectangle, float size, Color color)
        {
            float start = rectangle.xMin;
            float end = start + size;

            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            while (end <= rectangle.xMax)
            {
                Rect segmentRect = new Rect(start, rectangle.y, size, m_LineThickness);
                var segment = MeshGenerator.RectangleParams.MakeSolid(segmentRect, color, playModeTintColor);
                mgc.meshGenerator.DrawRectangle(segment);
                start = end + size;
                end = start + size;
            }

            // Make sure the last line segment always ends at the right of the rectangle
            if (start < rectangle.xMax)
            {
                Rect segmentRect = new Rect(start, rectangle.y, rectangle.xMax - start, m_LineThickness);
                var segment = MeshGenerator.RectangleParams.MakeSolid(segmentRect, color, playModeTintColor);
                mgc.meshGenerator.DrawRectangle(segment);
            }
            else
            {
                Rect segmentRect = new Rect(start - size, rectangle.y, rectangle.xMax - start + size, m_LineThickness);
                var segment =
                    MeshGenerator.RectangleParams.MakeSolid(segmentRect, color, playModeTintColor);
                mgc.meshGenerator.DrawRectangle(segment);
            }
        }

        private void DrawVerticalDottedLine(MeshGenerationContext mgc, Rect rectangle, float size, Color color)
        {
            float start = rectangle.yMin;
            float end = start + size;

            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            while (end <= rectangle.yMax)
            {
                Rect segmentRect = new Rect(rectangle.x, start, m_LineThickness, size);
                var segment =
                    MeshGenerator.RectangleParams.MakeSolid(segmentRect, color, playModeTintColor);
                mgc.meshGenerator.DrawRectangle(segment);
                start = end + size;
                end = start + size;
            }

            // Make sure the last line segment always ends at the bottom of the rectangle
            if (start < rectangle.yMax)
            {
                Rect segmentRect = new Rect(rectangle.x, start, m_LineThickness, rectangle.yMax - start);
                var segment =
                    MeshGenerator.RectangleParams.MakeSolid(segmentRect, color, playModeTintColor);
                mgc.meshGenerator.DrawRectangle(segment);
            }
            else
            {
                Rect segmentRect = new Rect(rectangle.x, start - size, m_LineThickness, rectangle.yMax - start + size);
                var segment =
                    MeshGenerator.RectangleParams.MakeSolid(segmentRect, color, playModeTintColor);
                mgc.meshGenerator.DrawRectangle(segment);
            }
        }

        private void DrawCrossHair(MeshGenerationContext mgc, float x, float y, float size, Color color)
        {
            Rect verticalRect = new Rect(x - m_LineThickness / 2, y - size / 4, m_LineThickness, size / 2);
            Rect horizontalRect = new Rect(x - size/4, y - m_LineThickness / 2, size / 2, m_LineThickness);

            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var verticalLine = MeshGenerator.RectangleParams.MakeSolid(verticalRect, color, playModeTintColor);
            var horizontalLine = MeshGenerator.RectangleParams.MakeSolid(horizontalRect, color, playModeTintColor);

            mgc.meshGenerator.DrawRectangle(verticalLine);
            mgc.meshGenerator.DrawRectangle(horizontalLine);
        }

        private void DrawRectangle(MeshGenerationContext mgc, Rect rectangle, Color color)
        {
            var border = new MeshGenerator.BorderParams()
            {
                rect = rectangle,
                playmodeTintColor = playModeTintColor,

                leftColor = color,
                topColor = color,
                rightColor = color,
                bottomColor = color,

                leftWidth = m_LineThickness,
                topWidth = m_LineThickness,
                rightWidth = m_LineThickness,
                bottomWidth = m_LineThickness,
            };

            mgc.meshGenerator.DrawBorder(border);
        }

        // Draw label with the same style as in TMP
        private void DrawLabelForLine(MeshGenerationContext mgc, String name, Vector2 start)
        {
            mgc.DrawText(name, new Vector2(start.x, start.y), m_LabelFontSize, m_LabelColor, m_RobotoMonoRegular);
        }
    }
}
