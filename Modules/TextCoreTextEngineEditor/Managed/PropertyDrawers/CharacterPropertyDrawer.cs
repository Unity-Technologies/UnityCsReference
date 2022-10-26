// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(Character))]
    internal class CharacterPropertyDrawer : PropertyDrawer
    {
        private Dictionary<uint, GlyphProxy> m_GlyphLookupDictionary;
        int m_GlyphSelectedForEditing = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_Unicode = property.FindPropertyRelative("m_Unicode");
            SerializedProperty prop_GlyphIndex = property.FindPropertyRelative("m_GlyphIndex");
            SerializedProperty prop_Scale = property.FindPropertyRelative("m_Scale");

            // Refresh glyph proxy lookup dictionary if needed.
            if (TextCorePropertyDrawerUtilities.s_RefreshGlyphProxyLookup)
                TextCorePropertyDrawerUtilities.RefreshGlyphProxyLookup(property.serializedObject);

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            EditorGUIUtility.labelWidth = 40f;
            EditorGUIUtility.fieldWidth = 50;

            Rect rect = new Rect(position.x + 50, position.y, position.width, 49);

            // Display non-editable fields
            if (GUI.enabled == false)
            {
                int unicode = prop_Unicode.intValue;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 120f, 18), new GUIContent("Unicode: <color=#FFFF80>0x" + unicode.ToString("X") + "</color>"), style);
                EditorGUI.LabelField(new Rect(rect.x + 115, rect.y, 120f, 18), unicode <= 0xFFFF ? new GUIContent("UTF16: <color=#FFFF80>\\u" + unicode.ToString("X4") + "</color>") : new GUIContent("UTF32: <color=#FFFF80>\\U" + unicode.ToString("X8") + "</color>"), style);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + 18, 120, 18), new GUIContent("Glyph ID: <color=#FFFF80>" + prop_GlyphIndex.intValue + "</color>"), style);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + 36, 80, 18), new GUIContent("Scale: <color=#FFFF80>" + prop_Scale.floatValue + "</color>"), style);

                // Draw Glyph (if exists)
                DrawGlyph(prop_GlyphIndex.uintValue, new Rect(position.x, position.y, 48, 58), property);
            }
            else // Display editable fields
            {
                EditorGUIUtility.labelWidth = 55f;
                GUI.SetNextControlName("Unicode Input");
                EditorGUI.BeginChangeCheck();
                string unicode = EditorGUI.TextField(new Rect(rect.x, rect.y, 120, 18), "Unicode:", prop_Unicode.intValue.ToString("X"));

                if (GUI.GetNameOfFocusedControl() == "Unicode Input")
                {
                    //Filter out unwanted characters.
                    char chr = Event.current.character;
                    if ((chr < '0' || chr > '9') && (chr < 'a' || chr > 'f') && (chr < 'A' || chr > 'F'))
                    {
                        Event.current.character = '\0';
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    // Update Unicode value
                    prop_Unicode.intValue = (int)TextUtilities.StringHexToInt(unicode);
                }

                // Cache current glyph index in case it needs to be restored if the new glyph index is invalid.
                int currentGlyphIndex = prop_GlyphIndex.intValue;

                EditorGUIUtility.labelWidth = 59f;
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedIntField(new Rect(rect.x, rect.y + 18, 100, 18), prop_GlyphIndex, new GUIContent("Glyph ID:"));
                if (EditorGUI.EndChangeCheck())
                {
                    // Get a reference to the font asset
                    FontAsset fontAsset = property.serializedObject.targetObject as FontAsset;

                    // Make sure new glyph index is valid.
                    int elementIndex = fontAsset.glyphTable.FindIndex(item => item.index == prop_GlyphIndex.intValue);

                    if (elementIndex == -1)
                        prop_GlyphIndex.intValue = currentGlyphIndex;
                    else
                        fontAsset.IsFontAssetLookupTablesDirty = true;
                }

                int glyphIndex = prop_GlyphIndex.intValue;

                // Reset glyph selection if new character has been selected.
                if (GUI.enabled && m_GlyphSelectedForEditing != glyphIndex)
                    m_GlyphSelectedForEditing = -1;

                // Display button to edit the glyph data.
                if (GUI.Button(new Rect(rect.x + 120, rect.y + 18, 75, 18), new GUIContent("Edit Glyph")))
                {
                    if (m_GlyphSelectedForEditing == -1)
                        m_GlyphSelectedForEditing = glyphIndex;
                    else
                        m_GlyphSelectedForEditing = -1;

                    // Button clicks should not result in potential change.
                    GUI.changed = false;
                }

                // Show the glyph property drawer if selected
                if (glyphIndex == m_GlyphSelectedForEditing && GUI.enabled)
                {
                    // Get a reference to the font asset
                    FontAsset fontAsset = property.serializedObject.targetObject as FontAsset;

                    if (fontAsset != null)
                    {
                        // Get the index of the glyph in the font asset glyph table.
                        int elementIndex = fontAsset.glyphTable.FindIndex(item => item.index == glyphIndex);

                        if (elementIndex != -1)
                        {
                            SerializedProperty prop_GlyphTable = property.serializedObject.FindProperty("m_GlyphTable");
                            SerializedProperty prop_Glyph = prop_GlyphTable.GetArrayElementAtIndex(elementIndex);

                            SerializedProperty prop_GlyphMetrics = prop_Glyph.FindPropertyRelative("m_Metrics");
                            SerializedProperty prop_GlyphRect = prop_Glyph.FindPropertyRelative("m_GlyphRect");

                            Rect newRect = EditorGUILayout.GetControlRect(false, 115);
                            EditorGUI.DrawRect(new Rect(newRect.x + 52, newRect.y - 20, newRect.width - 52, newRect.height - 5), new Color(0.1f, 0.1f, 0.1f, 0.45f));
                            EditorGUI.DrawRect(new Rect(newRect.x + 53, newRect.y - 19, newRect.width - 54, newRect.height - 7), new Color(0.3f, 0.3f, 0.3f, 0.8f));

                            // Display GlyphRect
                            newRect.x += 55;
                            newRect.y -= 18;
                            newRect.width += 5;
                            EditorGUI.PropertyField(newRect, prop_GlyphRect);

                            // Display GlyphMetrics
                            newRect.y += 45;
                            EditorGUI.PropertyField(newRect, prop_GlyphMetrics);

                            rect.y += 120;
                        }
                    }
                }

                EditorGUIUtility.labelWidth = 39f;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + 36, 80, 18), prop_Scale, new GUIContent("Scale:"));

                // Draw Glyph (if exists)
                DrawGlyph(prop_GlyphIndex.uintValue, new Rect(position.x, position.y, 48, 58), property);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 58;
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
