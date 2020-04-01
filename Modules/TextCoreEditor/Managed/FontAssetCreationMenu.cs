// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using System.IO;
using System.Collections.Generic;


namespace UnityEditor.TextCore
{
    static class FontAssetCreationMenu
    {
        /// <summary>
        /// Clear Font Asset Data
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/FontAsset/Reset", false, 100)]
        public static void ClearFontAssetData(MenuCommand command)
        {
            FontAsset fontAsset = command.context as FontAsset;

            if (fontAsset != null && Selection.activeObject != fontAsset)
            {
                Selection.activeObject = fontAsset;
            }

            fontAsset.ClearFontAssetData();

            // Need way to make UI Element dirty
            //TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        [MenuItem("Assets/Create/TextCore/Font Asset #%F12", false, 100, true)]
        public static void CreateFontAsset()
        {
            Object target = Selection.activeObject;

            // Make sure the selection is a font file
            if (target == null || target.GetType() != typeof(Font))
            {
                Debug.LogWarning("A Font file must first be selected in order to create a Font Asset.");
                return;
            }

            Font sourceFont = (Font)target;

            string sourceFontFilePath = AssetDatabase.GetAssetPath(target);

            string folderPath = Path.GetDirectoryName(sourceFontFilePath);
            string assetName = Path.GetFileNameWithoutExtension(sourceFontFilePath);

            string newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + " SDF.asset");

            // Initialize FontEngine
            FontEngine.InitializeFontEngine();

            // Load Font Face
            if (FontEngine.LoadFontFace(sourceFont, 90) != FontEngineError.Success)
            {
                Debug.LogWarning("Unable to load font face for [" + sourceFont.name + "].", sourceFont);
                return;
            }

            // Create new Font Asset
            FontAsset fontAsset = ScriptableObject.CreateInstance<FontAsset>();
            AssetDatabase.CreateAsset(fontAsset, newAssetFilePathWithName);

            fontAsset.version = "1.1.0";

            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            // Set font reference and GUID
            fontAsset.m_SourceFontFileGUID = AssetDatabase.AssetPathToGUID(sourceFontFilePath);
            fontAsset.m_SourceFontFile_EditorRef = sourceFont;
            fontAsset.atlasPopulationMode = FontAsset.AtlasPopulationMode.Dynamic;

            // Default atlas resolution is 1024 x 1024.
            int atlasWidth = fontAsset.atlasWidth = 1024;
            int atlasHeight = fontAsset.atlasHeight = 1024;
            int atlasPadding = fontAsset.atlasPadding = 9;
            fontAsset.atlasRenderMode = GlyphRenderMode.SDFAA;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = new Texture2D[1];

            // Create atlas texture of size zero.
            Texture2D texture = new Texture2D(0, 0, TextureFormat.Alpha8, false);

            texture.name = assetName + " Atlas";
            fontAsset.atlasTextures[0] = texture;
            AssetDatabase.AddObjectToAsset(texture, fontAsset);

            // Add free rectangle of the size of the texture.
            int packingModifier = ((GlyphRasterModes)fontAsset.atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
            fontAsset.freeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.usedGlyphRects = new List<GlyphRect>();

            // Create new Material and Add it as Sub-Asset
            Shader default_Shader = Shader.Find("Hidden/TextCore/Distance Field");
            Material tmp_material = new Material(default_Shader);

            tmp_material.name = texture.name + " Material";
            tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
            tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

            tmp_material.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

            tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
            tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);

            fontAsset.material = tmp_material;

            AssetDatabase.AddObjectToAsset(tmp_material, fontAsset);

            // Add Font Asset Creation Settings
            fontAsset.fontAssetCreationSettings = new FontAssetCreationSettings(fontAsset.m_SourceFontFileGUID, fontAsset.faceInfo.pointSize, 0, atlasPadding, 0, 1024, 1024, 7, string.Empty, (int)GlyphRenderMode.SDFAA);

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
        }
    }
}
