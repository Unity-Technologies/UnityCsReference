// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{


    internal partial class TextGenerator
    {
        bool NeedToRound => m_ShouldRenderBitmap;

        float Round(float v)
        {
            if (!NeedToRound)
                return v;
            // When rounding, we want to round halfway values always on the the same size even if there is a small floating point error in the calculation.
            // This could be the case for centered values where all values would be exactly 0.5 and the rounding direction would be unpredictable.
            // The small 0.02 pixel offset give a bit of play. This pattern is comming from other places in UITK, mostly from the layout,
            // but it hasn't been confirmed to be needed here (just a precaution).
            const float kNearestRoundingOffset = 0.48f;
            return Mathf.Floor(v + kNearestRoundingOffset);
        }

        public void ParsingPhase(TextInfo textInfo, TextGenerationSettings generationSettings, out uint charCode, out float maxVisibleDescender)
        {
            TextSettings textSettings = generationSettings.textSettings;
            m_CurrentMaterial = generationSettings.fontAsset.material;
            m_CurrentMaterialIndex = 0;
            m_MaterialReferenceStack.SetDefault(new MaterialReference(m_CurrentMaterialIndex, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            m_CurrentSpriteAsset = null;

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_TotalCharacterCount;

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (m_FontSize / generationSettings.fontAsset.m_FaceInfo.pointSize * generationSettings.fontAsset.m_FaceInfo.scale );
            float currentElementScale = baseScale;
            float currentEmScale = m_FontSize * 0.01f;
            m_FontScaleMultiplier = 1;
            m_ShouldRenderBitmap = generationSettings.fontAsset.IsBitmap();

            m_CurrentFontSize = m_FontSize;
            m_SizeStack.SetDefault(m_CurrentFontSize);

            charCode = 0; // Holds the character code of the currently being processed character.

            m_FontStyleInternal = generationSettings.fontStyle; // Set the default style.
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);
            m_FontStyleStack.Clear();

            m_LineJustification = generationSettings.textAlignment; // m_textAlignment; // Sets the line justification mode to match editor alignment.
            m_LineJustificationStack.SetDefault(m_LineJustification);

            float padding = 0;

            //float boldXAdvanceMultiplier = 1; // Used to increase spacing between character when style is bold.

            m_BaselineOffset = 0; // Used by subscript characters.
            m_BaselineOffsetStack.Clear();

            m_FontColor32 = generationSettings.color;
            m_HtmlColor = m_FontColor32;
            m_UnderlineColor = m_HtmlColor;
            m_StrikethroughColor = m_HtmlColor;

            m_ColorStack.SetDefault(m_HtmlColor);
            m_UnderlineColorStack.SetDefault(m_HtmlColor);
            m_StrikethroughColorStack.SetDefault(m_HtmlColor);
            m_HighlightStateStack.SetDefault(new HighlightState(m_HtmlColor, Offset.zero));

            m_ColorGradientPreset = null;
            m_ColorGradientStack.SetDefault(null);

            m_ItalicAngle = m_CurrentFontAsset.italicStyleSlant;
            m_ItalicAngleStack.SetDefault(m_ItalicAngle);

            // Clear the Action stack.
            m_ActionStack.Clear();

            m_FXScale = Vector3.one;
            m_FXRotation = Quaternion.identity;

            m_LineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_LineHeight = k_FloatUnset;
            float lineGap = Round(m_CurrentFontAsset.faceInfo.lineHeight - (m_CurrentFontAsset.m_FaceInfo.ascentLine - m_CurrentFontAsset.m_FaceInfo.descentLine));

            m_CSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_MonoSpacing = 0;
            m_XAdvance = 0; // Used to track the position of each character.

            m_TagLineIndent = 0; // Used for indentation of text.
            m_TagIndent = 0;
            m_IndentStack.SetDefault(0);
            m_TagNoParsing = false;

            m_CharacterCount = 0; // Total characters in the char[]

            // Tracking of line information
            m_FirstCharacterOfLine = 0;
            m_LastCharacterOfLine = 0;
            m_FirstVisibleCharacterOfLine = 0;
            m_LastVisibleCharacterOfLine = 0;
            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
            m_LineNumber = 0;
            m_StartOfLineAscender = 0;
            m_LineVisibleCharacterCount = 0;
            m_LineVisibleSpaceCount = 0;
            bool isStartOfNewLine = true;
            m_IsDrivenLineSpacing = false;
            m_FirstOverflowCharacterIndex = -1;
            m_LastBaseGlyphIndex = int.MinValue;

            bool kerning = TextGenerationSettings.fontFeatures.Contains(OTL_FeatureTag.kern);
            bool markToBase = TextGenerationSettings.fontFeatures.Contains(OTL_FeatureTag.mark);
            bool markToMark = TextGenerationSettings.fontFeatures.Contains(OTL_FeatureTag.mkmk);

            float marginWidth = m_MarginWidth > 0 ? m_MarginWidth : 0;
            float marginHeight = m_MarginHeight > 0 ? m_MarginHeight : 0;
            m_MarginLeft = 0;
            m_MarginRight = 0;
            m_Width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

            // Need to initialize these Extents structures
            m_MeshExtents.min = TextGeneratorUtilities.largePositiveVector2;
            m_MeshExtents.max = TextGeneratorUtilities.largeNegativeVector2;

            // Initialize lineInfo
            textInfo.ClearLineInfo();

            // Tracking of the highest Ascender
            m_MaxCapHeight = 0;
            m_MaxAscender = 0;
            m_MaxDescender = 0;
            m_PageAscender = 0;
            maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            m_IsNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;
            int lastSoftLineBreak = 0;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;
            var wordWrap = generationSettings.textWrappingMode;

            // Save character and line state before we begin layout.
            SaveWordWrappingState(ref m_SavedWordWrapState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedLineState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedEllipsisState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedLastValidState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedSoftLineBreakState, -1, -1, textInfo);

            m_EllipsisInsertionCandidateStack.Clear();

            // Clear the previous truncated / ellipsed state
            m_IsTextTruncated = false;

            // Safety Tracker
            int restoreCount = 0;

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                charCode = m_TextProcessingArray[i].unicode;

                if (restoreCount > 5)
                {
                    Debug.LogError("Line breaking recursion max threshold hit... Character [" + charCode + "] index: " + i);
                    characterToSubstitute.index = m_CharacterCount;
                    characterToSubstitute.unicode = k_EndOfText;
                }

                // Skip characters that have been substituted.
                if (charCode == 0x1A)
                    continue;

                // Parse Rich Text Tag
                #region Parse Rich Text Tag

                if (generationSettings.richText && charCode == '<')
                {
                    m_isTextLayoutPhase = true;
                    m_TextElementType = TextElementType.Character;
                    int endTagIndex;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex, generationSettings, textInfo, out bool isThreadSuccess))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        if (m_TextElementType == TextElementType.Character)
                            continue;
                    }
                }
                else
                {
                    m_TextElementType = textInfo.textElementInfo[m_CharacterCount].elementType;
                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;
                    m_CurrentFontAsset = textInfo.textElementInfo[m_CharacterCount].fontAsset;
                }

                #endregion End Parse Rich Text Tag

                int previousMaterialIndex = m_CurrentMaterialIndex;
                bool isUsingAltTypeface = textInfo.textElementInfo[m_CharacterCount].isUsingAlternateTypeface;

                m_isTextLayoutPhase = false;

                // Handle potential character substitutions
                #region Character Substitutions

                bool isInjectedCharacter = false;

                if (characterToSubstitute.index == m_CharacterCount)
                {
                    charCode = characterToSubstitute.unicode;
                    m_TextElementType = TextElementType.Character;
                    isInjectedCharacter = true;

                    switch (charCode)
                    {
                        case k_EndOfText:
                            textInfo.textElementInfo[m_CharacterCount].textElement = m_CurrentFontAsset.characterLookupTable[k_EndOfText];
                            m_IsTextTruncated = true;
                            break;
                        case k_HyphenMinus:
                            //
                            break;
                        case k_HorizontalEllipsis:
                            textInfo.textElementInfo[m_CharacterCount].textElement = m_Ellipsis.character;
                            textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                            textInfo.textElementInfo[m_CharacterCount].fontAsset = m_Ellipsis.fontAsset;
                            textInfo.textElementInfo[m_CharacterCount].material = m_Ellipsis.material;
                            textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex = m_Ellipsis.materialIndex;

                            // Need to increase reference count in the event the primary mesh has no characters.
                            m_MaterialReferences[m_Underline.materialIndex].referenceCount += 1;

                            // Indicates the source parsing data has been modified.
                            m_IsTextTruncated = true;

                            // End Of Text
                            characterToSubstitute.index = m_CharacterCount + 1;
                            characterToSubstitute.unicode = k_EndOfText;
                            break;
                    }
                }

                #endregion

                // When using Linked text, mark character as ignored and skip to next character.
                #region Linked Text

                if (m_CharacterCount < TextGenerationSettings.firstVisibleCharacter && charCode != k_EndOfText)
                {
                    textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                    textInfo.textElementInfo[m_CharacterCount].character = (char)k_ZeroWidthSpace;
                    textInfo.textElementInfo[m_CharacterCount].lineNumber = 0;
                    m_CharacterCount += 1;
                    continue;
                }

                #endregion

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

                if (m_TextElementType == TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)charCode))
                            charCode = char.ToUpper((char)charCode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)charCode))
                            charCode = char.ToLower((char)charCode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        if (char.IsLower((char)charCode))
                        {
                            smallCapsMultiplier = 0.8f;
                            charCode = char.ToUpper((char)charCode);
                        }
                    }
                }

                #endregion

                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                if (m_TextElementType == TextElementType.Sprite)
                {
                    // If a sprite is used as a fallback then get a reference to it and set the color to white.
                    SpriteCharacter sprite = (SpriteCharacter)textInfo.textElementInfo[m_CharacterCount].textElement;
                    m_CurrentSpriteAsset = sprite.textAsset as SpriteAsset;
                    m_SpriteIndex = (int)sprite.glyphIndex;

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    if (charCode == '<')
                        charCode = 57344 + (uint)m_SpriteIndex;
                    else
                        m_SpriteColor = Color.white;

                    float fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale );

                    // The sprite scale calculations are based on the font asset assigned to the text object.
                    if (m_CurrentSpriteAsset.m_FaceInfo.pointSize > 0)
                    {
                        float spriteScale = m_CurrentFontSize / m_CurrentSpriteAsset.m_FaceInfo.pointSize * m_CurrentSpriteAsset.m_FaceInfo.scale;
                        currentElementScale = sprite.m_Scale * sprite.m_Glyph.scale * spriteScale;
                        elementAscentLine = m_CurrentSpriteAsset.m_FaceInfo.ascentLine;
                        baselineOffset = m_CurrentSpriteAsset.m_FaceInfo.baseline * fontScale * m_FontScaleMultiplier * m_CurrentSpriteAsset.m_FaceInfo.scale;
                        elementDescentLine = m_CurrentSpriteAsset.m_FaceInfo.descentLine;
                    }
                    else
                    {
                        float spriteScale = m_CurrentFontSize / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale;
                        currentElementScale = m_CurrentFontAsset.m_FaceInfo.ascentLine / sprite.m_Glyph.metrics.height * sprite.m_Scale * sprite.m_Glyph.scale * spriteScale;
                        float scaleDelta = spriteScale / currentElementScale;
                        elementAscentLine = m_CurrentFontAsset.m_FaceInfo.ascentLine * scaleDelta;
                        baselineOffset = m_CurrentFontAsset.m_FaceInfo.baseline * fontScale * m_FontScaleMultiplier * m_CurrentFontAsset.m_FaceInfo.scale;
                        elementDescentLine = m_CurrentFontAsset.m_FaceInfo.descentLine * scaleDelta;
                    }

                    m_CachedTextElement = sprite;

                    textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Sprite;
                    textInfo.textElementInfo[m_CharacterCount].scale = currentElementScale;
                    textInfo.textElementInfo[m_CharacterCount].spriteAsset = m_CurrentSpriteAsset;
                    textInfo.textElementInfo[m_CharacterCount].fontAsset = m_CurrentFontAsset;
                    textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                    m_CurrentMaterialIndex = previousMaterialIndex;

                    padding = 0;
                }
                else if (m_TextElementType == TextElementType.Character)
                {
                    m_CachedTextElement = textInfo.textElementInfo[m_CharacterCount].textElement;
                    if (m_CachedTextElement == null)
                    {
                        continue;
                    }

                    m_CurrentFontAsset = textInfo.textElementInfo[m_CharacterCount].fontAsset;
                    m_CurrentMaterial = textInfo.textElementInfo[m_CharacterCount].material;
                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;

                    // Special handling if replaced character was a line feed where in this case we have to use the scale of the previous character.
                    float adjustedScale;
                    if (isInjectedCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_CharacterCount != m_FirstCharacterOfLine)
                        adjustedScale = textInfo.textElementInfo[m_CharacterCount - 1].pointSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale;
                    else
                        adjustedScale = m_CurrentFontSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale;

                    // Special handling for injected Ellipsis
                    if (isInjectedCharacter && charCode == k_HorizontalEllipsis)
                    {
                        elementAscentLine = 0;
                        elementDescentLine = 0;
                    }
                    else
                    {
                        elementAscentLine = m_CurrentFontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_CurrentFontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale * m_FontScaleMultiplier * m_CachedTextElement.m_Scale * m_CachedTextElement.m_Glyph.scale;
                    baselineOffset = Round(m_CurrentFontAsset.m_FaceInfo.baseline * adjustedScale * m_FontScaleMultiplier * m_CurrentFontAsset.m_FaceInfo.scale);

                    textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                    textInfo.textElementInfo[m_CharacterCount].scale = currentElementScale;

                    padding = m_Padding;
                }

                #endregion

                // Handle Soft Hyphen
                #region Handle Soft Hyphen

                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == k_SoftHyphen || charCode == k_EndOfText)
                    currentElementScale = 0;

                #endregion

                // Store some of the text object's information
                textInfo.textElementInfo[m_CharacterCount].character = charCode;
                textInfo.textElementInfo[m_CharacterCount].pointSize = m_CurrentFontSize;
                textInfo.textElementInfo[m_CharacterCount].color = m_HtmlColor;
                textInfo.textElementInfo[m_CharacterCount].underlineColor = m_UnderlineColor;
                textInfo.textElementInfo[m_CharacterCount].strikethroughColor = m_StrikethroughColor;
                textInfo.textElementInfo[m_CharacterCount].highlightState = m_HighlightState;
                textInfo.textElementInfo[m_CharacterCount].style = m_FontStyleInternal;
                if (m_FontWeightInternal == TextFontWeight.Bold)
                {
                    textInfo.textElementInfo[m_CharacterCount].style |= FontStyles.Bold;
                }

                // Cache glyph metrics
                Glyph altGlyph = textInfo.textElementInfo[m_CharacterCount].alternativeGlyph;
                GlyphMetrics currentGlyphMetrics = altGlyph == null ? m_CachedTextElement.m_Glyph.metrics : altGlyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                // Handle Kerning if Enabled.
                #region Handle Kerning

                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                float characterSpacingAdjustment = generationSettings.characterSpacing;

                if (kerning && m_TextElementType == TextElementType.Character)
                {
                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_CachedTextElement.m_GlyphIndex;

                    if (m_CharacterCount < totalCharacterCount - 1 && textInfo.textElementInfo[m_CharacterCount + 1].elementType == TextElementType.Character)
                    {
                        uint nextGlyphIndex = textInfo.textElementInfo[m_CharacterCount + 1].textElement.m_GlyphIndex;
                        uint key = nextGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments = adjustmentPair.firstAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    if (m_CharacterCount >= 1)
                    {
                        uint previousGlyphIndex = textInfo.textElementInfo[m_CharacterCount - 1].textElement.m_GlyphIndex;
                        uint key = baseGlyphIndex << 16 | previousGlyphIndex;

                        if (textInfo.textElementInfo[m_CharacterCount - 1].elementType == TextElementType.Character && m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    textInfo.textElementInfo[m_CharacterCount].adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
                }

                #endregion

                // Handle Diacritical Marks
                #region Handle Diacritical Marks

                bool isBaseGlyph = TextGeneratorUtilities.IsBaseGlyph((uint)charCode);

                if (isBaseGlyph)
                    m_LastBaseGlyphIndex = m_CharacterCount;

                if (m_CharacterCount > 0 && !isBaseGlyph)
                {
                    // Check for potential Mark-to-Base lookup if previous glyph was a base glyph
                    if (markToBase && m_LastBaseGlyphIndex != int.MinValue && m_LastBaseGlyphIndex == m_CharacterCount - 1)
                    {
                        Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                        uint baseGlyphIndex = baseGlyph.index;
                        uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                        uint key = markGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                        {
                            float advanceOffset = (textInfo.textElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

                            glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                            glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                            characterSpacingAdjustment = 0;
                        }
                    }
                    else
                    {
                        // Iterate from previous glyph to last base glyph checking for any potential Mark-to-Mark lookups to apply. Otherwise check for potential Mark-to-Base lookup between the current glyph and last base glyph
                        bool wasLookupApplied = false;

                        // Check for any potential Mark-to-Mark lookups
                        if (markToMark)
                        {
                            for (int characterLookupIndex = m_CharacterCount - 1; characterLookupIndex >= 0 && characterLookupIndex != m_LastBaseGlyphIndex; characterLookupIndex--)
                            {
                                // Handle any potential Mark-to-Mark lookup
                                Glyph baseMarkGlyph = textInfo.textElementInfo[characterLookupIndex].textElement.glyph;
                                uint baseGlyphIndex = baseMarkGlyph.index;
                                uint combiningMarkGlyphIndex = m_CachedTextElement.glyphIndex;
                                uint key = combiningMarkGlyphIndex << 16 | baseGlyphIndex;

                                if (m_CurrentFontAsset.fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.TryGetValue(key, out MarkToMarkAdjustmentRecord glyphAdjustmentRecord))
                                {
                                    float baseMarkOrigin = (textInfo.textElementInfo[characterLookupIndex].origin - m_XAdvance) / currentElementScale;
                                    float currentBaseline = baselineOffset - m_LineOffset + m_BaselineOffset;
                                    float baseMarkBaseline = (textInfo.textElementInfo[characterLookupIndex].baseLine - currentBaseline) / currentElementScale;

                                    glyphAdjustments.xPlacement = baseMarkOrigin + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.xPositionAdjustment;
                                    glyphAdjustments.yPlacement = baseMarkBaseline + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.yPositionAdjustment;

                                    characterSpacingAdjustment = 0;
                                    wasLookupApplied = true;
                                    break;
                                }
                            }
                        }

                        // If no Mark-to-Mark lookups were applied, check for potential Mark-to-Base lookup.
                        if (markToBase && m_LastBaseGlyphIndex != int.MinValue && !wasLookupApplied)
                        {
                            // Handle lookup for Mark-to-Base
                            Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                            uint baseGlyphIndex = baseGlyph.index;
                            uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                            uint key = markGlyphIndex << 16 | baseGlyphIndex;

                            if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float advanceOffset = (textInfo.textElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

                                glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                            }
                        }
                    }
                }

                // Adjust relevant text metrics
                elementAscentLine += glyphAdjustments.yPlacement;
                elementDescentLine += glyphAdjustments.yPlacement;

                #endregion

                // Initial Implementation for RTL support.
                #region Handle Right-to-Left

                if (generationSettings.isRightToLeft)
                {
                    m_XAdvance -= currentGlyphMetrics.horizontalAdvance * (1 - m_CharWidthAdjDelta) * currentElementScale;

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance -= generationSettings.wordSpacing * currentEmScale;
                }

                #endregion

                // Handle Mono Spacing
                #region Handle Mono Spacing

                float monoAdvance = 0;
                if (m_MonoSpacing != 0 && charCode != k_ZeroWidthSpace)
                {
                    if (m_DuoSpace && (charCode == '.' || charCode == ':' || charCode == ','))
                        monoAdvance = (m_MonoSpacing / 4 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_CharWidthAdjDelta);
                    else
                        monoAdvance = (m_MonoSpacing / 2 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_CharWidthAdjDelta);

                    m_XAdvance += monoAdvance;
                }
                #endregion

                // Set Padding based on selected font style
                #region Handle Style Padding

                float boldSpacingAdjustment;
                float stylePadding;
                bool hasGradientScale = m_CurrentFontAsset.atlasRenderMode != GlyphRenderMode.SMOOTH && m_CurrentFontAsset.atlasRenderMode != GlyphRenderMode.COLOR;
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((textInfo.textElementInfo[m_CharacterCount].style & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                {
                    if (hasGradientScale)
                    {
                        var gradientScale = generationSettings.isIMGUI && m_CurrentMaterial.HasFloat(TextShaderUtilities.ID_GradientScale) ? m_CurrentMaterial.GetFloat(TextShaderUtilities.ID_GradientScale) : m_CurrentFontAsset.atlasPadding + 1;
                        stylePadding = m_CurrentFontAsset.boldStyleWeight / 4.0f * gradientScale;

                        // Clamp overall padding to Gradient Scale size.
                        if (stylePadding + padding > gradientScale)
                            padding = gradientScale - stylePadding;
                    }
                    else
                        stylePadding = 0;
                    boldSpacingAdjustment = m_CurrentFontAsset.boldStyleSpacing;
                }
                else
                {
                    if (hasGradientScale)
                    {
                        var gradientScale = generationSettings.isIMGUI && m_CurrentMaterial.HasFloat(TextShaderUtilities.ID_GradientScale) ? m_CurrentMaterial.GetFloat(TextShaderUtilities.ID_GradientScale) : m_CurrentFontAsset.atlasPadding + 1;
                        stylePadding = m_CurrentFontAsset.m_RegularStyleWeight / 4.0f * gradientScale;

                        // Clamp overall padding to Gradient Scale size.
                        if (stylePadding + padding > gradientScale)
                            padding = gradientScale - stylePadding;
                    }
                    else
                        stylePadding = 0;
                    boldSpacingAdjustment = 0;
                }

                #endregion Handle Style Padding

                // Determine the position of the vertices of the Character or Sprite.
                #region Calculate Vertices Position

                Vector3 topLeft;
                topLeft.x = m_XAdvance + ((currentGlyphMetrics.horizontalBearingX * m_FXScale.x - padding - stylePadding + glyphAdjustments.xPlacement) * currentElementScale * (1 - m_CharWidthAdjDelta));
                topLeft.y = baselineOffset + Round((currentGlyphMetrics.horizontalBearingY + padding + glyphAdjustments.yPlacement) * currentElementScale) - m_LineOffset + m_BaselineOffset;
                topLeft.z = 0;

                Vector3 bottomLeft;
                bottomLeft.x = topLeft.x;
                bottomLeft.y = topLeft.y - ((currentGlyphMetrics.height + padding * 2) * currentElementScale);
                bottomLeft.z = 0;

                Vector3 topRight;
                topRight.x = bottomLeft.x + ((currentGlyphMetrics.width * m_FXScale.x + padding * 2 + stylePadding * 2) * currentElementScale * (1 - m_CharWidthAdjDelta));
                topRight.y = topLeft.y;
                topRight.z = 0;

                Vector3 bottomRight;
                bottomRight.x = topRight.x;
                bottomRight.y = bottomLeft.y;
                bottomRight.z = 0;

                #endregion

                // Check if we need to Shear the rectangles for Italic styles
                #region Handle Italic & Shearing

                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic))
                {
                    // Shift Top vertices forward by half (Shear Value * height of character) and Bottom vertices back by same amount.
                    float shearValue = m_ItalicAngle * 0.01f;
                    float midPoint = ((m_CurrentFontAsset.m_FaceInfo.capLine - (m_CurrentFontAsset.m_FaceInfo.baseline + m_BaselineOffset)) / 2) * m_FontScaleMultiplier * m_CurrentFontAsset.m_FaceInfo.scale;
                    Vector3 topShear = new Vector3(shearValue * ((currentGlyphMetrics.horizontalBearingY + padding + stylePadding - midPoint) * currentElementScale), 0, 0);
                    Vector3 bottomShear = new Vector3(shearValue * (((currentGlyphMetrics.horizontalBearingY - currentGlyphMetrics.height - padding - stylePadding - midPoint)) * currentElementScale), 0, 0);

                    topLeft += topShear;
                    bottomLeft += bottomShear;
                    topRight += topShear;
                    bottomRight += bottomShear;
                }

                #endregion Handle Italics & Shearing

                // Handle Character FX Rotation
                #region Handle Character FX Rotation

                if (m_FXRotation != Quaternion.identity)
                {
                    Matrix4x4 rotationMatrix = Matrix4x4.Rotate(m_FXRotation);
                    Vector3 positionOffset = (topRight + bottomLeft) / 2;

                    topLeft = rotationMatrix.MultiplyPoint3x4(topLeft - positionOffset) + positionOffset;
                    bottomLeft = rotationMatrix.MultiplyPoint3x4(bottomLeft - positionOffset) + positionOffset;
                    topRight = rotationMatrix.MultiplyPoint3x4(topRight - positionOffset) + positionOffset;
                    bottomRight = rotationMatrix.MultiplyPoint3x4(bottomRight - positionOffset) + positionOffset;
                }

                #endregion

                // Store vertex information for the character or sprite.
                textInfo.textElementInfo[m_CharacterCount].bottomLeft = bottomLeft;
                textInfo.textElementInfo[m_CharacterCount].topLeft = topLeft;
                textInfo.textElementInfo[m_CharacterCount].topRight = topRight;
                textInfo.textElementInfo[m_CharacterCount].bottomRight = bottomRight;

                textInfo.textElementInfo[m_CharacterCount].origin = Round(m_XAdvance + glyphAdjustments.xPlacement * currentElementScale);
                textInfo.textElementInfo[m_CharacterCount].baseLine = Round((baselineOffset - m_LineOffset + m_BaselineOffset) + glyphAdjustments.yPlacement * currentElementScale);
                textInfo.textElementInfo[m_CharacterCount].aspectRatio = (topRight.x - bottomLeft.x) / (topLeft.y - bottomLeft.y);

                // Compute text metrics
                #region Compute Ascender & Descender values

                // Element Ascender in line space
                float elementAscender = m_TextElementType == TextElementType.Character
                    ? elementAscentLine * currentElementScale / smallCapsMultiplier + m_BaselineOffset
                    : elementAscentLine * currentElementScale + m_BaselineOffset;

                // Element Descender in line space
                float elementDescender = m_TextElementType == TextElementType.Character
                    ? elementDescentLine * currentElementScale / smallCapsMultiplier + m_BaselineOffset
                    : elementDescentLine * currentElementScale + m_BaselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                // Max line ascender and descender in line space
                bool isFirstCharacterOfLine = m_CharacterCount == m_FirstCharacterOfLine;
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_BaselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max((elementAscender - m_BaselineOffset) / m_FontScaleMultiplier, adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_BaselineOffset) / m_FontScaleMultiplier, adjustedDescender);
                    }

                    m_MaxLineAscender = Mathf.Max(adjustedAscender, m_MaxLineAscender);
                    m_MaxLineDescender = Mathf.Min(adjustedDescender, m_MaxLineDescender);
                }

                // Element Ascender and Descender in object space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    textInfo.textElementInfo[m_CharacterCount].adjustedAscender = adjustedAscender;
                    textInfo.textElementInfo[m_CharacterCount].adjustedDescender = adjustedDescender;

                    textInfo.textElementInfo[m_CharacterCount].ascender = elementAscender - m_LineOffset;
                    m_MaxDescender = textInfo.textElementInfo[m_CharacterCount].descender = elementDescender - m_LineOffset;
                }
                else
                {
                    textInfo.textElementInfo[m_CharacterCount].adjustedAscender = m_MaxLineAscender;
                    textInfo.textElementInfo[m_CharacterCount].adjustedDescender = m_MaxLineDescender;

                    textInfo.textElementInfo[m_CharacterCount].ascender = m_MaxLineAscender - m_LineOffset;
                    m_MaxDescender = textInfo.textElementInfo[m_CharacterCount].descender = m_MaxLineDescender - m_LineOffset;
                }

                // Max text object ascender and cap height
                if (m_LineNumber == 0 )
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_MaxAscender = m_MaxLineAscender;
                        m_MaxCapHeight = Mathf.Max(m_MaxCapHeight, m_CurrentFontAsset.m_FaceInfo.capLine * currentElementScale / smallCapsMultiplier);
                    }
                }

                // Page ascender
                if (m_LineOffset == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }

                #endregion

                // Set Characters to not visible by default.
                textInfo.textElementInfo[m_CharacterCount].isVisible = false;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters

                if (charCode == k_Tab || ((wordWrap == TextWrappingMode.PreserveWhitespace || wordWrap == TextWrappingMode.PreserveWhitespaceNoWrap) && (isWhiteSpace || charCode == k_ZeroWidthSpace)) || (isWhiteSpace == false && charCode != k_ZeroWidthSpace && charCode != k_SoftHyphen && charCode != k_EndOfText) || (charCode == k_SoftHyphen && isSoftHyphenIgnored == false) || m_TextElementType == TextElementType.Sprite)
                {
                    textInfo.textElementInfo[m_CharacterCount].isVisible = true;

                    float marginLeft = m_MarginLeft;
                    float marginRight = m_MarginRight;

                    // Injected characters do not override margins
                    if (isInjectedCharacter)
                    {
                        marginLeft = textInfo.lineInfo[m_LineNumber].marginLeft;
                        marginRight = textInfo.lineInfo[m_LineNumber].marginRight;
                    }

                    widthOfTextArea = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_Width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    // Calculate the line breaking width of the text.
                    float textWidth = Mathf.Abs(m_XAdvance) + (!generationSettings.isRightToLeft ? currentGlyphMetrics.horizontalAdvance : 0) * (1 - m_CharWidthAdjDelta) * (charCode == k_SoftHyphen ? currentElementUnmodifiedScale : currentElementScale);
                    float textHeight = m_MaxAscender - (m_MaxLineDescender - m_LineOffset) + (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0);

                    int testedCharacterCount = m_CharacterCount;

                    // Handling of current line Vertical Bounds

                    #region Current Line Vertical Bounds Check

                    if (textHeight > marginHeight + 0.0001f)
                    {
                        // Set isTextOverflowing and firstOverflowCharacterIndex
                        if (m_FirstOverflowCharacterIndex == -1)
                            m_FirstOverflowCharacterIndex = m_CharacterCount;

                        // Check if Auto-Size is enabled
                        if (TextGenerationSettings.autoSize)
                        {
                            // Handle Line spacing adjustments

                            #region Line Spacing Adjustments

#pragma warning disable CS0162 // Unreachable code detected
                            if (m_LineSpacingDelta > TextGenerationSettings.lineSpacingMax && m_LineOffset > 0 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                float adjustmentDelta = (marginHeight - textHeight) / m_LineNumber;

                                m_LineSpacingDelta = Mathf.Max(m_LineSpacingDelta + adjustmentDelta / baseScale, TextGenerationSettings.lineSpacingMax);

                                return;
                            }
#pragma warning restore CS0162 // Unreachable code detected

                            #endregion


                            // Handle Text Auto-sizing resulting from text exceeding vertical bounds.

                            #region Text Auto-Sizing (Text greater than vertical bounds)

                            if (m_FontSize > TextGenerationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                m_MaxFontSize = m_FontSize;

                                float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                m_FontSize -= sizeDelta;
                                m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, TextGenerationSettings.fontSizeMin);

                                return;
                            }

                            #endregion Text Auto-Sizing
                        }

                        // Handle Vertical Overflow on current line
                        switch (generationSettings.overflowMode)
                        {
                            case TextOverflowMode.Overflow:
                            case TextOverflowMode.ScrollRect:
                            case TextOverflowMode.Masking:
                                // Nothing happens as vertical bounds are ignored in this mode.
                                break;

                            case TextOverflowMode.Truncate:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                                characterToSubstitute.index = testedCharacterCount;
                                characterToSubstitute.unicode = k_EndOfText;
                                continue;

                            case TextOverflowMode.Ellipsis:
                                if (m_LineNumber > 0)
                                {
                                    if (m_EllipsisInsertionCandidateStack.Count == 0)
                                    {
                                        i = -1;
                                        m_CharacterCount = 0;
                                        characterToSubstitute.index = 0;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        m_FirstCharacterOfLine = 0;
                                        continue;
                                    }

                                    var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                    i = RestoreWordWrappingState(ref ellipsisState, textInfo);

                                    i -= 1;
                                    m_CharacterCount -= 1;
                                    characterToSubstitute.index = m_CharacterCount;
                                    characterToSubstitute.unicode = k_HorizontalEllipsis;

                                    restoreCount += 1;
                                    continue;
                                }

                                break;
                            case TextOverflowMode.Linked:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                                // Truncate remaining text
                                characterToSubstitute.index = testedCharacterCount;
                                characterToSubstitute.unicode = k_EndOfText;
                                continue;

                        }
                    }

                    #endregion

                    // Handling of Horizontal Bounds

                    #region Current Line Horizontal Bounds Check

                    if (isBaseGlyph && textWidth > widthOfTextArea)
                    {
                        // Handle Line Breaking (if still possible)
                        if (wordWrap != TextWrappingMode.NoWrap && wordWrap != TextWrappingMode.PreserveWhitespaceNoWrap && m_CharacterCount != m_FirstCharacterOfLine)
                        {
                            // Restore state to previous safe line breaking
                            i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);

                            // Compute potential new line offset in the event a line break is needed.
                            float lineOffsetDelta = 0;
                            if (m_LineHeight == k_FloatUnset)
                            {
                                float ascender = textInfo.textElementInfo[m_CharacterCount].adjustedAscender;
                                lineOffsetDelta = (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0) - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + TextGenerationSettings.lineSpacing * currentEmScale;
                            }
                            else
                            {
                                lineOffsetDelta = m_LineHeight + TextGenerationSettings.lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            // Calculate new text height
                            float newTextHeight = m_MaxAscender + lineOffsetDelta + m_LineOffset - textInfo.textElementInfo[m_CharacterCount].adjustedDescender;

                            // Replace Soft Hyphen by Hyphen Minus 0x2D
                            #region Handle Soft Hyphenation
                            if (textInfo.textElementInfo[m_CharacterCount - 1].character == k_SoftHyphen && isSoftHyphenIgnored == false)
                            {
                                // Only inject Hyphen Minus if new line is possible
                                if (generationSettings.overflowMode == TextOverflowMode.Overflow || newTextHeight < marginHeight + 0.0001f)
                                {
                                    characterToSubstitute.index = m_CharacterCount - 1;
                                    characterToSubstitute.unicode = k_HyphenMinus;

                                    i -= 1;
                                    m_CharacterCount -= 1;
                                    continue;
                                }
                            }

                            isSoftHyphenIgnored = false;

                            // Ignore Soft Hyphen to prevent it from wrapping
                            if (textInfo.textElementInfo[m_CharacterCount].character == k_SoftHyphen)
                            {
                                isSoftHyphenIgnored = true;
                                continue;
                            }
                            #endregion

                            // Adjust character spacing before breaking up word if auto size is enabled
                            if (TextGenerationSettings.autoSize && isFirstWordOfLine)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments

                                if (m_CharWidthAdjDelta < TextGenerationSettings.charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_CharWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                    m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, TextGenerationSettings.charWidthMaxAdj / 100);

                                    return;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                #region Text Auto-Sizing (Text greater than vertical bounds)

                                if (m_FontSize > TextGenerationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_MaxFontSize = m_FontSize;

                                    float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                    m_FontSize -= sizeDelta;
                                    m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, TextGenerationSettings.fontSizeMin);

                                    return;
                                }
                                #endregion Text Auto-Sizing
                            }

                            // Special handling if first word of line and non breaking space
                            int savedSoftLineBreakingSpace = m_SavedSoftLineBreakState.previousWordBreak;
                            if (isFirstWordOfLine && savedSoftLineBreakingSpace != -1)
                            {
                                if (savedSoftLineBreakingSpace != lastSoftLineBreak)
                                {
                                    i = RestoreWordWrappingState(ref m_SavedSoftLineBreakState, textInfo);
                                    lastSoftLineBreak = savedSoftLineBreakingSpace;

                                    // check if soft hyphen
                                    if (textInfo.textElementInfo[m_CharacterCount - 1].character == k_SoftHyphen)
                                    {
                                        characterToSubstitute.index = m_CharacterCount - 1;
                                        characterToSubstitute.unicode = k_HyphenMinus;

                                        i -= 1;
                                        m_CharacterCount -= 1;
                                        continue;
                                    }
                                }
                            }

                            // Determine if new line of text would exceed the vertical bounds of text container
                            if (newTextHeight > marginHeight + 0.0001f)
                            {
                                // Set isTextOverflowing and firstOverflowCharacterIndex
                                if (m_FirstOverflowCharacterIndex == -1)
                                    m_FirstOverflowCharacterIndex = m_CharacterCount;

                                // Check if Auto-Size is enabled
                                if (TextGenerationSettings.autoSize)
                                {
                                    // Handle Line spacing adjustments
                                    #region Line Spacing Adjustments

#pragma warning disable CS0162 // Unreachable code detected
                                    if (m_LineSpacingDelta > TextGenerationSettings.lineSpacingMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustmentDelta = (marginHeight - newTextHeight) / (m_LineNumber + 1);

                                        m_LineSpacingDelta = Mathf.Max(m_LineSpacingDelta + adjustmentDelta / baseScale, TextGenerationSettings.lineSpacingMax);
                                        return;
                                    }
#pragma warning restore CS0162 // Unreachable code detected

                                    #endregion

                                    // Handle Character Width Adjustments
                                    #region Character Width Adjustments

                                    if (m_CharWidthAdjDelta < TextGenerationSettings.charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustedTextWidth = textWidth;

                                        // Determine full width of the text
                                        if (m_CharWidthAdjDelta > 0)
                                            adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                        float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                        m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                        m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, TextGenerationSettings.charWidthMaxAdj / 100);

                                        return;
                                    }

                                    #endregion

                                    // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                    #region Text Auto-Sizing (Text greater than vertical bounds)

                                    if (m_FontSize > TextGenerationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        m_MaxFontSize = m_FontSize;

                                        float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                        m_FontSize -= sizeDelta;
                                        m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, TextGenerationSettings.fontSizeMin);

                                        return;
                                    }

                                    #endregion Text Auto-Sizing
                                }

                                // Check Text Overflow Modes
                                switch (generationSettings.overflowMode)
                                {
                                    case TextOverflowMode.Overflow:
                                    case TextOverflowMode.ScrollRect:
                                    case TextOverflowMode.Masking:
                                        InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender, generationSettings, textInfo);
                                        isStartOfNewLine = true;
                                        isFirstWordOfLine = true;
                                        continue;

                                    case TextOverflowMode.Truncate:
                                        i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                                        characterToSubstitute.index = testedCharacterCount;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        continue;

                                    case TextOverflowMode.Ellipsis:
                                        if (m_EllipsisInsertionCandidateStack.Count == 0)
                                        {
                                            i = -1;
                                            m_CharacterCount = 0;
                                            characterToSubstitute.index = 0;
                                            characterToSubstitute.unicode = k_EndOfText;
                                            m_FirstCharacterOfLine = 0;
                                            continue;
                                        }

                                        var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                        i = RestoreWordWrappingState(ref ellipsisState, textInfo);

                                        i -= 1;
                                        m_CharacterCount -= 1;
                                        characterToSubstitute.index = m_CharacterCount;
                                        characterToSubstitute.unicode = k_HorizontalEllipsis;

                                        restoreCount += 1;
                                        continue;
                                    case TextOverflowMode.Linked:
                                        // Truncate remaining text
                                        characterToSubstitute.index = m_CharacterCount;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        continue;

                                }
                            }
                            else
                            {
                                //if (generationSettings.autoSize && isFirstWordOfLine)
                                //{
                                //    // Handle Character Width Adjustments
                                //    #region Character Width Adjustments
                                //    if (m_CharWidthAdjDelta < m_CharWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                //    {
                                //        //m_AutoSizeIterationCount = 0;
                                //        float adjustedTextWidth = textWidth;

                                //        // Determine full width of the text
                                //        if (m_CharWidthAdjDelta > 0)
                                //            adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                //        float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                //        m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                //        m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, m_CharWidthMaxAdj / 100);

                                //        //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Character Width by " + (m_CharWidthAdjDelta * 100) + "%");

                                //        GenerateTextMesh();
                                //        return;
                                //    }
                                //    #endregion
                                //}

                                // New line of text does not exceed vertical bounds of text container
                                InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender, generationSettings, textInfo);
                                isStartOfNewLine = true;
                                isFirstWordOfLine = true;
                                continue;
                            }
                        }
                        else
                        {
                            if (TextGenerationSettings.autoSize && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments

                                if (m_CharWidthAdjDelta < TextGenerationSettings.charWidthMaxAdj / 100)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_CharWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                    m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, TextGenerationSettings.charWidthMaxAdj / 100);

                                    return;
                                }

                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding horizontal bounds.
                                #region Text Exceeds Horizontal Bounds - Reducing Point Size

                                if (m_FontSize > TextGenerationSettings.fontSizeMin)
                                {
                                    // Adjust Point Size
                                    m_MaxFontSize = m_FontSize;

                                    float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                    m_FontSize -= sizeDelta;
                                    m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, TextGenerationSettings.fontSizeMin);

                                    return;
                                }

                                #endregion
                            }

                            // Check Text Overflow Modes
                            switch (generationSettings.overflowMode)
                            {
                                case TextOverflowMode.Overflow:
                                case TextOverflowMode.ScrollRect:
                                case TextOverflowMode.Masking:
                                    // Nothing happens as horizontal bounds are ignored in this mode.
                                    break;

                                case TextOverflowMode.Truncate:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);

                                    characterToSubstitute.index = testedCharacterCount;
                                    characterToSubstitute.unicode = k_EndOfText;
                                    continue;

                                case TextOverflowMode.Ellipsis:
                                    if (m_EllipsisInsertionCandidateStack.Count == 0)
                                    {
                                        i = -1;
                                        m_CharacterCount = 0;
                                        characterToSubstitute.index = 0;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        m_FirstCharacterOfLine = 0;
                                        continue;
                                    }

                                    var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                    i = RestoreWordWrappingState(ref ellipsisState, textInfo);

                                    i -= 1;
                                    m_CharacterCount -= 1;
                                    characterToSubstitute.index = m_CharacterCount;
                                    characterToSubstitute.unicode = k_HorizontalEllipsis;

                                    restoreCount += 1;
                                    continue;
                                case TextOverflowMode.Linked:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);

                                    // Truncate text the overflows the vertical bounds
                                    characterToSubstitute.index = m_CharacterCount;
                                    characterToSubstitute.unicode = k_EndOfText;
                                    continue;
                            }

                        }
                    }
                    #endregion

                    // Special handling of characters that are not ignored at the end of a line.
                    if (isWhiteSpace)
                    {
                        textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                        m_LineVisibleSpaceCount = textInfo.lineInfo[m_LineNumber].spaceCount += 1;
                        textInfo.lineInfo[m_LineNumber].marginLeft = marginLeft;
                        textInfo.lineInfo[m_LineNumber].marginRight = marginRight;
                        textInfo.spaceCount += 1;
                        if (charCode == k_NoBreakSpace)
                            textInfo.lineInfo[m_LineNumber].controlCharacterCount += 1;
                    }
                    else if (charCode == k_SoftHyphen)
                    {
                        textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                    }
                    else
                    {
                        // Determine Vertex Color
                        Color32 vertexColor = m_HtmlColor;

                        // Store Character & Sprite Vertex Information
                        if (m_TextElementType == TextElementType.Character)
                        {
                            // Save Character Vertex Data
                            SaveGlyphVertexInfo(padding, stylePadding, vertexColor, generationSettings, textInfo);
                        }
                        else if (m_TextElementType == TextElementType.Sprite)
                        {
                            SaveSpriteVertexInfo(vertexColor, generationSettings, textInfo);
                        }

                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_FirstVisibleCharacterOfLine = m_CharacterCount;
                        }

                        m_LineVisibleCharacterCount += 1;
                        m_LastVisibleCharacterOfLine = m_CharacterCount;
                        textInfo.lineInfo[m_LineNumber].marginLeft = marginLeft;
                        textInfo.lineInfo[m_LineNumber].marginRight = marginRight;
                    }
                }
                else
                {
                    // Special handling for text overflow linked mode
                    #region Check Vertical Bounds

                    if (generationSettings.overflowMode == TextOverflowMode.Linked && (charCode == k_LineFeed || charCode == 11))
                    {
                        float textHeight = m_MaxAscender - (m_MaxLineDescender - m_LineOffset) + (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0);

                        int testedCharacterCount = m_CharacterCount;

                        if (textHeight > marginHeight + 0.0001f)
                        {
                            // Set isTextOverflowing and firstOverflowCharacterIndex
                            if (m_FirstOverflowCharacterIndex == -1)
                                m_FirstOverflowCharacterIndex = m_CharacterCount;

                            i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                            // Truncate remaining text
                            characterToSubstitute.index = testedCharacterCount;
                            characterToSubstitute.unicode = k_EndOfText;
                            continue;
                        }
                    }

                    #endregion

                    // Track # of spaces per line which is used for line justification.
                    if ((charCode == k_LineFeed || charCode == k_VerticalTab || charCode == k_NoBreakSpace || charCode == k_FigureSpace || charCode == k_LineSeparator || charCode == k_ParagraphSeparator || char.IsSeparator((char)charCode)) && charCode != k_SoftHyphen && charCode != k_ZeroWidthSpace && charCode != k_WordJoiner)
                    {
                        textInfo.lineInfo[m_LineNumber].spaceCount += 1;
                        textInfo.spaceCount += 1;
                    }

                    // Special handling for control characters like <NBSP>
                    if (charCode == k_NoBreakSpace)
                        textInfo.lineInfo[m_LineNumber].controlCharacterCount += 1;

                }
                #endregion Handle Visible Characters

                // Tracking of potential insertion positions for Ellipsis character
                #region Track Potential Insertion Location for Ellipsis

                if (generationSettings.overflowMode == TextOverflowMode.Ellipsis && (isInjectedCharacter == false || charCode == k_HyphenMinus))
                {
                    float fontScale = m_CurrentFontSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale;
                    float scale = fontScale * m_FontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                    float marginLeft = m_MarginLeft;
                    float marginRight = m_MarginRight;

                    // Use the scale and margins of the previous character if Line Feed (LF) is not the first character of a line.
                    if (charCode == 0x0A && m_CharacterCount != m_FirstCharacterOfLine)
                    {
                        fontScale = textInfo.textElementInfo[m_CharacterCount - 1].pointSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale;
                        scale = fontScale * m_FontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                        marginLeft = textInfo.lineInfo[m_LineNumber].marginLeft;
                        marginRight = textInfo.lineInfo[m_LineNumber].marginRight;
                    }

                    float textWidth = Mathf.Abs(m_XAdvance) + (!generationSettings.isRightToLeft ? m_Ellipsis.character.m_Glyph.metrics.horizontalAdvance : 0) * (1 - m_CharWidthAdjDelta) * scale;
                    float widthOfTextAreaForEllipsis = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_Width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    if (textWidth < widthOfTextAreaForEllipsis)
                    {
                        SaveWordWrappingState(ref m_SavedEllipsisState, i, m_CharacterCount, textInfo);
                        m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                    }
                }

                #endregion

                // Store Rectangle positions for each Character.
                #region Store Character Data

                textInfo.textElementInfo[m_CharacterCount].lineNumber = m_LineNumber;

                if (charCode != k_LineFeed && charCode != k_VerticalTab && charCode != k_CarriageReturn && isInjectedCharacter == false /* && charCode != k_HorizontalEllipsis */ || textInfo.lineInfo[m_LineNumber].characterCount == 1)
                    textInfo.lineInfo[m_LineNumber].alignment = m_LineJustification;

                #endregion Store Character Data

                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops

                if (charCode != k_ZeroWidthSpace)
                {
                    if (charCode == k_Tab)
                    {
                        float tabSize = m_CurrentFontAsset.m_FaceInfo.tabWidth * m_CurrentFontAsset.tabMultiple * currentElementScale;
                        float tabs = Mathf.Ceil(m_XAdvance / tabSize) * tabSize;
                        m_XAdvance = tabs > m_XAdvance ? tabs : m_XAdvance + tabSize;
                    }
                    else if (m_MonoSpacing != 0)
                    {
                        float monoAdjustment;
                        if (m_DuoSpace && (charCode == '.' || charCode == ':' || charCode == ','))
                            monoAdjustment = m_MonoSpacing / 2 - monoAdvance;
                        else
                            monoAdjustment = m_MonoSpacing - monoAdvance;

                        m_XAdvance += (monoAdjustment + ((m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment) * currentEmScale) + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                        if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                            m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                    }
                    else if (generationSettings.isRightToLeft)
                    {
                        m_XAdvance -= ((glyphAdjustments.xAdvance * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta));

                        if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                            m_XAdvance -= generationSettings.wordSpacing * currentEmScale;
                    }
                    else
                    {
                        m_XAdvance += ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                        if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                            m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                    }
                }

                // Store xAdvance information
                textInfo.textElementInfo[m_CharacterCount].xAdvance = m_XAdvance ;

                #endregion Tabulation & Stops

                // Handle Carriage Return
                #region Carriage Return

                if (charCode == k_CarriageReturn)
                {
                    m_XAdvance = 0 + m_TagIndent;
                }

                #endregion Carriage Return

               

                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character

                if (charCode == k_LineFeed || charCode == k_VerticalTab || charCode == k_EndOfText || charCode == k_LineSeparator || charCode == k_LineSeparator || (charCode == k_HyphenMinus && isInjectedCharacter) || m_CharacterCount == totalCharacterCount - 1)
                {
                    // Adjust current line spacing (if necessary) before inserting new line
                    float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
                    {
                        TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount,Round( baselineAdjustmentDelta), textInfo);
                        m_MaxDescender -= baselineAdjustmentDelta;
                        m_LineOffset += baselineAdjustmentDelta;

                        // Adjust saved ellipsis state only if we are adjusting the same line number
                        if (m_SavedEllipsisState.lineNumber == m_LineNumber)
                        {
                            m_SavedEllipsisState = m_EllipsisInsertionCandidateStack.Pop();
                            m_SavedEllipsisState.startOfLineAscender += baselineAdjustmentDelta;
                            m_SavedEllipsisState.lineOffset += baselineAdjustmentDelta;
                            m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                        }
                    }


                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    float lineAscender = m_MaxLineAscender - m_LineOffset;
                    float lineDescender = m_MaxLineDescender - m_LineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;
                    if (!isMaxVisibleDescenderSet)
                        maxVisibleDescender = m_MaxDescender;

                    if (TextGenerationSettings.useMaxVisibleDescender && (m_CharacterCount >= TextGenerationSettings.maxVisibleCharacters || m_LineNumber >= TextGenerationSettings.maxVisibleLines))
                        isMaxVisibleDescenderSet = true;

                    // Save Line Information
                    textInfo.lineInfo[m_LineNumber].firstCharacterIndex = m_FirstCharacterOfLine;
                    textInfo.lineInfo[m_LineNumber].firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine = m_FirstCharacterOfLine > m_FirstVisibleCharacterOfLine ? m_FirstCharacterOfLine : m_FirstVisibleCharacterOfLine;
                    textInfo.lineInfo[m_LineNumber].lastCharacterIndex = m_LastCharacterOfLine = m_CharacterCount;
                    textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex = m_LastVisibleCharacterOfLine = m_LastVisibleCharacterOfLine < m_FirstVisibleCharacterOfLine ? m_FirstVisibleCharacterOfLine : m_LastVisibleCharacterOfLine;

                    int firstCharacterIndex = m_FirstVisibleCharacterOfLine;
                    int lastCharacterIndex = m_LastVisibleCharacterOfLine;
                    if (generationSettings.textWrappingMode == TextWrappingMode.PreserveWhitespace || generationSettings.textWrappingMode == TextWrappingMode.PreserveWhitespaceNoWrap)
                    {
                        // If last non visible character is /n it will have a xAdvance of 0
                        if (textInfo.textElementInfo[m_LastCharacterOfLine].xAdvance != 0)
                        {
                            firstCharacterIndex = m_FirstCharacterOfLine;
                            lastCharacterIndex = m_LastCharacterOfLine;
                        }
                    }

                    textInfo.lineInfo[m_LineNumber].characterCount = textInfo.lineInfo[m_LineNumber].lastCharacterIndex - textInfo.lineInfo[m_LineNumber].firstCharacterIndex + 1;
                    textInfo.lineInfo[m_LineNumber].visibleCharacterCount = m_LineVisibleCharacterCount;
                    textInfo.lineInfo[m_LineNumber].visibleSpaceCount = (textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex + 1 - textInfo.lineInfo[m_LineNumber].firstCharacterIndex) - m_LineVisibleCharacterCount;
                    textInfo.lineInfo[m_LineNumber].lineExtents.min = new Vector2(textInfo.textElementInfo[firstCharacterIndex].bottomLeft.x, lineDescender);
                    textInfo.lineInfo[m_LineNumber].lineExtents.max = new Vector2(textInfo.textElementInfo[lastCharacterIndex].topRight.x, lineAscender);
                    // UUM-46147: For IMGUI line length should include xAdvance for backward compatibility
                    textInfo.lineInfo[m_LineNumber].length = generationSettings.isIMGUI ? textInfo.textElementInfo[lastCharacterIndex].xAdvance : textInfo.lineInfo[m_LineNumber].lineExtents.max.x - (padding * currentElementScale);
                    textInfo.lineInfo[m_LineNumber].width = widthOfTextArea;

                    if (textInfo.lineInfo[m_LineNumber].characterCount == 1)
                        textInfo.lineInfo[m_LineNumber].alignment = m_LineJustification;

                    float maxAdvanceOffset = ((m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);
                    if (textInfo.textElementInfo[m_LastVisibleCharacterOfLine].isVisible)
                        textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance + (generationSettings.isRightToLeft ? maxAdvanceOffset : -maxAdvanceOffset);
                    else
                        textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastCharacterOfLine].xAdvance + (generationSettings.isRightToLeft ? maxAdvanceOffset : -maxAdvanceOffset);

                    textInfo.lineInfo[m_LineNumber].baseline = 0 - m_LineOffset;
                    textInfo.lineInfo[m_LineNumber].ascender = lineAscender;
                    textInfo.lineInfo[m_LineNumber].descender = lineDescender;
                    textInfo.lineInfo[m_LineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

                    // Add new line if not last line or character.
                    if (charCode == k_LineFeed || charCode == k_VerticalTab|| charCode == k_HyphenMinus || charCode == k_LineSeparator || charCode == k_ParagraphSeparator)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref m_SavedLineState, i, m_CharacterCount, textInfo);

                        m_LineNumber += 1;
                        isStartOfNewLine = true;
                        ignoreNonBreakingSpace = false;
                        isFirstWordOfLine = true;

                        m_FirstCharacterOfLine = m_CharacterCount + 1;
                        m_LineVisibleCharacterCount = 0;
                        m_LineVisibleSpaceCount = 0;

                        // Check to make sure Array is large enough to hold a new line.
                        if (m_LineNumber >= textInfo.lineInfo.Length)
                            TextGeneratorUtilities.ResizeLineExtents(m_LineNumber, textInfo);

                        float lastVisibleAscender = textInfo.textElementInfo[m_CharacterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_LineHeight == k_FloatUnset)
                        {
                            float lineOffsetDelta = 0 - m_MaxLineDescender + lastVisibleAscender + (lineGap + m_LineSpacingDelta) * baseScale + (TextGenerationSettings.lineSpacing + (charCode == k_LineFeed || charCode == k_ParagraphSeparator ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_LineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_LineOffset += m_LineHeight + (TextGenerationSettings.lineSpacing + (charCode == k_LineFeed || charCode == k_ParagraphSeparator ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                        m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                        m_StartOfLineAscender = lastVisibleAscender;

                        m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;

                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                        SaveWordWrappingState(ref m_SavedLastValidState, i, m_CharacterCount, textInfo);

                        m_CharacterCount += 1;

                        continue;
                    }

                    // If End of Text
                    if (charCode == k_EndOfText)
                        i = m_TextProcessingArray.Length;
                }

                #endregion Check for Linefeed or Last Character

                // Track extents of the text
                #region Track Text Extents

                // Determine the bounds of the Mesh.
                if (textInfo.textElementInfo[m_CharacterCount].isVisible)
                {
                    m_MeshExtents.min.x = Mathf.Min(m_MeshExtents.min.x, textInfo.textElementInfo[m_CharacterCount].bottomLeft.x);
                    m_MeshExtents.min.y = Mathf.Min(m_MeshExtents.min.y, textInfo.textElementInfo[m_CharacterCount].bottomLeft.y);

                    m_MeshExtents.max.x = Mathf.Max(m_MeshExtents.max.x, textInfo.textElementInfo[m_CharacterCount].topRight.x);
                    m_MeshExtents.max.y = Mathf.Max(m_MeshExtents.max.y, textInfo.textElementInfo[m_CharacterCount].topRight.y);
                }

                #endregion Track Text Extents

                // Save State of Mesh Creation for handling of Word Wrapping
                #region Save Word Wrapping State

                if ((wordWrap != TextWrappingMode.NoWrap && wordWrap != TextWrappingMode.PreserveWhitespaceNoWrap) || generationSettings.overflowMode == TextOverflowMode.Truncate || generationSettings.overflowMode == TextOverflowMode.Ellipsis || generationSettings.overflowMode == TextOverflowMode.Linked)
                {
                    bool shouldSaveHardLineBreak = false;
                    bool shouldSaveSoftLineBreak = false;

                    if ((isWhiteSpace || charCode == k_ZeroWidthSpace || (charCode == k_HyphenMinus && (m_CharacterCount <= 0 || char.IsWhiteSpace((char)textInfo.textElementInfo[m_CharacterCount - 1].character) == false)) || charCode == k_SoftHyphen) && (!m_IsNonBreakingSpace || ignoreNonBreakingSpace) && charCode != k_NoBreakSpace && charCode != k_FigureSpace && charCode != k_NonBreakingHyphen && charCode != k_NarrowNoBreakSpace && charCode != k_WordJoiner)
                    {
                        // Ignore Hyphen (0x2D) when preceded by a whitespace
                        if ((charCode == k_HyphenMinus && m_CharacterCount > 0 && char.IsWhiteSpace((char)textInfo.textElementInfo[m_CharacterCount - 1].character)) == false)
                        {
                            isFirstWordOfLine = false;
                            shouldSaveHardLineBreak = true;

                            //Reset soft line breaking point since we now have a valid hard break point.
                            m_SavedSoftLineBreakState.previousWordBreak = -1;
                        }
                    }
                    // Handling for East Asian scripts
                    else if (m_IsNonBreakingSpace == false && (TextGeneratorUtilities.IsHangul(charCode) && textSettings.lineBreakingRules.useModernHangulLineBreakingRules == false || TextGeneratorUtilities.IsCJK(charCode)))
                    {
                        bool isCurrentLeadingCharacter = textSettings.lineBreakingRules.leadingCharactersLookup.Contains((uint)charCode);
                        bool isNextFollowingCharacter = m_CharacterCount < totalCharacterCount - 1 && textSettings.lineBreakingRules.followingCharactersLookup.Contains((uint)textInfo.textElementInfo[m_CharacterCount + 1].character);

                        if (isCurrentLeadingCharacter == false)
                        {
                            if (isNextFollowingCharacter == false)
                            {
                                isFirstWordOfLine = false;
                                shouldSaveHardLineBreak = true;
                            }

                            if (isFirstWordOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    shouldSaveSoftLineBreak = true;

                                shouldSaveHardLineBreak = true;
                            }
                        }
                        else
                        {
                            if (isFirstWordOfLine && isFirstCharacterOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    shouldSaveSoftLineBreak = true;

                                shouldSaveHardLineBreak = true;
                            }
                        }
                    }
                    // Special handling for Latin characters followed by a CJK character.
                    else if (m_IsNonBreakingSpace == false && m_CharacterCount + 1 < totalCharacterCount && TextGeneratorUtilities.IsCJK(textInfo.textElementInfo[m_CharacterCount + 1].character))
                    {
                        shouldSaveHardLineBreak = true;
                    }
                    else if (isFirstWordOfLine)
                    {
                        // Special handling for non-breaking space and soft line breaks
                        if (isWhiteSpace && charCode != k_NoBreakSpace || (charCode == k_SoftHyphen && isSoftHyphenIgnored == false))
                            shouldSaveSoftLineBreak = true;

                        shouldSaveHardLineBreak = true;
                    }

                    // Save potential Hard lines break
                    if (shouldSaveHardLineBreak)
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);

                    // Save potential Soft line break
                    if (shouldSaveSoftLineBreak)
                        SaveWordWrappingState(ref m_SavedSoftLineBreakState, i, m_CharacterCount, textInfo);

                    // k_SaveProcessingStatesMarker.End();
                }

                #endregion Save Word Wrapping State

                // Consider only saving state on base glyphs
                SaveWordWrappingState(ref m_SavedLastValidState, i, m_CharacterCount, textInfo);

                m_CharacterCount += 1;
            }

            CloseAllLinkTags(textInfo);
        }

        void InsertNewLine(int i, float baseScale, float currentElementScale, float currentEmScale, float boldSpacingAdjustment, float characterSpacingAdjustment, float width, float lineGap, ref bool isMaxVisibleDescenderSet, ref float maxVisibleDescender, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Adjust line spacing if necessary
            float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
            if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
            {
                TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, Round( baselineAdjustmentDelta), textInfo);
                m_MaxDescender -= baselineAdjustmentDelta;
                m_LineOffset += baselineAdjustmentDelta;
            }

            // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
            float lineAscender = m_MaxLineAscender - m_LineOffset;
            float lineDescender = m_MaxLineDescender - m_LineOffset;

            // Update maxDescender and maxVisibleDescender
            m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;
            if (!isMaxVisibleDescenderSet)
                maxVisibleDescender = m_MaxDescender;

            if (TextGenerationSettings.useMaxVisibleDescender && (m_CharacterCount >= TextGenerationSettings.maxVisibleCharacters || m_LineNumber >= TextGenerationSettings.maxVisibleLines))
                isMaxVisibleDescenderSet = true;

            // Track & Store lineInfo for the new line
            textInfo.lineInfo[m_LineNumber].firstCharacterIndex = m_FirstCharacterOfLine;
            textInfo.lineInfo[m_LineNumber].firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine = m_FirstCharacterOfLine > m_FirstVisibleCharacterOfLine ? m_FirstCharacterOfLine : m_FirstVisibleCharacterOfLine;
            textInfo.lineInfo[m_LineNumber].lastCharacterIndex = m_LastCharacterOfLine = m_CharacterCount - 1 > 0 ? m_CharacterCount - 1 : 0;
            textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex = m_LastVisibleCharacterOfLine = m_LastVisibleCharacterOfLine < m_FirstVisibleCharacterOfLine ? m_FirstVisibleCharacterOfLine : m_LastVisibleCharacterOfLine;

            textInfo.lineInfo[m_LineNumber].characterCount = textInfo.lineInfo[m_LineNumber].lastCharacterIndex - textInfo.lineInfo[m_LineNumber].firstCharacterIndex + 1;
            textInfo.lineInfo[m_LineNumber].visibleCharacterCount = m_LineVisibleCharacterCount;
            textInfo.lineInfo[m_LineNumber].visibleSpaceCount = (textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex + 1 - textInfo.lineInfo[m_LineNumber].firstCharacterIndex) - m_LineVisibleCharacterCount;
            textInfo.lineInfo[m_LineNumber].lineExtents.min = new Vector2(textInfo.textElementInfo[m_FirstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
            textInfo.lineInfo[m_LineNumber].lineExtents.max = new Vector2(textInfo.textElementInfo[m_LastVisibleCharacterOfLine].topRight.x, lineAscender);
            // UUM-46147: For IMGUI line length should include xAdvance for backward compatibility
            textInfo.lineInfo[m_LineNumber].length = generationSettings.isIMGUI ? textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance : textInfo.lineInfo[m_LineNumber].lineExtents.max.x;
            textInfo.lineInfo[m_LineNumber].width = width;

            float glyphAdjustment = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].adjustedHorizontalAdvance;
            float maxAdvanceOffset = (glyphAdjustment * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - TextGenerationSettings.charWidthMaxAdj);
            float adjustedHorizontalAdvance = textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance + (generationSettings.isRightToLeft ? maxAdvanceOffset : -maxAdvanceOffset);
            textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance = Round(adjustedHorizontalAdvance);

            textInfo.lineInfo[m_LineNumber].baseline = 0 - m_LineOffset;
            textInfo.lineInfo[m_LineNumber].ascender = lineAscender;
            textInfo.lineInfo[m_LineNumber].descender = lineDescender;
            textInfo.lineInfo[m_LineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

            m_FirstCharacterOfLine = m_CharacterCount; // Store first character of the next line.
            m_LineVisibleCharacterCount = 0;
            m_LineVisibleSpaceCount = 0;

            // Store the state of the line before starting on the new line.
            SaveWordWrappingState(ref m_SavedLineState, i, m_CharacterCount - 1, textInfo);

            m_LineNumber += 1;

            // Check to make sure Array is large enough to hold a new line.
            if (m_LineNumber >= textInfo.lineInfo.Length)
                TextGeneratorUtilities.ResizeLineExtents(m_LineNumber, textInfo);

            // Apply Line Spacing based on scale of the last character of the line.
            if (m_LineHeight == k_FloatUnset)
            {
                float ascender = textInfo.textElementInfo[m_CharacterCount].adjustedAscender;
                float lineOffsetDelta = 0 - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + TextGenerationSettings.lineSpacing * currentEmScale;
                m_LineOffset += lineOffsetDelta;

                m_StartOfLineAscender = ascender;
            }
            else
            {
                m_LineOffset += m_LineHeight + TextGenerationSettings.lineSpacing * currentEmScale;
            }

            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;

            m_XAdvance = 0 + m_TagIndent;
        }
    }
}
