// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.TextCore.LowLevel;

#pragma warning disable CS0618 // glyphLookupTable is obsolete; used for internal glyph caching

namespace UnityEngine.TextCore.Text
{
    // ================================================================================
    // Properties and functions related to character and glyph additions as well as
    // tacking glyphs that need to be added to various font asset atlas textures.
    // ================================================================================
    public partial class FontAsset
    {
        // List and HashSet used for tracking font assets whose font atlas texture and character data needs updating.
        static List<FontAsset> k_FontAssets_FontFeaturesUpdateQueue = new List<FontAsset>();
        static HashSet<EntityId> k_FontAssets_FontFeaturesUpdateQueueLookup = new HashSet<EntityId>();

        static List<FontAsset> k_FontAssets_KerningUpdateQueue = new List<FontAsset>();
        static HashSet<EntityId> k_FontAssets_KerningUpdateQueueLookup = new HashSet<EntityId>();

        static List<Texture2D> k_FontAssets_AtlasTexturesUpdateQueue = new List<Texture2D>();
        static HashSet<EntityId> k_FontAssets_AtlasTexturesUpdateQueueLookup = new HashSet<EntityId>();

        /// <summary>
        /// Internal static array used to avoid allocations when using the GetGlyphPairAdjustmentTable().
        /// </summary>
        internal static uint[] k_GlyphIndexArray;

        internal static void RegisterFontAssetForFontFeatureUpdate(FontAsset fontAsset)
        {
            EntityId entityId = fontAsset.entityId;

            if (k_FontAssets_FontFeaturesUpdateQueueLookup.Add(entityId))
                k_FontAssets_FontFeaturesUpdateQueue.Add(fontAsset);
        }

        internal static void RegisterFontAssetForKerningUpdate(FontAsset fontAsset)
        {
            EntityId entityId = fontAsset.entityId;

            if (k_FontAssets_KerningUpdateQueueLookup.Add(entityId))
                k_FontAssets_KerningUpdateQueue.Add(fontAsset);
        }

        /// <summary>
        /// Function called to update the font atlas texture and character data of font assets to which
        /// new characters were added.
        /// </summary>
        internal static void UpdateFontFeaturesForFontAssetsInQueue()
        {
            int count = k_FontAssets_FontFeaturesUpdateQueue.Count;

            for (int i = 0; i < count; i++)
            {
                k_FontAssets_FontFeaturesUpdateQueue[i].UpdateGPOSFontFeaturesForNewlyAddedGlyphs();
            }

            if (count > 0)
            {
                k_FontAssets_FontFeaturesUpdateQueue.Clear();
                k_FontAssets_FontFeaturesUpdateQueueLookup.Clear();
            }

            count = k_FontAssets_KerningUpdateQueue.Count;

            for (int i = 0; i < count; i++)
            {
                k_FontAssets_KerningUpdateQueue[i].UpdateGlyphAdjustmentRecordsForNewGlyphs();
            }

            if (count > 0)
            {
                k_FontAssets_KerningUpdateQueue.Clear();
                k_FontAssets_KerningUpdateQueueLookup.Clear();
            }
        }

        /// <summary>
        /// Register Atlas Texture for Apply()
        /// </summary>
        /// <param name="texture">The texture on which to call Apply().</param>
        internal static void RegisterAtlasTextureForApply(Texture2D texture)
        {
            EntityId entityId = texture.GetEntityId();
            Debug.Assert(UnsafeUtility.SizeOf<EntityId>() == sizeof(long), "EntityId size has changed, please update the code below");
            if (k_FontAssets_AtlasTexturesUpdateQueueLookup.Add(entityId))
                k_FontAssets_AtlasTexturesUpdateQueue.Add(texture);
        }

        internal static void UpdateAtlasTexturesInQueue()
        {
            int count = k_FontAssets_AtlasTexturesUpdateQueueLookup.Count;

            for (int i = 0; i < count; i++)
                k_FontAssets_AtlasTexturesUpdateQueue[i].Apply(false, false);

            if (count > 0)
            {
                k_FontAssets_AtlasTexturesUpdateQueue.Clear();
                k_FontAssets_AtlasTexturesUpdateQueueLookup.Clear();
            }
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal static void UpdateFontAssetsInUpdateQueue()
        {
            UpdateAtlasTexturesInQueue();
            UpdateFontFeaturesForFontAssetsInQueue();
        }

                /// <summary>
        /// Function to check if a certain character exists in the font asset.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool HasCharacter(int character)
        {
            if (characterLookupTable == null)
                return false;

            return m_CharacterLookupDictionary.ContainsKey((uint)character);
        }

        /// <summary>
        /// Function to check if a character is contained in the font asset with the option to also check potential local fallbacks.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="tryAddCharacter"></param>
        /// <returns></returns>
        public bool HasCharacter(char character, bool searchFallbacks = false, bool tryAddCharacter = false)
        {
            return HasCharacter((uint)character, searchFallbacks, tryAddCharacter);
        }

        /// <summary>
        /// Function to check if a character is contained in the font asset with the option to also check potential local fallbacks.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="tryAddCharacter"></param>
        /// <returns></returns>
        public bool HasCharacter(uint character, bool searchFallbacks = false, bool tryAddCharacter = false)
        {
            // Read font asset definition if it hasn't already been done.
            if (characterLookupTable == null)
                return false;

            // Check font asset
            if (m_CharacterLookupDictionary.ContainsKey(character))
                return true;

            // Check if font asset is dynamic and if so try to add the requested character to it.
            if (tryAddCharacter && (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS))
            {
                Character returnedCharacter;

                if (TryAddCharacterInternal(character, FontStyles.Normal, TextFontWeight.Regular, out returnedCharacter))
                    return true;
            }

            if (searchFallbacks)
            {
                // Initialize or clear font asset lookup
                if (k_SearchedFontAssetLookup == null)
                    k_SearchedFontAssetLookup = new HashSet<EntityId>();
                else
                    k_SearchedFontAssetLookup.Clear();

                // Add current font asset to lookup
                k_SearchedFontAssetLookup.Add(GetEntityId());

                // Check font asset fallbacks
                if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                {
                    for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                    {
                        FontAsset fallback = fallbackFontAssetTable[i];
                        EntityId fallbackID = fallback.GetEntityId();

                        // Search fallback if not already contained in lookup
                        if (k_SearchedFontAssetLookup.Add(fallbackID))
                        {
                            if (fallback.HasCharacter_Internal(character, FontStyles.Normal, TextFontWeight.Regular, true, tryAddCharacter))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns a list of missing characters.
        /// </summary>
        /// <param name="text">String containing the characters to check.</param>
        /// <param name="missingCharacters">List of missing characters.</param>
        /// <returns></returns>
        public bool HasCharacters(string text, out List<char> missingCharacters)
        {
            if (characterLookupTable == null)
            {
                missingCharacters = null;
                return false;
            }

            missingCharacters = new List<char>();

            for (int i = 0; i < text.Length; i++)
            {
                uint character = FontAssetUtilities.GetCodePoint(text, ref i);

                if (!m_CharacterLookupDictionary.ContainsKey(character))
                    missingCharacters.Add((char)character);
            }

            if (missingCharacters.Count == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Function to check if the characters in the given string are contained in the font asset with the option to also check its potential local fallbacks.
        /// </summary>
        /// <param name="text">String containing the characters to check.</param>
        /// <param name="missingCharacters">Array containing the unicode values of the missing characters.</param>
        /// <param name="searchFallbacks">Determines if fallback font assets assigned to this font asset should be searched.</param>
        /// <param name="tryAddCharacter"></param>
        /// <returns>Returns true if all requested characters are available in the font asset and potential fallbacks.</returns>
        public bool HasCharacters(string text, out uint[] missingCharacters, bool searchFallbacks = false, bool tryAddCharacter = false)
        {
            missingCharacters = null;

            // Read font asset definition if it hasn't already been done.
            if (characterLookupTable == null)
                return false;

            // Clear internal list of
            s_MissingCharacterList.Clear();

            for (int i = 0; i < text.Length; i++)
            {
                bool isMissingCharacter = true;

                uint character = FontAssetUtilities.GetCodePoint(text, ref i);

                if (m_CharacterLookupDictionary.ContainsKey(character))
                    continue;

                // Check if fallback is dynamic and if so try to add the requested character to it.
                if (tryAddCharacter && (atlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS))
                {
                    Character returnedCharacter;

                    if (TryAddCharacterInternal(character, FontStyles.Normal, TextFontWeight.Regular, out returnedCharacter))
                        continue;
                }

                if (searchFallbacks)
                {
                    // Initialize or clear font asset lookup
                    if (k_SearchedFontAssetLookup == null)
                        k_SearchedFontAssetLookup = new HashSet<EntityId>();
                    else
                        k_SearchedFontAssetLookup.Clear();

                    // Add current font asset to lookup
                    k_SearchedFontAssetLookup.Add(GetEntityId());

                    // Check font asset fallbacks
                    if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                    {
                        for (int j = 0; j < fallbackFontAssetTable.Count && fallbackFontAssetTable[j] != null; j++)
                        {
                            FontAsset fallback = fallbackFontAssetTable[j];
                            EntityId fallbackID = fallback.GetEntityId();

                            // Search fallback if it has not already been searched
                            if (k_SearchedFontAssetLookup.Add(fallbackID))
                            {
                                if (fallback.HasCharacter_Internal(character, FontStyles.Normal, TextFontWeight.Regular, true, tryAddCharacter) == false)
                                    continue;

                                isMissingCharacter = false;
                                break;
                            }
                        }
                    }
                }

                if (isMissingCharacter)
                    s_MissingCharacterList.Add(character);
            }

            if (s_MissingCharacterList.Count > 0)
            {
                missingCharacters = s_MissingCharacterList.ToArray();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns false if any characters are missing.
        /// </summary>
        /// <param name="text">String containing the characters to check</param>
        /// <returns></returns>
        public bool HasCharacters(string text)
        {
            if (characterLookupTable == null)
                return false;

            for (int i = 0; i < text.Length; i++)
            {
                uint character = FontAssetUtilities.GetCodePoint(text, ref i);

                if (!m_CharacterLookupDictionary.ContainsKey(character))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Function to extract all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static string GetCharacters(FontAsset fontAsset)
        {
            string characters = string.Empty;

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters += (char)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }

        /// <summary>
        /// Function which returns an array that contains all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static int[] GetCharactersArray(FontAsset fontAsset)
        {
            int[] characters = new int[fontAsset.characterTable.Count];

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters[i] = (int)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="unicodes">Array that contains the characters to add to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(uint[] unicodes, bool includeFontFeatures = false)
        {
            return TryAddCharacters(unicodes, out _, includeFontFeatures);
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="unicodes">Array that contains the characters to add to the font asset.</param>
        /// <param name="missingUnicodes">Array containing the characters that could not be added to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(uint[] unicodes, out uint[] missingUnicodes, bool includeFontFeatures = false)
        {
            using var profilerScope = k_TryAddCharactersMarker.Auto();

            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (unicodes == null || unicodes.Length == 0 || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided Unicode list is Null or Empty.", this);

                missingUnicodes = null;
                return false;
            }

            // Load font face.
            if (LoadFontFace() != FontEngineError.Success)
            {
                missingUnicodes = new uint[unicodes.Length];
                var index = 0;
                foreach (var unicode in unicodes)
                {
                    missingUnicodes[index++] = unicode;
                }
                return false;
            }

            // Make sure font asset has been initialized
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();

            // guaranteed to be non-null at this point
            var characterLookupDictionary = m_CharacterLookupDictionary!;
            var glyphLookupDictionary = m_GlyphLookupDictionary!;

            // Clear lists used to track which character and glyphs were added or missing.
            m_GlyphsToAdd.Clear();
            m_GlyphsToAddLookup.Clear();
            m_CharactersToAdd.Clear();
            m_CharactersToAddLookup.Clear();
            s_MissingCharacterList.Clear();

            var isMissingCharacters = false;
            var unicodeCount = unicodes.Length;

            for (int i = 0; i < unicodeCount; i++)
            {
                uint unicode = FontAssetUtilities.GetCodePoint(unicodes, ref i);

                // Check if character is already contained in the character table.
                if (characterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this Unicode value.
                var glyphIndex = FontEngine.GetGlyphIndex(unicode);

                // Skip missing glyphs
                if (glyphIndex == 0)
                {
                    // Special handling for characters with potential alternative glyph representations
                    switch (unicode)
                    {
                        case 0xA0: // Non Breaking Space <NBSP>
                            // Use Space
                            glyphIndex = FontEngine.GetGlyphIndex(0x20);
                            break;
                        case 0xAD: // Soft Hyphen <SHY>
                        case 0x2011: // Non Breaking Hyphen
                            // Use Hyphen Minus
                            glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                            break;
                    }

                    // Skip to next character if no potential alternative glyph representation is present in font file.
                    if (glyphIndex == 0)
                    {
                        // Add character to list of missing characters.
                        s_MissingCharacterList.Add(unicode);

                        isMissingCharacters = true;
                        continue;
                    }
                }

                var character = new Character(unicode, glyphIndex);

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (glyphLookupDictionary.TryGetValue(glyphIndex, out var value))
                {
                    // Add a reference to the source text asset and glyph
                    character.glyph = value;
                    character.textAsset = this;

                    m_CharacterTable.Add(character);
                    characterLookupDictionary.Add(unicode, character);
                    continue;
                }

                // Make sure glyph index has not already been added to list of glyphs to add.
                if (m_GlyphsToAddLookup.Add(glyphIndex))
                    m_GlyphsToAdd.Add(glyphIndex);

                // Make sure unicode / character has not already been added.
                if (m_CharactersToAddLookup.Add(unicode))
                    m_CharactersToAdd.Add(character);
            }

            if (m_GlyphsToAdd.Count == 0)
            {
                //Debug.LogWarning("No characters will be added to font asset [" + this.name + "] either because they are already present in the font asset or missing from the font file.");
                missingUnicodes = unicodes;
                return isMissingCharacters ? false : true;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width <= 1 || m_AtlasTextures[m_AtlasTextureIndex].height <= 1)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph[] glyphs;
            var allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            // Add new glyphs to relevant font asset data structure
            {
                var additionalCapacity = glyphs.Length;
                EnsureAdditionalCapacity(m_GlyphTable, additionalCapacity);
                EnsureAdditionalCapacity(glyphLookupDictionary, additionalCapacity);
                EnsureAdditionalCapacity(m_GlyphIndexListNewlyAdded, additionalCapacity);
                EnsureAdditionalCapacity(m_GlyphIndexList, additionalCapacity);
            }

            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];

                var glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                glyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to allow
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures as well as track glyphs that could not be added to the current atlas texture.
            {
                var additionalCapacity = m_CharactersToAdd.Count;
                EnsureAdditionalCapacity(m_GlyphsToAdd, additionalCapacity);
                EnsureAdditionalCapacity(m_CharacterTable, additionalCapacity);
                EnsureAdditionalCapacity(characterLookupDictionary, additionalCapacity);
            }

            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                var character = m_CharactersToAdd[i];
                if (glyphLookupDictionary.TryGetValue(character.glyphIndex, out var glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                characterLookupDictionary.Add(character.unicode, character);

                // Remove character from list to add
                m_CharactersToAdd.RemoveAt(i);
                i--;
            }

            // Try adding missing glyphs to
            if (m_IsMultiAtlasTexturesEnabled && allGlyphsAddedToTexture == false)
            {
                while (allGlyphsAddedToTexture == false)
                    allGlyphsAddedToTexture = TryAddGlyphsToNewAtlasTexture();
            }
            else if (!allGlyphsAddedToTexture)
            {
                Debug.Log($"Atlas is full, consider enabling multi-atlas textures in the Font Asset: {name}");
            }

            // Get Font Features for the given characters
            if (includeFontFeatures)
            {
                UpdateFontFeaturesForNewlyAddedGlyphs();
            }

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);

            // Populate list of missing characters
            foreach (var character in m_CharactersToAdd)
            {
                s_MissingCharacterList.Add(character.unicode);
            }

            missingUnicodes = null;

            if (s_MissingCharacterList.Count > 0)
                missingUnicodes = s_MissingCharacterList.ToArray();

            return allGlyphsAddedToTexture && !isMissingCharacters;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal bool TryAddGlyphs(List<uint> glyphsToAdd)
        {
            // Load font face.
            if (LoadFontFace() != FontEngineError.Success)
            {
                return false;
            }

            // Make sure font asset has been initialized
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
            {
                ReadFontAssetDefinition();

                // In theory we know glyphsToAdd are missing, but they might not be if our lookups are simply not initialized.
                // We need to clean the list here.
                glyphsToAdd.RemoveAll(glyphId => m_GlyphLookupDictionary.ContainsKey(glyphId));

                if (glyphsToAdd.Count == 0)
                    return true;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width <= 1 || m_AtlasTextures[m_AtlasTextureIndex].height <= 1)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            bool allGlyphsAddedToTexture = false;
            while (!allGlyphsAddedToTexture)
            {
                Glyph[] glyphs;
                allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(glyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

                // Add new glyphs to relevant font asset data structure
                {
                    var additionalCapacity = glyphs.Length;
                    EnsureAdditionalCapacity(m_GlyphTable, additionalCapacity);
                    EnsureAdditionalCapacity(m_GlyphLookupDictionary, additionalCapacity);
                    EnsureAdditionalCapacity(m_GlyphIndexListNewlyAdded, additionalCapacity);
                    EnsureAdditionalCapacity(m_GlyphIndexList, additionalCapacity);
                }

                var successfullyAddedGlyphIndices = new HashSet<uint>();
                for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
                {
                    Glyph glyph = glyphs[i];

                    var glyphIndex = glyph.index;

                    glyph.atlasIndex = m_AtlasTextureIndex;

                    // Add new glyph to glyph table.
                    m_GlyphTable.Add(glyph);
                    m_GlyphLookupDictionary.Add(glyphIndex, glyph);
                    m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                    m_GlyphIndexList.Add(glyphIndex);

                    successfullyAddedGlyphIndices.Add(glyphIndex);
                }

                if (successfullyAddedGlyphIndices.Count > 0)
                {
                    glyphsToAdd.RemoveAll(id => successfullyAddedGlyphIndices.Contains(id));
                }

                RegisterAtlasTextureForApply(m_AtlasTextures[m_AtlasTextureIndex]);

                if (!m_IsMultiAtlasTexturesEnabled && !allGlyphsAddedToTexture)
                {
                    Debug.Log($"Atlas is full, consider enabling multi-atlas textures in the Font Asset: {name}");
                    break;
                }
                else if (!allGlyphsAddedToTexture && m_UsedGlyphRects.Count > 0)
                {
                    SetupNewAtlasTexture();
                }
                else if (!allGlyphsAddedToTexture && m_UsedGlyphRects.Count == 0)
                {
                    Debug.Log($"Texture is too small, consider reducing the sampling point size or augmenting the atlas's size in the Font Asset: {name}");
                    break;
                }
            }

            if (m_GetFontFeatures && m_GlyphIndexListNewlyAdded.Count > 0)
            {
                RegisterFontAssetForKerningUpdate(this);
            }

            FontEngine.SetTextureUploadMode(true);
            return allGlyphsAddedToTexture;
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="characters">String containing the characters to add to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(string characters, bool includeFontFeatures = false)
        {
            return TryAddCharacters(characters, out _, includeFontFeatures);
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="characters">String containing the characters to add to the font asset.</param>
        /// <param name="missingCharacters">String containing the characters that could not be added to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(string characters, out string missingCharacters, bool includeFontFeatures = false)
        {
            var charactersUInt = new uint[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                charactersUInt[i] = characters[i];
            }

            bool success = TryAddCharacters(charactersUInt, out uint[] missingCharactersUInt, includeFontFeatures);

            if (missingCharactersUInt == null || missingCharactersUInt.Length <= 0)
            {
                missingCharacters = null;
                return success;
            }
            StringBuilder stringBuilder = new StringBuilder(missingCharactersUInt.Length);
            foreach (uint value in missingCharactersUInt)
            {
                stringBuilder.Append((char)value);
            }
            missingCharacters = stringBuilder.ToString();
            return success;
        }

        internal bool TryAddGlyphVariantIndexInternal(uint unicode, uint nextCharacter, uint variantGlyphIndex)
        {
            return m_VariantGlyphIndexes.TryAdd((unicode, nextCharacter), variantGlyphIndex);
        }

        internal bool TryGetGlyphVariantIndexInternal(uint unicode, uint nextCharacter, out uint variantGlyphIndex)
        {
            return m_VariantGlyphIndexes.TryGetValue((unicode, nextCharacter), out variantGlyphIndex);
        }

        internal bool TryAddGlyphInternal(uint glyphIndex, out Glyph glyph, bool populateLigatures = true)
        {
            using (k_TryAddGlyphMarker.Auto())
            {
                glyph = null;

                // Check if glyph is already present in the font asset.
                if (glyphLookupTable.TryGetValue(glyphIndex, out glyph))
                {
                    return true;
                }

                // Load font face.
                if (LoadFontFace() != FontEngineError.Success)
                    return false;

                return TryAddGlyphToAtlas(glyphIndex, out glyph, populateLigatures);
            }
        }

        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// Function assumes internal user has already checked to make sure the character is not already contained in the font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <param name="getFontFeatures">Determines if Ligatures are populated or not</param>
        /// <param name="populateLigatures"></param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacterInternal(uint unicode, out Character character)
        {
            return TryAddCharacterInternal(unicode, FontStyles.Normal, TextFontWeight.Regular, out character);
        }

        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// Function assumes internal user has already checked to make sure the character is not already contained in the font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <param name="getFontFeatures">Determines if Ligatures are populated or not</param>
        /// <param name="populateLigatures"></param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacterInternal(uint unicode, FontStyles fontStyle, TextFontWeight fontWeight, out Character character, bool populateLigatures = true)
        {
            using (k_TryAddCharacterMarker.Auto())
            {
                character = null;

                // Check if the Unicode character is already known to be missing from the source font file.
                if (m_MissingUnicodesFromFontFile.Contains(unicode))
                {
                    return false;
                }

                // Load font face.
                if (LoadFontFace() != FontEngineError.Success)
                {
                    return false;
                }

                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
                if (glyphIndex == 0)
                {
                    // Special handling for characters with potential alternative glyph representations
                    switch (unicode)
                    {
                        case 0xA0: // Non Breaking Space <NBSP>
                            // Use Space
                            glyphIndex = FontEngine.GetGlyphIndex(0x20);
                            break;
                        case 0xAD: // Soft Hyphen <SHY>
                        case 0x2011: // Non Breaking Hyphen
                            // Use Hyphen Minus
                            glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                            break;
                    }

                    // Return if no potential alternative glyph representation is present in font file.
                    if (glyphIndex == 0)
                    {
                        m_MissingUnicodesFromFontFile.Add(unicode);

                        return false;
                    }
                }

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (glyphLookupTable.ContainsKey(glyphIndex))
                {
                    character = CreateCharacterAndAddToCache(unicode, m_GlyphLookupDictionary[glyphIndex], fontStyle, fontWeight);

                    // Makes the changes to the font asset persistent.
                    RegisterResourceForUpdate?.Invoke(this);

                    return true;
                }

                Glyph glyph = null;
                if (TryAddGlyphToAtlas(glyphIndex, out glyph, populateLigatures))
                {
                    // Add new character
                    character = CreateCharacterAndAddToCache(unicode, glyph, fontStyle, fontWeight);
                    return true;
                }
                return false;
            }
        }

        bool TryAddGlyphToAtlas(uint glyphIndex, out Glyph glyph, bool populateLigatures = true)
        {
            glyph = null;
            if (m_AtlasTextures[m_AtlasTextureIndex].isReadable == false)
            {
                Debug.LogWarning($"Unable to add the requested glyph to font asset [{this.name}]'s atlas texture. Please make the texture [{m_AtlasTextures[m_AtlasTextureIndex].name}] readable.", m_AtlasTextures[m_AtlasTextureIndex]);
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width <= 1 || m_AtlasTextures[m_AtlasTextureIndex].height <= 1)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            // Set texture upload mode to batching texture.Apply()
            // This will be set back to true in TryAddGlyphToTexture
            // It makes no sense to force to apply the texture per glyph
            // TODO: during the refactor, make it always false and remove the api
            // once we are certain all code path call RegisterAtlasTextureForApply
            FontEngine.SetTextureUploadMode(false);

            if (TryAddGlyphToTexture(glyphIndex, out glyph, populateLigatures))
                return true;

            // Add glyph which did not fit in current atlas texture to new atlas texture.
            if (m_IsMultiAtlasTexturesEnabled && m_UsedGlyphRects.Count > 0)
            {
                // Create new atlas texture
                SetupNewAtlasTexture();

                FontEngine.SetTextureUploadMode(false);
                if (TryAddGlyphToTexture(glyphIndex, out glyph, populateLigatures))
                    return true;
            }
            else if (m_UsedGlyphRects.Count > 0)
            {
                Debug.Log($"Atlas is full, consider enabling multi-atlas textures in the Font Asset: {name}");;
            }

            return false;
        }

        bool TryAddGlyphToTexture(uint glyphIndex, out Glyph glyph, bool populateLigatures = true)
        {
            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Update glyph atlas index
                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexList.Add(glyphIndex);
                m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                if (m_GetFontFeatures/*&& TMP_Settings.getFontFeaturesAtRuntime*/)
                {
                    if (populateLigatures)
                    {
                        UpdateGSUBFontFeaturesForNewGlyphIndex(glyphIndex);
                        RegisterFontAssetForFontFeatureUpdate(this);
                    }
                    else
                        RegisterFontAssetForKerningUpdate(this);
                }

                RegisterAtlasTextureForApply(m_AtlasTextures[m_AtlasTextureIndex]);
                FontEngine.SetTextureUploadMode(true);

                // Makes the changes to the font asset persistent.
                RegisterResourceForUpdate?.Invoke(this);

                return true;
            }

            return false;
        }

        bool TryAddGlyphsToNewAtlasTexture()
        {
            // Create and prepare new atlas texture
            SetupNewAtlasTexture();

            Glyph[] glyphs;

            // Try adding remaining glyphs in the newly created atlas texture
            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            // Add new glyphs to relevant data structures.
            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to allow us to track glyphs
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures as well as track glyphs that could not be added to the current atlas texture.
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                Character character = m_CharactersToAdd[i];
                Glyph glyph;

                if (m_GlyphLookupDictionary.TryGetValue(character.glyphIndex, out glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(character.unicode, character);

                // Remove character
                m_CharactersToAdd.RemoveAt(i);
                i -= 1;
            }

            return allGlyphsAddedToTexture;
        }

        void SetupNewAtlasTexture()
        {
            m_AtlasTextureIndex += 1;

            // Check size of atlas texture array
            if (m_AtlasTextures.Length == m_AtlasTextureIndex)
                Array.Resize(ref m_AtlasTextures, m_AtlasTextures.Length * 2);

            // Initialize new atlas texture
            TextureFormat texFormat = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_COLOR) == GlyphRasterModes.RASTER_MODE_COLOR ? TextureFormat.RGBA32 : TextureFormat.Alpha8;
            m_AtlasTextures[m_AtlasTextureIndex] = new Texture2D(m_AtlasWidth, m_AtlasHeight, texFormat, false);
            m_AtlasTextures[m_AtlasTextureIndex].hideFlags = m_AtlasTextures[0].hideFlags;
            FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);

            // Clear packing GlyphRects
            int packingModifier = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
            m_FreeGlyphRects.Clear();
            m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
            m_UsedGlyphRects.Clear();

            // Add new texture as sub asset to font asset
            Texture2D tex = m_AtlasTextures[m_AtlasTextureIndex];
            tex.name = atlasTexture.name + " " + m_AtlasTextureIndex;

            OnFontAssetTextureChanged?.Invoke(tex, this);
        }

        Character CreateCharacterAndAddToCache(uint unicode, Glyph glyph, FontStyles fontStyle, TextFontWeight fontWeight)
        {
            Character character;
            // We want to make sure we only add the character to the CharacterTable once
            if (!m_CharacterLookupDictionary.TryGetValue(unicode, out character))
            {
                character = new Character(unicode, this, glyph);
                m_CharacterTable.Add(character);
                AddCharacterToLookupCache(unicode, character, FontStyles.Normal, TextFontWeight.Regular);
            }

            if (fontStyle != FontStyles.Normal || fontWeight != TextFontWeight.Regular)
                AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight);

            return character;
        }
    }
}

#pragma warning restore CS0618
