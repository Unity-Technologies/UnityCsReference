// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;


namespace UnityEditor.TextCore
{
    [CustomPropertyDrawer(typeof(GlyphRect))]
    internal class GlyphRectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //EditorGUI.BeginProperty(position, label, property);

            SerializedProperty prop_X = property.FindPropertyRelative("m_X");
            SerializedProperty prop_Y = property.FindPropertyRelative("m_Y");
            SerializedProperty prop_Width = property.FindPropertyRelative("m_Width");
            SerializedProperty prop_Height = property.FindPropertyRelative("m_Height");

            // We get Rect since a valid position may not be provided by the caller.
            Rect rect = new Rect(position.x, position.y, position.width, 49);
            EditorGUI.LabelField(new Rect(rect.x, rect.y - 2.5f, rect.width, 18), new GUIContent("Glyph Rect"));

            EditorGUIUtility.labelWidth = 50f;
            EditorGUIUtility.fieldWidth = 20f;

            //GUI.enabled = false;
            float width = (rect.width - 75f) / 4;
            EditorGUI.PropertyField(new Rect(rect.x + width * 0, rect.y + 20, width - 5f, 18), prop_X, new GUIContent("X:"));
            EditorGUI.PropertyField(new Rect(rect.x + width * 1, rect.y + 20, width - 5f, 18), prop_Y, new GUIContent("Y:"));
            EditorGUI.PropertyField(new Rect(rect.x + width * 2, rect.y + 20, width - 5f, 18), prop_Width, new GUIContent("W:"));
            EditorGUI.PropertyField(new Rect(rect.x + width * 3, rect.y + 20, width - 5f, 18), prop_Height, new GUIContent("H:"));

            //EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 45f;
        }
    }
}
