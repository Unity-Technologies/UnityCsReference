// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal abstract class ProjectSettingsElementWithSO : VisualElement
    {
        protected SerializedObject m_SerializedObject;
        protected SettingsWindow m_SettingsWindow;

        protected Color HighlightSelectionColor = EditorResources.GetStyle("sb-settings-panel-client-area").GetColor("-unity-search-highlight-selection-color");
        protected Color HighlightColor = EditorResources.GetStyle("sb-settings-panel-client-area").GetColor("-unity-search-highlight-color");

        internal void Initialize(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;
            m_SettingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>("", false);

            Initialize();
        }

        protected abstract void Initialize();
    }
}
