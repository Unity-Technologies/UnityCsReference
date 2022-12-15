// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(LigatureSubstitutionRecord))]
    internal class LigatureSubstitutionRecordPropertyDrawer : PropertyDrawer
    {
        private Dictionary<uint, GlyphProxy> m_GlyphLookupDictionary;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_ComponentGlyphIDs = property.FindPropertyRelative("m_ComponentGlyphIDs");
            int ComponentGlyphIDCount = prop_ComponentGlyphIDs.arraySize;
            SerializedProperty prop_LigatureGlyphID = property.FindPropertyRelative("m_LigatureGlyphID");

            // Refresh glyph proxy lookup dictionary if needed.
            if (TextCorePropertyDrawerUtilities.s_RefreshGlyphProxyLookup)
                TextCorePropertyDrawerUtilities.RefreshGlyphProxyLookup(property.serializedObject);

            Rect rect = position;
            GUILayoutUtility.GetRect(position.width, 100);

            EditorGUIUtility.labelWidth = 115;
            EditorGUI.BeginChangeCheck();
            int size = EditorGUI.DelayedIntField(new Rect(rect.x, position.y + 3, 130, rect.height), new GUIContent("Component Glyphs"), prop_ComponentGlyphIDs.arraySize);
            if (EditorGUI.EndChangeCheck())
            {
                size = Mathf.Clamp(size, 0, 20);
                prop_ComponentGlyphIDs.arraySize = size;
                return;
            }

            // Spacing between glyphs
            int glyphSpacing = 60;

            // Draw Component Glyphs
            for (int i = 0; i < ComponentGlyphIDCount; i++)
            {
                Rect componentGlyphPosition = new Rect(50 + (glyphSpacing * i), position.y + 24, 48, 48);

                // Draw glyph
                uint glyphIndex = (uint)prop_ComponentGlyphIDs.GetArrayElementAtIndex(i).intValue;
                DrawGlyph(glyphIndex, componentGlyphPosition, property);

                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedIntField(new Rect(componentGlyphPosition.x - 13, componentGlyphPosition.y + 73, 40, EditorGUIUtility.singleLineHeight), prop_ComponentGlyphIDs.GetArrayElementAtIndex(i), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {

                }
            }

            // Draw Ligature glyph
            Rect ligatureGlyphPosition = new Rect(50 + (glyphSpacing * ComponentGlyphIDCount + 1), position.y + 3, 95, EditorGUIUtility.singleLineHeight);
            ligatureGlyphPosition.x = Mathf.Max(200, ligatureGlyphPosition.x);
            EditorGUI.LabelField(ligatureGlyphPosition, new GUIContent("Ligature Glyph"));

            DrawGlyph((uint)prop_LigatureGlyphID.intValue, new Rect(ligatureGlyphPosition.x + 37, ligatureGlyphPosition.y + 21, 48, 48), property);

            EditorGUI.BeginChangeCheck();
            EditorGUI.DelayedIntField(new Rect(ligatureGlyphPosition.x + 24, ligatureGlyphPosition.y + 94, 40, EditorGUIUtility.singleLineHeight), prop_LigatureGlyphID, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {

            }
        }

        void DrawGlyph(uint glyphIndex, Rect glyphDrawPosition, SerializedProperty property)
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
                glyphDrawPosition.x += -(glyphWidth * scale / 2 - padding * scale);
                glyphDrawPosition.y += glyphDrawPosition.height - glyph.metrics.horizontalBearingY * scale;
                glyphDrawPosition.width = glyphWidth * scale;
                glyphDrawPosition.height = glyphHeight * scale;

                // Could switch to using the default material of the font asset which would require passing scale to the shader.
                Graphics.DrawTexture(glyphDrawPosition, atlasTexture, texCoords, 0, 0, 0, 0, new Color(1f, 1f, 1f), mat);
            }
        }
    }
}
