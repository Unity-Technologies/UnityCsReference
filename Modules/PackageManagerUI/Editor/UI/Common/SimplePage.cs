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

        public override long numTotalItems => m_VisualStateList.numTotalItems;

        public override long numCurrentItems => m_VisualStateList.numItems;

        public override IEnumerable<VisualState> visualStates => m_VisualStateList;

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
                Rebuild();
                TriggerOnSubPageChanged();
            }
        }

        [NonSerialized]
        private PackageFiltering m_PackageFiltering;
        public void ResolveDependencies(PackageDatabase packageDatabase, PackageFiltering packageFiltering)
        {
            ResolveDependencies(packageDatabase);
            m_PackageFiltering = packageFiltering;
        }

        public SimplePage(PackageDatabase packageDatabase, PackageFiltering packageFiltering, PackageFilterTab tab, PageCapability capability) : base(packageDatabase, tab, capability)
        {
            ResolveDependencies(packageDatabase, packageFiltering);
        }

        public override bool UpdateFilters(PageFilters filters)
        {
            if (!base.UpdateFilters(filters))
                return false;

            Rebuild();
            return true;
        }

        public override void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
        {
            var addList = new List<IPackage>();
            var updateList = new List<IPackage>();
            var removeList = removed.Where(Contains).ToList();
            foreach (var package in added.Concat(postUpdate))
            {
                if (m_PackageFiltering.FilterByCurrentTab(package))
                {
                    if (Contains(package))
                        updateList.Add(package);
                    else
                        addList.Add(package);
                }
                else if (Contains(package))
                    removeList.Add(package);
            }

            if (addList.Any() || updateList.Any() || removeList.Any())
            {
                RebuildOrderedVisualStates();

                TriggerOnListUpdate(addList, updateList, removeList);

                RefreshVisualStates();
            }
        }

        public override void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            var visualStates = new HashSet<VisualState>();

            foreach (var packageUniqueId in packageUniqueIds)
            {
                var visualState = m_VisualStateList.GetVisualState(packageUniqueId);
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
            {
                visualState.userUnlocked = false;
                visualState.expanded = false;
            }
            TriggerOnVisualStateChange(unlockedVisualStates);
        }

        public override void TriggerOnSelectionChanged()
        {
            if (subPages.Skip(1).Any())
            {
                var packages = GetSelection().Select(s => m_PackageDatabase.GetPackage(s.packageUniqueId)).ToArray();
                if (packages.Any(p => currentSubPage?.filter?.Invoke(p) != true))
                    currentSubPage = subPages.FirstOrDefault(subPage => packages.All(p => subPage.filter?.Invoke(p) == true)) ?? currentSubPage;
            }
            base.TriggerOnSelectionChanged();
        }

        public override void Rebuild()
        {
            RebuildOrderedVisualStates();
            TriggerOnListRebuild();
            RefreshVisualStates();
        }

        private void RebuildOrderedVisualStates()
        {
            var isUpdateAvailableOnly = m_Filters?.updateAvailableOnly ?? false;
            var isSubscriptionBasedOnly = m_Filters?.subscriptionBasedOnly ?? false;
            var subPage = currentSubPage;
            var packages = m_PackageDatabase.allPackages.Where(
                p => m_PackageFiltering.FilterByCurrentTab(p)
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

            var orderedPackageIdGroupsAndDefaultLockedStates = orderedPackages.Select(p => new Tuple<string, string, bool>(p.uniqueId, GetGroupName(p),
                GetDefaultLockState(p)));
            m_VisualStateList.Rebuild(orderedPackageIdGroupsAndDefaultLockedStates);
        }

        public override bool GetDefaultLockState(IPackage package)
        {
            return package.versions.installed?.isDirectDependency != true &&
                m_PackageDatabase.GetFeatureDependents(package.versions.installed)?.Any() == true;
        }

        private void RefreshVisualStates()
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in m_VisualStateList)
            {
                var stateChanged = false;

                var package = m_PackageDatabase.GetPackage(state.packageUniqueId);
                var visible = m_PackageFiltering.FilterByCurrentSearchText(package);
                if (state.visible != visible)
                {
                    state.visible = visible;
                    stateChanged = true;
                }

                var expandable = GetSelection().Count <= 1;
                if (state.expandable != expandable)
                {
                    state.expandable = expandable;
                    stateChanged = true;
                }

                if (stateChanged)
                    changedVisualStates.Add(state);
            }

            if (changedVisualStates.Any())
                TriggerOnVisualStateChange(changedVisualStates);
        }

        public override VisualState GetVisualState(string packageUniqueId)
        {
            return m_VisualStateList.GetVisualState(packageUniqueId);
        }

        public override void SetExpanded(string packageUniqueId, bool value)
        {
            if (m_VisualStateList.SetExpanded(packageUniqueId, value))
                TriggerOnVisualStateChange(new[] { m_VisualStateList.GetVisualState(packageUniqueId) });
        }

        public override void SetSeeAllVersions(string packageUniqueId, bool value)
        {
            if (m_VisualStateList.SetSeeAllVersions(packageUniqueId, value))
                TriggerOnVisualStateChange(new[] { m_VisualStateList.GetVisualState(packageUniqueId) });
        }

        public override bool Contains(string packageUniqueId)
        {
            return m_VisualStateList.Contains(packageUniqueId);
        }

        public override void LoadMore(long numberOfPackages)
        {
            // do nothing, as for a single page we have a complete/known list and there's no more to load
        }

        public override void Load(IPackage package, IPackageVersion version = null)
        {
            // do nothing, as for a single page we have a complete/known list and there's no more to load
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
                Rebuild();
            TriggerOnSubPageChanged();
        }

        public override string GetGroupName(IPackage package)
        {
            return currentSubPage?.getGroupName?.Invoke(package) ?? base.GetGroupName(package);
        }
    }
}
