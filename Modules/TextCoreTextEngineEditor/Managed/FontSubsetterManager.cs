// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace UnityEditor.TextCore.Text
{
    [Flags]
    internal enum FontSubsetFlags : uint
    {
        None = 0x0,
        NoHinting = 0x1,
    }

    // Writes subset recipes onto the source font's importer and repoints the FontAsset at the hidden subset sub-asset.
    internal static class FontSubsetterManager
    {
        internal const string NoSourceFontImporterMessage =
            "The source font is not a font file in the project, so it can't be subset (system, package, and " +
            "built-in fonts are read-only).";

        internal struct SubsetRecipe
        {
            public string key; // GUID of the consumer FontAsset
            public string characters;
            public FontSubsetFlags flags;
            public int faceIndex;
        }

        internal static string GetRecipeKey(FontAsset fontAsset)
        {
            string path = AssetDatabase.GetAssetPath(fontAsset);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        internal static bool TryGetSourceFontImporter(FontAsset fontAsset, out TrueTypeFontImporter importer, out string fontPath)
        {
            importer = null;
            fontPath = null;

            var sourceFont = fontAsset.SourceFont_EditorRef;
            if (sourceFont == null)
                return false;

            fontPath = AssetDatabase.GetAssetPath(sourceFont);
            importer = AssetImporter.GetAtPath(fontPath) as TrueTypeFontImporter;
            return importer != null;
        }

        internal static bool TryGetActiveRecipe(FontAsset fontAsset, out SubsetRecipe recipe)
        {
            recipe = default;
            string key = GetRecipeKey(fontAsset);
            if (key == null || !TryGetSourceFontImporter(fontAsset, out var importer, out _))
                return false;

            foreach (var candidate in ReadRecipes(importer))
            {
                if (candidate.key == key)
                {
                    recipe = candidate;
                    return true;
                }
            }
            return false;
        }

        internal static bool ApplySubset(FontAsset fontAsset, string ranges, out string error)
        {
            if (!TryGenerateSubsetFont(fontAsset, ranges, fontAsset.faceInfo.faceIndex, out var subsetFont, out error))
                return false;

            // hb-subset emits a single-face font, so a baked non-zero face index no longer applies.
            RepointSourceFont(fontAsset, subsetFont, faceIndex: 0);
            return true;
        }

        // Same as ApplySubset but keeps the baked glyph table and atlas (valid since the subsetter retains glyph ids).
        internal static bool ApplySubsetKeepingBakedData(FontAsset fontAsset, string ranges, out string error)
        {
            if (!TryGenerateSubsetFont(fontAsset, ranges, fontAsset.faceInfo.faceIndex, out var subsetFont, out error))
                return false;

            fontAsset.m_FaceInfo.faceIndex = 0;
            fontAsset.sourceFontFile = subsetFont;
            fontAsset.UpdateSourceFontFile();
            fontAsset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
            return true;
        }

        internal static void RemoveSubset(FontAsset fontAsset)
        {
            int faceIndex = fontAsset.faceInfo.faceIndex;
            string key = GetRecipeKey(fontAsset);
            if (key != null && TryGetSourceFontImporter(fontAsset, out var importer, out _))
            {
                var recipes = ReadRecipes(importer);
                int index = recipes.FindIndex(r => r.key == key);
                if (index >= 0)
                {
                    faceIndex = recipes[index].faceIndex;
                    recipes.RemoveAt(index);
                    WriteRecipes(importer, recipes);
                    importer.SaveAndReimport();
                }
            }

            RepointSourceFont(fontAsset, fontAsset.SourceFont_EditorRef, faceIndex);
        }

        static void PruneUnusedRecipes(List<SubsetRecipe> recipes)
        {
            for (int i = recipes.Count - 1; i >= 0; i--)
            {
                // GUIDToAssetPath still returns a path for deleted assets, so probe the asset itself.
                string path = AssetDatabase.GUIDToAssetPath(recipes[i].key);
                if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<FontAsset>(path) == null)
                    recipes.RemoveAt(i);
            }
        }

        internal static Font ResolveDynamicSourceFont(FontAsset fontAsset)
        {
            string key = GetRecipeKey(fontAsset);
            if (key != null && TryGetSourceFontImporter(fontAsset, out var importer, out string fontPath)
                && Array.IndexOf(importer.GetSubsetRecipeKeys(), key) >= 0)
            {
                var subsetFont = FindSubsetFont(fontPath, key);
                if (subsetFont != null)
                    return subsetFont;
            }

            return fontAsset.SourceFont_EditorRef;
        }

        internal static long GetSubsetSizePreview(FontAsset fontAsset, string ranges, int faceIndex)
        {
            if (string.IsNullOrEmpty(ranges) || !TryGetSourceFontImporter(fontAsset, out _, out string fontPath))
                return 0;

            return (long)TrueTypeFontImporter.GetSubsetFontSize(fontPath, faceIndex, ranges, (uint)HintingFlags(fontAsset));
        }

        // hb-subset silently drops requested code points the font has no glyph for; this reports them.
        internal static string GetMissingCodePoints(FontAsset fontAsset, string ranges, int faceIndex)
        {
            var sourceFont = fontAsset.SourceFont_EditorRef;
            if (sourceFont == null || string.IsNullOrEmpty(ranges))
                return string.Empty;

            if (FontEngine.LoadFontFace(sourceFont, 90, faceIndex, out FontFaceHandle faceHandle) != FontEngineError.Success)
                return string.Empty;

            try
            {
                var missing = new SortedSet<uint>();
                foreach (uint c in UnicodeRanges.EnumerateCodePoints(ranges))
                {
                    if (!FontEngine.TryGetGlyphIndex(faceHandle, c, out _))
                        missing.Add(c);
                }
                return missing.Count == 0 ? string.Empty : UnicodeRanges.FromCodePoints(missing);
            }
            finally
            {
                FontEngine.UnloadFontFace(faceHandle);
            }
        }

        static bool TryGenerateSubsetFont(FontAsset fontAsset, string ranges, int faceIndex, out Font subsetFont, out string error)
        {
            subsetFont = null;
            error = null;
            FontSubsetFlags flags = HintingFlags(fontAsset);

            if (string.IsNullOrEmpty(ranges))
            {
                error = "The character set is empty.";
                return false;
            }

            string key = GetRecipeKey(fontAsset);
            if (key == null)
            {
                error = "The font asset is not saved in the project.";
                return false;
            }

            if (!TryGetSourceFontImporter(fontAsset, out var importer, out string fontPath))
            {
                error = NoSourceFontImporterMessage;
                return false;
            }

            var recipes = ReadRecipes(importer);
            PruneUnusedRecipes(recipes);

            var recipe = new SubsetRecipe
            {
                key = key,
                characters = ranges,
                flags = flags,
                faceIndex = faceIndex,
            };
            int index = recipes.FindIndex(r => r.key == key);
            if (index >= 0)
                recipes[index] = recipe;
            else
                recipes.Add(recipe);

            WriteRecipes(importer, recipes);
            importer.SaveAndReimport();

            subsetFont = FindSubsetFont(fontPath, key);
            if (subsetFont == null)
            {
                error = "The subset font could not be generated. See the Console for import warnings.";
                return false;
            }
            return true;
        }

        const GlyphRasterModes k_SdfRasterModes = GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_SDFAA
            | GlyphRasterModes.RASTER_MODE_MSDF | GlyphRasterModes.RASTER_MODE_MSDFA;

        // SDF rendering never executes hinting instructions, so they are dead weight in the subset.
        static FontSubsetFlags HintingFlags(FontAsset fontAsset)
        {
            var rasterModes = (GlyphRasterModes)fontAsset.atlasRenderMode;
            bool sdf = (rasterModes & k_SdfRasterModes) != 0;
            bool hinted = (rasterModes & GlyphRasterModes.RASTER_MODE_HINTED) != 0;
            return sdf && !hinted ? FontSubsetFlags.NoHinting : FontSubsetFlags.None;
        }

        static List<SubsetRecipe> ReadRecipes(TrueTypeFontImporter importer)
        {
            var keys = importer.GetSubsetRecipeKeys();
            var characters = importer.GetSubsetRecipeCharacters();
            var flags = importer.GetSubsetRecipeFlags();
            var faceIndices = importer.GetSubsetRecipeFaceIndices();

            var recipes = new List<SubsetRecipe>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                recipes.Add(new SubsetRecipe
                {
                    key = keys[i],
                    characters = characters[i],
                    flags = (FontSubsetFlags)flags[i],
                    faceIndex = faceIndices[i],
                });
            }
            return recipes;
        }

        static void WriteRecipes(TrueTypeFontImporter importer, List<SubsetRecipe> recipes)
        {
            int count = recipes.Count;
            var keys = new string[count];
            var characters = new string[count];
            var flags = new int[count];
            var faceIndices = new int[count];

            for (int i = 0; i < count; i++)
            {
                keys[i] = recipes[i].key;
                characters[i] = recipes[i].characters;
                flags[i] = (int)recipes[i].flags;
                faceIndices[i] = recipes[i].faceIndex;
            }

            importer.SetSubsetRecipes(keys, characters, flags, faceIndices);
        }

        internal static Font FindSubsetFont(string fontPath, string key)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fontPath))
            {
                if (asset is Font font && font.subsetKey == key)
                    return font;
            }
            return null;
        }

        static void RepointSourceFont(FontAsset fontAsset, Font font, int faceIndex)
        {
            // Face index first: UpdateSourceFontFile rebuilds the native shaping state with it.
            fontAsset.m_FaceInfo.faceIndex = faceIndex;
            fontAsset.sourceFontFile = font;
            fontAsset.UpdateSourceFontFile();
            fontAsset.ClearFontAssetData();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
        }

        internal static bool IsSubsetActive(FontAsset fontAsset)
        {
            var source = fontAsset.sourceFontFile;
            return source != null && !string.IsNullOrEmpty(source.subsetKey) && source.subsetKey == GetRecipeKey(fontAsset);
        }
    }
}
