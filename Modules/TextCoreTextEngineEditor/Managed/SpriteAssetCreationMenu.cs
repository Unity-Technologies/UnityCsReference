// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.U2D;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using GlyphRect = UnityEngine.TextCore.GlyphRect;
using GlyphMetrics = UnityEngine.TextCore.GlyphMetrics;


namespace UnityEditor.TextCore.Text
{
    internal static class SpriteAssetCreationMenu
    {
        // Add a Context Menu to the Sprite Asset Editor Panel to Create and Add a Default Material.
        [MenuItem("CONTEXT/SpriteAsset/Add Default Material", true, 2200)]
        static bool AddDefaultMaterialValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/SpriteAsset/Add Default Material", false, 2200)]
        static void AddDefaultMaterial(MenuCommand command)
        {
            SpriteAsset spriteAsset = (SpriteAsset)command.context;

            // Make sure the sprite asset already contains a default material
            if (spriteAsset != null && spriteAsset.material == null)
            {
                // Add new default material for sprite asset.
                AddDefaultMaterial(spriteAsset);
            }
        }

        // Add a Context Menu to the Sprite Asset Editor Panel to update existing sprite assets.
        [MenuItem("CONTEXT/SpriteAsset/Update Sprite Asset", true, 2100)]
        static bool UpdateSpriteAssetValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/SpriteAsset/Update Sprite Asset", false, 2100)]
        static void UpdateSpriteAsset(MenuCommand command)
        {
            SpriteAsset spriteAsset = (SpriteAsset)command.context;

            if (spriteAsset == null)
                return;

            UpdateSpriteAsset(spriteAsset);
        }

        internal static void UpdateSpriteAsset(SpriteAsset spriteAsset)
        {
            // Get a list of all the sprites contained in the texture referenced by the sprite asset.
            // This only works if the texture is set to sprite mode.
            string filePath = AssetDatabase.GetAssetPath(spriteAsset.spriteSheet);

            if (string.IsNullOrEmpty(filePath))
                return;

            // Get all the sprites defined in the sprite sheet texture referenced by this sprite asset.
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).ToArray();

            // Return if sprite sheet texture does not have any sprites defined in it.
            if (sprites.Length == 0)
            {
                Debug.Log("Sprite Asset <color=#FFFF80>[" + spriteAsset.name + "]</color>'s atlas texture does not appear to have any sprites defined in it. Use the Unity Sprite Editor to define sprites for this texture.", spriteAsset.spriteSheet);
                return;
            }

            List<SpriteGlyph> spriteGlyphTable = spriteAsset.spriteGlyphTable;

            // Find available glpyh indexes
            uint[] existingGlyphIndexes = spriteGlyphTable.Select(x => x.index).ToArray();
            List<uint> availableGlyphIndexes = new List<uint>();

            uint lastGlyphIndex = existingGlyphIndexes.Length > 0 ? existingGlyphIndexes.Last() : 0;
            int elementIndex = 0;
            for (uint i = 0; i < lastGlyphIndex; i++)
            {
                uint existingGlyphIndex = existingGlyphIndexes[elementIndex];

                if (i == existingGlyphIndex)
                    elementIndex += 1;
                else
                    availableGlyphIndexes.Add(i);
            }

            // Iterate over sprites contained in the updated sprite sheet to identify new and / or modified sprites.
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                // Check if current sprites is already contained in the sprite glyph table of the sprite asset.
                SpriteGlyph spriteGlyph = spriteGlyphTable.FirstOrDefault(x => x.sprite == sprite);

                if (spriteGlyph != null)
                {
                    // update existing sprite glyph
                    if (spriteGlyph.glyphRect.x != sprite.rect.x || spriteGlyph.glyphRect.y != sprite.rect.y || spriteGlyph.glyphRect.width != sprite.rect.width || spriteGlyph.glyphRect.height != sprite.rect.height)
                        spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                }
                else
                {
                    SpriteCharacter spriteCharacter;

                    // Check if this sprite potentially exists under the same name in the sprite character table.
                    if (spriteAsset.spriteCharacterTable != null && spriteAsset.spriteCharacterTable.Count > 0)
                    {
                        spriteCharacter = spriteAsset.spriteCharacterTable.FirstOrDefault(x => x.name == sprite.name);
                        spriteGlyph = spriteCharacter != null ? spriteGlyphTable[(int)spriteCharacter.glyphIndex] : null;

                        if (spriteGlyph != null)
                        {
                            // Update sprite reference and data
                            spriteGlyph.sprite = sprite;

                            if (spriteGlyph.glyphRect.x != sprite.rect.x || spriteGlyph.glyphRect.y != sprite.rect.y || spriteGlyph.glyphRect.width != sprite.rect.width || spriteGlyph.glyphRect.height != sprite.rect.height)
                                spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                        }
                    }

                    spriteGlyph = new SpriteGlyph();

                    // Get available glyph index
                    if (availableGlyphIndexes.Count > 0)
                    {
                        spriteGlyph.index = availableGlyphIndexes[0];
                        availableGlyphIndexes.RemoveAt(0);
                    }
                    else
                        spriteGlyph.index = (uint)spriteGlyphTable.Count;

                    spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                    spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                    spriteGlyph.scale = 1.0f;
                    spriteGlyph.sprite = sprite;

                    spriteGlyphTable.Add(spriteGlyph);

                    spriteCharacter = new SpriteCharacter(0xFFFE, spriteGlyph);

                    // Special handling for .notdef sprite name.
                    string fileNameToLowerInvariant = sprite.name.ToLowerInvariant();
                    if (fileNameToLowerInvariant == ".notdef" || fileNameToLowerInvariant == "notdef")
                    {
                        spriteCharacter.unicode = 0;
                        spriteCharacter.name = fileNameToLowerInvariant;
                    }
                    else
                    {
                        spriteCharacter.unicode = 0xFFFE;
                        if (!string.IsNullOrEmpty(sprite.name) && sprite.name.Length > 2 && sprite.name[0] == '0' && (sprite.name[1] == 'x' || sprite.name[1] == 'X'))
                        {
                            spriteCharacter.unicode = TextUtilities.StringHexToInt(sprite.name.Remove(0, 2));
                        }
                        spriteCharacter.name = sprite.name;
                    }

                    spriteCharacter.scale = 1.0f;

                    spriteAsset.spriteCharacterTable.Add(spriteCharacter);
                }
            }

            // Update Sprite Character Table to replace unicode 0x0 by 0xFFFE
            for (int i = 0; i < spriteAsset.spriteCharacterTable.Count; i++)
            {
                SpriteCharacter spriteCharacter = spriteAsset.spriteCharacterTable[i];
                if (spriteCharacter.unicode == 0)
                    spriteCharacter.unicode = 0xFFFE;
            }

            // Sort glyph table by glyph index
            spriteAsset.SortGlyphTable();
            spriteAsset.UpdateLookupTables();
            spriteAsset.MarkDirty();
            TextEventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, spriteAsset);
        }


        [MenuItem("Assets/Create/Text/Sprite Asset", false, 150)]
        internal static void CreateSpriteAsset()
        {
            Object[] targets = Selection.objects;

            if (targets == null)
            {
                Debug.LogWarning("A Sprite Texture must first be selected in order to create a Sprite Asset.");
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Object target = targets[i];

                // Make sure the selection is a font file
                if (target == null || target.GetType() != typeof(Texture2D))
                {
                    Debug.LogWarning("Selected Object [" + target.name + "] is not a Sprite Texture. A Sprite Texture must be selected in order to create a Sprite Asset.", target);
                    continue;
                }

                CreateSpriteAssetFromSelectedObject(target);
            }
        }


        static void CreateSpriteAssetFromSelectedObject(Object target)
        {
            // Get the path to the selected asset.
            string filePathWithName = AssetDatabase.GetAssetPath(target);
            string fileNameWithExtension = Path.GetFileName(filePathWithName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePathWithName);
            string filePath = filePathWithName.Replace(fileNameWithExtension, "");
            string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(filePath + fileNameWithoutExtension + ".asset");

            // Create new Sprite Asset
            SpriteAsset spriteAsset = ScriptableObject.CreateInstance<SpriteAsset>();
            AssetDatabase.CreateAsset(spriteAsset, uniqueAssetPath);

            spriteAsset.version = "1.1.0";

            // Compute the hash code for the sprite asset.
            spriteAsset.hashCode = TextUtilities.GetSimpleHashCode(spriteAsset.name);

            List<SpriteGlyph> spriteGlyphTable = new List<SpriteGlyph>();
            List<SpriteCharacter> spriteCharacterTable = new List<SpriteCharacter>();

            if (target.GetType() == typeof(Texture2D))
            {
                Texture2D sourceTex = target as Texture2D;

                // Assign new Sprite Sheet texture to the Sprite Asset.
                spriteAsset.spriteSheet = sourceTex;

                PopulateSpriteTables(sourceTex, ref spriteCharacterTable, ref spriteGlyphTable);

                spriteAsset.spriteCharacterTable = spriteCharacterTable;
                spriteAsset.spriteGlyphTable = spriteGlyphTable;

                // Add new default material for sprite asset.
                AddDefaultMaterial(spriteAsset);
            }
            else if (target.GetType() == typeof(SpriteAtlas))
            {
                //SpriteAtlas spriteAtlas = target as SpriteAtlas;

                //PopulateSpriteTables(spriteAtlas, ref spriteCharacterTable, ref spriteGlyphTable);

                //spriteAsset.spriteCharacterTable = spriteCharacterTable;
                //spriteAsset.spriteGlyphTable = spriteGlyphTable;

                //spriteAsset.spriteSheet = spriteGlyphTable[0].sprite.texture;

                //// Add new default material for sprite asset.
                //AddDefaultMaterial(spriteAsset);
            }

            // Update Lookup tables.
            spriteAsset.UpdateLookupTables();

            // Get the Sprites contained in the Sprite Sheet
            EditorUtility.SetDirty(spriteAsset);

            //spriteAsset.sprites = sprites;

            // Set source texture back to Not Readable.
            //texImporter.isReadable = false;

            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));  // Re-import font asset to get the new updated version.

            //AssetDatabase.Refresh();
        }

        private static void PopulateSpriteTables(Texture source, ref List<SpriteCharacter> spriteCharacterTable, ref List<SpriteGlyph> spriteGlyphTable)
        {
            //Debug.Log("Creating new Sprite Asset.");

            string filePath = AssetDatabase.GetAssetPath(source);

            // Get all the Sprites sorted by Index
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).OrderByDescending(x => x.rect.y).ThenBy(x => x.rect.x).ToArray();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                SpriteGlyph spriteGlyph = new SpriteGlyph();
                spriteGlyph.index = (uint)i;
                spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                spriteGlyph.scale = 1.0f;
                spriteGlyph.sprite = sprite;

                spriteGlyphTable.Add(spriteGlyph);

                SpriteCharacter spriteCharacter = new SpriteCharacter(0xFFFE, spriteGlyph);

                // Special handling for .notdef sprite name.
                string fileNameToLowerInvariant = sprite.name.ToLowerInvariant();
                if (fileNameToLowerInvariant == ".notdef" || fileNameToLowerInvariant == "notdef")
                {
                    spriteCharacter.unicode = 0;
                    spriteCharacter.name = fileNameToLowerInvariant;
                }
                else
                {
                    if (!string.IsNullOrEmpty(sprite.name) && sprite.name.Length > 2 && sprite.name[0] == '0' && (sprite.name[1] == 'x' || sprite.name[1] == 'X'))
                    {
                        spriteCharacter.unicode = TextUtilities.StringHexToInt(sprite.name.Remove(0, 2));
                    }
                    spriteCharacter.name = sprite.name;
                }

                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }

        private static void PopulateSpriteTables(SpriteAtlas spriteAtlas, ref List<SpriteCharacter> spriteCharacterTable, ref List<SpriteGlyph> spriteGlyphTable)
        {
            // Get number of sprites contained in the sprite atlas.
            int spriteCount = spriteAtlas.spriteCount;
            Sprite[] sprites = new Sprite[spriteCount];

            // Get all the sprites
            spriteAtlas.GetSprites(sprites);

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                SpriteGlyph spriteGlyph = new SpriteGlyph();
                spriteGlyph.index = (uint)i;
                spriteGlyph.metrics = new GlyphMetrics(sprite.textureRect.width, sprite.textureRect.height, -sprite.pivot.x, sprite.textureRect.height - sprite.pivot.y, sprite.textureRect.width);
                spriteGlyph.glyphRect = new GlyphRect(sprite.textureRect);
                spriteGlyph.scale = 1.0f;
                spriteGlyph.sprite = sprite;

                spriteGlyphTable.Add(spriteGlyph);

                SpriteCharacter spriteCharacter = new SpriteCharacter(0xFFFE, spriteGlyph);
                spriteCharacter.name = sprite.name;
                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }

        /// <summary>
        /// Create and add new default material to sprite asset.
        /// </summary>
        /// <param name="spriteAsset"></param>
        private static void AddDefaultMaterial(SpriteAsset spriteAsset)
        {
            Shader shader = TextShaderUtilities.ShaderRef_Sprite;
            Material material = new Material(shader);
            material.SetTexture(TextShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            material.hideFlags = HideFlags.HideInHierarchy;
            material.name = spriteAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }

    }
}
