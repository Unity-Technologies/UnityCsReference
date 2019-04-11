// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UnityEditorInternal.VR
{
    internal class VRCustomOptionsOculus : VRCustomOptions
    {
        static GUIContent s_SharedDepthBufferLabel = EditorGUIUtility.TextContent("Shared Depth Buffer|Enable depth buffer submission to allow for overlay depth occlusion, etc.");
        static GUIContent s_DashSupportLabel = EditorGUIUtility.TextContent("Dash Support|If enabled, pressing the home button brings up Dash, otherwise it brings up the older universal menu.");
        static GUIContent s_LowOverheadModeLabel = EditorGUIUtility.TextContent("Low Overhead Mode|If enabled, the GLES graphics driver will bypass validation code, potentially running faster.");

        SerializedProperty m_SharedDepthBuffer;
        SerializedProperty m_DashSupport;
        SerializedProperty m_LowOverheadMode;

        public override void Initialize(SerializedObject settings)
        {
            Initialize(settings, "oculus");
        }

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, propertyName);
            m_SharedDepthBuffer = FindPropertyAssert("sharedDepthBuffer");
            m_DashSupport = FindPropertyAssert("dashSupport");
            m_LowOverheadMode = FindPropertyAssert("lowOverheadMode");
        }

        public override Rect Draw(BuildTargetGroup target, Rect rect)
        {
            rect.y += EditorGUIUtility.standardVerticalSpacing;

            if (target != BuildTargetGroup.Android)
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                GUIContent label = EditorGUI.BeginProperty(rect, s_SharedDepthBufferLabel, m_SharedDepthBuffer);
                EditorGUI.BeginChangeCheck();
                bool boolValue = EditorGUI.Toggle(rect, label, m_SharedDepthBuffer.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SharedDepthBuffer.boolValue = boolValue;
                }
                EditorGUI.EndProperty();
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

                rect.height = EditorGUIUtility.singleLineHeight;
                label = EditorGUI.BeginProperty(rect, s_DashSupportLabel, m_DashSupport);
                EditorGUI.BeginChangeCheck();
                boolValue = EditorGUI.Toggle(rect, label, m_DashSupport.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_DashSupport.boolValue = boolValue;
                }
                EditorGUI.EndProperty();
            }
            else
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                GUIContent label = EditorGUI.BeginProperty(rect, s_LowOverheadModeLabel, m_LowOverheadMode);
                EditorGUI.BeginChangeCheck();
                bool boolValue = EditorGUI.Toggle(rect, label, m_LowOverheadMode.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_LowOverheadMode.boolValue = boolValue;
                }
                EditorGUI.EndProperty();
            }

            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            return rect;
        }

        public override float GetHeight(BuildTargetGroup target)
        {
            if (target != BuildTargetGroup.Android)
            {
                return (EditorGUIUtility.singleLineHeight * 2.0f) + (EditorGUIUtility.standardVerticalSpacing * 3.0f);
            }
            else
            {
                return (EditorGUIUtility.singleLineHeight * 1.0f) + (EditorGUIUtility.standardVerticalSpacing * 2.0f);
            }
        }
    }
}
