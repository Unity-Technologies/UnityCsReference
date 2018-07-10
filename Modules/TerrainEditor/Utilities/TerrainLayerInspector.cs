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
    internal class TerrainLayerInspector : Editor
    {
        [MenuItem("Assets/Create/Terrain Layer")]
        public static void CreateNewDefaultTerrainLayer()
        {
            TerrainLayer terrainLayer = new TerrainLayer();
            ProjectWindowUtil.CreateAsset(terrainLayer, "Untitled.terrainlayer");
        }

        [SerializeField]
        protected Vector2 m_Pos;

        SerializedProperty m_DiffuseTexture;
        SerializedProperty m_NormalMapTexture;
        SerializedProperty m_TileSize;
        SerializedProperty m_TileOffset;
        SerializedProperty m_Specular;
        SerializedProperty m_Metallic;
        SerializedProperty m_Smoothness;

        bool m_HasChanged = true;
        bool m_NormalMapHasCorrectTextureType;

        void CheckIfNormalMapHasCorrectTextureType()
        {
            string assetPath = AssetDatabase.GetAssetPath(m_NormalMapTexture.objectReferenceValue);
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

        bool ValidateTextures()
        {
            Texture2D diffuseTex = (Texture2D)m_DiffuseTexture.objectReferenceValue;
            if (diffuseTex == null)
            {
                EditorGUILayout.HelpBox("Assign a tiling texture", MessageType.Warning);
                return false;
            }
            if (diffuseTex.wrapMode != TextureWrapMode.Repeat)
            {
                // Base map generation does not support TextureWrapMode.Clamp properly
                EditorGUILayout.HelpBox("Texture wrap mode must be set to Repeat", MessageType.Warning);
                return false;
            }
            if (diffuseTex.width != Mathf.ClosestPowerOfTwo(diffuseTex.width) || diffuseTex.height != Mathf.ClosestPowerOfTwo(diffuseTex.height))
            {
                // power of two needed for efficient base map generation
                EditorGUILayout.HelpBox("Texture size must be power of two", MessageType.Warning);
                return false;
            }
            if (diffuseTex.mipmapCount <= 1)
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

        internal virtual void OnEnable()
        {
            if (!target)
                return;

            m_DiffuseTexture = serializedObject.FindProperty("m_DiffuseTexture");
            m_NormalMapTexture = serializedObject.FindProperty("m_NormalMapTexture");
            m_TileSize = serializedObject.FindProperty("m_TileSize");
            m_TileOffset = serializedObject.FindProperty("m_TileOffset");
            m_Specular = serializedObject.FindProperty("m_Specular");
            m_Metallic = serializedObject.FindProperty("m_Metallic");
            m_Smoothness = serializedObject.FindProperty("m_Smoothness");

            CheckIfNormalMapHasCorrectTextureType();
        }

        public override void OnInspectorGUI()
        {
            if (!target)
                return;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            Texture2D diffuseTexture = (Texture2D)EditorGUILayout.ObjectField(EditorGUIUtility.TextContent("Diffuse texture"), (Texture2D)m_DiffuseTexture.objectReferenceValue, typeof(Texture2D), false);
            Texture2D normalMapTexture = (Texture2D)EditorGUILayout.ObjectField(EditorGUIUtility.TextContent("Normal map texture"), (Texture2D)m_NormalMapTexture.objectReferenceValue, typeof(Texture2D), false);

            if (normalMapTexture != (Texture2D)m_NormalMapTexture.objectReferenceValue)
            {
                m_NormalMapTexture.objectReferenceValue = normalMapTexture;
                CheckIfNormalMapHasCorrectTextureType();
            }

            m_DiffuseTexture.objectReferenceValue = diffuseTexture;

            ValidateTextures();

            GUILayoutOption kWidth10 = GUILayout.Width(10);
            GUILayoutOption kMinWidth32 = GUILayout.MinWidth(32);

            GUILayout.Space(6);

            Vector2 tileSize = m_TileSize.vector2Value;
            Vector2 tileOffset = m_TileOffset.vector2Value;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("", EditorStyles.miniLabel, kWidth10);
            GUILayout.Label("x", EditorStyles.miniLabel, kWidth10);
            GUILayout.Label("y", EditorStyles.miniLabel, kWidth10);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Size", EditorStyles.miniLabel);
            tileSize.x = EditorGUILayout.FloatField(tileSize.x, EditorStyles.miniTextField, kMinWidth32);
            tileSize.y = EditorGUILayout.FloatField(tileSize.y, EditorStyles.miniTextField, kMinWidth32);
            m_TileSize.vector2Value = tileSize;
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Offset", EditorStyles.miniLabel);
            tileOffset.x = EditorGUILayout.FloatField(tileOffset.x, EditorStyles.miniTextField, kMinWidth32);
            tileOffset.y = EditorGUILayout.FloatField(tileOffset.y, EditorStyles.miniTextField, kMinWidth32);
            m_TileOffset.vector2Value = tileOffset;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            m_Specular.colorValue = EditorGUILayout.ColorField("Specular", m_Specular.colorValue);
            m_Metallic.floatValue = EditorGUILayout.Slider("Metallic", m_Metallic.floatValue, 0.0f, 1.0f);
            m_Smoothness.floatValue = EditorGUILayout.Slider("Smoothness", m_Smoothness.floatValue, 0.0f, 1.0f);

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
