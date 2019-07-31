// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindow : EditorWindow
    {
        [NonSerialized]
        private string m_PackageToSelectOnLoaded;

        [NonSerialized]
        private PackageFilterTab? m_FilterToSelectAfterLoad;

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

                PackageDatabase.instance.onRefreshOperationFinish += OnRefreshOperationFinish;
                PackageDatabase.instance.onRefreshOperationStart += OnRefreshOperationStart;
                PackageDatabase.instance.onRefreshOperationError += OnRefreshOperationError;

                PackageManagerWindowAnalytics.Setup();

                rootVisualElement.Add(root);
                root.StretchToParentSize();

                PackageFiltering.instance.currentFilterTab = PackageManagerPrefs.instance.lastUsedPackageFilter ?? PackageFilterTab.All;

                if (!PageManager.instance.HasFetchedPageForFilterTab(PackageFiltering.instance.currentFilterTab))
                    PageManager.instance.Refresh(RefreshOptions.CurrentFilter);

                if (PackageFiltering.instance.currentFilterTab != PackageFilterTab.All && !PageManager.instance.HasFetchedPageForFilterTab(PackageFilterTab.All))
                    PageManager.instance.Refresh(RefreshOptions.All);

                EditorApplication.focusChanged += OnFocusChanged;
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

            PackageDatabase.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;
            PackageDatabase.instance.onRefreshOperationStart -= OnRefreshOperationStart;
            PackageDatabase.instance.onRefreshOperationError -= OnRefreshOperationError;

            packageDetails.OnDisable();
            packageList.OnDisable();
            packageManagerToolbar.OnDisable();
            packageStatusbar.OnDisable();

            PageManager.instance.Clear();

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

        private void OnRefreshOperationFinish(PackageFilterTab tab)
        {
            packageManagerToolbar.SetEnabled(true);
            packageDetails.packageToolbarContainer.SetEnabled(true);

            if (m_FilterToSelectAfterLoad != null)
            {
                PackageFiltering.instance.currentFilterTab = (PackageFilterTab)m_FilterToSelectAfterLoad;
            }

            if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded) && (m_FilterToSelectAfterLoad == null || tab == m_FilterToSelectAfterLoad))
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
                            newFilterTab = PackageFilterTab.Modules;
                        else
                        {
                            var installedVersion = package.installedVersion;
                            if (installedVersion != null && installedVersion.isDirectDependency)
                                newFilterTab = PackageFilterTab.Local;
                        }
                        PackageFiltering.instance.currentFilterTab = newFilterTab;
                    }

                    PageManager.instance.SetSelected(package);

                    m_FilterToSelectAfterLoad = null;
                    m_PackageToSelectOnLoaded = null;
                }
            }
            else if (string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
            {
                m_FilterToSelectAfterLoad = null;
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

        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow(MenuCommand item)
        {
            OpenPackageManager(item.context?.name);
        }

        [UsedByNativeCode]
        internal static void OpenURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            var lastIndex = url.LastIndexOf('/');
            if (lastIndex > 0)
            {
                SelectPackageAndFilter(url.Substring(lastIndex + 1), PackageFilterTab.AssetStore);
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
                if (!PackageDatabase.instance.isEmpty && !refresh)
                {
                    s_Window.OnRefreshOperationFinish(filterTab ?? PackageFilterTab.All);
                }
                else if (refresh)
                {
                    PageManager.instance.Refresh(filterTab ?? PackageFilterTab.All);
                }
            }
            s_Window.Show();
        }

        private VisualElementCache cache;

        private PackageList packageList { get { return cache.Get<PackageList>("packageList"); } }
        private PackageDetails packageDetails { get { return cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar packageManagerToolbar { get {return cache.Get<PackageManagerToolbar>("topMenuToolbar");} }
        private PackageStatusBar packageStatusbar { get {return cache.Get<PackageStatusBar>("packageStatusBar");} }
    }
}
