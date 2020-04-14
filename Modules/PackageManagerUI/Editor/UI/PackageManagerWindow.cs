// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting;
using UnityEditor.UIElements;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// PackageManager Window helper class
    /// </summary>
    public static class Window
    {
        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow(MenuCommand item)
        {
            Open(item.context?.name);
        }

        /// <summary>
        /// Open Package Manager Window and select specified package (if any)
        /// </summary>
        /// <param name="packageNameOrDisplayName">Id or display name of package to select, can be null</param>
        public static void Open(string packageNameOrDisplayName)
        {
            PackageManagerWindow.OpenPackageManager(packageNameOrDisplayName);
        }
    }

    [EditorWindowTitle(title = "Package Manager", icon = "Package Manager")]
    internal class PackageManagerWindow : EditorWindow
    {
        [NonSerialized]
        private string m_PackageToSelectOnLoaded;

        [NonSerialized]
        private PackageFilterTab? m_FilterToSelectAfterLoad;

        [SerializeField]
        private float m_SplitPaneLeftWidth;

        internal static PackageManagerWindow instance { get; private set; }

        private ResourceLoader m_ResourceLoader;
        private SelectionProxy m_Selection;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Selection = container.Resolve<SelectionProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
        }

        public void OnEnable()
        {
            this.SetAntiAliasing(4);
            if (instance == null) instance = this;
            if (instance != this)
                return;

            ResolveDependencies();

            instance.titleContent = GetLocalizedTitleContent();

            var root = m_ResourceLoader.GetTemplate("PackageManagerWindow.uxml");
            root.styleSheets.Add(m_ResourceLoader.GetMainWindowStyleSheet());
            cache = new VisualElementCache(root);

            rootVisualElement.Add(root);

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

            root.StretchToParentSize();

            var newTab = m_PackageManagerPrefs.lastUsedPackageFilter ?? m_PackageManagerPrefs.defaultFilterTab;
            m_PackageFiltering.SetCurrentFilterTabWithoutNotify(newTab);
            packageManagerToolbar.SetFilter(newTab);

            if (newTab != PackageFilterTab.AssetStore)
                UIUtils.SetElementDisplay(packageLoadBar, false);

            if (m_PageManager.GetRefreshTimestamp(newTab) == 0)
                DelayRefresh(newTab);

            if (newTab != PackageFilterTab.All && m_PageManager.GetRefreshTimestamp(PackageFilterTab.All) == 0)
                DelayRefresh(PackageFilterTab.All);

            mainContainer.leftWidth = m_SplitPaneLeftWidth;
            mainContainer.onSizeChanged += width => { m_SplitPaneLeftWidth = width; };

            EditorApplication.focusChanged += OnFocusChanged;
            m_Selection.onSelectionChanged += RefreshSelectedInInspectorClass;

            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RefreshSelectedInInspectorClass();
        }

        private void DelayRefresh(PackageFilterTab tab)
        {
            if (m_PackageManagerPrefs.numItemsPerPage == null)
            {
                EditorApplication.delayCall += () => DelayRefresh(tab);
                return;
            }

            m_PageManager.Refresh(tab, (int)m_PackageManagerPrefs.numItemsPerPage);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            rootVisualElement.RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            rootVisualElement.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
            packageList.Focus();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            rootVisualElement.UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            rootVisualElement.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
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
            if (instance == null) instance = this;
            if (instance != this)
                return;

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

            instance = null;
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
            {
                m_PackageDatabase.GetPackageAndVersionByIdOrName(m_PackageToSelectOnLoaded, out package, out version);
            }

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
                if (version != null)
                {
                    if (!m_PackageManagerPrefs.showPreviewPackages && version.HasTag(PackageTag.Preview))
                        m_PackageManagerPrefs.showPreviewPackages = true;
                }

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

        private void OnFocus()
        {
            rootVisualElement.AddToClassList("focus");

            if (cache == null)
                return;

            packageList.OnFocus();
        }

        private void OnLostFocus()
        {
            rootVisualElement.RemoveFromClassList("focus");
        }

        private void RefreshSelectedInInspectorClass()
        {
            if (m_Selection.activeObject is PackageSelectionObject)
                rootVisualElement.AddToClassList("selectedInInspector");
            else
                rootVisualElement.RemoveFromClassList("selectedInInspector");
        }

        private void OnRefreshOperationStart()
        {
            packageManagerToolbar.SetEnabled(false);
            packageDetails.packageToolbarContainer.SetEnabled(false);
        }

        private void OnRefreshOperationError(UIError error)
        {
            Debug.Log(string.Format(L10n.Tr("[PackageManager] Error {0}"), error.message));

            packageManagerToolbar.SetEnabled(true);
            packageDetails.packageToolbarContainer.SetEnabled(true);
        }

        [UsedByNativeCode]
        internal static void OpenURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            var startIndex = url.LastIndexOf('/');
            if (startIndex > 0)
            {
                var id = url.Substring(startIndex + 1);
                var endIndex = id.IndexOf('?');
                if (endIndex > 0)
                    id = id.Substring(0, endIndex);
                SelectPackageAndFilterStatic(id, PackageFilterTab.AssetStore);
            }
        }

        [UsedByNativeCode]
        internal static void OpenPackageManager(string packageNameOrDisplayName)
        {
            var isWindowAlreadyVisible = Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault() != null;

            SelectPackageAndFilterStatic(packageNameOrDisplayName);

            if (!isWindowAlreadyVisible)
            {
                string packageId = null;
                if (!string.IsNullOrEmpty(packageNameOrDisplayName))
                {
                    var packageDatabase = ServicesContainer.instance.Resolve<PackageDatabase>();
                    IPackageVersion version;
                    IPackage package;
                    packageDatabase.GetPackageAndVersionByIdOrName(packageNameOrDisplayName, out package, out version);

                    packageId = version?.uniqueId ?? package?.versions.primary.uniqueId ?? string.Format("{0}@primary", packageNameOrDisplayName);
                }
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
            }
        }

        [UsedByNativeCode]
        internal static void OnPackageManagerResolve()
        {
            // we don't want to refresh at all if window refresh hasn't happened yet
            var pageManager = ServicesContainer.instance.Resolve<PageManager>();
            if (pageManager.GetRefreshTimestamp(RefreshOptions.UpmList | RefreshOptions.UpmListOffline) == 0)
                return;
            var upmCache = ServicesContainer.instance.Resolve<UpmCache>();
            upmCache.SetInstalledPackageInfos(PackageInfo.GetAll());
        }

        internal static void SelectPackageAndFilterStatic(string packageIdOrDisplayName, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            instance = GetWindow<PackageManagerWindow>(typeof(SceneView));
            instance.minSize = new Vector2(800, 250);
            instance.SelectPackageAndFilter(packageIdOrDisplayName, filterTab, refresh, searchText);
        }

        private void SelectPackageAndFilter(string packageIdOrDisplayName, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            if (!string.IsNullOrEmpty(packageIdOrDisplayName) || filterTab != null)
            {
                if (filterTab == null)
                {
                    IPackageVersion version;
                    IPackage package;
                    m_PackageDatabase.GetPackageAndVersionByIdOrName(packageIdOrDisplayName, out package, out version);
                    filterTab = m_PageManager.FindTab(package, version);
                }

                m_FilterToSelectAfterLoad = filterTab;
                m_PackageToSelectOnLoaded = packageIdOrDisplayName;
                packageManagerToolbar.SetCurrentSearch(searchText);

                if (refresh || m_PackageDatabase.isEmpty)
                    DelayRefresh((PackageFilterTab)filterTab);
                else
                    SelectPackageAndFilter();
            }
            Show();
        }

        internal static void CloseAll()
        {
            var windows = Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows == null)
                return;

            foreach (var window in windows)
                window.Close();
        }

        [MenuItem("internal:Packages/Reset Package Database")]
        public static void ResetPackageDatabase()
        {
            var packageFiltering = ServicesContainer.instance.Resolve<PackageFiltering>();
            var packageManagerPrefs = ServicesContainer.instance.Resolve<PackageManagerPrefs>();
            var pageManager = ServicesContainer.instance.Resolve<PageManager>();

            pageManager.Reload();
            pageManager.Refresh(packageFiltering.currentFilterTab, packageManagerPrefs.numItemsPerPage ?? PageManager.k_DefaultPageSize);
        }

        private VisualElementCache cache;

        internal PackageList packageList { get { return cache.Get<PackageList>("packageList"); } }
        internal PackageLoadBar packageLoadBar { get { return cache.Get<PackageLoadBar>("packageLoadBar"); } }
        private PackageDetails packageDetails { get { return cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar packageManagerToolbar { get {return cache.Get<PackageManagerToolbar>("topMenuToolbar");} }
        private PackageStatusBar packageStatusbar { get {return cache.Get<PackageStatusBar>("packageStatusBar");} }
        private SplitView mainContainer { get {return cache.Get<SplitView>("mainContainer");} }
    }
}
