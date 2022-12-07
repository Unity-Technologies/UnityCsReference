// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorVideoShader : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorVideoShader, UxmlTraits> { }
        internal class Styles
        {
            public static readonly GUIContent modeString = EditorGUIUtility.TrTextContent("Video", "Shaders used by the VideoPlayer decoding and compositing.");
        }

        SerializedProperty m_VideoShadersIncludeMode;

        protected override void Initialize()
        {
            m_VideoShadersIncludeMode = m_SerializedObject.FindProperty("m_VideoShadersIncludeMode");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var check = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_VideoShadersIncludeMode, Styles.modeString);
            if(check.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
