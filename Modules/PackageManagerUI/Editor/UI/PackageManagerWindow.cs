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

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindow : EditorWindow
    {
        [NonSerialized]
        private string m_PackageToSelectOnLoaded;

        [NonSerialized]
        private PackageFilterTab? m_FilterToSelectAfterLoad;

        public void OnEnable()
        {
            if (s_Root == null)
            {
                var windowResource = Resources.GetVisualTreeAsset("PackageManagerWindow.uxml");
                if (windowResource != null)
                {
                    s_Root = windowResource.CloneTree();
                    s_Root.styleSheets.Add(Resources.GetStyleSheet());
                    s_Cache = new VisualElementCache(s_Root);

                    PackageDatabase.instance.Setup();
                    SelectionManager.instance.Setup();

                    packageDetails.Setup();
                    packageList.Setup();
                    packageManagerToolbar.Setup();
                    packageStatusbar.Setup();

                    SetupDelayedPackageSelection();

                    PackageManagerWindowAnalytics.Setup();
                }
            }

            if (s_Root != null && s_Root.parent == null)
            {
                rootVisualElement.Add(s_Root);
                s_Root.StretchToParentSize();

                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    RefreshDatabase();

                PackageFiltering.instance.currentFilterTab = PackageManagerPrefs.instance.lastUsedPackageFilter ?? PackageFilterTab.All;
            }
        }

        private void RefreshDatabase()
        {
            // trigger both offline & offline refresh
            PackageDatabase.instance.Refresh(RefreshOptions.SearchAll | RefreshOptions.ListInstalled | RefreshOptions.OfflineMode);
            PackageDatabase.instance.Refresh(RefreshOptions.SearchAll | RefreshOptions.ListInstalled | RefreshOptions.Purchased);
        }

        public void OnDisable()
        {
            if (s_Root != null && rootVisualElement.Contains(s_Root))
                rootVisualElement.Remove(s_Root);

            PackageManagerPrefs.instance.lastUsedPackageFilter = PackageFiltering.instance.currentFilterTab;
        }

        private void OnDestroy()
        {
            foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                extension.OnWindowDestroy();
        }

        private void SetupDelayedPackageSelection()
        {
            packageManagerToolbar.SetEnabled(!PackageDatabase.instance.isEmpty);
            packageList.onPackagesLoaded += SelectPackageAndFilter;
        }

        private void SelectPackageAndFilter()
        {
            packageManagerToolbar.SetEnabled(true);

            if (m_FilterToSelectAfterLoad != null)
            {
                PackageFiltering.instance.currentFilterTab = (PackageFilterTab)m_FilterToSelectAfterLoad;
            }

            if (!string.IsNullOrEmpty(m_PackageToSelectOnLoaded))
            {
                var package = PackageDatabase.instance.GetPackage(m_PackageToSelectOnLoaded)
                    ?? PackageDatabase.instance.GetPackageByDisplayName(m_PackageToSelectOnLoaded);
                if (package != null)
                {
                    if (m_FilterToSelectAfterLoad == null)
                    {
                        var newFilterTab = PackageFilterTab.All;
                        if (package.versions.Any(v => v.HasTag(PackageTag.BuiltIn)))
                            newFilterTab = PackageFilterTab.Modules;
                        else
                        {
                            var installdVersion = package.installedVersion;
                            if (installdVersion != null && installdVersion.isDirectDependency)
                                newFilterTab = PackageFilterTab.Local;
                        }
                        PackageFiltering.instance.currentFilterTab = newFilterTab;
                    }

                    SelectionManager.instance.SetSelected(package);
                }
            }

            m_FilterToSelectAfterLoad = null;
            m_PackageToSelectOnLoaded = null;
        }

        private static VisualElement s_Root;

        private static VisualElementCache s_Cache;

        private PackageList packageList { get { return s_Cache.Get<PackageList>("packageList"); } }
        private PackageDetails packageDetails { get { return s_Cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar packageManagerToolbar { get {return s_Cache.Get<PackageManagerToolbar>("topMenuToolbar");} }
        private PackageStatusBar packageStatusbar { get {return s_Cache.Get<PackageStatusBar>("packageStatusBar");} }
        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow(MenuCommand item)
        {
            OpenPackageManager(item.context?.name);
        }

        [UsedByNativeCode]
        internal static void OpenPackageManager(string packageNameOrDisplayName)
        {
            var window = GetWindowDontShow<PackageManagerWindow>();
            var isWindowAlreadyVisible = window != null && window.m_Parent != null;

            SelectPackageAndFilter(packageNameOrDisplayName);

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
        }

        internal static void SelectPackageAndFilter(string packageIdOrDisplayName, PackageFilterTab? filterTab = null, bool refresh = false)
        {
            var window = GetWindow<PackageManagerWindow>(false, "Packages", true);
            window.minSize = new Vector2(700, 250);
            if (!string.IsNullOrEmpty(packageIdOrDisplayName))
            {
                window.m_PackageToSelectOnLoaded = packageIdOrDisplayName;
                window.m_FilterToSelectAfterLoad = filterTab;
                if (!PackageDatabase.instance.isEmpty && !refresh)
                {
                    window.SelectPackageAndFilter();
                }
                else if (refresh)
                {
                    window.RefreshDatabase();
                }
            }
            window.Show();
        }
    }
}
