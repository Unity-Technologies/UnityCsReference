// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PageManager
    {
        static IPageManager s_Instance = null;
        public static IPageManager instance { get { return s_Instance ?? PageManagerInternal.instance; } }

        public const int k_DefaultPageSize = 25;

        [MenuItem("internal:Packages/Reset Package Database")]
        public static void ResetPackageDatabase()
        {
            instance.Clear();
            instance.Setup();
            instance.Refresh(RefreshOptions.All);
        }

        internal class PageManagerInternal : ScriptableSingleton<PageManagerInternal>, IPageManager, ISerializationCallbackReceiver
        {
            public event Action<IPackageVersion> onSelectionChanged = delegate {};

            public event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>> onPageUpdate = delegate {};
            public event Action<IPage> onPageRebuild = delegate {};

            public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};

            internal Dictionary<PackageFilterTab, Page> m_Pages = new Dictionary<PackageFilterTab, Page>();

            [SerializeField]
            private Page[] m_SerializedPages = new Page[0];

            public void OnBeforeSerialize()
            {
                m_SerializedPages = m_Pages.Values.ToArray();
            }

            public void OnAfterDeserialize()
            {
                foreach (var page in m_SerializedPages)
                {
                    m_Pages[page.tab] = page;
                    RegisterPageEvents(page);
                }
            }

            private Page GetPageFromFilterTab(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;

                Page page;
                if (m_Pages.TryGetValue(filterTab, out page))
                {
                    return page;
                }

                page = new Page(filterTab);
                m_Pages[filterTab] = page;
                RegisterPageEvents(page);
                return page;
            }

            public bool HasPageForFilterTab(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return m_Pages.ContainsKey(filterTab);
            }

            private void RegisterPageEvents(Page page)
            {
                page.onSelectionChanged += (selection) => onSelectionChanged?.Invoke(selection);
                page.onVisualStateChange += (visualStates) => onVisualStateChange?.Invoke(visualStates);
                page.onPageUpdate += (addedOrUpdated, removed) => onPageUpdate?.Invoke(page, addedOrUpdated, removed);
            }

            public IPage GetCurrentPage()
            {
                return GetPageFromFilterTab();
            }

            public IPackageVersion GetSelectedVersion()
            {
                return GetPageFromFilterTab().GetSelectedVersion();
            }

            public void ClearSelection()
            {
                SetSelected(null);
            }

            public void SetSelected(IPackage package, IPackageVersion version = null)
            {
                GetPageFromFilterTab().SetSelected(package?.uniqueId, version?.uniqueId ?? package?.primaryVersion?.uniqueId);
            }

            public void SetSeeAllVersions(IPackage package, bool value)
            {
                GetPageFromFilterTab().SetSeeAllVersions(package?.uniqueId, value);
            }

            public void SetExpanded(IPackage package, bool value)
            {
                // prevent an item from being expandable when there are no extra versions
                if (value && !(package?.versions.Skip(1).Any() ?? false))
                    return;
                GetPageFromFilterTab().SetExpanded(package?.uniqueId, value);
            }

            private void OnInstalledOrUninstalled(IPackage package, IPackageVersion installedVersion = null)
            {
                if (package != null)
                    SetSelected(package, installedVersion);
            }

            private void OnUninstalled(IPackage package)
            {
                OnInstalledOrUninstalled(package);
            }

            private void OnSearchTextChanged(string searchText)
            {
                // clear current search result & start new fetch
                if (!string.IsNullOrEmpty(searchText))
                {
                    if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                        AssetStore.AssetStoreClient.instance.List(0, k_DefaultPageSize, searchText, false);
                }
                GetPageFromFilterTab().FilterBySearchText(searchText);
            }

            private void OnFilterChanged(PackageFilterTab filterTab)
            {
                if (filterTab == PackageFilterTab.AssetStore && !HasPageForFilterTab(filterTab))
                    AssetStore.AssetStoreClient.instance.List(0, k_DefaultPageSize);

                var page = GetPageFromFilterTab(filterTab);
                page.RebuildList();
                onPageRebuild?.Invoke(page);
            }

            private void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
            {
                GetPageFromFilterTab().OnPackagesChanged(added, removed, preUpdate, postUpdate);
            }

            private void OnProductListFetched(ProductList productList, bool fetchDetailsCalled)
            {
                GetPageFromFilterTab(PackageFilterTab.AssetStore).OnProductListFetched(productList, fetchDetailsCalled);
            }

            public VisualState GetVisualState(IPackage package)
            {
                return GetPageFromFilterTab().GetVisualState(package?.uniqueId);
            }

            public void Refresh(RefreshOptions options)
            {
                if ((options & RefreshOptions.CurrentFilter) != 0)
                {
                    switch (PackageFiltering.instance.currentFilterTab)
                    {
                        case PackageFilterTab.All:
                            options |= RefreshOptions.UpmList;
                            options |= RefreshOptions.UpmSearch;
                            break;
                        case PackageFilterTab.Local:
                            options |= RefreshOptions.UpmList;
                            break;
                        case PackageFilterTab.Modules:
                            options |= RefreshOptions.UpmSearchOffline;
                            options |= RefreshOptions.UpmListOffline;
                            break;
                        case PackageFilterTab.AssetStore:
                            options |= RefreshOptions.Purchased;
                            break;
                        case PackageFilterTab.InDevelopment:
                            options |= RefreshOptions.UpmList;
                            break;
                    }
                }

                if ((options & RefreshOptions.UpmSearchOffline) != 0)
                    UpmClient.instance.SearchAll(true);
                if ((options & RefreshOptions.UpmSearch) != 0)
                    UpmClient.instance.SearchAll();
                if ((options & RefreshOptions.UpmListOffline) != 0)
                    UpmClient.instance.List(true);
                if ((options & RefreshOptions.UpmList) != 0)
                    UpmClient.instance.List();
                if (ApplicationUtil.instance.isUserLoggedIn && (options & RefreshOptions.Purchased) != 0)
                    AssetStore.AssetStoreClient.instance.List(0, k_DefaultPageSize, string.Empty);
            }

            public void LoadMore()
            {
                GetPageFromFilterTab().LoadMore();
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (loggedIn)
                    Refresh(RefreshOptions.Purchased);
            }

            public void Setup()
            {
                PackageDatabase.instance.Setup();

                AssetStore.AssetStoreClient.instance.onProductListFetched += OnProductListFetched;

                PackageDatabase.instance.onInstallSuccess += OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess += OnUninstalled;
                PackageDatabase.instance.onPackagesChanged += OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged += OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged += OnSearchTextChanged;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
            }

            public void Clear()
            {
                AssetStore.AssetStoreClient.instance.onProductListFetched -= OnProductListFetched;

                PackageDatabase.instance.onInstallSuccess -= OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess -= OnUninstalled;
                PackageDatabase.instance.onPackagesChanged -= OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged -= OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged -= OnSearchTextChanged;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;

                PackageDatabase.instance.Clear();

                foreach (var page in m_Pages.Values)
                {
                    page.RebuildList();
                    onPageRebuild?.Invoke(page);
                }
                m_Pages.Clear();
            }
        }
    }
}
