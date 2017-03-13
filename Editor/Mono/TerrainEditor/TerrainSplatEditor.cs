// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;

namespace UnityEditor
{
    internal class TerrainSplatEditor : EditorWindow
    {
        private string m_ButtonTitle = string.Empty;
        private Vector2 m_ScrollPosition;
        private Terrain m_Terrain;
        private int m_Index = -1;

        public Texture2D m_Texture;
        public Texture2D m_NormalMap;
        private Vector2 m_TileSize;
        private Vector2 m_TileOffset;
        private Color m_Specular;
        private float m_Metallic;
        private float m_Smoothness;

        private bool m_NormalMapHasCorrectTextureType;

        public TerrainSplatEditor()
        {
            position = new Rect(50, 50, 200, 300);
            minSize = new Vector2(200, 300);
        }

        static internal void ShowTerrainSplatEditor(string title, string button, Terrain terrain, int index)
        {
            var editor = GetWindow<TerrainSplatEditor>(true, title);
            editor.m_ButtonTitle = button;
            editor.InitializeData(terrain, index);
        }

        void InitializeData(Terrain terrain, int index)
        {
            m_Terrain = terrain;
            m_Index = index;

            SplatPrototype info;
            if (index == -1)
                info = new SplatPrototype();
            else
                info = m_Terrain.terrainData.splatPrototypes[index];

            m_Texture = info.texture;
            m_NormalMap = info.normalMap;
            m_TileSize = info.tileSize;
            m_TileOffset = info.tileOffset;
            m_Specular = info.specular;
            m_Metallic = info.metallic;
            m_Smoothness = info.smoothness;

            CheckIfNormalMapHasCorrectTextureType();
        }

        private void CheckIfNormalMapHasCorrectTextureType()
        {
            string assetPath = AssetDatabase.GetAssetPath(m_NormalMap);

            if (string.IsNullOrEmpty(assetPath))
            {
                m_NormalMapHasCorrectTextureType = true;
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
                m_NormalMapHasCorrectTextureType = false; // has asset path but no importer - must be built-in resources
            else
                m_NormalMapHasCorrectTextureType = importer.textureType == TextureImporterType.NormalMap;
        }

        void ApplyTerrainSplat()
        {
            if (m_Terrain == null || m_Terrain.terrainData == null)
                return;

            SplatPrototype[] infos = m_Terrain.terrainData.splatPrototypes;
            if (m_Index == -1)
            {
                var newarray = new SplatPrototype[infos.Length + 1];
                System.Array.Copy(infos, 0, newarray, 0, infos.Length);
                m_Index = infos.Length;
                infos = newarray;
                infos[m_Index] = new SplatPrototype();
            }

            infos[m_Index].texture = m_Texture;
            infos[m_Index].normalMap = m_NormalMap;
            infos[m_Index].tileSize = m_TileSize;
            infos[m_Index].tileOffset = m_TileOffset;
            infos[m_Index].specular = m_Specular;
            infos[m_Index].metallic = m_Metallic;
            infos[m_Index].smoothness = m_Smoothness;

            m_Terrain.terrainData.splatPrototypes = infos;
            EditorUtility.SetDirty(m_Terrain);
        }

        private bool ValidateTerrain()
        {
            if (m_Terrain == null || m_Terrain.terrainData == null)
            {
                EditorGUILayout.HelpBox("Terrain does not exist", MessageType.Error);
                return false;
            }
            return true;
        }

        private bool ValidateTextures()
        {
            if (m_Texture == null)
            {
                EditorGUILayout.HelpBox("Assign a tiling texture", MessageType.Warning);
                return false;
            }
            if (m_Texture.wrapMode != TextureWrapMode.Repeat)
            {
                // Base map generation does not support TextureWrapMode.Clamp properly
                EditorGUILayout.HelpBox("Texture wrap mode must be set to Repeat", MessageType.Warning);
                return false;
            }
            if (m_Texture.width != Mathf.ClosestPowerOfTwo(m_Texture.width) || m_Texture.height != Mathf.ClosestPowerOfTwo(m_Texture.height))
            {
                // power of two needed for efficient base map generation
                EditorGUILayout.HelpBox("Texture size must be power of two", MessageType.Warning);
                return false;
            }
            if (m_Texture.mipmapCount <= 1)
            {
                // mip maps needed for efficient base map generation.
                // And without mipmaps the terrain will look & work bad anyway.
                EditorGUILayout.HelpBox("Texture must have mip maps", MessageType.Warning);
                return false;
            }

            if (!m_NormalMapHasCorrectTextureType)
            {
                EditorGUILayout.HelpBox("Normal texture should be imported as Normal Map.", MessageType.Warning);
                return false;
            }

            return true;
        }

        private static void TextureFieldGUI(string label, ref Texture2D texture, float alignmentOffset)
        {
            GUILayout.Space(6);
            GUILayout.BeginVertical(GUILayout.Width(80));
            GUILayout.Label(label);

            System.Type t = typeof(Texture2D);
            Rect r = GUILayoutUtility.GetRect(64, 64, 64, 64, GUILayout.MaxWidth(64));
            r.x += alignmentOffset;
            texture = EditorGUI.DoObjectField(r, r, EditorGUIUtility.GetControlID(12354, FocusType.Keyboard, r), texture, t, null, null, false) as Texture2D;

            GUILayout.EndVertical();
        }

        private static void SplatSizeGUI(ref Vector2 scale, ref Vector2 offset)
        {
            GUILayoutOption kWidth10 = GUILayout.Width(10);
            GUILayoutOption kMinWidth32 = GUILayout.MinWidth(32);

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("", EditorStyles.miniLabel, kWidth10);
            GUILayout.Label("x", EditorStyles.miniLabel, kWidth10);
            GUILayout.Label("y", EditorStyles.miniLabel, kWidth10);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Size", EditorStyles.miniLabel);
            scale.x = EditorGUILayout.FloatField(scale.x, EditorStyles.miniTextField, kMinWidth32);
            scale.y = EditorGUILayout.FloatField(scale.y, EditorStyles.miniTextField, kMinWidth32);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Offset", EditorStyles.miniLabel);
            offset.x = EditorGUILayout.FloatField(offset.x, EditorStyles.miniTextField, kMinWidth32);
            offset.y = EditorGUILayout.FloatField(offset.y, EditorStyles.miniTextField, kMinWidth32);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private static bool IsUsingMetallic(Terrain.MaterialType materialType, Material materialTemplate)
        {
            return materialType == Terrain.MaterialType.BuiltInStandard
                || (materialType == Terrain.MaterialType.Custom && materialTemplate != null && materialTemplate.HasProperty("_Metallic0"));
        }

        private static bool IsUsingSpecular(Terrain.MaterialType materialType, Material materialTemplate)
        {
            return materialType == Terrain.MaterialType.BuiltInStandard
                || (materialType == Terrain.MaterialType.Custom && materialTemplate != null && materialTemplate.HasProperty("_Specular0"));
        }

        private static bool IsUsingSmoothness(Terrain.MaterialType materialType, Material materialTemplate)
        {
            return materialType == Terrain.MaterialType.BuiltInStandard
                || (materialType == Terrain.MaterialType.Custom && materialTemplate != null && materialTemplate.HasProperty("_Smoothness0"));
        }

        private void OnGUI()
        {
            const float controlSize = 64;
            EditorGUIUtility.labelWidth = position.width - controlSize - 20;

            bool isValid = true;

            m_ScrollPosition = EditorGUILayout.BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, GUI.skin.scrollView);

            isValid &= ValidateTerrain();

            EditorGUI.BeginChangeCheck();

            // texture & normal map
            GUILayout.BeginHorizontal();

            string textureLabel = "";
            float alignmentOffset = 0.0f;

            switch (m_Terrain.materialType)
            {
                case Terrain.MaterialType.BuiltInLegacyDiffuse:
                    textureLabel = "\n Diffuse (RGB)";
                    alignmentOffset = 15.0f;
                    break;

                case Terrain.MaterialType.BuiltInLegacySpecular:
                    textureLabel = "Diffuse (RGB)\n   Gloss (A)";
                    alignmentOffset = 12.0f;
                    break;

                case Terrain.MaterialType.BuiltInStandard:
                    textureLabel = " Albedo (RGB)\nSmoothness (A)";
                    alignmentOffset = 15.0f;
                    break;

                case Terrain.MaterialType.Custom:
                    textureLabel = " \n  Splat";
                    alignmentOffset = 0.0f;
                    break;
            }

            TextureFieldGUI(textureLabel, ref m_Texture, alignmentOffset);

            Texture2D oldNormalMap = m_NormalMap;
            TextureFieldGUI("\nNormal", ref m_NormalMap, -4.0f);

            if (m_NormalMap != oldNormalMap)
                CheckIfNormalMapHasCorrectTextureType();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            isValid &= ValidateTextures();

            // PBR text & settings
            if (isValid)
            {
                if (IsUsingMetallic(m_Terrain.materialType, m_Terrain.materialTemplate))
                {
                    EditorGUILayout.Space();
                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 75.0f;
                    m_Metallic = EditorGUILayout.Slider("Metallic", m_Metallic, 0.0f, 1.0f);
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }
                else if (IsUsingSpecular(m_Terrain.materialType, m_Terrain.materialTemplate))
                {
                    m_Specular = EditorGUILayout.ColorField("Specular", m_Specular);
                }

                if (IsUsingSmoothness(m_Terrain.materialType, m_Terrain.materialTemplate) && !TextureUtil.HasAlphaTextureFormat(m_Texture.format))
                {
                    EditorGUILayout.Space();
                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 75.0f;
                    m_Smoothness = EditorGUILayout.Slider("Smoothness", m_Smoothness, 0.0f, 1.0f);
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }
            }

            // tiling & offset
            SplatSizeGUI(ref m_TileSize, ref m_TileOffset);

            bool modified = EditorGUI.EndChangeCheck();

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            // button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = isValid;

            if (GUILayout.Button(m_ButtonTitle, GUILayout.MinWidth(100)))
            {
                ApplyTerrainSplat();
                Close();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            if (modified && isValid && m_Index != -1)
            {
                ApplyTerrainSplat();
            }
        }
    }
} //namespace
