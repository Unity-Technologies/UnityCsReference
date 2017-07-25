// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UnityEditorInternal.VR
{
    internal class VRCustomOptionsGoogleVR : VRCustomOptions
    {
        static GUIContent[] s_DepthOptions =
        {
            new GUIContent("16-bit depth"),
            new GUIContent("24-bit depth"),
            new GUIContent("24-bit depth | 8-bit stencil")
        };
        static GUIContent s_DepthFormatLabel = new GUIContent("Depth Format");

        SerializedProperty m_DepthFormat;

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, propertyName);
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
            return EditorGUIUtility.singleLineHeight + (EditorGUIUtility.standardVerticalSpacing * 2.0f);
        }
    }

    internal class VRCustomOptionsCardboard : VRCustomOptionsGoogleVR
    {
        static GUIContent s_EnableTransitionVewLabel = new GUIContent("Enable Tansition View");
        SerializedProperty m_EnableTransitionView;

        public override void Initialize(SerializedObject settings)
        {
            Initialize(settings, "cardboard");
        }

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, propertyName);
            m_EnableTransitionView = FindPropertyAssert("enableTransitionView");
        }

        public override Rect Draw(Rect rect)
        {
            rect = base.Draw(rect);

            rect.height = EditorGUIUtility.singleLineHeight;
            GUIContent label = EditorGUI.BeginProperty(rect, s_EnableTransitionVewLabel, m_EnableTransitionView);
            EditorGUI.BeginChangeCheck();
            bool boolValue  = EditorGUI.Toggle(rect, label, m_EnableTransitionView.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_EnableTransitionView.boolValue = boolValue;
            }
            EditorGUI.EndProperty();
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            return rect;
        }

        public override float GetHeight()
        {
            return base.GetHeight() + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    internal class VRCustomOptionsDaydream : VRCustomOptionsGoogleVR
    {
        const float s_Indent = 10.0f;

        static GUIContent s_ForegroundIconLabel = EditorGUIUtility.TextContent("Foreground Icon|Icon should be a Texture with dimensions of 512px by 512px and a 1:1 aspect ratio.");
        static GUIContent s_BackgroundIconLabel = EditorGUIUtility.TextContent("Background Icon|Icon should be a Texture with dimensions of 512px by 512px and a 1:1 aspect ratio.");
        static GUIContent s_SustainedPerformanceModeLabel = EditorGUIUtility.TextContent("Sustained Performance Mode|Sustained Performance Mode is intended to provide a consistent level of performance for a prolonged amount of time");
        static GUIContent s_EnableVideoLayer = EditorGUIUtility.TextContent("Enable Video Surface|Enable the use of the video surface integrated with Daydream asynchronous reprojection.");
        static GUIContent s_UseProtectedVideoMemoryLabel = EditorGUIUtility.TextContent("Use Protected Memory|Enable the use of DRM protection. Only usable if all content is DRM Protected.");

        SerializedProperty m_DaydreamIcon;
        SerializedProperty m_DaydreamIconBackground;
        SerializedProperty m_DaydreamUseSustainedPerformanceMode;
        SerializedProperty m_DaydreamEnableVideoLayer;
        SerializedProperty m_DaydreamUseProtectedMemory;

        public override void Initialize(SerializedObject settings)
        {
            Initialize(settings, "daydream");
        }

        public override void Initialize(SerializedObject settings, string propertyName)
        {
            base.Initialize(settings, propertyName);
            m_DaydreamIcon = FindPropertyAssert("daydreamIconForeground");
            m_DaydreamIconBackground = FindPropertyAssert("daydreamIconBackground");
            m_DaydreamUseSustainedPerformanceMode = FindPropertyAssert("useSustainedPerformanceMode");
            m_DaydreamEnableVideoLayer = FindPropertyAssert("enableVideoLayer");
            m_DaydreamUseProtectedMemory = FindPropertyAssert("useProtectedVideoMemory");
        }

        private Rect DrawTextureUI(Rect rect, GUIContent propLabel, SerializedProperty prop)
        {
            rect.height = EditorGUI.kObjectFieldThumbnailHeight;
            GUIContent label = EditorGUI.BeginProperty(rect, propLabel, prop);

            EditorGUI.BeginChangeCheck();
            Texture2D objectReferenceValue = EditorGUI.ObjectField(rect, label, (Texture2D)prop.objectReferenceValue, typeof(Texture2D), false) as Texture2D;
            if (EditorGUI.EndChangeCheck())
            {
                prop.objectReferenceValue = objectReferenceValue;
            }

            EditorGUI.EndProperty();
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            return rect;
        }

        public override Rect Draw(Rect rect)
        {
            rect = base.Draw(rect);

            rect = DrawTextureUI(rect, s_ForegroundIconLabel, m_DaydreamIcon);
            rect = DrawTextureUI(rect, s_BackgroundIconLabel, m_DaydreamIconBackground);

            rect.height = EditorGUIUtility.singleLineHeight;
            GUIContent label = EditorGUI.BeginProperty(rect, s_SustainedPerformanceModeLabel, m_DaydreamUseSustainedPerformanceMode);
            EditorGUI.BeginChangeCheck();
            bool boolValue  = EditorGUI.Toggle(rect, label, m_DaydreamUseSustainedPerformanceMode.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_DaydreamUseSustainedPerformanceMode.boolValue = boolValue;
            }
            EditorGUI.EndProperty();
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            rect.height = EditorGUIUtility.singleLineHeight;
            label = EditorGUI.BeginProperty(rect, s_EnableVideoLayer, m_DaydreamEnableVideoLayer);
            EditorGUI.BeginChangeCheck();
            boolValue  = EditorGUI.Toggle(rect, label, m_DaydreamEnableVideoLayer.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_DaydreamEnableVideoLayer.boolValue = boolValue;
                if (!boolValue) m_DaydreamUseProtectedMemory.boolValue = false;
            }
            EditorGUI.EndProperty();
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            if (m_DaydreamEnableVideoLayer.boolValue)
            {
                rect.x += s_Indent;
                rect.width -= s_Indent;
                rect.height = EditorGUIUtility.singleLineHeight;
                label = EditorGUI.BeginProperty(rect, s_UseProtectedVideoMemoryLabel, m_DaydreamUseProtectedMemory);
                EditorGUI.BeginChangeCheck();
                boolValue  = EditorGUI.Toggle(rect, label, m_DaydreamUseProtectedMemory.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_DaydreamUseProtectedMemory.boolValue = boolValue;
                }
                EditorGUI.EndProperty();
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.x -= s_Indent;
                rect.width += s_Indent;
            }

            return rect;
        }

        public override float GetHeight()
        {
            float singleLineCount = 2.0f;
            float thumbnailCount = 2.0f;
            float verticalSpacingCount = 3.0f;

            if (m_DaydreamEnableVideoLayer.boolValue)
            {
                singleLineCount += 1.0f;
                verticalSpacingCount += 1.0f;
            }

            return base.GetHeight() + (EditorGUIUtility.singleLineHeight * singleLineCount) +
                (EditorGUI.kObjectFieldThumbnailHeight * thumbnailCount) +
                (EditorGUIUtility.standardVerticalSpacing * verticalSpacingCount);
        }
    }
}
