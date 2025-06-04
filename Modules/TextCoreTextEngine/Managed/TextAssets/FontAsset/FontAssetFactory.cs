// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text;
#nullable enable

internal class FontAssetFactory
{
    internal const GlyphRenderMode k_SmoothEditorBitmapGlyphRenderMode = GlyphRenderMode.SMOOTH_HINTED;
    internal const GlyphRenderMode k_RasterEditorBitmapGlyphRenderMode = GlyphRenderMode.RASTER_HINTED;
    static readonly HashSet<FontAsset> visitedFontAssets = new();

    public static FontAsset? CloneFontAssetWithBitmapRendering(FontAsset baseFontAsset, int fontSize, bool isRaster)
    {
        visitedFontAssets.Clear();
        return CloneFontAssetWithBitmapRenderingInternal(baseFontAsset, fontSize, isRaster);
    }

    static FontAsset? CloneFontAssetWithBitmapRenderingInternal(FontAsset baseFontAsset, int fontSize, bool isRaster)
    {
        visitedFontAssets.Add(baseFontAsset);
        FontAsset? resultFontAsset = CloneFontAssetWithBitmapSettings(baseFontAsset, fontSize, isRaster);

        if (resultFontAsset != null)
        {
            ProcessFontWeights(resultFontAsset, baseFontAsset, fontSize, isRaster);
            ProcessFallbackFonts(resultFontAsset, baseFontAsset, fontSize, isRaster);
        }

        return resultFontAsset;
    }

    static FontAsset? CloneFontAssetWithBitmapSettings(FontAsset source, int size, bool isRaster)
    {
        bool shouldInstantiate = source.atlasRenderMode != GlyphRenderMode.SDFAA || !source.IsEditorFont || source.sourceFontFile == null;
        FontAsset? newFontAsset;

        if (source.atlasPopulationMode == AtlasPopulationMode.DynamicOS)
        {
            newFontAsset = FontAsset.CreateFontAsset(source.faceInfo.familyName, source.faceInfo.styleName, size, 6, isRaster ? k_RasterEditorBitmapGlyphRenderMode : k_SmoothEditorBitmapGlyphRenderMode);
            if(newFontAsset != null) SetupFontAssetForBitmapSettings(newFontAsset);
        }
        else if (shouldInstantiate) // Color Glyph or Empty Container
        {
            newFontAsset = Object.Instantiate(source);
            if (newFontAsset != null)
            {
                newFontAsset.fallbackFontAssetTable = new List<FontAsset>();
                newFontAsset.m_IsClone = true;
                newFontAsset.IsEditorFont = true;
                SetHideFlags(newFontAsset);
            }
        }
        else
        {
            newFontAsset = FontAsset.CreateFontAsset(source.sourceFontFile, size, 6, isRaster ? k_RasterEditorBitmapGlyphRenderMode : k_SmoothEditorBitmapGlyphRenderMode, source.atlasWidth, source.atlasHeight);
            if (newFontAsset != null) SetupFontAssetForBitmapSettings(newFontAsset);
        }

        return newFontAsset;
    }

    static void ProcessFontWeights(FontAsset resultFontAsset, FontAsset baseFontAsset, int fontSize, bool isRaster)
    {
        for (int i = 0; i < baseFontAsset.fontWeightTable.Length; i++)
        {
            var fontWeight = baseFontAsset.fontWeightTable[i];
            if (fontWeight.regularTypeface != null)
            {
                resultFontAsset.fontWeightTable[i].regularTypeface = CloneFontAssetWithBitmapSettings(fontWeight.regularTypeface, fontSize, isRaster);
            }
            if (fontWeight.italicTypeface != null)
            {
                resultFontAsset.fontWeightTable[i].italicTypeface = CloneFontAssetWithBitmapSettings(fontWeight.italicTypeface, fontSize, isRaster);
            }
        }
    }

    static void ProcessFallbackFonts(FontAsset resultFontAsset, FontAsset baseFontAsset, int fontSize, bool isRaster)
    {
        if (baseFontAsset.fallbackFontAssetTable == null)
            return;

        foreach (var fallbackFontAsset in baseFontAsset.fallbackFontAssetTable)
        {
            if (fallbackFontAsset != null && !visitedFontAssets.Contains(fallbackFontAsset))
            {
                visitedFontAssets.Add(fallbackFontAsset);
                resultFontAsset.fallbackFontAssetTable ??= new List<FontAsset>();
                var newFallbackFontAsset = CloneFontAssetWithBitmapRenderingInternal(fallbackFontAsset, fontSize, isRaster);
                if (newFallbackFontAsset)
                    resultFontAsset.fallbackFontAssetTable.Add(newFallbackFontAsset);
            }
        }
    }

    internal static FontAsset? ConvertFontToFontAsset(Font font)
    {
        if(font == null)
            return null;

        FontAsset? fontAsset = null;

        fontAsset = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.DEFAULT, 1024, 1024, AtlasPopulationMode.Dynamic, true);

        if (fontAsset != null)
            SetupFontAssetSettings(fontAsset);

        return fontAsset;
    }

    internal static void SetupFontAssetSettings(FontAsset fontAsset)
    {
        if (!fontAsset)
            return;

        SetHideFlags(fontAsset);
        fontAsset.isMultiAtlasTexturesEnabled = true;
        fontAsset.IsEditorFont = true;
    }

    static void SetupFontAssetForBitmapSettings(FontAsset fontAsset)
    {
        if (!fontAsset)
            return;

        SetHideFlags(fontAsset);

        fontAsset.IsEditorFont = true;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        // When the checkerboard switch is enabled, the font atlas texture should be bilinear filtered so that we can see the pattern being blurrred when not perfectly aligned.
        // The text could be rendered with point filtering all the time for bitmap fonts otherwise.

        fontAsset.atlasTexture.filterMode = TextGenerator.EnableCheckerboardPattern ? FilterMode.Bilinear : FilterMode.Point;
    }

    public static void SetHideFlags(FontAsset fontAsset)
    {
        if (!fontAsset)
            return;

        fontAsset.hideFlags = HideFlags.DontSave;
        fontAsset.atlasTextures[0].hideFlags = HideFlags.DontSave;
        fontAsset.material.hideFlags = HideFlags.DontSave;
    }
}


