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
    internal class PaginatedPage : BasePage
    {
        [SerializeField]
        private PaginatedVisualStateList m_VisualStateList = new PaginatedVisualStateList();

        public override long numTotalItems => m_VisualStateList.numTotalItems;

        public override long numCurrentItems => m_VisualStateList.numItems;

        public bool morePackagesToFetch => numCurrentItems < numTotalItems;

        public override IEnumerable<VisualState> visualStates => m_VisualStateList;

        public override IEnumerable<SubPage> subPages => Enumerable.Empty<SubPage>();

        public override SubPage currentSubPage { get => null; set {} }

        public override string contentType { get; set; }

        [NonSerialized]
        private AssetStoreClient m_AssetStoreClient;
        [NonSerialized]
        private PackageFiltering m_PackageFiltering;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        public void ResolveDependencies(PackageDatabase packageDatabase, AssetStoreClient assetStoreClient, PackageFiltering packageFiltering, PackageManagerPrefs packageManagerPrefs)
        {
            ResolveDependencies(packageDatabase);
            m_AssetStoreClient = assetStoreClient;
            m_PackageFiltering = packageFiltering;
            m_PackageManagerPrefs = packageManagerPrefs;
        }

        public PaginatedPage(string contentType, PackageDatabase packageDatabase, AssetStoreClient assetStoreClient, PackageFiltering packageFiltering, PackageManagerPrefs packageManagerPrefs, PackageFilterTab tab, PageCapability capability) : base(packageDatabase, tab, capability)
        {
            this.contentType = contentType;
            ResolveDependencies(packageDatabase, assetStoreClient, packageFiltering, packageManagerPrefs);
        }

        public override VisualState GetVisualState(string packageUniqueId)
        {
            return m_VisualStateList.GetVisualState(packageUniqueId);
        }

        public override bool UpdateFilters(PageFilters filters)
        {
            if (!base.UpdateFilters(filters))
                return false;

            var queryArgs = BuildQueryFromFilter(0, m_PackageManagerPrefs.numItemsPerPage ?? PageManager.k_DefaultPageSize);
            m_AssetStoreClient.ListPurchases(queryArgs);

            ClearAndRebuild();
            return true;
        }

        // in the case of paginated pages, we don't need to change the list itself (it's predefined and trumps other custom filtering)
        // hence we don't need to trigger any list change event.
        public override void OnPackagesChanged(PackagesChangeArgs args)
        {
            var addList = new List<IPackage>();
            var updateList = new List<IPackage>();
            var removeList = args.removed.Where(Contains).ToList();
            foreach (var package in args.added.Concat(args.updated))
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
                TriggerOnListUpdate(addList, updateList, removeList);

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
                var package = m_PackageDatabase.GetPackage(state.packageUniqueId);
                if (package != null)
                {
                    var visible = tab == PackageFilterTab.AssetStore ? true : m_PackageFiltering.FilterByCurrentSearchText(package);
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
                status = filters?.status,
                categories = filters?.categories,
                labels = filters?.labels,
                orderBy = filters?.orderBy,
                isReverseOrder = filters?.isReverseOrder ?? false
            };
        }

        public void ClearAndRebuild()
        {
            m_VisualStateList.ClearList();
            m_VisualStateList.ClearExtraItems();
            Rebuild();
        }

        public override void LoadMore(long numberOfPackages)
        {
            if (numCurrentItems >= numTotalItems)
                return;

            var queryArgs = BuildQueryFromFilter((int)numCurrentItems, (int)numberOfPackages);
            m_AssetStoreClient.ListPurchases(queryArgs);
        }

        public override void Load(IPackage package, IPackageVersion version = null)
        {
            var packageInPage = Contains(package);
            if (!packageInPage)
            {
                if (package == null || !long.TryParse(package.uniqueId, out _))
                    return;

                m_VisualStateList.AddExtraItem(package.uniqueId);
            }
            if (!packageInPage)
                TriggerOnListUpdate(added: new[] { package });
            else
                TriggerOnListUpdate(updated: new[] { package });

            SetNewSelection(new[] { new PackageAndVersionIdPair(package.uniqueId, version?.uniqueId)});
        }

        public override void LoadExtraItems(IEnumerable<IPackage> packages)
        {
            var addedPackages = packages.Where(p => !Contains(p)).ToArray();
            foreach (var package in addedPackages)
                m_VisualStateList.AddExtraItem(package.uniqueId);
            TriggerOnListUpdate(added: addedPackages);
        }

        public void OnProductExtraFetched(long productId)
        {
            var uniqueId = productId.ToString();
            var isNewItem = !Contains(uniqueId);

            if (isNewItem)
                m_VisualStateList.AddExtraItem(productId.ToString());

            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore)
            {
                var package = m_PackageDatabase.GetPackage(uniqueId);
                if (package == null)
                    return;
                if (isNewItem)
                    TriggerOnListUpdate(added: new[] { package });
                else
                    TriggerOnListUpdate(updated: new[] { package });
                SetNewSelection(new[] { new PackageAndVersionIdPair(package.uniqueId) });
            }
        }

        public void OnProductListFetched(AssetStorePurchases purchases)
        {
            var isSet = purchases.queryArgs?.isFilterSet == true;
            if (isSet && !filters.Equals(purchases.queryArgs))
                return;

            if (purchases.startIndex > 0 && numTotalItems != purchases.total)
            {
                // if a new page has arrived but the total has changed or the searchText has changed, do a re-fetch
                var queryArgs = BuildQueryFromFilter((int)numCurrentItems, purchases.startIndex + purchases.list.Count);
                m_AssetStoreClient.ListPurchases(queryArgs);
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

            // only try to rebuild the list immediately if we are already on the `AssetStore` tab.
            // if not we'll just wait for tab switch which will trigger the rebuild as well
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore)
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

                var addedPackages = added?.Select(id => m_PackageDatabase.GetPackage(id));
                var removedPackages = removed?.Select(id => m_PackageDatabase.GetPackage(id));
                TriggerOnListUpdate(added: addedPackages, removed: removedPackages);
            }

            RefreshVisualStates();
        }

        public override bool Contains(string packageUniqueId)
        {
            return m_VisualStateList?.Contains(packageUniqueId) ?? false;
        }

        public override void AddSubPage(SubPage subPage)
        {
            // do nothing
        }
    }
}
