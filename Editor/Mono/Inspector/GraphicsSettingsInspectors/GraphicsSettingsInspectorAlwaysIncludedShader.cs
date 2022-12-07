// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorAlwaysIncludedShader : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorAlwaysIncludedShader, UxmlTraits> { }

        SerializedProperty m_AlwaysIncludedShaders;

        protected override void Initialize()
        {
            m_AlwaysIncludedShaders = m_SerializedObject.FindProperty("m_AlwaysIncludedShaders");
            m_AlwaysIncludedShaders.isExpanded = true;

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var check = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_AlwaysIncludedShaders, true);
            if (check.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
