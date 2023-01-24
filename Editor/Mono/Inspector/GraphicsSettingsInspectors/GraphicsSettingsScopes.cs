// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class LabelWidthScope : GUI.Scope
    {
        static StyleBlock window => EditorResources.GetStyle("sb-settings-window");
        static float s_DefaultLabelWidth => window.GetFloat("-unity-label-width");

        readonly float m_LabelWidth;
        public LabelWidthScope(float layoutMaxWidth)
        {
            m_LabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = s_DefaultLabelWidth;
        }

        public LabelWidthScope() : this(s_DefaultLabelWidth)
        {
        }

        protected override void CloseScope()
        {
            EditorGUIUtility.labelWidth = m_LabelWidth;
        }
    }

    internal class WideScreenScope : GUI.Scope
    {
        readonly bool m_CurrentWideMode;

        public WideScreenScope(VisualElement currentElement)
        {
            m_CurrentWideMode = EditorGUIUtility.wideMode;
            
            // the inspector's width can be NaN if this is our first layout check.
            // If that's the case we'll set wideMode to true to avoid computing too tall an inspector on the first layout calculation
            var inspectorWidth = currentElement.layout.width;
            EditorGUIUtility.wideMode = float.IsNaN(inspectorWidth) || inspectorWidth > Editor.k_WideModeMinWidth;
        }

        protected override void CloseScope()
        {
            EditorGUIUtility.wideMode = m_CurrentWideMode;
        }
    }
}
