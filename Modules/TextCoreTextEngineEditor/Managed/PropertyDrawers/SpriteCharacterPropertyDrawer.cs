// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(SpriteCharacter))]
    internal class SpriteCharacterPropertyDrawer : PropertyDrawer
    {
        int m_GlyphSelectedForEditing = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_SpriteName = property.FindPropertyRelative("m_Name");
            SerializedProperty prop_SpriteUnicode = property.FindPropertyRelative("m_Unicode");
            SerializedProperty prop_SpriteGlyphIndex = property.FindPropertyRelative("m_GlyphIndex");
            SerializedProperty prop_SpriteScale = property.FindPropertyRelative("m_Scale");


            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            EditorGUIUtility.labelWidth = 40f;
            EditorGUIUtility.fieldWidth = 50;

            Rect rect = new Rect(position.x + 60, position.y, position.width, 49);

            // Display non-editable fields
            if (GUI.enabled == false)
            {
                // Sprite Character Index
                int spriteCharacterIndex;
                int.TryParse(property.displayName.Split(' ')[1], out spriteCharacterIndex);
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 75f, 18), new GUIContent("Index: <color=#FFFF80>" + spriteCharacterIndex + "</color>"), style);

                EditorGUI.LabelField(new Rect(rect.x + 75f, rect.y, 120f, 18), new GUIContent("Unicode: <color=#FFFF80>0x" + prop_SpriteUnicode.intValue.ToString("X") + "</color>"), style);
                EditorGUI.LabelField(new Rect(rect.x + 195f, rect.y, rect.width - 255, 18), new GUIContent("Name: <color=#FFFF80>" + prop_SpriteName.stringValue + "</color>"), style);

                EditorGUI.LabelField(new Rect(rect.x, rect.y + 18, 120, 18), new GUIContent("Glyph ID: <color=#FFFF80>" + prop_SpriteGlyphIndex.intValue + "</color>"), style);

                // Draw Sprite Glyph (if exists)
                DrawSpriteGlyph(position, property);

                EditorGUI.LabelField(new Rect(rect.x, rect.y + 36, 80, 18), new GUIContent("Scale: <color=#FFFF80>" + prop_SpriteScale.floatValue + "</color>"), style);
            }
            else // Display editable fields
            {
                // Get a reference to the underlying Sprite Asset
                SpriteAsset spriteAsset = property.serializedObject.targetObject as SpriteAsset;

                // Sprite Character Index
                int spriteCharacterIndex;
                int.TryParse(property.displayName.Split(' ')[1], out spriteCharacterIndex);

                EditorGUI.LabelField(new Rect(rect.x, rect.y, 75f, 18), new GUIContent("Index: <color=#FFFF80>" + spriteCharacterIndex + "</color>"), style);

                EditorGUIUtility.labelWidth = 55f;
                GUI.SetNextControlName("Unicode Input");
                EditorGUI.BeginChangeCheck();
                string unicode = EditorGUI.DelayedTextField(new Rect(rect.x + 75f, rect.y, 120, 18), "Unicode:", prop_SpriteUnicode.intValue.ToString("X"));

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
                    prop_SpriteUnicode.intValue = (int)TextUtilities.StringHexToInt(unicode);
                    spriteAsset.m_IsSpriteAssetLookupTablesDirty = true;
                }

                EditorGUIUtility.labelWidth = 41f;
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedTextField(new Rect(rect.x + 195f, rect.y, rect.width - 255, 18), prop_SpriteName, new GUIContent("Name:"));
                if (EditorGUI.EndChangeCheck())
                {
                    spriteAsset.m_IsSpriteAssetLookupTablesDirty = true;
                }

                EditorGUIUtility.labelWidth = 59f;
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedIntField(new Rect(rect.x, rect.y + 18, 100, 18), prop_SpriteGlyphIndex, new GUIContent("Glyph ID:"));
                if (EditorGUI.EndChangeCheck())
                {
                    spriteAsset.m_IsSpriteAssetLookupTablesDirty = true;
                }

                // Draw Sprite Glyph (if exists)
                DrawSpriteGlyph(position, property);

                int glyphIndex = prop_SpriteGlyphIndex.intValue;

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
                    if (spriteAsset != null)
                    {
                        SerializedProperty prop_SpriteGlyph = FindSpriteGlyphProperty(property.serializedObject, (uint)glyphIndex);

                        if (prop_SpriteGlyph != null)
                        {
                            SerializedProperty prop_GlyphMetrics = prop_SpriteGlyph.FindPropertyRelative("m_Metrics");
                            SerializedProperty prop_GlyphRect = prop_SpriteGlyph.FindPropertyRelative("m_GlyphRect");

                            Rect newRect = EditorGUILayout.GetControlRect(false, 115);
                            EditorGUI.DrawRect(new Rect(newRect.x + 62, newRect.y - 20, newRect.width - 62, newRect.height - 5), new Color(0.1f, 0.1f, 0.1f, 0.45f));
                            EditorGUI.DrawRect(new Rect(newRect.x + 63, newRect.y - 19, newRect.width - 64, newRect.height - 7), new Color(0.3f, 0.3f, 0.3f, 0.8f));

                            // Display GlyphRect
                            newRect.x += 65;
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
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + 36, 80, 18), prop_SpriteScale, new GUIContent("Scale:"));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 58;
        }

        internal static SerializedProperty FindSpriteGlyphProperty(SerializedObject serializedObject, uint glyphIndex)
        {
            SerializedProperty prop_SpriteGlyphTable = serializedObject.FindProperty("m_SpriteGlyphTable");
            int glyphCount = prop_SpriteGlyphTable.arraySize;

            // The live glyph table can diverge from the serialized one, so its index is only a hint.
            SpriteAsset spriteAsset = serializedObject.targetObject as SpriteAsset;
            int hintIndex = spriteAsset != null ? spriteAsset.spriteGlyphTable.FindIndex(item => item.index == glyphIndex) : -1;
            if (hintIndex >= 0 && hintIndex < glyphCount)
            {
                SerializedProperty prop_SpriteGlyph = prop_SpriteGlyphTable.GetArrayElementAtIndex(hintIndex);
                if ((uint)prop_SpriteGlyph.FindPropertyRelative("m_Index").intValue == glyphIndex)
                    return prop_SpriteGlyph;
            }

            for (int i = 0; i < glyphCount; i++)
            {
                SerializedProperty prop_SpriteGlyph = prop_SpriteGlyphTable.GetArrayElementAtIndex(i);
                if ((uint)prop_SpriteGlyph.FindPropertyRelative("m_Index").intValue == glyphIndex)
                    return prop_SpriteGlyph;
            }

            return null;
        }

        void DrawSpriteGlyph(Rect position, SerializedProperty property)
        {
            SpriteAsset spriteAsset = property.serializedObject.targetObject as SpriteAsset;

            if (spriteAsset == null)
                return;

            SerializedProperty prop_SpriteGlyph = FindSpriteGlyphProperty(property.serializedObject, (uint)property.FindPropertyRelative("m_GlyphIndex").intValue);

            if (prop_SpriteGlyph == null)
                return;

            Texture tex = spriteAsset.spriteSheet;

            if (tex == null)
            {
                Debug.LogWarning("Please assign a valid Sprite Atlas texture to the [" + spriteAsset.name + "] Sprite Asset.", spriteAsset);
                return;
            }

            GlyphRect glyphRect = TextCorePropertyDrawerUtilities.GetGlyphRectFromGlyphSerializedProperty(prop_SpriteGlyph);

            Vector2 spriteTexPosition = new Vector2(position.x, position.y);
            Vector2 spriteSize = new Vector2(48, 48);
            Vector2 alignmentOffset = new Vector2((58 - spriteSize.x) / 2, (58 - spriteSize.y) / 2);

            float x = glyphRect.x;
            float y = glyphRect.y;
            float spriteWidth = glyphRect.width;
            float spriteHeight = glyphRect.height;

            if (spriteWidth >= spriteHeight)
            {
                spriteSize.y = spriteHeight * spriteSize.x / spriteWidth;
                spriteTexPosition.y += (spriteSize.x - spriteSize.y) / 2;
            }
            else
            {
                spriteSize.x = spriteWidth * spriteSize.y / spriteHeight;
                spriteTexPosition.x += (spriteSize.y - spriteSize.x) / 2;
            }

            // Compute the normalized texture coordinates
            Rect texCoords = new Rect(x / tex.width, y / tex.height, spriteWidth / tex.width, spriteHeight / tex.height);
            GUI.DrawTextureWithTexCoords(new Rect(spriteTexPosition.x + alignmentOffset.x, spriteTexPosition.y + alignmentOffset.y, spriteSize.x, spriteSize.y), tex, texCoords, true);
        }
    }
}
