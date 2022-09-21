// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class SimplePage : BasePage
    {
        [SerializeField]
        private VisualStateList m_VisualStateList = new VisualStateList();

        public override IVisualStateList visualStates => m_VisualStateList;

        private List<SubPage> m_SubPages;
        public override IEnumerable<SubPage> subPages => m_SubPages ?? Enumerable.Empty<SubPage>();

        public override string contentType { get => currentSubPage?.contentType; set {} }

        [SerializeField]
        private string m_SelectedSubPageName;
        public override SubPage currentSubPage
        {
            get => subPages.FirstOrDefault(page => page.name == m_SelectedSubPageName) ?? subPages.FirstOrDefault();
            set
            {
                var newSelectedSubpageName = value?.name ?? string.Empty;
                if (m_SelectedSubPageName == newSelectedSubpageName)
                    return;
                m_SelectedSubPageName = newSelectedSubpageName;
                RebuildVisualStatesAndUpdateVisibilityWithSearchText();
                TriggerOnSubPageChanged();
            }
        }

        public SimplePage(PackageDatabase packageDatabase,
                          PackageManagerPrefs packageManagerPrefs,
                          PackageFilterTab tab,
                          PageCapability capability)
            :base(packageDatabase, packageManagerPrefs, tab, capability)
        {
            ResolveDependencies(packageDatabase, packageManagerPrefs);
        }

        public override bool UpdateFilters(PageFilters filters)
        {
            if (!base.UpdateFilters(filters))
                return false;

            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
            return true;
        }

        public override void UpdateSearchText(string searchText)
        {
            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
        }

        public override void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            var visualStates = new HashSet<VisualState>();

            foreach (var packageUniqueId in packageUniqueIds)
            {
                var visualState = m_VisualStateList.Get(packageUniqueId);
                if (visualState != null && visualState.userUnlocked != unlocked)
                {
                    visualState.userUnlocked = unlocked;
                    visualStates.Add(visualState);
                }
            }
            TriggerOnVisualStateChange(visualStates);
        }

        public override void ResetUserUnlockedState()
        {
            var unlockedVisualStates = visualStates.Where(v => v.userUnlocked);
            foreach (var visualState in unlockedVisualStates)
                visualState.userUnlocked = false;
            TriggerOnVisualStateChange(unlockedVisualStates);
        }

        public override void TriggerOnSelectionChanged(bool isExplicitUserSelection = false)
        {
            if (subPages.Skip(1).Any())
            {
                var packages = GetSelection().Select(s => m_PackageDatabase.GetPackage(s.packageUniqueId)).ToArray();
                if (packages.Any(p => currentSubPage?.filter?.Invoke(p) != true))
                    currentSubPage = subPages.FirstOrDefault(subPage => packages.All(p => subPage.filter?.Invoke(p) == true)) ?? currentSubPage;
            }
            base.TriggerOnSelectionChanged(isExplicitUserSelection);
        }

        public void RebuildVisualStatesAndUpdateVisibilityWithSearchText()
        {
            RebuildAndReorderVisualStates();
            TriggerListRebuild();
            UpdateVisualStateVisbilityWithSearchText();
        }

        public override void OnActivated()
        {
            ResetUserUnlockedState();
            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
            TriggerOnSelectionChanged();
        }

        public override void OnDeactivated()
        {
            var selectedVisualStates = GetSelectedVisualStates();
            var selectedGroups = new HashSet<string>(selectedVisualStates.Select(v => v.groupName).Where(groupName => !string.IsNullOrEmpty(groupName)));
            foreach (var group in selectedGroups)
                SetGroupExpanded(group, true);
        }

        public override void RebuildAndReorderVisualStates()
        {
            var isUpdateAvailableOnly = m_Filters?.updateAvailableOnly ?? false;
            var isSubscriptionBasedOnly = m_Filters?.subscriptionBasedOnly ?? false;
            var subPage = currentSubPage;
            var packages = m_PackageDatabase.allPackages.Where(
                p => p.IsInTab(tab)
                && (subPage?.filter?.Invoke(p) ?? true)
                && (!isUpdateAvailableOnly || p.state == PackageState.UpdateAvailable)
                && (!isSubscriptionBasedOnly || p.hasEntitlements));
            var orderBy = m_Filters?.orderBy ?? string.Empty;
            var isReversOrder = m_Filters?.isReverseOrder ?? false;
            IOrderedEnumerable<IPackage> orderedPackages;
            if (orderBy == "name")
            {
                orderedPackages = !isReversOrder? packages.OrderBy(p => p.name) : packages.OrderByDescending(p => p.name);
            }
            else if (orderBy == "publishedDate")
            {
                if (!isReversOrder)
                    orderedPackages = packages.
                        OrderBy(p => p.versions.primary?.publishedDate ?? DateTime.MinValue).
                        ThenBy(p => p.versions.primary?.displayName ?? p.name);
                else
                    orderedPackages = packages.
                        OrderByDescending(p => p.versions.primary?.publishedDate ?? DateTime.MinValue).
                        ThenBy(p => p.versions.primary?.displayName ?? p.name);
            }
            else if (orderBy == "entitlements")
            {
                if (!isReversOrder)
                    orderedPackages = packages.
                        OrderBy(p => p.hasEntitlements ? 0 : 1).
                        ThenBy(p => p.versions.primary?.displayName ?? p.name);
                else
                    orderedPackages = packages.
                        OrderByDescending(p => p.hasEntitlements ? 0 : 1).
                        ThenBy(p => p.versions.primary?.displayName ?? p.name);
            }
            else if (orderBy == "hasUpdate")
            {
                if (!isReversOrder)
                    orderedPackages = packages.
                        OrderBy(p => p.state == PackageState.UpdateAvailable ? 0 : 1).
                        ThenBy(p => p.versions.primary?.displayName ?? p.name);
                else
                    orderedPackages = packages.
                        OrderByDescending(p => p.state == PackageState.UpdateAvailable ? 0 : 1).
                        ThenBy(p => p.versions.primary?.displayName ?? p.name);
            }
            else // displayName
            {
                orderedPackages = !isReversOrder? packages.OrderBy(p => p.versions.primary?.displayName ?? p.name) : packages.OrderByDescending(p => p.versions.primary?.displayName ?? p.name);
            }

            var orderedPackageIdGroupsAndDefaultLockedStates = orderedPackages.Select(
                p => (packageUniqueId: p.uniqueId, groupName: GetGroupName(p), lockedByDefault: GetDefaultLockState(p)));
            m_VisualStateList.Rebuild(orderedPackageIdGroupsAndDefaultLockedStates);
        }

        public override bool GetDefaultLockState(IPackage package)
        {
            return package.versions.installed?.isDirectDependency != true &&
                m_PackageDatabase.GetFeaturesThatUseThisPackage(package.versions.installed)?.Any() == true;
        }

        public override void AddSubPage(SubPage subPage)
        {
            if (subPage == null || subPages.Any(page => page.name == subPage.name))
                return;
            if (m_SubPages == null)
                m_SubPages = new List<SubPage>();
            m_SubPages.Add(subPage);
            m_SubPages.Sort((p1, p2) => p1.priority - p2.priority);
            if (m_SelectedSubPageName == subPage.name)
                RebuildVisualStatesAndUpdateVisibilityWithSearchText();
            TriggerOnSubPageChanged();
        }

        public override string GetGroupName(IPackage package)
        {
            return currentSubPage?.getGroupName?.Invoke(package) ?? base.GetGroupName(package);
        }

        // All the following load functions do nothing, because for a SimplePage we already know the complete list and there's no more to load
        public override void LoadMore(long numberOfPackages) { }
        public override void Load(string packageUniqueId) { }
        public override void LoadExtraItems(IEnumerable<IPackage> packages) { }
    }
}
