// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting;
using UnityEditor.UIElements;
using System.Collections.Generic;

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

        public void OnEnable()
        {
            this.SetAntiAliasing(4);
            if (instance == null) instance = this;
            if (instance != this)
                return;

            instance.titleContent = GetLocalizedTitleContent();

            var windowResource = Resources.GetVisualTreeAsset("PackageManagerWindow.uxml");
            if (windowResource != null)
            {
                var root = windowResource.Instantiate();
                root.styleSheets.Add(Resources.GetMainWindowStyleSheet());
                cache = new VisualElementCache(root);

                rootVisualElement.Add(root);
                LocalizeVisualElementAndAllChildrenText(rootVisualElement);

                PageManager.instance.Setup();

                packageDetails.OnEnable();
                packageList.OnEnable();
                packageLoadBar.OnEnable();
                packageManagerToolbar.OnEnable();
                packageStatusbar.OnEnable();

                packageManagerToolbar.SetEnabled(!PackageDatabase.instance.isEmpty);
                packageDetails.packageToolbarContainer.SetEnabled(!PackageDatabase.instance.isEmpty);

                PageManager.instance.onRefreshOperationFinish += OnRefreshOperationFinish;
                PageManager.instance.onRefreshOperationStart += OnRefreshOperationStart;
                PageManager.instance.onRefreshOperationError += OnRefreshOperationError;
                PackageFiltering.instance.onFilterTabChanged += OnFilterChanged;

                PackageManagerWindowAnalytics.Setup();

                root.StretchToParentSize();

                var newTab = PackageManagerPrefs.instance.lastUsedPackageFilter ?? PackageFiltering.instance.defaultFilterTab;
                PackageFiltering.instance.SetCurrentFilterTabWithoutNotify(newTab);
                packageManagerToolbar.SetFilter(newTab);

                if (newTab != PackageFilterTab.AssetStore)
                    UIUtils.SetElementDisplay(packageLoadBar, false);

                if (PageManager.instance.GetRefreshTimestamp(newTab) == 0)
                    DelayRefresh(newTab);

                if (newTab != PackageFilterTab.All && PageManager.instance.GetRefreshTimestamp(PackageFilterTab.All) == 0)
                    DelayRefresh(PackageFilterTab.All);

                mainContainer.leftWidth = m_SplitPaneLeftWidth;
                mainContainer.onSizeChanged += width => { m_SplitPaneLeftWidth = width; };

                EditorApplication.focusChanged += OnFocusChanged;
                ApplicationUtil.instance.onEditorSelectionChanged += RefreshSelectedInInspectorClass;

                rootVisualElement.focusable = true;
                rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

                RefreshSelectedInInspectorClass();
            }
        }

        private void DelayRefresh(PackageFilterTab tab)
        {
            var numberOfPackages = packageList.CalculateNumberOfPackagesToDisplay();
            if (numberOfPackages == 0)
            {
                EditorApplication.delayCall += () => DelayRefresh(tab);
                return;
            }

            PageManager.instance.Refresh(tab, numberOfPackages);
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
            if (focus && canRefresh && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                PageManager.instance.Refresh(RefreshOptions.PurchasedOffline, packageList?.CalculateNumberOfPackagesToDisplay() ?? PageManager.k_DefaultPageSize);
        }

        public void OnDisable()
        {
            if (instance == null) instance = this;
            if (instance != this)
                return;

            PackageManagerPrefs.instance.lastUsedPackageFilter = PackageFiltering.instance.currentFilterTab;

            PageManager.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;
            PageManager.instance.onRefreshOperationStart -= OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationError -= OnRefreshOperationError;
            PackageFiltering.instance.onFilterTabChanged -= OnFilterChanged;

            packageDetails.OnDisable();
            packageList.OnDisable();
            packageLoadBar.OnDisable();
            packageManagerToolbar.OnDisable();
            packageStatusbar.OnDisable();

            EditorApplication.focusChanged -= OnFocusChanged;
            ApplicationUtil.instance.onEditorSelectionChanged -= RefreshSelectedInInspectorClass;
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

            if (m_FilterToSelectAfterLoad != null && PageManager.instance.GetRefreshTimestamp(m_FilterToSelectAfterLoad) > 0)
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
                PackageDatabase.instance.GetPackageAndVersionByIdOrName(m_PackageToSelectOnLoaded, out package, out version);

            if (m_FilterToSelectAfterLoad == PackageFilterTab.AssetStore)
            {
                PackageFiltering.instance.currentFilterTab = PackageFilterTab.AssetStore;

                if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
                {
                    if (package == null || package is PlaceholderPackage)
                        PageManager.instance.Fetch(m_PackageToSelectOnLoaded);
                    else
                        PageManager.instance.GetPage(PackageFilterTab.AssetStore).Load(package, version);
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
                    if (!PackageManagerPrefs.instance.showPreviewPackages && version.HasTag(PackageTag.Preview))
                        PackageManagerPrefs.instance.showPreviewPackages = true;
                }

                PackageFiltering.instance.currentFilterTab = tab;
                if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
                {
                    PageManager.instance.SetSelected(package, version, true);
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
            if (ApplicationUtil.instance.activeSelection is PackageSelectionObject)
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
            Debug.Log(string.Format(ApplicationUtil.instance.GetTranslationForText("[PackageManager] Error {0}"), error.message));

            packageManagerToolbar.SetEnabled(true);
            packageDetails.packageToolbarContainer.SetEnabled(true);
        }

        /// <summary>
        /// Traverses the entire VisualElement tree from the specified root and localizes any text found to
        ///  be part of Labels or Buttons.
        /// Do not call this function at any point after the list of packages is loaded, as we do not
        ///  want the dynamic text in the loaded package names/info to be localized by this mechanism.
        /// </summary>
        private void LocalizeVisualElementAndAllChildrenText(VisualElement root)
        {
            root.Query<TextElement>().ForEach((textElement) => {
                ApplicationUtil.instance.TranslateTextElement(textElement);
            });
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
                SelectPackageAndFilter(id, PackageFilterTab.AssetStore);
            }
        }

        [UsedByNativeCode]
        internal static void OpenPackageManager(string packageNameOrDisplayName)
        {
            var window = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault();
            var isWindowAlreadyVisible = window != null;
            if (!isWindowAlreadyVisible)
            {
                string packageId = null;
                if (!string.IsNullOrEmpty(packageNameOrDisplayName))
                {
                    IPackageVersion version;
                    IPackage package;
                    PackageDatabase.instance.GetPackageAndVersionByIdOrName(packageNameOrDisplayName, out package, out version);

                    packageId = version?.uniqueId ?? package?.versions.primary.uniqueId ?? string.Format(ApplicationUtil.instance.GetTranslationForText("{0}@primary"), packageNameOrDisplayName);
                }
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
            }

            SelectPackageAndFilter(packageNameOrDisplayName);
        }

        [UsedByNativeCode]
        internal static void OnPackageManagerResolve()
        {
            // we don't want to refresh at all if page manager hasn't been initialized before
            if (!PageManager.instance.isInitialized)
                return;

            UpmCache.instance.SetInstalledPackageInfos(PackageInfo.GetAll());
        }

        internal static void SelectPackageAndFilter(string packageIdOrDisplayName, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            instance = GetWindow<PackageManagerWindow>(typeof(SceneView));
            instance.minSize = new Vector2(800, 250);
            if (!string.IsNullOrEmpty(packageIdOrDisplayName) || filterTab != null)
            {
                if (filterTab == null)
                {
                    IPackageVersion version;
                    IPackage package;
                    PackageDatabase.instance.GetPackageAndVersionByIdOrName(packageIdOrDisplayName, out package, out version);
                    filterTab = PageManager.instance.FindTab(package, version);
                }

                instance.m_FilterToSelectAfterLoad = filterTab;
                instance.m_PackageToSelectOnLoaded = packageIdOrDisplayName;
                instance.packageManagerToolbar.SetCurrentSearch(searchText);

                if (refresh || PackageDatabase.instance.isEmpty)
                    instance.DelayRefresh((PackageFilterTab)filterTab);
                else
                    instance.SelectPackageAndFilter();
            }
            instance.Show();
        }

        internal static void CloseAll()
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows == null)
                return;

            foreach (var window in windows)
                window.Close();
        }

        [MenuItem("internal:Packages/Reset Package Database")]
        public static void ResetPackageDatabase()
        {
            PageManager.instance.Reload();
            PageManager.instance.Refresh(PackageFiltering.instance.currentFilterTab, instance?.packageList?.CalculateNumberOfPackagesToDisplay() ?? PageManager.k_DefaultPageSize);
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
