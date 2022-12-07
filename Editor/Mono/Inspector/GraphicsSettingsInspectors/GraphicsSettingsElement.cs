// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal abstract class GraphicsSettingsElement : VisualElement
    {
        protected SerializedObject m_SerializedObject;
        protected SettingsWindow m_SettingsWindow;

        protected Color HighlightSelectionColor = EditorResources.GetStyle("sb-settings-panel-client-area").GetColor("-unity-search-highlight-selection-color");
        protected Color HighlightColor = EditorResources.GetStyle("sb-settings-panel-client-area").GetColor("-unity-search-highlight-color");

        //We rely on SupportedOn attribute for the cases when we need to show element for SRP.
        //Here is a way to specify when we want to have element visible for BuiltinOnly.
        //Important notice: we check first for SupportedOn first, then for this backup field.
        public virtual bool BuiltinOnly => false;

        internal void Initialize(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            m_SettingsWindow = EditorWindow.GetWindow<SettingsWindow>();

            Initialize();
        }

        protected abstract void Initialize();
    }
}
