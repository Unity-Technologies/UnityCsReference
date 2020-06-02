// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindowRoot : VisualElement
    {
        [NonSerialized]
        private string m_PackageToSelectOnLoaded;

        [NonSerialized]
        private PackageFilterTab? m_FilterToSelectAfterLoad;

        private ResourceLoader m_ResourceLoader;
        private SelectionProxy m_Selection;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private UnityConnectProxy m_UnityConnectProxy;
        private void ResolveDependencies(ResourceLoader resourceLoader,
            SelectionProxy selection,
            PackageFiltering packageFiltering,
            PackageManagerPrefs packageManagerPrefs,
            PackageDatabase packageDatabase,
            PageManager pageManager,
            PackageManagerProjectSettingsProxy settingsProxy,
            UnityConnectProxy unityConnectProxy)
        {
            m_ResourceLoader = resourceLoader;
            m_Selection = selection;
            m_PackageFiltering = packageFiltering;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_UnityConnectProxy = unityConnectProxy;
        }

        public PackageManagerWindowRoot(ResourceLoader resourceLoader,
                                        SelectionProxy selection,
                                        PackageFiltering packageFiltering,
                                        PackageManagerPrefs packageManagerPrefs,
                                        PackageDatabase packageDatabase,
                                        PageManager pageManager,
                                        PackageManagerProjectSettingsProxy settingsProxy,
                                        UnityConnectProxy unityConnectProxy)
        {
            ResolveDependencies(resourceLoader, selection, packageFiltering, packageManagerPrefs, packageDatabase, pageManager, settingsProxy, unityConnectProxy);

            styleSheets.Add(m_ResourceLoader.GetMainWindowStyleSheet());

            var root = m_ResourceLoader.GetTemplate("PackageManagerWindow.uxml");
            Add(root);
            cache = new VisualElementCache(root);
        }

        public void OnEnable()
        {
            packageDetails.OnEnable();
            packageList.OnEnable();
            packageLoadBar.OnEnable();
            packageManagerToolbar.OnEnable();
            packageStatusbar.OnEnable();

            packageManagerToolbar.SetEnabled(!m_PackageDatabase.isEmpty);
            packageDetails.packageToolbarContainer.SetEnabled(!m_PackageDatabase.isEmpty);

            m_PageManager.onRefreshOperationFinish += OnRefreshOperationFinish;
            m_PageManager.onRefreshOperationStart += OnRefreshOperationStart;
            m_PageManager.onRefreshOperationError += OnRefreshOperationError;
            m_PackageFiltering.onFilterTabChanged += OnFilterChanged;

            PackageManagerWindowAnalytics.Setup();

            var newTab = m_PackageManagerPrefs.lastUsedPackageFilter ?? m_PackageManagerPrefs.defaultFilterTab;
            m_PackageFiltering.SetCurrentFilterTabWithoutNotify(newTab);
            packageManagerToolbar.SetFilter(newTab);

            if (newTab != PackageFilterTab.AssetStore)
                UIUtils.SetElementDisplay(packageLoadBar, false);

            if (m_PageManager.GetRefreshTimestamp(newTab) == 0)
                DelayRefresh(newTab);

            if (newTab != PackageFilterTab.All && m_PageManager.GetRefreshTimestamp(PackageFilterTab.All) == 0)
                DelayRefresh(PackageFilterTab.All);

            EditorApplication.focusChanged += OnFocusChanged;
            m_Selection.onSelectionChanged += RefreshSelectedInInspectorClass;

            focusable = true;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RefreshSelectedInInspectorClass();
        }

        private void DelayRefresh(PackageFilterTab tab)
        {
            if (m_PackageManagerPrefs.numItemsPerPage == null ||
                tab == PackageFilterTab.AssetStore && !m_UnityConnectProxy.isUserInfoReady)
            {
                EditorApplication.delayCall += () => DelayRefresh(tab);
                return;
            }

            m_PageManager.Refresh(tab, (int)m_PackageManagerPrefs.numItemsPerPage);
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
                m_PageManager.Refresh(RefreshOptions.PurchasedOffline, m_PackageManagerPrefs.numItemsPerPage ?? PageManager.k_DefaultPageSize);
        }

        public void OnDisable()
        {
            m_PackageManagerPrefs.lastUsedPackageFilter = m_PackageFiltering.currentFilterTab;

            m_PageManager.onRefreshOperationFinish -= OnRefreshOperationFinish;
            m_PageManager.onRefreshOperationStart -= OnRefreshOperationStart;
            m_PageManager.onRefreshOperationError -= OnRefreshOperationError;
            m_PackageFiltering.onFilterTabChanged -= OnFilterChanged;

            packageDetails.OnDisable();
            packageList.OnDisable();
            packageLoadBar.OnDisable();
            packageManagerToolbar.OnDisable();
            packageStatusbar.OnDisable();

            EditorApplication.focusChanged -= OnFocusChanged;
            m_Selection.onSelectionChanged -= RefreshSelectedInInspectorClass;
        }

        public void OnDestroy()
        {
            foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                extension.OnWindowDestroy();
        }

        private void OnRefreshOperationFinish()
        {
            packageManagerToolbar.SetEnabled(true);
            packageDetails.packageToolbarContainer.SetEnabled(true);

            if (m_FilterToSelectAfterLoad != null && m_PageManager.GetRefreshTimestamp(m_FilterToSelectAfterLoad) > 0)
                SelectPackageAndFilter();
        }

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            if (!filterTab.Equals(PackageFilterTab.AssetStore))
                UIUtils.SetElementDisplay(packageLoadBar, false);
            else
            {
                packageLoadBar.Refresh();
                UIUtils.SetElementDisplay(packageLoadBar, true);
            }
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
                return;
            }

            if (package != null || m_FilterToSelectAfterLoad != null)
            {
                var tab = m_FilterToSelectAfterLoad ?? PackageFilterTab.All;

                m_PackageFiltering.currentFilterTab = tab;
                if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
                {
                    m_PageManager.SetSelected(package, version, true);
                    packageList.OnFocus();
                }

                m_FilterToSelectAfterLoad = null;
                m_PackageToSelectOnLoaded = null;
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

        private void OnRefreshOperationStart()
        {
            packageManagerToolbar.SetEnabled(false);
            packageDetails.packageToolbarContainer.SetEnabled(false);
        }

        private void OnRefreshOperationError(UIError error)
        {
            packageManagerToolbar.SetEnabled(true);
            packageDetails.packageToolbarContainer.SetEnabled(true);
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
                        if (!m_SettingsProxy.enablePreviewPackages && semVersion.HasValue && (semVersion.Value.Major == 0 || semVersion.Value.Prerelease.StartsWith("preview")))
                        {
                            Debug.Log("You must check \"Enable Preview Packages\" in Project Settings > Package Manager in order to see this package.");
                            filterTab = m_PackageFiltering.currentFilterTab;
                            packageToSelect = null;
                        }
                        else
                            filterTab = PackageFilterTab.All;
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

        private VisualElementCache cache { set; get; }
        internal PackageList packageList { get { return cache.Get<PackageList>("packageList"); } }
        internal PackageLoadBar packageLoadBar { get { return cache.Get<PackageLoadBar>("packageLoadBar"); } }
        private PackageDetails packageDetails { get { return cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar packageManagerToolbar { get { return cache.Get<PackageManagerToolbar>("topMenuToolbar"); } }
        private PackageStatusBar packageStatusbar { get { return cache.Get<PackageStatusBar>("packageStatusBar"); } }
        private VisualSplitter mainContainer { get { return cache.Get<VisualSplitter>("mainContainer"); } }
    }
}
