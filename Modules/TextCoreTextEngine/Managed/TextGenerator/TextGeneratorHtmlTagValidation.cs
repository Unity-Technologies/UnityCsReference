// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.TextCore.Text
{
    internal partial class TextGenerator
    {
        /// <summary>
        /// Function to identify and validate the rich tag. Returns the position of the > if the tag was valid.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        bool ValidateHtmlTag(TextProcessingElement[] chars, int startIndex, out int endIndex, TextGenerationSettings generationSettings, TextInfo textInfo, out bool isThreadSuccess)
        {
            bool isMainThread = !JobsUtility.IsExecutingJob;
            isThreadSuccess = true;
            TextSettings textSettings = generationSettings.textSettings;

            int tagCharCount = 0;
            byte attributeFlag = 0;

            int attributeIndex = 0;
            ClearMarkupTagAttributes();
            TagValueType tagValueType = TagValueType.None;
            TagUnitType tagUnitType = TagUnitType.Pixels;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;
            bool startedWithQuotes = false;
            bool startedWithDoubleQuotes = false;

            for (int i = startIndex; i < chars.Length && chars[i].unicode != 0 && tagCharCount < m_HtmlTag.Length && chars[i].unicode != '<'; i++)
            {
                uint unicode = chars[i].unicode;

                if (unicode == '>') // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    m_HtmlTag[tagCharCount] = (char)0;
                    break;
                }

                m_HtmlTag[tagCharCount] = (char)unicode;
                tagCharCount += 1;

                if (attributeFlag == 1)
                {
                    if (tagValueType == TagValueType.None)
                    {
                        // Check for attribute type
                        if (unicode == '+' || unicode == '-' || unicode == '.' || (unicode >= '0' && unicode <= '9'))
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.NumericalValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.ColorValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '\'')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                            startedWithQuotes = true;
                        }
                        else if (unicode == '"')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                            startedWithDoubleQuotes = true;
                        }
                        else
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueHashCode = (m_XmlAttribute[attributeIndex].valueHashCode << 5) + m_XmlAttribute[attributeIndex].valueHashCode ^ TextGeneratorUtilities.ToUpperFast((char)unicode);
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (tagValueType == TagValueType.NumericalValue)
                        {
                            // Check for termination of numerical value.
                            if (unicode == 'p' || unicode == 'e' || unicode == '%' || unicode == ' ')
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;

                                switch (unicode)
                                {
                                    case 'e':
                                        m_XmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.FontUnits;
                                        break;
                                    case '%':
                                        m_XmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Percentage;
                                        break;
                                    default:
                                        m_XmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Pixels;
                                        break;
                                }

                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;

                            }
                            else
                            {
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (tagValueType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                        else if (tagValueType == TagValueType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            // startedWithQuotes/doubleQuotes is needed to make sure we don't exit the tag if a different type of quotes is used (UUM-59167)
                            if (!(startedWithDoubleQuotes && unicode == '"') && !(startedWithQuotes && unicode == '\''))
                            {
                                m_XmlAttribute[attributeIndex].valueHashCode = (m_XmlAttribute[attributeIndex].valueHashCode << 5) + m_XmlAttribute[attributeIndex].valueHashCode ^ TextGeneratorUtilities.ToUpperFast((char)unicode);
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                    }
                }

                if (unicode == '=') // '='
                    attributeFlag = 1;

                // Compute HashCode for the name of the attribute
                if (attributeFlag == 0 && unicode == ' ')
                {
                    if (isTagSet) return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    tagValueType = TagValueType.None;
                    tagUnitType = TagUnitType.Pixels;
                    attributeIndex += 1;
                    m_XmlAttribute[attributeIndex].nameHashCode = 0;
                    m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                    m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                    m_XmlAttribute[attributeIndex].valueHashCode = 0;
                    m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                    m_XmlAttribute[attributeIndex].valueLength = 0;
                }

                if (attributeFlag == 0)
                    m_XmlAttribute[attributeIndex].nameHashCode = (m_XmlAttribute[attributeIndex].nameHashCode << 5) + m_XmlAttribute[attributeIndex].nameHashCode ^ TextGeneratorUtilities.ToUpperFast((char)unicode);

                if (attributeFlag == 2 && unicode == ' ')
                    attributeFlag = 0;
            }

            if (!isValidHtmlTag)
                return false;

            #region Rich Text Tag Processing
            // Special handling of the no parsing tag </noparse> </NOPARSE> tag
            if (m_TagNoParsing && (m_XmlAttribute[0].nameHashCode != (int)MarkupTag.SLASH_NO_PARSE))
                return false;

            if (m_XmlAttribute[0].nameHashCode == (int)MarkupTag.SLASH_NO_PARSE)
            {
                m_TagNoParsing = false;
                return true;
            }

            // Color <#FFF> 3 Hex values (short form)
            if (m_HtmlTag[0] == k_NumberSign && tagCharCount == 4)
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FFF7> 4 Hex values with alpha (short form)
            else if (m_HtmlTag[0] == k_NumberSign && tagCharCount == 5)
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FF00FF>
            else if (m_HtmlTag[0] == k_NumberSign && tagCharCount == 7) // if Tag begins with # and contains 7 characters.
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FF00FF00> with alpha
            else if (m_HtmlTag[0] == k_NumberSign && tagCharCount == k_Tab) // if Tag begins with # and contains 9 characters.
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            else
            {
                float value = 0;
                float fontScale;

                switch ((MarkupTag)m_XmlAttribute[0].nameHashCode)
                {
                    case MarkupTag.BOLD:
                        m_FontStyleInternal |= FontStyles.Bold;
                        m_FontStyleStack.Add(FontStyles.Bold);

                        m_FontWeightInternal = TextFontWeight.Bold;
                        return true;
                    case MarkupTag.SLASH_BOLD:
                        if ((generationSettings.fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Bold) == 0)
                            {
                                m_FontStyleInternal &= ~FontStyles.Bold;
                                m_FontWeightInternal = m_FontWeightStack.Peek();
                            }
                        }
                        return true;
                    case MarkupTag.ITALIC:
                        m_FontStyleInternal |= FontStyles.Italic;
                        m_FontStyleStack.Add(FontStyles.Italic);

                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.ANGLE)
                        {
                            m_ItalicAngle = (int)TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);

                            // Make sure angle is within valid range.
                            if (m_ItalicAngle < -180 || m_ItalicAngle > 180) return false;
                        }
                        else
                            m_ItalicAngle = m_CurrentFontAsset.italicStyleSlant;

                        m_ItalicAngleStack.Add(m_ItalicAngle);

                        return true;
                    case MarkupTag.SLASH_ITALIC:
                        if ((generationSettings.fontStyle & FontStyles.Italic) != FontStyles.Italic)
                        {
                            m_ItalicAngle = m_ItalicAngleStack.Remove();

                            if (m_FontStyleStack.Remove(FontStyles.Italic) == 0)
                                m_FontStyleInternal &= ~FontStyles.Italic;
                        }
                        return true;
                    case MarkupTag.STRIKETHROUGH:
                        m_FontStyleInternal |= FontStyles.Strikethrough;
                        m_FontStyleStack.Add(FontStyles.Strikethrough);

                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.COLOR)
                        {
                            m_StrikethroughColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            m_StrikethroughColor.a = m_HtmlColor.a < m_StrikethroughColor.a ? (byte)(m_HtmlColor.a) : (byte)(m_StrikethroughColor.a);
                            if (textInfo != null)
                                textInfo.hasMultipleColors = true;
                        }
                        else
                            m_StrikethroughColor = m_HtmlColor;

                        m_StrikethroughColorStack.Add(m_StrikethroughColor);

                        return true;
                    case MarkupTag.SLASH_STRIKETHROUGH:
                        if ((generationSettings.fontStyle & FontStyles.Strikethrough) != FontStyles.Strikethrough)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Strikethrough) == 0)
                                m_FontStyleInternal &= ~FontStyles.Strikethrough;
                        }

                        m_StrikethroughColor = m_StrikethroughColorStack.Remove();
                        return true;
                    case MarkupTag.UNDERLINE:
                        m_FontStyleInternal |= FontStyles.Underline;
                        m_FontStyleStack.Add(FontStyles.Underline);

                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.COLOR)
                        {
                            m_UnderlineColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            m_UnderlineColor.a = m_HtmlColor.a < m_UnderlineColor.a ? (m_HtmlColor.a) : (m_UnderlineColor.a);
                            if (textInfo != null)
                                textInfo.hasMultipleColors = true;
                        }
                        else
                            m_UnderlineColor = m_HtmlColor;

                        m_UnderlineColorStack.Add(m_UnderlineColor);

                        return true;
                    case MarkupTag.SLASH_UNDERLINE:
                        if ((generationSettings.fontStyle & FontStyles.Underline) != FontStyles.Underline)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Underline) == 0)
                                m_FontStyleInternal &= ~FontStyles.Underline;
                        }

                        m_UnderlineColor = m_UnderlineColorStack.Remove();
                        return true;
                    case MarkupTag.MARK:
                        m_FontStyleInternal |= FontStyles.Highlight;
                        m_FontStyleStack.Add(FontStyles.Highlight);

                        Color32 highlightColor = new Color32(255, 255, 0, 64);
                        Offset highlightPadding = Offset.zero;

                        // Handle Mark Tag and potential attributes
                        for (int i = 0; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            switch ((MarkupTag)m_XmlAttribute[i].nameHashCode)
                            {
                                // Mark tag
                                case MarkupTag.MARK:
                                    if (m_XmlAttribute[i].valueType == TagValueType.ColorValue)
                                        highlightColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                                    break;

                                // Color attribute
                                case MarkupTag.COLOR:
                                    highlightColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);
                                    break;

                                // Padding attribute
                                case MarkupTag.PADDING:
                                    int paramCount = TextGeneratorUtilities.GetAttributeParameters(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength, ref m_AttributeParameterValues);
                                    if (paramCount != 4) return false;

                                    highlightPadding = new Offset(m_AttributeParameterValues[0], m_AttributeParameterValues[1], m_AttributeParameterValues[2], m_AttributeParameterValues[3]);
                                    highlightPadding *= m_FontSize * 0.01f * (generationSettings.isOrthographic ? 1 : 0.1f);
                                    break;
                            }
                        }

                        highlightColor.a = m_HtmlColor.a < highlightColor.a ? (byte)(m_HtmlColor.a) : (byte)(highlightColor.a);

                        m_HighlightState = new HighlightState(highlightColor, highlightPadding);
                        m_HighlightStateStack.Push(m_HighlightState);
                        if (textInfo != null)
                            textInfo.hasMultipleColors = true;

                        return true;
                    case MarkupTag.SLASH_MARK:
                        if ((generationSettings.fontStyle & FontStyles.Highlight) != FontStyles.Highlight)
                        {
                            m_HighlightStateStack.Remove();
                            m_HighlightState = m_HighlightStateStack.current;

                            if (m_FontStyleStack.Remove(FontStyles.Highlight) == 0)
                                m_FontStyleInternal &= ~FontStyles.Highlight;
                        }
                        return true;
                    case MarkupTag.SUBSCRIPT:
                        m_FontScaleMultiplier *= m_CurrentFontAsset.faceInfo.subscriptSize > 0 ? m_CurrentFontAsset.faceInfo.subscriptSize : 1;
                        m_BaselineOffsetStack.Push(m_BaselineOffset);
                        fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        m_BaselineOffset += m_CurrentFontAsset.faceInfo.subscriptOffset * fontScale * m_FontScaleMultiplier;

                        m_FontStyleStack.Add(FontStyles.Subscript);
                        m_FontStyleInternal |= FontStyles.Subscript;
                        return true;
                    case MarkupTag.SLASH_SUBSCRIPT:
                        if ((m_FontStyleInternal & FontStyles.Subscript) == FontStyles.Subscript)
                        {
                            if (m_FontScaleMultiplier < 1)
                            {
                                m_BaselineOffset = m_BaselineOffsetStack.Pop();
                                m_FontScaleMultiplier /= m_CurrentFontAsset.faceInfo.subscriptSize > 0 ? m_CurrentFontAsset.faceInfo.subscriptSize : 1;
                            }

                            if (m_FontStyleStack.Remove(FontStyles.Subscript) == 0)
                                m_FontStyleInternal &= ~FontStyles.Subscript;
                        }
                        return true;
                    case MarkupTag.SUPERSCRIPT:
                        m_FontScaleMultiplier *= m_CurrentFontAsset.faceInfo.superscriptSize > 0 ? m_CurrentFontAsset.faceInfo.superscriptSize : 1;
                        m_BaselineOffsetStack.Push(m_BaselineOffset);
                        fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        m_BaselineOffset += m_CurrentFontAsset.faceInfo.superscriptOffset * fontScale * m_FontScaleMultiplier;

                        m_FontStyleStack.Add(FontStyles.Superscript);
                        m_FontStyleInternal |= FontStyles.Superscript;
                        return true;
                    case MarkupTag.SLASH_SUPERSCRIPT:
                        if ((m_FontStyleInternal & FontStyles.Superscript) == FontStyles.Superscript)
                        {
                            if (m_FontScaleMultiplier < 1)
                            {
                                m_BaselineOffset = m_BaselineOffsetStack.Pop();
                                m_FontScaleMultiplier /= m_CurrentFontAsset.faceInfo.superscriptSize > 0 ? m_CurrentFontAsset.faceInfo.superscriptSize : 1;
                            }

                            if (m_FontStyleStack.Remove(FontStyles.Superscript) == 0)
                                m_FontStyleInternal &= ~FontStyles.Superscript;
                        }
                        return true;
                    case MarkupTag.FONT_WEIGHT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch ((int)value)
                        {
                            case 100:
                                m_FontWeightInternal = TextFontWeight.Thin;
                                break;
                            case 200:
                                m_FontWeightInternal = TextFontWeight.ExtraLight;
                                break;
                            case 300:
                                m_FontWeightInternal = TextFontWeight.Light;
                                break;
                            case 400:
                                m_FontWeightInternal = TextFontWeight.Regular;
                                break;
                            case 500:
                                m_FontWeightInternal = TextFontWeight.Medium;
                                break;
                            case 600:
                                m_FontWeightInternal = TextFontWeight.SemiBold;
                                break;
                            case 700:
                                m_FontWeightInternal = TextFontWeight.Bold;
                                break;
                            case 800:
                                m_FontWeightInternal = TextFontWeight.Heavy;
                                break;
                            case 900:
                                m_FontWeightInternal = TextFontWeight.Black;
                                break;
                        }

                        m_FontWeightStack.Add(m_FontWeightInternal);

                        return true;
                    case MarkupTag.SLASH_FONT_WEIGHT:
                        m_FontWeightStack.Remove();

                        if (m_FontStyleInternal == FontStyles.Bold)
                            m_FontWeightInternal = TextFontWeight.Bold;
                        else
                            m_FontWeightInternal = m_FontWeightStack.Peek();

                        return true;
                    case MarkupTag.POSITION:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_XAdvance = value * (generationSettings.isOrthographic ? 1.0f : 0.1f);
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnitType.FontUnits:
                                m_XAdvance = value * m_CurrentFontSize * (generationSettings.isOrthographic ? 1.0f : 0.1f);
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnitType.Percentage:
                                m_XAdvance = m_MarginWidth * value / 100;
                                //m_isIgnoringAlignment = true;
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_POSITION:
                        m_IsIgnoringAlignment = false;
                        return true;
                    case MarkupTag.VERTICAL_OFFSET:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_BaselineOffset = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_BaselineOffset = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                //m_BaselineOffset = m_MarginHeight * val / 100;
                                return false;
                        }
                        return false;
                    case MarkupTag.SLASH_VERTICAL_OFFSET:
                        m_BaselineOffset = 0;
                        return true;
                    case MarkupTag.PAGE:
                        // This tag only works when Overflow - Page mode is used.
                        if (generationSettings.overflowMode == TextOverflowMode.Page)
                        {
                            m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;
                            m_LineOffset = 0;
                            m_PageNumber += 1;
                            m_IsNewPage = true;
                        }
                        return true;
                    case MarkupTag.NO_BREAK:
                        m_IsNonBreakingSpace = true;
                        return true;
                    case MarkupTag.SLASH_NO_BREAK:
                        m_IsNonBreakingSpace = false;
                        return true;
                    case MarkupTag.SIZE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                if (m_HtmlTag[5] == k_Plus) // <size=+00>
                                {
                                    m_CurrentFontSize = m_FontSize + value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    return true;
                                }
                                else if (m_HtmlTag[5] == k_HyphenMinus) // <size=-00>
                                {
                                    m_CurrentFontSize = m_FontSize + value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    return true;
                                }
                                else // <size=00.0>
                                {
                                    m_CurrentFontSize = value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    return true;
                                }
                            case TagUnitType.FontUnits:
                                m_CurrentFontSize = m_FontSize * value;
                                m_SizeStack.Add(m_CurrentFontSize);
                                return true;
                            case TagUnitType.Percentage:
                                m_CurrentFontSize = m_FontSize * value / 100;
                                m_SizeStack.Add(m_CurrentFontSize);
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_SIZE:
                        m_CurrentFontSize = m_SizeStack.Remove();
                        return true;
                    case MarkupTag.FONT:
                        int fontHashCode = m_XmlAttribute[0].valueHashCode;
                        int materialAttributeHashCode = m_XmlAttribute[1].nameHashCode;
                        int materialHashCode = m_XmlAttribute[1].valueHashCode;

                        // Special handling for <font=default> or <font=Default>
                        if (fontHashCode == (int)MarkupTag.DEFAULT)
                        {
                            m_CurrentFontAsset = m_MaterialReferences[0].fontAsset;
                            m_CurrentMaterial = m_MaterialReferences[0].material;
                            m_CurrentMaterialIndex = 0;
                            m_MaterialReferenceStack.Add(m_MaterialReferences[0]);

                            return true;
                        }

                        FontAsset tempFont;
                        Material tempMaterial;

                        // HANDLE NEW FONT ASSET

                        // Check if we already have a reference to this font asset.
                        MaterialReferenceManager.TryGetFontAsset(fontHashCode, out tempFont);

                        // Try loading font asset from potential delegate or resources.
                        if (tempFont == null)
                        {
                            if (tempFont == null)
                            {
                                if (!isMainThread)
                                {
                                    isThreadSuccess = false;
                                    return false;
                                }
                                // Load Font Asset
                                tempFont = Resources.Load<FontAsset>(textSettings.defaultFontAssetPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                            }

                            if (tempFont == null)
                                return false;

                            // Add new reference to the font asset as well as default material to the MaterialReferenceManager
                            MaterialReferenceManager.AddFontAsset(tempFont);
                        }

                        // HANDLE NEW MATERIAL
                        if (materialAttributeHashCode == 0 && materialHashCode == 0)
                        {
                            // No material specified then use default font asset material.
                            m_CurrentMaterial = tempFont.material;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        else if (materialAttributeHashCode == (int)MarkupTag.MATERIAL) // using material attribute
                        {
                            if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                            {
                                m_CurrentMaterial = tempMaterial;

                                m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                                m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                            }
                            else
                            {
                                if (!isMainThread)
                                {
                                    isThreadSuccess = false;
                                    return false;
                                }
                                // Load new material
                                tempMaterial = Resources.Load<Material>(textSettings.defaultFontAssetPath + new string(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength));

                                if (tempMaterial == null)
                                    return false;

                                // Add new reference to this material in the MaterialReferenceManager
                                MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                                m_CurrentMaterial = tempMaterial;

                                m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                                m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                            }
                        }
                        else
                            return false;

                        m_CurrentFontAsset = tempFont;

                        return true;
                    case MarkupTag.SLASH_FONT:
                    {
                        MaterialReference materialReference = m_MaterialReferenceStack.Remove();

                        m_CurrentFontAsset = materialReference.fontAsset;
                        m_CurrentMaterial = materialReference.material;
                        m_CurrentMaterialIndex = materialReference.index;

                        return true;
                    }
                    case MarkupTag.MATERIAL:
                        materialHashCode = m_XmlAttribute[0].valueHashCode;

                        // Special handling for <material=default> or <material=Default>
                        if (materialHashCode == (int)MarkupTag.DEFAULT)
                        {
                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_CurrentFontAsset.atlas.GetInstanceID() != m_CurrentMaterial.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            m_CurrentMaterial = m_MaterialReferences[0].material;
                            m_CurrentMaterialIndex = 0;

                            m_MaterialReferenceStack.Add(m_MaterialReferences[0]);

                            return true;
                        }


                        // Check if material
                        if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                        {
                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_CurrentFontAsset.atlas.GetInstanceID() != tempMaterial.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            m_CurrentMaterial = tempMaterial;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        else
                        {
                            if (!isMainThread)
                            {
                                isThreadSuccess = false;
                                return false;
                            }
                            // Load new material
                            tempMaterial = Resources.Load<Material>(textSettings.defaultFontAssetPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));

                            if (tempMaterial == null)
                                return false;

                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_CurrentFontAsset.atlas.GetInstanceID() != tempMaterial.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            // Add new reference to this material in the MaterialReferenceManager
                            MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                            m_CurrentMaterial = tempMaterial;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        return true;
                    case MarkupTag.SLASH_MATERIAL:
                    {
                        //if (m_CurrentMaterial.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID() != m_MaterialReferenceStack.PreviousItem().material.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID())
                        //    return false;

                        MaterialReference materialReference = m_MaterialReferenceStack.Remove();

                        m_CurrentMaterial = materialReference.material;
                        m_CurrentMaterialIndex = materialReference.index;

                        return true;
                    }
                    case MarkupTag.SPACE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_XAdvance += value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_XAdvance += value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                // Not applicable
                                return false;
                        }
                        return false;
                    case MarkupTag.ALPHA:
                        if (m_XmlAttribute[0].valueLength != 3) return false;

                        m_HtmlColor.a = (byte)(TextGeneratorUtilities.HexToInt(m_HtmlTag[7]) * 16 + TextGeneratorUtilities.HexToInt(m_HtmlTag[8]));
                        return true;

                    case MarkupTag.A:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues)
                        {
                            // For IMGUI, we want to treat the a tag as we do with the link tag
                            if (generationSettings.isIMGUI && textInfo != null)
                            {
                                int index = textInfo.linkCount;

                                if (index + 1 > textInfo.linkInfo.Length)
                                    TextInfo.Resize(ref textInfo.linkInfo, index + 1);

                                textInfo.linkInfo[index].hashCode = (int)MarkupTag.HREF;
                                textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_CharacterCount;

                                textInfo.linkInfo[index].linkIdFirstCharacterIndex = 3;
                                if (m_XmlAttribute[1].valueLength > 0)
                                    textInfo.linkInfo[index].SetLinkId(m_HtmlTag, 2, m_XmlAttribute[1].valueLength + m_XmlAttribute[1].valueStartIndex - 1);
                            }
                            else if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.HREF && textInfo != null)
                            {
                                // Make sure linkInfo array is of appropriate size.
                                int index = textInfo.linkCount;

                                if (index + 1 > textInfo.linkInfo.Length)
                                    TextInfo.Resize(ref textInfo.linkInfo, index + 1);

                                textInfo.linkInfo[index].hashCode = (int)MarkupTag.HREF;
                                textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_CharacterCount;
                                textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_XmlAttribute[1].valueStartIndex;
                                textInfo.linkInfo[index].SetLinkId(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            }
                            textInfo.linkCount += 1;
                        }
                        return true;

                    case MarkupTag.SLASH_A:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues && textInfo != null)
                        {
                            if (textInfo.linkInfo.Length <= 0 || textInfo.linkCount <= 0)
                            {
                                if (generationSettings.textSettings.displayWarnings)
                                    Debug.LogWarning($"There seems to be an issue with the formatting of the <a> tag. Possible issues include: missing or misplaced closing '>', missing or incorrect attribute, or unclosed quotes for attribute values. Please review the tag syntax.");
                            }
                            else
                            {
                                int index = textInfo.linkCount - 1;
                                textInfo.linkInfo[index].linkTextLength = m_CharacterCount - textInfo.linkInfo[index].linkTextfirstCharacterIndex;
                            }
                        }
                        return true;

                    case MarkupTag.LINK:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues && textInfo != null)
                        {
                            int index = textInfo.linkCount;

                            if (index + 1 > textInfo.linkInfo.Length)
                                TextInfo.Resize(ref textInfo.linkInfo, index + 1);

                            textInfo.linkInfo[index].hashCode = m_XmlAttribute[0].valueHashCode;
                            textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_CharacterCount;

                            textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_XmlAttribute[0].valueStartIndex;
                            textInfo.linkInfo[index].SetLinkId(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        }
                        return true;
                    case MarkupTag.SLASH_LINK:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues && textInfo != null)
                        {
                            if (textInfo.linkCount < textInfo.linkInfo.Length)
                            {
                                textInfo.linkInfo[textInfo.linkCount].linkTextLength = m_CharacterCount - textInfo.linkInfo[textInfo.linkCount].linkTextfirstCharacterIndex;

                                textInfo.linkCount += 1;
                            }
                        }
                        return true;
                    case MarkupTag.ALIGN: // <align=>
                        switch ((MarkupTag)m_XmlAttribute[0].valueHashCode)
                        {
                            case MarkupTag.LEFT: // <align=left>
                                m_LineJustification = TextAlignment.MiddleLeft;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.RIGHT: // <align=right>
                                m_LineJustification = TextAlignment.MiddleRight;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.CENTER: // <align=center>
                                m_LineJustification = TextAlignment.MiddleCenter;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.JUSTIFIED: // <align=justified>
                                m_LineJustification = TextAlignment.MiddleJustified;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.FLUSH: // <align=flush>
                                m_LineJustification = TextAlignment.MiddleFlush;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_ALIGN:
                        m_LineJustification = m_LineJustificationStack.Remove();
                        return true;
                    case MarkupTag.WIDTH:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_Width = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                return false;
                            //break;
                            case TagUnitType.Percentage:
                                m_Width = m_MarginWidth * value / 100;
                                break;
                        }
                        return true;
                    case MarkupTag.SLASH_WIDTH:
                        m_Width = -1;
                        return true;
                    // STYLE tag is now handled inline and replaced by its definition.
                    //case 322689: // <style="name">
                    //case 233057: // <STYLE>
                    //    Style style = StyleSheet.GetStyle(m_XmlAttribute[0].valueHashCode);

                    //    if (style == null) return false;

                    //    m_StyleStack.Add(style.hashCode);

                    //    // Parse Style Macro
                    //    for (int i = 0; i < style.styleOpeningTagArray.Length; i++)
                    //    {
                    //        if (style.styleOpeningTagArray[i] == k_LesserThan)
                    //        {
                    //            if (ValidateHtmlTag(style.styleOpeningTagArray, i + 1, out i) == false) return false;
                    //        }
                    //    }
                    //    return true;
                    //case 1112618: // </style>
                    //case 1022986: // </STYLE>
                    //    style = StyleSheet.GetStyle(m_XmlAttribute[0].valueHashCode);

                    //    if (style == null)
                    //    {
                    //        // Get style from the Style Stack
                    //        int styleHashCode = m_StyleStack.CurrentItem();
                    //        style = StyleSheet.GetStyle(styleHashCode);

                    //        m_StyleStack.Remove();
                    //    }

                    //    if (style == null) return false;
                    //    //// Parse Style Macro
                    //    for (int i = 0; i < style.styleClosingTagArray.Length; i++)
                    //    {
                    //        if (style.styleClosingTagArray[i] == k_LesserThan)
                    //            ValidateHtmlTag(style.styleClosingTagArray, i + 1, out i);
                    //    }
                    //    return true;
                    case MarkupTag.COLOR:
                        if (textInfo != null)
                            textInfo.hasMultipleColors = true;
                        // <color=#FFF> 3 Hex (short hand)
                        if (m_HtmlTag[6] == k_NumberSign && tagCharCount == k_LineFeed)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FFF7> 4 Hex (short hand)
                        else if (m_HtmlTag[6] == k_NumberSign && tagCharCount == 11)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FF00FF> 3 Hex pairs
                        if (m_HtmlTag[6] == k_NumberSign && tagCharCount == k_CarriageReturn)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FF00FF00> 4 Hex pairs
                        else if (m_HtmlTag[6] == k_NumberSign && tagCharCount == 15)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }

                        // <color=name>
                        switch (m_XmlAttribute[0].valueHashCode)
                        {
                            case (int)MarkupTag.RED: // <color=red>
                                m_HtmlColor = Color.red;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.LIGHTBLUE: // <color=lightblue>
                                m_HtmlColor = new Color32(173, 216, 230, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.BLUE: // <color=blue>
                                m_HtmlColor = Color.blue;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.GREY: // <color=grey>
                                m_HtmlColor = new Color32(128, 128, 128, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.BLACK: // <color=black>
                                m_HtmlColor = Color.black;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.GREEN: // <color=green>
                                m_HtmlColor = Color.green;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.WHITE: // <color=white>
                                m_HtmlColor = Color.white;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.ORANGE: // <color=orange>
                                m_HtmlColor = new Color32(255, 128, 0, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.PURPLE: // <color=purple>
                                m_HtmlColor = new Color32(160, 32, 240, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.YELLOW: // <color=yellow>
                                m_HtmlColor = Color.yellow;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                        }
                        return false;

                    case MarkupTag.GRADIENT:
                        int gradientPresetHashCode = m_XmlAttribute[0].valueHashCode;
                        TextColorGradient tempColorGradientPreset;

                        // Check if Color Gradient Preset has already been loaded.
                        if (MaterialReferenceManager.TryGetColorGradientPreset(gradientPresetHashCode, out tempColorGradientPreset))
                        {
                            m_ColorGradientPreset = tempColorGradientPreset;
                        }
                        else
                        {
                            // Load Color Gradient Preset
                            if (tempColorGradientPreset == null)
                            {
                                if (!isMainThread)
                                {
                                    isThreadSuccess = false;
                                    return false;
                                }
                                tempColorGradientPreset = Resources.Load<TextColorGradient>(textSettings.defaultColorGradientPresetsPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                            }

                            if (tempColorGradientPreset == null)
                                return false;

                            MaterialReferenceManager.AddColorGradientPreset(gradientPresetHashCode, tempColorGradientPreset);
                            m_ColorGradientPreset = tempColorGradientPreset;
                        }

                        m_ColorGradientPresetIsTinted = false;

                        // Check Attributes
                        for (int i = 1; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            // Get attribute name
                            int nameHashCode = m_XmlAttribute[i].nameHashCode;

                            switch ((MarkupTag)nameHashCode)
                            {
                                case MarkupTag.TINT:
                                    m_ColorGradientPresetIsTinted = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength) != 0;
                                    break;
                            }
                        }

                        m_ColorGradientStack.Add(m_ColorGradientPreset);

                        // TODO : Add support for defining preset in the tag itself

                        return true;

                    case MarkupTag.SLASH_GRADIENT:
                        m_ColorGradientPreset = m_ColorGradientStack.Remove();
                        return true;

                    case MarkupTag.CHARACTER_SPACE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_CSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_CSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;
                    case MarkupTag.SLASH_CHARACTER_SPACE:
                        if (!m_isTextLayoutPhase || textInfo == null) return true;

                        // Adjust xAdvance to remove extra space from last character.
                        if (m_CharacterCount > 0)
                        {
                            m_XAdvance -= m_CSpacing;
                            textInfo.textElementInfo[m_CharacterCount - 1].xAdvance = m_XAdvance;
                        }
                        m_CSpacing = 0;
                        return true;
                    case MarkupTag.MONOSPACE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (m_XmlAttribute[0].unitType)
                        {
                            case TagUnitType.Pixels:
                                m_MonoSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_MonoSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }

                        // Check for potential DuoSpace attribute.
                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.DUOSPACE)
                            m_DuoSpace = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength) != 0;

                        return true;
                    case MarkupTag.SLASH_MONOSPACE:
                        m_MonoSpacing = 0;
                        m_DuoSpace = false;
                        return true;
                    case MarkupTag.CLASS:
                        return false;
                    case MarkupTag.SLASH_COLOR:
                        m_HtmlColor = m_ColorStack.Remove();
                        return true;
                    case MarkupTag.INDENT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_TagIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_TagIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_TagIndent = m_MarginWidth * value / 100;
                                break;
                        }
                        m_IndentStack.Add(m_TagIndent);

                        m_XAdvance = m_TagIndent;
                        return true;
                    case MarkupTag.SLASH_INDENT:
                        m_TagIndent = m_IndentStack.Remove();
                        //m_XAdvance = m_TagIndent;
                        return true;
                    case MarkupTag.LINE_INDENT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_TagLineIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_TagLineIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_TagLineIndent = m_MarginWidth * value / 100;
                                break;
                        }

                        m_XAdvance += m_TagLineIndent;
                        return true;
                    case MarkupTag.SLASH_LINE_INDENT:
                        m_TagLineIndent = 0;
                        return true;
                    case MarkupTag.SPRITE:
                        int spriteAssetHashCode = m_XmlAttribute[0].valueHashCode;
                        SpriteAsset tempSpriteAsset;
                        m_SpriteIndex = -1;

                        // CHECK TAG FORMAT
                        if (m_XmlAttribute[0].valueType == TagValueType.None || m_XmlAttribute[0].valueType == TagValueType.NumericalValue)
                        {
                            // No Sprite Asset is assigned to the text object
                            if (generationSettings.spriteAsset != null)
                            {
                                m_CurrentSpriteAsset = generationSettings.spriteAsset;
                            }
                            else if (textSettings.defaultSpriteAsset != null)
                            {
                                m_CurrentSpriteAsset = textSettings.defaultSpriteAsset;
                            }
                            else if (s_DefaultSpriteAsset != null)
                            {
                                m_CurrentSpriteAsset = s_DefaultSpriteAsset;
                            }
                            else if (s_DefaultSpriteAsset == null && isMainThread)
                            {
                                s_DefaultSpriteAsset = Resources.Load<SpriteAsset>("Sprite Assets/Default Sprite Asset");
                                m_CurrentSpriteAsset = s_DefaultSpriteAsset;
                            }

                            // No valid sprite asset available
                            if (m_CurrentSpriteAsset == null)
                                return false;
                        }
                        else
                        {
                            // A Sprite Asset has been specified
                            if (MaterialReferenceManager.TryGetSpriteAsset(spriteAssetHashCode, out tempSpriteAsset))
                            {
                                m_CurrentSpriteAsset = tempSpriteAsset;
                            }
                            else
                            {
                                // Load Sprite Asset
                                if (tempSpriteAsset == null)
                                {
                                    if (tempSpriteAsset == null)
                                    {
                                        if (!isMainThread)
                                        {
                                            isThreadSuccess = false;
                                            return false;
                                        }
                                        tempSpriteAsset = Resources.Load<SpriteAsset>(textSettings.defaultSpriteAssetPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                                    }
                                }

                                if (tempSpriteAsset == null)
                                    return false;

                                MaterialReferenceManager.AddSpriteAsset(spriteAssetHashCode, tempSpriteAsset);
                                m_CurrentSpriteAsset = tempSpriteAsset;
                            }
                        }

                        // Handling of <sprite=index> legacy tag format.
                        if (m_XmlAttribute[0].valueType == TagValueType.NumericalValue) // <sprite=index>
                        {
                            int index = (int)TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                            // Reject tag if value is invalid.
                            if (index == Int16.MinValue) return false;

                            // Check to make sure sprite index is valid
                            if (index > m_CurrentSpriteAsset.spriteCharacterTable.Count - 1) return false;

                            m_SpriteIndex = index;
                        }

                        m_SpriteColor = Color.white;
                        m_TintSprite = false;

                        // Handle Sprite Tag Attributes
                        for (int i = 0; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            int nameHashCode = m_XmlAttribute[i].nameHashCode;
                            int index = 0;

                            switch ((MarkupTag)nameHashCode)
                            {
                                case MarkupTag.NAME:
                                    m_CurrentSpriteAsset = SpriteAsset.SearchForSpriteByHashCode(m_CurrentSpriteAsset, m_XmlAttribute[i].valueHashCode, true, out index);
                                    if (index == -1) return false;

                                    m_SpriteIndex = index;
                                    break;
                                case MarkupTag.INDEX:
                                    index = (int)TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);

                                    // Reject tag if value is invalid.
                                    if (index == Int16.MinValue) return false;

                                    // Check to make sure sprite index is valid
                                    if (index > m_CurrentSpriteAsset.spriteCharacterTable.Count - 1) return false;

                                    m_SpriteIndex = index;
                                    break;
                                case MarkupTag.TINT:
                                    m_TintSprite = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength) != 0;
                                    break;
                                case MarkupTag.COLOR:
                                    m_SpriteColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);
                                    break;
                                case MarkupTag.ANIM:
                                    int paramCount = TextGeneratorUtilities.GetAttributeParameters(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength, ref m_AttributeParameterValues);
                                    if (paramCount != 3) return false;

                                    m_SpriteIndex = (int)m_AttributeParameterValues[0];

                                    if (m_isTextLayoutPhase)
                                    {
                                        // It is possible for a sprite to get animated when it ends up being truncated.
                                        // Should consider moving the animation of the sprite after text geometry upload.
                                        //spriteAnimator.DoSpriteAnimation(m_CharacterCount, m_CurrentSpriteAsset, m_SpriteIndex, (int)m_AttributeParameterValues[1], (int)m_AttributeParameterValues[2]);
                                    }

                                    break;
                                //case 45545: // size
                                //case 32745: // SIZE

                                //    break;
                                default:
                                    if (nameHashCode != (int)MarkupTag.SPRITE)
                                        return false;
                                    break;
                            }
                        }

                        if (m_SpriteIndex == -1) return false;

                        // Material HashCode for the Sprite Asset is the Sprite Asset Hash Code
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentSpriteAsset.material, m_CurrentSpriteAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                        m_TextElementType = TextElementType.Sprite;
                        return true;
                    case MarkupTag.LOWERCASE:
                        m_FontStyleInternal |= FontStyles.LowerCase;
                        m_FontStyleStack.Add(FontStyles.LowerCase);
                        return true;
                    case MarkupTag.SLASH_LOWERCASE:
                        if ((generationSettings.fontStyle & FontStyles.LowerCase) != FontStyles.LowerCase)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.LowerCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.LowerCase;
                        }
                        return true;
                    case MarkupTag.ALLCAPS:
                    case MarkupTag.UPPERCASE:
                        m_FontStyleInternal |= FontStyles.UpperCase;
                        m_FontStyleStack.Add(FontStyles.UpperCase);
                        return true;
                    case MarkupTag.SLASH_ALLCAPS:
                    case MarkupTag.SLASH_UPPERCASE:
                        if ((generationSettings.fontStyle & FontStyles.UpperCase) != FontStyles.UpperCase)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.UpperCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.UpperCase;
                        }
                        return true;
                    case MarkupTag.SMALLCAPS:
                        m_FontStyleInternal |= FontStyles.SmallCaps;
                        m_FontStyleStack.Add(FontStyles.SmallCaps);
                        return true;
                    case MarkupTag.SLASH_SMALLCAPS:
                        if ((generationSettings.fontStyle & FontStyles.SmallCaps) != FontStyles.SmallCaps)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.SmallCaps) == 0)
                                m_FontStyleInternal &= ~FontStyles.SmallCaps;
                        }
                        return true;
                    case MarkupTag.MARGIN:
                        // Check value type
                        switch (m_XmlAttribute[0].valueType)
                        {
                            case TagValueType.NumericalValue:
                                value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px

                                // Reject tag if value is invalid.
                                if (value == Int16.MinValue) return false;

                                // Determine tag unit type
                                switch (tagUnitType)
                                {
                                    case TagUnitType.Pixels:
                                        m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                        break;
                                    case TagUnitType.FontUnits:
                                        m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                        break;
                                    case TagUnitType.Percentage:
                                        m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                        break;
                                }
                                m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                                m_MarginRight = m_MarginLeft;
                                return true;

                            case TagValueType.None:
                                for (int i = 1; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                                {
                                    // Get attribute name
                                    int nameHashCode = m_XmlAttribute[i].nameHashCode;

                                    switch ((MarkupTag)nameHashCode)
                                    {
                                        case MarkupTag.LEFT:
                                            value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength); // px

                                            // Reject tag if value is invalid.
                                            if (value == Int16.MinValue) return false;

                                            switch (m_XmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                                            break;

                                        case MarkupTag.RIGHT:
                                            value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength); // px

                                            // Reject tag if value is invalid.
                                            if (value == Int16.MinValue) return false;

                                            switch (m_XmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_MarginRight = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_MarginRight = m_MarginRight >= 0 ? m_MarginRight : 0;
                                            break;
                                    }
                                }
                                return true;
                        }

                        return false;
                    case MarkupTag.SLASH_MARGIN:
                        m_MarginLeft = 0;
                        m_MarginRight = 0;
                        return true;
                    case MarkupTag.MARGIN_LEFT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                break;
                        }
                        m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                        return true;
                    case MarkupTag.MARGIN_RIGHT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginRight = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                break;
                        }
                        m_MarginRight = m_MarginRight >= 0 ? m_MarginRight : 0;
                        return true;
                    case MarkupTag.LINE_HEIGHT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_LineHeight = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_LineHeight = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                                m_LineHeight = generationSettings.fontAsset.faceInfo.lineHeight * value / 100 * fontScale;
                                break;
                        }
                        return true;
                    case MarkupTag.SLASH_LINE_HEIGHT:
                        m_LineHeight = k_FloatUnset;
                        return true;
                    case MarkupTag.NO_PARSE:
                        m_TagNoParsing = true;
                        return true;
                    case MarkupTag.ACTION:
                        int actionID = m_XmlAttribute[0].valueHashCode;

                        if (m_isTextLayoutPhase)
                        {
                            m_ActionStack.Add(actionID);

                            Debug.Log("Action ID: [" + actionID + "] First character index: " + m_CharacterCount);
                        }
                        //if (m_isTextLayoutPhase)
                        //{
                        // TMP_Action action = TMP_Action.GetAction(m_XmlAttribute[0].valueHashCode);
                        //}
                        return true;
                    case MarkupTag.SLASH_ACTION:
                        if (m_isTextLayoutPhase)
                        {
                            Debug.Log("Action ID: [" + m_ActionStack.CurrentItem() + "] Last character index: " + (m_CharacterCount - 1));
                        }

                        m_ActionStack.Remove();
                        return true;
                    case MarkupTag.SCALE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        m_FXScale = new Vector3(value, 1, 1);

                        return true;
                    case MarkupTag.SLASH_SCALE:
                        m_FXScale = Vector3.one;
                        return true;
                    case MarkupTag.ROTATE:
                        // TODO: Add attribute to provide for ability to use Random Rotation
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        m_FXRotation = Quaternion.Euler(0, 0, value);

                        return true;
                    case MarkupTag.SLASH_ROTATE:
                        m_FXRotation = Quaternion.identity;
                        return true;
                    case MarkupTag.TABLE:
                        //switch (m_XmlAttribute[1].nameHashCode)
                        //{
                        //    case 327550: // width
                        //        float tableWidth = ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);

                        //        // Reject tag if value is invalid.
                        //        if (tableWidth == Int16.MinValue) return false;

                        //        switch (tagUnitType)
                        //        {
                        //            case TagUnitType.Pixels:
                        //                Debug.Log("Table width = " + tableWidth + "px.");
                        //                break;
                        //            case TagUnitType.FontUnits:
                        //                Debug.Log("Table width = " + tableWidth + "em.");
                        //                break;
                        //            case TagUnitType.Percentage:
                        //                Debug.Log("Table width = " + tableWidth + "%.");
                        //                break;
                        //        }
                        //        break;
                        //}
                        return false;
                    case MarkupTag.SLASH_TABLE:
                        return false;
                    case MarkupTag.TR:
                        return false;
                    case MarkupTag.SLASH_TR:
                        return false;
                    case MarkupTag.TH:
                        // Set style to bold and center alignment
                        return false;
                    case MarkupTag.SLASH_TH:
                        return false;
                    case MarkupTag.TD:
                        // Style options
                        //for (int i = 1; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        //{
                        //    switch (m_XmlAttribute[i].nameHashCode)
                        //    {
                        //        case 327550: // width
                        //            float tableWidth = ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);

                        //            switch (tagUnitType)
                        //            {
                        //                case TagUnitType.Pixels:
                        //                    Debug.Log("Table width = " + tableWidth + "px.");
                        //                    break;
                        //                case TagUnitType.FontUnits:
                        //                    Debug.Log("Table width = " + tableWidth + "em.");
                        //                    break;
                        //                case TagUnitType.Percentage:
                        //                    Debug.Log("Table width = " + tableWidth + "%.");
                        //                    break;
                        //            }
                        //            break;
                        //        case 275917: // align
                        //            switch (m_XmlAttribute[i].valueHashCode)
                        //            {
                        //                case 3774683: // left
                        //                    Debug.Log("TD align=\"left\".");
                        //                    break;
                        //                case 136703040: // right
                        //                    Debug.Log("TD align=\"right\".");
                        //                    break;
                        //                case -458210101: // center
                        //                    Debug.Log("TD align=\"center\".");
                        //                    break;
                        //                case -523808257: // justified
                        //                    Debug.Log("TD align=\"justified\".");
                        //                    break;
                        //            }
                        //            break;
                        //    }
                        //}

                        return false;
                    case MarkupTag.SLASH_TD:
                        return false;
                }
            }
            #endregion

            return false;
        }

        void ClearMarkupTagAttributes()
        {
            int length = m_XmlAttribute.Length;
            for (int i = 0; i < length; i++)
                m_XmlAttribute[i] = new RichTextTagAttribute();
        }

    }
}
