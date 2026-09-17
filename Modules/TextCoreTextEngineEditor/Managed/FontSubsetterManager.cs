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

        // The primitives below take the consumer asset (recipe key owner) and its editor source
        // font directly, so other font asset types (TMP) can reuse the subsetting machinery.
        internal static string GetRecipeKey(UnityEngine.Object consumerAsset)
        {
            string path = AssetDatabase.GetAssetPath(consumerAsset);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        internal static bool TryGetSourceFontImporter(FontAsset fontAsset, out TrueTypeFontImporter importer, out string fontPath)
        {
            return TryGetSourceFontImporter(fontAsset.SourceFont_EditorRef, out importer, out fontPath);
        }

        internal static bool TryGetSourceFontImporter(Font sourceFontEditorRef, out TrueTypeFontImporter importer, out string fontPath)
        {
            importer = null;
            fontPath = null;

            if (sourceFontEditorRef == null)
                return false;

            fontPath = AssetDatabase.GetAssetPath(sourceFontEditorRef);
            importer = AssetImporter.GetAtPath(fontPath) as TrueTypeFontImporter;
            return importer != null;
        }

        internal static bool TryGetActiveRecipe(FontAsset fontAsset, out SubsetRecipe recipe)
        {
            return TryGetActiveRecipe(fontAsset, fontAsset.SourceFont_EditorRef, out recipe);
        }

        internal static bool TryGetActiveRecipe(UnityEngine.Object consumerAsset, Font sourceFontEditorRef, out SubsetRecipe recipe)
        {
            recipe = default;
            string key = GetRecipeKey(consumerAsset);
            if (key == null || !TryGetSourceFontImporter(sourceFontEditorRef, out var importer, out _))
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
            RemoveRecipe(fontAsset, fontAsset.SourceFont_EditorRef, ref faceIndex);

            RepointSourceFont(fontAsset, fontAsset.SourceFont_EditorRef, faceIndex);
        }

        // Removes the consumer's recipe from the source font importer. When a recipe existed,
        // faceIndex is updated to the face index it had baked before subsetting.
        internal static void RemoveRecipe(UnityEngine.Object consumerAsset, Font sourceFontEditorRef, ref int faceIndex)
        {
            string key = GetRecipeKey(consumerAsset);
            if (key == null || !TryGetSourceFontImporter(sourceFontEditorRef, out var importer, out _))
                return;

            var recipes = ReadRecipes(importer);
            int index = recipes.FindIndex(r => r.key == key);
            if (index < 0)
                return;

            faceIndex = recipes[index].faceIndex;
            recipes.RemoveAt(index);
            WriteRecipes(importer, recipes);
            importer.SaveAndReimport();
        }

        static void PruneUnusedRecipes(List<SubsetRecipe> recipes)
        {
            for (int i = recipes.Count - 1; i >= 0; i--)
            {
                // GUIDToAssetPath still returns a path for deleted assets, so probe the asset itself.
                // Consumers are not necessarily FontAssets (e.g. TMP_FontAsset), so probe as plain Object.
                string path = AssetDatabase.GUIDToAssetPath(recipes[i].key);
                if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
                    recipes.RemoveAt(i);
            }
        }

        internal static Font ResolveDynamicSourceFont(FontAsset fontAsset)
        {
            var subsetFont = ResolveSubsetFont(fontAsset, fontAsset.SourceFont_EditorRef);
            return subsetFont != null ? subsetFont : fontAsset.SourceFont_EditorRef;
        }

        // Returns the subset sub-asset the consumer's recipe produced, or null when none is active.
        internal static Font ResolveSubsetFont(UnityEngine.Object consumerAsset, Font sourceFontEditorRef)
        {
            string key = GetRecipeKey(consumerAsset);
            if (key != null && TryGetSourceFontImporter(sourceFontEditorRef, out var importer, out string fontPath)
                && Array.IndexOf(importer.GetSubsetRecipeKeys(), key) >= 0)
            {
                return FindSubsetFont(fontPath, key);
            }

            return null;
        }

        internal static long GetSubsetSizePreview(FontAsset fontAsset, string ranges, int faceIndex)
        {
            return GetSubsetSizePreview(fontAsset.SourceFont_EditorRef, ranges, faceIndex, HintingFlags(fontAsset));
        }

        internal static long GetSubsetSizePreview(Font sourceFontEditorRef, string ranges, int faceIndex, FontSubsetFlags flags)
        {
            if (string.IsNullOrEmpty(ranges) || !TryGetSourceFontImporter(sourceFontEditorRef, out _, out string fontPath))
                return 0;

            return (long)TrueTypeFontImporter.GetSubsetFontSize(fontPath, faceIndex, ranges, (uint)flags);
        }

        internal static string GetMissingCodePoints(FontAsset fontAsset, string ranges, int faceIndex)
        {
            return GetMissingCodePoints(fontAsset.SourceFont_EditorRef, ranges, faceIndex);
        }

        // hb-subset silently drops requested code points the font has no glyph for; this reports them.
        internal static string GetMissingCodePoints(Font sourceFontEditorRef, string ranges, int faceIndex)
        {
            var sourceFont = sourceFontEditorRef;
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
            return TryGenerateSubsetFont(fontAsset, fontAsset.SourceFont_EditorRef, ranges, faceIndex, HintingFlags(fontAsset), out subsetFont, out error);
        }

        internal static bool TryGenerateSubsetFont(UnityEngine.Object consumerAsset, Font sourceFontEditorRef, string ranges, int faceIndex, FontSubsetFlags flags, out Font subsetFont, out string error)
        {
            subsetFont = null;
            error = null;

            if (string.IsNullOrEmpty(ranges))
            {
                error = "The character set is empty.";
                return false;
            }

            string key = GetRecipeKey(consumerAsset);
            if (key == null)
            {
                error = "The font asset is not saved in the project.";
                return false;
            }

            if (!TryGetSourceFontImporter(sourceFontEditorRef, out var importer, out string fontPath))
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
            {
                // Updating an active subset: the consumer's faceIndex is already 0, so keep the
                // original face baked into the recipe.
                recipe.faceIndex = recipes[index].faceIndex;
                recipes[index] = recipe;
            }
            else
            {
                recipes.Add(recipe);
            }

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

        static FontSubsetFlags HintingFlags(FontAsset fontAsset)
        {
            return HintingFlags(fontAsset.atlasRenderMode);
        }

        // SDF rendering never executes hinting instructions, so they are dead weight in the subset.
        internal static FontSubsetFlags HintingFlags(GlyphRenderMode renderMode)
        {
            var rasterModes = (GlyphRasterModes)renderMode;
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
            return IsSubsetActive(fontAsset, fontAsset.sourceFontFile);
        }

        internal static bool IsSubsetActive(UnityEngine.Object consumerAsset, Font currentSourceFontFile)
        {
            return currentSourceFontFile != null && !string.IsNullOrEmpty(currentSourceFontFile.subsetKey) && currentSourceFontFile.subsetKey == GetRecipeKey(consumerAsset);
        }
    }
}
