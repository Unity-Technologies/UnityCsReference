// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.TextCore
{
    static class FontUtilities
    {
        /// <summary>
        /// List containing instance ID of font assets already searched.
        /// </summary>
        static List<int> s_SearchedFontAssets;

        internal static Character GetCharacterFromFontAsset(uint unicode, FontAsset sourceFontAsset, bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface, out FontAsset fontAsset)
        {
            if (includeFallbacks)
            {
                if (s_SearchedFontAssets == null)
                    s_SearchedFontAssets = new List<int>();
                else
                    s_SearchedFontAssets.Clear();
            }

            return GetCharacterFromFontAsset_Internal(unicode, sourceFontAsset, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, out fontAsset);
        }

        internal static Character GetCharacterFromFontAssets(uint unicode, List<FontAsset> fontAssets, bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface, out FontAsset fontAsset)
        {
            isAlternativeTypeface = false;

            // Make sure font asset list is valid
            if (fontAssets == null || fontAssets.Count == 0)
            {
                fontAsset = null;
                return null;
            }

            if (includeFallbacks)
            {
                if (s_SearchedFontAssets == null)
                    s_SearchedFontAssets = new List<int>();
                else
                    s_SearchedFontAssets.Clear();
            }

            int fontAssetCount = fontAssets.Count;

            for (int i = 0; i < fontAssetCount; i++)
            {
                if (fontAssets[i] == null) continue;

                Character characterData = GetCharacterFromFontAsset_Internal(unicode, fontAssets[i], includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, out fontAsset);

                if (characterData != null)
                    return characterData;
            }

            fontAsset = null;

            return null;
        }

        static Character GetCharacterFromFontAsset_Internal(uint unicode, FontAsset sourceFontAsset, bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface, out FontAsset fontAsset)
        {
            fontAsset = null;
            isAlternativeTypeface = false;
            Character characterData = null;

            #region FONT WEIGHT AND FONT STYLE HANDLING
            // Determine if a font weight or style is used. If so check if an alternative typeface is assigned for the given weight and / or style.
            bool isItalic = (fontStyle & FontStyles.Italic) == FontStyles.Italic;

            if (isItalic || fontWeight != FontWeight.Regular)
            {
                // Get reference to the font weight pairs of the given font asset.
                FontWeights[] fontWeights = sourceFontAsset.fontWeightTable;

                switch (fontWeight)
                {
                    case FontWeight.Thin:
                        fontAsset = isItalic ? fontWeights[1].italicTypeface : fontWeights[1].regularTypeface;
                        break;
                    case FontWeight.ExtraLight:
                        fontAsset = isItalic ? fontWeights[2].italicTypeface : fontWeights[2].regularTypeface;
                        break;
                    case FontWeight.Light:
                        fontAsset = isItalic ? fontWeights[3].italicTypeface : fontWeights[3].regularTypeface;
                        break;
                    case FontWeight.Regular:
                        fontAsset = isItalic ? fontWeights[4].italicTypeface : fontWeights[4].regularTypeface;
                        break;
                    case FontWeight.Medium:
                        fontAsset = isItalic ? fontWeights[5].italicTypeface : fontWeights[5].regularTypeface;
                        break;
                    case FontWeight.SemiBold:
                        fontAsset = isItalic ? fontWeights[6].italicTypeface : fontWeights[6].regularTypeface;
                        break;
                    case FontWeight.Bold:
                        fontAsset = isItalic ? fontWeights[7].italicTypeface : fontWeights[7].regularTypeface;
                        break;
                    case FontWeight.Heavy:
                        fontAsset = isItalic ? fontWeights[8].italicTypeface : fontWeights[8].regularTypeface;
                        break;
                    case FontWeight.Black:
                        fontAsset = isItalic ? fontWeights[9].italicTypeface : fontWeights[9].regularTypeface;
                        break;
                }

                if (fontAsset != null)
                {
                    if (fontAsset.characterLookupTable.TryGetValue(unicode, out characterData))
                    {
                        isAlternativeTypeface = true;

                        return characterData;
                    }
                    else if (fontAsset.atlasPopulationMode == FontAsset.AtlasPopulationMode.Dynamic)
                    {
                        if (fontAsset.TryAddCharacter(unicode, out characterData))
                        {
                            isAlternativeTypeface = true;

                            return characterData;
                        }

                        // If we find the requested character, we add it to the font asset character table
                        // and return its character data.
                        // We also add this character to the list of characters we will need to add to the font atlas.
                        // We assume the font atlas has room otherwise this font asset should not be marked as dynamic.
                        // Alternatively, we could also add multiple pages of font atlas textures (feature consideration).
                    }

                    // At this point, we were not able to find the requested character in the alternative typeface
                    // so we check the source font asset and its potential fallbacks.
                }
            }
            #endregion

            // Search the source font asset for the requested character.
            if (sourceFontAsset.characterLookupTable.TryGetValue(unicode, out characterData))
            {
                // We were able to locate the requested character in the given font asset.
                fontAsset = sourceFontAsset;

                return characterData;
            }
            else if (sourceFontAsset.atlasPopulationMode == FontAsset.AtlasPopulationMode.Dynamic)
            {
                if (sourceFontAsset.TryAddCharacter(unicode, out characterData))
                {
                    fontAsset = sourceFontAsset;

                    return characterData;
                }

                // If we find the requested character, we add it to the font asset character table
                // and return its character data.
                // We also add this character to the list of characters we will need to add to the font atlas.
                // We assume the font atlas has room otherwise this font asset should not be marked as dynamic.
                // Alternatively, we could also add multiple pages of font atlas textures (feature consideration)
            }

            // Search fallback font assets if we still don't have a valid character and include fallback is set to true.
            if (characterData == null && includeFallbacks && sourceFontAsset.fallbackFontAssetTable != null)
            {
                // Get reference to the list of fallback font assets.
                List<FontAsset> fallbackFontAssets = sourceFontAsset.fallbackFontAssetTable;
                int fallbackCount = fallbackFontAssets.Count;

                if (fallbackFontAssets != null && fallbackCount > 0)
                {
                    for (int i = 0; i < fallbackCount && characterData == null; i++)
                    {
                        FontAsset temp = fallbackFontAssets[i];

                        if (temp == null) continue;

                        int id = temp.GetInstanceID();

                        // Skip over the fallback font asset in the event it is null or if already searched.
                        if (s_SearchedFontAssets.Contains(id))
                            continue;

                        // Add to list of font assets already searched.
                        s_SearchedFontAssets.Add(id);

                        characterData = GetCharacterFromFontAsset_Internal(unicode, temp, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface, out fontAsset);

                        if (characterData != null)
                        {
                            return characterData;
                        }
                    }
                }
            }

            return null;
        }
    }
}
