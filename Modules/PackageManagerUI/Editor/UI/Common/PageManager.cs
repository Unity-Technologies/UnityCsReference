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
    internal class PageManager : ISerializationCallbackReceiver
    {
        internal const string k_AssetStorePackageGroupName = "Asset Store";
        internal const string k_UnityPackageGroupName = "Unity";
        internal const string k_OtherPackageGroupName = "Other";

        internal const int k_DefaultPageSize = 25;

        private static readonly RefreshOptions[] k_RefreshOptionsByTab =
        {
            RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.UnityRegistry
            RefreshOptions.UpmList,                                             // PackageFilterTab.InProject
            RefreshOptions.UpmListOffline | RefreshOptions.UpmSearchOffline,    // PackageFilterTab.BuiltIn
            RefreshOptions.Purchased,                                           // PackageFilterTab.AssetStore
            RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.MyRegistries
        };

        public virtual event Action<PageSelection> onSelectionChanged = delegate {};
        public virtual event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};
        public virtual event Action<ListUpdateArgs> onListUpdate = delegate {};
        public virtual event Action<IPage> onListRebuild = delegate {};
        public virtual event Action<IPage> onSubPageChanged = delegate {};
        public virtual event Action<PageFilters> onFiltersChange = delegate {};

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
        private UpmCache m_UpmCache;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private UpmRegistryClient m_UpmRegistryClient;
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
            UpmCache upmCache,
            UpmClient upmClient,
            UpmRegistryClient upmRegistryClient,
            AssetStoreClient assetStoreClient,
            PackageDatabase packageDatabase,
            PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_Application = application;
            m_Selection = selection;
            m_UnityConnect = unityConnect;
            m_PackageFiltering = packageFiltering;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_UpmCache = upmCache;
            m_UpmClient = upmClient;
            m_UpmRegistryClient = upmRegistryClient;
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

        public virtual PackageSelectionObject GetPackageSelectionObject(PackageAndVersionIdPair pair, bool createIfNotFound = false)
        {
            m_PackageDatabase.GetPackageAndVersion(pair?.packageUniqueId, pair?.versionUniqueId, out var package, out var version);
            return GetPackageSelectionObject(package, version, createIfNotFound);
        }

        public virtual PackageSelectionObject GetPackageSelectionObject(IPackage package, IPackageVersion version = null, bool createIfNotFound = false)
        {
            if (package == null)
                return null;

            var uniqueId = version?.uniqueId ?? package.uniqueId;
            var packageSelectionObject = m_PackageSelectionObjects.Get(uniqueId);
            if (packageSelectionObject == null && createIfNotFound)
            {
                packageSelectionObject = ScriptableObject.CreateInstance<PackageSelectionObject>();
                packageSelectionObject.hideFlags = HideFlags.DontSave;
                packageSelectionObject.name = package.displayName;
                packageSelectionObject.displayName = package.displayName;
                packageSelectionObject.packageUniqueId = package.uniqueId;
                packageSelectionObject.versionUniqueId = version?.uniqueId;
                m_PackageSelectionObjects[uniqueId] = packageSelectionObject;
            }
            return packageSelectionObject;
        }

        private IPage GetPageFromTab(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageFiltering.currentFilterTab;
            return m_Pages.TryGetValue(filterTab, out var page) ? page : CreatePageFromTab(tab);
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
                page = new PaginatedPage(L10n.Tr("assets"), m_PackageDatabase, m_AssetStoreClient, m_PackageFiltering, m_PackageManagerPrefs, filterTab, new PageCapability
                {
                    requireUserLoggedIn = true,
                    requireNetwork = true,
                    supportFilters = true,
                    orderingValues = new[]
                    {
                        new PageCapability.Ordering("Name (asc)", "name", PageCapability.Order.Ascending),
                        new PageCapability.Ordering("Name (desc)", "name", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Purchased date", "purchased_date", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Recently updated", "update_date", PageCapability.Order.Descending)
                    }
                });
            }
            else if (filterTab == PackageFilterTab.UnityRegistry)
            {
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
                {
                    requireUserLoggedIn = false,
                    requireNetwork = false,
                    supportLocalReordering = true,
                    supportFilters = true,
                    orderingValues = new[]
                    {
                        new PageCapability.Ordering("Name (asc)", "displayName", PageCapability.Order.Ascending),
                        new PageCapability.Ordering("Name (desc)", "displayName", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Published date", "publishedDate", PageCapability.Order.Descending),
                    }
                });
            }
            else if (filterTab == PackageFilterTab.MyRegistries)
            {
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
                {
                    requireUserLoggedIn = false,
                    supportLocalReordering = true,
                    requireNetwork = false,
                    supportFilters = true,
                    orderingValues = new[]
                    {
                        new PageCapability.Ordering("Name (asc)", "displayName", PageCapability.Order.Ascending),
                        new PageCapability.Ordering("Name (desc)", "displayName", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Published date", "publishedDate", PageCapability.Order.Descending)
                    }
                });
            }
            else if (filterTab == PackageFilterTab.InProject)
            {
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
                {
                    requireUserLoggedIn = false,
                    requireNetwork = false,
                    supportLocalReordering = true,
                    supportFilters = true,
                    orderingValues = new[]
                    {
                        new PageCapability.Ordering("Name (asc)", "displayName", PageCapability.Order.Ascending),
                        new PageCapability.Ordering("Name (desc)", "displayName", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Published date", "publishedDate", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Recently updated", "updateDate", PageCapability.Order.Descending)
                    }
                });
            }
            else // filterTab == PackageFilterTab.BuiltIn
            {
                page = new SimplePage(m_PackageDatabase, m_PackageFiltering, filterTab, new PageCapability
                {
                    requireUserLoggedIn = false,
                    requireNetwork = false,
                    supportLocalReordering = true,
                    orderingValues = new[]
                    {
                        new PageCapability.Ordering("Name (asc)", "displayName", PageCapability.Order.Ascending),
                        new PageCapability.Ordering("Name (desc)", "displayName", PageCapability.Order.Descending)
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
            page.onSubPageChanged += TriggerOnSubPageChanged;
            page.onFiltersChange += TriggerOnFiltersChange;
        }

        private void UnregisterPageEvents(IPage page)
        {
            page.onSelectionChanged -= OnPageSelectionChanged;
            page.onVisualStateChange -= TriggerOnVisualStateChange;
            page.onListUpdate -= TriggerOnPageUpdate;
            page.onListRebuild -= TriggerOnPageRebuild;
            page.onSubPageChanged -= TriggerOnSubPageChanged;
            page.onFiltersChange -= TriggerOnFiltersChange;
        }

        private void OnPageSelectionChanged(PageSelection selection)
        {
            onSelectionChanged?.Invoke(selection);
            SelectInInspector(selection, false);
        }

        private void TriggerOnVisualStateChange(VisualStateChangeArgs args)
        {
            if (args.page.tab == m_PackageFiltering.currentFilterTab)
                onVisualStateChange?.Invoke(args.visualStates);
        }

        private void TriggerOnPageUpdate(ListUpdateArgs args)
        {
            if (args.page.tab == m_PackageFiltering.currentFilterTab)
                onListUpdate?.Invoke(args);
        }

        private void TriggerOnPageRebuild(IPage page)
        {
            if (page.tab == m_PackageFiltering.currentFilterTab)
                onListRebuild?.Invoke(page);
        }

        private void TriggerOnSubPageChanged(IPage page)
        {
            if (page.tab == m_PackageFiltering.currentFilterTab)
                onSubPageChanged?.Invoke(page);
        }

        private void TriggerOnFiltersChange(PageFilters filters)
        {
            onFiltersChange?.Invoke(filters);
        }

        public virtual IPage GetCurrentPage()
        {
            return GetPageFromTab();
        }

        public virtual IPage GetPage(PackageFilterTab tab)
        {
            return GetPageFromTab(tab);
        }

        public virtual PageSelection GetSelection()
        {
            return GetPageFromTab().GetSelection();
        }

        public virtual void ClearSelection()
        {
            SetSelected(Enumerable.Empty<PackageAndVersionIdPair>());
        }

        public virtual void SetSelected(IPackage package, IPackageVersion version = null, bool forceSelectInInspector = false)
        {
            SetSelected(new[] { new PackageAndVersionIdPair(package?.uniqueId, version?.uniqueId) }, forceSelectInInspector);
        }

        public virtual void SetSelected(IEnumerable<PackageAndVersionIdPair> newSelection, bool forceSelectInInspector = false)
        {
            var page = GetPageFromTab();
            page.SetNewSelection(newSelection);
            if (forceSelectInInspector)
                SelectInInspector(page.GetSelection(), true);
        }

        public virtual void ToggleSelected(string packageUniqueId, bool forceSelectInInspector = false)
        {
            var page = GetPageFromTab();
            page.ToggleSelection(packageUniqueId);
            if (forceSelectInInspector)
                SelectInInspector(page.GetSelection(), true);
        }

        public virtual void AmendSelection(IEnumerable<PackageAndVersionIdPair> toAddOrUpdate, IEnumerable<PackageAndVersionIdPair> toRemove, bool forceSelectInInspector = false)
        {
            var page = GetPageFromTab();
            page.AmendSelection(toAddOrUpdate, toRemove);
            if (forceSelectInInspector)
                SelectInInspector(page.GetSelection(), true);
        }

        public virtual void RemoveSelection(IEnumerable<PackageAndVersionIdPair> toRemove, bool forceSelectInInspector = false)
        {
            var previousFirstSelection = GetSelection().firstSelection;
            AmendSelection(Enumerable.Empty<PackageAndVersionIdPair>(), toRemove, forceSelectInInspector);
            if (!GetSelection().Any())
                SetSelected(new[] { previousFirstSelection });
        }

        // Returns true if selection is updated after this call, otherwise returns false
        public virtual bool UpdateSelectionIfCurrentSelectionIsInvalid()
        {
            var page = GetCurrentPage();
            var selection = page.GetSelection();

            var toAddOrUpdate = new List<PackageAndVersionIdPair>();
            var toRemove = new List<PackageAndVersionIdPair>();
            foreach (var item in selection)
            {
                m_PackageDatabase.GetPackageAndVersion(item, out var package, out var version);
                var visualState = page.GetVisualState(item.packageUniqueId);
                if (package == null || visualState?.visible != true)
                    toRemove.Add(item);
            }

            if (selection.Count > 0 && toAddOrUpdate.Count + toRemove.Count == 0)
                return false;

            if (toRemove.Count == selection.Count)
            {
                var firstVisible = page.visualStates.FirstOrDefault(v => v.visible && !selection.Contains(v.packageUniqueId));
                if (firstVisible != null)
                    toAddOrUpdate.Add(new PackageAndVersionIdPair(firstVisible.packageUniqueId));
            }

            return page.AmendSelection(toAddOrUpdate, toRemove);
        }

        public virtual void TriggerOnSelectionChanged()
        {
            GetPageFromTab().TriggerOnSelectionChanged();
        }

        private void SelectInInspector(PageSelection selection, bool forceSelectInInspector)
        {
            // There are 3 situations when we want to select a package in the inspector
            // 1) we explicitly/force to select it as a result of a manual action (in this case, forceSelectInInspector should be set to true)
            // 2) currently there's no active selection at all
            // 3) currently another package is selected in inspector, hence we are sure that we are not stealing selection from some other window
            if (forceSelectInInspector || m_Selection.activeObject == null || m_Selection.activeObject is PackageSelectionObject)
            {
                var packageSelectionObjects = selection.Select(s => GetPackageSelectionObject(s, true)).ToArray();
                m_Selection.objects = packageSelectionObjects;
            }
        }

        public virtual void SetSeeAllVersions(string packageUniqueId, bool value)
        {
            m_UpmCache.SetLoadAllVersions(packageUniqueId, value);
        }

        public virtual bool IsGroupExpanded(string groupName)
        {
            return GetPageFromTab().IsGroupExpanded(groupName);
        }

        public virtual void SetGroupExpanded(string groupName, bool value)
        {
            GetPageFromTab().SetGroupExpanded(groupName, value);
        }

        public virtual PackageFilterTab FindTab(IPackage package, IPackageVersion version = null)
        {
            return FindTab(new[] { version ?? package?.versions.primary });
        }

        private PackageFilterTab FindTab(IEnumerable<IPackageVersion> packageVersions)
        {
            var firstPackageVersion = packageVersions?.FirstOrDefault();
            if (firstPackageVersion == null)
                return m_PackageFiltering.currentFilterTab;

            // Since built in packages can never be in other tabs, we only need to check the first item to know which tab these packages belong to
            if (firstPackageVersion.package.Is(PackageType.BuiltIn))
                return PackageFilterTab.BuiltIn;

            var page = GetPageFromTab();
            if (packageVersions.All(v => page.Contains(v.packageUniqueId)))
                return page.tab;

            if (!m_SettingsProxy.enablePreReleasePackages && packageVersions.Any(v => v.version?.Prerelease.StartsWith("pre.") == true))
            {
                Debug.Log("You must check \"Enable Pre-release Packages\" in Project Settings > Package Manager in order to see this package.");
                return m_PackageFiltering.currentFilterTab;
            }

            if (packageVersions.All(v => v.package.versions.installed != null || (v.package.progress == PackageProgress.Installing && v.package is PlaceholderPackage)))
                return PackageFilterTab.InProject;

            if (packageVersions.All(v => v.HasTag(PackageTag.Unity)))
                return PackageFilterTab.UnityRegistry;

            if (packageVersions.All(v => v.package.Is(PackageType.AssetStore)))
                return PackageFilterTab.UnityRegistry;

            return PackageFilterTab.MyRegistries;
        }

        private void OnPackageUniqueIdFinalize(string tempPackageUniqueId, string finalPackageUniqueId)
        {
            var selection = GetSelection();
            if (!selection.Contains(tempPackageUniqueId))
                return;
            AmendSelection(new[] { new PackageAndVersionIdPair(finalPackageUniqueId) }, new[] { new PackageAndVersionIdPair(tempPackageUniqueId) });
        }

        private void OnSearchTextChanged(string searchText)
        {
            UpdateSearchTextOnPage(GetPageFromTab(), searchText);
        }

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            var page = GetPageFromTab(filterTab);

            page.ResetUserUnlockedState();
            if (GetRefreshTimestamp(page.tab) == 0)
                Refresh(filterTab);
            page.Rebuild();
            UpdateSearchTextOnPage(page, m_PackageFiltering.currentSearchText);

            OnPageSelectionChanged(page.GetSelection());

            if (m_PackageFiltering.previousFilterTab != null && m_PackageFiltering.previousFilterTab != PackageFilterTab.AssetStore)
            {
                var previousPage = GetPage((PackageFilterTab)m_PackageFiltering.previousFilterTab);
                var selectedVisualStates = previousPage.GetSelectedVisualStates();
                var selectedGroups = new HashSet<string>(selectedVisualStates.Select(v => v.groupName).Where(groupName => !string.IsNullOrEmpty(groupName)));
                foreach (var group in selectedGroups)
                    previousPage.SetGroupExpanded(group, true);
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

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            GetPageFromTab().OnPackagesChanged(args);

            foreach (var package in args.removed)
            {
                var packageSelectionObject = GetPackageSelectionObject(package);
                if (packageSelectionObject != null)
                {
                    m_PackageSelectionObjects.Remove(packageSelectionObject.uniqueId);
                    UnityEngine.Object.DestroyImmediate(packageSelectionObject);
                }
            }
        }

        private void OnProductListFetched(AssetStorePurchases productList)
        {
            GetPageFromTab<PaginatedPage>(PackageFilterTab.AssetStore).OnProductListFetched(productList);
        }

        private void OnProductExtraFetched(long productId)
        {
            GetPageFromTab<PaginatedPage>(PackageFilterTab.AssetStore).OnProductExtraFetched(productId);
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

        public virtual void Refresh(PackageFilterTab? tab = null)
        {
            Refresh(GetRefreshOptionsByTab(tab ?? m_PackageFiltering.currentFilterTab));
        }

        public virtual void Refresh(RefreshOptions options)
        {
            if ((options & RefreshOptions.UpmAny) != 0)
            {
                var entitlements = m_PackageDatabase.allPackages.Where(package => package.hasEntitlementsError);
                if (entitlements.Any())
                {
                    foreach (var package in entitlements)
                        package.ClearErrors(error => error.errorCode == UIErrorCode.UpmError_Forbidden);
                    TriggerOnSelectionChanged();
                }
            }

            if ((options & RefreshOptions.UpmSearch) != 0)
            {
                m_UpmClient.SearchAll();
                // Since the SearchAll online call now might return error and an empty list, we want to trigger a `SearchOffline` call if
                // we detect that SearchOffline has not been called before. That way we will have some offline result to show to the user instead of nothing
                if (!m_RefreshTimestamps.TryGetValue(RefreshOptions.UpmSearchOffline, out var value) || value == 0)
                    options |= RefreshOptions.UpmSearchOffline;
            }
            if ((options & RefreshOptions.UpmSearchOffline) != 0)
                m_UpmClient.SearchAll(true);
            if ((options & RefreshOptions.UpmList) != 0)
            {
                m_UpmClient.List();
                // Do the same logic for the List operations as the Search operations
                if (!m_RefreshTimestamps.TryGetValue(RefreshOptions.UpmListOffline, out var value) || value == 0)
                    options |= RefreshOptions.UpmListOffline;
            }
            if ((options & RefreshOptions.UpmListOffline) != 0)
                m_UpmClient.List(true);
            if ((options & RefreshOptions.Purchased) != 0)
            {
                var queryArgs = new PurchasesQueryArgs
                {
                    startIndex = 0,
                    limit = Math.Max(GetCurrentPage().numCurrentItems, m_PackageManagerPrefs.numItemsPerPage ?? k_DefaultPageSize),
                    searchText = m_PackageFiltering.currentSearchText
                };

                IPage page;
                if (m_Pages.TryGetValue(PackageFilterTab.AssetStore, out page))
                {
                    queryArgs.status = page.filters.status;
                    queryArgs.categories = page.filters.categories;
                    queryArgs.labels = page.filters.labels;
                    queryArgs.orderBy = page.filters.orderBy;
                    queryArgs.isReverseOrder = page.filters.isReverseOrder;
                }

                m_AssetStoreClient.ListPurchases(queryArgs);
            }
            if ((options & RefreshOptions.PurchasedOffline) != 0)
                m_AssetStoreClient.RefreshLocal();
        }

        public virtual void CancelRefresh(PackageFilterTab? tab = null)
        {
            CancelRefresh(GetRefreshOptionsByTab(tab ?? m_PackageFiltering.currentFilterTab));
        }

        public virtual void CancelRefresh(RefreshOptions options)
        {
            if ((options & RefreshOptions.Purchased) != 0)
                m_AssetStoreClient.CancelListPurchases();
        }

        public virtual void Fetch(string uniqueId)
        {
            if (m_UnityConnect.isUserLoggedIn && long.TryParse(uniqueId, out var productId))
                m_AssetStoreClient.ExtraFetch(productId);
        }

        public virtual void LoadMore(long numberOfPackages)
        {
            GetPageFromTab().LoadMore(numberOfPackages);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                // When users log out, even when we are not on `My Assets` tab we should still clear the Asset Store page properly
                GetPageFromTab<PaginatedPage>(PackageFilterTab.AssetStore).ClearAndRebuild();

                // We also want to clear the refresh time stamp here so that the next time users visit the Asset Store page, we'll call
                // refresh properly
                m_RefreshTimestamps[RefreshOptions.Purchased] = 0;
            }
            else if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore &&
                m_Application.isInternetReachable && !EditorApplication.isPlaying &&
                !EditorApplication.isCompiling &&
                !IsRefreshInProgress(RefreshOptions.Purchased))
                Refresh(RefreshOptions.Purchased);
        }

        private void OnEditorSelectionChanged()
        {
            var selectionIds = new List<PackageAndVersionIdPair>();
            var selectedVersions = new List<IPackageVersion>();
            foreach (var selectionObject in m_Selection.objects)
            {
                var packageSelectionObject = selectionObject as PackageSelectionObject;
                if (packageSelectionObject == null)
                    return;
                m_PackageDatabase.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out var package, out var version);
                if (package == null && version == null)
                    continue;
                selectedVersions.Add(version ?? package?.versions.primary);
                selectionIds.Add(new PackageAndVersionIdPair(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId));
            }

            if (!selectionIds.Any())
                return;

            m_PackageFiltering.currentFilterTab = FindTab(selectedVersions);
            SetSelected(selectionIds);
        }

        public void OnEnable()
        {
            InitializeRefreshTimestamps();
            InitializeSelectionObjects();
            InitializeSubPages();

            m_UpmClient.onListOperation += OnRefreshOperation;
            m_UpmClient.onSearchAllOperation += OnRefreshOperation;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;

            m_AssetStoreClient.onListOperation += OnRefreshOperation;
            m_AssetStoreClient.onProductListFetched += OnProductListFetched;
            m_AssetStoreClient.onProductExtraFetched += OnProductExtraFetched;

            m_PackageDatabase.onPackageUniqueIdFinalize += OnPackageUniqueIdFinalize;
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;

            m_PackageFiltering.onFilterTabChanged += OnFilterChanged;
            m_PackageFiltering.onSearchTextChanged += OnSearchTextChanged;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Selection.onSelectionChanged += OnEditorSelectionChanged;
        }

        public void OnDisable()
        {
            m_UpmClient.onListOperation -= OnRefreshOperation;
            m_UpmClient.onSearchAllOperation -= OnRefreshOperation;

            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;

            m_AssetStoreClient.onListOperation -= OnRefreshOperation;
            m_AssetStoreClient.onProductListFetched -= OnProductListFetched;
            m_AssetStoreClient.onProductExtraFetched -= OnProductExtraFetched;

            m_PackageDatabase.onPackageUniqueIdFinalize -= OnPackageUniqueIdFinalize;
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;

            m_PackageFiltering.onFilterTabChanged -= OnFilterChanged;
            m_PackageFiltering.onSearchTextChanged -= OnSearchTextChanged;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Selection.onSelectionChanged -= OnEditorSelectionChanged;
        }

        public virtual void Reload()
        {
            m_RefreshTimestamps.Clear();
            InitializeRefreshTimestamps();

            ClearPages();

            InitializeSubPages();

            m_RefreshErrors.Clear();
            m_RefreshOperationsInProgress.Clear();

            m_PackageDatabase.Reload();
        }

        private void InitializeRefreshTimestamps()
        {
            foreach (RefreshOptions filter in Enum.GetValues(typeof(RefreshOptions)))
            {
                if (filter == RefreshOptions.None || filter == RefreshOptions.UpmAny)
                    continue;

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
                    m_PackageSelectionObjects[packageSelectionObject.uniqueId] = packageSelectionObject;
            }
        }

        private void InitializeSubPages()
        {
            static bool FilterAllPackages(IPackage package) => true;

            static string GroupPackagesAndFeatures(IPackage package)
            {
                if (package?.Is(PackageType.Feature) == true) return L10n.Tr("Features");
                return L10n.Tr("Packages");
            }

            static string GroupPackagesWithAuthorAndFeatures(IPackage package)
            {
                if (package?.Is(PackageType.Feature) == true && package.versions.All(v => v.HasTag(PackageTag.Unity))) return L10n.Tr("Features");
                return string.Format(L10n.Tr("Packages - {0}"), BasePage.GetDefaultGroupName(PackageFilterTab.InProject, package));
            }

            AddSubPage(PackageFilterTab.UnityRegistry, "all", L10n.Tr("All"), L10n.Tr("packages and features"), 0, FilterAllPackages, GroupPackagesAndFeatures);
            AddSubPage(PackageFilterTab.InProject, "all", L10n.Tr("All"), L10n.Tr("packages and features"), 0, FilterAllPackages, GroupPackagesWithAuthorAndFeatures);
        }

        public SubPage AddSubPage(PackageFilterTab tab, string name, string displayName, string contentType, int priority = 0, Func<IPackage, bool> filterFunction = null, Func<IPackage, string> groupNameFunction = null, Func<string, string, int> compareGroupFunction = null)
        {
            var subPage = new SubPage(tab, name, displayName, contentType, priority, filterFunction, groupNameFunction, compareGroupFunction);
            GetPage(tab).AddSubPage(subPage);
            return subPage;
        }

        private void ClearPages()
        {
            foreach (var page in m_Pages.Values)
            {
                page.SetNewSelection(Enumerable.Empty<PackageAndVersionIdPair>());
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
                if (item.Value == 0 && !m_RefreshErrors.ContainsKey(item.Key))
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

        public virtual void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            GetCurrentPage().SetPackagesUserUnlockedState(packageUniqueIds, unlocked);
        }
    }
}
