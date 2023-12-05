// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Dropdown Button exported from <see cref="BuildPlayerWindow"/>,
    /// as UIToolkit does not support DropdownButton.
    /// </summary>
    internal class DropdownButton : VisualElement
    {
        const int k_ButtonWidth = 105;
        readonly GenericMenu m_Menu;
        readonly GUIContent m_BuildButton;
        readonly Action m_DefaultClicked;

        public DropdownButton(string text, Action defaultClicked, GenericMenu menu)
        {
            this.m_Menu = menu;
            m_BuildButton = new GUIContent(text);
            m_DefaultClicked = defaultClicked;
            Add(new IMGUIContainer(RenderIMGUI));
        }

        public void SetText(string text)
        {
            m_BuildButton.text = text;
        }

        void RenderIMGUI()
        {
            Rect buildRect = GUILayoutUtility.GetRect(m_BuildButton, BuildProfileModuleUtil.dropDownToggleButton, GUILayout.Width(k_ButtonWidth));
            Rect buildRectPopupButton = buildRect;
            buildRectPopupButton.x += buildRect.width - 16;
            buildRectPopupButton.width = 16;

            if (m_Menu != null && EditorGUI.DropdownButton(buildRectPopupButton, GUIContent.none, FocusType.Passive,
                    GUIStyle.none))
            {
                m_Menu.DropDown(buildRect);
            }
            else
            {
                GUIStyle style = m_Menu == null ? GUI.skin.button : BuildProfileModuleUtil.dropDownToggleButton;
                if (GUI.Button(buildRect, m_BuildButton, style))
                {
                    m_DefaultClicked();
                }
            }
        }
    }
}
