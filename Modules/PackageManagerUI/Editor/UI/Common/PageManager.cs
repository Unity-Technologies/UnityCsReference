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
        internal const string k_UnityPackageGroupName = "Unity";
        internal const string k_OtherPackageGroupName = "Other";
        internal const string k_CustomPackageGroupName = "Custom";

        static IPageManager s_Instance = null;
        public static IPageManager instance { get { return s_Instance ?? PageManagerInternal.instance; } }

        public const int k_DefaultPageSize = 25;

        internal class PageManagerInternal : ScriptableSingleton<PageManagerInternal>, IPageManager, ISerializationCallbackReceiver
        {
            public event Action<IPackageVersion> onSelectionChanged = delegate {};

            public event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>> onPageUpdate = delegate {};
            public event Action<IPage> onPageRebuild = delegate {};

            public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};

            internal Dictionary<PackageFilterTab, Page> m_Pages = new Dictionary<PackageFilterTab, Page>();

            [SerializeField]
            private Page[] m_SerializedPages = new Page[0];

            [NonSerialized]
            private bool m_EventsRegistered;

            [SerializeField]
            private bool m_Initialized;
            public bool isInitialized => m_Initialized;

            [MenuItem("internal:Packages/Reset Package Database")]
            public static void ResetPackageDatabase()
            {
                var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
                foreach (var window in windows)
                    window.Close();

                instance.Reload();
            }

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

            public bool HasFetchedPageForFilterTab(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                if (!m_Pages.ContainsKey(filterTab))
                    return false;

                return m_Pages[filterTab].isAlreadyFetched;
            }

            private void RegisterPageEvents(Page page)
            {
                page.onSelectionChanged += (selection) => onSelectionChanged?.Invoke(selection);
                page.onVisualStateChange += (visualStates) => onVisualStateChange?.Invoke(visualStates);
                page.onPageUpdate += (addedOrUpdated, removed) => onPageUpdate?.Invoke(page, addedOrUpdated, removed);
            }

            private void UnegisterPageEvents(Page page)
            {
                page.onSelectionChanged -= (selection) => onSelectionChanged?.Invoke(selection);
                page.onVisualStateChange -= (visualStates) => onVisualStateChange?.Invoke(visualStates);
                page.onPageUpdate -= (addedOrUpdated, removed) => onPageUpdate?.Invoke(page, addedOrUpdated, removed);
            }

            public IPage GetCurrentPage()
            {
                return GetPageFromFilterTab();
            }

            public virtual bool HasPage(PackageFilterTab tab)
            {
                return m_Pages.ContainsKey(tab);
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

            public bool IsGroupExpanded(string groupName)
            {
                return GetPageFromFilterTab().IsGroupExpanded(groupName);
            }

            public void SetGroupExpanded(string groupName, bool value)
            {
                GetPageFromFilterTab().SetGroupExpanded(groupName, value);
            }

            private void OnInstalled(IPackage package, IPackageVersion installedVersion = null)
            {
                if (package != null)
                    SetSelected(package, installedVersion);
            }

            private void OnUninstalled(IPackage package)
            {
                if (GetVisualState(package) != null)
                    SetSelected(package);
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
                var page = GetPageFromFilterTab(filterTab);
                if (PackageDatabase.instance.GetRefreshTimestamp(page.tab) == 0)
                    Refresh(filterTab);

                page.RebuildList();
                onPageRebuild?.Invoke(page);

                if (PackageFiltering.instance.previousFilterTab != null)
                {
                    var previousPage = GetPageFromFilterTab((PackageFilterTab)PackageFiltering.instance.previousFilterTab);
                    var selectedGoup = previousPage.GetSelectedVisualState()?.groupName;
                    if (!string.IsNullOrEmpty(selectedGoup))
                        previousPage.SetGroupExpanded(selectedGoup, true);
                }
            }

            private void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
            {
                GetPageFromFilterTab().OnPackagesChanged(added, removed, preUpdate, postUpdate);
            }

            private void OnProductListFetched(ProductList productList, bool fetchDetailsCalled)
            {
                GetPageFromFilterTab(PackageFilterTab.AssetStore).OnProductListFetched(productList, fetchDetailsCalled);
            }

            private void OnProductFetched(long productId)
            {
                GetPageFromFilterTab(PackageFilterTab.AssetStore).OnProductFetched(productId);
            }

            public VisualState GetVisualState(IPackage package)
            {
                return GetPageFromFilterTab().GetVisualState(package?.uniqueId);
            }

            public void Refresh(PackageFilterTab tab)
            {
                Refresh(PackageDatabase.GetRefreshOptionsFromFilterTab(tab));
            }

            public void Refresh(RefreshOptions options)
            {
                // make sure the events are registered before actually calling the actual refresh functions
                // such that we don't lose any callbacks events
                RegisterEvents();
                if ((options & RefreshOptions.CurrentFilter) != 0)
                    options |= PackageDatabase.GetRefreshOptionsFromFilterTab(PackageFiltering.instance.currentFilterTab);

                if ((options & RefreshOptions.UpmSearchOffline) != 0)
                    UpmClient.instance.SearchAll(true);
                if ((options & RefreshOptions.UpmSearch) != 0)
                    UpmClient.instance.SearchAll();
                if ((options & RefreshOptions.UpmListOffline) != 0)
                    UpmClient.instance.List(true);
                if ((options & RefreshOptions.UpmList) != 0)
                    UpmClient.instance.List();
                if ((options & RefreshOptions.Purchased) != 0)
                    AssetStore.AssetStoreClient.instance.List(0, k_DefaultPageSize, string.Empty);
                if ((options & RefreshOptions.PurchasedOffline) != 0)
                    AssetStore.AssetStoreClient.instance.RefreshLocal();
            }

            public void Fetch(string uniqueId)
            {
                long productId;
                if (ApplicationUtil.instance.isUserLoggedIn && long.TryParse(uniqueId, out productId))
                {
                    AssetStore.AssetStoreClient.instance.Fetch(productId);
                }
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
                m_Initialized = true;
                RegisterEvents();
            }

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;

                m_EventsRegistered = true;

                PackageDatabase.instance.RegisterEvents();

                AssetStore.AssetStoreClient.instance.onProductListFetched += OnProductListFetched;
                AssetStore.AssetStoreClient.instance.onProductFetched += OnProductFetched;

                UpmRegistryClient.instance.onRegistriesModified += OnRegistriesModified;

                PackageDatabase.instance.onInstallSuccess += OnInstalled;
                PackageDatabase.instance.onUninstallSuccess += OnUninstalled;
                PackageDatabase.instance.onPackagesChanged += OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged += OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged += OnSearchTextChanged;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                AssetStore.AssetStoreClient.instance.onProductListFetched -= OnProductListFetched;
                AssetStore.AssetStoreClient.instance.onProductFetched -= OnProductFetched;

                UpmRegistryClient.instance.onRegistriesModified -= OnRegistriesModified;

                PackageDatabase.instance.onInstallSuccess -= OnInstalled;
                PackageDatabase.instance.onUninstallSuccess -= OnUninstalled;
                PackageDatabase.instance.onPackagesChanged -= OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged -= OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged -= OnSearchTextChanged;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;

                PackageDatabase.instance.UnregisterEvents();
            }

            private void OnRegistriesModified()
            {
                Refresh(RefreshOptions.UpmSearch);
            }

            internal void Reload()
            {
                UnregisterEvents();

                foreach (var page in m_Pages.Values)
                {
                    page.RebuildList();
                    onPageRebuild?.Invoke(page);
                    UnegisterPageEvents(page);
                }
                m_Pages.Clear();

                PackageDatabase.instance.Reload();

                RegisterEvents();
            }
        }
    }
}
