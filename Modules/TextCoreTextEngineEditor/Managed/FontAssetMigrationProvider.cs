// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

#pragma warning disable CS0618 // AtlasPopulationMode.Static and FontFeatureTable are obsolete; this is the code that migrates away from them

namespace UnityEditor.TextCore.Text
{
    // What survives a static-to-dynamic conversion. The choices are ordered from cleanest to most
    // conservative; anything not kept is rebuilt on demand by the Advanced Text Generator.
    internal enum KeepBakedData
    {
        None,
        Atlas,
        AtlasAndLegacyTables,
    }

    // Adapts one font asset type to the migration window. The window is scoped to a single
    // provider at a time; other text systems (TMP) register theirs on load.
    internal abstract partial class FontAssetMigrationProvider
    {
        [AutoStaticsCleanupOnCodeReload]
        static List<FontAssetMigrationProvider> s_Providers;

        static List<FontAssetMigrationProvider> Providers => s_Providers ??= new List<FontAssetMigrationProvider> { new TextCoreFontAssetMigrationProvider() };

        internal static FontAssetMigrationProvider Default => Providers[0];

        internal static void RegisterProvider(FontAssetMigrationProvider provider)
        {
            if (Providers.Exists(p => p.GetType() == provider.GetType()))
                return;
            Providers.Add(provider);
        }

        internal static FontAssetMigrationProvider FindProvider(UnityEngine.Object asset)
        {
            foreach (FontAssetMigrationProvider provider in Providers)
            {
                if (provider.Owns(asset))
                    return provider;
            }
            return Default;
        }

        internal static FontAssetMigrationProvider FindProvider(string typeName)
        {
            foreach (FontAssetMigrationProvider provider in Providers)
            {
                if (provider.GetType().FullName == typeName)
                    return provider;
            }
            return Default;
        }

        internal abstract bool Owns(UnityEngine.Object asset);
        internal abstract IEnumerable<UnityEngine.Object> ScanStaticAssets();
        internal abstract bool IsStatic(UnityEngine.Object asset);
        internal abstract bool CanMigrate(UnityEngine.Object asset, out string reason);
        internal abstract bool HasStaleGlyphIndices(UnityEngine.Object asset);
        internal abstract bool Convert(UnityEngine.Object asset, out string error, bool dropLegacyData, bool dropAtlas);
        internal abstract uint[] GetBakedCodePoints(UnityEngine.Object asset);
        internal abstract Font GetSourceFont(UnityEngine.Object asset);
        internal abstract int GetGlyphCount(UnityEngine.Object asset);
        internal abstract string GetAtlasDescription(UnityEngine.Object asset);
        internal abstract KeepBakedData DefaultKeepBakedData(UnityEngine.Object asset);
        internal abstract string DocsUrl { get; }
    }

    // Implements the scan and conversion flow once over per-type primitives. FontAsset and
    // TMP_FontAsset have structurally identical members but no common base type.
    internal abstract class FontAssetMigrationProvider<TAsset> : FontAssetMigrationProvider where TAsset : UnityEngine.Object
    {
        internal sealed override bool Owns(UnityEngine.Object asset) => asset is TAsset;

        // The "adb" provider queries the live asset database, so just-created assets are found without
        // waiting on a search index, and it scopes to the project's own assets (packages excluded) by
        // default. Staticness is checked on the loaded asset: property queries need an index with
        // property indexing enabled, which the project may not have.
        internal sealed override IEnumerable<UnityEngine.Object> ScanStaticAssets()
        {
            var assets = new List<UnityEngine.Object>();
            using (var context = Search.SearchService.CreateContext("adb", "t:" + typeof(TAsset).Name))
            using (ISearchList results = Search.SearchService.Request(context, SearchFlags.Synchronous))
            {
                foreach (SearchItem item in results)
                {
                    var asset = item?.ToObject<TAsset>();
                    if (asset != null && IsStatic(asset))
                        assets.Add(asset);
                }
            }
            return assets;
        }

        internal sealed override bool IsStatic(UnityEngine.Object asset) => IsStatic((TAsset)asset);

        internal sealed override bool CanMigrate(UnityEngine.Object asset, out string reason) => CanMigrate((TAsset)asset, out reason);

        internal sealed override bool HasStaleGlyphIndices(UnityEngine.Object asset) => HasStaleGlyphIndices((TAsset)asset);

        internal sealed override bool Convert(UnityEngine.Object asset, out string error, bool dropLegacyData, bool dropAtlas) =>
            Convert((TAsset)asset, out error, dropLegacyData, dropAtlas);

        internal sealed override uint[] GetBakedCodePoints(UnityEngine.Object asset) => GetBakedCodePoints((TAsset)asset);

        internal sealed override Font GetSourceFont(UnityEngine.Object asset) => GetSourceFont((TAsset)asset);

        internal sealed override int GetGlyphCount(UnityEngine.Object asset)
        {
            List<Glyph> glyphTable = GetGlyphTable((TAsset)asset);
            return glyphTable != null ? glyphTable.Count : 0;
        }

        internal sealed override string GetAtlasDescription(UnityEngine.Object asset)
        {
            var fontAsset = (TAsset)asset;
            Vector2Int size = GetAtlasSize(fontAsset);
            return $"{size.x}×{size.y} · {GetAtlasRenderMode(fontAsset)}";
        }

        internal sealed override KeepBakedData DefaultKeepBakedData(UnityEngine.Object asset)
        {
            bool sdfaa = asset is TAsset fontAsset
                && GetAtlasRenderMode(fontAsset) is GlyphRenderMode.SDFAA or GlyphRenderMode.SDFAA_HINTED;
            return sdfaa ? KeepBakedData.None : KeepBakedData.Atlas;
        }

        internal bool CanMigrate(TAsset fontAsset, out string reason)
        {
            reason = null;

            // Generic == is reference equality; route through UnityEngine.Object to catch destroyed assets.
            if ((UnityEngine.Object)fontAsset == null)
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
            List<Glyph> glyphTable = GetGlyphTable(fontAsset);
            if (glyphTable == null || glyphTable.Count == 0)
                return true;

            if (!FontSubsetterManager.TryGetSourceFontImporter(GetSourceFont(fontAsset), out _, out _))
            {
                reason = FontAssetStaticMigrator.NoSourceFontMessage;
                return false;
            }

            return true;
        }

        // dropAtlas also drops the legacy data: clearing the atlas clears every baked table with it.
        internal bool Convert(TAsset fontAsset, out string error, bool dropLegacyData = true, bool dropAtlas = true)
        {
            if (!CanMigrate(fontAsset, out error))
                return false;

            List<Glyph> glyphTable = GetGlyphTable(fontAsset);
            if (glyphTable == null || glyphTable.Count == 0)
            {
                ConvertEmpty(fontAsset, dropLegacyData);
                return true;
            }

            if (FontSubsetterManager.TryGetSourceFontImporter(GetSourceFont(fontAsset), out var importer, out _))
                FontAssetStaticMigrator.EnsureDynamicSourceFont(importer, fontAsset);

            uint[] codePoints = GetBakedCodePoints(fontAsset);
            if (codePoints.Length == 0)
            {
                error = "The baked glyphs can't be mapped back to characters of the source font.";
                return false;
            }
            string ranges = UnicodeRanges.FromCodePoints(codePoints);

            if (dropAtlas)
            {
                // Nothing baked survives, so glyph-id drift in the source font doesn't matter.
                SetAtlasPopulationMode(fontAsset, dynamic: true);
                if (!ApplySubset(fontAsset, ranges, out error))
                {
                    // A failed subset must leave the asset static and retryable.
                    SetAtlasPopulationMode(fontAsset, dynamic: false);
                    return false;
                }

                SetClearDynamicDataOnBuild(fontAsset, true);
                // The subset apply cleared the atlas but kept its size; shrink it until glyphs are added.
                ClearFontAssetData(fontAsset);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssetIfDirty(fontAsset);
                return true;
            }

            if (HasStaleGlyphIndices(fontAsset))
            {
                error = FontAssetStaticMigrator.OutOfSyncMessage;
                return false;
            }

            SetAtlasPopulationMode(fontAsset, dynamic: true);
            // Keeps the baked tables from being wiped on build and editor quit.
            SetClearDynamicDataOnBuild(fontAsset, false);
            FontAssetStaticMigrator.MakeAtlasTexturesReadable(GetAtlasTextures(fontAsset));

            if (!ApplySubsetKeepingBakedData(fontAsset, ranges, out error))
            {
                SetAtlasPopulationMode(fontAsset, dynamic: false);
                SetClearDynamicDataOnBuild(fontAsset, true);
                return false;
            }

            if (dropLegacyData)
            {
                ClearStandardPipelineTables(fontAsset);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssetIfDirty(fontAsset);
            }

            return true;
        }

        void ConvertEmpty(TAsset fontAsset, bool dropLegacyData)
        {
            if (FontSubsetterManager.TryGetSourceFontImporter(GetSourceFont(fontAsset), out var importer, out _))
                FontAssetStaticMigrator.EnsureDynamicSourceFont(importer, fontAsset);

            SetAtlasPopulationMode(fontAsset, dynamic: true);
            FontAssetStaticMigrator.MakeAtlasTexturesReadable(GetAtlasTextures(fontAsset));
            if (dropLegacyData)
                ClearStandardPipelineTables(fontAsset);
            UpdateSourceFontFile(fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
        }

        // The character table survives glyph id drift; the cmap reverse map is a fallback for assets without one.
        internal uint[] GetBakedCodePoints(TAsset fontAsset)
        {
            if ((UnityEngine.Object)fontAsset == null)
                return Array.Empty<uint>();

            var unicodes = new HashSet<uint>();
            foreach ((uint unicode, _) in GetBakedCharacters(fontAsset))
                unicodes.Add(unicode);

            if (unicodes.Count > 0)
            {
                var result = new uint[unicodes.Count];
                unicodes.CopyTo(result);
                return result;
            }

            List<Glyph> glyphTable = GetGlyphTable(fontAsset);
            if (glyphTable == null || glyphTable.Count == 0)
                return Array.Empty<uint>();

            return FontAssetStaticMigrator.MapGlyphsThroughSourceFont(GetSourceFont(fontAsset), GetFaceIndex(fontAsset), glyphTable);
        }

        // Baked ids from a different font revision would make the kept atlas render the wrong glyphs under ATG.
        internal bool HasStaleGlyphIndices(TAsset fontAsset)
        {
            return FontAssetStaticMigrator.HasStaleGlyphIndices(GetSourceFont(fontAsset), GetFaceIndex(fontAsset), GetBakedCharacters(fontAsset));
        }

        protected abstract bool IsStatic(TAsset fontAsset);
        protected abstract Font GetSourceFont(TAsset fontAsset);
        protected abstract int GetFaceIndex(TAsset fontAsset);
        protected abstract List<Glyph> GetGlyphTable(TAsset fontAsset);
        protected abstract IEnumerable<(uint unicode, uint glyphIndex)> GetBakedCharacters(TAsset fontAsset);
        protected abstract GlyphRenderMode GetAtlasRenderMode(TAsset fontAsset);
        protected abstract Vector2Int GetAtlasSize(TAsset fontAsset);
        protected abstract Texture2D[] GetAtlasTextures(TAsset fontAsset);
        protected abstract void SetAtlasPopulationMode(TAsset fontAsset, bool dynamic);
        protected abstract void SetClearDynamicDataOnBuild(TAsset fontAsset, bool clear);
        protected abstract void ClearStandardPipelineTables(TAsset fontAsset);
        protected abstract void ClearFontAssetData(TAsset fontAsset);
        protected abstract void UpdateSourceFontFile(TAsset fontAsset);
        protected abstract bool ApplySubset(TAsset fontAsset, string ranges, out string error);
        protected abstract bool ApplySubsetKeepingBakedData(TAsset fontAsset, string ranges, out string error);
    }

    internal sealed class TextCoreFontAssetMigrationProvider : FontAssetMigrationProvider<FontAsset>
    {
        internal override string DocsUrl => "https://docs.unity3d.com/Manual/ui-systems/migrate-static-font-assets.html";

        protected override bool IsStatic(FontAsset fontAsset) =>
            fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic && fontAsset.atlasPopulationMode != AtlasPopulationMode.DynamicOS;

        protected override Font GetSourceFont(FontAsset fontAsset) => fontAsset.SourceFont_EditorRef;

        protected override int GetFaceIndex(FontAsset fontAsset) => fontAsset.faceInfo.faceIndex;

        protected override List<Glyph> GetGlyphTable(FontAsset fontAsset) => fontAsset.m_GlyphTable;

        protected override IEnumerable<(uint unicode, uint glyphIndex)> GetBakedCharacters(FontAsset fontAsset)
        {
            List<Character> characterTable = fontAsset.m_CharacterTable;
            if (characterTable == null)
                yield break;
            foreach (var character in characterTable)
                yield return (character.unicode, character.glyphIndex);
        }

        protected override GlyphRenderMode GetAtlasRenderMode(FontAsset fontAsset) => fontAsset.atlasRenderMode;

        protected override Vector2Int GetAtlasSize(FontAsset fontAsset) => new Vector2Int(fontAsset.atlasWidth, fontAsset.atlasHeight);

        protected override Texture2D[] GetAtlasTextures(FontAsset fontAsset) => fontAsset.atlasTextures;

        protected override void SetAtlasPopulationMode(FontAsset fontAsset, bool dynamic) =>
            fontAsset.atlasPopulationMode = dynamic ? AtlasPopulationMode.Dynamic : AtlasPopulationMode.Static;

        protected override void SetClearDynamicDataOnBuild(FontAsset fontAsset, bool clear) => fontAsset.clearDynamicDataOnBuild = clear;

        protected override void ClearStandardPipelineTables(FontAsset fontAsset)
        {
            fontAsset.m_CharacterTable.Clear();
            fontAsset.m_FontFeatureTable = new FontFeatureTable();
        }

        protected override void ClearFontAssetData(FontAsset fontAsset) => fontAsset.ClearFontAssetData(setAtlasSizeToZero: true);

        protected override void UpdateSourceFontFile(FontAsset fontAsset) => fontAsset.UpdateSourceFontFile();

        protected override bool ApplySubset(FontAsset fontAsset, string ranges, out string error) =>
            FontSubsetterManager.ApplySubset(fontAsset, ranges, out error);

        protected override bool ApplySubsetKeepingBakedData(FontAsset fontAsset, string ranges, out string error) =>
            FontSubsetterManager.ApplySubsetKeepingBakedData(fontAsset, ranges, out error);
    }
}
