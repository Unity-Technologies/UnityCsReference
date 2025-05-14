// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.TextCore.Text
{
    internal static class FontAssetUtilities
    {
        /// <summary>
        /// HashSet containing instance ID of font assets already searched.
        /// </summary>
        static HashSet<int> k_SearchedAssets;

        /// <summary>
        /// Returns the text element (character) for the given unicode value taking into consideration the requested font style and weight.
        /// Function searches the source font asset, its list of font assets assigned as alternative typefaces and potentially its fallbacks.
        /// The font asset out parameter contains a reference to the font asset containing the character.
        /// The typeface type indicates whether the returned font asset is the source font asset, an alternative typeface or fallback font asset.
        /// </summary>
        /// <param name="unicode">The unicode value of the requested character</param>
        /// <param name="sourceFontAsset">The font asset to be searched</param>
        /// <param name="includeFallbacks">Include the fallback font assets in the search</param>
        /// <param name="fontStyle">The font style</param>
        /// <param name="fontWeight">The font weight</param>
        /// <param name="isAlternativeTypeface">Indicates if the OUT font asset is an alternative typeface or fallback font asset</param>
        /// <param name="fontAsset">The font asset that contains the requested character</param>
        /// <returns></returns>
        internal static Character GetCharacterFromFontAsset(uint unicode, FontAsset sourceFontAsset, bool includeFallbacks, FontStyles fontStyle, TextFontWeight fontWeight, out bool isAlternativeTypeface, bool populateLigatures)
        {
            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            return GetCharacterFromFontAsset_Internal(unicode, sourceFontAsset, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, populateLigatures);
        }

        /// <summary>
        /// Internal function returning the text element character for the given unicode value taking into consideration the font style and weight.
        /// Function searches the source font asset, list of font assets assigned as alternative typefaces and list of fallback font assets.
        /// </summary>
        private static Character GetCharacterFromFontAsset_Internal(uint unicode, FontAsset sourceFontAsset, bool includeFallbacks, FontStyles fontStyle, TextFontWeight fontWeight, out bool isAlternativeTypeface, bool populateLigatures)
        {
            bool canWriteOnAsset = !TextGenerator.IsExecutingJob;
            isAlternativeTypeface = false;
            Character character = null;

            #region FONT WEIGHT AND FONT STYLE HANDLING
            // Determine if a font weight or style is used. If so check if an alternative typeface is assigned for the given weight and / or style.
            bool isItalic = (fontStyle & FontStyles.Italic) == FontStyles.Italic;

            if (isItalic || fontWeight != TextFontWeight.Regular)
            {
                // Get reference to the font weight pairs of the given font asset.
                FontWeightPair[] fontWeights = sourceFontAsset.fontWeightTable;

                int fontWeightIndex = TextUtilities.GetTextFontWeightIndex(fontWeight);

                FontAsset temp = isItalic ? fontWeights[fontWeightIndex].italicTypeface : fontWeights[fontWeightIndex].regularTypeface;

                if (temp != null)
                {
                    if (!canWriteOnAsset && temp.m_CharacterLookupDictionary == null)
                        return null;

                    if (temp.GetCharacterInLookupCache(unicode, fontStyle, fontWeight, out character))
                    {
                        if (character.textAsset != null)
                        {
                            isAlternativeTypeface = true;
                            return character;
                        }

                        if (!canWriteOnAsset)
                            return null;

                        // Remove character from lookup table
                        temp.RemoveCharacterInLookupCache(unicode, fontStyle, fontWeight);
                    }

                    if (temp.atlasPopulationMode == AtlasPopulationMode.Dynamic || temp.atlasPopulationMode == AtlasPopulationMode.DynamicOS)
                    {
                        if (!canWriteOnAsset)
                        {
                            if (!temp.m_MissingUnicodesFromFontFile.Contains(unicode))
                                return null;
                        }


                        else if (temp.TryAddCharacterInternal(unicode, fontStyle, fontWeight, out character, populateLigatures))
                        {
                            isAlternativeTypeface = true;
                            return character;
                        }

                        // Check if the source font file contains the requested character.
                        //if (TryGetCharacterFromFontFile(unicode, fontAsset, out characterData))
                        //{
                        //    isAlternativeTypeface = true;

                        //    return characterData;
                        //}

                        // If we find the requested character, we add it to the font asset character table
                        // and return its character data.
                        // We also add this character to the list of characters we will need to add to the font atlas.
                        // We assume the font atlas has room otherwise this font asset should not be marked as dynamic.
                        // Alternatively, we could also add multiple pages of font atlas textures (feature consideration).
                    }
                    // Search the source font asset for the requested character, removing bold and italic status
                    else if (temp.GetCharacterInLookupCache(unicode, FontStyles.Normal, TextFontWeight.Regular, out character))
                    {
                        if (character.textAsset != null)
                        {
                            isAlternativeTypeface = true;
                            return character;
                        }

                        if (!canWriteOnAsset)
                            return null;

                        temp.RemoveCharacterInLookupCache(unicode, fontStyle, fontWeight);
                    }

                    // At this point, we were not able to find the requested character in the alternative typeface
                    // so we check the source font asset and its potential fallbacks.
                }
            }
            #endregion

            if (!canWriteOnAsset && sourceFontAsset.m_CharacterLookupDictionary == null)
                return null;

            // Search the source font asset for the requested character.
            if (sourceFontAsset.GetCharacterInLookupCache(unicode, fontStyle, fontWeight, out character))
            {
                if (character.textAsset != null)
                    return character;

                if (!canWriteOnAsset)
                    return null;

                sourceFontAsset.RemoveCharacterInLookupCache(unicode, fontStyle, fontWeight);
            }

            if (sourceFontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic || sourceFontAsset.atlasPopulationMode == AtlasPopulationMode.DynamicOS)
            {
                if (!canWriteOnAsset)
                    return null;

                if (sourceFontAsset.TryAddCharacterInternal(unicode, fontStyle, fontWeight, out character, populateLigatures))
                    return character;
            }
            // Search the source font asset for the requested character, removing bold and italic status
            else if (sourceFontAsset.GetCharacterInLookupCache(unicode, FontStyles.Normal, TextFontWeight.Regular, out character))
            {
                if (character.textAsset != null)
                    return character;

                if (!canWriteOnAsset)
                    return null;

                sourceFontAsset.RemoveCharacterInLookupCache(unicode, fontStyle, fontWeight);
            }

            if (character == null && !canWriteOnAsset)
                return null;

            // Search fallback font assets if we still don't have a valid character and include fallback is set to true.
            if (character == null && includeFallbacks && sourceFontAsset.fallbackFontAssetTable != null)
            {
                // Get reference to the list of fallback font assets.
                List<FontAsset> fallbackFontAssets = sourceFontAsset.fallbackFontAssetTable;
                int fallbackCount = fallbackFontAssets.Count;

                if (fallbackCount == 0)
                    return null;

                for (int i = 0; i < fallbackCount; i++)
                {
                    FontAsset temp = fallbackFontAssets[i];

                    if (temp == null)
                        continue;

                    int id = temp.GetHashCode();

                    // Try adding font asset to search list. If already present skip to the next one otherwise check if it contains the requested character.
                    if (k_SearchedAssets.Add(id) == false)
                        continue;

                    // Add reference to this search query
                    //sourceFontAsset.FallbackSearchQueryLookup.Add(id);

                    character = GetCharacterFromFontAsset_Internal(unicode, temp, true, fontStyle, fontWeight, out isAlternativeTypeface, populateLigatures);

                    if (character != null)
                        return character;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the text element (character) for the given unicode value taking into consideration the requested font style and weight.
        /// Function searches the provided list of font assets, the list of font assets assigned as alternative typefaces to them as well as their fallbacks.
        /// The font asset out parameter contains a reference to the font asset containing the character.
        /// The typeface type indicates whether the returned font asset is the source font asset, an alternative typeface or fallback font asset.
        /// </summary>
        /// <param name="unicode">The unicode value of the requested character</param>
        /// <param name="sourceFontAsset">The font asset originating the search query</param>
        /// <param name="fontAssets">The list of font assets to search</param>
        /// <param name="includeFallbacks">Determines if the fallback of each font assets on the list will be searched</param>
        /// <param name="fontStyle">The font style</param>
        /// <param name="fontWeight">The font weight</param>
        /// <param name="isAlternativeTypeface">Determines if the OUT font asset is an alternative typeface or fallback font asset</param>
        /// <returns></returns>
        public static Character GetCharacterFromFontAssets(uint unicode, FontAsset sourceFontAsset, List<FontAsset> fontAssets, List<FontAsset> OSFallbackList, bool includeFallbacks, FontStyles fontStyle, TextFontWeight fontWeight, out bool isAlternativeTypeface)
        {
            return GetCharacterFromFontAssetsInternal(unicode, sourceFontAsset, fontAssets, OSFallbackList, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface);
        }

        internal static Character GetCharacterFromFontAssetsInternal(uint unicode, FontAsset sourceFontAsset, List<FontAsset> fontAssets, List<FontAsset> OSFallbackList, bool includeFallbacks, FontStyles fontStyle, TextFontWeight fontWeight, out bool isAlternativeTypeface, bool populateLigatures = true)
        {
            isAlternativeTypeface = false;

            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            Character character = GetCharacterFromFontAssetsInternal(unicode, fontAssets, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, populateLigatures);
            if (character != null)
                return character;

            return GetCharacterFromFontAssetsInternal(unicode, OSFallbackList, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, populateLigatures);
        }

        private static Character GetCharacterFromFontAssetsInternal(uint unicode, List<FontAsset> fontAssets, bool includeFallbacks, FontStyles fontStyle, TextFontWeight fontWeight, out bool isAlternativeTypeface, bool populateLigatures = true)
        {
            isAlternativeTypeface = false;

            // Make sure font asset list is valid
            if (fontAssets == null || fontAssets.Count == 0)
                return null;

            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            int fontAssetCount = fontAssets.Count;

            for (int i = 0; i < fontAssetCount; i++)
            {
                FontAsset fontAsset = fontAssets[i];

                if (fontAsset == null) continue;

                // Add reference to this search query
                //sourceFontAsset.FallbackSearchQueryLookup.Add(fontAsset.instanceID);

                Character character = GetCharacterFromFontAsset_Internal(unicode, fontAsset, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, populateLigatures);

                if (character != null)
                    return character;
            }

            return null;
        }


        internal static TextElement GetTextElementFromTextAssets(uint unicode, FontAsset sourceFontAsset, List<TextAsset> textAssets, bool includeFallbacks, FontStyles fontStyle, TextFontWeight fontWeight, out bool isAlternativeTypeface, bool populateLigatures)
        {
            isAlternativeTypeface = false;

            // Make sure font asset list is valid
            if (textAssets == null || textAssets.Count == 0)
                return null;

            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            int textAssetCount = textAssets.Count;

            for (int i = 0; i < textAssetCount; i++)
            {
                TextAsset textAsset = textAssets[i];

                if (textAsset == null) continue;

                if (textAsset.GetType() == typeof(FontAsset))
                {
                    FontAsset fontAsset = textAsset as FontAsset;
                    Character character = GetCharacterFromFontAsset_Internal(unicode, fontAsset, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, populateLigatures);

                    if (character != null)
                        return character;
                }
                else
                {
                    SpriteAsset spriteAsset = textAsset as SpriteAsset;
                    SpriteCharacter spriteCharacter = GetSpriteCharacterFromSpriteAsset_Internal(unicode, spriteAsset, true);

                    if (spriteCharacter != null)
                        return spriteCharacter;
                }
            }

            return null;
        }


        // =====================================================================
        // SPRITE ASSET - Functions
        // =====================================================================

        /// <summary>
        ///
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="spriteAsset"></param>
        /// <param name="includeFallbacks"></param>
        /// <returns></returns>
        public static SpriteCharacter GetSpriteCharacterFromSpriteAsset(uint unicode, SpriteAsset spriteAsset, bool includeFallbacks)
        {
            bool canWriteOnAsset = !TextGenerator.IsExecutingJob;
            // Make sure we have a valid sprite asset to search
            if (spriteAsset == null)
                return null;

            SpriteCharacter spriteCharacter;

            // Search sprite asset for potential sprite character for the given unicode value
            if (spriteAsset.spriteCharacterLookupTable.TryGetValue(unicode, out spriteCharacter))
                return spriteCharacter;

            if (includeFallbacks)
            {
                // Clear searched assets
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();

                // Add current sprite asset to already searched assets.
                k_SearchedAssets.Add(spriteAsset.GetHashCode());

                List<SpriteAsset> fallbackSpriteAsset = spriteAsset.fallbackSpriteAssets;

                if (fallbackSpriteAsset != null && fallbackSpriteAsset.Count > 0)
                {
                    int fallbackCount = fallbackSpriteAsset.Count;

                    for (int i = 0; i < fallbackCount; i++)
                    {
                        SpriteAsset temp = fallbackSpriteAsset[i];

                        if (temp == null)
                            continue;

                        int id = temp.GetHashCode();

                        // Try adding asset to search list. If already present skip to the next one otherwise check if it contains the requested character.
                        if (k_SearchedAssets.Add(id) == false)
                            continue;

                        spriteCharacter = GetSpriteCharacterFromSpriteAsset_Internal(unicode, temp, true);

                        if (spriteCharacter != null)
                            return spriteCharacter;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="spriteAsset"></param>
        /// <param name="includeFallbacks"></param>
        /// <returns></returns>
        static SpriteCharacter GetSpriteCharacterFromSpriteAsset_Internal(uint unicode, SpriteAsset spriteAsset, bool includeFallbacks)
        {
            SpriteCharacter spriteCharacter;

            // Search sprite asset for potential sprite character for the given unicode value
            if (spriteAsset.spriteCharacterLookupTable.TryGetValue(unicode, out spriteCharacter))
                return spriteCharacter;

            if (includeFallbacks)
            {
                List<SpriteAsset> fallbackSpriteAsset = spriteAsset.fallbackSpriteAssets;

                if (fallbackSpriteAsset != null && fallbackSpriteAsset.Count > 0)
                {
                    int fallbackCount = fallbackSpriteAsset.Count;

                    for (int i = 0; i < fallbackCount; i++)
                    {
                        SpriteAsset temp = fallbackSpriteAsset[i];

                        if (temp == null)
                            continue;

                        int id = temp.GetHashCode();

                        // Try adding asset to search list. If already present skip to the next one otherwise check if it contains the requested character.
                        if (k_SearchedAssets.Add(id) == false)
                            continue;

                        spriteCharacter = GetSpriteCharacterFromSpriteAsset_Internal(unicode, temp, true);

                        if (spriteCharacter != null)
                            return spriteCharacter;
                    }
                }
            }

            return null;
        }

        public static uint GetCodePoint(string text, ref int index)
        {
            char c = text[index];
            if (char.IsHighSurrogate(c)
                && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]))
            {
                uint cp = (uint)char.ConvertToUtf32(c, text[index + 1]);
                index++;
                return cp;
            }

            return c;
        }

        public static uint GetCodePoint(uint[] codesPoints, ref int index)
        {
            char c = (char)codesPoints[index];
            if (char.IsHighSurrogate(c)
                && index + 1 < codesPoints.Length
                && char.IsLowSurrogate((char)codesPoints[index + 1]))
            {
                uint cp = (uint)char.ConvertToUtf32(c, (char)codesPoints[index + 1]);
                index++;
                return cp;
            }

            return c;
        }
    }
}
