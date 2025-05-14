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
                    m_TargetTextElement.uitkTextHandle.RemoveTextInfoFromPermanentCache();
                    wasInPermanentCache = false;
                }


                m_TargetTextElement = value;


                if (m_TargetTextElement != null)
                {
                    wasInPermanentCache = m_TargetTextElement.uitkTextHandle.IsCachedPermanent;
                    if(!wasInPermanentCache)
                        m_TargetTextElement.uitkTextHandle.AddTextInfoToPermanentCache();
                }

                DrawTextInfo();
            }
        }

        private const float k_SmallVisibleSize = 10;
        private const float k_LabelFontSize = 10;

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

            if (m_TargetTextElement == null || m_TargetTextElement.uitkTextHandle.useAdvancedText)
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
            int characterCount = m_TargetTextElement.uitkTextHandle.GetTextElementCount();

            for (int i = 0; i < characterCount; i++)
            {
                var currentChar = m_TargetTextElement.uitkTextHandle.GetScaledCharacterMetrics(i);

                //TODO: check if visible: maxVisibleChar, maxVisibleLines, firstVisibleChar (not currently implemented in Text Element)

                float origin = currentChar.origin;
                float advance = currentChar.xAdvance;
                float ascentline = currentChar.ascentline;
                float baseline = currentChar.baseline;
                float descentline = currentChar.descentline;
                float height = m_TargetTextElement.layout.height;

                // Draw character bounds
                if (currentChar.isVisible)
                {
                    static Vector2 Flip(Vector2 v, float height)
                    {
                        return new Vector2(v.x, height - v.y);
                    }

                    DrawDottedRectangle(mgc, Flip(currentChar.topLeft,height), Flip(currentChar.topRight,height), Flip(currentChar.bottomRight,height), Flip(currentChar.bottomLeft,height), Color.green);

                    //All lines need to be reworked. I thin the height need to be scaled
                    // Draw ascent line
                    Vector2 asc_start = new Vector2(currentChar.topLeft.x, height - ascentline);
                    Vector2 asc_end = new Vector2(currentChar.topRight.x, height - ascentline);

                    DrawDottedLine(mgc, asc_start, asc_end, Color.yellow);

                    // Draw descent line
                    Vector2 desc_start = new Vector2(currentChar.topLeft.x, height - descentline);
                    Vector2 desc_end = new Vector2(currentChar.topRight.x, height - descentline);
                    DrawDottedLine(mgc, desc_start, desc_end, Color.yellow);

                    // Draw baseline
                    Vector2 baseline_start = new Vector2(currentChar.topLeft.x, height - baseline);
                    Vector2 baseline_end = new Vector2(currentChar.topRight.x, height - baseline);
                    DrawDottedLine(mgc, baseline_start, baseline_end, Color.yellow);

                    float capline = baseline + currentChar.fontCapLine * currentChar.scale;
                    float meanline = baseline + currentChar.fontMeanLine * currentChar.scale;

                    // Draw cap line
                    Vector2 cap_start = new Vector2(currentChar.topLeft.x, height - capline);
                    Vector2 cap_end = new Vector2(currentChar.topRight.x, height - capline);
                    DrawDottedLine(mgc, cap_start, cap_end, Color.cyan);

                    // Draw mean line
                    Vector2 mean_start = new Vector2(currentChar.topLeft.x, height - meanline);
                    Vector2 mean_end = new Vector2(currentChar.topRight.x, height - meanline);
                    DrawDottedLine(mgc, mean_start, mean_end, Color.cyan);

                    // Draw labels if zoomed in to a certain scale
                    // The check was only considering the scale of the parent, so it was bad in most cases.
                    // the color of the label is not too distracting even if the label is too small to read.
                    //if (m_TargetTextElement.parent.resolvedStyle.scale.value.x > 2)
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
                    Rect currentRect = new Rect(origin, height - ascentline,
                        Math.Abs(whiteSpaceAdvance - origin), Math.Abs(ascentline - descentline));
                    DrawDottedRectangle(mgc, currentRect, Color.gray);
                }

                // Draw origin
                Vector2 origin_position = new Vector2(origin, height - baseline);
                DrawCrossHair(mgc, origin_position, k_SmallVisibleSize / 2, Color.cyan);

                // Draw horizontal advance
                Vector2 advance_position = new Vector2(advance, m_TargetTextElement.layout.height - baseline);
                DrawCrossHair(mgc, advance_position, k_SmallVisibleSize / 2, Color.yellow);

            }
        }

        private void DrawWords(MeshGenerationContext mgc)
        {
            //TODO user getter method to get the words instead of using textInfo directly
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
                    var currCharInfo = m_TargetTextElement.uitkTextHandle.GetScaledCharacterMetrics(characterIndex);
                    int currentLine = currCharInfo.lineNumber;

                    maxAscender = Mathf.Max(maxAscender, currCharInfo.ascentline);
                    minDescender = Mathf.Min(minDescender, currCharInfo.descentline);

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
                            DrawRectangle(mgc, currentRect, Color.green);
                        }
                    }

                    // If reached last character of the word
                    if (isBeginRegion && j == currWord.characterCount - 1)
                    {
                        isBeginRegion = false;

                        Rect currentRect = new Rect(wordLeft, m_TargetTextElement.layout.height - maxAscender,
                            currCharInfo.topRight.x - wordLeft, Math.Abs(maxAscender - minDescender));
                        DrawRectangle(mgc, currentRect, Color.green);
                    }

                    // If the word is split and will continue on the next line
                    else if (isBeginRegion && currentLine != m_TargetTextElement.uitkTextHandle.GetScaledCharacterMetrics(characterIndex + 1).lineNumber)
                    {
                        isBeginRegion = false;

                        Rect currentRect = new Rect(wordLeft, m_TargetTextElement.layout.height - maxAscender,
                            currCharInfo.topRight.x - wordLeft, Math.Abs(maxAscender - minDescender));
                        DrawRectangle(mgc, currentRect, Color.green);

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                    }
                }
            }
        }

        private void DrawLinks(MeshGenerationContext mgc)
        {
            //TODO link info getter for ATG
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
                    var currentCharInfo = m_TargetTextElement.uitkTextHandle.GetScaledCharacterMetrics(characterIndex);
                    int currentLine = currentCharInfo.lineNumber;

                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascentline);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descentline);


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
                            DrawRectangle(mgc, currentRectangle, Color.cyan);
                        }
                    }

                    // If reached last character of the link
                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        Rect currentRectangle = new Rect(linkleft, m_TargetTextElement.layout.height - maxAscender,
                            currentCharInfo.topRight.x - linkleft, Math.Abs(maxAscender - minDescender));
                        DrawRectangle(mgc, currentRectangle, Color.cyan);
                    }

                    // If the link is split on more than one line
                    else if (isBeginRegion && currentLine != textInfo.textElementInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        Rect currentRectangle = new Rect(linkleft, m_TargetTextElement.layout.height - maxAscender,
                            currentCharInfo.topRight.x - linkleft, Math.Abs(maxAscender - minDescender));
                        DrawRectangle(mgc, currentRectangle, Color.cyan);

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                    }
                }
            }
        }

        private void DrawLines(MeshGenerationContext mgc)
        {
            //TODO LineCount getter for ATG
            var textInfo = m_TargetTextElement.uitkTextHandle.textInfo;
            int lineCount = textInfo.lineCount;

            for (int i = 0; i < lineCount; i++)
            {
                var lineInfo = textInfo.lineInfo[i];
                var firstCharacterInfo = m_TargetTextElement.uitkTextHandle.GetScaledCharacterMetrics(lineInfo.firstCharacterIndex);
                var lastCharacterInfo = m_TargetTextElement.uitkTextHandle.GetScaledCharacterMetrics(lineInfo.lastCharacterIndex - 1);

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

                DrawDottedRectangle(mgc, currentRectangle, Color.green);

                // Draw ascent line
                Vector2 asc_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - ascentline);
                Vector2 asc_end = new Vector2(lineRight, m_TargetTextElement.layout.height - ascentline);
                DrawDottedLine(mgc, asc_start, asc_end, Color.yellow);


                // Draw descent line
                Vector2 desc_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - descentline);
                Vector2 desc_end = new Vector2(lineRight, m_TargetTextElement.layout.height - descentline);
                DrawDottedLine(mgc, desc_start, desc_end, Color.yellow);

                // Draw baseline
                Vector2 baseline_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - baseline);
                Vector2 baseline_end = new Vector2(lineRight, m_TargetTextElement.layout.height - baseline);
                DrawDottedLine(mgc, baseline_start, baseline_end, Color.yellow);

                float capline = baseline + firstCharacterInfo.fontCapLine * firstCharacterInfo.scale;
                float meanline = baseline + firstCharacterInfo.fontMeanLine * firstCharacterInfo.scale;

                Vector2 cap_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - capline);
                Vector2 cap_end = new Vector2(lineRight, m_TargetTextElement.layout.height - capline);

                Vector2 mean_start = new Vector2(lineLeft, m_TargetTextElement.layout.height - meanline);
                Vector2 mean_end = new Vector2(lineRight, m_TargetTextElement.layout.height - meanline);

                // Draw cap line
                DrawDottedLine(mgc, cap_start, cap_end, Color.cyan);

                // Draw mean line
                DrawDottedLine(mgc, mean_start, mean_end, Color.cyan);

                // Draw labels if zoomed in to a certain scale
                if (m_TargetTextElement.parent.transform.scale.x > 2)
                {
                    DrawLabelForLine(mgc, "Ascent Line", asc_start);
                    DrawLabelForLine(mgc, "Descent Line", desc_start);
                    DrawLabelForLine(mgc, "Base Line", baseline_start);
                    DrawLabelForLine(mgc, "Cap Line", cap_start);
                    DrawLabelForLine(mgc, "Mean Line", mean_start);
                }
            }
        }

        private Vector2 GetOffset()
        {
            //adjust the rectangle to account for the padding and border
            var style = m_TargetTextElement.resolvedStyle;
            var x_offset = style.paddingLeft + style.borderLeftWidth;
            var y_offset = style.paddingBottom + style.borderBottomWidth;

            return new Vector2(x_offset, -y_offset);
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
            //TODO : dotted line
            start = m_TargetTextElement.LocalToWorld(start);
            end = m_TargetTextElement.LocalToWorld(end);

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
        private void DrawLabelForLine(MeshGenerationContext mgc, String name, Vector2 start)
        {
            start = m_TargetTextElement.LocalToWorld(start + GetOffset());
            mgc.DrawText(name, new Vector2(start.x, start.y), k_LabelFontSize, m_LabelColor, m_RobotoMonoRegular);
        }
    }
}
