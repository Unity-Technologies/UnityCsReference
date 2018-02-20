// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    [CustomPropertyDrawer(typeof(ParticleSystem.MinMaxGradient))]
    public class MinMaxGradientPropertyDrawer : PropertyDrawer
    {
        class PropertyData
        {
            public SerializedProperty mode;
            public SerializedProperty gradientMin;
            public SerializedProperty gradientMax;
            public SerializedProperty colorMin;
            public SerializedProperty colorMax;
        }

        // Its possible that the PropertyDrawer may be used to draw more than one MinMaxCurve property(arrays, lists)
        Dictionary<string, PropertyData> m_PropertyDataPerPropertyPath = new Dictionary<string, PropertyData>();
        PropertyData m_Property;

        class Styles
        {
            public readonly float stateButtonWidth = 18;
            public readonly GUIContent[] modes = new[]
            {
                EditorGUIUtility.TrTextContent("Color"),
                EditorGUIUtility.TrTextContent("Gradient"),
                EditorGUIUtility.TrTextContent("Random Between Two Colors"),
                EditorGUIUtility.TrTextContent("Random Between Two Gradients"),
                EditorGUIUtility.TrTextContent("Random Color")
            };
        };
        static Styles s_Styles;

        void Init(SerializedProperty property)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            if (m_PropertyDataPerPropertyPath.TryGetValue(property.propertyPath, out m_Property))
                return;

            m_Property = new PropertyData()
            {
                mode = property.FindPropertyRelative("m_Mode"),
                gradientMin = property.FindPropertyRelative("m_GradientMin"),
                gradientMax = property.FindPropertyRelative("m_GradientMax"),
                colorMin = property.FindPropertyRelative("m_ColorMin"),
                colorMax = property.FindPropertyRelative("m_ColorMax")
            };
            m_PropertyDataPerPropertyPath.Add(property.propertyPath, m_Property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);
            return m_Property.mode.intValue == (int)MinMaxGradientState.k_RandomBetweenTwoGradients ? EditorGUI.kSingleLineHeight * 2.0f : EditorGUI.kSingleLineHeight;
        }

        static void DrawTwoPropertyFields(Rect rect, GUIContent label, SerializedProperty prop1, SerializedProperty prop2, bool singleLine)
        {
            rect = EditorGUI.PrefixLabel(rect, label);

            var property1Rect = rect;
            var property2Rect = rect;
            if (singleLine)
            {
                float wideModeWidth = rect.width * 0.5f;
                property1Rect.width = wideModeWidth;
                property2Rect.x += wideModeWidth;
                property2Rect.width = wideModeWidth;
            }
            else
            {
                property2Rect.y += EditorGUI.kSingleLineHeight;
            }
            EditorGUI.PropertyField(property1Rect, prop1, GUIContent.none);
            EditorGUI.PropertyField(property2Rect, prop2, GUIContent.none);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            Rect fieldRect = position;
            fieldRect.height = EditorGUI.kSingleLineHeight;
            var mode = (MinMaxGradientState)m_Property.mode.intValue;

            // Mode
            fieldRect.xMax -= s_Styles.stateButtonWidth;
            var modeRect = new Rect(fieldRect.xMax, fieldRect.y, s_Styles.stateButtonWidth, fieldRect.height);
            EditorGUI.BeginProperty(modeRect, GUIContent.none, m_Property.mode);
            EditorGUI.BeginChangeCheck();
            int newSelection = EditorGUI.Popup(modeRect, GUIContent.none, m_Property.mode.intValue, s_Styles.modes, EditorStyles.minMaxStateDropdown);
            if (EditorGUI.EndChangeCheck())
                m_Property.mode.intValue = newSelection;
            EditorGUI.EndProperty();

            if (m_Property.mode.hasMultipleDifferentValues)
            {
                EditorGUI.LabelField(fieldRect, GUIContent.Temp("-"));
                return;
            }

            switch (mode)
            {
                case MinMaxGradientState.k_Color:
                    EditorGUI.PropertyField(fieldRect, m_Property.colorMax, label);
                    break;
                case MinMaxGradientState.k_Gradient:
                case MinMaxGradientState.k_RandomColor:
                    EditorGUI.PropertyField(fieldRect, m_Property.gradientMax, label);
                    break;
                case MinMaxGradientState.k_RandomBetweenTwoColors:
                    DrawTwoPropertyFields(fieldRect, label, m_Property.colorMin, m_Property.colorMax, true);
                    break;
                case MinMaxGradientState.k_RandomBetweenTwoGradients:
                    DrawTwoPropertyFields(fieldRect, label, m_Property.gradientMin, m_Property.gradientMax, false);
                    break;
            }
        }
    }
}
