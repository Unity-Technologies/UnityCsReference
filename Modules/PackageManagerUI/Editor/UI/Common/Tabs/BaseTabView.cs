// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class BaseTabView<T> : VisualElement, ITabView<T> where T : BaseTabElement
    {
        protected VisualElement m_HeaderContainer;
        protected VisualElement m_BodyContainer;

        protected Dictionary<string, Button> m_HeaderButtons;
        protected Dictionary<string, T> m_TabElements;

        protected string m_SelectedTabId;
        public string selectedTabId => m_SelectedTabId;

        public virtual event Action<T, T> onTabSwitched = delegate {};

        public IEnumerable<T> tabs => m_TabElements.Values;

        public BaseTabView()
        {
            m_HeaderContainer = new VisualElement();
            m_HeaderContainer.AddClasses("tabsHeaderContainer container row");
            m_BodyContainer = new VisualElement();
            m_BodyContainer.AddClasses("tabsBodyContainer");

            m_HeaderButtons = new Dictionary<string, Button>();
            m_TabElements = new Dictionary<string, T>();

            Add(m_HeaderContainer);
            Add(m_BodyContainer);
        }

        public void AddTab(T tab)
        {
            if (m_TabElements.ContainsKey(tab.id))
                return;

            var headerButton = CreateHeaderButtonForTab(tab);

            m_HeaderButtons.Add(tab.id, headerButton);
            m_TabElements.Add(tab.id, tab);

            m_HeaderContainer.Add(headerButton);
            m_BodyContainer.Add(tab);
        }

        public void RemoveTab(T tab)
        {
            m_TabElements.Remove(tab.id);
            m_BodyContainer.Remove(tab);

            if (m_HeaderButtons.TryGetValue(tab.id, out var header))
            {
                m_HeaderButtons.Remove(tab.id);
                m_HeaderContainer.Remove(header);
            }
        }

        public void ClearTabs()
        {
            m_HeaderButtons.Clear();
            m_TabElements.Clear();

            m_HeaderContainer.Clear();
            m_BodyContainer.Clear();
        }

        private Button CreateHeaderButtonForTab(T tab)
        {
            var headerButton = new Button();
            headerButton.name = $"{tab.id}Button";
            headerButton.text = L10n.Tr(tab.displayName);
            headerButton.clicked += () => OnTabHeaderClicked(tab);
            return headerButton;
        }

        public T GetTab(string tabIdentifier = "")
        {
            return !string.IsNullOrEmpty(tabIdentifier) && m_TabElements.TryGetValue(tabIdentifier, out var result) ? result : null;
        }

        public A GetTab<A>(string tabIdentifier) where A : T
        {
            return GetTab(tabIdentifier) as A;
        }

        // return boolean indicating whether selection was successful or not
        public bool SelectTab(string id)
        {
            return SelectTab(GetTab(id));
        }

        protected virtual void OnTabHeaderClicked(T tab)
        {
            SelectTab(tab);
        }

        public virtual bool SelectTab(T tabToSelect)
        {
            if (tabToSelect is null || !m_HeaderButtons.ContainsKey(tabToSelect.id) || tabToSelect.id == selectedTabId)
                return false;

            var previousSelectedTab = GetTab(selectedTabId);
            if (previousSelectedTab != null)
            {
                m_HeaderButtons[previousSelectedTab.id].RemoveFromClassList("tabHeaderSelected");
                UIUtils.SetElementDisplay(previousSelectedTab, false);
            }

            m_HeaderButtons[tabToSelect.id].AddClasses("tabHeaderSelected");
            UIUtils.SetElementDisplay(tabToSelect, true);
            m_SelectedTabId = tabToSelect.id;

            onTabSwitched.Invoke(previousSelectedTab, tabToSelect);
            return true;
        }
    }
}
