// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using System.Linq;


namespace UnityEditorInternal.VR
{
    internal class VRCustomOptionsHololens : VRCustomOptions
    {
        static GUIContent[] s_DepthOptions =
        {
            new GUIContent("16-bit depth"),
            new GUIContent("24-bit depth")
        };

        static GUIContent s_DepthFormatLabel = new GUIContent("Depth Format");

        SerializedProperty m_DepthFormat;

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, "hololens");
            m_DepthFormat = FindPropertyAssert("depthFormat");
        }

        public override Rect Draw(Rect rect)
        {
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            rect.height = EditorGUIUtility.singleLineHeight;

            GUIContent label = EditorGUI.BeginProperty(rect, s_DepthFormatLabel, m_DepthFormat);
            EditorGUI.BeginChangeCheck();
            int intValue = EditorGUI.Popup(rect, label, m_DepthFormat.intValue, s_DepthOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_DepthFormat.intValue = intValue;
            }
            EditorGUI.EndProperty();

            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            return rect;
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2.0f;
        }
    }
}
