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
    internal abstract class SimplePage : BasePage
    {
        public static readonly PageSortOption[] k_DefaultSupportedSortOptions = { PageSortOption.NameAsc, PageSortOption.NameDesc, PageSortOption.PublishedDateDesc };
        public static readonly PageFilters.Status[] k_DefaultSupportedStatusFilters = { PageFilters.Status.UpdateAvailable };

        public override IEnumerable<PageFilters.Status> supportedStatusFilters => k_DefaultSupportedStatusFilters;
        public override IEnumerable<PageSortOption> supportedSortOptions => k_DefaultSupportedSortOptions;

        [SerializeField]
        private VisualStateList m_VisualStateList = new();
        public override IVisualStateList visualStates => m_VisualStateList;

        public override PageCapability capability => PageCapability.SupportLocalReordering;

        public SimplePage(PackageDatabase packageDatabase) : base(packageDatabase) {}

        public override bool UpdateFilters(PageFilters newFilters)
        {
            if (!base.UpdateFilters(newFilters))
                return false;

            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
            return true;
        }

        protected override void RefreshListOnSearchTextChange()
        {
            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
        }

        public override void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            var changedVisualStates = new HashSet<VisualState>();

            foreach (var packageUniqueId in packageUniqueIds)
            {
                var visualState = m_VisualStateList.Get(packageUniqueId);
                if (visualState == null || visualState.userUnlocked == unlocked)
                    continue;
                visualState.userUnlocked = unlocked;
                changedVisualStates.Add(visualState);
            }
            TriggerOnVisualStateChange(changedVisualStates);
        }

        public override void ResetUserUnlockedState()
        {
            var unlockedVisualStates = visualStates.Where(v => v.userUnlocked).ToArray();
            foreach (var visualState in unlockedVisualStates)
                visualState.userUnlocked = false;
            TriggerOnVisualStateChange(unlockedVisualStates);
        }

        public void RebuildVisualStatesAndUpdateVisibilityWithSearchText()
        {
            RebuildAndReorderVisualStates();
            RefreshSupportedStatusFiltersOnEntitlementPackageChange();
            TriggerListRebuild();
            UpdateVisualStateVisibilityWithSearchText();
        }

        public override void OnActivated()
        {
            base.OnActivated();
            ResetUserUnlockedState();
            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
            TriggerOnSelectionChanged();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            var selectedVisualStates = GetSelectedVisualStates();
            var selectedGroups = new HashSet<string>(selectedVisualStates.Select(v => v.groupName).Where(groupName => !string.IsNullOrEmpty(groupName)));
            foreach (var group in selectedGroups)
                SetGroupExpanded(group, true);
        }

        public override void RebuildAndReorderVisualStates()
        {
            var filterByStatus = filters.status;
            var packages = m_PackageDatabase.allPackages.Where(
                p => ShouldInclude(p)
                     && (filterByStatus != PageFilters.Status.UpdateAvailable || p.state == PackageState.UpdateAvailable)
                     && (filterByStatus != PageFilters.Status.SubscriptionBased || p.hasEntitlements));
            var orderedVisualStates = packages.OrderBy(p => p, new PackageComparer(filters.sortOption)).Select(p =>
            {
                var visualState = m_VisualStateList.Get(p.uniqueId) ?? new VisualState(p.uniqueId);
                visualState.groupName = GetGroupName(p);
                visualState.lockedByDefault = GetDefaultLockState(p);
                return visualState;
            }).ToList();
            var orderedGroups = orderedVisualStates.Select(v => v.groupName).Distinct().Where(i => !string.IsNullOrEmpty(i)).ToList();
            if (orderedGroups.Count > 1)
                SortGroupNames(orderedGroups);
            m_VisualStateList.Rebuild(orderedVisualStates, orderedGroups);
        }

        protected virtual void SortGroupNames(List<string> groupNames)
        {
            groupNames.Sort((x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));
        }

        public override bool GetDefaultLockState(IPackage package)
        {
            return package.versions.installed?.isDirectDependency != true &&
                m_PackageDatabase.GetFeaturesThatUseThisPackage(package.versions.installed)?.Any() == true;
        }

        // All the following load functions do nothing, because for a SimplePage we already know the complete list and there's no more to load
        public override void LoadMore(long numberOfPackages) {}
        public override void Load(string packageUniqueId) {}
        public override void LoadExtraItems(IEnumerable<IPackage> packages) {}

        private class PackageComparer : IComparer<IPackage>
        {
            private PageSortOption m_SortOption;
            public PackageComparer(PageSortOption sortOption)
            {
                m_SortOption = sortOption;
            }

            private static int CompareByDisplayName(IPackageVersion x, IPackageVersion y)
            {
                return string.Compare(x.displayName, y.displayName, StringComparison.CurrentCultureIgnoreCase);
            }

            public int Compare(IPackage packageX, IPackage packageY)
            {
                var x = packageX?.versions.primary;
                var y = packageY?.versions.primary;
                if (x == null || y == null)
                    return 0;

                var result = 0;
                switch (m_SortOption)
                {
                    case PageSortOption.NameAsc:
                        return CompareByDisplayName(x, y);
                    case PageSortOption.NameDesc:
                        return -CompareByDisplayName(x, y);
                    case PageSortOption.PublishedDateDesc:
                        result = -(x.publishedDate ?? DateTime.MinValue).CompareTo(y.publishedDate ?? DateTime.MinValue);
                        break;
                    // Sorting by Update date and Purchase date is only available through the Asset Store backend API
                    // So we will resort to default sorting (by display name) in this case.
                    case PageSortOption.UpdateDateDesc:
                    case PageSortOption.PurchasedDateDesc:
                    default:
                        break;
                }
                // We want to use display name as the secondary sort option if the primary sort option returns 0
                // If sort option is set to anything sort option that's not supported, we'll sort by display name
                return result != 0 ? result : CompareByDisplayName(x, y);
            }
        }
    }
}
