// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditorInternal.VR
{
    internal class VRCustomOptionsLumin : VRCustomOptions
    {
        static GUIContent[] s_DepthOptions =
        {
            new GUIContent("16-bit depth"),
            new GUIContent("24-bit depth")
        };

        static GUIContent[] s_FrameTimingOptions =
        {
            new GUIContent("Unspecified"),
            new GUIContent("Maximum"),
            new GUIContent("60Hz"),
            new GUIContent("120Hz")
        };

        static GUIContent s_DepthFormatLabel = new GUIContent("Depth Format");
        static GUIContent s_FrameTimingLabel = new GUIContent("Target Frame Rate");
        static GUIContent s_GlBlobCacheLabel = new GUIContent("Enable OpenGL Shader Cache File");
        static GUIContent s_GlBlobMaxSizeLabel = new GUIContent("Max GL Blob Size in bytes");
        static GUIContent s_GlFileMaxSizeLabel = new GUIContent("Max GL Cache File Size in bytes");

        const string s_GLBlobToolTip = "Select to optimize application and use cached shader data.";
        const string s_GlBlobMaxSizeToolTip = "The maximium size for each shader blob data, units in bytes.";
        const string s_GlFileMaxSizeToolTip = "The maximium size for shader cache file, units in bytes.";

        SerializedProperty m_DepthFormat;
        SerializedProperty m_FrameTiming;
        SerializedProperty m_EnableGLCache;
        SerializedProperty m_GLMaxBlobSize;
        SerializedProperty m_GLMaxFileSize;

        public override void Initialize(SerializedObject settings)
        {
            Initialize(settings, "lumin");
        }

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, propertyName);
            m_DepthFormat = FindPropertyAssert("depthFormat");
            m_FrameTiming = FindPropertyAssert("frameTiming");
            m_EnableGLCache = FindPropertyAssert("enableGLCache");
            m_GLMaxBlobSize = FindPropertyAssert("glCacheMaxBlobSize");
            m_GLMaxFileSize = FindPropertyAssert("glCacheMaxFileSize");
        }

        public override Rect Draw(BuildTargetGroup target, Rect rect)
        {
            if (target != BuildTargetGroup.Lumin)
            {
                return Rect.zero;
            }
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

            label = EditorGUI.BeginProperty(rect, s_FrameTimingLabel, m_FrameTiming);
            EditorGUI.BeginChangeCheck();
            intValue = EditorGUI.Popup(rect, label, m_FrameTiming.intValue, s_FrameTimingOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_FrameTiming.intValue = intValue;
            }
            EditorGUI.EndProperty();

            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            label = EditorGUI.BeginProperty(rect, s_GlBlobCacheLabel, m_EnableGLCache);
            label.tooltip = s_GLBlobToolTip;
            EditorGUI.BeginChangeCheck();
            bool glCacheEnabled = EditorGUI.Toggle(rect, label, m_EnableGLCache.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_EnableGLCache.boolValue = glCacheEnabled;
            }
            EditorGUI.EndProperty();

            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.BeginDisabledGroup(glCacheEnabled == false);
            label = EditorGUI.BeginProperty(rect, s_GlBlobMaxSizeLabel, m_GLMaxBlobSize);
            label.tooltip = s_GlBlobMaxSizeToolTip;
            EditorGUI.BeginChangeCheck();
            int glMaxBlobSize = EditorGUI.IntField(rect, label, m_GLMaxBlobSize.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_GLMaxBlobSize.intValue = glMaxBlobSize;
            }
            EditorGUI.EndProperty();

            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            label = EditorGUI.BeginProperty(rect, s_GlFileMaxSizeLabel, m_GLMaxFileSize);
            label.tooltip = s_GlFileMaxSizeToolTip;
            EditorGUI.BeginChangeCheck();
            int glMaxFileSize = EditorGUI.IntField(rect, label, m_GLMaxFileSize.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_GLMaxFileSize.intValue = glMaxFileSize;
            }
            EditorGUI.EndProperty();

            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.EndDisabledGroup();

            return rect;
        }

        public override float GetHeight(BuildTargetGroup target)
        {
            float entryCount = 6.0f;
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * entryCount;
        }
    }
}
