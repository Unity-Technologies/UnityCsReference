// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorCullingSettings : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorCullingSettings, UxmlTraits>
        {
        }

        internal class Styles
        {
            public static readonly GUIContent cullingSettings = EditorGUIUtility.TrTextContent("Culling Settings");
            public static readonly GUIContent cameraRelativeSettings = EditorGUIUtility.TrTextContent("Camera-Relative Culling");

            public static readonly GUIContent cameraRelativeLightCulling = EditorGUIUtility.TrTextContent("Lights",
                "When enabled, Unity uses the camera position as the reference point for culling lights instead of the world space origin.");

            public static readonly GUIContent cameraRelativeShadowCulling = EditorGUIUtility.TrTextContent("Shadows",
                "When enabled, Unity uses the camera position as the reference point for culling shadows instead of the world space origin.");

        }

        SerializedProperty m_CameraRelativeLightCulling;
        SerializedProperty m_CameraRelativeShadowCulling;

        protected override void Initialize()
        {
            m_CameraRelativeLightCulling = m_SerializedObject.FindProperty("m_CameraRelativeLightCulling");
            m_CameraRelativeShadowCulling = m_SerializedObject.FindProperty("m_CameraRelativeShadowCulling");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var check = new EditorGUI.ChangeCheckScope();
            using var settingsScope = new LabelWidthScope();
            using var wideScreenScope = new WideScreenScope(this);
            GUILayout.Label(Styles.cullingSettings, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Styles.cameraRelativeSettings, EditorStyles.label);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_CameraRelativeLightCulling, Styles.cameraRelativeLightCulling);
            EditorGUILayout.PropertyField(m_CameraRelativeShadowCulling, Styles.cameraRelativeShadowCulling);
            EditorGUI.indentLevel--;
            if (check.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
