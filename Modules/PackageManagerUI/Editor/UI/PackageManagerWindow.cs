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
                root.styleSheets.Add(Resources.GetStyleSheet());
                cache = new VisualElementCache(root);

                PageManager.instance.Setup();

                packageDetails.OnEnable();
                packageList.OnEnable();
                packageManagerToolbar.OnEnable();
                packageStatusbar.OnEnable();

                packageManagerToolbar.SetEnabled(!PackageDatabase.instance.isEmpty);
                packageDetails.packageToolbarContainer.SetEnabled(!PackageDatabase.instance.isEmpty);

                PageManager.instance.onRefreshOperationFinish += OnRefreshOperationFinish;
                PageManager.instance.onRefreshOperationStart += OnRefreshOperationStart;
                PageManager.instance.onRefreshOperationError += OnRefreshOperationError;

                PackageManagerWindowAnalytics.Setup();

                rootVisualElement.Add(root);
                root.StretchToParentSize();

                var newTab = PackageManagerPrefs.instance.lastUsedPackageFilter ?? PackageFilterTab.All;
                PackageFiltering.instance.currentFilterTab = newTab;

                if (PageManager.instance.GetRefreshTimestamp(newTab) == 0)
                    PageManager.instance.Refresh(newTab);

                if (newTab != PackageFilterTab.All && PageManager.instance.GetRefreshTimestamp(PackageFilterTab.All) == 0)
                    PageManager.instance.Refresh(PackageFilterTab.All);

                mainContainer.leftWidth = m_SplitPaneLeftWidth;
                mainContainer.onSizeChanged += width => { m_SplitPaneLeftWidth = width; };

                EditorApplication.focusChanged += OnFocusChanged;
                Selection.selectionChanged += OnEditorSelectionChanged;

                rootVisualElement.focusable = true;
                rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }
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
                PageManager.instance.Refresh(RefreshOptions.PurchasedOffline);
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

            packageDetails.OnDisable();
            packageList.OnDisable();
            packageManagerToolbar.OnDisable();
            packageStatusbar.OnDisable();

            EditorApplication.focusChanged -= OnFocusChanged;
            Selection.selectionChanged -= OnEditorSelectionChanged;
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

        private void SelectPackageAndFilter()
        {
            IPackageVersion version = null;
            IPackage package = null;
            if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
            {
                var packageUniqueId = m_PackageToSelectOnLoaded.Split('@')[0];

                PackageDatabase.instance.GetPackageAndVersion(packageUniqueId, m_PackageToSelectOnLoaded, out package, out version);
                if (package == null)
                    package = PackageDatabase.instance.GetPackage(m_PackageToSelectOnLoaded) ?? PackageDatabase.instance.GetPackageByDisplayName(m_PackageToSelectOnLoaded);
            }

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
                    PageManager.instance.GetPage(tab).SetSelected(package, version);

                m_FilterToSelectAfterLoad = null;
                m_PackageToSelectOnLoaded = null;
            }
        }

        private void OnFocus()
        {
            packageList.AddToClassList("focus");
            packageDetails.AddToClassList("focus");
            packageList.OnFocus();
        }

        private void OnLostFocus()
        {
            packageList.RemoveFromClassList("focus");
            packageDetails.RemoveFromClassList("focus");
        }

        private void OnEditorSelectionChanged()
        {
            if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded) || m_FilterToSelectAfterLoad != null)
                return;

            if (Selection.activeObject is PackageSelectionObject)
            {
                var packageSelectionObject = (PackageSelectionObject)Selection.activeObject;
                IPackage package;
                IPackageVersion version;

                PackageDatabase.instance.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out package, out version);
                if (package == null || version == null)
                    return;

                var tab = PageManager.instance.FindTab(packageSelectionObject.versionUniqueId);
                PackageFiltering.instance.currentFilterTab = tab;
                PageManager.instance.GetPage(tab).SetSelected(package, version);
            }
        }

        private void OnRefreshOperationStart()
        {
            packageManagerToolbar.SetEnabled(false);
            packageDetails.packageToolbarContainer.SetEnabled(false);
        }

        private void OnRefreshOperationError(Error error)
        {
            Debug.Log("[PackageManager] Error " + error.message);

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
                SelectPackageAndFilter(id, PackageFilterTab.AssetStore);
            }
        }

        [UsedByNativeCode]
        internal static void OpenPackageManager(string packageNameOrDisplayName)
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            var isWindowAlreadyVisible = windows != null;
            if (!isWindowAlreadyVisible)
            {
                string packageId = null;
                if (!string.IsNullOrEmpty(packageNameOrDisplayName))
                {
                    var packageUniqueId = packageNameOrDisplayName.Split('@')[0];

                    IPackageVersion version;
                    IPackage package;
                    PackageDatabase.instance.GetPackageAndVersion(packageUniqueId, packageNameOrDisplayName, out package, out version);
                    if (package == null)
                        package = PackageDatabase.instance.GetPackage(packageNameOrDisplayName) ?? PackageDatabase.instance.GetPackageByDisplayName(packageNameOrDisplayName);

                    packageId = version?.uniqueId ?? package?.versions.primary.uniqueId ?? $"{packageNameOrDisplayName}@primary";
                }
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
            }

            SelectPackageAndFilter(packageNameOrDisplayName);
        }

        internal static void SelectPackageAndFilter(string packageIdOrDisplayName, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            instance = GetWindow<PackageManagerWindow>();
            instance.minSize = new Vector2(700, 250);
            if (!string.IsNullOrEmpty(packageIdOrDisplayName) || filterTab != null)
            {
                if (filterTab == null)
                    filterTab = PageManager.instance.FindTab(packageIdOrDisplayName);

                instance.m_FilterToSelectAfterLoad = filterTab;
                instance.m_PackageToSelectOnLoaded = packageIdOrDisplayName;
                instance.packageManagerToolbar.SetCurrentSearch(searchText);

                if (refresh || PackageDatabase.instance.isEmpty)
                    PageManager.instance.Refresh(filterTab);
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

        private VisualElementCache cache;

        private PackageList packageList { get { return cache.Get<PackageList>("packageList"); } }
        private PackageDetails packageDetails { get { return cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar packageManagerToolbar { get {return cache.Get<PackageManagerToolbar>("topMenuToolbar");} }
        private PackageStatusBar packageStatusbar { get {return cache.Get<PackageStatusBar>("packageStatusBar");} }
        private SplitView mainContainer { get {return cache.Get<SplitView>("mainContainer");} }
    }
}
