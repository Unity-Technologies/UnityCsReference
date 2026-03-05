// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class SimplePageWithPackages : SimplePage, IPage<IPackage>
    {
        public static readonly PageSortOption[] k_DefaultSupportedSortOptions = { PageSortOption.NameAsc, PageSortOption.NameDesc, PageSortOption.PublishedDateDesc };
        public static readonly PageFilterStatus[] k_DefaultSupportedStatusFilters = { PageFilterStatus.UpdateAvailable };

        protected virtual bool updateWhenInactive => false;

        [NonSerialized]
        protected IPackageDatabase m_PackageDatabase;
        [ExcludeFromCodeCoverage]
        public void ResolveDependencies(IPackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        protected SimplePageWithPackages(IPackageDatabase packageDatabase)
        {
            ResolveDependencies(packageDatabase);

            UpdateSupportedSortOptions(k_DefaultSupportedSortOptions, false);
            UpdateSupportedStatuses(k_DefaultSupportedStatusFilters, false);
        }

        public override void OnEnable()
        {
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PackageDatabase.onPackageUniqueIdFinalize += OnPackageUniqueIdFinalize;
        }

        public override void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PackageDatabase.onPackageUniqueIdFinalize -= OnPackageUniqueIdFinalize;
        }

        protected void OnPackagesChanged(PackagesChangeArgs args)
        {
            // We don't need to worry about packages change when the page is not active, because when an inactive page
            // becomes active, it will rebuild its visual states from scratch anyway.
            if (!isActive && !updateWhenInactive)
                return;

            var addList = new List<string>();
            var updateList = new List<string>();
            var removeList = new List<string>(args.removed.SelectAsEnumerable(i => i.uniqueId).Filter(i => visualStates.Contains(i)));
            foreach (var package in args.added.Join(args.updated))
            {
                if (ShouldInclude(package))
                {
                    if (visualStates.Contains(package.uniqueId))
                        updateList.Add(package.uniqueId);
                    else
                        addList.Add(package.uniqueId);
                }
                else if (visualStates.Contains(package.uniqueId))
                    removeList.Add(package.uniqueId);
            }

            IncrementalListUpdate(addList, updateList, removeList);
        }

        private void ApplyFiltersAndSearchText()
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in visualStates)
            {
                var newVisibility = MatchesSearchTextAndFilter(state.itemUniqueId);
                if (state.visible == newVisibility)
                    continue;
                state.visible = newVisibility;
                changedVisualStates.Add(state);
            }

            if (changedVisualStates.Count > 0)
                TriggerOnVisualStateChange(changedVisualStates);
        }

        protected override void RebuildVisualStateList()
        {
            var includedItems = new List<(IPackage package, VisualState visualState)>();
            foreach (var p in m_PackageDatabase.allPackages)
            {
                if (!ShouldInclude(p))
                    continue;
                var visualState = m_VisualStateList.Get(p.uniqueId) ?? new VisualState(p.uniqueId);
                visualState.groupName = GetGroupName(p);
                visualState.lockedByDefault = m_PackageDatabase.IsUsedByFeature(p.versions.installed);
                includedItems.Add((p, visualState));
            }
            includedItems.Sort(new Comparer(filters.sortOption, CompareGroupName));

            m_VisualStateList.Rebuild(includedItems.SelectToNewArray(i => i.visualState));
        }

        private void OnPackageUniqueIdFinalize(string tempPackageUniqueId, string finalPackageUniqueId)
        {
            if (!GetSelection().Contains(tempPackageUniqueId))
                return;
            AmendSelection(new[] { finalPackageUniqueId }, new[] { tempPackageUniqueId }, false);
        }

        protected override bool MatchesSearchTextAndFilter(string itemUniqueId)
        {
            var filterByStatus = filters.status;
            var package = m_PackageDatabase.GetPackage(itemUniqueId);
            return package != null
                   && (filterByStatus != PageFilterStatus.UpdateAvailable || package.state == PackageState.UpdateAvailable)
                   && (filterByStatus != PageFilterStatus.SubscriptionBased || package.hasEntitlements)
                   && package.versions.primary.MatchesSearchText(trimmedSearchText);
        }

        public virtual string GetGroupName(IPackage package) => string.Empty;
        public abstract bool ShouldInclude(IPackage item);

        private class Comparer : IComparer<(IPackage package, VisualState visualState)>
        {
            private readonly PageSortOption m_SortOption;
            private readonly Comparison<string> m_GroupNameComparison;
            public Comparer(PageSortOption sortOption, Comparison<string> compareGroupName)
            {
                m_SortOption = sortOption;
                m_GroupNameComparison = compareGroupName;
            }

            private static int CompareByDisplayName(IPackageVersion x, IPackageVersion y)
            {
                return string.Compare(x.displayName, y.displayName, StringComparison.CurrentCultureIgnoreCase);
            }

            public int Compare((IPackage package, VisualState visualState) x, (IPackage package, VisualState visualState) y)
            {
                if (m_GroupNameComparison != null)
                {
                    var groupNameCompareResult = m_GroupNameComparison(x.visualState.groupName, y.visualState.groupName);
                    if (groupNameCompareResult != 0)
                        return groupNameCompareResult;
                }

                var versionX = x.package?.versions.primary;
                var versionY = y.package?.versions.primary;
                if (versionX == null || versionY == null)
                    return 0;

                var result = 0;
                switch (m_SortOption)
                {
                    case PageSortOption.NameAsc:
                        return CompareByDisplayName(versionX, versionY);
                    case PageSortOption.NameDesc:
                        return -CompareByDisplayName(versionX, versionY);
                    case PageSortOption.PublishedDateDesc:
                        result = -(versionX.publishedDate ?? DateTime.MinValue).CompareTo(versionY.publishedDate ?? DateTime.MinValue);
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
                return result != 0 ? result : CompareByDisplayName(versionX, versionY);
            }
        }
    }
}
