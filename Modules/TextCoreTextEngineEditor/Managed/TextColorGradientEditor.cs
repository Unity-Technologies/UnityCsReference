// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    [CustomEditor(typeof(TextColorGradient))]
    internal class TextColorGradientEditor : Editor
    {
        SerializedProperty m_ColorMode;
        SerializedProperty m_TopLeftColor;
        SerializedProperty m_TopRightColor;
        SerializedProperty m_BottomLeftColor;
        SerializedProperty m_BottomRightColor;

        enum ColorMode
        {
            Single,
            HorizontalGradient,
            VerticalGradient,
            FourCornersGradient
        }

        void OnEnable()
        {
            m_ColorMode = serializedObject.FindProperty("colorMode");
            m_TopLeftColor = serializedObject.FindProperty("topLeft");
            m_TopRightColor = serializedObject.FindProperty("topRight");
            m_BottomLeftColor = serializedObject.FindProperty("bottomLeft");
            m_BottomRightColor = serializedObject.FindProperty("bottomRight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ColorMode, new GUIContent("Color Mode"));
            if (EditorGUI.EndChangeCheck())
            {
                switch ((ColorMode)m_ColorMode.enumValueIndex)
                {
                    case ColorMode.Single:
                        m_TopRightColor.colorValue = m_TopLeftColor.colorValue;
                        m_BottomLeftColor.colorValue = m_TopLeftColor.colorValue;
                        m_BottomRightColor.colorValue = m_TopLeftColor.colorValue;
                        break;
                    case ColorMode.HorizontalGradient:
                        m_BottomLeftColor.colorValue = m_TopLeftColor.colorValue;
                        m_BottomRightColor.colorValue = m_TopRightColor.colorValue;
                        break;
                    case ColorMode.VerticalGradient:
                        m_TopRightColor.colorValue = m_TopLeftColor.colorValue;
                        m_BottomRightColor.colorValue = m_BottomLeftColor.colorValue;
                        break;
                }
            }
            Rect rect;
            switch ((ColorMode)m_ColorMode.enumValueIndex)
            {
                case ColorMode.Single:
                    EditorGUI.BeginChangeCheck();
                    rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                    EditorGUI.PrefixLabel(rect, new GUIContent("Colors"));
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = (rect.width - EditorGUIUtility.labelWidth) / (EditorGUIUtility.wideMode ? 1f : 2f);
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_TopLeftColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_TopRightColor.colorValue = m_TopLeftColor.colorValue;
                        m_BottomLeftColor.colorValue = m_TopLeftColor.colorValue;
                        m_BottomRightColor.colorValue = m_TopLeftColor.colorValue;
                    }
                    break;

                case ColorMode.HorizontalGradient:
                    rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                    EditorGUI.PrefixLabel(rect, new GUIContent("Colors"));
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2f;

                    EditorGUI.BeginChangeCheck();
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_TopLeftColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_BottomLeftColor.colorValue = m_TopLeftColor.colorValue;
                    }

                    rect.x += rect.width;

                    EditorGUI.BeginChangeCheck();
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_TopRightColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_BottomRightColor.colorValue = m_TopRightColor.colorValue;
                    }
                    break;

                case ColorMode.VerticalGradient:
                    rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                    EditorGUI.PrefixLabel(rect, new GUIContent("Colors"));
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = (rect.width - EditorGUIUtility.labelWidth) / (EditorGUIUtility.wideMode ? 1f : 2f);
                    rect.height = EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2);

                    EditorGUI.BeginChangeCheck();
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_TopLeftColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_TopRightColor.colorValue = m_TopLeftColor.colorValue;
                    }

                    rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = (rect.width - EditorGUIUtility.labelWidth) / (EditorGUIUtility.wideMode ? 1f : 2f);
                    rect.height = EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2);

                    EditorGUI.BeginChangeCheck();
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_BottomLeftColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_BottomRightColor.colorValue = m_BottomLeftColor.colorValue;
                    }
                    break;

                case ColorMode.FourCornersGradient:
                    rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                    EditorGUI.PrefixLabel(rect, new GUIContent("Colors"));
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = (rect.width - EditorGUIUtility.labelWidth)  / 2f;
                    rect.height = EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2);

                    TextCoreEditorUtilities.DrawColorProperty(rect, m_TopLeftColor);
                    rect.x += rect.width;
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_TopRightColor);

                    rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2f;
                    rect.height = EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2);

                    TextCoreEditorUtilities.DrawColorProperty(rect, m_BottomLeftColor);
                    rect.x += rect.width;
                    TextCoreEditorUtilities.DrawColorProperty(rect, m_BottomRightColor);
                    break;
            }

            if (serializedObject.ApplyModifiedProperties())
                TextEventManager.ON_COLOR_GRADIENT_PROPERTY_CHANGED(target as TextColorGradient);
        }
    }
}
