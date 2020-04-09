// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PaginatedPage : BasePage
    {
        [SerializeField]
        private PaginatedVisualStateList m_VisualStateList = new PaginatedVisualStateList();

        public override long numTotalItems => m_VisualStateList.numTotalItems;

        public override long numCurrentItems => m_VisualStateList.numItems;

        public bool morePackagesToFetch => numCurrentItems < numTotalItems;

        public override IEnumerable<VisualState> visualStates => m_VisualStateList;

        public PaginatedPage(PackageFilterTab tab, PageCapability capability) : base(tab, capability)
        {
        }

        public override VisualState GetVisualState(string packageUniqueId)
        {
            return m_VisualStateList.GetVisualState(packageUniqueId);
        }

        public override void UpdateFilters(PageFilters filters)
        {
            if (this.filters.Equals(filters))
                return;

            m_Filters = filters.Clone();
            var queryArgs = BuildQueryFromFilter(0, PackageManagerWindow.instance?.packageList?.CalculateNumberOfPackagesToDisplay() ?? PageManager.k_DefaultPageSize);
            AssetStoreClient.instance.ListPurchases(queryArgs, false);

            m_VisualStateList.ClearList();
            m_VisualStateList.ClearExtraItems();

            RefreshVisualStates();
            TriggerOnListRebuild();
        }

        // in the case of paginated pages, we don't need to change the list itself (it's predefined and trumps other custom filtering)
        // hence we don't need to trigger any list change event.
        public override void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
        {
            var addOrUpdateList = new List<IPackage>();
            var removeList = removed.Where(Contains).ToList();
            foreach (var package in added.Concat(postUpdate))
            {
                if (PackageFiltering.instance.FilterByCurrentTab(package))
                    addOrUpdateList.Add(package);
                else if (Contains(package))
                    removeList.Add(package);
            }

            if (addOrUpdateList.Any() || removeList.Any())
            {
                TriggerOnListUpdate(addOrUpdateList, removeList, addOrUpdateList.Any());

                RefreshVisualStates();
            }
        }

        public override void Rebuild()
        {
            TriggerOnListRebuild();
            RefreshVisualStates();
        }

        private void RefreshVisualStates()
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in m_VisualStateList ?? Enumerable.Empty<VisualState>())
            {
                var package = PackageDatabase.instance.GetPackage(state.packageUniqueId);
                if (package != null)
                {
                    var visible = PackageFiltering.instance.FilterByCurrentSearchText(package);
                    if (state.visible != visible)
                    {
                        state.visible = visible;
                        changedVisualStates.Add(state);
                    }
                }
            }

            if (changedVisualStates.Any())
                TriggerOnVisualStateChange(changedVisualStates);
        }

        private PurchasesQueryArgs BuildQueryFromFilter(int startIndex, int limit)
        {
            return new PurchasesQueryArgs
            {
                startIndex = startIndex,
                limit = limit,
                searchText = filters?.searchText,
                statuses = filters?.statuses,
                categories = filters?.categories,
                labels = filters?.labels,
                orderBy = filters?.orderBy,
                isReverseOrder = filters?.isReverseOrder ?? false
            };
        }

        public override void LoadMore(int numberOfPackages)
        {
            if (numCurrentItems >= numTotalItems)
                return;

            var queryArgs = BuildQueryFromFilter((int)numCurrentItems, numberOfPackages);
            AssetStoreClient.instance.ListPurchases(queryArgs, false);
        }

        public override void Load(IPackage package, IPackageVersion version = null)
        {
            if (!Contains(package))
            {
                long productId;
                if (package == null || !long.TryParse(package.uniqueId, out productId))
                    return;

                m_VisualStateList.AddExtraItem(package.uniqueId);
            }
            TriggerOnListUpdate(new[] { package }, Enumerable.Empty<IPackage>(), false);
            SetSelected(package.uniqueId, version?.uniqueId ?? package.versions.primary?.uniqueId);
        }

        public void OnProductFetched(long productId)
        {
            var uniqueId = productId.ToString();
            if (Contains(uniqueId))
                return;

            m_VisualStateList.AddExtraItem(productId.ToString());

            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                var package = PackageDatabase.instance.GetPackage(uniqueId);
                TriggerOnListUpdate(new[] { package }, Enumerable.Empty<IPackage>(), false);
                SetSelected(package?.uniqueId, package?.versions.primary?.uniqueId);
            }
        }

        public void OnProductListFetched(AssetStorePurchases purchases, bool fetchDetailsCalled)
        {
            var isSet = purchases.queryArgs?.isFilterSet == true;
            if (isSet && !filters.Equals(purchases.queryArgs))
                return;

            if (purchases.startIndex > 0 && numTotalItems != purchases.total)
            {
                // if a new page has arrived but the total has changed or the searchText has changed, do a re-fetch
                var queryArgs = BuildQueryFromFilter((int)numCurrentItems, purchases.startIndex + purchases.list.Count);
                AssetStoreClient.instance.ListPurchases(queryArgs);
                return;
            }

            var oldPackageIds = new HashSet<string>(m_VisualStateList.Select(v => v.packageUniqueId));
            var newPackageIds = purchases.productIds.Select(id => id.ToString()).ToList();
            if (purchases.startIndex == 0)
            {
                // override the result if the new list starts from index 0 (meaning it's a refresh)
                m_VisualStateList.Rebuild(newPackageIds);
                m_VisualStateList.ClearExtraItems();
                m_VisualStateList.SetTotal(purchases.total);
            }
            else if (purchases.startIndex == numCurrentItems)
            {
                // append the result if it is the next page
                m_VisualStateList.AddRange(newPackageIds);
                m_VisualStateList.ClearExtraItems();
            }
            else
            {
                // if the content is neither starting from zero or next page, we simply discard it
                return;
            }

            if (!fetchDetailsCalled && purchases.list.Any())
                AssetStoreClient.instance.FetchDetails(purchases.productIds);

            // only try to rebuild the list immediately if we are already on the `AssetStore` tab.
            // if not we'll just wait for tab switch which will trigger the rebuild as well
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                HashSet<string> removed = null;
                List<string> added = null;
                if (purchases.startIndex == 0)
                {
                    removed = oldPackageIds;
                    added = new List<string>();
                    foreach (var id in newPackageIds)
                    {
                        if (removed.Contains(id))
                            removed.Remove(id);
                        else
                            added.Add(id);
                    }
                }
                else if (purchases.startIndex == oldPackageIds.Count)
                {
                    added = newPackageIds;
                }

                var addedPackages = added?.Select(id => PackageDatabase.instance.GetPackage(id));
                var removedPackages = removed?.Select(id => PackageDatabase.instance.GetPackage(id));
                TriggerOnListUpdate(addedPackages, removedPackages, false);
            }

            RefreshVisualStates();
        }

        public override bool Contains(string packageUniqueId)
        {
            return m_VisualStateList?.Contains(packageUniqueId) ?? false;
        }

        public override void SetExpanded(string packageUniqueId, bool value)
        {
            if (m_VisualStateList.SetExpanded(packageUniqueId, value))
                TriggerOnVisualStateChange(new[] { GetVisualState(packageUniqueId) });
        }

        public override void SetSeeAllVersions(string packageUniqueId, bool value)
        {
            if (m_VisualStateList.SetSeeAllVersions(packageUniqueId, value))
                TriggerOnVisualStateChange(new[] { GetVisualState(packageUniqueId) });
        }
    }
}
