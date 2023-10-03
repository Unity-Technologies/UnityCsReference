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

        /*
        [MenuItem("Assets/Create/TextMeshPro/Font Asset Fallback", false, 105)]
        public static void CreateFallbackFontAsset()
        {
            Object target = Selection.activeObject;

            // Make sure the selection is a font file
            if (target == null || target.GetType() != typeof(TMP_FontAsset))
            {
                Debug.LogWarning("A Font file must first be selected in order to create a Font Asset.");
                return;
            }

            TMP_FontAsset sourceFontAsset = (TMP_FontAsset)target;

            string sourceFontFilePath = AssetDatabase.GetAssetPath(target);

            string folderPath = Path.GetDirectoryName(sourceFontFilePath);
            string assetName = Path.GetFileNameWithoutExtension(sourceFontFilePath);

            string newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " - Fallback.asset");

            //// Create new TM Font Asset.
            TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            AssetDatabase.CreateAsset(fontAsset, newAssetFilePathWithName);

            fontAsset.version = "1.1.0";

            fontAsset.faceInfo = sourceFontAsset.faceInfo;

            fontAsset.m_SourceFontFileGUID = sourceFontAsset.m_SourceFontFileGUID;
            fontAsset.m_SourceFontFile_EditorRef = sourceFontAsset.m_SourceFontFile_EditorRef;
            fontAsset.atlasPopulationMode = TMP_FontAsset.AtlasPopulationMode.Dynamic;

            int atlasWidth = fontAsset.atlasWidth = sourceFontAsset.atlasWidth;
            int atlasHeight = fontAsset.atlasHeight = sourceFontAsset.atlasHeight;
            int atlasPadding = fontAsset.atlasPadding = sourceFontAsset.atlasPadding;
            fontAsset.atlasRenderMode = sourceFontAsset.atlasRenderMode;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = new Texture2D[1];

            // Create and add font atlas texture
            Texture2D texture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false);
            Color32[] colors = new Color32[atlasWidth * atlasHeight];
            texture.SetPixels32(colors);

            texture.name = assetName + " Atlas";
            fontAsset.atlasTextures[0] = texture;
            AssetDatabase.AddObjectToAsset(texture, fontAsset);

            // Add free rectangle of the size of the texture.
            int packingModifier = ((GlyphRasterModes)fontAsset.atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
            fontAsset.m_FreeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.m_UsedGlyphRects = new List<GlyphRect>();

            // Create new Material and Add it as Sub-Asset
            Material tmp_material = new Material(sourceFontAsset.material);

            tmp_material.name = texture.name + " Material";
            tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
            tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

            tmp_material.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

            tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);

            fontAsset.material = tmp_material;

            AssetDatabase.AddObjectToAsset(tmp_material, fontAsset);

            // Add Font Asset Creation Settings
            // TODO

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
        }
        */

        [MenuItem("Assets/Create/Text/Font Asset", false, 100)]
        internal static void CreateFontAsset()
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

                CreateFontAssetFromSelectedObject(target);
            }
        }

        static void CreateFontAssetFromSelectedObject(Object target)
        {
            Font font = (Font)target;

            string sourceFontFilePath = AssetDatabase.GetAssetPath(target);

            string folderPath = Path.GetDirectoryName(sourceFontFilePath);
            string assetName = Path.GetFileNameWithoutExtension(sourceFontFilePath);

            string newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " SDF.asset");

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
            int atlasWidth = fontAsset.atlasWidth = 1024;
            int atlasHeight = fontAsset.atlasHeight = 1024;
            int atlasPadding = fontAsset.atlasPadding = 9;
            fontAsset.atlasRenderMode = GlyphRenderMode.SDFAA;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = new Texture2D[1];

            // Create atlas texture of size zero.
            Texture2D texture = new Texture2D(1, 1, TextureFormat.Alpha8, false);

            texture.name = assetName + " Atlas";
            fontAsset.atlasTextures[0] = texture;
            AssetDatabase.AddObjectToAsset(texture, fontAsset);

            // Add free rectangle of the size of the texture.
            int packingModifier = ((GlyphRasterModes)fontAsset.atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
            fontAsset.freeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.usedGlyphRects = new List<GlyphRect>();

            // Create new Material and Add it as Sub-Asset
            Shader default_Shader = TextShaderUtilities.ShaderRef_MobileSDF;
            Material tmp_material = new Material(default_Shader);

            tmp_material.name = texture.name + " Material";
            tmp_material.SetTexture(TextShaderUtilities.ID_MainTex, texture);
            tmp_material.SetFloat(TextShaderUtilities.ID_TextureWidth, atlasWidth);
            tmp_material.SetFloat(TextShaderUtilities.ID_TextureHeight, atlasHeight);

            tmp_material.SetFloat(TextShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

            tmp_material.SetFloat(TextShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
            tmp_material.SetFloat(TextShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);

            fontAsset.material = tmp_material;

            AssetDatabase.AddObjectToAsset(tmp_material, fontAsset);

            // Add Font Asset Creation Settings
            fontAsset.fontAssetCreationEditorSettings = new FontAssetCreationEditorSettings(fontAsset.m_SourceFontFileGUID, fontAsset.faceInfo.pointSize, 0, atlasPadding, 0, 1024, 1024, 7, string.Empty, (int)GlyphRenderMode.SDFAA);

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
        }
    }
}
