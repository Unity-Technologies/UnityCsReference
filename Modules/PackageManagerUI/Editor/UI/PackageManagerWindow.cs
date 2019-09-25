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

    internal class PackageManagerWindow : EditorWindow
    {
        [NonSerialized]
        private string m_PackageToSelectOnLoaded;

        [NonSerialized]
        private PackageFilterTab? m_FilterToSelectAfterLoad;

        [SerializeField]
        private float m_SplitPaneLeftWidth;

        private static PackageManagerWindow s_Window;

        public void OnEnable()
        {
            this.SetAntiAliasing(4);
            if (s_Window == null) s_Window = this;
            if (s_Window != this)
                return;

            var windowResource = Resources.GetVisualTreeAsset("PackageManagerWindow.uxml");
            if (windowResource != null)
            {
                var root = windowResource.CloneTree();
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
            if (s_Window == null) s_Window = this;
            if (s_Window != this)
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
        }

        public void OnDestroy()
        {
            if (s_Window == null) s_Window = this;
            if (s_Window != this)
                return;

            foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                extension.OnWindowDestroy();

            s_Window = null;
        }

        private void OnRefreshOperationFinish()
        {
            packageManagerToolbar.SetEnabled(true);
            packageDetails.packageToolbarContainer.SetEnabled(true);
            SelectPackageAndFilter();
        }

        private void SelectPackageAndFilter()
        {
            if (m_FilterToSelectAfterLoad != null)
            {
                PackageFiltering.instance.currentFilterTab = (PackageFilterTab)m_FilterToSelectAfterLoad;
            }

            if (string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
            {
                m_FilterToSelectAfterLoad = null;
            }
            else
            {
                var package = PackageDatabase.instance.GetPackage(m_PackageToSelectOnLoaded)
                    ?? PackageDatabase.instance.GetPackageByDisplayName(m_PackageToSelectOnLoaded);

                if (m_FilterToSelectAfterLoad == PackageFilterTab.AssetStore && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                {
                    if (package == null || package is PlaceholderPackage)
                        PageManager.instance.Fetch(m_PackageToSelectOnLoaded);
                    else
                        PageManager.instance.GetCurrentPage().Load(package);

                    m_FilterToSelectAfterLoad = null;
                    m_PackageToSelectOnLoaded = null;
                    return;
                }

                if (package != null)
                {
                    if (m_FilterToSelectAfterLoad == null)
                    {
                        var newFilterTab = PackageFilterTab.All;
                        if (package.versions.Any(v => v.HasTag(PackageTag.BuiltIn)))
                            newFilterTab = PackageFilterTab.BuiltIn;
                        else
                        {
                            var installedVersion = package.installedVersion;
                            if (installedVersion != null && installedVersion.isDirectDependency)
                                newFilterTab = PackageFilterTab.InProject;
                        }
                        PackageFiltering.instance.currentFilterTab = newFilterTab;
                    }

                    PageManager.instance.SetSelected(package);

                    m_FilterToSelectAfterLoad = null;
                    m_PackageToSelectOnLoaded = null;
                }
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
                    var package = PackageDatabase.instance.GetPackage(packageNameOrDisplayName)
                        ?? PackageDatabase.instance.GetPackageByDisplayName(packageNameOrDisplayName);
                    packageId = package?.primaryVersion.uniqueId ?? $"{packageNameOrDisplayName}@primary";
                }
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
            }

            SelectPackageAndFilter(packageNameOrDisplayName);
        }

        internal static void SelectPackageAndFilter(string packageIdOrDisplayName, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            s_Window = GetWindow<PackageManagerWindow>();
            s_Window.titleContent = new GUIContent("Package Manager");
            s_Window.minSize = new Vector2(700, 250);
            if (!string.IsNullOrEmpty(packageIdOrDisplayName))
            {
                s_Window.packageManagerToolbar.SetCurrentSearch(searchText);
                s_Window.m_PackageToSelectOnLoaded = packageIdOrDisplayName;
                s_Window.m_FilterToSelectAfterLoad = filterTab;
                if (refresh)
                {
                    PageManager.instance.Refresh(filterTab ?? PackageFilterTab.All);
                }
                else if (!PackageDatabase.instance.isEmpty)
                {
                    s_Window.SelectPackageAndFilter();
                }
            }
            s_Window.Show();
        }

        private VisualElementCache cache;

        private PackageList packageList { get { return cache.Get<PackageList>("packageList"); } }
        private PackageDetails packageDetails { get { return cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar packageManagerToolbar { get {return cache.Get<PackageManagerToolbar>("topMenuToolbar");} }
        private PackageStatusBar packageStatusbar { get {return cache.Get<PackageStatusBar>("packageStatusBar");} }
        private SplitView mainContainer { get {return cache.Get<SplitView>("mainContainer");} }
    }
}
