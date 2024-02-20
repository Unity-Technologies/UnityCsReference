// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class TabbedView : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new TabbedView();
        }

        const string s_UssClassName = "unity-tabbed-view";
        const string s_ContentContainerClassName = "unity-tabbed-view__content-container";
        const string s_TabsContainerClassName = "unity-tabbed-view__tabs-container";

        readonly VisualElement m_TabContent;
        readonly VisualElement m_Content;

        readonly List<TabButton> m_Tabs = new();
        TabButton m_ActiveTab;
        internal TabButton ActiveTab => m_ActiveTab;
        internal int ActiveTabIndex => m_ActiveTab != null ? m_Tabs.IndexOf(m_ActiveTab) : -1;

        public override VisualElement contentContainer => m_Content;

        public TabbedView()
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(EditorGUIUtility.Load("StyleSheets/InspectorWindow/TabbedView.uss") as StyleSheet);

            m_TabContent = new VisualElement { name = "unity-tabs-container" };
            m_TabContent.AddToClassList(s_TabsContainerClassName);
            hierarchy.Add(m_TabContent);

            m_Content = new VisualElement { name = "unity-content-container" };
            m_Content.AddToClassList(s_ContentContainerClassName);
            hierarchy.Add(m_Content);

            RegisterCallback<AttachToPanelEvent>(OnAttachPanelEvent);
        }

        public void AddTab(TabButton tabButton, bool activate)
        {
            switch (m_Tabs.Count)
            {
                case 0:
                    SetStyleForLeftmost(tabButton);
                    break;
                case > 0:
                    SetStyleForRightmost(tabButton);
                    break;
            }

            m_Tabs.Add(tabButton);
            m_TabContent.Add(tabButton);

            UpdateBorderStyles();

            tabButton.OnClose += RemoveTab;
            tabButton.OnSelect += Activate;

            Add(tabButton.Target);
            tabButton.Target.style.display = DisplayStyle.None;

            if (activate)
                Activate(tabButton);
        }

        public void RemoveTab(TabButton tabButton)
        {
            var index = m_Tabs.IndexOf(tabButton);

            // If this tab is the active one make sure we deselect it first...
            if (m_ActiveTab == tabButton)
            {
                DeselectTab(tabButton);
                m_ActiveTab = null;
            }

            m_Tabs.RemoveAt(index);
            m_TabContent.Remove(tabButton);

            UpdateBorderStyles();

            tabButton.OnClose -= RemoveTab;
            tabButton.OnSelect -= Activate;

            // If we closed the active tab AND we have any tabs left - active the next valid one...
            if (m_ActiveTab != null || m_Tabs.Count == 0)
                return;
            var clampedIndex = Mathf.Clamp(index, 0, m_Tabs.Count - 1);
            var tabToActivate = m_Tabs[clampedIndex];

            Activate(tabToActivate);
        }

        void UpdateBorderStyles()
        {
            var tabAmount = m_Tabs.Count;
            if (tabAmount == 0)
                return;

            for (int i = 0; i < tabAmount; i++)
            {
                ResetBorderStyles(m_Tabs[i]);
                if (i == 0)
                    SetStyleForLeftmost(m_Tabs[i]);
                if (i + 1 == tabAmount)
                    SetStyleForRightmost(m_Tabs[i]);
            }
        }

        void OnAttachPanelEvent(AttachToPanelEvent e)
        {
            // This code takes any existing tab buttons and hooks them into the system...
            for (int i = 0; i < m_Content.childCount; ++i)
            {
                VisualElement element = m_Content[i];

                if (element is TabButton button)
                {
                    m_Content.Remove(element);
                    if (button.Target == null)
                    {
                        string targetId = button.TargetId;

                        button.Target = this.Q(targetId);
                    }

                    AddTab(button, false);
                    --i;
                }
                else
                {
                    element.style.display = DisplayStyle.None;
                }
            }

            // Finally, if we need to, activate this tab...
            if (m_ActiveTab != null)
            {
                SelectTab(m_ActiveTab);
            }
            else if (m_TabContent.childCount > 0)
            {
                m_ActiveTab = (TabButton)m_TabContent[0];

                SelectTab(m_ActiveTab);
            }
        }

        static void SetStyleForLeftmost(VisualElement element)
        {
            element.style.borderTopLeftRadius = 3;
            foreach (var child in element.Children())
            {
                child.style.borderTopLeftRadius = 3;
                break;
            }
        }

        static void SetStyleForRightmost(VisualElement element)
        {
            element.style.borderRightWidth = 1;
            element.style.borderTopRightRadius = 3;
            foreach (var child in element.Children())
            {
                child.style.borderTopRightRadius = 3;
                break;
            }
        }

        static void ResetBorderStyles(VisualElement element)
        {
            element.style.borderTopLeftRadius = 0;
            foreach (var child in element.Children())
            {
                child.style.borderTopLeftRadius = 0;
                break;
            }
            element.style.borderRightWidth = 0;
            element.style.borderTopRightRadius = 0;
            foreach (var child in element.Children())
            {
                child.style.borderTopRightRadius = 0;
                break;
            }
        }

        public void SelectTab(int index)
        {
            if(index >= m_Tabs.Count || index < 0)
                return;

            Activate(m_Tabs[index]);
        }

        public void Activate(TabButton button)
        {
            if (m_ActiveTab == button)
                return;

            if (m_ActiveTab != null)
            {
                DeselectTab(m_ActiveTab);
            }

            m_ActiveTab = button;
            SelectTab(m_ActiveTab);
        }

        void SelectTab(TabButton tabButton)
        {
            VisualElement target = tabButton.Target;

            tabButton.Select();
            if (target != null)
                target.style.display = DisplayStyle.Flex;
        }

        void DeselectTab(TabButton tabButton)
        {
            VisualElement target = tabButton.Target;

            if (target != null)
                target.style.display = DisplayStyle.None;

            tabButton.Deselect();
        }
    }
}
