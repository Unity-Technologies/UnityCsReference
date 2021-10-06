// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(GlyphMetrics))]
    internal class GlyphMetricsPropertyDrawer : PropertyDrawer
    {
        private static readonly GUIContent k_GlyphMetricLabel = new GUIContent("Glyph Metrics", "The layout metrics of the glyph.");
        private static readonly GUIContent k_WidthPropertyLabel = new GUIContent("W:", "The width of the glyph.");
        private static readonly GUIContent k_HeightPropertyLabel = new GUIContent("H:", "The height of the glyph.");
        private static readonly GUIContent k_BearingXPropertyLabel = new GUIContent("BX:", "The horizontal bearing X of the glyph.");
        private static readonly GUIContent k_BearingYPropertyLabel = new GUIContent("BY:", "The horizontal bearing Y of the glyph.");
        private static readonly GUIContent k_HorizontalAdvancePropertyLabel = new GUIContent("AD:", "The horizontal advance of the glyph.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_Width = property.FindPropertyRelative("m_Width");
            SerializedProperty prop_Height = property.FindPropertyRelative("m_Height");
            SerializedProperty prop_HoriBearingX = property.FindPropertyRelative("m_HorizontalBearingX");
            SerializedProperty prop_HoriBearingY = property.FindPropertyRelative("m_HorizontalBearingY");
            SerializedProperty prop_HoriAdvance = property.FindPropertyRelative("m_HorizontalAdvance");

            // We get Rect since a valid position may not be provided by the caller.
            Rect rect = new Rect(position.x, position.y, position.width, 49);

            EditorGUI.LabelField(new Rect(rect.x, rect.y - 2.5f, rect.width, 18), k_GlyphMetricLabel);

            EditorGUIUtility.labelWidth = 20f;
            EditorGUIUtility.fieldWidth = 15f;

            //GUI.enabled = false;
            float width = (rect.width - 75f) / 2;
            EditorGUI.PropertyField(new Rect(rect.x + width * 0, rect.y + 20, width - 5f, 18), prop_Width, k_WidthPropertyLabel);
            EditorGUI.PropertyField(new Rect(rect.x + width * 1, rect.y + 20, width - 5f, 18), prop_Height, k_HeightPropertyLabel);

            //GUI.enabled = true;

            width = (rect.width - 75f) / 3;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(rect.x + width * 0, rect.y + 40, width - 5f, 18), prop_HoriBearingX, k_BearingXPropertyLabel);
            EditorGUI.PropertyField(new Rect(rect.x + width * 1, rect.y + 40, width - 5f, 18), prop_HoriBearingY, k_BearingYPropertyLabel);
            EditorGUI.PropertyField(new Rect(rect.x + width * 2, rect.y + 40, width - 5f, 18), prop_HoriAdvance, k_HorizontalAdvancePropertyLabel);
            if (EditorGUI.EndChangeCheck())
            {
                //
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 65f;
        }
    }
}
