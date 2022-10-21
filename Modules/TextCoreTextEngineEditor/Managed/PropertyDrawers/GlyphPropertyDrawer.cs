// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(Glyph))]
    internal class GlyphPropertyDrawer : PropertyDrawer
    {
        private Dictionary<uint, GlyphProxy> m_GlyphLookupDictionary;

        private static readonly GUIContent k_ScaleLabel = new GUIContent("Scale:", "The scale of this glyph.");
        private static readonly GUIContent k_AtlasIndexLabel = new GUIContent("Atlas Index:", "The index of the atlas texture that contains this glyph.");
        private static readonly GUIContent k_ClassTypeLabel = new GUIContent("Class Type:", "The class definition type of this glyph.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_GlyphIndex = property.FindPropertyRelative("m_Index");
            SerializedProperty prop_GlyphMetrics = property.FindPropertyRelative("m_Metrics");
            SerializedProperty prop_GlyphRect = property.FindPropertyRelative("m_GlyphRect");
            SerializedProperty prop_Scale = property.FindPropertyRelative("m_Scale");
            SerializedProperty prop_AtlasIndex = property.FindPropertyRelative("m_AtlasIndex");
            SerializedProperty prop_ClassDefinitionType = property.FindPropertyRelative("m_ClassDefinitionType");

            // Refresh glyph proxy lookup dictionary if needed.
            if (TextCorePropertyDrawerUtilities.s_RefreshGlyphProxyLookup)
                TextCorePropertyDrawerUtilities.RefreshGlyphProxyLookup(property.serializedObject);

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
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 65, 75, 18), prop_Scale, k_ScaleLabel);

            EditorGUIUtility.labelWidth = 70f;
            EditorGUI.PropertyField(new Rect(rect.x + 85, rect.y + 65, 95, 18), prop_AtlasIndex, k_AtlasIndexLabel);

            if (prop_ClassDefinitionType != null)
            {
                EditorGUIUtility.labelWidth = 70f;
                float minWidth = Mathf.Max(90, rect.width - 270);
                EditorGUI.PropertyField(new Rect(rect.x + 190, rect.y + 65, minWidth, 18), prop_ClassDefinitionType, k_ClassTypeLabel);
            }

            DrawGlyph(prop_GlyphIndex.uintValue, new Rect(position.x, position.y + 10, 64, 80), property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 130f;
        }

        void DrawGlyph(uint glyphIndex, Rect position, SerializedProperty property)
        {
            // Get a reference to the serialized object which can either be a TMP_FontAsset or FontAsset.
            SerializedObject so = property.serializedObject;
            if (so == null)
                return;

            if (m_GlyphLookupDictionary == null)
                m_GlyphLookupDictionary = TextCorePropertyDrawerUtilities.GetGlyphProxyLookupDictionary(so);

            // Try getting a reference to the glyph for the given glyph index.
            if (!m_GlyphLookupDictionary.TryGetValue(glyphIndex, out GlyphProxy glyph))
                return;

            Texture2D atlasTexture;
            if (TextCorePropertyDrawerUtilities.TryGetAtlasTextureFromSerializedObject(so, glyph.atlasIndex, out atlasTexture) == false)
                return;

            Material mat;
            if (TextCorePropertyDrawerUtilities.TryGetMaterial(so, atlasTexture, out mat) == false)
                return;

            // Draw glyph from atlas texture.
            Rect glyphDrawPosition = position;

            int padding = so.FindProperty("m_AtlasPadding").intValue;
            GlyphRect glyphRect = glyph.glyphRect;
            int glyphOriginX = glyphRect.x - padding;
            int glyphOriginY = glyphRect.y - padding;
            int glyphWidth = glyphRect.width + padding * 2;
            int glyphHeight = glyphRect.height + padding * 2;

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
