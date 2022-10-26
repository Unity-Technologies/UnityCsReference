// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(GlyphRect))]
    internal class GlyphRectPropertyDrawer : PropertyDrawer
    {
        private static readonly GUIContent k_GlyphRectLabel = new GUIContent("Glyph Rect", "A rectangle (rect) that represents the position of the glyph in the atlas texture.");
        private static readonly GUIContent k_XPropertyLabel = new GUIContent("X:", "The X coordinate of the glyph in the atlas texture.");
        private static readonly GUIContent k_YPropertyLabel = new GUIContent("Y:", "The Y coordinate of the glyph in the atlas texture.");
        private static readonly GUIContent k_WidthPropertyLabel = new GUIContent("W:", "The width of the glyph in the atlas texture.");
        private static readonly GUIContent k_HeightPropertyLabel = new GUIContent("H:", "The height of the glyph in the atlas texture.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //EditorGUI.BeginProperty(position, label, property);

            SerializedProperty prop_X = property.FindPropertyRelative("m_X");
            SerializedProperty prop_Y = property.FindPropertyRelative("m_Y");
            SerializedProperty prop_Width = property.FindPropertyRelative("m_Width");
            SerializedProperty prop_Height = property.FindPropertyRelative("m_Height");

            // We get Rect since a valid position may not be provided by the caller.
            Rect rect = new Rect(position.x, position.y, position.width, 49);
            EditorGUI.LabelField(new Rect(rect.x, rect.y - 2.5f, rect.width, 18), k_GlyphRectLabel);

            EditorGUIUtility.labelWidth = 20f;
            EditorGUIUtility.fieldWidth = 20f;

            //GUI.enabled = false;
            float width = (rect.width - 75f) / 4;
            EditorGUI.PropertyField(new Rect(rect.x + width * 0, rect.y + 20, width - 5f, 18), prop_X, k_XPropertyLabel);
            EditorGUI.PropertyField(new Rect(rect.x + width * 1, rect.y + 20, width - 5f, 18), prop_Y, k_YPropertyLabel);
            EditorGUI.PropertyField(new Rect(rect.x + width * 2, rect.y + 20, width - 5f, 18), prop_Width, k_WidthPropertyLabel);
            EditorGUI.PropertyField(new Rect(rect.x + width * 3, rect.y + 20, width - 5f, 18), prop_Height, k_HeightPropertyLabel);

            //EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 45f;
        }
    }
}
