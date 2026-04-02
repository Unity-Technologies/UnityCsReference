// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.Multiplayer.Editor
{
    internal class TabGroup : VisualElement
    {
        private Toolbar m_Header;
        private VisualElement m_Content;
        private List<Func<VisualElement>> m_Creators = new();
        private int m_CurrentTab;

        public TabGroup()
        {
            var styleSheet = EditorGUIUtility.LoadRequired("Multiplayer/UI/TabGroup.uss") as StyleSheet;
            styleSheets.Add(styleSheet);

            m_Header = new Toolbar();
            m_Content = new VisualElement();
            m_Content.AddToClassList("tab-content");

            Add(m_Header);
            Add(m_Content);

            EnableInClassList("unity-help-box", true);
        }

        public void AddTab(string label, Func<VisualElement> contentCreator)
        {
            var button = new ToolbarToggle() { text = label };
            button.userData = m_Header.childCount;
            button.RegisterValueChangedCallback(OnTabValueChange);

            m_Header.Add(button);
            m_Creators.Add(contentCreator);

            var buttonsCount = m_Header.childCount;

            for (int i = 0; i < buttonsCount; i++)
            {
                m_Header[i].RemoveFromClassList("tab-first");
                m_Header[i].RemoveFromClassList("tab-last");
            }

            m_Header[0].EnableInClassList("tab-first", true);
            m_Header[buttonsCount - 1].EnableInClassList("tab-last", true);

            if ((int)button.userData == 0)
                ActivateTab(0);
        }

        private void ActivateTab(int tabIndex)
        {
            m_CurrentTab = tabIndex;
            var buttonsCount = m_Header.childCount;

            for (int i = 0; i < buttonsCount; i++)
            {
                ((ToolbarToggle)m_Header[i]).SetValueWithoutNotify(i == tabIndex);
            }

            m_Content.Clear();
            m_Content.Add(m_Creators[tabIndex]());
        }

        private void OnTabValueChange(ChangeEvent<bool> evt)
        {
            ActivateTab((int)((ToolbarToggle)evt.currentTarget).userData);
        }

        public void Refresh()
        {
            ActivateTab(m_CurrentTab);
        }
    }
}
