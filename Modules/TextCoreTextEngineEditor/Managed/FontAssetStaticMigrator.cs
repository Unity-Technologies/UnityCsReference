// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace UnityEditor.TextCore.Text
{
    // Type-agnostic pieces of the static-to-dynamic conversion; the flow lives in FontAssetMigrationProvider<TAsset>.
    internal static class FontAssetStaticMigrator
    {
        internal const string OutOfSyncMessage =
            "The source font has changed since this font asset's atlas was generated, so the baked glyphs no " +
            "longer match it. Re-generate the atlas with the Font Asset Creator before converting.";

        internal const string NoSourceFontMessage =
            "This font asset can't be converted to dynamic because it has no source font file in the project.";

        internal const string StaticNotSupportedMessage =
            "Static font assets aren't supported by the Advanced Text Generator. Open the Font Asset Migration " +
            "window to convert this asset to a subsetted dynamic font asset.";

        // A dynamic font asset needs a dynamic source font: other import modes don't include the font data
        // in builds (and GetBakedCodePoints needs it).
        internal static void EnsureDynamicSourceFont(TrueTypeFontImporter importer, UnityEngine.Object fontAsset)
        {
            if (importer.fontTextureCase == FontTextureCase.Dynamic)
                return;

            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.SaveAndReimport();
            Debug.LogWarning($"Changed the import mode of '{importer.assetPath}' to Dynamic, as required by " +
                $"the dynamic font asset '{fontAsset.name}'. This includes the font data in builds.", fontAsset);
        }

        internal static uint[] MapGlyphsThroughSourceFont(Font font, int faceIndex, List<Glyph> glyphTable)
        {
            if (font == null)
                return Array.Empty<uint>();

            // Point size is irrelevant to the character map; any valid value works.
            if (FontEngine.LoadFontFace(font, 90, faceIndex, out FontFaceHandle faceHandle) != FontEngineError.Success)
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

        internal static bool HasStaleGlyphIndices(Font font, int faceIndex, IEnumerable<(uint unicode, uint glyphIndex)> bakedCharacters)
        {
            if (font == null || FontEngine.LoadFontFace(font, 90, faceIndex, out FontFaceHandle faceHandle) != FontEngineError.Success)
                return false;

            try
            {
                foreach ((uint unicode, uint bakedGlyphIndex) in bakedCharacters)
                {
                    if (!FontEngine.TryGetGlyphIndex(faceHandle, unicode, out uint glyphIndex) || glyphIndex != bakedGlyphIndex)
                        return true;
                }
                return false;
            }
            finally
            {
                FontEngine.UnloadFontFace(faceHandle);
            }
        }

        internal static void MakeAtlasTexturesReadable(Texture2D[] textures)
        {
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
