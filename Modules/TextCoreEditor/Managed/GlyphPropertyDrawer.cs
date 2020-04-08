// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using System.Collections;


namespace UnityEditor.TextCore
{
    [CustomPropertyDrawer(typeof(Glyph))]
    class GlyphPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_GlyphIndex = property.FindPropertyRelative("m_Index");
            SerializedProperty prop_GlyphMetrics = property.FindPropertyRelative("m_Metrics");
            SerializedProperty prop_GlyphRect = property.FindPropertyRelative("m_GlyphRect");
            SerializedProperty prop_Scale = property.FindPropertyRelative("m_Scale");
            SerializedProperty prop_AtlasIndex = property.FindPropertyRelative("m_AtlasIndex");

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            Rect rect = new Rect(position.x + 70, position.y, position.width, 49);

            float labelWidth = GUI.skin.label.CalcSize(new GUIContent("ID: " + prop_GlyphIndex.intValue)).x;
            EditorGUI.LabelField(new Rect(position.x + (64 - labelWidth) / 2, position.y + 85, 64f, 18f), new GUIContent("ID: <color=#FFFF80>" + prop_GlyphIndex.intValue + "</color>"), style);

            // We get Rect since a valid position may not be provided by the caller.
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, position.width, 49), prop_GlyphRect);

            rect.y += 45;
            EditorGUI.PropertyField(rect, prop_GlyphMetrics);

            EditorGUIUtility.labelWidth = 40f;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 65, 75, 18), prop_Scale, new GUIContent("Scale:"));

            EditorGUIUtility.labelWidth = 74f;
            EditorGUI.PropertyField(new Rect(rect.x + 85, rect.y + 65, 95, 18), prop_AtlasIndex, new GUIContent("Atlas Index:"));

            DrawGlyph(position, property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 130f;
        }

        void DrawGlyph(Rect position, SerializedProperty property)
        {
            // Serialized object can either be a FontAsset or a TMP_FontAsset
            SerializedObject so = property.serializedObject;

            if (so == null)
                return;

            // Get reference to the atlas texture for the given atlas index.
            int atlasIndex = property.FindPropertyRelative("m_AtlasIndex").intValue;

            SerializedProperty atlasTextureProperty = so.FindProperty("m_AtlasTextures");
            Texture2D atlasTexture = atlasTextureProperty.GetArrayElementAtIndex(atlasIndex).objectReferenceValue as Texture2D;
            if (atlasTexture == null)
                return;

            Material mat;

            GlyphRenderMode atlasRenderMode = (GlyphRenderMode)so.FindProperty("m_AtlasRenderMode").intValue;
            int atlasPadding = so.FindProperty("m_AtlasPadding").intValue;

            if (((GlyphRasterModes)atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                mat = FontAssetEditor.internalBitmapMaterial;

                if (mat == null)
                    return;

                mat.mainTexture = atlasTexture;
                mat.SetColor("_Color", Color.white);
            }
            else
            {
                mat = FontAssetEditor.internalSDFMaterial;

                if (mat == null)
                    return;

                mat.mainTexture = atlasTexture;
                mat.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + 1);
            }

            // Draw glyph from atlas texture.
            Rect glyphDrawPosition = new Rect(position.x, position.y + 2, 64, 80);

            SerializedProperty glyphRectProperty = property.FindPropertyRelative("m_GlyphRect");

            int padding = atlasPadding;
            int padding2X = padding * 2;

            int glyphOriginX = glyphRectProperty.FindPropertyRelative("m_X").intValue - padding;
            int glyphOriginY = glyphRectProperty.FindPropertyRelative("m_Y").intValue - padding;
            int glyphWidth = glyphRectProperty.FindPropertyRelative("m_Width").intValue + padding2X;
            int glyphHeight = glyphRectProperty.FindPropertyRelative("m_Height").intValue + padding2X;

            SerializedProperty faceInfoProperty = so.FindProperty("m_FaceInfo");
            float ascentLine = faceInfoProperty.FindPropertyRelative("m_AscentLine").floatValue;
            float descentLine = faceInfoProperty.FindPropertyRelative("m_DescentLine").floatValue;

            float normalizedHeight = ascentLine - descentLine;
            float scale = glyphDrawPosition.width / normalizedHeight;

            // Compute the normalized texture coordinates
            Rect texCoords = new Rect((float)glyphOriginX / atlasTexture.width, (float)glyphOriginY / atlasTexture.height, (float)glyphWidth / atlasTexture.width, (float)glyphHeight / atlasTexture.height);

            if (Event.current.type == EventType.Repaint)
            {
                glyphDrawPosition.x += (glyphDrawPosition.width - glyphWidth * scale) / 2;
                glyphDrawPosition.y += (glyphDrawPosition.height - glyphHeight * scale) / 2;
                glyphDrawPosition.width = glyphWidth * scale;
                glyphDrawPosition.height = glyphHeight * scale;

                // Could switch to using the default material of the font asset which would require passing scale to the shader.
                Graphics.DrawTexture(glyphDrawPosition, atlasTexture, texCoords, 0, 0, 0, 0, new Color(1f, 1f, 1f), mat);
            }
        }
    }
}
