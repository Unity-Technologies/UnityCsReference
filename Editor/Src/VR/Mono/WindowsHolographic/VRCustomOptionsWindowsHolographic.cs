// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using System.Linq;


namespace UnityEditorInternal.VR
{
    internal class VRCustomOptionsWindowsMR : VRCustomOptions
    {
        static GUIContent[] s_DepthOptions =
        {
            new GUIContent("16-bit depth"),
            new GUIContent("24-bit depth")
        };

        static GUIContent s_DepthFormatLabel = new GUIContent("Depth Format");
        static GUIContent s_DepthBufferSharingLabel = new GUIContent("Enable Depth Buffer Sharing");

        SerializedProperty m_DepthFormat;
        SerializedProperty m_DepthBufferSharingEnabled;

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, "hololens");
            m_DepthFormat = FindPropertyAssert("depthFormat");
            m_DepthBufferSharingEnabled = FindPropertyAssert("depthBufferSharingEnabled");
        }

        public override Rect Draw(BuildTargetGroup target, Rect rect)
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

            label = EditorGUI.BeginProperty(rect, s_DepthBufferSharingLabel, m_DepthBufferSharingEnabled);
            EditorGUI.BeginChangeCheck();
            bool depthBufferSharingEnabled = EditorGUI.Toggle(rect, s_DepthBufferSharingLabel, m_DepthBufferSharingEnabled.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_DepthBufferSharingEnabled.boolValue = depthBufferSharingEnabled;
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
