// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class BaseTabView<T> : VisualElement, ITabView<T> where T : BaseTabElement
    {
        protected VisualElement m_HeaderContainer;
        protected VisualElement m_TabHeaderContainer;
        protected VisualElement m_BodyContainer;
        protected DropdownButton m_TabHeaderDropdown;

        protected Dictionary<string, Button> m_HeaderButtons;
        protected Dictionary<string, T> m_TabElements;

        protected List<string> m_ValidTabIds;

        protected string m_SelectedTabId;
        public string selectedTabId => m_SelectedTabId;

        public virtual event Action<T, T> onTabSwitched = delegate {};

        public IEnumerable<T> tabs => m_TabElements.Values.OfType<T>();

        protected float m_CalculatedTabHorizontalMarginAndPadding = 0f;
        protected const float k_DropdownButtonWidth = 14f;

        private void InitializeHeader()
        {
            m_HeaderContainer = new VisualElement();
            m_HeaderContainer.AddClasses("container row headerContainer");

            m_TabHeaderContainer = new VisualElement();
            m_TabHeaderContainer.AddClasses("tabsHeaderContainer container row");

            m_TabHeaderDropdown = new DropdownButton();
            m_TabHeaderDropdown.AddClasses("tabsHeaderDropdown");
            m_TabHeaderDropdown.menu = new DropdownMenu();

            m_HeaderContainer.Add(m_TabHeaderContainer);
            m_HeaderContainer.Add(m_TabHeaderDropdown);
        }

        public BaseTabView()
        {
            m_BodyContainer = new VisualElement();
            m_BodyContainer.AddClasses("tabsBodyContainer");

            InitializeHeader();

            m_HeaderButtons = new Dictionary<string, Button>();
            m_TabElements = new Dictionary<string, T>();
            m_ValidTabIds = new List<string>();

            Add(m_HeaderContainer);
            Add(m_BodyContainer);

            RegisterCallback((GeometryChangedEvent evt) => OnGeometryChanged(evt));
        }

        protected void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // do not re-calculate unless there's been at least one pixel worth of change
            if (Math.Abs(evt.oldRect.width - evt.newRect.width) >= 1)
                CalculateTabHeaderDropdown(evt.newRect.width - 13f); // account for scroll bar width
        }

        protected float GetTotalWidthForTabHeader(string tabId)
        {
            var headerButton = m_HeaderButtons[tabId];

            return TextUtilities.MeasureVisualElementTextSize(headerButton, headerButton.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x + m_CalculatedTabHorizontalMarginAndPadding;
        }

        protected void AddTabDropdownAction(Button tabHeader)
        {
            var tabId = tabHeader.userData.ToString();
            m_TabHeaderDropdown.menu.AppendAction(tabHeader.text, a => { SelectTabHeaderFromDropdown(tabHeader); }, DropdownMenuAction.AlwaysEnabled, tabId);
        }

        protected void ClearDropdown()
        {
            m_TabHeaderDropdown.menu.ClearItems();
        }

        protected void RemoveFirstDropdownAction()
        {
            int itemCount = m_TabHeaderDropdown.menu.MenuItems().Count;
            if (itemCount > 0)
            {
                m_TabHeaderDropdown.menu.RemoveItemAt(0);
            }
        }

        private void SelectTabHeaderFromDropdown(Button tabHeader)
        {
            var tabHeaderId = tabHeader.userData.ToString();

            UIUtils.SetElementDisplay(tabHeader, true);
            SelectTab(tabHeaderId);

            var tabIdsAndAssociatedWidths = m_ValidTabIds.Select(t =>
            {
                return (t, GetTotalWidthForTabHeader(t));
            }).ToList();

            var dropdownTabIds = CalculateDropdownTabIds(rect.width, m_SelectedTabId, k_DropdownButtonWidth, tabIdsAndAssociatedWidths);
            ReconstructTabHeaderDropdown(dropdownTabIds);
        }

        private void ReconstructTabHeaderDropdown(HashSet<string> dropdownTabIds)
        {
            ClearDropdown();

            foreach (var tabId in m_ValidTabIds)
            {
                if (dropdownTabIds.Contains(tabId))
                {
                    UIUtils.SetElementDisplay(m_HeaderButtons[tabId], false);
                    AddTabDropdownAction(m_HeaderButtons[tabId]);
                }
                else
                {
                    UIUtils.SetElementDisplay(m_HeaderButtons[tabId], true);
                }
            }

            UIUtils.SetElementDisplay(m_TabHeaderDropdown, m_TabHeaderDropdown.menu.MenuItems().Any());
        }

        // use HashSet since if selected ID is in the middle of tabs, order can't be preserved anyway- and since order is known internally through m_ValidTabIds, use HashSet for fast lookup
        public static HashSet<string> CalculateDropdownTabIds(float windowWidth, string selectedTabId, float dropdownButtonWidth, List<(string tabId, float tabEstimatedWidth)> tabIdsAndAssociatedWidths)
        {
            var dropdownTabIds = new HashSet<string>();

            if (float.IsNaN(windowWidth))
                return dropdownTabIds;

            var totalTabWidth = tabIdsAndAssociatedWidths.Sum(t => t.tabEstimatedWidth);
            if (totalTabWidth < windowWidth)
                return dropdownTabIds;

            for (var i = tabIdsAndAssociatedWidths.Count - 1; i >= 0; --i)
            {
                var elem = tabIdsAndAssociatedWidths[i];

                // always ensure at least the selected tab is visible
                if (elem.tabId == selectedTabId)
                    continue;

                totalTabWidth -= elem.tabEstimatedWidth;
                dropdownTabIds.Add(elem.tabId);

                // at this point we know at least one element must be added to the dropdown, so begin considering the dropdown
                //  width in the comparison
                if (totalTabWidth + dropdownButtonWidth < windowWidth)
                    break;
            }

            return dropdownTabIds;
        }

        protected void CalculateTabHeaderDropdown(float newWidth)
        {
            var tabIdsAndAssociatedWidths = m_ValidTabIds.Select(t =>
            {
                return (t, GetTotalWidthForTabHeader(t));
            }).ToList();

            var dropdownTabIds = CalculateDropdownTabIds(rect.width, m_SelectedTabId, k_DropdownButtonWidth, tabIdsAndAssociatedWidths);
            ReconstructTabHeaderDropdown(dropdownTabIds);
        }

        public void AddTab(T tab)
        {
            if (m_TabElements.ContainsKey(tab.id))
                return;

            var headerButton = CreateHeaderButtonForTab(tab);

            m_HeaderButtons.Add(tab.id, headerButton);
            m_TabElements.Add(tab.id, tab);

            m_TabHeaderContainer.Add(headerButton);
            m_BodyContainer.Add(tab);

            m_ValidTabIds.Add(tab.id);
        }

        public void RemoveTab(T tab)
        {
            m_TabElements.Remove(tab.id);
            m_BodyContainer.Remove(tab);

            if (m_HeaderButtons.TryGetValue(tab.id, out var header))
            {
                m_HeaderButtons.Remove(tab.id);
                m_TabHeaderContainer.Remove(header);
            }
        }

        public void ClearTabs()
        {
            m_HeaderButtons.Clear();
            m_TabElements.Clear();

            m_TabHeaderContainer.Clear();
            m_BodyContainer.Clear();
        }

        private Button CreateHeaderButtonForTab(T tab)
        {
            var headerButton = new Button();
            headerButton.name = $"{tab.id}Button";
            headerButton.userData = tab.id;
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
