// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(MarkToBaseAdjustmentRecord))]
    internal class MarkToBaseAdjustmentRecordPropertyDrawer : PropertyDrawer
    {
        private Dictionary<uint, GlyphProxy> m_GlyphLookupDictionary;

        private bool isEditingEnabled = false;
        private bool isSelectable = false;

        private string m_PreviousInput;

        //static GUIContent s_CharacterTextFieldLabel = new GUIContent("Char:", "Enter the character or its UTF16 or UTF32 Unicode character escape sequence. For UTF16 use \"\\uFF00\" and for UTF32 use \"\\UFF00FF00\" representation.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_BaseGlyphID = property.FindPropertyRelative("m_BaseGlyphID");
            SerializedProperty prop_BaseGlyphAnchorPoint = property.FindPropertyRelative("m_BaseGlyphAnchorPoint");

            SerializedProperty prop_MarkGlyphID = property.FindPropertyRelative("m_MarkGlyphID");
            SerializedProperty prop_MarkAdjustmentRecord = property.FindPropertyRelative("m_MarkPositionAdjustment");

            // Refresh glyph proxy lookup dictionary if needed.
            if (TextCorePropertyDrawerUtilities.s_RefreshGlyphProxyLookup)
                TextCorePropertyDrawerUtilities.RefreshGlyphProxyLookup(property.serializedObject);

            position.yMin += 2;

            float width = position.width / 2;
            float padding = 5.0f;

            Rect rect;

            isEditingEnabled = GUI.enabled;
            isSelectable = label.text == "Selectable";

            if (isSelectable)
                GUILayoutUtility.GetRect(position.width, 75);
            else
                GUILayoutUtility.GetRect(position.width, 55);

            GUIStyle style = new GUIStyle(EditorStyles.label) {richText = true};

            // Base Glyph
            GUI.enabled = isEditingEnabled;
            if (isSelectable)
            {
                float labelWidth = GUI.skin.label.CalcSize(new GUIContent("ID: " + prop_BaseGlyphID.intValue)).x;

                if (!isEditingEnabled)
                {
                    EditorGUI.LabelField(new Rect(position.x + (64 - labelWidth) / 2, position.y + 60, 64f, 18f), new GUIContent("ID: <color=#FFFF80>" + prop_BaseGlyphID.intValue + "</color>"), style);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUIUtility.labelWidth = 25f;
                    EditorGUI.DelayedIntField(new Rect(position.x + (64 - labelWidth) / 2, position.y + 60, 64, 18), prop_BaseGlyphID, new GUIContent("ID:"));
                    if (EditorGUI.EndChangeCheck())
                    {

                    }
                }

                GUI.enabled = isEditingEnabled;
                EditorGUIUtility.labelWidth = 30f;

                rect = new Rect(position.x + 70, position.y + 10, (width - 70) - padding, 18);
                EditorGUI.PropertyField(rect, prop_BaseGlyphAnchorPoint.FindPropertyRelative("m_XCoordinate"), new GUIContent("X:"));

                rect.y += 20;
                EditorGUI.PropertyField(rect, prop_BaseGlyphAnchorPoint.FindPropertyRelative("m_YCoordinate"), new GUIContent("Y:"));

                DrawGlyph((uint)prop_BaseGlyphID.intValue, new Rect(position.x, position.y + 2, 64, 60), property);
            }

            // Mark Glyph
            GUI.enabled = isEditingEnabled;
            if (isSelectable)
            {
                float labelWidth = GUI.skin.label.CalcSize(new GUIContent("ID: " + prop_MarkGlyphID.intValue)).x;

                if (!isEditingEnabled)
                {
                    EditorGUI.LabelField(new Rect(position.width / 2 + 20 + (64 - labelWidth) / 2, position.y + 60, 64f, 18f), new GUIContent("ID: <color=#FFFF80>" + prop_MarkGlyphID.intValue + "</color>"), style);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUIUtility.labelWidth = 25f;
                    EditorGUI.DelayedIntField(new Rect(position.width / 2 + 20 + (64 - labelWidth) / 2, position.y + 60, 64, 18), prop_MarkGlyphID, new GUIContent("ID:"));
                    if (EditorGUI.EndChangeCheck())
                    {

                    }
                }

                GUI.enabled = isEditingEnabled;
                EditorGUIUtility.labelWidth = 30f;

                rect = new Rect(position.width / 2 + 20 + 70, position.y + 10, (width - 70) - padding, 18);
                EditorGUI.PropertyField(rect, prop_MarkAdjustmentRecord.FindPropertyRelative("m_XPositionAdjustment"), new GUIContent("X:"));

                rect.y += 20;
                EditorGUI.PropertyField(rect, prop_MarkAdjustmentRecord.FindPropertyRelative("m_YPositionAdjustment"), new GUIContent("Y:"));

                DrawGlyph((uint)prop_MarkGlyphID.intValue, new Rect(position.width / 2 + 20, position.y + 2, 64, 60), property);
            }
        }

        bool ValidateInput(string source)
        {
            int length = string.IsNullOrEmpty(source) ? 0 : source.Length;

            ////Filter out unwanted characters.
            Event evt = Event.current;

            char c = evt.character;

            if (c != '\0')
            {
                switch (length)
                {
                    case 0:
                        break;
                    case 1:
                        if (source != m_PreviousInput)
                            return true;

                        if ((source[0] == '\\' && (c == 'u' || c == 'U')) == false)
                            evt.character = '\0';

                        break;
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
                            evt.character = '\0';
                        break;
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        if (source[1] == 'u' || (c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
                            evt.character = '\0';

                        // Validate input
                        if (length == 6 && source[1] == 'u' && source != m_PreviousInput)
                            return true;
                        break;
                    case 10:
                        if (source != m_PreviousInput)
                            return true;

                        evt.character = '\0';
                        break;
                }
            }

            m_PreviousInput = source;

            return false;
        }

        uint GetUnicodeCharacter (string source)
        {
            uint unicode;

            if (source.Length == 1)
                unicode = source[0];
            else if (source.Length == 6)
                unicode = (uint)TextUtilities.StringHexToInt(source.Replace("\\u", ""));
            else
                unicode = (uint)TextUtilities.StringHexToInt(source.Replace("\\U", ""));

            return unicode;
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
