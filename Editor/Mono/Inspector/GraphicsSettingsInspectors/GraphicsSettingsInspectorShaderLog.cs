// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorShaderLog : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorShaderLog, UxmlTraits> { }

        internal class Styles
        {
            public static readonly GUIContent shaderPreloadSettings = EditorGUIUtility.TrTextContent("Shader Loading");
            public static readonly GUIContent logWhenShaderIsCompiled = EditorGUIUtility.TrTextContent("Log Shader Compilation",
                "When enabled, the player will print shader information each time a shader is being compiled (development and debug mode only).");
        }

        SerializedProperty m_LogWhenShaderIsCompiled;

        protected override void Initialize()
        {
            m_LogWhenShaderIsCompiled = m_SerializedObject.FindProperty("m_LogWhenShaderIsCompiled");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var settingsScope = new LabelWidthScope();
            using var wideScreenScope = new WideScreenScope(this);
            GUILayout.Label(Styles.shaderPreloadSettings, EditorStyles.boldLabel);

            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_LogWhenShaderIsCompiled, Styles.logWhenShaderIsCompiled);
            if (changeScope.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
