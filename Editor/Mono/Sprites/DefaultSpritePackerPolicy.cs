// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Sprites
{
    // DefaultPackerPolicy will pack rectangles no matter what Sprite mesh type is unless their packing tag contains "[TIGHT]".
    internal class DefaultPackerPolicy : IPackerPolicy
    {
        protected class Entry
        {
            public Sprite            sprite;
            public AtlasSettings     settings;
            public string            atlasName;
            public SpritePackingMode packingMode;
            public int               anisoLevel;
        }

        private const uint kDefaultPaddingPower = 3; // Good for base and two mip levels.

        public virtual int GetVersion() { return 1; }
        public virtual bool AllowSequentialPacking { get { return false; } }

        protected virtual string TagPrefix { get { return "[TIGHT]"; } }
        protected virtual bool AllowTightWhenTagged { get { return true; } }
        protected virtual bool AllowRotationFlipping { get { return false; } }

        public void OnGroupAtlases(BuildTarget target, PackerJob job, int[] textureImporterInstanceIDs)
        {
            List<Entry> entries = new List<Entry>();

            string targetName = "";
            if (target != BuildTarget.NoTarget)
            {
                targetName = BuildPipeline.GetBuildTargetName(target);
            }

            foreach (int instanceID in textureImporterInstanceIDs)
            {
                TextureImporter ti = EditorUtility.InstanceIDToObject(instanceID) as TextureImporter;

                TextureFormat desiredFormat;
                ColorSpace colorSpace;
                int compressionQuality;
                ti.ReadTextureImportInstructions(target, out desiredFormat, out colorSpace, out compressionQuality);

                TextureImporterSettings tis = new TextureImporterSettings();
                ti.ReadTextureSettings(tis);

                bool hasAlphaSplittingForCompression = (targetName != "" && HasPlatformEnabledAlphaSplittingForCompression(targetName, ti));

                Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(ti.assetPath).Select(x => x as Sprite).Where(x => x != null).ToArray();
                foreach (Sprite sprite in sprites)
                {
                    Entry entry = new Entry();
                    entry.sprite = sprite;
                    entry.settings.format = desiredFormat;
                    entry.settings.colorSpace = colorSpace;
                    // Use Compression Quality for Grouping later only for Compressed Formats. Otherwise leave it Empty.
                    entry.settings.compressionQuality = UnityEditor.TextureUtil.IsCompressedTextureFormat(desiredFormat) ? compressionQuality : 0;
                    entry.settings.filterMode = Enum.IsDefined(typeof(FilterMode), ti.filterMode) ? ti.filterMode : FilterMode.Bilinear;
                    entry.settings.maxWidth = 2048;
                    entry.settings.maxHeight = 2048;
                    entry.settings.generateMipMaps = ti.mipmapEnabled;
                    entry.settings.enableRotation = AllowRotationFlipping;
                    entry.settings.allowsAlphaSplitting = TextureImporter.IsTextureFormatETC1Compression(desiredFormat) && hasAlphaSplittingForCompression;
                    if (ti.mipmapEnabled)
                        entry.settings.paddingPower = kDefaultPaddingPower;
                    else
                        entry.settings.paddingPower = (uint)EditorSettings.spritePackerPaddingPower;
                    entry.atlasName = ParseAtlasName(ti.spritePackingTag);
                    entry.packingMode = GetPackingMode(ti.spritePackingTag, tis.spriteMeshType);
                    entry.anisoLevel = ti.anisoLevel;

                    entries.Add(entry);
                }

                Resources.UnloadAsset(ti);
            }

            // First split sprites into groups based on atlas name
            var atlasGroups =
                from e in entries
                group e by e.atlasName;
            foreach (var atlasGroup in atlasGroups)
            {
                int page = 0;
                // Then split those groups into smaller groups based on texture settings
                var settingsGroups =
                    from t in atlasGroup
                    group t by t.settings;
                foreach (var settingsGroup in settingsGroups)
                {
                    string atlasName = atlasGroup.Key;
                    if (settingsGroups.Count() > 1)
                        atlasName += string.Format(" (Group {0})", page);

                    AtlasSettings settings = settingsGroup.Key;
                    settings.anisoLevel = 1;
                    // Use the highest aniso level from all entries in this atlas
                    if (settings.generateMipMaps)
                        foreach (Entry entry in settingsGroup)
                            if (entry.anisoLevel > settings.anisoLevel)
                                settings.anisoLevel = entry.anisoLevel;

                    job.AddAtlas(atlasName, settings);
                    foreach (Entry entry in settingsGroup)
                    {
                        job.AssignToAtlas(atlasName, entry.sprite, entry.packingMode, SpritePackingRotation.None);
                    }

                    ++page;
                }
            }
        }

        protected bool HasPlatformEnabledAlphaSplittingForCompression(string targetName, TextureImporter ti)
        {
            TextureImporterPlatformSettings platformSettings = ti.GetPlatformTextureSettings(targetName);
            return (platformSettings.overridden && platformSettings.allowsAlphaSplitting);
        }

        protected bool IsTagPrefixed(string packingTag)
        {
            packingTag = packingTag.Trim();
            if (packingTag.Length < TagPrefix.Length)
                return false;
            return (packingTag.Substring(0, TagPrefix.Length) == TagPrefix);
        }

        private string ParseAtlasName(string packingTag)
        {
            string name = packingTag.Trim();
            if (IsTagPrefixed(name))
                name = name.Substring(TagPrefix.Length).Trim();
            return (name.Length == 0) ? "(unnamed)" : name;
        }

        private SpritePackingMode GetPackingMode(string packingTag, SpriteMeshType meshType)
        {
            if (meshType == SpriteMeshType.Tight)
                if (IsTagPrefixed(packingTag) == AllowTightWhenTagged)
                    return SpritePackingMode.Tight;
            return SpritePackingMode.Rectangle;
        }
    }
}
