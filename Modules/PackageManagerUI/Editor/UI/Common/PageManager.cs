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

        internal class PageManagerInternal : ScriptableSingleton<PageManagerInternal>, IPageManager, ISerializationCallbackReceiver
        {
            private static readonly RefreshOptions[] k_RefreshOptionsByTab =
            {
                RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.All
                RefreshOptions.UpmList,                                             // PackageFilterTab.InProject
                RefreshOptions.UpmListOffline | RefreshOptions.UpmSearchOffline,    // PackageFilterTab.BuiltIn
                RefreshOptions.Purchased,                                           // PackageFilterTab.AssetStore
                RefreshOptions.UpmList                                              // PackageFilterTab.InDevelopment
            };

            public event Action<IPackageVersion> onSelectionChanged = delegate {};

            public event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>> onPageUpdate = delegate {};
            public event Action<IPage> onPageRebuild = delegate {};

            public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};

            public event Action onRefreshOperationStart = delegate {};
            public event Action onRefreshOperationFinish = delegate {};
            public event Action<Error> onRefreshOperationError = delegate {};

            private Dictionary<RefreshOptions, long> m_RefreshTimestamps = new Dictionary<RefreshOptions, long>();
            private Dictionary<RefreshOptions, Error> m_RefreshErrors = new Dictionary<RefreshOptions, Error>();

            [NonSerialized]
            private List<IOperation> m_RefreshOperationsInProgress = new List<IOperation>();

            // array created to help serialize dictionaries
            [SerializeField]
            private RefreshOptions[] m_SerializedRefreshTimestampsKeys = new RefreshOptions[0];

            [SerializeField]
            private long[] m_SerializedRefreshTimestampsValues = new long[0];

            [SerializeField]
            private RefreshOptions[] m_SerializedRefreshErrorsKeys = new RefreshOptions[0];

            [SerializeField]
            private Error[] m_SerializedRefreshErrorsValues = new Error[0];

            internal Dictionary<PackageFilterTab, Page> m_Pages = new Dictionary<PackageFilterTab, Page>();

            [SerializeField]
            private Page[] m_SerializedPages = new Page[0];

            [MenuItem("internal:Packages/Reset Package Database")]
            public static void ResetPackageDatabase()
            {
                instance.Reset();
                instance.Refresh();
            }

            public void OnBeforeSerialize()
            {
                m_SerializedPages = m_Pages.Values.ToArray();

                m_SerializedRefreshTimestampsKeys = m_RefreshTimestamps.Keys.ToArray();
                m_SerializedRefreshTimestampsValues = m_RefreshTimestamps.Values.ToArray();
                m_SerializedRefreshErrorsKeys = m_RefreshErrors.Keys.ToArray();
                m_SerializedRefreshErrorsValues = m_RefreshErrors.Values.ToArray();
            }

            public void OnAfterDeserialize()
            {
                foreach (var page in m_SerializedPages)
                {
                    m_Pages[page.tab] = page;
                    RegisterPageEvents(page);
                }

                for (var i = 0; i < m_SerializedRefreshTimestampsKeys.Length; i++)
                    m_RefreshTimestamps[m_SerializedRefreshTimestampsKeys[i]] = m_SerializedRefreshTimestampsValues[i];

                for (var i = 0; i < m_SerializedRefreshErrorsKeys.Length; i++)
                    m_RefreshErrors[m_SerializedRefreshErrorsKeys[i]] = m_SerializedRefreshErrorsValues[i];
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
                var page = GetPageFromFilterTab(filterTab);
                if (GetRefreshTimestamp(page.tab) == 0)
                    Refresh(filterTab);

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

            private void OnProductFetched(long productId)
            {
                GetPageFromFilterTab(PackageFilterTab.AssetStore).OnProductFetched(productId);
            }

            public VisualState GetVisualState(IPackage package)
            {
                return GetPageFromFilterTab().GetVisualState(package?.uniqueId);
            }

            private static RefreshOptions GetRefreshOptionsByTab(PackageFilterTab tab)
            {
                var index = (int)tab;
                return index >= k_RefreshOptionsByTab.Length ? RefreshOptions.None : k_RefreshOptionsByTab[index];
            }

            public void Refresh(PackageFilterTab? tab = null)
            {
                Refresh(GetRefreshOptionsByTab(tab ?? PackageFiltering.instance.currentFilterTab));
            }

            public void Refresh(RefreshOptions options)
            {
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

            private void OnInternetReachabilityChange(bool value)
            {
                if (value && !EditorApplication.isPlaying)
                    Refresh();
            }

            public void Setup()
            {
                PackageDatabase.instance.Setup();

                UpmClient.instance.onListOperation += OnRefreshOperation;
                UpmClient.instance.onSearchAllOperation += OnRefreshOperation;

                AssetStore.AssetStoreClient.instance.onListOperation += OnRefreshOperation;
                AssetStore.AssetStoreClient.instance.onProductListFetched += OnProductListFetched;
                AssetStore.AssetStoreClient.instance.onProductFetched += OnProductFetched;

                PackageDatabase.instance.onInstallSuccess += OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess += OnUninstalled;
                PackageDatabase.instance.onPackagesChanged += OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged += OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged += OnSearchTextChanged;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
                ApplicationUtil.instance.onInternetReachabilityChange += OnInternetReachabilityChange;
            }

            public void Clear()
            {
                UpmClient.instance.onListOperation -= OnRefreshOperation;
                UpmClient.instance.onSearchAllOperation -= OnRefreshOperation;

                AssetStore.AssetStoreClient.instance.onListOperation -= OnRefreshOperation;
                AssetStore.AssetStoreClient.instance.onProductListFetched -= OnProductListFetched;
                AssetStore.AssetStoreClient.instance.onProductFetched -= OnProductFetched;

                PackageDatabase.instance.onInstallSuccess -= OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess -= OnUninstalled;
                PackageDatabase.instance.onPackagesChanged -= OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged -= OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged -= OnSearchTextChanged;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
                ApplicationUtil.instance.onInternetReachabilityChange -= OnInternetReachabilityChange;

                PackageDatabase.instance.Clear();
            }

            internal void Reset()
            {
                Clear();

                foreach (var page in m_Pages.Values)
                {
                    page.RebuildList();
                    onPageRebuild?.Invoke(page);
                    UnegisterPageEvents(page);
                }
                m_Pages.Clear();
                m_RefreshTimestamps.Clear();
                m_RefreshErrors.Clear();
                m_RefreshOperationsInProgress.Clear();

                PackageDatabase.instance.Reset();

                Setup();
            }

            private void OnRefreshOperation(IOperation operation)
            {
                m_RefreshOperationsInProgress.Add(operation);
                operation.onOperationSuccess += () => OnRefreshOperationSuccess(operation);
                operation.onOperationFinalized += () => OnRefreshOperationFinalized(operation);
                operation.onOperationError += (error) => OnRefreshOperationError(operation, error);
                if (m_RefreshOperationsInProgress.Count > 1)
                    return;
                onRefreshOperationStart?.Invoke();
            }

            private void OnRefreshOperationSuccess(IOperation operation)
            {
                m_RefreshTimestamps[operation.refreshOptions] = operation.timestamp;
                if (m_RefreshErrors.ContainsKey(operation.refreshOptions))
                    m_RefreshErrors.Remove(operation.refreshOptions);
            }

            private void OnRefreshOperationError(IOperation operation, Error error)
            {
                m_RefreshErrors[operation.refreshOptions] = error;
                onRefreshOperationError?.Invoke(error);
            }

            private void OnRefreshOperationFinalized(IOperation operation)
            {
                m_RefreshOperationsInProgress.Remove(operation);
                if (m_RefreshOperationsInProgress.Any())
                    return;
                onRefreshOperationFinish?.Invoke();
            }

            public bool IsRefreshInProgress(RefreshOptions option)
            {
                return m_RefreshOperationsInProgress.Any(operation => (operation.refreshOptions & option) != 0);
            }

            public long GetRefreshTimestamp(RefreshOptions option)
            {
                var result = 0L;
                foreach (var item in m_RefreshTimestamps)
                {
                    if ((option & item.Key) == 0)
                        continue;
                    if (result == 0)
                        result = item.Value;
                    else if (result > item.Value)
                        result = item.Value;
                }
                return result;
            }

            public Error GetRefreshError(RefreshOptions option)
            {
                // only return the first one when there are multiple errors
                foreach (var item in m_RefreshErrors)
                    if ((option & item.Key) != 0)
                        return item.Value;
                return null;
            }

            public long GetRefreshTimestamp(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return GetRefreshTimestamp(GetRefreshOptionsByTab(filterTab));
            }

            public Error GetRefreshError(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return GetRefreshError(GetRefreshOptionsByTab(filterTab));
            }

            public bool IsRefreshInProgress(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return IsRefreshInProgress(GetRefreshOptionsByTab(filterTab));
            }
        }
    }
}
