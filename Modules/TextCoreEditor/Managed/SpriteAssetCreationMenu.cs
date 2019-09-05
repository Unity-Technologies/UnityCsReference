// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace UnityEditor.TextCore
{
    static class TextSpriteAssetMenu
    {
        // Add a Context Menu to the Sprite Asset Editor Panel to Create and Add a Default Material.
        [MenuItem("CONTEXT/TextSpriteAsset/Add Default Material", false, 2000)]
        static void CopyTexture(MenuCommand command)
        {
            TextSpriteAsset spriteAsset = (TextSpriteAsset)command.context;

            // Make sure the sprite asset already contains a default material
            if (spriteAsset != null && spriteAsset.material == null)
            {
                // Add new default material for sprite asset.
                AddDefaultMaterial(spriteAsset);
            }
        }

        // Add a Context Menu to the Sprite Asset Editor Panel to update existing sprite assets.
        [MenuItem("CONTEXT/TextSpriteAsset/Update Sprite Asset", false, 2100)]
        static void UpdateSpriteAsset(MenuCommand command)
        {
            TextSpriteAsset spriteAsset = (TextSpriteAsset)command.context;

            if (spriteAsset == null)
                return;

            // Get a list of all the sprites contained in the texture referenced by the sprite asset.
            // This only works if the texture is set to sprite mode.
            string filePath = AssetDatabase.GetAssetPath(spriteAsset.spriteSheet);

            if (string.IsNullOrEmpty(filePath))
                return;

            // Get all the Sprites sorted Left to Right / Top to Bottom
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).OrderByDescending(x => x.rect.y).ThenBy(x => x.rect.x).ToArray();

            List<SpriteGlyph> spriteGlyphTable = spriteAsset.spriteGlyphTable;

            // Finding available glyph indexes to insert new glyphs into.
            var tempGlyphTable = spriteGlyphTable.OrderBy(glyph => glyph.index).ToList();
            List<uint> availableGlyphIndexes = new List<uint>();

            int elementIndex = 0;
            for (uint i = 0; i < tempGlyphTable[tempGlyphTable.Count - 1].index; i++)
            {
                uint currentElementIndex = tempGlyphTable[elementIndex].index;

                if (i == currentElementIndex)
                    elementIndex += 1;
                else
                    availableGlyphIndexes.Add(i);
            }

            // Iterate over each of the sprites in the texture to try to match them to existing sprites in the sprite asset.
            for (int i = 0; i < sprites.Length; i++)
            {
                int id = sprites[i].GetInstanceID();

                int glyphIndex = spriteGlyphTable.FindIndex(item => item.sprite.GetInstanceID() == id);

                if (glyphIndex == -1)
                {
                    // Add new Sprite Glyph to the table
                    Sprite sprite = sprites[i];

                    SpriteGlyph spriteGlyph = new SpriteGlyph();

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

                    SpriteCharacter spriteCharacter = new SpriteCharacter(0, spriteGlyph);
                    spriteCharacter.name = sprite.name;
                    spriteCharacter.scale = 1.0f;

                    spriteAsset.spriteCharacterTable.Add(spriteCharacter);
                }
                else
                {
                    // Look for changes in existing Sprite Glyph
                    Sprite sprite = sprites[i];

                    SpriteGlyph spriteGlyph = spriteGlyphTable[glyphIndex];

                    // We only update changes to the sprite position / glyph rect.
                    if (spriteGlyph.glyphRect.x != sprite.rect.x || spriteGlyph.glyphRect.y != sprite.rect.y || spriteGlyph.glyphRect.width != sprite.rect.width || spriteGlyph.glyphRect.height != sprite.rect.height)
                        spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                }
            }

            // Sort glyph table by glyph index
            spriteAsset.SortGlyphTable();
            spriteAsset.UpdateLookupTables();
            //TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, spriteAsset);
        }

        // Add a menu option to easily create a new sprite asset.
        [MenuItem("Assets/Create/TextCore/Sprite Asset", false, 110, true)]
        static void CreateTextSpriteAsset()
        {
            Object target = Selection.activeObject;

            // Make sure the selection is a texture.
            if (target == null || target.GetType() != typeof(Texture2D))
            {
                Debug.LogWarning("A texture which contains sprites must first be selected in order to create a TextSprite asset.");
                return;
            }

            Texture2D sourceTex = target as Texture2D;

            // Get the path to the selected texture.
            string filePathWithName = AssetDatabase.GetAssetPath(sourceTex);
            string fileNameWithExtension = Path.GetFileName(filePathWithName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePathWithName);
            string filePath = filePathWithName.Replace(fileNameWithExtension, "");

            // Check if Sprite Asset already exists
            TextSpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath(filePath + fileNameWithoutExtension + ".asset", typeof(TextSpriteAsset)) as TextSpriteAsset;
            bool isNewAsset = spriteAsset == null;

            if (isNewAsset)
            {
                // Create new Sprite Asset using this texture
                spriteAsset = ScriptableObject.CreateInstance<TextSpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, filePath + fileNameWithoutExtension + ".asset");

                spriteAsset.version = "1.1.0";

                // Compute the hash code for the sprite asset.
                spriteAsset.hashCode = TextUtilities.GetHashCodeCaseInSensitive(spriteAsset.name);

                // Assign new Sprite Sheet texture to the Sprite Asset.
                spriteAsset.spriteSheet = sourceTex;

                List<SpriteGlyph> spriteGlyphTable = new List<SpriteGlyph>();
                List<SpriteCharacter> spriteCharacterTable = new List<SpriteCharacter>();

                PopulateSpriteTables(sourceTex, ref spriteCharacterTable, ref spriteGlyphTable);

                spriteAsset.spriteCharacterTable = spriteCharacterTable;
                spriteAsset.spriteGlyphTable = spriteGlyphTable;

                // Add new default material for sprite asset.
                AddDefaultMaterial(spriteAsset);
            }
            //else
            //{
            //    //spriteAsset.spriteInfoList = UpdateSpriteInfo(spriteAsset);

            //    // Make sure the sprite asset already contains a default material
            //    if (spriteAsset.material == null)
            //    {
            //        // Add new default material for sprite asset.
            //        AddDefaultMaterial(spriteAsset);
            //    }
            //}

            // Get the Sprites contained in the Sprite Sheet
            EditorUtility.SetDirty(spriteAsset);

            //spriteAsset.sprites = sprites;

            // Set source texture back to Not Readable.
            //texImporter.isReadable = false;


            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));  // Re-import font asset to get the new updated version.

            //AssetDatabase.Refresh();
        }

        static void PopulateSpriteTables(Texture source, ref List<SpriteCharacter> spriteCharacterTable, ref List<SpriteGlyph> spriteGlyphTable)
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

                SpriteCharacter spriteCharacter = new SpriteCharacter(0, spriteGlyph);
                spriteCharacter.name = sprite.name;
                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }

        /// <summary>
        /// Create and add new default material to sprite asset.
        /// </summary>
        /// <param name="spriteAsset"></param>
        static void AddDefaultMaterial(TextSpriteAsset spriteAsset)
        {
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            Material material = new Material(shader);
            material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            //material.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }
    }
}
