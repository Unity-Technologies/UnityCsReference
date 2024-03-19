// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    internal partial class TextGenerator
    {
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void Prepare(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            Profiler.BeginSample("TextGenerator.Prepare");
            m_Padding = generationSettings.extraPadding;
            m_CurrentFontAsset = generationSettings.fontAsset;

            // Set the font style that is assigned by the builder
            m_FontStyleInternal = generationSettings.fontStyle;
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;

            // Find and cache Underline & Ellipsis characters.
            GetSpecialCharacters(generationSettings);

            ComputeMarginSize(generationSettings.screenRect, generationSettings.margins);

            //ParseInputText
            PopulateTextBackingArray(generationSettings.renderedText);
            PopulateTextProcessingArray(generationSettings);
            SetArraySizes(m_TextProcessingArray, generationSettings, textInfo);

            // Reset Font min / max used with Auto-sizing
            if (generationSettings.autoSize)
                m_FontSize = Mathf.Clamp(generationSettings.fontSize, generationSettings.fontSizeMin, generationSettings.fontSizeMax);
            else
                m_FontSize = generationSettings.fontSize;

            m_MaxFontSize = generationSettings.fontSizeMax;
            m_MinFontSize = generationSettings.fontSizeMin;
            m_LineSpacingDelta = 0;
            m_CharWidthAdjDelta = 0;

            Profiler.EndSample();
        }

        internal bool PrepareFontAsset(TextGenerationSettings generationSettings)
        {
            m_CurrentFontAsset = generationSettings.fontAsset;

            // Set the font style that is assigned by the builder
            m_FontStyleInternal = generationSettings.fontStyle;
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;

            // Find and cache Underline & Ellipsis characters.
            if (!GetSpecialCharacters(generationSettings))
                return false;

            //ParseInputText
            PopulateTextBackingArray(generationSettings.renderedText);
            PopulateTextProcessingArray(generationSettings);
            return PopulateFontAsset(generationSettings, m_TextProcessingArray);
        }

        // This function parses through the Char[] to determine how many characters will be visible. It then makes sure the arrays are large enough for all those characters.
        int SetArraySizes(TextProcessingElement[] textProcessingArray, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            TextSettings textSettings = generationSettings.textSettings;

            int spriteCount = 0;

            m_TotalCharacterCount = 0;
            m_isTextLayoutPhase = false;
            m_TagNoParsing = false;
            m_FontStyleInternal = generationSettings.fontStyle;
            m_FontStyleStack.Clear();

            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;

            m_MaterialReferenceStack.SetDefault(new MaterialReference(m_CurrentMaterialIndex, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            m_MaterialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
            m_CurrentSpriteAsset = null;

            if (textInfo == null)
                textInfo = new TextInfo(VertexDataLayout.Mesh);
            else if (textInfo.textElementInfo.Length < m_InternalTextProcessingArraySize)
                TextInfo.Resize(ref textInfo.textElementInfo, m_InternalTextProcessingArraySize, false);

            m_TextElementType = TextElementType.Character;

            // Handling for Underline special character
            #region Setup Underline Special Character
            /*
            GetUnderlineSpecialCharacter(m_CurrentFontAsset);
            if (m_Underline.character != null)
            {
                if (m_Underline.fontAsset.GetInstanceID() != m_CurrentFontAsset.GetInstanceID())
                {
                    if (generationSettings.textSettings.matchMaterialPreset && m_CurrentMaterial.GetInstanceID() != m_Underline.fontAsset.material.GetInstanceID())
                        m_Underline.material = TMP_MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_Underline.fontAsset.material);
                    else
                        m_Underline.material = m_Underline.fontAsset.material;

                    m_Underline.materialIndex = MaterialReference.AddMaterialReference(m_Underline.material, m_Underline.fontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);
                    m_MaterialReferences[m_Underline.materialIndex].referenceCount = 0;
                }
            }
            */
            #endregion


            // Handling for Ellipsis special character
            #region Setup Ellipsis Special Character
            if (generationSettings.overflowMode == TextOverflowMode.Ellipsis)
            {
                GetEllipsisSpecialCharacter(generationSettings);

                if (m_Ellipsis.character != null)
                {
                    if (m_Ellipsis.fontAsset.GetHashCode() != m_CurrentFontAsset.GetHashCode())
                    {
                        if (textSettings.matchMaterialPreset && m_CurrentMaterial.GetHashCode() != m_Ellipsis.fontAsset.material.GetHashCode())
                            m_Ellipsis.material = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_Ellipsis.fontAsset.material);
                        else
                            m_Ellipsis.material = m_Ellipsis.fontAsset.material;

                        m_Ellipsis.materialIndex = MaterialReference.AddMaterialReference(m_Ellipsis.material, m_Ellipsis.fontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                        m_MaterialReferences[m_Ellipsis.materialIndex].referenceCount = 0;
                    }
                }
                else
                {
                    generationSettings.overflowMode = TextOverflowMode.Truncate;

                    if (textSettings.displayWarnings)
                        Debug.LogWarning("The character used for Ellipsis is not available in font asset [" + m_CurrentFontAsset.name + "] or any potential fallbacks. Switching Text Overflow mode to Truncate.");
                }
            }
            #endregion

            // Check if we should process Ligatures
            bool ligature = generationSettings.fontFeatures.Contains(OTL_FeatureTag.liga);

            // Clear Linked Text object if we have one
            //if (generationSettings.overflowMode == TextOverflowMode.Linked && m_linkedTextComponent != null && !m_IsCalculatingPreferredValues)
            //    m_linkedTextComponent.text = string.Empty;

            // Parsing XML tags in the text
            for (int i = 0; i < textProcessingArray.Length && textProcessingArray[i].unicode != 0; i++)
            {
                //Make sure the characterInfo array can hold the next text element.
                if (textInfo.textElementInfo == null || m_TotalCharacterCount >= textInfo.textElementInfo.Length)
                    TextInfo.Resize(ref textInfo.textElementInfo, m_TotalCharacterCount + 1, true);

                uint unicode = textProcessingArray[i].unicode;
                int prevMaterialIndex = m_CurrentMaterialIndex;

                // PARSE XML TAGS
                #region PARSE XML TAGS
                if (generationSettings.richText && unicode == '<')
                {
                    prevMaterialIndex = m_CurrentMaterialIndex;
                    int endTagIndex;

                    // Check if Tag is Valid
                    if (ValidateHtmlTag(textProcessingArray, i + 1, out endTagIndex, generationSettings, textInfo, out bool isThreadSuccess))
                    {
                        int tagStartIndex = textProcessingArray[i].stringIndex;
                        i = endTagIndex;

                        if (m_TextElementType == TextElementType.Sprite)
                        {
                            m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;


                            textInfo.textElementInfo[m_TotalCharacterCount].character = (char)(57344 + m_SpriteIndex);
                            textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].textElement = m_CurrentSpriteAsset.spriteCharacterTable[m_SpriteIndex];
                            textInfo.textElementInfo[m_TotalCharacterCount].elementType = m_TextElementType;
                            textInfo.textElementInfo[m_TotalCharacterCount].index = tagStartIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].stringLength = textProcessingArray[i].stringIndex - tagStartIndex + 1;

                            // Restore element type and material index to previous values.
                            m_TextElementType = TextElementType.Character;
                            m_CurrentMaterialIndex = prevMaterialIndex;

                            spriteCount += 1;
                            m_TotalCharacterCount += 1;
                        }

                        continue;
                    }
                }
                #endregion

                bool isUsingAlternativeTypeface;
                bool isUsingFallbackOrAlternativeTypeface = false;

                FontAsset prevFontAsset = m_CurrentFontAsset;
                Material prevMaterial = m_CurrentMaterial;
                prevMaterialIndex = m_CurrentMaterialIndex;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles
                if (m_TextElementType == TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)unicode))
                            unicode = char.ToLower((char)unicode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        // Only convert lowercase characters to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);
                    }
                }
                #endregion

                // Lookup the Glyph data for each character and cache it.
                #region LOOKUP GLYPH
                TextElement character = GetTextElement(generationSettings, (uint)unicode, m_CurrentFontAsset, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, ligature);

                // Check if Lowercase or Uppercase variant of the character is available.
                /* Not sure this is necessary anyone as it is very unlikely with recursive search through fallback fonts.
                if (glyph == null)
                {
                    if (char.IsLower((char)c))
                    {
                        if (m_CurrentFontAsset.characterDictionary.TryGetValue(char.ToUpper((char)c), out glyph))
                            c = chars[i] = char.ToUpper((char)c);
                    }
                    else if (char.IsUpper((char)c))
                    {
                        if (m_CurrentFontAsset.characterDictionary.TryGetValue(char.ToLower((char)c), out glyph))
                            c = chars[i] = char.ToLower((char)c);
                    }
                }*/

                // Special handling for missing character.
                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                if (character == null)
                {
                    DoMissingGlyphCallback(unicode, textProcessingArray[i].stringIndex, m_CurrentFontAsset, textInfo);

                    // Save the original unicode character
                    uint srcGlyph = unicode;

                    // Try replacing the missing glyph character by the Settings file Missing Glyph or Square (9633) character.
                    unicode = textProcessingArray[i].unicode = (uint)textSettings.missingCharacterUnicode == 0 ? k_Square : (uint)textSettings.missingCharacterUnicode;

                    // Check for the missing glyph character in the currently assigned font asset and its fallbacks
                    character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, ligature);

                    if (character == null)
                    {
                        character = FontAssetUtilities.GetCharacterFromFontAssetsInternal((uint)unicode, m_CurrentFontAsset, textSettings.fallbackFontAssets, textSettings.fallbackOSFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, ligature);
                    }

                    if (character == null)
                    {
                        // Search for the missing glyph in the Settings Default Font Asset.
                        if (textSettings.defaultFontAsset != null)
                            character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, textSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, ligature);
                    }

                    if (character == null)
                    {
                        // Use Space (32) Glyph from the currently assigned font asset.
                        unicode = textProcessingArray[i].unicode = 32;
                        character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, ligature);
                    }

                    if (character == null)
                    {
                        // Use End of Text (0x03) Glyph from the currently assigned font asset.
                        unicode = textProcessingArray[i].unicode = k_EndOfText;
                        character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, ligature);
                    }

                    if (textSettings.displayWarnings)
                    {
                        bool isMainThread = !JobsUtility.IsExecutingJob;
                        string formattedWarning = srcGlyph > 0xFFFF
                            ? string.Format("The character with Unicode value \\U{0:X8} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4}.", srcGlyph, isMainThread ? generationSettings.fontAsset.name : generationSettings.fontAsset.GetHashCode(), character.unicode)
                            : string.Format("The character with Unicode value \\u{0:X4} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4}.", srcGlyph, isMainThread ? generationSettings.fontAsset.name : generationSettings.fontAsset.GetHashCode(), character.unicode);

                        Debug.LogWarning(formattedWarning);
                    }
                }

                textInfo.textElementInfo[m_TotalCharacterCount].alternativeGlyph = null;

                if (character.elementType == TextElementType.Character)
                {
                    if (character.textAsset.instanceID != m_CurrentFontAsset.instanceID)
                    {
                        isUsingFallbackOrAlternativeTypeface = true;
                        m_CurrentFontAsset = character.textAsset as FontAsset;
                    }

                    #region VARIATION SELECTOR
                    uint nextCharacter = i + 1 < textProcessingArray.Length ? (uint)textProcessingArray[i + 1].unicode : 0;
                    if (nextCharacter >= 0xFE00 && nextCharacter <= 0xFE0F || nextCharacter >= 0xE0100 && nextCharacter <= 0xE01EF)
                    {
                        uint variantGlyphIndex;
                        // Get potential variant glyph index
                        if (!m_CurrentFontAsset.TryGetGlyphVariantIndexInternal(unicode, nextCharacter, out variantGlyphIndex))
                        {
                            variantGlyphIndex = m_CurrentFontAsset.GetGlyphVariantIndex(unicode, nextCharacter);
                            m_CurrentFontAsset.TryAddGlyphVariantIndexInternal(unicode, nextCharacter, variantGlyphIndex);
                        }

                        if (variantGlyphIndex != 0)
                        {
                            if (m_CurrentFontAsset.TryAddGlyphInternal(variantGlyphIndex, out Glyph glyph))
                            {
                                textInfo.textElementInfo[m_TotalCharacterCount].alternativeGlyph = glyph;
                            }
                        }

                        textProcessingArray[i + 1].unicode = 0x1A;
                        i += 1;
                    }
                    #endregion

                    #region LIGATURES
                    if (ligature && m_CurrentFontAsset.fontFeatureTable.m_LigatureSubstitutionRecordLookup.TryGetValue(character.glyphIndex, out List<LigatureSubstitutionRecord> records))
                    {
                        if (records == null)
                            break;

                        for (int j = 0; j < records.Count; j++)
                        {
                            LigatureSubstitutionRecord record = records[j];

                            int componentCount = record.componentGlyphIDs.Length;
                            uint ligatureGlyphID = record.ligatureGlyphID;

                            //
                            for (int k = 1; k < componentCount; k++)
                            {
                                uint componentUnicode = (uint)textProcessingArray[i + k].unicode;

                                // Special Handling for Zero Width Joiner (ZWJ)
                                //if (componentUnicode == 0x200D)
                                //    continue;

                                uint glyphIndex = m_CurrentFontAsset.GetGlyphIndex(componentUnicode, out bool _);

                                if (glyphIndex == record.componentGlyphIDs[k])
                                    continue;

                                ligatureGlyphID = 0;
                                break;
                            }

                            if (ligatureGlyphID != 0)
                            {
                                if (m_CurrentFontAsset.TryAddGlyphInternal(ligatureGlyphID, out Glyph glyph))
                                {
                                    textInfo.textElementInfo[m_TotalCharacterCount].alternativeGlyph = glyph;

                                    // Update text processing array
                                    for (int c = 0; c < componentCount; c++)
                                    {
                                        if (c == 0)
                                        {
                                            textProcessingArray[i + c].length = componentCount;
                                            continue;
                                        }

                                        textProcessingArray[i + c].unicode = 0x1A;
                                    }

                                    i += componentCount - 1;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                // Save text element data
                textInfo.textElementInfo[m_TotalCharacterCount].elementType = TextElementType.Character;
                textInfo.textElementInfo[m_TotalCharacterCount].textElement = character;
                textInfo.textElementInfo[m_TotalCharacterCount].isUsingAlternateTypeface = isUsingAlternativeTypeface;
                textInfo.textElementInfo[m_TotalCharacterCount].character = (char)unicode;
                textInfo.textElementInfo[m_TotalCharacterCount].index = textProcessingArray[i].stringIndex;
                textInfo.textElementInfo[m_TotalCharacterCount].stringLength = textProcessingArray[i].length;
                textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;

                // Special handling if the character is a sprite.
                if (character.elementType == TextElementType.Sprite)
                {
                    SpriteAsset spriteAssetRef = character.textAsset as SpriteAsset;
                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(spriteAssetRef.material, spriteAssetRef, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                    m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;

                    textInfo.textElementInfo[m_TotalCharacterCount].elementType = TextElementType.Sprite;
                    textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                    // Restore element type and material index to previous values.
                    m_TextElementType = TextElementType.Character;
                    m_CurrentMaterialIndex = prevMaterialIndex;

                    spriteCount += 1;
                    m_TotalCharacterCount += 1;

                    continue;
                }

                if (isUsingFallbackOrAlternativeTypeface && m_CurrentFontAsset.instanceID != generationSettings.fontAsset.instanceID)
                {
                    // Create Fallback material instance matching current material preset if necessary
                    if (textSettings.matchMaterialPreset)
                        m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_CurrentFontAsset.material);
                    else
                        m_CurrentMaterial = m_CurrentFontAsset.material;

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                }

                // Handle Multi Atlas Texture support
                if (character != null && character.glyph.atlasIndex > 0)
                {
                    m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentFontAsset, m_CurrentMaterial, character.glyph.atlasIndex);

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                    isUsingFallbackOrAlternativeTypeface = true;
                }

                if (!char.IsWhiteSpace((char)unicode) && unicode != CodePoint.ZERO_WIDTH_SPACE)
                {
                    // Limit the mesh of the main text object to 65535 vertices and use sub objects for the overflow.
                    if (generationSettings.isIMGUI && m_MaterialReferences[m_CurrentMaterialIndex].referenceCount >= 16383)
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_CurrentMaterial), m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                    m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;
                }

                textInfo.textElementInfo[m_TotalCharacterCount].material = m_CurrentMaterial;
                textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;
                m_MaterialReferences[m_CurrentMaterialIndex].isFallbackMaterial = isUsingFallbackOrAlternativeTypeface;

                // Restore previous font asset and material if fallback font was used.
                if (isUsingFallbackOrAlternativeTypeface)
                {
                    m_MaterialReferences[m_CurrentMaterialIndex].fallbackMaterial = prevMaterial;
                    m_CurrentFontAsset = prevFontAsset;
                    m_CurrentMaterial = prevMaterial;
                    m_CurrentMaterialIndex = prevMaterialIndex;
                }

                m_TotalCharacterCount += 1;
            }

            // Early return if we are calculating the preferred values.
            if (m_IsCalculatingPreferredValues)
            {
                m_IsCalculatingPreferredValues = false;

                return m_TotalCharacterCount;
            }

            // Save material and sprite count.
            textInfo.spriteCount = spriteCount;
            int materialCount = textInfo.materialCount = m_MaterialReferenceIndexLookup.Count;

            // Check if we need to resize the MeshInfo array for handling different materials.
            if (materialCount > textInfo.meshInfo.Length)
                TextInfo.Resize(ref textInfo.meshInfo, materialCount, false);

            // Resize textElementInfo[] if allocations are excessive
            if (m_VertexBufferAutoSizeReduction && textInfo.textElementInfo.Length - m_TotalCharacterCount > 256)
                TextInfo.Resize(ref textInfo.textElementInfo, Mathf.Max(m_TotalCharacterCount + 1, 256), true);

            // Iterate through the material references to set the mesh buffer allocations
            for (int i = 0; i < materialCount; i++)
            {
                int referenceCount = m_MaterialReferences[i].referenceCount;

                // Check to make sure buffer allocations can accommodate the required text elements.
                if ((textInfo.meshInfo[i].vertexData == null && textInfo.meshInfo[i].vertices == null) || textInfo.meshInfo[i].vertexBufferSize < referenceCount * 4)
                {
                    if (textInfo.meshInfo[i].vertexData == null && textInfo.meshInfo[i].vertices == null)
                    {
                        textInfo.meshInfo[i] = new MeshInfo(referenceCount + 1, textInfo.vertexDataLayout, generationSettings.isIMGUI);
                    }
                    else
                        textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount), generationSettings.isIMGUI);
                }
                else if (textInfo.meshInfo[i].vertexBufferSize - referenceCount * 4 > 1024)
                {
                    // Resize vertex buffers if allocations are excessive.
                    textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.Max(Mathf.NextPowerOfTwo(referenceCount), 256), generationSettings.isIMGUI);
                }

                // Assign material reference
                textInfo.meshInfo[i].material = m_MaterialReferences[i].material;
                textInfo.meshInfo[i].glyphRenderMode = m_MaterialReferences[i].fontAsset.atlasRenderMode;
            }

            return m_TotalCharacterCount;
        }

        TextElement GetTextElement(TextGenerationSettings generationSettings, uint unicode, FontAsset fontAsset, FontStyles fontStyle, TextFontWeight fontWeight, out bool isUsingAlternativeTypeface, bool populateLigatures)
        {
            bool canWriteOnAsset = !IsExecutingJob;
            //Debug.Log("Unicode: " + unicode.ToString("X8"));

            TextSettings textSettings = generationSettings.textSettings;
            if (generationSettings.emojiFallbackSupport && TextGeneratorUtilities.IsEmoji(unicode))
            {
                if (textSettings.emojiFallbackTextAssets != null && textSettings.emojiFallbackTextAssets.Count > 0)
                {
                    TextElement textElement = FontAssetUtilities.GetTextElementFromTextAssets(unicode, fontAsset, textSettings.emojiFallbackTextAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

                    if (textElement != null)
                    {
                        // Add character to font asset lookup cache
                        //fontAsset.AddCharacterToLookupCache(unicode, character);

                        return textElement;
                    }
                }
            }

            Character character = FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

            if (character != null)
                return character;

            if (!canWriteOnAsset && (fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic || fontAsset.atlasPopulationMode == AtlasPopulationMode.DynamicOS))
                return null;

            // Search potential list of fallback font assets assigned to the font asset.
            if (fontAsset.m_FallbackFontAssetTable != null && fontAsset.m_FallbackFontAssetTable.Count > 0)
                character = FontAssetUtilities.GetCharacterFromFontAssetsInternal(unicode, fontAsset, fontAsset.m_FallbackFontAssetTable, OSFallbackList: null, true, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

            if (character != null)
            {
                // Add character to font asset lookup cache
                fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight);

                return character;
            }

            bool fontAssetEquals = canWriteOnAsset ? fontAsset.instanceID == generationSettings.fontAsset.instanceID : fontAsset == generationSettings.fontAsset;
            // Search for the character in the primary font asset if not the current font asset
            if (!fontAssetEquals)
            {
                // Search primary font asset
                character = FontAssetUtilities.GetCharacterFromFontAsset(unicode, generationSettings.fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

                // Use material and index of primary font asset.
                if (character != null)
                {
                    m_CurrentMaterialIndex = 0;
                    m_CurrentMaterial = m_MaterialReferences[0].material;

                    // Add character to font asset lookup cache
                    fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight);

                    return character;
                }

                // Search list of potential fallback font assets assigned to the primary font asset.
                if (generationSettings.fontAsset.m_FallbackFontAssetTable != null && generationSettings.fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = FontAssetUtilities.GetCharacterFromFontAssetsInternal(unicode, fontAsset, generationSettings.fontAsset.m_FallbackFontAssetTable, OSFallbackList: null, true, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

                if (character != null)
                {
                    // Add character to font asset lookup cache
                    fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight);

                    return character;
                }
            }

            // Search for the character in potential local Sprite Asset assigned to the text object.
            if (generationSettings.spriteAsset != null)
            {
                SpriteCharacter spriteCharacter = FontAssetUtilities.GetSpriteCharacterFromSpriteAsset(unicode, generationSettings.spriteAsset, true);

                if (spriteCharacter != null)
                    return spriteCharacter;
            }

            if (textSettings.GetStaticFallbackOSFontAsset() == null && !canWriteOnAsset)
                return null;
            // Search for the character in the list of fallback assigned in the settings (General Fallbacks).
            character = FontAssetUtilities.GetCharacterFromFontAssetsInternal(unicode, fontAsset, textSettings.fallbackFontAssets, textSettings.fallbackOSFontAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

            if (character != null)
            {
                // Add character to font asset lookup cache
                fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight);

                return character;
            }

            // Search for the character in the Default Font Asset assigned in the settings file.
            if (textSettings.defaultFontAsset != null)
                character = FontAssetUtilities.GetCharacterFromFontAsset(unicode, textSettings.defaultFontAsset, true, fontStyle, fontWeight, out isUsingAlternativeTypeface, populateLigatures);

            if (character != null)
            {
                // Add character to font asset lookup cache
                fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight);

                return character;
            }

            // Search for the character in the Default Sprite Asset assigned in the settings file.
            if (textSettings.defaultSpriteAsset != null)
            {
                if (!canWriteOnAsset && textSettings.defaultSpriteAsset.m_SpriteCharacterLookup == null)
                    return null;
                SpriteCharacter spriteCharacter = FontAssetUtilities.GetSpriteCharacterFromSpriteAsset(unicode, textSettings.defaultSpriteAsset, true);

                if (spriteCharacter != null)
                    return spriteCharacter;
            }

            return null;
        }

        /// <summary>
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">Source text to be converted</param>
        void PopulateTextBackingArray(in RenderedText sourceText)
        {
            int writeIndex = 0;
            int length = sourceText.CharacterCount;

            // Make sure array size is appropriate
            if (length >= m_TextBackingArray.Capacity)
                m_TextBackingArray.Resize((length));

            foreach (var character in sourceText)
            {
                m_TextBackingArray[writeIndex] = character;
                writeIndex += 1;
            }

            // Terminate array with zero as we are not clearing the array on new invocation of this function.
            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;
        }

        /// <summary>
        ///
        /// </summary>
        void PopulateTextProcessingArray(TextGenerationSettings generationSettings)
        {
            int srcLength = m_TextBackingArray.Count;

            // Make sure parsing buffer is large enough to handle the required text.
            if (m_TextProcessingArray.Length < srcLength)
                TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray, srcLength);

            // Reset Style stack back to default
            TextProcessingStack<int>.SetDefault(m_TextStyleStacks, 0);

            m_TextStyleStackDepth = 0;
            int writeIndex = 0;

            int styleHashCode = m_TextStyleStacks[0].Pop();
            TextStyle textStyle = TextGeneratorUtilities.GetStyle(generationSettings, styleHashCode);

            // Insert Opening Style
            if (textStyle != null && textStyle.hashCode != (int)MarkupTag.NORMAL)
                TextGeneratorUtilities.InsertOpeningStyleTag(textStyle, ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);

            var tagNoParsing = generationSettings.tagNoParsing;

            int readIndex = 0;
            for (; readIndex < srcLength; readIndex++)
            {
                uint c = m_TextBackingArray[readIndex];

                if (c == 0)
                    break;

                // TODO: Since we do not set the TextInputSource at this very moment, we have defaulted this conditional to check for
                //       TextInputSource.TextString whereas in TMP, it checks for TextInputSource.TextInputBox
                if (/*generationSettings.inputSource == TextInputSource.TextString && */ c == '\\' && readIndex < srcLength - 1)
                {
                    switch (m_TextBackingArray[readIndex + 1])
                    {
                        case 92: // \ escape
                            if (!generationSettings.parseControlCharacters) break;

                            readIndex += 1;
                            break;
                        case 110: // \n LineFeed
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 10 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 114: // \r Carriage Return
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 13 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 116: // \t Tab
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 9 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 118: // \v Vertical tab used as soft line break
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 11 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 117: // \u0000 for UTF-16 Unicode
                            if (srcLength > readIndex + 5 && TextGeneratorUtilities.IsValidUTF16(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = TextGeneratorUtilities.GetUTF16(m_TextBackingArray, readIndex + 2) };

                                readIndex += 5;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                        case 85: // \U00000000 for UTF-32 Unicode
                            if (srcLength > readIndex + 9 && TextGeneratorUtilities.IsValidUTF32(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 10, unicode = TextGeneratorUtilities.GetUTF32(m_TextBackingArray, readIndex + 2) };

                                readIndex += 9;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                    }
                }

                // Handle surrogate pair conversion
                if (c >= CodePoint.HIGH_SURROGATE_START && c <= CodePoint.HIGH_SURROGATE_END && srcLength > readIndex + 1 && m_TextBackingArray[readIndex + 1] >= CodePoint.LOW_SURROGATE_START && m_TextBackingArray[readIndex + 1] <= CodePoint.LOW_SURROGATE_END)
                {
                    m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 2, unicode = TextGeneratorUtilities.ConvertToUTF32(c, m_TextBackingArray[readIndex + 1]) };

                    readIndex += 1;
                    writeIndex += 1;
                    continue;
                }

                // Handle inline replacement of <style> and <br> tags.
                if (c == '<' && generationSettings.richText)
                {
                    // Read tag hash code
                    int hashCode = TextGeneratorUtilities.GetMarkupTagHashCode(m_TextBackingArray, readIndex + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.NO_PARSE:
                            tagNoParsing = true;
                            break;
                        case MarkupTag.SLASH_NO_PARSE:
                            tagNoParsing = false;
                            break;
                        case MarkupTag.BR:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 10 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.CR:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 13 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = k_NoBreakSpace };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0x200B };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWJ:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0x200D };
                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.SHY:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0xAD };

                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.A:
                            // Additional check
                            if (m_TextBackingArray.Count > readIndex + 4 && m_TextBackingArray[readIndex + 3] == 'h' && m_TextBackingArray[readIndex + 4] == 'r')
                                TextGeneratorUtilities.InsertOpeningTextStyle(TextGeneratorUtilities.GetStyle(generationSettings, (int)MarkupTag.A), ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);
                            break;
                        case MarkupTag.STYLE:
                            if (tagNoParsing) break;

                            int openWriteIndex = writeIndex;
                            if (TextGeneratorUtilities.ReplaceOpeningStyleTag(ref m_TextBackingArray, readIndex, out int srcOffset, ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings))
                            {
                                // Update potential text elements added by the opening style.
                                for (; openWriteIndex < writeIndex; openWriteIndex++)
                                {
                                    m_TextProcessingArray[openWriteIndex].stringIndex = readIndex;
                                    m_TextProcessingArray[openWriteIndex].length = (srcOffset - readIndex) + 1;
                                }

                                readIndex = srcOffset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_A:
                            TextGeneratorUtilities.InsertClosingTextStyle(TextGeneratorUtilities.GetStyle(generationSettings, (int)MarkupTag.A), ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);
                            break;
                        case MarkupTag.SLASH_STYLE:
                            if (tagNoParsing) break;

                            int closeWriteIndex = writeIndex;
                            TextGeneratorUtilities.ReplaceClosingStyleTag(ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);

                            // Update potential text elements added by the closing style.
                            for (; closeWriteIndex < writeIndex; closeWriteIndex++)
                            {
                                m_TextProcessingArray[closeWriteIndex].stringIndex = readIndex;
                                m_TextProcessingArray[closeWriteIndex].length = 8;
                            }

                            readIndex += 7;
                            continue;
                    }

                    // Validate potential text markup element
                    // if (TryGetTextMarkupElement(m_TextBackingArray.Text, ref readIndex, out TextProcessingElement markupElement))
                    // {
                    //     m_TextProcessingArray[writeIndex] = markupElement;
                    //     writeIndex += 1;
                    //     continue;
                    // }
                }

                // Lookup character and glyph data
                // TODO: Add future implementation for character and glyph lookups
                if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = c };

                writeIndex += 1;
            }

            m_TextStyleStackDepth = 0;

            // Insert Closing Style
            if (textStyle != null && textStyle.hashCode != (int)MarkupTag.NORMAL)
                TextGeneratorUtilities.InsertClosingStyleTag(ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);

            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

            m_TextProcessingArray[writeIndex].unicode = 0;
            m_InternalTextProcessingArraySize = writeIndex;
        }

        bool PopulateFontAsset(TextGenerationSettings generationSettings, TextProcessingElement[] textProcessingArray)
        {
            bool canWriteOnAsset = !IsExecutingJob;
            TextSettings textSettings = generationSettings.textSettings;

            int spriteCount = 0;

            m_TotalCharacterCount = 0;
            m_isTextLayoutPhase = false;
            m_TagNoParsing = false;
            m_FontStyleInternal = generationSettings.fontStyle;
            m_FontStyleStack.Clear();

            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;

            m_MaterialReferenceStack.SetDefault(new MaterialReference(m_CurrentMaterialIndex, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            m_MaterialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

            m_TextElementType = TextElementType.Character;

            if (generationSettings.overflowMode == TextOverflowMode.Ellipsis)
            {
                GetEllipsisSpecialCharacter(generationSettings);

                if (m_Ellipsis.character != null)
                {
                    if (m_Ellipsis.fontAsset.GetHashCode() != m_CurrentFontAsset.GetHashCode())
                    {
                        if (textSettings.matchMaterialPreset && m_CurrentMaterial.GetHashCode() != m_Ellipsis.fontAsset.material.GetHashCode())
                        {
                            if (!canWriteOnAsset)
                                return false;
                            m_Ellipsis.material = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_Ellipsis.fontAsset.material);
                        }
                        else
                            m_Ellipsis.material = m_Ellipsis.fontAsset.material;

                        m_Ellipsis.materialIndex = MaterialReference.AddMaterialReference(m_Ellipsis.material, m_Ellipsis.fontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                        m_MaterialReferences[m_Ellipsis.materialIndex].referenceCount = 0;
                    }
                }
            }

            // Check if we should process Ligatures
            bool ligature = generationSettings.fontFeatures.Contains(OTL_FeatureTag.liga);

            // Parsing XML tags in the text
            for (int i = 0; i < textProcessingArray.Length && textProcessingArray[i].unicode != 0; i++)
            {
                uint unicode = textProcessingArray[i].unicode;
                int prevMaterialIndex = m_CurrentMaterialIndex;

                // PARSE XML TAGS
                #region PARSE XML TAGS
                if (generationSettings.richText && unicode == '<')
                {
                    prevMaterialIndex = m_CurrentMaterialIndex;
                    int endTagIndex;

                    // Check if Tag is Valid
                    if (ValidateHtmlTag(textProcessingArray, i + 1, out endTagIndex, generationSettings, null, out bool isThreadSuccess))
                    {
                        int tagStartIndex = textProcessingArray[i].stringIndex;
                        i = endTagIndex;

                        if (m_TextElementType == TextElementType.Sprite)
                        {
                            // Restore element type and material index to previous values.
                            m_TextElementType = TextElementType.Character;
                            m_CurrentMaterialIndex = prevMaterialIndex;

                            spriteCount += 1;
                            m_TotalCharacterCount += 1;
                        }

                        continue;
                    }
                    if(!isThreadSuccess)
                        return false;
                }
                #endregion

                bool isUsingFallbackOrAlternativeTypeface = false;

                FontAsset prevFontAsset = m_CurrentFontAsset;
                Material prevMaterial = m_CurrentMaterial;
                prevMaterialIndex = m_CurrentMaterialIndex;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles
                if (m_TextElementType == TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)unicode))
                            unicode = char.ToLower((char)unicode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        // Only convert lowercase characters to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);
                    }
                }
                #endregion

                if (!canWriteOnAsset && m_CurrentFontAsset.m_CharacterLookupDictionary == null)
                    return false;

                // Lookup the Glyph data for each character and cache it.
                #region LOOKUP GLYPH
                TextElement character = GetTextElement(generationSettings, (uint)unicode, m_CurrentFontAsset, m_FontStyleInternal, m_FontWeightInternal, out _, ligature);

                // Special handling for missing character.
                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                if (character == null)
                {
                    if (!canWriteOnAsset)
                        return false;

                    // Save the original unicode character
                    uint srcGlyph = unicode;

                    // Try replacing the missing glyph character by the Settings file Missing Glyph or Square (9633) character.
                    unicode = textProcessingArray[i].unicode = (uint)textSettings.missingCharacterUnicode == 0 ? k_Square : (uint)textSettings.missingCharacterUnicode;

                    // Check for the missing glyph character in the currently assigned font asset and its fallbacks
                    character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out _, ligature);

                    if (character == null)
                    {
                        if (textSettings.GetStaticFallbackOSFontAsset() == null && !canWriteOnAsset)
                            return false;
                        // Search for the missing glyph character in the Settings Fallback list.
                        character = FontAssetUtilities.GetCharacterFromFontAssetsInternal((uint)unicode, m_CurrentFontAsset, textSettings.fallbackFontAssets, textSettings.fallbackOSFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out _, ligature);
                    }

                    if (character == null)
                    {
                        // Search for the missing glyph in the Settings Default Font Asset.
                        if (textSettings.defaultFontAsset != null)
                            character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, textSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out _, ligature);
                    }

                    if (character == null)
                    {
                        // Use Space (32) Glyph from the currently assigned font asset.
                        unicode = textProcessingArray[i].unicode = 32;
                        character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out _, ligature);
                    }

                    if (character == null)
                    {
                        // Use End of Text (0x03) Glyph from the currently assigned font asset.
                        unicode = textProcessingArray[i].unicode = k_EndOfText;
                        character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out _, ligature);
                    }

                    if (textSettings.displayWarnings)
                    {
                        string formattedWarning = srcGlyph > 0xFFFF
                            ? string.Format("The character with Unicode value \\U{0:X8} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4}.", srcGlyph, generationSettings.fontAsset.name, character.unicode)
                            : string.Format("The character with Unicode value \\u{0:X4} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4}.", srcGlyph, generationSettings.fontAsset.name, character.unicode);

                        Debug.LogWarning(formattedWarning);
                    }
                }

                if (character.elementType == TextElementType.Character)
                {
                    bool textAssetEquals = canWriteOnAsset ? character.textAsset.instanceID == m_CurrentFontAsset.instanceID : character.textAsset == m_CurrentFontAsset;
                    if (!textAssetEquals)
                    {
                        isUsingFallbackOrAlternativeTypeface = true;
                        m_CurrentFontAsset = character.textAsset as FontAsset;
                    }

                    #region VARIATION SELECTOR
                    uint nextCharacter = i + 1 < textProcessingArray.Length ? (uint)textProcessingArray[i + 1].unicode : 0;
                    if (nextCharacter >= 0xFE00 && nextCharacter <= 0xFE0F || nextCharacter >= 0xE0100 && nextCharacter <= 0xE01EF)
                    {

                        uint variantGlyphIndex;
                        // Get potential variant glyph index
                        if (!m_CurrentFontAsset.TryGetGlyphVariantIndexInternal(unicode, nextCharacter, out variantGlyphIndex))
                        {
                            if (!canWriteOnAsset)
                                return false;
                            variantGlyphIndex = m_CurrentFontAsset.GetGlyphVariantIndex(unicode, nextCharacter);
                            m_CurrentFontAsset.TryAddGlyphVariantIndexInternal(unicode, nextCharacter, variantGlyphIndex);
                        }

                        if (variantGlyphIndex != 0)
                        {
                            m_CurrentFontAsset.TryAddGlyphInternal(variantGlyphIndex, out Glyph glyph);
                        }

                        textProcessingArray[i + 1].unicode = 0x1A;
                        i += 1;
                    }
                    #endregion

                    #region LIGATURES
                    if (ligature && m_CurrentFontAsset.fontFeatureTable.m_LigatureSubstitutionRecordLookup.TryGetValue(character.glyphIndex, out List<LigatureSubstitutionRecord> records))
                    {
                        if (records == null)
                            break;

                        for (int j = 0; j < records.Count; j++)
                        {
                            LigatureSubstitutionRecord record = records[j];

                            int componentCount = record.componentGlyphIDs.Length;
                            uint ligatureGlyphID = record.ligatureGlyphID;

                            //
                            for (int k = 1; k < componentCount; k++)
                            {
                                uint componentUnicode = (uint)textProcessingArray[i + k].unicode;

                                // Special Handling for Zero Width Joiner (ZWJ)
                                //if (componentUnicode == 0x200D)
                                //    continue;

                                uint glyphIndex = m_CurrentFontAsset.GetGlyphIndex(componentUnicode, out bool success);

                                if (!success)
                                    return false;

                                if (glyphIndex == record.componentGlyphIDs[k])
                                    continue;

                                ligatureGlyphID = 0;
                                break;
                            }

                            if (ligatureGlyphID != 0)
                            {
                                if (!canWriteOnAsset)
                                    return false;

                                if (m_CurrentFontAsset.TryAddGlyphInternal(ligatureGlyphID, out Glyph _))
                                {

                                    // Update text processing array
                                    for (int c = 0; c < componentCount; c++)
                                    {
                                        if (c == 0)
                                        {
                                            textProcessingArray[i + c].length = componentCount;
                                            continue;
                                        }

                                        textProcessingArray[i + c].unicode = 0x1A;
                                    }

                                    i += componentCount - 1;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                // Special handling if the character is a sprite.
                if (character.elementType == TextElementType.Sprite)
                {
                    SpriteAsset spriteAssetRef = character.textAsset as SpriteAsset;
                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(spriteAssetRef.material, spriteAssetRef, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                    // Restore element type and material index to previous values.
                    m_TextElementType = TextElementType.Character;
                    m_CurrentMaterialIndex = prevMaterialIndex;

                    spriteCount += 1;
                    m_TotalCharacterCount += 1;

                    continue;
                }

                if (isUsingFallbackOrAlternativeTypeface && m_CurrentFontAsset.instanceID != generationSettings.fontAsset.instanceID)
                {
                    // Create Fallback material instance matching current material preset if necessary
                    if (canWriteOnAsset)
                    {
                        if (textSettings.matchMaterialPreset)
                            m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_CurrentFontAsset.material);
                        else
                            m_CurrentMaterial = m_CurrentFontAsset.material;
                    }
                    else
                    {
                        if (textSettings.matchMaterialPreset)
                            return false;
                        else
                            m_CurrentMaterial = m_CurrentFontAsset.material;
                    }

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                }

                // Handle Multi Atlas Texture support
                if (character != null && character.glyph.atlasIndex > 0)
                {
                    if (canWriteOnAsset)
                        m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentFontAsset, m_CurrentMaterial, character.glyph.atlasIndex);
                    else
                        return false;

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                    isUsingFallbackOrAlternativeTypeface = true;
                }

                if (!char.IsWhiteSpace((char)unicode) && unicode != CodePoint.ZERO_WIDTH_SPACE)
                {
                    // Limit the mesh of the main text object to 65535 vertices and use sub objects for the overflow.
                    if (generationSettings.isIMGUI && m_MaterialReferences[m_CurrentMaterialIndex].referenceCount >= 16383)
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_CurrentMaterial), m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                    m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;
                }

                m_MaterialReferences[m_CurrentMaterialIndex].isFallbackMaterial = isUsingFallbackOrAlternativeTypeface;

                // Restore previous font asset and material if fallback font was used.
                if (isUsingFallbackOrAlternativeTypeface)
                {
                    m_MaterialReferences[m_CurrentMaterialIndex].fallbackMaterial = prevMaterial;
                    m_CurrentFontAsset = prevFontAsset;
                    m_CurrentMaterial = prevMaterial;
                    m_CurrentMaterialIndex = prevMaterialIndex;
                }

                m_TotalCharacterCount += 1;
            }

            return true;
        }

        /// <summary>
        /// Update the margin width and height
        /// </summary>
        void ComputeMarginSize(Rect rect, Vector4 margins)
        {
            m_MarginWidth = rect.width - margins.x - margins.z;
            m_MarginHeight = rect.height - margins.y - margins.w;

            // Update the corners of the RectTransform
            m_RectTransformCorners[0].x = 0;
            m_RectTransformCorners[0].y = 0;
            m_RectTransformCorners[1].x = 0;
            m_RectTransformCorners[1].y = rect.height;
            m_RectTransformCorners[2].x = rect.width;
            m_RectTransformCorners[2].y = rect.height;
            m_RectTransformCorners[3].x = rect.width;
            m_RectTransformCorners[3].y = 0;
        }

        protected struct SpecialCharacter
        {
            public Character character;
            public FontAsset fontAsset;
            public Material material;
            public int materialIndex;

            public SpecialCharacter(Character character, int materialIndex)
            {
                this.character = character;
                this.fontAsset = character.textAsset as FontAsset;
                this.material = this.fontAsset != null ? this.fontAsset.material : null;
                this.materialIndex = materialIndex;
            }
        }

        /// <summary>
        /// Method used to find and cache references to the Underline and Ellipsis characters.
        /// </summary>
        /// <param name=""></param>
        protected bool GetSpecialCharacters(TextGenerationSettings generationSettings)
        {
            if (!GetEllipsisSpecialCharacter(generationSettings))
                return false;

            return GetUnderlineSpecialCharacter(generationSettings);
        }

        protected bool GetEllipsisSpecialCharacter(TextGenerationSettings generationSettings)
        {
            bool canWriteOnAsset = !IsExecutingJob;
            bool isUsingAlternativeTypeface;

            FontAsset fontAsset = m_CurrentFontAsset ?? generationSettings.fontAsset;
            TextSettings textSettings = generationSettings.textSettings;
            bool populateLigature = generationSettings.fontFeatures.Contains(OTL_FeatureTag.liga);

            // Search base font asset
            Character character = FontAssetUtilities.GetCharacterFromFontAsset(k_HorizontalEllipsis, fontAsset, false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, populateLigature);

            if (character == null)
            {
                // Search primary fallback list
                if (fontAsset.m_FallbackFontAssetTable != null && fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = FontAssetUtilities.GetCharacterFromFontAssetsInternal(k_HorizontalEllipsis, fontAsset, fontAsset.m_FallbackFontAssetTable, OSFallbackList: null, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, populateLigature);
            }

            // Search the setting's general fallback list
            if (character == null)
            {
                if (textSettings.GetStaticFallbackOSFontAsset() == null && !canWriteOnAsset)
                    return false;
                character = FontAssetUtilities.GetCharacterFromFontAssetsInternal(k_HorizontalEllipsis, fontAsset, textSettings.fallbackFontAssets, textSettings.fallbackOSFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, populateLigature);
            }

            // Search the setting's default font asset
            if (character == null)
            {
                if (textSettings.defaultFontAsset != null)
                    character = FontAssetUtilities.GetCharacterFromFontAsset(k_HorizontalEllipsis, textSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, populateLigature);
            }

            if (character != null)
                m_Ellipsis = new SpecialCharacter(character, 0);

            return true;
        }

        protected bool GetUnderlineSpecialCharacter(TextGenerationSettings generationSettings)
        {
            bool canWriteOnAsset = !IsExecutingJob;
            bool isUsingAlternativeTypeface;

            FontAsset fontAsset = m_CurrentFontAsset ?? generationSettings.fontAsset;
            TextSettings textSettings = generationSettings.textSettings;
            bool populateLigature = generationSettings.fontFeatures.Contains(OTL_FeatureTag.liga);

            // Search base font asset
            Character character = FontAssetUtilities.GetCharacterFromFontAsset(0x5F, fontAsset, false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, populateLigature);

            if (character == null && !canWriteOnAsset)
                return false;

            if (character != null)
                m_Underline = new SpecialCharacter(character, m_CurrentMaterialIndex);

            return true;
        }

        protected void DoMissingGlyphCallback(uint unicode, int stringIndex, FontAsset fontAsset, TextInfo textInfo)
        {
            // Event to allow users to modify the content of the text info before the text is rendered.
            OnMissingCharacter?.Invoke(unicode, stringIndex, textInfo, fontAsset);
        }
    }
}
