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
    internal class Page : IPage, ISerializationCallbackReceiver
    {
        [Serializable]
        public class FilteredList
        {
            public bool enabled;
            public ProductList baseList = new ProductList();
            public ProductList searchList = new ProductList();
        }

        public event Action<IPackageVersion> onSelectionChanged = delegate {};
        public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};
        public event Action<IEnumerable<IPackage>, IEnumerable<IPackage>> onPageUpdate = delegate {};

        public bool isLoading => false;

        [SerializeField]
        internal bool m_IsAlreadyFetched;

        public bool isAlreadyFetched => m_IsAlreadyFetched;

        [SerializeField]
        private bool m_MorePackagesToFetch;

        [SerializeField]
        private bool m_MoreSearchPackagesToFetch;

        public bool morePackagesToFetch => string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText) ? m_MorePackagesToFetch : m_MoreSearchPackagesToFetch;

        // note that what's saved in `m_SelectedStates` is a clone of the actual state, so should not be used directly
        [SerializeField]
        private List<VisualState> m_SelectedStates = new List<VisualState>();

        [SerializeField]
        private List<VisualState> m_OrderedPackageVisualStates = new List<VisualState>();

        private Dictionary<string, int> m_PackageVisualStateLookup = new Dictionary<string, int>();

        public List<VisualState> packageVisualStates => m_OrderedPackageVisualStates;

        // if the filter list is not enabled, we show everything that fits the tab in the package database
        // if the filter list enabled, we only show items that in the list & an in the database
        [SerializeField]
        private FilteredList m_FilteredList = new FilteredList();

        [SerializeField]
        private PackageFilterTab m_Tab;
        public PackageFilterTab tab => m_Tab;

        public Page(PackageFilterTab tab)
        {
            m_Tab = tab;
            m_IsAlreadyFetched = false;
        }

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            SetupLookupTable();
        }

        public VisualState GetVisualState(string packageUniqueId)
        {
            int index;
            if (!string.IsNullOrEmpty(packageUniqueId) && m_PackageVisualStateLookup.TryGetValue(packageUniqueId, out index))
                return m_OrderedPackageVisualStates[index];
            return null;
        }

        public IPackageVersion GetSelectedVersion()
        {
            return m_SelectedStates.Select(s =>
            {
                IPackage package;
                IPackageVersion version;
                PackageDatabase.instance.GetPackageAndVersion(s.packageUniqueId, s.selectedVersionId, out package, out version);
                return version ?? package?.primaryVersion;
            }).FirstOrDefault();
        }

        public void FilterBySearchText(string updatedSearchText = null)
        {
            if (updatedSearchText != null && m_FilteredList.enabled && m_FilteredList.searchList.searchText != updatedSearchText)
            {
                if (m_FilteredList.searchList.list.Count > 0)
                {
                    var extraSearchItems = new HashSet<long>(m_FilteredList.searchList.list);
                    foreach (var id in m_FilteredList.baseList.list)
                    {
                        if (extraSearchItems.Contains(id))
                            extraSearchItems.Remove(id);
                    }

                    // reset the search list if search text have changed, and remove the items from the list
                    m_FilteredList.searchList = new ProductList();
                    m_MoreSearchPackagesToFetch = false;

                    var removeList = extraSearchItems.Select(id => PackageDatabase.instance.GetPackage(id.ToString())).Where(p => p != null);
                    RebuildList();
                    onPageUpdate?.Invoke(new IPackage[0] {}, removeList);
                    return;
                }

                // Refresh the `load more` status.
                m_FilteredList.searchList = new ProductList();
                m_MoreSearchPackagesToFetch = false;
                onPageUpdate?.Invoke(new IPackage[0] {}, new IPackage[0] {});
            }

            var changedVisualStates = new List<VisualState>();
            foreach (var state in packageVisualStates)
            {
                var package = PackageDatabase.instance.GetPackage(state.packageUniqueId);
                var visible = PackageFiltering.instance.FilterByCurrentSearchText(package);
                if (state.visible != visible)
                {
                    state.visible = visible;
                    changedVisualStates.Add(state);
                }
            }

            if (changedVisualStates.Any())
                onVisualStateChange?.Invoke(changedVisualStates);

            RefreshSelected();
        }

        private void RefreshSelected()
        {
            // refresh selection when the selected is null or not visible
            var selected = GetVisualState(m_SelectedStates.FirstOrDefault()?.packageUniqueId);
            IPackage package;
            IPackageVersion version;
            PackageDatabase.instance.GetPackageAndVersion(selected?.packageUniqueId, selected?.selectedVersionId, out package, out version);
            if (!(selected?.visible ?? false) || package == null)
            {
                var firstVisible = packageVisualStates.FirstOrDefault(v => v.visible);
                package = firstVisible == null ? null : PackageDatabase.instance.GetPackage(firstVisible.packageUniqueId);
                version = package?.primaryVersion;
                SetSelected(package?.uniqueId, version?.uniqueId);
            }
            else if (version == null)
            {
                version = package?.primaryVersion;
                SetSelected(package?.uniqueId, version?.uniqueId);
            }
        }

        public void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
        {
            var addOrUpdateList = new List<IPackage>();
            var removeList = removed.ToList();
            foreach (var package in added.Concat(postUpdate))
            {
                if (PackageFiltering.instance.FilterByCurrentTab(package))
                    addOrUpdateList.Add(package);
                else
                    removeList.Add(package);
            }

            m_IsAlreadyFetched = true;
            RebuildList(addOrUpdateList, removeList);
        }

        public void RebuildList(IEnumerable<IPackage> addOrUpdateList = null, IEnumerable<IPackage> removeList = null)
        {
            if (!m_FilteredList.enabled)
            {
                var orderedPackages = PackageDatabase.instance.allPackages
                    .Where(p => PackageFiltering.instance.FilterByCurrentTab(p))
                    .OrderBy(p => p.primaryVersion?.displayName ?? p.name);
                // the normal way
                m_OrderedPackageVisualStates = orderedPackages.Select(p => GetVisualState(p.uniqueId) ?? new VisualState(p.uniqueId)).ToList();
                SetupLookupTable();

                m_MorePackagesToFetch = false;
                m_MoreSearchPackagesToFetch = false;
            }
            else
            {
                var isSearchMode = !string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText);
                var newOrderedStates = new List<VisualState>();
                var newLookupTable = new Dictionary<string, int>();

                foreach (var id in m_FilteredList.baseList.list)
                {
                    var package = PackageDatabase.instance.GetPackage(id.ToString());
                    if (package != null)
                    {
                        newLookupTable[package.uniqueId] = newOrderedStates.Count;
                        newOrderedStates.Add(GetVisualState(package.uniqueId) ?? new VisualState(package.uniqueId));
                    }
                }

                if (isSearchMode && m_FilteredList.searchList.searchText == PackageFiltering.instance.currentSearchText)
                {
                    foreach (var id in m_FilteredList.searchList.list)
                    {
                        if (newLookupTable.ContainsKey(id.ToString()))
                            continue;
                        var package = PackageDatabase.instance.GetPackage(id.ToString());
                        if (package != null)
                        {
                            newLookupTable[package.uniqueId] = newOrderedStates.Count;
                            newOrderedStates.Add(GetVisualState(package.uniqueId) ?? new VisualState(package.uniqueId));
                        }
                    }
                }

                m_OrderedPackageVisualStates = newOrderedStates;
                m_PackageVisualStateLookup = newLookupTable;
            }

            if (addOrUpdateList != null || removeList != null)
                onPageUpdate?.Invoke(addOrUpdateList ?? Enumerable.Empty<IPackage>(), removeList ?? Enumerable.Empty<IPackage>());

            FilterBySearchText();
        }

        public void LoadMore()
        {
            var isSearchMode = !string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText);
            var productList = isSearchMode ? m_FilteredList.searchList : m_FilteredList.baseList;
            if (productList.list.Count < productList.total)
                AssetStore.AssetStoreClient.instance.List(productList.list.Count, PageManager.k_DefaultPageSize, PackageFiltering.instance.currentSearchText, false);
        }

        public void Load(IPackage package, IPackageVersion version = null)
        {
            var selectedState = GetVisualState(package.uniqueId);
            if (selectedState == null)
            {
                long productId;
                if (package == null || !long.TryParse(package.uniqueId, out productId))
                    return;

                var targetList = m_FilteredList.baseList;
                if (!m_FilteredList.enabled || targetList.list.Contains(productId))
                    return;

                targetList.list.Add(productId);
                m_MorePackagesToFetch = targetList.total > targetList.list.Count;
            }

            m_IsAlreadyFetched = true;
            RebuildList(new[] { package }, Enumerable.Empty<IPackage>());
            SetSelected(package.uniqueId, version?.uniqueId ?? package.primaryVersion?.uniqueId);
        }

        public void OnProductFetched(long productId)
        {
            var targetList = m_FilteredList.baseList;
            if (!m_FilteredList.enabled || targetList.list.Contains(productId))
                return;

            targetList.list.Add(productId);
            m_MorePackagesToFetch = targetList.total > targetList.list.Count;

            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                var package = PackageDatabase.instance.GetPackage(productId.ToString());
                RebuildList(new[] { package }, Enumerable.Empty<IPackage>());
                SetSelected(package?.uniqueId, package?.primaryVersion?.uniqueId);
            }
        }

        public void OnProductListFetched(ProductList productList, bool fetchDetailsCalled)
        {
            var isSearchResult = !string.IsNullOrEmpty(productList.searchText);
            if (isSearchResult && productList.searchText != PackageFiltering.instance.currentSearchText)
                return;

            var targetList = isSearchResult ? m_FilteredList.searchList : m_FilteredList.baseList;
            if (productList.startIndex > 0 && (targetList.total != productList.total || (targetList.searchText ?? "") != (productList.searchText ?? "")))
            {
                // if a new page has arrived but the total has changed or the searchText has changed, do a re-fetch
                AssetStore.AssetStoreClient.instance.List(0, productList.startIndex + productList.list.Count, PackageFiltering.instance.currentSearchText, true);
                return;
            }

            var rebuildList = PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore;

            HashSet<long> removed = null;
            List<long> added = null;
            if (productList.startIndex == 0)
            {
                if (rebuildList)
                {
                    removed = new HashSet<long>(targetList.list);
                    added = new List<long>();
                    foreach (var id in productList.list)
                    {
                        if (removed.Contains(id))
                            removed.Remove(id);
                        else
                            added.Add(id);
                    }
                }
                // override the result if the new list starts from index 0 (meaning it's a refresh)
                targetList.list = productList.list;
                targetList.total = productList.total;
                targetList.searchText = productList.searchText;
            }
            else if (productList.startIndex == targetList.list.Count && (targetList.searchText ?? "") == (productList.searchText ?? ""))
            {
                // append the result if it is the next page
                targetList.list.AddRange(productList.list);
                if (rebuildList)
                    added = productList.list;
            }
            else
            {
                // if the content is neither starting from zero or next page, we simply discard it
                return;
            }

            m_MorePackagesToFetch = m_FilteredList.baseList.total > m_FilteredList.baseList.list.Count;
            m_MoreSearchPackagesToFetch = m_FilteredList.searchList.total > m_FilteredList.searchList.list.Count;

            m_FilteredList.enabled = true;
            if (!fetchDetailsCalled && productList.list.Any())
                AssetStore.AssetStoreClient.instance.FetchDetails(productList.list);

            if (rebuildList)
            {
                m_IsAlreadyFetched = true;
                var addedPackages = added?.Select(id => PackageDatabase.instance.GetPackage(id.ToString()));
                var removedPackages = removed?.Select(id => PackageDatabase.instance.GetPackage(id.ToString()));
                RebuildList(addedPackages, removedPackages);
            }
        }

        public void SetSelected(string packageUniqueId, string versionUniqueId)
        {
            var oldSelection = m_SelectedStates.FirstOrDefault();
            if (oldSelection?.packageUniqueId == packageUniqueId && oldSelection?.selectedVersionId == versionUniqueId)
                return;

            foreach (var selection in m_SelectedStates)
            {
                var state = GetVisualState(selection.packageUniqueId);
                if (state != null)
                    state.selectedVersionId = string.Empty;
            }
            m_SelectedStates.Clear();

            if (!string.IsNullOrEmpty(packageUniqueId) && !string.IsNullOrEmpty(versionUniqueId))
            {
                var selectedState = GetVisualState(packageUniqueId);
                if (selectedState != null)
                {
                    selectedState.selectedVersionId = versionUniqueId;
                    m_SelectedStates.Add(selectedState.Clone());
                }
            }
            onSelectionChanged?.Invoke(GetSelectedVersion());
            onVisualStateChange?.Invoke(new[] { GetVisualState(oldSelection?.packageUniqueId), GetVisualState(packageUniqueId) }.Where(s => s != null));
        }

        public void SetExpanded(string packageUniqueId, bool value)
        {
            var state = GetVisualState(packageUniqueId);
            if (state != null && state.expanded != value)
            {
                state.expanded = value;
                if (!value)
                    state.seeAllVersions = false;
                onVisualStateChange?.Invoke(new[] { state });
            }
        }

        public void SetSeeAllVersions(string packageUniqueId, bool value)
        {
            var state = GetVisualState(packageUniqueId);
            if (state != null && state.seeAllVersions != value)
            {
                state.seeAllVersions = value;
                onVisualStateChange?.Invoke(new[] { state });
            }
        }

        private void SetupLookupTable()
        {
            m_PackageVisualStateLookup = new Dictionary<string, int>();

            for (var i = 0; i < m_OrderedPackageVisualStates.Count; i++)
                m_PackageVisualStateLookup[m_OrderedPackageVisualStates[i].packageUniqueId] = i;
        }
    }
}
