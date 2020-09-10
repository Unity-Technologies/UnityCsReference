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

        internal const int k_DefaultPageSize = 25;

        static IPageManager s_Instance = null;
        public static IPageManager instance { get { return s_Instance ?? PageManagerInternal.instance; } }

        internal class PageManagerInternal : ScriptableSingleton<PageManagerInternal>, IPageManager, ISerializationCallbackReceiver
        {
            private static readonly RefreshOptions[] k_RefreshOptionsByTab =
            {
                RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.UnityRegistry
                RefreshOptions.UpmList,                                             // PackageFilterTab.InProject
                RefreshOptions.UpmListOffline | RefreshOptions.UpmSearchOffline,    // PackageFilterTab.BuiltIn
                RefreshOptions.Purchased,                                           // PackageFilterTab.AssetStore
                RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.MyRegistries
            };

            public event Action<IPackageVersion> onSelectionChanged = delegate {};
            public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};
            public event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>, bool> onListUpdate = delegate {};
            public event Action<IPage> onListRebuild = delegate {};

            public event Action onRefreshOperationStart = delegate {};
            public event Action onRefreshOperationFinish = delegate {};
            public event Action<UIError> onRefreshOperationError = delegate {};

            private Dictionary<RefreshOptions, long> m_RefreshTimestamps = new Dictionary<RefreshOptions, long>();
            private Dictionary<RefreshOptions, UIError> m_RefreshErrors = new Dictionary<RefreshOptions, UIError>();

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
            private UIError[] m_SerializedRefreshErrorsValues = new UIError[0];

            private Dictionary<PackageFilterTab, IPage> m_Pages = new Dictionary<PackageFilterTab, IPage>();

            [SerializeField]
            private SimplePage[] m_SerializedSimplePages = new SimplePage[0];

            [SerializeField]
            private PaginatedPage[] m_SerializedPaginatedPage = new PaginatedPage[0];

            [NonSerialized]
            private bool m_EventsRegistered;

            [SerializeField]
            private bool m_Initialized;
            public bool isInitialized => m_Initialized;

            [SerializeField]
            private int[] m_SerializedPackageSelectionInstanceIds = new int[0];

            [NonSerialized]
            private Dictionary<string, PackageSelectionObject> m_PackageSelectionObjects = new Dictionary<string, PackageSelectionObject>();

            public void OnBeforeSerialize()
            {
                m_SerializedSimplePages = m_Pages.Values.Select(p => p as SimplePage).Where(p => p != null).ToArray();
                m_SerializedPaginatedPage = m_Pages.Values.Select(p => p as PaginatedPage).Where(p => p != null).ToArray();

                m_SerializedRefreshTimestampsKeys = m_RefreshTimestamps.Keys.ToArray();
                m_SerializedRefreshTimestampsValues = m_RefreshTimestamps.Values.ToArray();
                m_SerializedRefreshErrorsKeys = m_RefreshErrors.Keys.ToArray();
                m_SerializedRefreshErrorsValues = m_RefreshErrors.Values.ToArray();

                m_SerializedPackageSelectionInstanceIds = m_PackageSelectionObjects.Select(kp => kp.Value.GetInstanceID()).ToArray();
            }

            public void OnAfterDeserialize()
            {
                foreach (var page in m_SerializedSimplePages.Cast<IPage>().Concat(m_SerializedPaginatedPage))
                {
                    m_Pages[page.tab] = page;
                    RegisterPageEvents(page);
                }

                for (var i = 0; i < m_SerializedRefreshTimestampsKeys.Length; i++)
                    m_RefreshTimestamps[m_SerializedRefreshTimestampsKeys[i]] = m_SerializedRefreshTimestampsValues[i];

                for (var i = 0; i < m_SerializedRefreshErrorsKeys.Length; i++)
                    m_RefreshErrors[m_SerializedRefreshErrorsKeys[i]] = m_SerializedRefreshErrorsValues[i];
            }

            public virtual PackageSelectionObject GetPackageSelectionObject(IPackage package, IPackageVersion version = null, bool createIfNotFound = false)
            {
                if (package == null)
                    return null;

                version = version ?? package.versions.primary;
                var packageSelectionObject = m_PackageSelectionObjects.Get(version.uniqueId);
                if (packageSelectionObject == null && createIfNotFound)
                {
                    packageSelectionObject = ScriptableObject.CreateInstance<PackageSelectionObject>();
                    packageSelectionObject.hideFlags = HideFlags.DontSave;
                    packageSelectionObject.name = version.uniqueId;
                    packageSelectionObject.displayName = version.displayName;
                    packageSelectionObject.packageUniqueId = package.uniqueId;
                    packageSelectionObject.versionUniqueId = version.uniqueId;
                    m_PackageSelectionObjects[version.uniqueId] = packageSelectionObject;
                }
                return packageSelectionObject;
            }

            private IPage GetPageFromTab(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;

                IPage page;
                return m_Pages.TryGetValue(filterTab, out page) ? page : CreatePageFromTab(tab);
            }

            private T GetPageFromTab<T>(PackageFilterTab? tab = null) where T : class, IPage
            {
                return GetPageFromTab(tab) as T;
            }

            private IPage CreatePageFromTab(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                IPage page;
                if (filterTab == PackageFilterTab.AssetStore)
                {
                    page = new PaginatedPage(filterTab, new PageCapability
                    {
                        requireUserLoggedIn = true,
                        requireNetwork = true,
                        supportFilters = true,
                        orderingValues = new[]
                        {
                            new PageCapability.Ordering("Name", "name"),
                            new PageCapability.Ordering("Purchased date", "purchased_date"),
                            new PageCapability.Ordering("Update date", "update_date"),
                        }
                    });
                }
                else if (filterTab == PackageFilterTab.UnityRegistry || filterTab == PackageFilterTab.MyRegistries)
                {
                    page = new SimplePage(filterTab, new PageCapability
                    {
                        requireUserLoggedIn = false,
                        requireNetwork = false,
                        orderingValues = new[]
                        {
                            new PageCapability.Ordering("Name", "displayName"),
                            new PageCapability.Ordering("Published date", "publishedDate")
                        }
                    });
                }
                else if (filterTab == PackageFilterTab.InProject)
                {
                    page = new SimplePage(filterTab, new PageCapability
                    {
                        requireUserLoggedIn = false,
                        requireNetwork = false,
                        orderingValues = new[]
                        {
                            new PageCapability.Ordering("Name", "displayName"),
                            new PageCapability.Ordering("Published date", "publishedDate"),
                            new PageCapability.Ordering("Update date", "updateDate")
                        }
                    });
                }
                else // filterTab == PackageFilterTab.BuiltIn
                {
                    page = new SimplePage(filterTab, new PageCapability
                    {
                        requireUserLoggedIn = false,
                        requireNetwork = false,
                        orderingValues = new[]
                        {
                            new PageCapability.Ordering("Name", "displayName")
                        }
                    });
                }
                m_Pages[filterTab] = page;
                RegisterPageEvents(page);
                return page;
            }

            private void RegisterPageEvents(IPage page)
            {
                page.onSelectionChanged += OnPageSelectionChanged;
                page.onVisualStateChange += TriggerOnVisualStateChange;
                page.onListUpdate += TriggerOnPageUpdate;
                page.onListRebuild += TriggerOnPageRebuild;
            }

            private void UnregisterPageEvents(IPage page)
            {
                page.onSelectionChanged -= OnPageSelectionChanged;
                page.onVisualStateChange -= TriggerOnVisualStateChange;
                page.onListUpdate -= TriggerOnPageUpdate;
                page.onListRebuild -= TriggerOnPageRebuild;
            }

            private void OnPageSelectionChanged(IPackageVersion version)
            {
                onSelectionChanged?.Invoke(version);

                SelectInInspector(PackageDatabase.instance.GetPackage(version), version, false);
            }

            private void TriggerOnVisualStateChange(IEnumerable<VisualState> visualStates)
            {
                onVisualStateChange?.Invoke(visualStates);
            }

            private void TriggerOnPageUpdate(IPage page, IEnumerable<IPackage> addedOrUpdated, IEnumerable<IPackage> removed, bool reorder)
            {
                onListUpdate?.Invoke(page, addedOrUpdated, removed, reorder);
            }

            private void TriggerOnPageRebuild(IPage page)
            {
                onListRebuild?.Invoke(page);
            }

            public IPage GetCurrentPage()
            {
                return GetPageFromTab();
            }

            public IPage GetPage(PackageFilterTab tab)
            {
                return GetPageFromTab(tab);
            }

            public virtual bool HasPage(PackageFilterTab tab)
            {
                return m_Pages.ContainsKey(tab);
            }

            public IPackageVersion GetSelectedVersion()
            {
                return GetPageFromTab().GetSelectedVersion();
            }

            public void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version)
            {
                GetPageFromTab().GetSelectedPackageAndVersion(out package, out version);
            }

            public void ClearSelection()
            {
                SetSelected(null);
            }

            public void SetSelected(IPackage package, IPackageVersion version = null, bool forceSelectInInspector = false)
            {
                GetPageFromTab().SetSelected(package, version);
                SelectInInspector(package, version, forceSelectInInspector);
            }

            private void SelectInInspector(IPackage package, IPackageVersion version, bool forceSelectInInspector)
            {
                // There are two situations when we want to select a package in the inspector
                // 1) we explicitly/force to select it as a result of an manual action (in this case, forceSelectInInspector should be set to true)
                // 2) currently another package is selected in inspector, hence we are sure that we are not stealing selection from some other window
                var currentPackageSelection = ApplicationUtil.instance.activeSelection as PackageSelectionObject;
                if (forceSelectInInspector || currentPackageSelection != null)
                {
                    var packageSelectionObject = GetPackageSelectionObject(package, version, true);
                    if (ApplicationUtil.instance.activeSelection != packageSelectionObject)
                        ApplicationUtil.instance.activeSelection = packageSelectionObject;
                }
            }

            public void SetSeeAllVersions(IPackage package, bool value)
            {
                GetPageFromTab().SetSeeAllVersions(package, value);
            }

            public void SetExpanded(IPackage package, bool value)
            {
                GetPageFromTab().SetExpanded(package, value);
            }

            public bool IsGroupExpanded(string groupName)
            {
                return GetPageFromTab().IsGroupExpanded(groupName);
            }

            public void SetGroupExpanded(string groupName, bool value)
            {
                GetPageFromTab().SetGroupExpanded(groupName, value);
            }

            public PackageFilterTab FindTab(IPackage package, IPackageVersion version = null)
            {
                var page = GetPageFromTab();
                if (page.Contains(package?.uniqueId))
                    return page.tab;

                if (package?.Is(PackageType.BuiltIn) == true)
                    return PackageFilterTab.BuiltIn;

                if (package?.Is(PackageType.AssetStore) == true)
                    return PackageFilterTab.AssetStore;

                if (version?.isInstalled == true || package?.versions?.installed != null)
                    return PackageFilterTab.InProject;

                if (package?.Is(PackageType.Unity) == true)
                    return PackageFilterTab.UnityRegistry;

                return PackageFilterTab.MyRegistries;
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
                UpdateSearchTextOnPage(GetPageFromTab(), searchText);
            }

            private void OnFilterChanged(PackageFilterTab filterTab)
            {
                var page = GetPageFromTab(filterTab);
                if (GetRefreshTimestamp(page.tab) == 0)
                    Refresh(filterTab);
                page.Rebuild();
                UpdateSearchTextOnPage(page, PackageFiltering.instance.currentSearchText);

                // When the filter tab is changed, on a page level, the selection hasn't changed because selection is kept for each filter
                // However, if you look at package manager as a whole
                OnPageSelectionChanged(page.GetSelectedVersion());

                if (PackageFiltering.instance.previousFilterTab != null)
                {
                    var previousPage = GetPage((PackageFilterTab)PackageFiltering.instance.previousFilterTab);
                    var selectedGoup = previousPage.GetSelectedVisualState()?.groupName;
                    if (!string.IsNullOrEmpty(selectedGoup))
                        previousPage.SetGroupExpanded(selectedGoup, true);
                }
            }

            private static void UpdateSearchTextOnPage(IPage page, string searchText)
            {
                if (page.filters.searchText == searchText)
                    return;
                var filters = page.filters.Clone();
                filters.searchText = searchText;
                page.UpdateFilters(filters);
            }

            private void OnShowDependenciesChanged(bool value)
            {
                if (PackageFiltering.instance.currentFilterTab != PackageFilterTab.InProject)
                    return;
                var page = GetPageFromTab();
                page.Rebuild();
            }

            private void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
            {
                GetPageFromTab().OnPackagesChanged(added, removed, preUpdate, postUpdate);

                foreach (var package in removed)
                {
                    var packageSelectionObject = GetPackageSelectionObject(package);
                    if (packageSelectionObject != null)
                    {
                        m_PackageSelectionObjects.Remove(packageSelectionObject.versionUniqueId);
                        UnityEngine.Object.DestroyImmediate(packageSelectionObject);
                    }
                }
            }

            private void OnProductListFetched(AssetStorePurchases productList, bool fetchDetailsCalled)
            {
                GetPageFromTab<PaginatedPage>(PackageFilterTab.AssetStore).OnProductListFetched(productList, fetchDetailsCalled);
            }

            private void OnProductFetched(long productId)
            {
                GetPageFromTab<PaginatedPage>(PackageFilterTab.AssetStore).OnProductFetched(productId);
            }

            public VisualState GetVisualState(IPackage package)
            {
                return GetPageFromTab().GetVisualState(package?.uniqueId);
            }

            private static RefreshOptions GetRefreshOptionsByTab(PackageFilterTab tab)
            {
                var index = (int)tab;
                return index >= k_RefreshOptionsByTab.Length ? RefreshOptions.None : k_RefreshOptionsByTab[index];
            }

            public void Refresh(PackageFilterTab? tab = null, int pageSize = k_DefaultPageSize)
            {
                Refresh(GetRefreshOptionsByTab(tab ?? PackageFiltering.instance.currentFilterTab), pageSize);
            }

            public void Refresh(RefreshOptions options, int pageSize = k_DefaultPageSize)
            {
                if (pageSize == 0)
                    return;

                // make sure the events are registered before actually calling the actual refresh functions
                // such that we don't lose any callbacks events
                UnregisterEvents();
                RegisterEvents();

                if ((options & RefreshOptions.UpmSearch) != 0)
                {
                    UpmClient.instance.SearchAll();
                    // Since the SearchAll online call now might return error and an empty list, we want to trigger a `SearchOffline` call if
                    // we detect that SearchOffline has not been called before. That way we will have some offline result to show to the user instead of nothing
                    if (!m_RefreshTimestamps.TryGetValue(RefreshOptions.UpmSearchOffline, out var value) || value == 0)
                        options |= RefreshOptions.UpmSearchOffline;
                }
                if ((options & RefreshOptions.UpmSearchOffline) != 0)
                    UpmClient.instance.SearchAll(true);
                if ((options & RefreshOptions.UpmList) != 0)
                {
                    UpmClient.instance.List();
                    // Do the same logic for the List operations as the Search operations
                    if (!m_RefreshTimestamps.TryGetValue(RefreshOptions.UpmListOffline, out var value) || value == 0)
                        options |= RefreshOptions.UpmListOffline;
                }
                if ((options & RefreshOptions.UpmListOffline) != 0)
                    UpmClient.instance.List(true);
                if ((options & RefreshOptions.Purchased) != 0)
                {
                    var queryArgs = new PurchasesQueryArgs
                    {
                        startIndex = 0,
                        limit = pageSize,
                        searchText = string.Empty
                    };

                    IPage page;
                    if (m_Pages.TryGetValue(PackageFilterTab.AssetStore, out page))
                    {
                        queryArgs.statuses = page.filters.statuses;
                        queryArgs.categories = page.filters.categories;
                        queryArgs.labels = page.filters.labels;
                        queryArgs.orderBy = page.filters.orderBy;
                        queryArgs.isReverseOrder = page.filters.isReverseOrder;
                    }

                    AssetStoreClient.instance.ListPurchases(queryArgs, false);
                }
                if ((options & RefreshOptions.PurchasedOffline) != 0)
                    AssetStoreClient.instance.RefreshLocal();
            }

            public void Fetch(string uniqueId)
            {
                long productId;
                if (ApplicationUtil.instance.isUserLoggedIn && long.TryParse(uniqueId, out productId))
                {
                    AssetStoreClient.instance.Fetch(productId);
                }
            }

            public void LoadMore(int numberOfPackages)
            {
                GetPageFromTab().LoadMore(numberOfPackages);
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                var canRefresh = PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore &&
                    ApplicationUtil.instance.isInternetReachable &&
                    !EditorApplication.isPlaying &&
                    !EditorApplication.isCompiling &&
                    !IsRefreshInProgress(RefreshOptions.Purchased);
                if (canRefresh && loggedIn)
                    Refresh(RefreshOptions.Purchased, PackageManagerWindow.instance?.packageList?.CalculateNumberOfPackagesToDisplay() ?? k_DefaultPageSize);
            }

            private void OnEditorSelectionChanged()
            {
                var packageSelectionObject = ApplicationUtil.instance.activeSelection as PackageSelectionObject;
                if (packageSelectionObject == null)
                    return;

                IPackage package;
                IPackageVersion version;
                PackageDatabase.instance.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out package, out version);
                if (package == null || version == null)
                    return;

                var tab = FindTab(package, version);
                PackageFiltering.instance.currentFilterTab = tab;
                SetSelected(package, version);
            }

            public void Setup()
            {
                m_Initialized = true;
                InitializeRefreshTimestamps();
                InitializeSelectionObjects();
                RegisterEvents();
            }

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;

                m_EventsRegistered = true;

                PackageDatabase.instance.RegisterEvents();

                UpmClient.instance.onListOperation += OnRefreshOperation;
                UpmClient.instance.onSearchAllOperation += OnRefreshOperation;

                UpmRegistryClient.instance.onRegistriesModified += OnRegistriesModified;

                AssetStoreClient.instance.onListOperation += OnRefreshOperation;
                AssetStoreClient.instance.onProductListFetched += OnProductListFetched;
                AssetStoreClient.instance.onProductFetched += OnProductFetched;

                PackageDatabase.instance.onInstallSuccess += OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess += OnUninstalled;
                PackageDatabase.instance.onPackagesChanged += OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged += OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged += OnSearchTextChanged;

                PackageManagerProjectSettings.instance.onEnablePackageDependenciesChanged += OnShowDependenciesChanged;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
                ApplicationUtil.instance.onEditorSelectionChanged += OnEditorSelectionChanged;
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                UpmClient.instance.onListOperation -= OnRefreshOperation;
                UpmClient.instance.onSearchAllOperation -= OnRefreshOperation;

                UpmRegistryClient.instance.onRegistriesModified -= OnRegistriesModified;

                AssetStoreClient.instance.onListOperation -= OnRefreshOperation;
                AssetStoreClient.instance.onProductListFetched -= OnProductListFetched;
                AssetStoreClient.instance.onProductFetched -= OnProductFetched;

                PackageDatabase.instance.onInstallSuccess -= OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess -= OnUninstalled;
                PackageDatabase.instance.onPackagesChanged -= OnPackagesChanged;

                PackageFiltering.instance.onFilterTabChanged -= OnFilterChanged;
                PackageFiltering.instance.onSearchTextChanged -= OnSearchTextChanged;

                PackageManagerProjectSettings.instance.onEnablePackageDependenciesChanged -= OnShowDependenciesChanged;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
                ApplicationUtil.instance.onEditorSelectionChanged -= OnEditorSelectionChanged;

                PackageDatabase.instance.UnregisterEvents();
            }

            public void Reload()
            {
                UnregisterEvents();

                m_RefreshTimestamps.Clear();
                InitializeRefreshTimestamps();
                InitializeSelectionObjects();

                ClearPages();

                m_RefreshErrors.Clear();
                m_RefreshOperationsInProgress.Clear();

                PackageDatabase.instance.Reload();

                RegisterEvents();
            }

            private void InitializeRefreshTimestamps()
            {
                foreach (RefreshOptions filter in Enum.GetValues(typeof(RefreshOptions)))
                {
                    if (m_RefreshTimestamps.ContainsKey(filter))
                        continue;

                    m_RefreshTimestamps[filter] = 0;
                }
            }

            private void InitializeSelectionObjects()
            {
                foreach (var id in m_SerializedPackageSelectionInstanceIds)
                {
                    var packageSelectionObject = UnityEngine.Object.FindObjectFromInstanceID(id) as PackageSelectionObject;
                    if (packageSelectionObject != null)
                        m_PackageSelectionObjects[packageSelectionObject.name] = packageSelectionObject;
                }
            }

            private void ClearPages()
            {
                foreach (var page in m_Pages.Values)
                {
                    page.SetSelected(null);
                    page.Rebuild();
                    UnregisterPageEvents(page);
                }
                m_Pages.Clear();
            }

            private void OnRegistriesModified()
            {
                Refresh(RefreshOptions.UpmSearch);
            }

            private void OnRefreshOperation(IOperation operation)
            {
                m_RefreshOperationsInProgress.Add(operation);
                operation.onOperationSuccess += OnRefreshOperationSuccess;
                operation.onOperationFinalized += OnRefreshOperationFinalized;
                operation.onOperationError += OnRefreshOperationError;
                if (m_RefreshOperationsInProgress.Count > 1)
                    return;
                onRefreshOperationStart?.Invoke();
            }

            private void OnRefreshOperationSuccess(IOperation operation)
            {
                m_RefreshTimestamps[operation.refreshOptions] = operation.timestamp;
                if (operation.refreshOptions == RefreshOptions.UpmSearch)
                {
                    // when an online operation successfully returns with a timestamp newer than the offline timestamp, we update the offline timestamp as well
                    // since we merge the online & offline result in the PackageDatabase and it's the newer ones that are being shown
                    if (!m_RefreshTimestamps.TryGetValue(RefreshOptions.UpmSearchOffline, out var value) || value < operation.timestamp)
                        m_RefreshTimestamps[RefreshOptions.UpmSearchOffline] = operation.timestamp;
                }
                else if (operation.refreshOptions == RefreshOptions.UpmList)
                {
                    // Do the same logic for the List operations as the Search operations
                    if (!m_RefreshTimestamps.TryGetValue(RefreshOptions.UpmListOffline, out var value) || value < operation.timestamp)
                        m_RefreshTimestamps[RefreshOptions.UpmListOffline] = operation.timestamp;
                }
                if (m_RefreshErrors.ContainsKey(operation.refreshOptions))
                    m_RefreshErrors.Remove(operation.refreshOptions);
            }

            private void OnRefreshOperationError(IOperation operation, UIError error)
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

            public bool IsInitialFetchingDone(RefreshOptions option)
            {
                foreach (var item in m_RefreshTimestamps)
                {
                    if ((option & item.Key) == 0)
                        continue;
                    if (item.Value == 0 && !m_RefreshErrors.ContainsKey(item.Key))
                        return false;
                }
                return true;
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

            public UIError GetRefreshError(RefreshOptions option)
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

            public UIError GetRefreshError(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return GetRefreshError(GetRefreshOptionsByTab(filterTab));
            }

            public bool IsRefreshInProgress(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return IsRefreshInProgress(GetRefreshOptionsByTab(filterTab));
            }

            public bool IsInitialFetchingDone(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return IsInitialFetchingDone(GetRefreshOptionsByTab(filterTab));
            }
        }
    }
}
