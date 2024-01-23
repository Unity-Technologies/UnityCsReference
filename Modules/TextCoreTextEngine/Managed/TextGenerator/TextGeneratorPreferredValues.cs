// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal partial class TextGenerator
    {
        public static Vector2 GetPreferredValues(TextGenerationSettings settings, TextInfo textInfo)
        {
            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return Vector2.zero;
            }

            TextGenerator textGenerator = GetTextGenerator();

            textGenerator.Prepare(settings, textInfo);
            return textGenerator.GetPreferredValuesInternal(settings, textInfo);
        }

        Vector2 GetPreferredValuesInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (generationSettings.textSettings == null)
                return Vector2.zero;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_MarginWidth != 0 ? m_MarginWidth : TextGeneratorUtilities.largePositiveFloat, m_MarginHeight != 0 ? m_MarginHeight : TextGeneratorUtilities.largePositiveFloat);
            TextWrappingMode wrapMode = generationSettings.wordWrap ? TextWrappingMode.Normal : TextWrappingMode.NoWrap;

            m_AutoSizeIterationCount = 0;

            return CalculatePreferredValues(ref fontSize, margin, generationSettings.autoSize, wrapMode, generationSettings, textInfo);
        }

        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize, bool isTextAutoSizingEnabled, TextWrappingMode textWrapMode, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            Profiler.BeginSample("TextGenerator.CalculatePreferredValues");

            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (generationSettings.fontAsset == null || generationSettings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned.");

                return Vector2.zero;
            }

            // Early exit if we don't have any Text to generate.
            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == (char)0)
            {
                Profiler.EndSample();
                return Vector2.zero;
            }

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;
            m_MaterialReferenceStack.SetDefault(new MaterialReference(0, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_TotalCharacterCount; // m_VisibleCharacters.Count;

            if (m_InternalTextElementInfo == null || totalCharacterCount > m_InternalTextElementInfo.Length)
                m_InternalTextElementInfo = new TextElementInfo[totalCharacterCount > 1024 ? totalCharacterCount + 256 : Mathf.NextPowerOfTwo(totalCharacterCount)];

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (fontSize / generationSettings.fontAsset.faceInfo.pointSize * generationSettings.fontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
            float currentElementScale = baseScale;
            float currentEmScale = fontSize * 0.01f * (generationSettings.isOrthographic ? 1 : 0.1f);
            m_FontScaleMultiplier = 1;

            m_CurrentFontSize = fontSize;
            m_SizeStack.SetDefault(m_CurrentFontSize);
            float fontSizeDelta = 0;

            m_FontStyleInternal = generationSettings.fontStyle; // Set the default style.

            m_LineJustification = generationSettings.textAlignment; // m_textAlignment; // Sets the line justification mode to match editor alignment.
            m_LineJustificationStack.SetDefault(m_LineJustification);

            m_BaselineOffset = 0; // Used by subscript characters.
            m_BaselineOffsetStack.Clear();

            m_FXScale = Vector3.one;

            m_LineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_LineHeight = k_FloatUnset;
            float lineGap = m_CurrentFontAsset.faceInfo.lineHeight - (m_CurrentFontAsset.faceInfo.ascentLine - m_CurrentFontAsset.faceInfo.descentLine);

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
            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
            m_LineNumber = 0;
            m_StartOfLineAscender = 0;
            m_IsDrivenLineSpacing = false;
            m_LastBaseGlyphIndex = int.MinValue;

            bool kerning = generationSettings.fontFeatures.Contains(OTL_FeatureTag.kern);
            bool markToBase = generationSettings.fontFeatures.Contains(OTL_FeatureTag.mark);
            bool markToMark = generationSettings.fontFeatures.Contains(OTL_FeatureTag.mkmk);

            TextSettings textSettings = generationSettings.textSettings;

            float marginWidth = marginSize.x;
            float marginHeight = marginSize.y;
            m_MarginLeft = 0;
            m_MarginRight = 0;

            m_Width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

            // Used by Unity's Auto Layout system.
            float renderedWidth = 0;
            float renderedHeight = 0;
            float textWidth = 0;
            m_IsCalculatingPreferredValues = true;

            // Tracking of the highest Ascender
            m_MaxCapHeight = 0;
            m_MaxAscender = 0;
            m_MaxDescender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            m_IsNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;

            WordWrapState internalWordWrapState = new WordWrapState();
            WordWrapState internalLineState = new WordWrapState();
            WordWrapState internalSoftLineBreak = new WordWrapState();

            // Clear the previous truncated / ellipsed state
            m_IsTextTruncated = false;

            // Counter to prevent recursive lockup when computing preferred values.
            m_AutoSizeIterationCount += 1;

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                uint charCode = m_TextProcessingArray[i].unicode;

                // Skip characters that have been substituted.
                if (charCode == 0x1A)
                    continue;

                // Parse Rich Text Tag

                #region Parse Rich Text Tag

                if (generationSettings.richText && charCode == k_LesserThan) // '<'
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

                int prevMaterialIndex = m_CurrentMaterialIndex;
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
                            m_InternalTextElementInfo[m_CharacterCount].textElement = m_CurrentFontAsset.characterLookupTable[k_EndOfText];
                            m_IsTextTruncated = true;
                            break;
                        case k_HyphenMinus:
                            //
                            break;
                        case k_HorizontalEllipsis:
                            m_InternalTextElementInfo[m_CharacterCount].textElement = m_Ellipsis.character;
                            m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                            m_InternalTextElementInfo[m_CharacterCount].fontAsset = m_Ellipsis.fontAsset;
                            m_InternalTextElementInfo[m_CharacterCount].material = m_Ellipsis.material;
                            m_InternalTextElementInfo[m_CharacterCount].materialReferenceIndex = m_Ellipsis.materialIndex;

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

                if (m_CharacterCount < generationSettings.firstVisibleCharacter && charCode != k_EndOfText)
                {
                    m_InternalTextElementInfo[m_CharacterCount].isVisible = false;
                    m_InternalTextElementInfo[m_CharacterCount].character = (char)k_ZeroWidthSpace;
                    m_InternalTextElementInfo[m_CharacterCount].lineNumber = 0;
                    m_CharacterCount += 1;
                    continue;
                }

                #endregion


                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.

                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

                if (m_TextElementType == TextElementType.Character)
                {
                    if ( /*(m_fontStyle & FontStyles.UpperCase) == FontStyles.UpperCase ||*/ (m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)charCode))
                            charCode = char.ToUpper((char)charCode);

                    }
                    else if ( /*(m_fontStyle & FontStyles.LowerCase) == FontStyles.LowerCase ||*/ (m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)charCode))
                            charCode = char.ToLower((char)charCode);
                    }
                    else if ( /*(m_fontStyle & FontStyles.SmallCaps) == FontStyles.SmallCaps ||*/ (m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
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

                    if (sprite == null) continue;

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    if (charCode == k_LesserThan)
                        charCode = 57344 + (uint)m_SpriteIndex;

                    // The sprite scale calculations are based on the font asset assigned to the text object.
                    if (m_CurrentSpriteAsset.faceInfo.pointSize > 0)
                    {
                        float spriteScale = (m_CurrentFontSize / m_CurrentSpriteAsset.faceInfo.pointSize * m_CurrentSpriteAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        currentElementScale = sprite.scale * sprite.glyph.scale * spriteScale;
                        elementAscentLine = m_CurrentSpriteAsset.faceInfo.ascentLine;

                        //baselineOffset = m_CurrentSpriteAsset.faceInfo.baseline * m_fontScale * m_FontScaleMultiplier * m_CurrentSpriteAsset.faceInfo.scale;
                        elementDescentLine = m_CurrentSpriteAsset.faceInfo.descentLine;
                    }
                    else
                    {
                        float spriteScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        currentElementScale = m_CurrentFontAsset.faceInfo.ascentLine / sprite.glyph.metrics.height * sprite.scale * sprite.glyph.scale * spriteScale;
                        float scaleDelta = spriteScale / currentElementScale;
                        elementAscentLine = m_CurrentFontAsset.faceInfo.ascentLine * scaleDelta;

                        //baselineOffset = m_CurrentFontAsset.faceInfo.baseline * m_fontScale * m_FontScaleMultiplier * m_CurrentFontAsset.faceInfo.scale;
                        elementDescentLine = m_CurrentFontAsset.faceInfo.descentLine * scaleDelta;
                    }

                    m_CachedTextElement = sprite;

                    m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Sprite;
                    m_InternalTextElementInfo[m_CharacterCount].scale = currentElementScale;

                    m_CurrentMaterialIndex = prevMaterialIndex;
                }
                else if (m_TextElementType == TextElementType.Character)
                {
                    m_CachedTextElement = textInfo.textElementInfo[m_CharacterCount].textElement;
                    if (m_CachedTextElement == null) continue;

                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;

                    float adjustedScale;
                    if (isInjectedCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_CharacterCount != m_FirstCharacterOfLine)
                        adjustedScale = textInfo.textElementInfo[m_CharacterCount - 1].pointSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_CurrentFontSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);

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

                    currentElementScale = adjustedScale * m_FontScaleMultiplier * m_CachedTextElement.scale;

                    m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                }

                #endregion


                // Handle Soft Hyphen

                #region Handle Soft Hyphen

                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == k_SoftHyphen || charCode == k_EndOfText)
                    currentElementScale = 0;

                #endregion


                // Store some of the text object's information
                m_InternalTextElementInfo[m_CharacterCount].character = (char)charCode;

                // Cache glyph metrics
                Glyph altGlyph = textInfo.textElementInfo[m_CharacterCount].alternativeGlyph;
                GlyphMetrics currentGlyphMetrics = altGlyph == null ? m_CachedTextElement.m_Glyph.metrics : altGlyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                // Handle Kerning if Enabled.

                #region Handle Kerning

                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                float characterSpacingAdjustment = generationSettings.characterSpacing;
                if (kerning)
                {
                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_CachedTextElement.m_GlyphIndex;

                    if (m_CharacterCount < totalCharacterCount - 1)
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

                        if (m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    m_InternalTextElementInfo[m_CharacterCount].adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
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
                    if (m_LastBaseGlyphIndex != int.MinValue && m_LastBaseGlyphIndex == m_CharacterCount - 1)
                    {
                        Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                        uint baseGlyphIndex = baseGlyph.index;
                        uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                        uint key = markGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                        {
                            float advanceOffset = (m_InternalTextElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

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
                                float baseMarkBaseline = (m_InternalTextElementInfo[characterLookupIndex].baseLine - currentBaseline) / currentElementScale;

                                glyphAdjustments.xPlacement = baseMarkOrigin + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = baseMarkBaseline + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                                wasLookupApplied = true;
                                break;
                            }
                        }

                        // If no Mark-to-Mark lookups were applied, check for potential Mark-to-Base lookup.
                        if (m_LastBaseGlyphIndex != int.MinValue && !wasLookupApplied)
                        {
                            // Handle lookup for Mark-to-Base
                            Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                            uint baseGlyphIndex = baseGlyph.index;
                            uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                            uint key = markGlyphIndex << 16 | baseGlyphIndex;

                            if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float advanceOffset = (m_InternalTextElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

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

                //if (generationSettings.isRightToLeft)
                //{
                //    m_XAdvance -= ((m_CachedTextElement.xAdvance * boldXAdvanceMultiplier + m_characterSpacing + generationSettings.wordSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                //    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                //        m_XAdvance -= generationSettings.wordSpacing * currentElementScale;
                //}

                #endregion


                // Handle Mono Spacing

                #region Handle Mono Spacing

                float monoAdvance = 0;
                if (m_MonoSpacing != 0 && charCode != k_ZeroWidthSpace)
                {
                    monoAdvance = (m_MonoSpacing / 2 - (m_CachedTextElement.glyph.metrics.width / 2 + m_CachedTextElement.glyph.metrics.horizontalBearingX) * currentElementScale) * (1 - m_CharWidthAdjDelta);
                    m_XAdvance += monoAdvance;
                }

                #endregion


                // Set Padding based on selected font style

                #region Handle Style Padding

                float boldSpacingAdjustment = 0;
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                    boldSpacingAdjustment = m_CurrentFontAsset.boldStyleSpacing;

                #endregion Handle Style Padding

                m_InternalTextElementInfo[m_CharacterCount].origin = m_XAdvance + glyphAdjustments.xPlacement * currentElementScale;
                m_InternalTextElementInfo[m_CharacterCount].baseLine = (baselineOffset - m_LineOffset + m_BaselineOffset) + glyphAdjustments.yPlacement * currentElementScale;

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
                    m_InternalTextElementInfo[m_CharacterCount].adjustedAscender = adjustedAscender;
                    m_InternalTextElementInfo[m_CharacterCount].adjustedDescender = adjustedDescender;

                    m_InternalTextElementInfo[m_CharacterCount].ascender = elementAscender - m_LineOffset;
                    m_MaxDescender = m_InternalTextElementInfo[m_CharacterCount].descender = elementDescender - m_LineOffset;
                }
                else
                {
                    m_InternalTextElementInfo[m_CharacterCount].adjustedAscender = m_MaxLineAscender;
                    m_InternalTextElementInfo[m_CharacterCount].adjustedDescender = m_MaxLineDescender;

                    m_InternalTextElementInfo[m_CharacterCount].ascender = m_MaxLineAscender - m_LineOffset;
                    m_MaxDescender = m_InternalTextElementInfo[m_CharacterCount].descender = m_MaxLineDescender - m_LineOffset;
                }

                // Max text object ascender and cap height
                if (m_LineNumber == 0 || m_IsNewPage)
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
                    if (!isWhiteSpace || m_CharacterCount == m_FirstCharacterOfLine)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }

                #endregion

                bool isJustifiedOrFlush = ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Flush) == HorizontalAlignment.Flush || ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Justified) == HorizontalAlignment.Justified;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.

                #region Handle Visible Characters

                if (charCode == k_Tab || charCode == k_ZeroWidthSpace || ((textWrapMode == TextWrappingMode.PreserveWhitespace || textWrapMode == TextWrappingMode.PreserveWhitespaceNoWrap) && (isWhiteSpace || charCode == k_ZeroWidthSpace)) || (isWhiteSpace == false && charCode != k_ZeroWidthSpace && charCode != k_SoftHyphen && charCode != k_EndOfText) || (charCode == k_SoftHyphen && isSoftHyphenIgnored == false) || m_TextElementType == TextElementType.Sprite)
                {
                    //float marginLeft = m_MarginLeft;
                    //float marginRight = m_MarginRight;

                    // Injected characters do not override margins
                    //if (isInjectedCharacter)
                    //{
                    //    marginLeft = textInfo.lineInfo[m_LineNumber].marginLeft;
                    //    marginRight = textInfo.lineInfo[m_LineNumber].marginRight;
                    //}

                    widthOfTextArea = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - m_MarginLeft - m_MarginRight, m_Width) : marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

                    // Calculate the line breaking width of the text.
                    textWidth = Mathf.Abs(m_XAdvance) + currentGlyphMetrics.horizontalAdvance * (1 - m_CharWidthAdjDelta) * (charCode == k_SoftHyphen ? currentElementUnmodifiedScale : currentElementScale);

                    int testedCharacterCount = m_CharacterCount;

                    // Handling of Horizontal Bounds

                    #region Current Line Horizontal Bounds Check

                    if (isBaseGlyph && textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        // Handle Line Breaking (if still possible)
                        if (textWrapMode != TextWrappingMode.NoWrap && textWrapMode != TextWrappingMode.PreserveWhitespaceNoWrap && m_CharacterCount != m_FirstCharacterOfLine)
                        {
                            // Restore state to previous safe line breaking
                            i = RestoreWordWrappingState(ref internalWordWrapState, textInfo);

                            // Replace Soft Hyphen by Hyphen Minus 0x2D

                            #region Handle Soft Hyphenation

                            if (m_InternalTextElementInfo[m_CharacterCount - 1].character == k_SoftHyphen && isSoftHyphenIgnored == false && generationSettings.overflowMode == TextOverflowMode.Overflow)
                            {
                                characterToSubstitute.index = m_CharacterCount - 1;
                                characterToSubstitute.unicode = k_HyphenMinus;

                                i -= 1;
                                m_CharacterCount -= 1;
                                continue;
                            }

                            isSoftHyphenIgnored = false;

                            // Ignore Soft Hyphen to prevent it from wrapping
                            if (m_InternalTextElementInfo[m_CharacterCount].character == k_SoftHyphen)
                            {
                                isSoftHyphenIgnored = true;
                                continue;
                            }

                            #endregion

                            // Adjust character spacing before breaking up word if auto size is enabled

                            #region Handle Text Auto Size (if word wrapping is no longer possible)

                            if (isTextAutoSizingEnabled && isFirstWordOfLine)
                            {
                                // Handle Character Width Adjustments

                                #region Character Width Adjustments

                                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_CharWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, generationSettings.charWidthMaxAdj / 100);

                                    Profiler.EndSample();
                                    return Vector2.zero;
                                }

                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding vertical bounds.

                                #region Text Auto-Sizing (Text greater than vertical bounds)

                                if (fontSize > generationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_MaxFontSize = fontSize;

                                    float sizeDelta = Mathf.Max((fontSize - m_MinFontSize) / 2, 0.05f);
                                    fontSize -= sizeDelta;
                                    fontSize = Mathf.Max((int)(fontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMin);

                                    // TODO: Need to investigate this value when moving it to a service as TMP might require this value to be zero.
                                    //       It's currently omitted as UITK expects the text height to be auto-increased if a word becomes longer
                                    //       and longer. User scenario: User enters a very long word with no spaces that spawns longer than it's container.
                                    // return Vector2.zero;
                                }

                                #endregion Text Auto-Sizing
                            }

                            #endregion

                            // Adjust line spacing if necessary
                            float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
                            if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                            {
                                //AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, baselineAdjustmentDelta);
                                m_MaxDescender -= baselineAdjustmentDelta;
                                m_LineOffset += baselineAdjustmentDelta;
                            }

                            // Calculate line ascender and make sure if last character is superscript or subscript that we check that as well.
                            float lineAscender = m_MaxLineAscender - m_LineOffset;
                            float lineDescender = m_MaxLineDescender - m_LineOffset;

                            // Update maxDescender and maxVisibleDescender
                            m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;
                            if (!isMaxVisibleDescenderSet)
                                maxVisibleDescender = m_MaxDescender;

                            if (generationSettings.useMaxVisibleDescender && (m_CharacterCount >= generationSettings.maxVisibleCharacters || m_LineNumber >= generationSettings.maxVisibleLines))
                                isMaxVisibleDescenderSet = true;

                            // Store first character of the next line.
                            m_FirstCharacterOfLine = m_CharacterCount;
                            m_LineVisibleCharacterCount = 0;

                            // Store the state of the line before starting on the new line.
                            SaveWordWrappingState(ref internalLineState, i, m_CharacterCount - 1, textInfo);

                            m_LineNumber += 1;

                            float ascender = m_InternalTextElementInfo[m_CharacterCount].adjustedAscender;

                            // Compute potential new line offset in the event a line break is needed.
                            if (m_LineHeight == k_FloatUnset)
                            {
                                m_LineOffset += 0 - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + generationSettings.lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = false;
                            }
                            else
                            {
                                m_LineOffset += m_LineHeight + generationSettings.lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                            m_StartOfLineAscender = ascender;

                            m_XAdvance = 0 + m_TagIndent;

                            //isStartOfNewLine = true;
                            isFirstWordOfLine = true;
                            continue;
                        }
                    }

                    #endregion

                    // Compute Preferred Width & Height
                    renderedWidth = Mathf.Max(renderedWidth, textWidth + m_MarginLeft + m_MarginRight);
                    renderedHeight = Mathf.Max(renderedHeight, m_MaxAscender - m_MaxDescender);
                }

                #endregion Handle Visible Characters


                // Check if Line Spacing of previous line needs to be adjusted.

                #region Adjust Line Spacing

                if (m_LineOffset > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                {
                    float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;

                    //AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, offsetDelta);
                    m_MaxDescender -= offsetDelta;
                    m_LineOffset += offsetDelta;

                    m_StartOfLineAscender += offsetDelta;
                    internalWordWrapState.lineOffset = m_LineOffset;
                    internalWordWrapState.startOfLineAscender = m_StartOfLineAscender;
                }

                #endregion


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.

                #region XAdvance, Tabulation & Stops

                if (charCode != k_ZeroWidthSpace)
                {
                    if (charCode == k_Tab)
                    {
                        float tabSize = m_CurrentFontAsset.faceInfo.tabWidth * m_CurrentFontAsset.tabMultiple * currentElementScale;
                        float tabs = Mathf.Ceil(m_XAdvance / tabSize) * tabSize;
                        m_XAdvance = tabs > m_XAdvance ? tabs : m_XAdvance + tabSize;
                    }
                    else if (m_MonoSpacing != 0)
                    {
                        m_XAdvance += (m_MonoSpacing - monoAdvance + ((m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment) * currentEmScale) + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                        if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                            m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                    }
                    else
                    {
                        m_XAdvance += ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                        if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                            m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                    }
                }

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

                if (charCode == k_LineFeed || charCode == k_VerticalTab || charCode == k_EndOfText || charCode == k_LineSeparator || charCode == k_ParagraphSeparator || m_CharacterCount == totalCharacterCount - 1)
                {
                    // Check if Line Spacing of previous line needs to be adjusted.
                    float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                    {
                        m_MaxDescender -= baselineAdjustmentDelta;
                        m_LineOffset += baselineAdjustmentDelta;
                    }

                    m_IsNewPage = false;

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    //float lineAscender = m_MaxLineAscender - m_LineOffset;
                    float lineDescender = m_MaxLineDescender - m_LineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;

                    // Add new line if not last lines or character.
                    if (charCode == k_LineFeed || charCode == k_VerticalTab || charCode == k_HyphenMinus || charCode == k_LineSeparator || charCode == k_ParagraphSeparator)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref internalLineState, i, m_CharacterCount, textInfo);

                        // Store the state of the last Character before the new line.
                        SaveWordWrappingState(ref internalWordWrapState, i, m_CharacterCount, textInfo);

                        m_LineNumber += 1;
                        m_FirstCharacterOfLine = m_CharacterCount + 1;

                        float ascender = m_InternalTextElementInfo[m_CharacterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_LineHeight == k_FloatUnset)
                        {
                            float lineOffsetDelta = 0 - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + (generationSettings.lineSpacing + (charCode == k_LineFeed || charCode == k_ParagraphSeparator ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_LineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_LineOffset += m_LineHeight + (generationSettings.lineSpacing + (charCode == k_LineFeed || charCode == k_ParagraphSeparator ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                        m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                        m_StartOfLineAscender = ascender;

                        m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;

                        m_CharacterCount += 1;
                        continue;
                    }

                    // If End of Text
                    if (charCode == k_EndOfText)
                        i = m_TextProcessingArray.Length;
                }

                #endregion Check for Linefeed or Last Character


                // Save State of Mesh Creation for handling of Word Wrapping

                #region Save Word Wrapping State

                if ((textWrapMode != TextWrappingMode.NoWrap && textWrapMode != TextWrappingMode.PreserveWhitespaceNoWrap) || generationSettings.overflowMode == TextOverflowMode.Truncate || generationSettings.overflowMode == TextOverflowMode.Ellipsis)
                {
                    bool shouldSaveHardLineBreak = false;
                    bool shouldSaveSoftLineBreak = false;

                    if ((isWhiteSpace || charCode == k_ZeroWidthSpace || charCode == k_HyphenMinus || charCode == k_SoftHyphen) && (!m_IsNonBreakingSpace || ignoreNonBreakingSpace) && charCode != k_NoBreakSpace && charCode != k_FigureSpace && charCode != k_NonBreakingHyphen && charCode != k_NarrowNoBreakSpace && charCode != k_WordJoiner)
                    {
                        // Ignore Hyphen (0x2D) when preceded by a whitespace
                        if ((charCode == k_HyphenMinus && m_CharacterCount > 0 && char.IsWhiteSpace((char)textInfo.textElementInfo[m_CharacterCount - 1].character)) == false)
                        {
                            isFirstWordOfLine = false;
                            shouldSaveHardLineBreak = true;

                            // Reset soft line breaking point since we now have a valid hard break point.
                            internalSoftLineBreak.previousWordBreak = -1;
                        }
                    }

                    // Handling for East Asian scripts
                    else if (m_IsNonBreakingSpace == false && (TextGeneratorUtilities.IsHangul((uint)charCode) && textSettings.lineBreakingRules.useModernHangulLineBreakingRules == false || TextGeneratorUtilities.IsCJK((uint)charCode)))
                    {
                        bool isCurrentLeadingCharacter = textSettings.lineBreakingRules.leadingCharactersLookup.Contains((uint)charCode);
                        bool isNextFollowingCharacter = m_CharacterCount < totalCharacterCount - 1 && textSettings.lineBreakingRules.leadingCharactersLookup.Contains(m_InternalTextElementInfo[m_CharacterCount + 1].character);

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
                        SaveWordWrappingState(ref internalWordWrapState, i, m_CharacterCount, textInfo);

                    // Save potential Soft line break
                    if (shouldSaveSoftLineBreak)
                        SaveWordWrappingState(ref internalSoftLineBreak, i, m_CharacterCount, textInfo);
                }

                #endregion Save Word Wrapping State

                m_CharacterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.

            #region Check Auto-Sizing (Upper Font Size Bounds)

            fontSizeDelta = m_MaxFontSize - m_MinFontSize;
            if (isTextAutoSizingEnabled && fontSizeDelta > 0.051f && fontSize < generationSettings.fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                // Reset character width adjustment delta
                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                    m_CharWidthAdjDelta = 0;

                m_MinFontSize = fontSize;

                float sizeDelta = Mathf.Max((m_MaxFontSize - fontSize) / 2, 0.05f);
                fontSize += sizeDelta;
                fontSize = Mathf.Min((int)(fontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMax);

                Profiler.EndSample();
                return Vector2.zero;
            }

            #endregion End Auto-sizing Check

            m_IsCalculatingPreferredValues = false;

            // Adjust Preferred Width and Height to account for Margins.
            renderedWidth += generationSettings.margins.x > 0 ? generationSettings.margins.x : 0;
            renderedWidth += generationSettings.margins.z > 0 ? generationSettings.margins.z : 0;

            renderedHeight += generationSettings.margins.y > 0 ? generationSettings.margins.y : 0;
            renderedHeight += generationSettings.margins.w > 0 ? generationSettings.margins.w : 0;

            // Round Preferred Values to nearest 5/100.
            renderedWidth = (int)(renderedWidth * 100 + 1f) / 100f;
            renderedHeight = (int)(renderedHeight * 100 + 1f) / 100f;

            Profiler.EndSample();

            return new Vector2(renderedWidth, renderedHeight);
        }
    }
}
