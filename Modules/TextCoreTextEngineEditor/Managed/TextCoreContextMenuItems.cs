// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    internal class TextCoreContextMenuItems : Editor
    {
        private static Texture m_copiedTexture;
        private static Material m_copiedProperties;
        private static Material m_copiedAtlasProperties;

        // ================================================================================
        // Material related context menu options
        // ================================================================================

        // Select the currently assigned material or material preset.
        [MenuItem("CONTEXT/Material/Select Material", priority = 500)]
        static void SelectMaterial(MenuCommand command)
        {
            Material mat = command.context as Material;

            // Select current material
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(mat);
        }

        // // Add a Context Menu to allow easy duplication of the Material.
        // [MenuItem("CONTEXT/Material/Create Material Preset", false)]
        // static void DuplicateMaterial(MenuCommand command)
        // {
        //     // Get the type of text object
        //     // If material is not a base material, we get material leaks...
        //
        //     Material source_Mat = (Material)command.context;
        //     if (!EditorUtility.IsPersistent(source_Mat))
        //     {
        //         Debug.LogWarning("Material is an instance and cannot be converted into a persistent asset.");
        //         return;
        //     }
        //
        //     string assetPath = AssetDatabase.GetAssetPath(source_Mat).Split('.')[0];
        //
        //     if (assetPath.IndexOf("Assets/", System.StringComparison.InvariantCultureIgnoreCase) == -1)
        //         assetPath = "Assets/New Material Preset";
        //
        //     Material duplicate = new Material(source_Mat);
        //
        //     // Need to manually copy the shader keywords
        //     duplicate.shaderKeywords = source_Mat.shaderKeywords;
        //
        //     AssetDatabase.CreateAsset(duplicate, AssetDatabase.GenerateUniqueAssetPath(assetPath + ".mat"));
        //
        //     GameObject[] selectedObjects = Selection.gameObjects;
        //
        //     // Assign new Material Preset to selected text objects (if TMP package is present)
        //     // TODO : Make this work with any material assigned to game objects.
        //     #if TEXTMESHPRO_4_0_OR_NEWER
        //     for (int i = 0; i < selectedObjects.Length; i++)
        //     {
        //         TMPro textObject = selectedObjects[i].GetComponent<TMP_Text>();
        //
        //         if (textObject != null)
        //         {
        //             textObject.fontSharedMaterial = duplicate;
        //         }
        //         else
        //         {
        //             TMP_SubMesh subMeshObject = selectedObjects[i].GetComponent<TMP_SubMesh>();
        //
        //             if (subMeshObject != null)
        //                 subMeshObject.sharedMaterial = duplicate;
        //             else
        //             {
        //                 TMP_SubMeshUI subMeshUIObject = selectedObjects[i].GetComponent<TMP_SubMeshUI>();
        //
        //                 if (subMeshUIObject != null)
        //                     subMeshUIObject.sharedMaterial = duplicate;
        //             }
        //         }
        //     }
        //     #endif
        //
        //     // Ping newly created Material Preset.
        //     EditorUtility.FocusProjectWindow();
        //     EditorGUIUtility.PingObject(duplicate);
        // }

        // Add a Context Menu to the Texture Editor Panel to allow Copy / Paste of Texture.
        [MenuItem("CONTEXT/Texture/Copy", priority = 2000)]
        static void CopyTexture(MenuCommand command)
        {
            m_copiedTexture = command.context as Texture;
        }

        //This function is used for debugging and fixing potentially broken font atlas links.
        [MenuItem("CONTEXT/Material/Copy Atlas", priority = 2000)]
        static void CopyAtlas(MenuCommand command)
        {
            Material mat = command.context as Material;

            m_copiedAtlasProperties = new Material(mat);
            m_copiedAtlasProperties.hideFlags = HideFlags.DontSave;
        }

        // This function is used for debugging and fixing potentially broken font atlas links
        [MenuItem("CONTEXT/Material/Paste Atlas", validate = true)]
        static bool PasteAtlasValidate(MenuCommand command)
        {
            if (m_copiedAtlasProperties == null && m_copiedTexture == null)
                return false;

            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/Material/Paste Atlas", priority = 2001)]
        static void PasteAtlas(MenuCommand command)
        {
            Material mat = command.context as Material;

            if (mat == null)
                return;

            if (m_copiedAtlasProperties != null)
            {
                Undo.RecordObject(mat, "Paste Texture");

                // Make sure we have valid Property IDs
                TextShaderUtilities.GetShaderPropertyIDs();

                if (m_copiedAtlasProperties.HasProperty(TextShaderUtilities.ID_MainTex))
                    mat.SetTexture(TextShaderUtilities.ID_MainTex, m_copiedAtlasProperties.GetTexture(TextShaderUtilities.ID_MainTex));

                if (m_copiedAtlasProperties.HasProperty(TextShaderUtilities.ID_GradientScale))
                {
                    mat.SetFloat(TextShaderUtilities.ID_GradientScale, m_copiedAtlasProperties.GetFloat(TextShaderUtilities.ID_GradientScale));
                    mat.SetFloat(TextShaderUtilities.ID_TextureWidth, m_copiedAtlasProperties.GetFloat(TextShaderUtilities.ID_TextureWidth));
                    mat.SetFloat(TextShaderUtilities.ID_TextureHeight, m_copiedAtlasProperties.GetFloat(TextShaderUtilities.ID_TextureHeight));
                }
            }
            else if (m_copiedTexture != null)
            {
                Undo.RecordObject(mat, "Paste Texture");

                mat.SetTexture(TextShaderUtilities.ID_MainTex, m_copiedTexture);
            }
        }

        // COPY MATERIAL PROPERTIES
        [MenuItem("CONTEXT/Material/Copy Material Properties", secondaryPriority = 1)]
        static void CopyMaterialProperties(MenuCommand command)
        {
            Material mat = null;
            if (command.context.GetType() == typeof(Material))
                mat = (Material)command.context;
            else
            {
                mat = Selection.activeGameObject.GetComponent<CanvasRenderer>().GetMaterial();
            }

            m_copiedProperties = new Material(mat);

            m_copiedProperties.shaderKeywords = mat.shaderKeywords;

            m_copiedProperties.hideFlags = HideFlags.DontSave;
        }

        // PASTE MATERIAL PROPERTIES
        [MenuItem("CONTEXT/Material/Paste Material Properties", validate = true)]
        static bool PasteMaterialPropertiesValidate(MenuCommand command)
        {
            if (m_copiedProperties == null)
                return false;

            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/Material/Paste Material Properties", secondaryPriority = 2)]
        static void PasteMaterialProperties(MenuCommand command)
        {
            if (m_copiedProperties == null)
            {
                Debug.LogWarning("No Material Properties to Paste. Use Copy Material Properties first.");
                return;
            }

            Material mat = null;
            if (command.context.GetType() == typeof(Material))
                mat = (Material)command.context;
            else
            {
                mat = Selection.activeGameObject.GetComponent<CanvasRenderer>().GetMaterial();
            }

            Undo.RecordObject(mat, "Paste Material");

            TextShaderUtilities.GetShaderPropertyIDs(); // Make sure we have valid Property IDs
            if (mat.HasProperty(TextShaderUtilities.ID_GradientScale))
            {
                // Preserve unique SDF properties from destination material.
                m_copiedProperties.SetTexture(TextShaderUtilities.ID_MainTex, mat.GetTexture(TextShaderUtilities.ID_MainTex));
                m_copiedProperties.SetFloat(TextShaderUtilities.ID_GradientScale, mat.GetFloat(TextShaderUtilities.ID_GradientScale));
                m_copiedProperties.SetFloat(TextShaderUtilities.ID_TextureWidth, mat.GetFloat(TextShaderUtilities.ID_TextureWidth));
                m_copiedProperties.SetFloat(TextShaderUtilities.ID_TextureHeight, mat.GetFloat(TextShaderUtilities.ID_TextureHeight));
            }

            EditorShaderUtilities.CopyMaterialProperties(m_copiedProperties, mat);

            // Copy ShaderKeywords from one material to the other.
            mat.shaderKeywords = m_copiedProperties.shaderKeywords;

            // Let TextMeshPro Objects that this mat has changed.
            TextEventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
        }

        // Enable Resetting of Material properties without losing unique properties of the font atlas.
        [MenuItem("CONTEXT/Material/Reset", validate = true)]
        static bool ResetSettingsValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/Material/Reset", priority = 2100)]
        static void ResetSettings(MenuCommand command)
        {
            Material mat = null;
            if (command.context.GetType() == typeof(Material))
                mat = (Material)command.context;
            else
            {
                mat = Selection.activeGameObject.GetComponent<CanvasRenderer>().GetMaterial();
            }

            Undo.RecordObject(mat, "Reset Material");

            TextShaderUtilities.GetShaderPropertyIDs(); // Make sure we have valid Property IDs
            if (mat.HasProperty(TextShaderUtilities.ID_GradientScale))
            {
                bool isSRPShader = mat.HasProperty(TextShaderUtilities.ID_IsoPerimeter);

                // Copy unique properties of the SDF Material
                var texture = mat.GetTexture(TextShaderUtilities.ID_MainTex);
                var gradientScale = mat.GetFloat(TextShaderUtilities.ID_GradientScale);
                float texWidth = 0, texHeight = 0;
                float normalWeight = 0, boldWeight = 0;

                if (!isSRPShader)
                {
                    texWidth = mat.GetFloat(TextShaderUtilities.ID_TextureWidth);
                    texHeight = mat.GetFloat(TextShaderUtilities.ID_TextureHeight);
                    normalWeight = mat.GetFloat(TextShaderUtilities.ID_WeightNormal);
                    boldWeight = mat.GetFloat(TextShaderUtilities.ID_WeightBold);
                }


                var stencilId = 0.0f;
                var stencilComp = 0.0f;

                if (mat.HasProperty(TextShaderUtilities.ID_StencilID))
                {
                    stencilId = mat.GetFloat(TextShaderUtilities.ID_StencilID);
                    stencilComp = mat.GetFloat(TextShaderUtilities.ID_StencilComp);
                }

                // Reset the material
                Unsupported.SmartReset(mat);

                // Reset ShaderKeywords
                mat.shaderKeywords = new string[0]; // { "BEVEL_OFF", "GLOW_OFF", "UNDERLAY_OFF" };

                // Copy unique material properties back to the material.
                mat.SetTexture(TextShaderUtilities.ID_MainTex, texture);
                mat.SetFloat(TextShaderUtilities.ID_GradientScale, gradientScale);

                if (!isSRPShader)
                {
                    mat.SetFloat(TextShaderUtilities.ID_TextureWidth, texWidth);
                    mat.SetFloat(TextShaderUtilities.ID_TextureHeight, texHeight);
                    mat.SetFloat(TextShaderUtilities.ID_WeightNormal, normalWeight);
                    mat.SetFloat(TextShaderUtilities.ID_WeightBold, boldWeight);
                }

                if (mat.HasProperty(TextShaderUtilities.ID_StencilID))
                {
                    mat.SetFloat(TextShaderUtilities.ID_StencilID, stencilId);
                    mat.SetFloat(TextShaderUtilities.ID_StencilComp, stencilComp);
                }
            }
            else
            {
                Unsupported.SmartReset(mat);
            }

            TextEventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
        }

        // ================================================================================
        // Font Asset related context menu options
        // ================================================================================

        /// <summary>
        ///
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/FontAsset/Update Atlas Texture...", priority = 2000)]
        static void RegenerateFontAsset(MenuCommand command)
        {
            FontAsset fontAsset = command.context as FontAsset;

            if (fontAsset != null)
            {
                FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(fontAsset);
            }
        }

        /// <summary>
        /// Clear Dynamic Font Asset data such as glyph, character and font features.
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/FontAsset/Reset", validate = true)]
        static bool ClearFontAssetDataValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/FontAsset/Reset", priority = 2100)]
        static void ClearFontAssetData(MenuCommand command)
        {
            FontAsset fontAsset = command.context as FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            ClearFontAssetData(fontAsset);
        }

        internal static void ClearFontAssetData(FontAsset fontAsset)
        {
            if (fontAsset == null)
                return;

            fontAsset.ClearFontAssetData(true);
            TextResourceManager.RebuildFontAssetCache();

            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        //This function is used for debugging and fixing potentially broken font atlas links.
        [MenuItem("CONTEXT/Font Asset/Extract Atlas", priority = 2101)]
        static void ExtractAtlas(MenuCommand command)
        {
            FontAsset font = command.context as FontAsset;

            string fontPath = AssetDatabase.GetAssetPath(font);
            string texPath = Path.GetDirectoryName(fontPath) + "/" + Path.GetFileNameWithoutExtension(fontPath) + " Atlas.png";

            // Create a Serialized Object of the texture to allow us to make it readable.
            SerializedObject texprop = new SerializedObject(font.material.GetTexture(TextShaderUtilities.ID_MainTex));
            texprop.FindProperty("m_IsReadable").boolValue = true;
            texprop.ApplyModifiedProperties();

            // Create a copy of the texture.
            Texture2D tex = Instantiate(font.material.GetTexture(TextShaderUtilities.ID_MainTex)) as Texture2D;

            // Set the texture to not readable again.
            texprop.FindProperty("m_IsReadable").boolValue = false;
            texprop.ApplyModifiedProperties();

            Debug.Log(texPath);
            // Saving File for Debug
            var pngData = tex.EncodeToPNG();
            File.WriteAllBytes(texPath, pngData);

            AssetDatabase.Refresh();
            DestroyImmediate(tex);
        }

        /// <summary>
        /// Clear Character and Glyph data (only).
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/FontAsset/Clear Dynamic Data", validate = true, priority = 2102)]
        static bool ClearFontCharacterDataValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/FontAsset/Clear Dynamic Data", priority = 2102)]
        static void ClearFontCharacterData(MenuCommand command)
        {
            FontAsset fontAsset = command.context as FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            ClearFontCharacterData(fontAsset);
        }

        internal static void ClearFontCharacterData(FontAsset fontAsset)
        {
            if (fontAsset == null)
                return;

            fontAsset.ClearCharacterAndGlyphTablesInternal();

            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        /// <summary>
        /// Import all font features
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/FontAsset/Import Font Features", validate = true, priority = 2110)]
        static bool ReimportFontFeaturesValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/FontAsset/Import Font Features", priority = 2110)]
        static void ReimportFontFeatures(MenuCommand command)
        {
            FontAsset fontAsset = command.context as FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            fontAsset.ImportFontFeatures();

            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/TrueTypeFontImporter/Create Font Asset...", priority = 200, secondaryPriority = 1)]
        static void CreateFontAsset(MenuCommand command)
        {
            TrueTypeFontImporter importer = command.context as TrueTypeFontImporter;

            if (importer != null)
            {
                Font sourceFontFile = AssetDatabase.LoadAssetAtPath<Font>(importer.assetPath);

                if (sourceFontFile)
                    FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(sourceFontFile);
            }
        }
    }
}
