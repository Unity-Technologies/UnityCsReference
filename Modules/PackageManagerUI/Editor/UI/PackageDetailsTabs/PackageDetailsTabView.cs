// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsTabView : BaseTabView<PackageDetailsTabElement>
    {
        [Serializable]
        public new class UxmlSerializedData : BaseTabView<PackageDetailsTabElement>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseTabView<PackageDetailsTabElement>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new PackageDetailsTabView();
        }

        private readonly Label m_EntitlementsErrorLabel;

        public IEnumerable<PackageDetailsTabElement> orderedTabs => m_BodyContainer.Children().FilterByType<PackageDetailsTabElement>();

        private IPackageVersion m_Version = null;
        private readonly HashSet<PackageDetailsTabElement> m_DeferredRefreshTracker = new();

        // hardcoded constant values taken from styles applied to Package Details tab headers
        // if these ever change, these values will need to be updated
        private const float k_TabHeaderPaddingLeft = 10.0f;
        private const float k_TabHeaderPaddingRight = 10.0f;

        public PackageDetailsTabView()
        {
            m_TabHeaderContainer.name = "packageDetailsTabViewHeaderContainer";
            m_BodyContainer.name = "packageDetailsTabViewBodyContainer";
            m_TabHeaderDropdown.name = "packageDetailsTabViewHeaderDropdown";
            m_TabHeaderDropdown.SetIcon(Icon.PullDown);

            m_EntitlementsErrorLabel = new Label(L10n.Tr("Information is unavailable because the package license isn't registered to your user account."));
            m_EntitlementsErrorLabel.AddToClassList("packageDetailsTabMessage");
            Add(m_EntitlementsErrorLabel);

            m_CalculatedTabHorizontalMarginAndPadding = k_TabHeaderPaddingLeft + k_TabHeaderPaddingRight;
        }

        public void RefreshAllTabs(IPackageVersion version)
        {
            m_ValidTabIds.Clear();
            RefreshTabs(tabs, version);
            UIUtils.SetElementDisplay(m_BodyContainer, !version.hasEntitlementsError);
            UIUtils.SetElementDisplay(m_EntitlementsErrorLabel, version.hasEntitlementsError);

            CalculateTabHeaderDropdown();
        }

        public void RefreshTabs(IEnumerable<string> tabIds, IPackageVersion version)
        {
            var tabsToRefresh = tabIds.SelectAsEnumerable(GetTab).Filter(tab => tab != null);
            RefreshTabs(tabsToRefresh, version);
        }

        public void RefreshTab(string tabId, IPackageVersion version)
        {
            var tab = GetTab<PackageDetailsTabElement>(tabId);
            if (tab != null)
                RefreshTabs(new [] { tab }, version);
        }

        private void RefreshTabs(IEnumerable<PackageDetailsTabElement> tabsToRefresh, IPackageVersion version)
        {
            m_Version = version;
            foreach (var tab in tabsToRefresh)
            {
                m_DeferredRefreshTracker.Add(tab);
                if (!RefreshTabAndHeaderVisibility(tab, version))
                {
                    if (m_ValidTabIds.Contains(tab.id))
                    {
                        m_ValidTabIds.Remove(tab.id);
                    }
                    m_DeferredRefreshTracker.Remove(tab);
                }
                else if (!m_ValidTabIds.Contains(tab.id))
                {
                    m_ValidTabIds.Add(tab.id);
                }
            }

            UpdateSelectionIfCurrentSelectionIsInvalid();
            DeferredRefresh();
        }

        private void UpdateSelectionIfCurrentSelectionIsInvalid()
        {
            if (UIUtils.IsElementVisible(GetTab(selectedTabId)))
                return;

            SelectTab(orderedTabs.FirstMatch(tab => tab.IsValid(m_Version)));
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
