// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    using AssetStoreCachePathConfig = UnityEditorInternal.AssetStoreCachePathManager.CachePathConfig;
    using AssetStoreConfigStatus = UnityEditorInternal.AssetStoreCachePathManager.ConfigStatus;

    internal class PackageManagerWindowRoot : VisualElement, IWindow
    {
        private string m_PackageToSelectOnLoaded;

        private PackageFilterTab? m_FilterToSelectAfterLoad;

        private string m_SubPageToSelectAfterLoad;

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
        private SelectionProxy m_Selection;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private UnityConnectProxy m_UnityConnectProxy;
        private ApplicationProxy m_ApplicationProxy;
        private UpmClient m_UpmClient;
        private AssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        private void ResolveDependencies(ResourceLoader resourceLoader,
            ExtensionManager extensionManager,
            SelectionProxy selection,
            PackageFiltering packageFiltering,
            PackageManagerPrefs packageManagerPrefs,
            PackageDatabase packageDatabase,
            PageManager pageManager,
            PackageManagerProjectSettingsProxy settingsProxy,
            UnityConnectProxy unityConnectProxy,
            ApplicationProxy applicationProxy,
            UpmClient upmClient,
            AssetStoreCachePathProxy assetStoreCachePathProxy)
        {
            m_ResourceLoader = resourceLoader;
            m_ExtensionManager = extensionManager;
            m_Selection = selection;
            m_PackageFiltering = packageFiltering;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_UnityConnectProxy = unityConnectProxy;
            m_ApplicationProxy = applicationProxy;
            m_UpmClient = upmClient;
            m_AssetStoreCachePathProxy = assetStoreCachePathProxy;
        }

        public PackageManagerWindowRoot(ResourceLoader resourceLoader,
                                        ExtensionManager extensionManager,
                                        SelectionProxy selection,
                                        PackageFiltering packageFiltering,
                                        PackageManagerPrefs packageManagerPrefs,
                                        PackageDatabase packageDatabase,
                                        PageManager pageManager,
                                        PackageManagerProjectSettingsProxy settingsProxy,
                                        UnityConnectProxy unityConnectProxy,
                                        ApplicationProxy applicationProxy,
                                        UpmClient upmClient,
                                        AssetStoreCachePathProxy assetStoreCachePathProxy)
        {
            ResolveDependencies(resourceLoader, extensionManager, selection, packageFiltering, packageManagerPrefs, packageDatabase, pageManager, settingsProxy, unityConnectProxy, applicationProxy, upmClient, assetStoreCachePathProxy);
        }

        public void OnEnable()
        {
            styleSheets.Add(m_ResourceLoader.packageManagerWindowStyleSheet);

            var root = m_ResourceLoader.GetTemplate("PackageManagerWindow.uxml");
            Add(root);
            cache = new VisualElementCache(root);
            var newTab = m_PackageManagerPrefs.lastUsedPackageFilter ?? PackageFiltering.k_DefaultFilterTab;

            // Reset the lock icons when users open a new Package Manager window
            m_PageManager.GetPage(newTab).ResetUserUnlockedState();

            packageDetails.OnEnable();
            packageList.OnEnable();
            packageManagerToolbar.OnEnable();
            packageSubPageFilterBar.OnEnable();
            packageStatusbar.OnEnable();

            leftColumnContainer.style.flexGrow = m_PackageManagerPrefs.splitterFlexGrow;
            rightColumnContainer.style.flexGrow = 1 - m_PackageManagerPrefs.splitterFlexGrow;

            m_PageManager.onRefreshOperationFinish += OnRefreshOperationFinish;
            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCachePathProxy.onConfigChanged += OnAssetStoreCacheConfigChange;

            PackageManagerWindowAnalytics.Setup();

            EditorApplication.focusChanged += OnFocusChanged;
            m_Selection.onSelectionChanged += RefreshSelectedInInspectorClass;

            focusable = true;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RefreshSelectedInInspectorClass();
        }

        public void OnCreateGUI()
        {
            var newTab = m_PackageManagerPrefs.lastUsedPackageFilter ?? PackageFiltering.k_DefaultFilterTab;

            // set the current filter tab value after all the callback system has been setup so that we don't miss any callbacks
            m_PackageFiltering.currentFilterTab = newTab;

            if (m_PageManager.GetRefreshTimestamp(newTab) == 0)
                DelayRefresh(newTab);

            if (newTab != PackageFilterTab.UnityRegistry && m_PageManager.GetRefreshTimestamp(PackageFilterTab.UnityRegistry) == 0 && m_ApplicationProxy.isUpmRunning)
                DelayRefresh(PackageFilterTab.UnityRegistry);

            m_ExtensionManager.OnWindowCreated(this, packageDetails.extensionContainer, packageDetails.toolbar.extensions);
        }

        private void DelayRefresh(PackageFilterTab tab, string subPage = "")
        {
            if (!m_ApplicationProxy.isUpmRunning)
            {
                if (!m_ApplicationProxy.isBatchMode)
                    Debug.Log(L10n.Tr("[Package Manager Window] UPM server is not running. Please check that your Editor was not launched with '-noUpm' command line option."));

                packageList.HideListShowEmptyArea(L10n.Tr("UPM server is not running"));
                packageStatusbar.DisableRefresh();
                return;
            }

            if (tab == PackageFilterTab.AssetStore &&
                (m_PackageManagerPrefs.numItemsPerPage == null || !m_UnityConnectProxy.isUserInfoReady))
            {
                EditorApplication.delayCall += () => DelayRefresh(tab, subPage);
                return;
            }

            m_PageManager.Refresh(tab);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
            packageList.Focus();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
        }

        private void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (evt.commandName == EventCommandNames.Find)
                evt.StopPropagation();
        }

        private void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            if (evt.commandName == EventCommandNames.Find)
            {
                packageManagerToolbar.FocusOnSearch();
                evt.StopPropagation();
            }
        }

        private void OnFocusChanged(bool focus)
        {
            var canRefresh = !EditorApplication.isPlaying && !EditorApplication.isCompiling;
            if (focus && canRefresh && m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore)
                m_PageManager.Refresh(RefreshOptions.PurchasedOffline);
        }

        public void OnDisable()
        {
            m_PackageManagerPrefs.lastUsedPackageFilter = m_PackageFiltering.currentFilterTab;

            m_PageManager.onRefreshOperationFinish -= OnRefreshOperationFinish;
            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged -= OnAssetStoreCacheConfigChange;

            packageDetails.OnDisable();
            packageList.OnDisable();
            packageManagerToolbar.OnDisable();
            packageSubPageFilterBar.OnDisable();
            packageStatusbar.OnDisable();

            EditorApplication.focusChanged -= OnFocusChanged;
            m_Selection.onSelectionChanged -= RefreshSelectedInInspectorClass;

            m_PackageManagerPrefs.splitterFlexGrow = leftColumnContainer.resolvedStyle.flexGrow;
        }

        private void OnAssetStoreCacheConfigChange(AssetStoreCachePathConfig config)
        {
            if ((config.status == AssetStoreConfigStatus.Success || config.status == AssetStoreConfigStatus.ReadOnly) && m_PageManager.GetRefreshTimestamp(PackageFilterTab.AssetStore) > 0)
                m_PageManager.Refresh(RefreshOptions.PurchasedOffline);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!userInfoReady || m_PackageDatabase.isEmpty || !m_PageManager.IsInitialFetchingDone())
                return;

            var entitlements = m_PackageDatabase.allPackages.Where(package =>  package.hasEntitlements);
            if (loggedIn)
            {
                if (entitlements.Any(package => (package.versions?.primary.isInstalled ?? false) && (package.versions?.primary.hasEntitlementsError ?? false)))
                    m_UpmClient.Resolve();
                else
                {
                    m_PageManager.Refresh(RefreshOptions.UpmList | RefreshOptions.UpmSearch);
                    m_PageManager.TriggerOnSelectionChanged();
                }
            }
            else
            {
                if (entitlements.Any())
                {
                    m_PageManager.Refresh(RefreshOptions.UpmList | RefreshOptions.UpmSearch);
                    m_PageManager.TriggerOnSelectionChanged();
                }
            }
        }

        public void OnDestroy()
        {
            m_ExtensionManager.OnWindowDestroy();
            LoadingSpinner.ClearAllSpinners();
        }

        private void OnRefreshOperationFinish()
        {
            if (m_FilterToSelectAfterLoad != null && m_PageManager.GetRefreshTimestamp(m_FilterToSelectAfterLoad) > 0)
                SelectPackageAndFilter();
        }

        private void SelectPackageAndFilter()
        {
            IPackageVersion version = null;
            IPackage package = null;
            if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
                m_PackageDatabase.GetPackageAndVersionByIdOrName(m_PackageToSelectOnLoaded, out package, out version);

            if (m_FilterToSelectAfterLoad == PackageFilterTab.AssetStore)
            {
                m_PackageFiltering.currentFilterTab = PackageFilterTab.AssetStore;

                if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
                {
                    if (package == null || package is PlaceholderPackage)
                        m_PageManager.Fetch(m_PackageToSelectOnLoaded);
                    else
                        m_PageManager.GetPage(PackageFilterTab.AssetStore).Load(package, version);
                }

                m_FilterToSelectAfterLoad = null;
                m_PackageToSelectOnLoaded = null;
                m_SubPageToSelectAfterLoad = null;
                return;
            }

            // The !IsInitialFetchingDone check was added to the start of this function in the past for the Entitlement Error checker,
            // But it caused `Open In Unity` to not work sometimes for the `My Assets` tab. Hence we moved the check from the beginning
            // of this function to after the `My Assets` logic is done so that we don't break `My Assets` and the Entitlement Error checker.
            if (!m_PageManager.IsInitialFetchingDone())
                return;

            if (package != null || m_FilterToSelectAfterLoad != null)
            {
                var tab = m_FilterToSelectAfterLoad ?? PackageFiltering.k_DefaultFilterTab;

                if (m_SubPageToSelectAfterLoad != null)
                {
                    void SelectSubPage(IPage page, string subPageName)
                    {
                        var subPage = page.subPages.FirstOrDefault(page => string.Compare(page.name, subPageName, StringComparison.InvariantCultureIgnoreCase) == 0) ?? page.subPages.First();
                        page.currentSubPage = subPage;
                    }

                    void OnFilterTabChangedSelectSubPage(PackageFilterTab filterTab)
                    {
                        m_PackageFiltering.onFilterTabChanged -= OnFilterTabChangedSelectSubPage;
                        SelectSubPage(m_PageManager.GetCurrentPage(), m_SubPageToSelectAfterLoad);
                    }

                    if (m_PackageFiltering.currentFilterTab == tab)
                        SelectSubPage(m_PageManager.GetCurrentPage(), m_SubPageToSelectAfterLoad);
                    else
                        m_PackageFiltering.onFilterTabChanged += OnFilterTabChangedSelectSubPage;

                    m_PackageToSelectOnLoaded = null;
                }

                m_PackageFiltering.currentFilterTab = tab;
                if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
                {
                    m_PageManager.SetSelected(package, version, true);
                    packageList.OnFocus();
                }

                m_FilterToSelectAfterLoad = null;
                m_PackageToSelectOnLoaded = null;
                m_SubPageToSelectAfterLoad = null;
            }
        }

        public void OnFocus()
        {
            AddToClassList("focus");
        }

        public void OnLostFocus()
        {
            RemoveFromClassList("focus");
        }

        private void RefreshSelectedInInspectorClass()
        {
            if (m_Selection.activeObject is PackageSelectionObject)
                AddToClassList("selectedInInspector");
            else
                RemoveFromClassList("selectedInInspector");
        }

        public void SelectFilterSubPage(string filterTabOrSubPage = "")
        {
            if (!string.IsNullOrEmpty(filterTabOrSubPage))
            {
                var split = filterTabOrSubPage.Split('/');
                if (PackageFilterTab.TryParse(split[0], true, out PackageFilterTab tab))
                {
                    var subPage = split.Length > 1 ? split[1] : string.Empty;
                    m_PackageToSelectOnLoaded = null;
                    m_FilterToSelectAfterLoad = tab;
                    m_SubPageToSelectAfterLoad = subPage;

                    if (m_PackageDatabase.isEmpty)
                        DelayRefresh(tab, subPage);
                    else
                        SelectPackageAndFilter();
                }
            }
        }

        public void SelectPackageAndFilter(string packageToSelect, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            if (!string.IsNullOrEmpty(packageToSelect) || filterTab != null)
            {
                if (filterTab == null)
                {
                    m_PackageDatabase.GetPackageAndVersionByIdOrName(packageToSelect, out var package, out var version);
                    if (package != null)
                        filterTab = m_PageManager.FindTab(package, version);
                    else
                    {
                        var packageToSelectSplit = packageToSelect.Split('@');
                        var versionString = packageToSelectSplit.Length == 2 ? packageToSelectSplit[1] : string.Empty;

                        // Package is not found in PackageDatabase but we can determine if it's a preview package or not with it's version string.
                        SemVersionParser.TryParse(versionString, out var semVersion);
                        if (!m_SettingsProxy.enablePreReleasePackages && semVersion.HasValue && (semVersion.Value.Major == 0 || semVersion.Value.Prerelease.StartsWith("preview")))
                        {
                            Debug.Log("You must check \"Enable Preview Packages\" in Project Settings > Package Manager in order to see this package.");
                            filterTab = m_PackageFiltering.currentFilterTab;
                            packageToSelect = null;
                        }
                        else
                            filterTab = PackageFilterTab.UnityRegistry;
                    }
                }

                m_FilterToSelectAfterLoad = filterTab;
                m_PackageToSelectOnLoaded = packageToSelect;
                packageManagerToolbar.SetCurrentSearch(searchText);

                if (refresh || m_PackageDatabase.isEmpty)
                    DelayRefresh((PackageFilterTab)filterTab);
                else
                    SelectPackageAndFilter();
            }
        }

        public AddPackageByNameDropdown OpenAddPackageByNameDropdown(string url)
        {
            var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_PackageFiltering, m_UpmClient, m_PackageDatabase, m_PageManager, PackageManagerWindow.instance);

            var packageNameAndVersion = url.Replace(PackageManagerWindow.k_UpmUrl, string.Empty);
            var packageName = string.Empty;
            var packageVersion = string.Empty;

            if (packageNameAndVersion.Contains("@"))
            {
                var values = packageNameAndVersion.Split('@');
                if (values.Count() > 1)
                {
                    packageName = values[0];
                    packageVersion = values[1];
                }
            }
            else
                packageName = packageNameAndVersion;

            DropdownElement.ShowDropdown(this, dropdown);

            // We need to set the name and version after the dropdown is shown,
            // so that the OnTextFieldChange of placeholder gets called
            dropdown.packageNameField.value = packageName;
            dropdown.packageVersionField.value = packageVersion;
            return dropdown;
        }

        public IDetailsExtension AddDetailsExtension()
        {
            return m_ExtensionManager.CreateDetailsExtension();
        }

        public IPackageActionMenu AddPackageActionMenu()
        {
            return m_ExtensionManager.CreatePackageActionMenu();
        }

        public IPackageActionButton AddPackageActionButton()
        {
            return m_ExtensionManager.CreatePackageActionButton();
        }

        public void Select(string identifier)
        {
            SelectPackageAndFilter(identifier);
        }

        public PackageSelectionArgs activeSelection
        {
            get
            {
                var selections = m_PageManager.GetSelection();

                // When there are multiple versions selected, we want to make the legacy single select arguments to be null
                // that way extension UI implemented for single package selection will not show for multi-select cases.
                var versions = selections.Select(selection =>
                {
                    m_PackageDatabase.GetPackageAndVersion(selection, out var package, out var version);
                    return version ?? package?.versions.primary;
                }).ToArray();

                var version = versions.Length > 1 ? null : versions.FirstOrDefault();
                return new PackageSelectionArgs { package = version?.package, packageVersion = version, versions = versions, window = this };
            }
        }

        public IMenu addMenu => packageManagerToolbar.addMenu;
        public IMenu advancedMenu => packageManagerToolbar.toolbarSettingsMenu;

        private VisualElementCache cache { set; get; }
        internal PackageList packageList { get { return cache.Get<PackageList>("packageList"); } }
        internal PackageDetails packageDetails { get { return cache.Get<PackageDetails>("packageDetails"); } }
        internal PackageManagerToolbar packageManagerToolbar { get { return cache.Get<PackageManagerToolbar>("topMenuToolbar"); } }
        private PackageSubPageFilterBar packageSubPageFilterBar { get { return cache.Get<PackageSubPageFilterBar>("packageSubPageFilterBar"); } }
        internal PackageStatusBar packageStatusbar { get { return cache.Get<PackageStatusBar>("packageStatusBar"); } }
        private VisualElement leftColumnContainer { get { return cache.Get<VisualElement>("leftColumnContainer"); } }
        private VisualElement rightColumnContainer { get { return cache.Get<VisualElement>("rightColumnContainer"); } }
    }
}
