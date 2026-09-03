// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

#pragma warning disable CS0618 // FontFeatureTable is obsolete; cleared here as part of leaving the standard pipeline

namespace UnityEditor.TextCore.Text
{
    // Converts a static FontAsset to dynamic in place, backed by a subset of its source font; GUID, atlas, and tables kept.
    internal static class FontAssetStaticMigrator
    {
        internal const string OutOfSyncMessage =
            "The source font has changed since this font asset's atlas was generated, so the baked glyphs no " +
            "longer match it. Re-generate the atlas with the Font Asset Creator before converting.";

        internal const string NoSourceFontMessage =
            "This font asset can't be converted to dynamic because it has no source font file in the project.";

        internal const string StaticNotSupportedMessage =
            "Static font assets aren't supported by the Advanced Text Generator. Converting to dynamic keeps " +
            "the atlas, material, and references, backed by a subset of the source font.";

        public static bool CanMigrate(FontAsset fontAsset, out string reason)
        {
            reason = null;

            if (fontAsset == null)
            {
                reason = "No font asset selected.";
                return false;
            }

            if (!IsStatic(fontAsset))
            {
                reason = "This font asset is already dynamic.";
                return false;
            }

            // Nothing baked means nothing to subset; fallback-container assets convert by the mode switch alone.
            if (fontAsset.m_GlyphTable == null || fontAsset.m_GlyphTable.Count == 0)
                return true;

            if (!FontSubsetterManager.TryGetSourceFontImporter(fontAsset, out _, out _))
            {
                reason = NoSourceFontMessage;
                return false;
            }

            return true;
        }

        internal static bool IsStatic(FontAsset fontAsset)
        {
            return fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic
                && fontAsset.atlasPopulationMode != AtlasPopulationMode.DynamicOS;
        }

        public static bool Convert(FontAsset fontAsset, out string error)
        {
            if (!CanMigrate(fontAsset, out error))
                return false;

            if (fontAsset.m_GlyphTable == null || fontAsset.m_GlyphTable.Count == 0)
            {
                ConvertEmpty(fontAsset);
                return true;
            }

            FontSubsetterManager.TryGetSourceFontImporter(fontAsset, out var importer, out _);
            EnsureDynamicSourceFont(fontAsset, importer);

            uint[] codePoints = GetBakedCodePoints(fontAsset);
            if (codePoints.Length == 0)
            {
                error = "The baked glyphs can't be mapped back to characters of the source font.";
                return false;
            }
            string ranges = UnicodeRanges.FromCodePoints(codePoints);

            if (HasStaleGlyphIndices(fontAsset))
            {
                error = OutOfSyncMessage;
                return false;
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            // Keeps the baked tables from being wiped on build and editor quit.
            fontAsset.clearDynamicDataOnBuild = false;
            MakeAtlasTexturesReadable(fontAsset);
            ClearStandardPipelineTables(fontAsset);

            return FontSubsetterManager.ApplySubsetKeepingBakedData(fontAsset, ranges, out error);
        }

        // A dynamic font asset needs a dynamic source font: other import modes don't include the font data
        // in builds (and GetBakedCodePoints needs it).
        static void EnsureDynamicSourceFont(FontAsset fontAsset, TrueTypeFontImporter importer)
        {
            if (importer.fontTextureCase == FontTextureCase.Dynamic)
                return;

            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.SaveAndReimport();
            Debug.LogWarning($"Changed the import mode of '{importer.assetPath}' to Dynamic, as required by " +
                $"the dynamic font asset '{fontAsset.name}'. This includes the font data in builds.", fontAsset);
        }

        static void ClearStandardPipelineTables(FontAsset fontAsset)
        {
            fontAsset.m_CharacterTable.Clear();
            fontAsset.m_FontFeatureTable = new FontFeatureTable();
        }

        static void ConvertEmpty(FontAsset fontAsset)
        {
            if (FontSubsetterManager.TryGetSourceFontImporter(fontAsset, out var importer, out _))
                EnsureDynamicSourceFont(fontAsset, importer);

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            MakeAtlasTexturesReadable(fontAsset);
            ClearStandardPipelineTables(fontAsset);
            fontAsset.UpdateSourceFontFile();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
        }

        // The character table survives glyph id drift; the cmap reverse map is a fallback for assets without one.
        internal static uint[] GetBakedCodePoints(FontAsset fontAsset)
        {
            if (fontAsset == null)
                return Array.Empty<uint>();

            var characterTable = fontAsset.m_CharacterTable;
            if (characterTable != null && characterTable.Count > 0)
            {
                var unicodes = new HashSet<uint>();
                foreach (var character in characterTable)
                    unicodes.Add(character.unicode);

                var result = new uint[unicodes.Count];
                unicodes.CopyTo(result);
                return result;
            }

            return MapGlyphsThroughSourceFont(fontAsset);
        }

        static uint[] MapGlyphsThroughSourceFont(FontAsset fontAsset)
        {
            List<Glyph> glyphTable = fontAsset.m_GlyphTable;
            if (glyphTable == null || glyphTable.Count == 0)
                return Array.Empty<uint>();

            Font font = fontAsset.SourceFont_EditorRef;
            if (font == null)
                return Array.Empty<uint>();

            // Point size is irrelevant to the character map; any valid value works.
            if (FontEngine.LoadFontFace(font, 90, fontAsset.faceInfo.faceIndex, out FontFaceHandle faceHandle) != FontEngineError.Success)
                return Array.Empty<uint>();

            try
            {
                Dictionary<uint, List<int>> glyphToUnicodes = FontEngine.GetCharacterMap(faceHandle);
                var codePoints = new HashSet<uint>();
                foreach (Glyph glyph in glyphTable)
                {
                    if (glyphToUnicodes.TryGetValue(glyph.index, out List<int> unicodes))
                    {
                        foreach (int unicode in unicodes)
                            codePoints.Add((uint)unicode);
                    }
                }

                var result = new uint[codePoints.Count];
                codePoints.CopyTo(result);
                return result;
            }
            finally
            {
                FontEngine.UnloadFontFace(faceHandle);
            }
        }

        // Baked ids from a different font revision would make the kept atlas render the wrong glyphs under ATG.
        internal static bool HasStaleGlyphIndices(FontAsset fontAsset)
        {
            var characterTable = fontAsset.m_CharacterTable;
            if (characterTable == null || characterTable.Count == 0)
                return false;

            Font font = fontAsset.SourceFont_EditorRef;
            if (font == null || FontEngine.LoadFontFace(font, 90, fontAsset.faceInfo.faceIndex, out FontFaceHandle faceHandle) != FontEngineError.Success)
                return false;

            try
            {
                foreach (var character in characterTable)
                {
                    if (!FontEngine.TryGetGlyphIndex(faceHandle, character.unicode, out uint glyphIndex) || glyphIndex != character.glyphIndex)
                        return true;
                }
                return false;
            }
            finally
            {
                FontEngine.UnloadFontFace(faceHandle);
            }
        }

        static void MakeAtlasTexturesReadable(FontAsset fontAsset)
        {
            Texture2D[] textures = fontAsset.atlasTextures;
            if (textures == null)
                return;

            foreach (Texture2D texture in textures)
            {
                if (texture != null && !texture.isReadable)
                    FontEngineEditorUtilities.SetAtlasTextureIsReadable(texture, true);
            }
        }
    }
}
