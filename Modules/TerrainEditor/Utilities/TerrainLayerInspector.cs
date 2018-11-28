// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System.Collections;
using System.IO;
using UnityEditorInternal;
using UnityEditor;


namespace UnityEditor
{
    [CustomEditor(typeof(TerrainLayer))]
    [CanEditMultipleObjects]
    internal class TerrainLayerInspector : Editor
    {
        [MenuItem("Assets/Create/Terrain Layer")]
        public static void CreateNewDefaultTerrainLayer()
        {
            TerrainLayer terrainLayer = new TerrainLayer();
            ProjectWindowUtil.CreateAsset(terrainLayer, "New Terrain Layer.terrainlayer");
        }

        private class Styles
        {
            public readonly GUIContent diffuseTexture = EditorGUIUtility.TrTextContent("Diffuse", "Depending on your terrain material type, the Alpha channel can be:\n\n  Built-in diffuse: Unused\n  Built-in specular: Gloss\n  Built-in standard or HDRP TerrainLit: Smoothness");
            public readonly GUIContent diffuseTextureMaskMapEnabled = EditorGUIUtility.TrTextContent("Diffuse", "Depending on your terrain material type, the Alpha channel can be:\n\n  Built-in diffuse: Unused\n  Built-in specular: Gloss\n  Built-in standard: Smoothness\n  HDRP TerrainLit: Density");
            public readonly GUIContent normalMapTexture = EditorGUIUtility.TrTextContent("Normal Map");
            public readonly GUIContent maskMapTexture = EditorGUIUtility.TrTextContent("Mask Map", "HDRP TerrainLit shader uses this texture for:\n\n  R: Metallic\n  G: AO\n  B: Height\n  A: Smoothness (Diffuse Alpha becomes Density)\n\nCustom shaders can use this texture for whatever purpose.");
            public readonly GUIContent channelRemapping = EditorGUIUtility.TrTextContent("Channel Remapping");
            public readonly GUIContent red = EditorGUIUtility.TrTextContent("Red");
            public readonly GUIContent green = EditorGUIUtility.TrTextContent("Green");
            public readonly GUIContent blue = EditorGUIUtility.TrTextContent("Blue");
            public readonly GUIContent alpha = EditorGUIUtility.TrTextContent("Alpha");
            public readonly GUIContent min = EditorGUIUtility.TrTextContent("Min", "The value when the texture channel is 0");
            public readonly GUIContent max = EditorGUIUtility.TrTextContent("Max", "The value when the texture channel is 1");
            public readonly GUIContent tilingSettings = EditorGUIUtility.TrTextContent("Tiling Settings");
            public readonly GUIContent tileSize = EditorGUIUtility.TrTextContent("Size");
            public readonly GUIContent tileOffset = EditorGUIUtility.TrTextContent("Offset");
        }

        private static Styles s_Styles = new Styles();

        [SerializeField]
        protected Vector2 m_Pos;

        SerializedProperty m_DiffuseTexture;
        SerializedProperty m_NormalMapTexture;
        SerializedProperty m_MaskMapTexture;
        SerializedProperty m_TileSize;
        SerializedProperty m_TileOffset;
        SerializedProperty m_Specular;
        SerializedProperty m_Metallic;
        SerializedProperty m_Smoothness;
        SerializedProperty m_NormalScale;
        SerializedProperty m_MaskRemapMinR;
        SerializedProperty m_MaskRemapMinG;
        SerializedProperty m_MaskRemapMinB;
        SerializedProperty m_MaskRemapMinA;
        SerializedProperty m_MaskRemapMaxR;
        SerializedProperty m_MaskRemapMaxG;
        SerializedProperty m_MaskRemapMaxB;
        SerializedProperty m_MaskRemapMaxA;

        bool m_HasChanged = true;
        bool m_NormalMapHasCorrectTextureType;
        bool m_ShowMaskRemap = false;

        ITerrainLayerCustomUI m_CustomUI = null;
        Terrain m_CustomUITerrain = null;

        internal bool m_ShowMaskMap = true;
        private bool m_MaskMapUsed = false;
        private GUIContent m_MaskMapText = s_Styles.maskMapTexture;
        private GUIContent m_DiffuseMapText = s_Styles.diffuseTexture;
        private GUIContent m_DiffuseMapMaskMapEnabledText = s_Styles.diffuseTextureMaskMapEnabled;
        private GUIContent m_MaskRemapRText = s_Styles.red;
        private GUIContent m_MaskRemapGText = s_Styles.green;
        private GUIContent m_MaskRemapBText = s_Styles.blue;
        private GUIContent m_MaskRemapAText = s_Styles.alpha;

        internal virtual void OnEnable()
        {
            if (!target)
                return;

            m_DiffuseTexture = serializedObject.FindProperty("m_DiffuseTexture");
            m_NormalMapTexture = serializedObject.FindProperty("m_NormalMapTexture");
            m_MaskMapTexture = serializedObject.FindProperty("m_MaskMapTexture");
            m_TileSize = serializedObject.FindProperty("m_TileSize");
            m_TileOffset = serializedObject.FindProperty("m_TileOffset");
            m_Specular = serializedObject.FindProperty("m_Specular");
            m_Metallic = serializedObject.FindProperty("m_Metallic");
            m_Smoothness = serializedObject.FindProperty("m_Smoothness");
            m_NormalScale = serializedObject.FindProperty("m_NormalScale");
            m_MaskRemapMinR = serializedObject.FindProperty("m_MaskMapRemapMin.x");
            m_MaskRemapMinG = serializedObject.FindProperty("m_MaskMapRemapMin.y");
            m_MaskRemapMinB = serializedObject.FindProperty("m_MaskMapRemapMin.z");
            m_MaskRemapMinA = serializedObject.FindProperty("m_MaskMapRemapMin.w");
            m_MaskRemapMaxR = serializedObject.FindProperty("m_MaskMapRemapMax.x");
            m_MaskRemapMaxG = serializedObject.FindProperty("m_MaskMapRemapMax.y");
            m_MaskRemapMaxB = serializedObject.FindProperty("m_MaskMapRemapMax.z");
            m_MaskRemapMaxA = serializedObject.FindProperty("m_MaskMapRemapMax.w");

            m_NormalMapHasCorrectTextureType = TerrainLayerUtility.CheckNormalMapTextureType(m_NormalMapTexture.objectReferenceValue as Texture2D);
        }

        internal void SetCustomUI(ITerrainLayerCustomUI customUI, Terrain terrain)
        {
            m_CustomUI = customUI;
            m_CustomUITerrain = terrain;
        }

        internal void UpdateMaskMapChannelUsages(string maskMapR, string maskMapG, string maskMapB, string maskMapA, string diffuseA, string diffuseAMaskEnabled, bool maskMapUsed)
        {
            if (String.IsNullOrEmpty(maskMapR) && String.IsNullOrEmpty(maskMapG) && String.IsNullOrEmpty(maskMapB) && String.IsNullOrEmpty(maskMapA))
            {
                m_ShowMaskMap = false;
                m_MaskMapUsed = false;
                return;
            }

            m_ShowMaskMap = true;
            m_MaskMapUsed = maskMapUsed;

            m_MaskMapText = new GUIContent(s_Styles.maskMapTexture);
            m_MaskMapText.tooltip = "";
            if (!String.IsNullOrEmpty(maskMapR))
            {
                m_MaskMapText.tooltip += "Red: " + maskMapR;
                m_MaskRemapRText.text = maskMapR;
            }
            else
                m_MaskRemapRText = null;

            if (!String.IsNullOrEmpty(maskMapG))
            {
                m_MaskMapText.tooltip += (String.IsNullOrEmpty(m_MaskMapText.tooltip) ? "" : "\n") + "Green: " + maskMapG;
                m_MaskRemapGText.text = maskMapG;
            }
            else
                m_MaskRemapGText = null;

            if (!String.IsNullOrEmpty(maskMapB))
            {
                m_MaskMapText.tooltip += (String.IsNullOrEmpty(m_MaskMapText.tooltip) ? "" : "\n") + "Blue: " + maskMapB;
                m_MaskRemapBText.text = maskMapB;
            }
            else
                m_MaskRemapBText = null;

            if (!String.IsNullOrEmpty(maskMapA))
            {
                m_MaskMapText.tooltip += (String.IsNullOrEmpty(m_MaskMapText.tooltip) ? "" : "\n") + "Alpha: " + maskMapA;
                m_MaskRemapAText.text = maskMapA;
            }
            else
                m_MaskRemapAText = null;

            if (!String.IsNullOrEmpty(diffuseA))
            {
                m_DiffuseMapText = new GUIContent(s_Styles.diffuseTexture);
                m_DiffuseMapText.tooltip = "Alpha: " + diffuseA;

                if (!String.IsNullOrEmpty(diffuseAMaskEnabled))
                {
                    m_DiffuseMapMaskMapEnabledText = new GUIContent(s_Styles.diffuseTextureMaskMapEnabled);
                    m_DiffuseMapMaskMapEnabledText.tooltip = "Alpha: " + diffuseAMaskEnabled;
                }
                else
                    m_DiffuseMapMaskMapEnabledText = m_DiffuseMapText;
            }
        }

        private static void DoMinMaxLabels(GUIContent minLabel, GUIContent maxLabel, GUIStyle style)
        {
            var r = EditorGUILayout.GetControlRect();
            r.x += EditorGUIUtility.labelWidth;
            r.width -= EditorGUIUtility.labelWidth;
            var halfWidth = (r.width - EditorGUI.kSpacing) / 2;
            var r1 = new Rect(r.x, r.y, halfWidth, r.height);
            var r2 = new Rect(r.x + halfWidth + EditorGUI.kSpacing, r.y, halfWidth, r.height);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(r1, minLabel, style);
            EditorGUI.LabelField(r2, maxLabel, style);
            EditorGUI.indentLevel = indent;
        }

        private static void DoMinMaxFloatFields(GUIContent label, GUIStyle labelStyle, SerializedProperty minField, SerializedProperty maxField, GUIStyle fieldStyle)
        {
            if (label == null)
                return;

            var r = EditorGUILayout.GetControlRect();
            r = EditorGUI.PrefixLabel(r, label, labelStyle);
            var halfWidth = (r.width - EditorGUI.kSpacing) / 2;
            var r1 = new Rect(r.x, r.y, halfWidth, r.height);
            var r2 = new Rect(r.x + halfWidth + EditorGUI.kSpacing, r.y, halfWidth, r.height);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginProperty(r1, null, minField);
            EditorGUI.BeginChangeCheck();
            float minValue = EditorGUI.FloatField(r1, minField.floatValue, fieldStyle);
            if (EditorGUI.EndChangeCheck())
                minField.floatValue = minValue;
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(r2, null, maxField);
            EditorGUI.BeginChangeCheck();
            float maxValue = EditorGUI.FloatField(r2, maxField.floatValue, fieldStyle);
            if (EditorGUI.EndChangeCheck())
                maxField.floatValue = maxValue;
            EditorGUI.EndProperty();

            EditorGUI.indentLevel = indent;
        }

        public override void OnInspectorGUI()
        {
            if (!target)
                return;

            if (m_CustomUI != null && m_CustomUITerrain != null
                && m_CustomUI.OnTerrainLayerGUI(target as TerrainLayer, m_CustomUITerrain))
                return;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            var curMaskMap = m_MaskMapTexture.objectReferenceValue as Texture2D;
            bool maskMapUsed = m_MaskMapUsed || curMaskMap != null;

            var r = EditorGUILayout.GetControlRect(true, EditorGUI.kObjectFieldThumbnailHeight);
            var diffuseLabel = m_ShowMaskMap ? (maskMapUsed ? m_DiffuseMapMaskMapEnabledText : m_DiffuseMapText) : s_Styles.diffuseTexture;
            EditorGUI.BeginProperty(r, diffuseLabel, m_DiffuseTexture);
            EditorGUI.BeginChangeCheck();
            var diffuseTexture = EditorGUI.ObjectField(r, diffuseLabel, m_DiffuseTexture.objectReferenceValue as Texture2D, typeof(Texture2D), false) as Texture2D;
            if (EditorGUI.EndChangeCheck())
                m_DiffuseTexture.objectReferenceValue = diffuseTexture;
            EditorGUI.EndProperty();

            TerrainLayerUtility.ValidateDiffuseTextureUI(diffuseTexture);

            r = EditorGUILayout.GetControlRect(true, EditorGUI.kObjectFieldThumbnailHeight);
            EditorGUI.BeginProperty(r, s_Styles.normalMapTexture, m_NormalMapTexture);
            EditorGUI.BeginChangeCheck();
            var normalMapTexture = EditorGUI.ObjectField(r, s_Styles.normalMapTexture, m_NormalMapTexture.objectReferenceValue as Texture2D, typeof(Texture2D), false) as Texture2D;
            if (EditorGUI.EndChangeCheck())
            {
                m_NormalMapTexture.objectReferenceValue = normalMapTexture;
                m_NormalMapHasCorrectTextureType = TerrainLayerUtility.CheckNormalMapTextureType(normalMapTexture);
            }
            EditorGUI.EndProperty();

            TerrainLayerUtility.ValidateNormalMapTextureUI(normalMapTexture, m_NormalMapHasCorrectTextureType);

            if (normalMapTexture != null)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_NormalScale);
                --EditorGUI.indentLevel;
                EditorGUILayout.Space();
            }

            if (m_ShowMaskMap)
            {
                r = EditorGUILayout.GetControlRect(true, EditorGUI.kObjectFieldThumbnailHeight);
                EditorGUI.BeginProperty(r, m_MaskMapText, m_MaskMapTexture);
                EditorGUI.BeginChangeCheck();
                var maskMapTexture = EditorGUI.ObjectField(r, m_MaskMapText, curMaskMap, typeof(Texture2D), false) as Texture2D;
                if (EditorGUI.EndChangeCheck())
                    m_MaskMapTexture.objectReferenceValue = maskMapTexture;
                EditorGUI.EndProperty();

                TerrainLayerUtility.ValidateMaskMapTextureUI(maskMapTexture);

                if (maskMapUsed)
                {
                    ++EditorGUI.indentLevel;
                    m_ShowMaskRemap = EditorGUILayout.Foldout(m_ShowMaskRemap, s_Styles.channelRemapping);
                    if (m_ShowMaskRemap)
                    {
                        DoMinMaxLabels(s_Styles.min, s_Styles.max, EditorStyles.miniLabel);
                        DoMinMaxFloatFields(m_MaskRemapRText, EditorStyles.miniLabel, m_MaskRemapMinR, m_MaskRemapMaxR, EditorStyles.miniTextField);
                        DoMinMaxFloatFields(m_MaskRemapGText, EditorStyles.miniLabel, m_MaskRemapMinG, m_MaskRemapMaxG, EditorStyles.miniTextField);
                        DoMinMaxFloatFields(m_MaskRemapBText, EditorStyles.miniLabel, m_MaskRemapMinB, m_MaskRemapMaxB, EditorStyles.miniTextField);
                        DoMinMaxFloatFields(m_MaskRemapAText, EditorStyles.miniLabel, m_MaskRemapMinA, m_MaskRemapMaxA, EditorStyles.miniTextField);
                    }
                    --EditorGUI.indentLevel;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Specular);
            EditorGUILayout.Slider(m_Metallic, 0.0f, 1.0f);
            EditorGUILayout.Slider(m_Smoothness, 0.0f, 1.0f);

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(m_TileSize, m_TileOffset);

            m_HasChanged |= EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
        }

        public override bool HasPreviewGUI()
        {
            if (target == null)
                return false;

            TerrainLayer t = (TerrainLayer)target;
            if (t.diffuseTexture == null)
                return false;

            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            Texture2D mask = (Texture2D)m_DiffuseTexture.objectReferenceValue;
            if (mask == null)
            {
                mask = EditorGUIUtility.whiteTexture;
                m_HasChanged = true;
            }

            int texWidth = Mathf.Min(mask.width, 256);
            int texHeight = Mathf.Min(mask.height, 256);

            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

            EditorGUI.DrawPreviewTexture(wantedRect, mask);

            m_Pos = PreviewGUI.EndScrollView();
        }

        public sealed override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            TerrainLayer t = AssetDatabase.LoadMainAssetAtPath(assetPath) as TerrainLayer;

            if (t == null || t.diffuseTexture == null)
                return null;

            int texwidth = t.diffuseTexture.width;
            int texheight = t.diffuseTexture.height;
            PreviewHelpers.AdjustWidthAndHeightForStaticPreview(texwidth, texheight, ref width, ref height);

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(t.diffuseTexture, tempRT);
            Texture2D previewTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = tempRT;
            previewTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            previewTexture.Apply();
            RenderTexture.ReleaseTemporary(tempRT);
            tempRT = null;
            RenderTexture.active = oldRT;
            return previewTexture;
        }
    }
}
