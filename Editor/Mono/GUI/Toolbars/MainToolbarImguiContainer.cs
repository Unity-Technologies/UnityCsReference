// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Editor Utility/Imgui Subtoolbars", typeof(DefaultMainToolbar))]
    sealed class MainToolbarImguiContainer : IMGUIContainer
    {
        const float k_PaddingBetweenSubToolbar = 4;
        const string k_USSClassName = "unity-editor-toolbar-imgui-container";
        static readonly List<SubToolbar> s_SubToolbars = new List<SubToolbar>(1);
        float m_CurrentWidth;

        public static void AddDeprecatedSubToolbar(SubToolbar subToolbar)
        {
            s_SubToolbars.Add(subToolbar);
        }

        public MainToolbarImguiContainer()
        {
            AddToClassList(k_USSClassName);
            onGUIHandler = OnGUI;
        }

        void OnGUI()
        {
            style.display = s_SubToolbars.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateContainerWidth();
            Rect r = rect;
            foreach (var subToolbar in s_SubToolbars)
            {
                r.width = subToolbar.Width;
                subToolbar.OnGUI(rect);
                r.x += rect.width + k_PaddingBetweenSubToolbar;
            }
        }

        void UpdateContainerWidth()
        {
            float targetWidth = 0;
            foreach (var subToolbar in s_SubToolbars)
            {
                targetWidth += subToolbar.Width;
            }

            targetWidth += (s_SubToolbars.Count - 1) * k_PaddingBetweenSubToolbar;

            if (!Mathf.Approximately(m_CurrentWidth, targetWidth))
            {
                m_CurrentWidth = targetWidth;
                style.width = m_CurrentWidth;
                // Set the min and max to ensure the specified width is respected as it does not seem to be always the case
                // for some reason.
                style.minWidth = m_CurrentWidth;
                style.maxWidth = m_CurrentWidth;
            }
        }
    }
}
