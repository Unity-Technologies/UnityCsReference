// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsTabView : BaseTabView<PackageDetailsTabElement>
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsTabView> { }

        private Label m_EntitlementsErrorLabel;

        public IEnumerable<PackageDetailsTabElement> orderedTabs => m_BodyContainer.Children().OfType<PackageDetailsTabElement>();

        private IPackageVersion m_Version = null;
        private HashSet<PackageDetailsTabElement> m_DeferredRefreshTracker = new HashSet<PackageDetailsTabElement>();

        public PackageDetailsTabView()
        {
            m_HeaderContainer.name = "packageDetailsTabViewHeaderContainer";
            m_BodyContainer.name = "packageDetailsTabViewBodyContainer";

            m_EntitlementsErrorLabel = new Label(L10n.Tr("Information is unavailable because the package license isn't registered to your user account."));
            m_EntitlementsErrorLabel.AddClasses("packageTabsEntitlementsError");
            Add(m_EntitlementsErrorLabel);
        }

        public void RefreshAllTabs(IPackageVersion version)
        {
            RefreshTabs(tabs, version);
            UIUtils.SetElementDisplay(m_BodyContainer, !version.hasEntitlementsError);
            UIUtils.SetElementDisplay(m_EntitlementsErrorLabel, version.hasEntitlementsError);
        }

        public void RefreshTabs(IEnumerable<string> tabIds, IPackageVersion version)
        {
            var tabs = tabIds.Select(id => GetTab(id)).Where(tab => tab != null);
            RefreshTabs(tabs, version);
        }

        public void RefreshTab(string tabId, IPackageVersion version)
        {
            var tabs = new[] { GetTab<PackageDetailsTabElement>(tabId) }.Where(tab => tab != null);
            RefreshTabs(tabs, version);
        }

        private void RefreshTabs(IEnumerable<PackageDetailsTabElement> tabs, IPackageVersion version)
        {
            m_Version = version;
            foreach (var tab in tabs)
            {
                m_DeferredRefreshTracker.Add(tab);
                if (!RefreshTabAndHeaderVisibility(tab, version))
                    m_DeferredRefreshTracker.Remove(tab);
            }

            UpdateSelectionIfCurrentSelectionIsInvalid();
            DeferredRefresh();
        }

        private void UpdateSelectionIfCurrentSelectionIsInvalid()
        {
            if (UIUtils.IsElementVisible(GetTab(selectedTabId)))
                return;

            var firstValidTab = orderedTabs.FirstOrDefault(tab => tab.IsValid(m_Version));
            SelectTab(firstValidTab);
        }

        private bool RefreshTabAndHeaderVisibility(PackageDetailsTabElement tab, IPackageVersion version)
        {
            var tabHeader = m_HeaderButtons[tab.id];
            var isValid = tab.IsValid(version);
            UIUtils.SetElementDisplay(tabHeader, isValid);
            UIUtils.SetElementDisplay(tab, isValid && tab.id == selectedTabId);
            return isValid;
        }

        private void DeferredRefresh()
        {
            var selectedTab = GetTab(selectedTabId);
            if (m_DeferredRefreshTracker.Contains(selectedTab))
            {
                selectedTab.Refresh(m_Version);
                m_DeferredRefreshTracker.Remove(selectedTab);
            }
        }

        protected override void OnTabHeaderClicked(PackageDetailsTabElement tab)
        {
            base.OnTabHeaderClicked(tab);
            PackageManagerWindowAnalytics.SendEvent("changeDetailsTab", m_Version?.uniqueId);
        }

        public override bool SelectTab(PackageDetailsTabElement tabToSelect)
        {
            var tabSwitched = base.SelectTab(tabToSelect);
            if (tabSwitched)
                DeferredRefresh();
            return tabSwitched;
        }
    }
}
