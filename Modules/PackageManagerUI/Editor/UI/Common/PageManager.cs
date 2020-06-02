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
    internal class PageManager : ISerializationCallbackReceiver
    {
        internal const int k_DefaultPageSize = 25;

        private static readonly RefreshOptions[] k_RefreshOptionsByTab =
        {
            RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.All
            RefreshOptions.UpmList,                                             // PackageFilterTab.InProject
            RefreshOptions.UpmListOffline | RefreshOptions.UpmSearchOffline,    // PackageFilterTab.BuiltIn
            RefreshOptions.Purchased                                            // PackageFilterTab.AssetStore
        };

        public virtual event Action<IPackageVersion> onSelectionChanged = delegate {};
        public virtual event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};
        public virtual event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>, bool> onListUpdate = delegate {};
        public virtual event Action<IPage> onListRebuild = delegate {};

        public virtual event Action onRefreshOperationStart = delegate {};
        public virtual event Action onRefreshOperationFinish = delegate {};
        public virtual event Action<UIError> onRefreshOperationError = delegate {};

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

        [SerializeField]
        private int[] m_SerializedPackageSelectionInstanceIds = new int[0];

        [NonSerialized]
        private Dictionary<string, PackageSelectionObject> m_PackageSelectionObjects = new Dictionary<string, PackageSelectionObject>();

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private SelectionProxy m_Selection;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private PackageFiltering m_PackageFiltering;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private AssetStoreClient m_AssetStoreClient;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(ApplicationProxy application,
            SelectionProxy selection,
            UnityConnectProxy unityConnect,
            PackageFiltering packageFiltering,
            PackageManagerPrefs packageManagerPrefs,
            UpmClient upmClient,
            AssetStoreClient assetStoreClient,
            PackageDatabase packageDatabase,
            PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_Application = application;
            m_Selection = selection;
            m_UnityConnect = unityConnect;
            m_PackageFiltering = packageFiltering;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_UpmClient = upmClient;
            m_AssetStoreClient = assetStoreClient;
            m_PackageDatabase = packageDatabase;
            m_SettingsProxy = settingsProxy;

            foreach (var page in m_SerializedSimplePages)
                page.ResolveDependencies(packageDatabase, packageFiltering);
            foreach (var page in m_SerializedPaginatedPage)
                page.ResolveDependencies(packageDatabase, assetStoreClient, packageFiltering, packageManagerPrefs);
        }

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
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;

            IPage page;
            return m_Pages.TryGetValue(filterTab, out page) ? page : CreatePageFromTab(tab);
        }

        private T GetPageFromTab<T>(PackageFilterTab? tab = null) where T : class, IPage
        {
            return GetPageFromTab(tab) as T;
        }

        private IPage CreatePageFromTab(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;
            IPage page;
            if (filterTab == PackageFilterTab.AssetStore)
            {
                page = new PaginatedPage(m_PackageDatabase, m_AssetStoreClient, m_PackageFiltering, m_PackageManagerPrefs, filterTab, new PageCapability
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
            else if (filterTab == PackageFilterTab.All)
            {
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
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
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
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
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
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

            SelectInInspector(m_PackageDatabase.GetPackage(version), version, false);
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

        public virtual IPage GetCurrentPage()
        {
            return GetPageFromTab();
        }

        public virtual IPage GetPage(PackageFilterTab tab)
        {
            return GetPageFromTab(tab);
        }

        public virtual IPackageVersion GetSelectedVersion()
        {
            return GetPageFromTab().GetSelectedVersion();
        }

        public virtual void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version)
        {
            GetPageFromTab().GetSelectedPackageAndVersion(out package, out version);
        }

        public virtual void ClearSelection()
        {
            SetSelected(null);
        }

        public virtual void SetSelected(IPackage package, IPackageVersion version = null, bool forceSelectInInspector = false)
        {
            GetPageFromTab().SetSelected(package, version);
            SelectInInspector(package, version, forceSelectInInspector);
        }

        private void SelectInInspector(IPackage package, IPackageVersion version, bool forceSelectInInspector)
        {
            // There are two situations when we want to select a package in the inspector
            // 1) we explicitly/force to select it as a result of a manual action (in this case, forceSelectInInspector should be set to true)
            // 2) currently another package is selected in inspector, hence we are sure that we are not stealing selection from some other window
            var currentPackageSelection = m_Selection.activeObject as PackageSelectionObject;
            if (forceSelectInInspector || currentPackageSelection != null)
            {
                var packageSelectionObject = GetPackageSelectionObject(package, version, true);
                if (m_Selection.activeObject != packageSelectionObject)
                    m_Selection.activeObject = packageSelectionObject;
            }
        }

        public virtual void SetSeeAllVersions(IPackage package, bool value)
        {
            GetPageFromTab().SetSeeAllVersions(package, value);
        }

        public virtual void SetExpanded(IPackage package, bool value)
        {
            GetPageFromTab().SetExpanded(package, value);
        }

        public virtual PackageFilterTab FindTab(IPackage package, IPackageVersion version = null)
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

            if (!m_SettingsProxy.enablePreviewPackages && (version?.version?.Major == 0 || (version?.version?.Prerelease.StartsWith("preview") ?? false) ||
                                                           package?.versions.primary.version?.Major == 0 || (package?.versions.primary.version?.Prerelease.StartsWith("preview") ?? false)))
            {
                Debug.Log("You must check \"Enable Preview Packages\" in Project Settings > Package Manager in order to see this package.");
                return page.tab;
            }

            return PackageFilterTab.All;
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
            UpdateSearchTextOnPage(page, m_PackageFiltering.currentSearchText);

            // When the filter tab is changed, on a page level, the selection hasn't changed because selection is kept for each filter
            // However, the active selection is still changed if you look at package manager as a whole
            OnPageSelectionChanged(page.GetSelectedVersion());
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
            if (m_PackageFiltering.currentFilterTab != PackageFilterTab.InProject)
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

        public virtual VisualState GetVisualState(IPackage package)
        {
            return GetPageFromTab().GetVisualState(package?.uniqueId);
        }

        private static RefreshOptions GetRefreshOptionsByTab(PackageFilterTab tab)
        {
            var index = (int)tab;
            return index >= k_RefreshOptionsByTab.Length ? RefreshOptions.None : k_RefreshOptionsByTab[index];
        }

        public virtual void Refresh(PackageFilterTab? tab = null, int pageSize = k_DefaultPageSize)
        {
            Refresh(GetRefreshOptionsByTab(tab ?? m_PackageFiltering.currentFilterTab), pageSize);
        }

        public virtual void Refresh(RefreshOptions options, int pageSize = k_DefaultPageSize)
        {
            if (pageSize == 0)
                return;

            if ((options & RefreshOptions.UpmSearchOffline) != 0)
                m_UpmClient.SearchAll(true);
            if ((options & RefreshOptions.UpmSearch) != 0)
                m_UpmClient.SearchAll();
            if ((options & RefreshOptions.UpmListOffline) != 0)
                m_UpmClient.List(true);
            if ((options & RefreshOptions.UpmList) != 0)
                m_UpmClient.List();
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

                m_AssetStoreClient.ListPurchases(queryArgs, false);
            }
            if ((options & RefreshOptions.PurchasedOffline) != 0)
                m_AssetStoreClient.RefreshLocal();
        }

        public virtual void Fetch(string uniqueId)
        {
            long productId;
            if (m_UnityConnect.isUserLoggedIn && long.TryParse(uniqueId, out productId))
            {
                m_AssetStoreClient.Fetch(productId);
            }
        }

        public virtual void LoadMore(int numberOfPackages)
        {
            GetPageFromTab().LoadMore(numberOfPackages);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            var canRefresh = m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore &&
                m_Application.isInternetReachable &&
                !EditorApplication.isPlaying &&
                !EditorApplication.isCompiling &&
                !IsRefreshInProgress(RefreshOptions.Purchased);
            if (canRefresh && loggedIn)
                Refresh(RefreshOptions.Purchased, m_PackageManagerPrefs.numItemsPerPage ?? k_DefaultPageSize);
        }

        private void OnEditorSelectionChanged()
        {
            var packageSelectionObject = m_Selection.activeObject as PackageSelectionObject;
            if (packageSelectionObject == null)
                return;

            IPackage package;
            IPackageVersion version;
            m_PackageDatabase.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out package, out version);
            if (package == null || version == null)
                return;

            var tab = FindTab(package, version);
            m_PackageFiltering.currentFilterTab = tab;
            SetSelected(package, version);
        }

        public void OnEnable()
        {
            InitializeRefreshTimestamps();
            InitializeSelectionObjects();

            m_UpmClient.onListOperation += OnRefreshOperation;
            m_UpmClient.onSearchAllOperation += OnRefreshOperation;

            m_AssetStoreClient.onListOperation += OnRefreshOperation;
            m_AssetStoreClient.onProductListFetched += OnProductListFetched;
            m_AssetStoreClient.onProductFetched += OnProductFetched;

            m_PackageDatabase.onInstallSuccess += OnInstalledOrUninstalled;
            m_PackageDatabase.onUninstallSuccess += OnUninstalled;
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;

            m_PackageFiltering.onFilterTabChanged += OnFilterChanged;
            m_PackageFiltering.onSearchTextChanged += OnSearchTextChanged;

            m_PackageManagerPrefs.onShowDependenciesChanged += OnShowDependenciesChanged;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Selection.onSelectionChanged += OnEditorSelectionChanged;
        }

        public void OnDisable()
        {
            m_UpmClient.onListOperation -= OnRefreshOperation;
            m_UpmClient.onSearchAllOperation -= OnRefreshOperation;

            m_AssetStoreClient.onListOperation -= OnRefreshOperation;
            m_AssetStoreClient.onProductListFetched -= OnProductListFetched;
            m_AssetStoreClient.onProductFetched -= OnProductFetched;

            m_PackageDatabase.onInstallSuccess -= OnInstalledOrUninstalled;
            m_PackageDatabase.onUninstallSuccess -= OnUninstalled;
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;

            m_PackageFiltering.onFilterTabChanged -= OnFilterChanged;
            m_PackageFiltering.onSearchTextChanged -= OnSearchTextChanged;

            m_PackageManagerPrefs.onShowDependenciesChanged -= OnShowDependenciesChanged;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Selection.onSelectionChanged -= OnEditorSelectionChanged;
        }

        public virtual void Reload()
        {
            m_RefreshTimestamps.Clear();
            InitializeRefreshTimestamps();

            ClearPages();

            m_RefreshErrors.Clear();
            m_RefreshOperationsInProgress.Clear();

            m_PackageDatabase.Reload();
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

        public virtual bool IsRefreshInProgress(RefreshOptions option)
        {
            return m_RefreshOperationsInProgress.Any(operation => (operation.refreshOptions & option) != 0);
        }

        public virtual bool IsInitialFetchingDone(RefreshOptions option)
        {
            foreach (var item in m_RefreshTimestamps)
            {
                if ((option & item.Key) == 0)
                    continue;
                if (item.Value == 0)
                    return false;
            }
            return true;
        }

        public virtual long GetRefreshTimestamp(RefreshOptions option)
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

        public virtual UIError GetRefreshError(RefreshOptions option)
        {
            // only return the first one when there are multiple errors
            foreach (var item in m_RefreshErrors)
                if ((option & item.Key) != 0)
                    return item.Value;
            return null;
        }

        public virtual long GetRefreshTimestamp(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;
            return GetRefreshTimestamp(GetRefreshOptionsByTab(filterTab));
        }

        public virtual UIError GetRefreshError(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;
            return GetRefreshError(GetRefreshOptionsByTab(filterTab));
        }

        public virtual bool IsRefreshInProgress(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;
            return IsRefreshInProgress(GetRefreshOptionsByTab(filterTab));
        }

        public virtual bool IsInitialFetchingDone(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;
            return IsInitialFetchingDone(GetRefreshOptionsByTab(filterTab));
        }
    }
}
