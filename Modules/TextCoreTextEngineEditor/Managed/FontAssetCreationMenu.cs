// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.TextCore.LowLevel;

using GlyphRect = UnityEngine.TextCore.GlyphRect;

namespace UnityEditor.TextCore.Text
{
    internal static class FontAsset_CreationMenu
    {
        /// <summary>
        /// Enables the creation of a font asset with unique face info attributes but sharing the same atlas texture and material.
        /// </summary>
        [MenuItem("Assets/Create/Text/Font Asset Variant", false, 105)]
        internal static void CreateFontAssetVariant()
        {
            Object target = Selection.activeObject;

            // Make sure the selection is a font file
            if (target == null || target.GetType() != typeof(FontAsset))
            {
                Debug.LogWarning("A Font Asset must first be selected in order to create a Font Asset Variant.");
                return;
            }

            FontAsset sourceFontAsset = (FontAsset)target;

            string sourceFontFilePath = AssetDatabase.GetAssetPath(target);

            string folderPath = Path.GetDirectoryName(sourceFontFilePath);
            string assetName = Path.GetFileNameWithoutExtension(sourceFontFilePath);

            string newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " - Variant.asset");

            // Set Texture and Material reference to the source font asset.
            FontAsset fontAsset = ScriptableObject.Instantiate<FontAsset>(sourceFontAsset);
            AssetDatabase.CreateAsset(fontAsset, newAssetFilePathWithName);

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = sourceFontAsset.atlasTextures;
            fontAsset.material = sourceFontAsset.material;

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Create/Text/FontAsset/SDF", false, 100)]
        static void CreateFontAssetSDF()
        {
            CreateFontAsset(GlyphRenderMode.SDFAA);
        }

        [MenuItem("Assets/Create/Text/FontAsset/Bitmap", false, 105)]
        static void CreateFontAssetBitmap()
        {
            CreateFontAsset(GlyphRenderMode.SMOOTH);
        }


        [MenuItem("Assets/Create/Text/FontAsset/Color", false, 110)]
        static void CreateFontAssetColor()
        {
            CreateFontAsset(GlyphRenderMode.COLOR);
        }


        static void CreateFontAsset(GlyphRenderMode renderMode)
        {
            Object[] targets = Selection.objects;

            if (targets == null)
            {
                Debug.LogWarning("A Font file must first be selected in order to create a Font Asset.");
                return;
            }


            for (int i = 0; i < targets.Length; i++)
            {
                Object target = targets[i];

                // Make sure the selection is a font file
                if (target == null || target.GetType() != typeof(Font))
                {
                    Debug.LogWarning("Selected Object [" + target?.name + "] is not a Font file. A Font file must be selected in order to create a Font Asset.", target);
                    continue;
                }

                CreateFontAssetFromSelectedObject(target, renderMode);
            }
        }

        static void CreateFontAssetFromSelectedObject(Object target, GlyphRenderMode renderMode)
        {
            Font font = (Font)target;

            string sourceFontFilePath = AssetDatabase.GetAssetPath(target);

            string folderPath = Path.GetDirectoryName(sourceFontFilePath);
            string assetName = Path.GetFileNameWithoutExtension(sourceFontFilePath);

            string newAssetFilePathWithName;
            ;
            switch (renderMode)
            {
                case GlyphRenderMode.SMOOTH:
                    newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " Bitmap.asset");
                    break;

                case GlyphRenderMode.COLOR:
                    newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " Color.asset");
                    break;

                case GlyphRenderMode.SDFAA:
                default:
                    newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " SDF.asset");
                    break;
            }

            // Initialize FontEngine
            FontEngine.InitializeFontEngine();

            // Load Font Face
            if (FontEngine.LoadFontFace(font, 90) != FontEngineError.Success)
            {
                Debug.LogWarning("Unable to load font face for [" + font.name + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.", font);
                return;
            }

            // Create new Font Asset
            FontAsset fontAsset = ScriptableObject.CreateInstance<FontAsset>();
            AssetDatabase.CreateAsset(fontAsset, newAssetFilePathWithName);

            fontAsset.version = "1.1.0";
            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            // Set font reference and GUID
            fontAsset.sourceFontFile = font;
            fontAsset.m_SourceFontFileGUID = AssetDatabase.AssetPathToGUID(sourceFontFilePath);
            fontAsset.m_SourceFontFile_EditorRef = font;

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            //fontAsset.clearDynamicDataOnBuild = TextSettings.clearDynamicDataOnBuild;

            // Default atlas resolution is 1024 x 1024.
            fontAsset.atlasTextures = new Texture2D[1];
            int atlasWidth = fontAsset.atlasWidth = 1024;
            int atlasHeight = fontAsset.atlasHeight = 1024;
            int atlasPadding = fontAsset.atlasPadding = 9;

            Texture2D texture;
            Material mat;
            Shader shader;
            int packingModifier;

            switch (renderMode)
            {
                case GlyphRenderMode.SMOOTH:
                    fontAsset.atlasRenderMode = GlyphRenderMode.SMOOTH;
                    texture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                    shader = TextShaderUtilities.ShaderRef_MobileBitmap;
                    packingModifier = 0;
                    mat = new Material(shader);
                    break;
                case GlyphRenderMode.COLOR:
                    fontAsset.atlasRenderMode = GlyphRenderMode.COLOR;
                    texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    shader = TextShaderUtilities.ShaderRef_Sprite;
                    packingModifier = 0;
                    mat = new Material(shader);
                    break;
                case GlyphRenderMode.SDFAA:
                default:
                    fontAsset.atlasRenderMode = GlyphRenderMode.SDFAA;
                    texture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                    shader = TextShaderUtilities.ShaderRef_MobileSDF;
                    packingModifier = 1;
                    mat = new Material(shader);

                    mat.SetFloat(TextShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);
                    mat.SetFloat(TextShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
                    mat.SetFloat(TextShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);

                    break;
            }

            texture.name = assetName + " Atlas";
            mat.name = texture.name + " Material";

            fontAsset.atlasTextures[0] = texture;
            AssetDatabase.AddObjectToAsset(texture, fontAsset);

            fontAsset.freeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.usedGlyphRects = new List<GlyphRect>();

            mat.SetTexture(TextShaderUtilities.ID_MainTex, texture);
            mat.SetFloat(TextShaderUtilities.ID_TextureWidth, atlasWidth);
            mat.SetFloat(TextShaderUtilities.ID_TextureHeight, atlasHeight);

            fontAsset.material = mat;
            AssetDatabase.AddObjectToAsset(mat, fontAsset);

            // Add Font Asset Creation Settings
            fontAsset.fontAssetCreationEditorSettings = new FontAssetCreationEditorSettings(fontAsset.m_SourceFontFileGUID, fontAsset.faceInfo.pointSize, 0, atlasPadding, 0, 1024, 1024, 7, string.Empty, (int)GlyphRenderMode.SDFAA);

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
        }
    }
}
