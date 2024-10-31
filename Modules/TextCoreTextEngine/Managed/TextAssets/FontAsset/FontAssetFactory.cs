// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text;
#nullable enable

internal class FontAssetFactory
{
    const GlyphRenderMode k_DefaultEditorBitmapGlyphRenderMode = GlyphRenderMode.SMOOTH_HINTED;
    static readonly HashSet<FontAsset> visitedFontAssets = new();

    public static FontAsset? CloneFontAssetWithBitmapRendering(FontAsset baseFontAsset, float fontSize)
    {
        visitedFontAssets.Clear();
        return CloneFontAssetWithBitmapRenderingInternal(baseFontAsset, fontSize);
    }

    static FontAsset? CloneFontAssetWithBitmapRenderingInternal(FontAsset baseFontAsset, float fontSize)
    {
        visitedFontAssets.Add(baseFontAsset);
        FontAsset? resultFontAsset = CloneFontAssetWithBitmapSettings(baseFontAsset, fontSize);

        if (resultFontAsset != null)
        {
            ProcessFontWeights(resultFontAsset, baseFontAsset, fontSize);
            ProcessFallbackFonts(resultFontAsset, baseFontAsset, fontSize);
        }

        return resultFontAsset;
    }

    static FontAsset? CloneFontAssetWithBitmapSettings(FontAsset source, float size)
    {
        bool shouldInstantiate = source.atlasRenderMode != GlyphRenderMode.SDFAA || !source.IsEditorFont || source.sourceFontFile == null;
        FontAsset? newFontAsset;

        if (source.atlasPopulationMode == AtlasPopulationMode.DynamicOS)
        {
            newFontAsset = FontAsset.CreateFontAsset(source.faceInfo.familyName, source.faceInfo.styleName, size, 6, k_DefaultEditorBitmapGlyphRenderMode);
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
            newFontAsset = FontAsset.CreateFontAsset(source.sourceFontFile, size, 6, k_DefaultEditorBitmapGlyphRenderMode, source.atlasWidth, source.atlasHeight);
            if (newFontAsset != null) SetupFontAssetForBitmapSettings(newFontAsset);
        }

        return newFontAsset;
    }

    static void ProcessFontWeights(FontAsset resultFontAsset, FontAsset baseFontAsset, float fontSize)
    {
        for (int i = 0; i < baseFontAsset.fontWeightTable.Length; i++)
        {
            var fontWeight = baseFontAsset.fontWeightTable[i];
            if (fontWeight.regularTypeface != null)
            {
                resultFontAsset.fontWeightTable[i].regularTypeface = CloneFontAssetWithBitmapSettings(fontWeight.regularTypeface, fontSize);
            }
            if (fontWeight.italicTypeface != null)
            {
                resultFontAsset.fontWeightTable[i].italicTypeface = CloneFontAssetWithBitmapSettings(fontWeight.italicTypeface, fontSize);
            }
        }
    }

    static void ProcessFallbackFonts(FontAsset resultFontAsset, FontAsset baseFontAsset, float fontSize)
    {
        if (baseFontAsset.fallbackFontAssetTable == null)
            return;

        foreach (var fallbackFontAsset in baseFontAsset.fallbackFontAssetTable)
        {
            if (fallbackFontAsset != null && !visitedFontAssets.Contains(fallbackFontAsset))
            {
                visitedFontAssets.Add(fallbackFontAsset);
                resultFontAsset.fallbackFontAssetTable ??= new List<FontAsset>();
                var newFallbackFontAsset = CloneFontAssetWithBitmapRenderingInternal(fallbackFontAsset, fontSize);
                if (newFallbackFontAsset)
                    resultFontAsset.fallbackFontAssetTable.Add(newFallbackFontAsset);
            }
        }
    }

        static readonly string k_SystemFontName =
            "Lucida Grande"
        ;
    internal static FontAsset? CreateDefaultEditorFontAsset(Font font, Shader shader)
    {
        if(font == null)
            return null;

        FontAsset? fontAsset=null;
        if (font.name == "System Normal" || font.name == "System Small" || font.name == "System Big" || font.name == "System Warning")
        {
            fontAsset = FontAsset.CreateFontAssetInternal(k_SystemFontName, "Regular", 90);
            if (fontAsset != null)
            {
                fontAsset.InternalDynamicOS = true;

                var boldFontAsset = FontAsset.CreateFontAssetInternal(k_SystemFontName, "Bold", 90);
                if (boldFontAsset != null)
                {
                    boldFontAsset.InternalDynamicOS = true;
                    fontAsset.fontWeightTable[7].regularTypeface = boldFontAsset;
                    SetupFontAssetSettings(boldFontAsset, shader);
                }

                var boldItalicFontAsset = FontAsset.CreateFontAssetInternal(k_SystemFontName, "Bold Italic", 90);
                if (boldItalicFontAsset != null)
                {
                    boldItalicFontAsset.InternalDynamicOS = true;
                    fontAsset.fontWeightTable[7].italicTypeface = boldItalicFontAsset;
                    SetupFontAssetSettings(boldItalicFontAsset, shader);
                }

                var italicFontAsset = FontAsset.CreateFontAssetInternal(k_SystemFontName, "Italic", 90);
                if (italicFontAsset != null)
                {
                    italicFontAsset.InternalDynamicOS = true;
                    fontAsset.fontWeightTable[4].italicTypeface = italicFontAsset;
                    SetupFontAssetSettings(italicFontAsset, shader);
                }
            }
        }
        else if (font.name == "System Normal Bold" || font.name == "System Small Bold")
        {
            fontAsset = FontAsset.CreateFontAssetInternal(k_SystemFontName, "Bold", 90);
            if (fontAsset != null)
            {
                fontAsset.InternalDynamicOS = true;
            }
        }
        else if (font.name == "Inter-Regular")
        {
            fontAsset = FontAsset.CreateFontAssetInternal("Inter", "Regular", 90);
            if (fontAsset != null)
            {
                fontAsset.InternalDynamicOS = true;

                var boldFontAsset = FontAsset.CreateFontAssetInternal("Inter", "Bold", 90);
                if (boldFontAsset != null)
                {
                    boldFontAsset.InternalDynamicOS = true;
                    fontAsset.fontWeightTable[7].regularTypeface = boldFontAsset;
                    SetupFontAssetSettings(boldFontAsset, shader);
                }

                var italicFontAsset = FontAsset.CreateFontAssetInternal("Inter", "Italic", 90);
                if (italicFontAsset != null)
                {
                    italicFontAsset.InternalDynamicOS = true;
                    fontAsset.fontWeightTable[4].italicTypeface = italicFontAsset;
                    SetupFontAssetSettings(italicFontAsset, shader);
                }

                var boldItalicFontAsset = FontAsset.CreateFontAssetInternal("Inter", "Bold Italic", 90);
                if (boldItalicFontAsset != null)
                {
                    boldItalicFontAsset.InternalDynamicOS = true;
                    fontAsset.fontWeightTable[7].italicTypeface = boldItalicFontAsset;
                    SetupFontAssetSettings(boldItalicFontAsset, shader);
                }
            }
        }
        else
        {
            fontAsset = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, shader, AtlasPopulationMode.Dynamic, true);
        }

        if (fontAsset != null)
            SetupFontAssetSettings(fontAsset, shader);

        return fontAsset;
    }

    static void SetupFontAssetSettings(FontAsset fontAsset, Shader shader)
    {
        if (!fontAsset)
            return;

        SetHideFlags(fontAsset);
        fontAsset.material.shader = shader;
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


