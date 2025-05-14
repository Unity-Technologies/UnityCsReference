// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore.Text
{
    internal partial class TextGenerator
    {
        public void LayoutPhase(TextInfo textInfo, TextGenerationSettings generationSettings, float maxVisibleDescender)
        {
            int lastVertIndex = m_MaterialReferences[m_Underline.materialIndex].referenceCount * 4;

            // Partial clear of the vertices array to mark unused vertices as degenerate.
            textInfo.meshInfo[m_CurrentMaterialIndex].Clear(false);

            // Handle Text Alignment
            #region Text Vertical Alignment

            Vector3 anchorOffset = Vector3.zero;
            Vector3[] corners = m_RectTransformCorners; // GetTextContainerLocalCorners();

            // Handle Vertical Text Alignment
            switch (generationSettings.textAlignment)
            {
                // Top Vertically
                case TextAlignment.TopCenter:
                case TextAlignment.TopLeft:
                case TextAlignment.TopRight:
                case TextAlignment.TopJustified:
                case TextAlignment.TopFlush:
                case TextAlignment.TopGeoAligned:
                    anchorOffset = corners[1] + new Vector3(0 , 0 - m_MaxAscender, 0);
                    break;

                // Middle Vertically
                case TextAlignment.MiddleLeft:
                case TextAlignment.MiddleRight:
                case TextAlignment.MiddleCenter:
                case TextAlignment.MiddleJustified:
                case TextAlignment.MiddleFlush:
                case TextAlignment.MiddleGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0, 0 - (m_MaxAscender + maxVisibleDescender) / 2, 0);
                    break;

                // Bottom Vertically
                case TextAlignment.BottomCenter:
                case TextAlignment.BottomLeft:
                case TextAlignment.BottomRight:
                case TextAlignment.BottomJustified:
                case TextAlignment.BottomFlush:
                case TextAlignment.BottomGeoAligned:
                    anchorOffset = corners[0] + new Vector3(0, 0 - maxVisibleDescender, 0);
                    break;

                // Baseline Vertically
                case TextAlignment.BaselineCenter:
                case TextAlignment.BaselineLeft:
                case TextAlignment.BaselineRight:
                case TextAlignment.BaselineJustified:
                case TextAlignment.BaselineFlush:
                case TextAlignment.BaselineGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0, 0, 0);
                    break;

                // Midline Vertically
                case TextAlignment.MidlineLeft:
                case TextAlignment.MidlineCenter:
                case TextAlignment.MidlineRight:
                case TextAlignment.MidlineJustified:
                case TextAlignment.MidlineFlush:
                case TextAlignment.MidlineGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0, 0 - (m_MeshExtents.max.y + m_MeshExtents.min.y) / 2, 0);
                    break;

                // Capline Vertically
                case TextAlignment.CaplineLeft:
                case TextAlignment.CaplineCenter:
                case TextAlignment.CaplineRight:
                case TextAlignment.CaplineJustified:
                case TextAlignment.CaplineFlush:
                case TextAlignment.CaplineGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0, 0 - (m_MaxCapHeight) / 2, 0);
                    break;
            }
            #endregion

            // Initialization for Second Pass
            Vector3 justificationOffset = Vector3.zero;
            Vector3 offset = Vector3.zero;

            int wordCount = 0;
            int lineCount = 0;
            int lastLine = 0;
            bool isFirstSeperator = false;

            bool isStartOfWord = false;
            int wordFirstChar = 0;
            int wordLastChar = 0;

            // Second Pass : Line Justification, UV Mapping, Character & Line Visibility & more.

            Color32 underlineColor = Color.white;
            Color32 strikethroughColor = Color.white;
            HighlightState highlightState = new HighlightState(new Color32(255, 255, 0, 64), Offset.zero);
            float xScale = 0;
            float xScaleMax = 0;
            float underlineStartScale = 0;
            float underlineEndScale = 0;
            float underlineMaxScale = 0;
            float underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;

            float strikethroughPointSize = 0;
            float strikethroughScale = 0;
            float strikethroughBaseline = 0;

            bool beginUnderline = false;
            Vector3 underlineStart = Vector3.zero; // Used to track where underline starts & ends.
            Vector3 underlineEnd = Vector3.zero;

            bool beginStrikethrough = false;
            Vector3 strikethroughStart = Vector3.zero;
            Vector3 strikethroughEnd = Vector3.zero;

            bool beginHighlight = false;
            Vector3 highlightStart = Vector3.zero;
            Vector3 highlightEnd = Vector3.zero;

            TextElementInfo[] textElementInfos = textInfo.textElementInfo;

            #region Handle Line Justification & UV Mapping & Character Visibility & More

            for (int i = 0; i < m_CharacterCount; i++)
            {
                FontAsset currentFontAsset = textElementInfos[i].fontAsset;

                char unicode = (char)textElementInfos[i].character;
                bool isWhiteSpace = char.IsWhiteSpace(unicode);

                int currentLine = textElementInfos[i].lineNumber;
                LineInfo lineInfo = textInfo.lineInfo[currentLine];
                lineCount = currentLine + 1;

                TextAlignment lineAlignment = lineInfo.alignment;

                // Process Line Justification
                #region Handle Line Justification

                switch (lineAlignment)
                {
                    case TextAlignment.TopLeft:
                    case TextAlignment.MiddleLeft:
                    case TextAlignment.BottomLeft:
                    case TextAlignment.BaselineLeft:
                    case TextAlignment.MidlineLeft:
                    case TextAlignment.CaplineLeft:
                        if (!generationSettings.isRightToLeft)
                            justificationOffset = new Vector3(0 + lineInfo.marginLeft, 0, 0);
                        else
                            justificationOffset = new Vector3(0 - lineInfo.maxAdvance, 0, 0);
                        break;

                    case TextAlignment.TopCenter:
                    case TextAlignment.MiddleCenter:
                    case TextAlignment.BottomCenter:
                    case TextAlignment.BaselineCenter:
                    case TextAlignment.MidlineCenter:
                    case TextAlignment.CaplineCenter:
                        justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width / 2 - lineInfo.maxAdvance / 2, 0, 0);
                        break;

                    case TextAlignment.TopGeoAligned:
                    case TextAlignment.MiddleGeoAligned:
                    case TextAlignment.BottomGeoAligned:
                    case TextAlignment.BaselineGeoAligned:
                    case TextAlignment.MidlineGeoAligned:
                    case TextAlignment.CaplineGeoAligned:
                        justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width / 2 - (lineInfo.lineExtents.min.x + lineInfo.lineExtents.max.x) / 2, 0, 0);
                        break;

                    case TextAlignment.TopRight:
                    case TextAlignment.MiddleRight:
                    case TextAlignment.BottomRight:
                    case TextAlignment.BaselineRight:
                    case TextAlignment.MidlineRight:
                    case TextAlignment.CaplineRight:
                        if (!generationSettings.isRightToLeft)
                            justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width - lineInfo.maxAdvance, 0, 0);
                        else
                            justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);
                        break;

                    case TextAlignment.TopJustified:
                    case TextAlignment.MiddleJustified:
                    case TextAlignment.BottomJustified:
                    case TextAlignment.BaselineJustified:
                    case TextAlignment.MidlineJustified:
                    case TextAlignment.CaplineJustified:
                    case TextAlignment.TopFlush:
                    case TextAlignment.MiddleFlush:
                    case TextAlignment.BottomFlush:
                    case TextAlignment.BaselineFlush:
                    case TextAlignment.MidlineFlush:
                    case TextAlignment.CaplineFlush:
                        // Skip Zero Width Characters and spaces outside of the margins.
                        if (i > lineInfo.lastVisibleCharacterIndex || unicode == 0x0A || unicode == k_SoftHyphen || unicode == k_ZeroWidthSpace || unicode == k_WordJoiner || unicode == k_EndOfText) break;

                        char lastCharOfCurrentLine = (char)textElementInfos[lineInfo.lastCharacterIndex].character;

                        bool isFlush = ((HorizontalAlignment)lineAlignment & HorizontalAlignment.Flush) == HorizontalAlignment.Flush;

                        // In Justified mode, all lines are justified except the last one.
                        // In Flush mode, all lines are justified.
                        if (char.IsControl(lastCharOfCurrentLine) == false && currentLine < m_LineNumber || isFlush || lineInfo.maxAdvance > lineInfo.width)
                        {
                            // First character of each line.
                            if (currentLine != lastLine || i == 0 || i == TextGenerationSettings.firstVisibleCharacter)
                            {
                                if (!generationSettings.isRightToLeft)
                                    justificationOffset = new Vector3(lineInfo.marginLeft, 0, 0);
                                else
                                    justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);

                                if (char.IsSeparator(unicode))
                                    isFirstSeperator = true;
                                else
                                    isFirstSeperator = false;
                            }
                            else
                            {
                                float gap = generationSettings.isRightToLeft ? lineInfo.width + lineInfo.maxAdvance : lineInfo.width - lineInfo.maxAdvance;
                                int visibleCount = lineInfo.visibleCharacterCount - 1 + lineInfo.controlCharacterCount;
                                int spaces = lineInfo.visibleSpaceCount - lineInfo.controlCharacterCount;

                                if (isFirstSeperator) { spaces -= 1; visibleCount += 1; }

                                float ratio = spaces > 0 ? TextGenerationSettings.wordWrappingRatio : 1;

                                if (spaces < 1) spaces = 1;

                                if (unicode != k_NoBreakSpace && (unicode == k_Tab || char.IsSeparator(unicode)))
                                {
                                    if (!generationSettings.isRightToLeft)
                                        justificationOffset += new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                    else
                                        justificationOffset -= new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                }
                                else
                                {
                                    if (!generationSettings.isRightToLeft)
                                        justificationOffset += new Vector3(gap * ratio / visibleCount, 0, 0);
                                    else
                                        justificationOffset -= new Vector3(gap * ratio / visibleCount, 0, 0);
                                }
                            }
                        }
                        else
                        {
                            if (!generationSettings.isRightToLeft)
                                justificationOffset = new Vector3(lineInfo.marginLeft, 0, 0); // Keep last line left justified.
                            else
                                justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0); // Keep last line right justified.
                        }

                        //Debug.Log("Char [" + (char)charCode + "] Code:" + charCode + "  Line # " + currentLine + "  Offset:" + justificationOffset + "  # Spaces:" + lineInfo.spaceCount + "  # Characters:" + lineInfo.characterCount);
                        break;
                }

                #endregion End Text Justification

                offset = anchorOffset + justificationOffset;
                offset = new(Round(offset.x), Round(offset.y));

                // Handle UV2 mapping options and packing of scale information into UV2.
                #region Handling of UV2 mapping & Scale packing

                bool isCharacterVisible = textElementInfos[i].isVisible;
                if (isCharacterVisible)
                {
                    TextElementType elementType = textElementInfos[i].elementType;
                    switch (elementType)
                    {
                        // CHARACTERS
                        case TextElementType.Character:
                            Extents lineExtents = lineInfo.lineExtents;

                            // Setup UV2 based on Character Mapping Options Selected

                            #region Handle UV Mapping Options


                            textElementInfos[i].vertexBottomLeft.uv2.x = 0;
                            textElementInfos[i].vertexTopLeft.uv2.x = 0;
                            textElementInfos[i].vertexTopRight.uv2.x = 1;
                            textElementInfos[i].vertexBottomRight.uv2.x = 1;

                            textElementInfos[i].vertexBottomLeft.uv2.y = 0;
                            textElementInfos[i].vertexTopLeft.uv2.y = 1;
                            textElementInfos[i].vertexTopRight.uv2.y = 1;
                            textElementInfos[i].vertexBottomRight.uv2.y = 0;


                            #endregion

                            // Pack UV's so that we can pass Xscale needed for Shader to maintain 1:1 ratio.
                            #region Pack Scale into UV2

                            xScale = textElementInfos[i].scale * (1 - m_CharWidthAdjDelta) * 1; // generationSettings.scale;
                            if (!textElementInfos[i].isUsingAlternateTypeface && (textElementInfos[i].style & FontStyles.Bold) == FontStyles.Bold) xScale *= -1;

                            // Set SDF Scale
                            textElementInfos[i].vertexBottomLeft.uv.w = xScale;
                            textElementInfos[i].vertexTopLeft.uv.w = xScale;
                            textElementInfos[i].vertexTopRight.uv.w = xScale;
                            textElementInfos[i].vertexBottomRight.uv.w = xScale;

                            // TODO: To revise the code below. Right now, it is required by UITK to handle bold styling, while in TMP is not necessary.
                            // Optimization to avoid having a vector2 returned from the Pack UV function.
                            textElementInfos[i].vertexBottomLeft.uv2.x = 1;
                            textElementInfos[i].vertexBottomLeft.uv2.y = xScale;
                            textElementInfos[i].vertexTopLeft.uv2.x = 1;
                            textElementInfos[i].vertexTopLeft.uv2.y = xScale;
                            textElementInfos[i].vertexTopRight.uv2.x = 1;
                            textElementInfos[i].vertexTopRight.uv2.y = xScale;
                            textElementInfos[i].vertexBottomRight.uv2.x = 1;
                            textElementInfos[i].vertexBottomRight.uv2.y = xScale;

                            #endregion

                            break;

                        // SPRITES
                        case TextElementType.Sprite:
                            // Nothing right now
                            break;
                    }

                    // Handle maxVisibleCharacters, maxVisibleLines and Overflow Page Mode.
                    #region Handle maxVisibleCharacters / maxVisibleLines / Page Mode

                    if (i < TextGenerationSettings.maxVisibleCharacters && wordCount < TextGenerationSettings.maxVisibleWords && currentLine < TextGenerationSettings.maxVisibleLines)
                    {
                        textElementInfos[i].vertexBottomLeft.position += offset;
                        textElementInfos[i].vertexTopLeft.position += offset;
                        textElementInfos[i].vertexTopRight.position += offset;
                        textElementInfos[i].vertexBottomRight.position += offset;
                    }
                    else
                    {
                        textElementInfos[i].vertexBottomLeft.position = Vector3.zero;
                        textElementInfos[i].vertexTopLeft.position = Vector3.zero;
                        textElementInfos[i].vertexTopRight.position = Vector3.zero;
                        textElementInfos[i].vertexBottomRight.position = Vector3.zero;
                        textElementInfos[i].isVisible = false;
                    }
                    #endregion


                    // Fill Vertex Buffers for the various types of element
                    if (elementType == TextElementType.Character)
                    {
                        TextGeneratorUtilities.FillCharacterVertexBuffers(i, generationSettings.shouldConvertToLinearSpace, generationSettings, textInfo, NeedToRound);
                    }
                    else if (elementType == TextElementType.Sprite)
                    {
                        TextGeneratorUtilities.FillSpriteVertexBuffers(i, generationSettings.shouldConvertToLinearSpace, generationSettings, textInfo);
                    }
                }
                #endregion


                // Apply Alignment and Justification Offset
                textInfo.textElementInfo[i].bottomLeft += offset;
                textInfo.textElementInfo[i].topLeft += offset;
                textInfo.textElementInfo[i].topRight += offset;
                textInfo.textElementInfo[i].bottomRight += offset;

                textInfo.textElementInfo[i].origin += offset.x;
                textInfo.textElementInfo[i].xAdvance += offset.x;

                textInfo.textElementInfo[i].ascender += offset.y;
                textInfo.textElementInfo[i].descender += offset.y;
                textInfo.textElementInfo[i].baseLine += offset.y;

                // Update MeshExtents
                if (isCharacterVisible)
                {
                    //m_MeshExtents.min = new Vector2(Mathf.Min(m_MeshExtents.min.x, textInfo.textElementInfo[i].bottomLeft.x), Mathf.Min(m_MeshExtents.min.y, textInfo.textElementInfo[i].bottomLeft.y));
                    //m_MeshExtents.max = new Vector2(Mathf.Max(m_MeshExtents.max.x, textInfo.textElementInfo[i].topRight.x), Mathf.Max(m_MeshExtents.max.y, textInfo.textElementInfo[i].topLeft.y));
                }

                // Need to recompute lineExtent to account for the offset from justification.
                #region Adjust lineExtents resulting from alignment offset

                if (currentLine != lastLine || i == m_CharacterCount - 1)
                {
                    // Update the previous line's extents
                    if (currentLine != lastLine)
                    {
                        var lastCharacterIndex =
                          (generationSettings.textWrappingMode == TextWrappingMode.PreserveWhitespace || generationSettings.textWrappingMode == TextWrappingMode.PreserveWhitespaceNoWrap)
                          ? textInfo.lineInfo[lastLine].lastCharacterIndex : textInfo.lineInfo[lastLine].lastVisibleCharacterIndex;
                        textInfo.lineInfo[lastLine].baseline += offset.y;
                        textInfo.lineInfo[lastLine].ascender += offset.y;
                        textInfo.lineInfo[lastLine].descender += offset.y;

                        textInfo.lineInfo[lastLine].maxAdvance += offset.x;

                        textInfo.lineInfo[lastLine].lineExtents.min = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[lastLine].firstCharacterIndex].bottomLeft.x, textInfo.lineInfo[lastLine].descender);
                        textInfo.lineInfo[lastLine].lineExtents.max = new Vector2(textInfo.textElementInfo[lastCharacterIndex].topRight.x, textInfo.lineInfo[lastLine].ascender);
                    }

                    // Update the current line's extents
                    if (i == m_CharacterCount - 1)
                    {
                        var lastCharacterIndex =
                         (generationSettings.textWrappingMode == TextWrappingMode.PreserveWhitespace || generationSettings.textWrappingMode == TextWrappingMode.PreserveWhitespaceNoWrap)
                         ? textInfo.lineInfo[currentLine].lastCharacterIndex : textInfo.lineInfo[currentLine].lastVisibleCharacterIndex;
                        textInfo.lineInfo[currentLine].baseline += offset.y;
                        textInfo.lineInfo[currentLine].ascender += offset.y;
                        textInfo.lineInfo[currentLine].descender += offset.y;

                        textInfo.lineInfo[currentLine].maxAdvance += offset.x;

                        textInfo.lineInfo[currentLine].lineExtents.min = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[currentLine].firstCharacterIndex].bottomLeft.x, textInfo.lineInfo[currentLine].descender);
                        textInfo.lineInfo[currentLine].lineExtents.max = new Vector2(textInfo.textElementInfo[lastCharacterIndex].topRight.x, textInfo.lineInfo[currentLine].ascender);
                    }
                }
                #endregion


                // Track Word Count per line and for the object
                #region Track Word Count

                if (char.IsLetterOrDigit(unicode) || unicode == k_HyphenMinus || unicode == k_SoftHyphen || unicode == k_Hyphen || unicode == k_NonBreakingHyphen)
                {
                    if (isStartOfWord == false)
                    {
                        isStartOfWord = true;
                        wordFirstChar = i;
                    }

                    // If last character is a word
                    if (isStartOfWord && i == m_CharacterCount - 1)
                    {
                        int size = textInfo.wordInfo.Length;
                        int index = textInfo.wordCount;

                        if (textInfo.wordCount + 1 > size)
                            TextInfo.Resize(ref textInfo.wordInfo, size + 1);

                        wordLastChar = i;

                        textInfo.wordInfo[index].firstCharacterIndex = wordFirstChar;
                        textInfo.wordInfo[index].lastCharacterIndex = wordLastChar;
                        textInfo.wordInfo[index].characterCount = wordLastChar - wordFirstChar + 1;

                        wordCount += 1;
                        textInfo.wordCount += 1;
                        textInfo.lineInfo[currentLine].wordCount += 1;
                    }
                }
                else if (isStartOfWord || i == 0 && (!char.IsPunctuation(unicode) || isWhiteSpace || unicode == k_ZeroWidthSpace || i == m_CharacterCount - 1))
                {
                    if (i > 0 && i < textElementInfos.Length - 1 && i < m_CharacterCount && (unicode == k_SingleQuote || unicode == k_RightSingleQuote) && char.IsLetterOrDigit((char)textElementInfos[i - 1].character) && char.IsLetterOrDigit((char)textElementInfos[i + 1].character)) { }
                    else
                    {
                        wordLastChar = i == m_CharacterCount - 1 && char.IsLetterOrDigit(unicode) ? i : i - 1;
                        isStartOfWord = false;

                        int size = textInfo.wordInfo.Length;
                        int index = textInfo.wordCount;

                        if (textInfo.wordCount + 1 > size)
                            TextInfo.Resize(ref textInfo.wordInfo, size + 1);

                        textInfo.wordInfo[index].firstCharacterIndex = wordFirstChar;
                        textInfo.wordInfo[index].lastCharacterIndex = wordLastChar;
                        textInfo.wordInfo[index].characterCount = wordLastChar - wordFirstChar + 1;

                        wordCount += 1;
                        textInfo.wordCount += 1;
                        textInfo.lineInfo[currentLine].wordCount += 1;
                    }
                }
                #endregion

                // Setup & Handle Underline
                #region Underline

                // NOTE: Need to figure out how underline will be handled with multiple fonts and which font will be used for the underline.
                bool isUnderline = (textInfo.textElementInfo[i].style & FontStyles.Underline) == FontStyles.Underline;
                if (isUnderline)
                {
                    bool isUnderlineVisible = true;
                    textInfo.textElementInfo[i].underlineVertexIndex = lastVertIndex;

                    if (i > TextGenerationSettings.maxVisibleCharacters || currentLine > TextGenerationSettings.maxVisibleLines)
                        isUnderlineVisible = false;

                    // We only use the scale of visible characters.
                    if (!isWhiteSpace && unicode != k_ZeroWidthSpace)
                    {
                        underlineMaxScale = Mathf.Max(underlineMaxScale, textInfo.textElementInfo[i].scale);
                        xScaleMax = Mathf.Max(xScaleMax, Mathf.Abs(xScale));
                        underlineBaseLine = Mathf.Min( underlineBaseLine, textInfo.textElementInfo[i].baseLine + currentFontAsset.faceInfo.underlineOffset * underlineMaxScale);
                    }

                    if (beginUnderline == false && isUnderlineVisible == true && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode)) { }
                        else
                        {
                            beginUnderline = true;
                            underlineStartScale = textInfo.textElementInfo[i].scale;
                            if (underlineMaxScale == 0)
                            {
                                underlineMaxScale = underlineStartScale;
                                xScaleMax = xScale;
                            }

                            underlineStart = new Vector3(textInfo.textElementInfo[i].bottomLeft.x, underlineBaseLine, 0);
                            underlineColor = textInfo.textElementInfo[i].underlineColor;
                        }
                    }

                    // End Underline if text only contains one character.
                    if (beginUnderline && m_CharacterCount == 1)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        // Terminate underline at previous visible character if space or carriage return.
                        if (isWhiteSpace || unicode == k_ZeroWidthSpace)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            underlineEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = textInfo.textElementInfo[lastVisibleCharacterIndex].scale;
                        }
                        else
                        {
                            // End underline if last character of the line.
                            underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = textInfo.textElementInfo[i].scale;
                        }

                        beginUnderline = false;
                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && !isUnderlineVisible)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i - 1].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && i < m_CharacterCount - 1 && !ColorUtilities.CompareColors(underlineColor, textInfo.textElementInfo[i + 1].underlineColor))
                    {
                        // End underline if underline color has changed.
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                }
                else
                {
                    // End Underline
                    if (beginUnderline == true)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i - 1].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                }
                #endregion


                // Setup & Handle Strikethrough
                #region Strikethrough

                // NOTE: Need to figure out how underline will be handled with multiple fonts and which font will be used for the underline.
                bool isStrikethrough = (textInfo.textElementInfo[i].style & FontStyles.Strikethrough) == FontStyles.Strikethrough;
                float strikethroughOffset = currentFontAsset.faceInfo.strikethroughOffset;

                if (isStrikethrough)
                {
                    bool isStrikeThroughVisible = true;
                    textInfo.textElementInfo[i].strikethroughVertexIndex = m_MaterialReferences[m_Underline.materialIndex].referenceCount * 4;

                    if (i > TextGenerationSettings.maxVisibleCharacters || currentLine > TextGenerationSettings.maxVisibleLines)
                        isStrikeThroughVisible = false;

                    if (beginStrikethrough == false && isStrikeThroughVisible && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode)) { }
                        else
                        {
                            beginStrikethrough = true;
                            strikethroughPointSize = textInfo.textElementInfo[i].pointSize;
                            strikethroughScale = textInfo.textElementInfo[i].scale;
                            strikethroughStart = new Vector3(textInfo.textElementInfo[i].bottomLeft.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                            strikethroughColor = textInfo.textElementInfo[i].strikethroughColor;
                            strikethroughBaseline = textInfo.textElementInfo[i].baseLine;
                        }
                    }

                    // End Strikethrough if text only contains one character.
                    if (beginStrikethrough && m_CharacterCount == 1)
                    {
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i == lineInfo.lastCharacterIndex)
                    {
                        // Terminate Strikethrough at previous visible character if space or carriage return.
                        if (isWhiteSpace || unicode == k_ZeroWidthSpace)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, textInfo.textElementInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }
                        else
                        {
                            // Terminate Strikethrough at last character of line.
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }

                        beginStrikethrough = false;
                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i < m_CharacterCount && (textInfo.textElementInfo[i + 1].pointSize != strikethroughPointSize || !TextGeneratorUtilities.Approximately(textInfo.textElementInfo[i + 1].baseLine + offset.y, strikethroughBaseline)))
                    {
                        // Terminate Strikethrough if scale changes.
                        beginStrikethrough = false;

                        int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                        if (i > lastVisibleCharacterIndex)
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, textInfo.textElementInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        else
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i < m_CharacterCount && currentFontAsset.GetHashCode() != textElementInfos[i + 1].fontAsset.GetHashCode())
                    {
                        // Terminate Strikethrough if font asset changes.
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && !isStrikeThroughVisible)
                    {
                        // Terminate Strikethrough if character is not visible.
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, textInfo.textElementInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                }
                else
                {
                    // End Strikethrough
                    if (beginStrikethrough == true)
                    {
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, textInfo.textElementInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                }
                #endregion

                // HANDLE TEXT HIGHLIGHTING
                #region Text Highlighting

                bool isHighlight = (textInfo.textElementInfo[i].style & FontStyles.Highlight) == FontStyles.Highlight;
                if (isHighlight)
                {
                    bool isHighlightVisible = true;

                    if (i > TextGenerationSettings.maxVisibleCharacters || currentLine > TextGenerationSettings.maxVisibleLines)
                        isHighlightVisible = false;

                    if (beginHighlight == false && isHighlightVisible == true && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode)) { }
                        else
                        {
                            beginHighlight = true;
                            highlightStart = TextGeneratorUtilities.largePositiveVector2;
                            highlightEnd = TextGeneratorUtilities.largeNegativeVector2;
                            highlightState = textInfo.textElementInfo[i].highlightState;
                        }
                    }

                    if (beginHighlight)
                    {
                        TextElementInfo currentCharacter = textInfo.textElementInfo[i];
                        HighlightState currentState = currentCharacter.highlightState;

                        bool isColorTransition = false;

                        // Handle Highlight color changes
                        if (highlightState != currentState)
                        {
                            // Adjust previous highlight section to prevent a gaps between sections.
                            if (isWhiteSpace)
                                highlightEnd.x = (highlightEnd.x - highlightState.padding.right + currentCharacter.origin) / 2;
                            else
                                highlightEnd.x = (highlightEnd.x - highlightState.padding.right + currentCharacter.bottomLeft.x) / 2;

                            highlightStart.y = Mathf.Min(highlightStart.y, currentCharacter.descender);
                            highlightEnd.y = Mathf.Max(highlightEnd.y, currentCharacter.ascender);

                            DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);

                            beginHighlight = true;
                            highlightStart = new Vector2(highlightEnd.x, currentCharacter.descender - currentState.padding.bottom);

                            if (isWhiteSpace)
                                highlightEnd = new Vector2(currentCharacter.xAdvance + currentState.padding.right, currentCharacter.ascender + currentState.padding.top);
                            else
                                highlightEnd = new Vector2(currentCharacter.topRight.x + currentState.padding.right, currentCharacter.ascender + currentState.padding.top);

                            highlightState = currentState;

                            isColorTransition = true;
                        }

                        if (!isColorTransition)
                        {
                            if (isWhiteSpace)
                            {
                                // Use the Min / Max of glyph metrics if white space.
                                highlightStart.x = Mathf.Min(highlightStart.x, currentCharacter.origin - highlightState.padding.left);
                                highlightEnd.x = Mathf.Max(highlightEnd.x, currentCharacter.xAdvance + highlightState.padding.right);
                            }
                            else
                            {
                                // Use the Min / Max of character bounds
                                highlightStart.x = Mathf.Min(highlightStart.x, currentCharacter.bottomLeft.x - highlightState.padding.left);
                                highlightEnd.x = Mathf.Max(highlightEnd.x, currentCharacter.topRight.x + highlightState.padding.right);
                            }

                            highlightStart.y = Mathf.Min(highlightStart.y, currentCharacter.descender - highlightState.padding.bottom);
                            highlightEnd.y = Mathf.Max(highlightEnd.y, currentCharacter.ascender + highlightState.padding.top);
                        }
                    }

                    // End Highlight if text only contains one character.
                    if (beginHighlight && m_CharacterCount == 1)
                    {
                        beginHighlight = false;

                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                    else if (beginHighlight && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                    else if (beginHighlight && !isHighlightVisible)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                }
                else
                {
                    // End Highlight
                    if (beginHighlight == true)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                }

                #endregion

                lastLine = currentLine;
            }

            #endregion

            // METRICS ABOUT THE TEXT OBJECT
            textInfo.characterCount = m_CharacterCount;
            textInfo.spriteCount = m_SpriteCount;
            textInfo.lineCount = lineCount;
            textInfo.wordCount = wordCount != 0 && m_CharacterCount > 0 ? wordCount : 1;
        }
    }
}
