// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(MarkToBaseAdjustmentRecord))]
    internal class MarkToBaseAdjustmentRecordPropertyDrawer : PropertyDrawer
    {
        private bool isEditingEnabled = false;
        private bool isSelectable = false;

        private string m_PreviousInput;

        //private FontAsset m_FontAsset;

        static GUIContent s_CharacterTextFieldLabel = new GUIContent("Char:", "Enter the character or its UTF16 or UTF32 Unicode character escape sequence. For UTF16 use \"\\uFF00\" and for UTF32 use \"\\UFF00FF00\" representation.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_BaseGlyphID = property.FindPropertyRelative("m_BaseGlyphID");
            SerializedProperty prop_BaseGlyphAnchorPoint = property.FindPropertyRelative("m_BaseGlyphAnchorPoint");

            SerializedProperty prop_MarkGlyphID = property.FindPropertyRelative("m_MarkGlyphID");
            SerializedProperty prop_MarkAdjustmentRecord = property.FindPropertyRelative("m_MarkPositionAdjustment");

            position.yMin += 2;

            float width = position.width / 2;
            float padding = 5.0f;

            Rect rect;

            isEditingEnabled = GUI.enabled;
            isSelectable = label.text == "Selectable" ? true : false;

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
                        FontAsset fontAsset = property.serializedObject.targetObject as FontAsset;
                        if (fontAsset != null)
                        {
                            property.serializedObject.ApplyModifiedProperties();
                            fontAsset.ReadFontAssetDefinition();
                            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
                        }
                    }
                }

                GUI.enabled = isEditingEnabled;
                EditorGUIUtility.labelWidth = 30f;

                rect = new Rect(position.x + 70, position.y + 10, (width - 70) - padding, 18);
                EditorGUI.PropertyField(rect, prop_BaseGlyphAnchorPoint.FindPropertyRelative("m_XCoordinate"), new GUIContent("X:"));

                rect.y += 20;
                EditorGUI.PropertyField(rect, prop_BaseGlyphAnchorPoint.FindPropertyRelative("m_YCoordinate"), new GUIContent("Y:"));

                DrawGlyph((uint)prop_BaseGlyphID.intValue, new Rect(position.x, position.y, position.width, position.height), property);
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
                         FontAsset fontAsset = property.serializedObject.targetObject as FontAsset;
                         if (fontAsset != null)
                         {
                             property.serializedObject.ApplyModifiedProperties();
                             fontAsset.ReadFontAssetDefinition();
                             TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
                         }
                    }
                }

                GUI.enabled = isEditingEnabled;
                EditorGUIUtility.labelWidth = 30f;

                rect = new Rect(position.width / 2 + 20 + 70, position.y + 10, (width - 70) - padding, 18);
                EditorGUI.PropertyField(rect, prop_MarkAdjustmentRecord.FindPropertyRelative("m_XPositionAdjustment"), new GUIContent("X:"));

                rect.y += 20;
                EditorGUI.PropertyField(rect, prop_MarkAdjustmentRecord.FindPropertyRelative("m_YPositionAdjustment"), new GUIContent("Y:"));

                DrawGlyph((uint)prop_MarkGlyphID.intValue, new Rect(position.width / 2 + 20, position.y, position.width, position.height), property);
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

        void DrawGlyph(uint glyphIndex, Rect position, SerializedProperty property)
        {
            // Get a reference to the font asset
            FontAsset fontAsset = property.serializedObject.targetObject as FontAsset;

            if (fontAsset == null)
                return;

            Glyph glyph;

            // Check if glyph is present in the atlas texture.
            if (!fontAsset.glyphLookupTable.TryGetValue(glyphIndex, out glyph))
                return;

            // Get the atlas index of the glyph and lookup its atlas texture
            int atlasIndex = glyph.atlasIndex;
            Texture2D atlasTexture = fontAsset.atlasTextures.Length > atlasIndex ? fontAsset.atlasTextures[atlasIndex] : null;

            if (atlasTexture == null)
                return;

            Material mat;
            if (((GlyphRasterModes)fontAsset.atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                mat = FontAssetEditor.internalBitmapMaterial;

                if (mat == null)
                    return;

                mat.mainTexture = atlasTexture;
            }
            else
            {
                mat = FontAssetEditor.internalSDFMaterial;

                if (mat == null)
                    return;

                mat.mainTexture = atlasTexture;
                mat.SetFloat(TextShaderUtilities.ID_GradientScale, fontAsset.atlasPadding + 1);
            }

            // Draw glyph from atlas texture.
            Rect glyphDrawPosition = new Rect(position.x, position.y + 2, 64, 60);

            GlyphRect glyphRect = glyph.glyphRect;

            int padding = fontAsset.atlasPadding;

            int glyphOriginX = glyphRect.x - padding;
            int glyphOriginY = glyphRect.y - padding;
            int glyphWidth = glyphRect.width + padding * 2;
            int glyphHeight = glyphRect.height + padding * 2;

            float normalizedHeight = fontAsset.faceInfo.ascentLine - fontAsset.faceInfo.descentLine;
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
