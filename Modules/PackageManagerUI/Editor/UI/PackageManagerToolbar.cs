// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System;
using UnityEngine;
using System.IO;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageManagerToolbar> {}

        static bool HasPackageInDevelopment => PackageDatabase.instance.allPackages.Any(p => p.versions.installed?.HasTag(PackageTag.InDevelopment) ?? false);

        private long m_SearchTextChangeTimestamp;

        private const long k_SearchEventDelayTicks = TimeSpan.TicksPerSecond / 3;

        public PackageManagerToolbar()
        {
            var root = Resources.GetTemplate("PackageManagerToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            SetupAddMenu();
            SetupFilterMenu();
            SetupAdvancedMenu();

            m_SearchTextChangeTimestamp = 0;
        }

        public void OnEnable()
        {
            SetFilter(PackageFiltering.instance.currentFilterTab);
            searchToolbar.SetValueWithoutNotify(PackageFiltering.instance.currentSearchText);
            searchToolbar.RegisterValueChangedCallback(OnSearchTextChanged);

            PackageDatabase.instance.onPackagesChanged += OnPackagesChanged;

            PackageFiltering.instance.onFilterTabChanged += SetFilter;
        }

        public void OnDisable()
        {
            searchToolbar.UnregisterValueChangedCallback(OnSearchTextChanged);
            PackageDatabase.instance.onPackagesChanged -= OnPackagesChanged;
            PackageFiltering.instance.onFilterTabChanged -= SetFilter;
        }

        public void FocusOnSearch()
        {
            searchToolbar.Focus();
            // Line below is required to make sure focus is in textfield
            searchToolbar.Q<TextField>()?.visualInput?.Focus();
        }

        private void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
        {
            // If nothing in the change list is related to `in development` packages
            // we can skip the whole database scan to save some time
            var changed = added.Concat(removed).Concat(preUpdate).Concat(postUpdate);
            if (!changed.Any(p => p.versions.installed?.HasTag(PackageTag.InDevelopment) ?? false))
                return;

            // If we have a filter set and no packages are in this filter, reset the filter to local packages.
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.InDevelopment && !HasPackageInDevelopment)
                SetFilter(PackageFilterTab.InProject);
        }

        internal void SetCurrentSearch(string text)
        {
            searchToolbar.SetValueWithoutNotify(text);
            PackageFiltering.instance.currentSearchText = text;
        }

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            m_SearchTextChangeTimestamp = DateTime.Now.Ticks;

            EditorApplication.update -= DelayedSearchEvent;
            EditorApplication.update += DelayedSearchEvent;
        }

        private void DelayedSearchEvent()
        {
            if (DateTime.Now.Ticks - m_SearchTextChangeTimestamp > k_SearchEventDelayTicks)
            {
                EditorApplication.update -= DelayedSearchEvent;
                PackageFiltering.instance.currentSearchText = searchToolbar.value;
                if (!string.IsNullOrEmpty(searchToolbar.value))
                    PackageManagerWindowAnalytics.SendEvent("search");
            }
        }

        private static string GetFilterDisplayName(PackageFilterTab filter)
        {
            switch (filter)
            {
                case PackageFilterTab.All:
                    return L10n.Tr("All packages");
                case PackageFilterTab.InProject:
                    return L10n.Tr("In Project");
                case PackageFilterTab.BuiltIn:
                    return L10n.Tr("Built-in packages");
                case PackageFilterTab.AssetStore:
                    return L10n.Tr("My Assets");
                case PackageFilterTab.InDevelopment:
                    return L10n.Tr("In Development");
                default:
                    return filter.ToString();
            }
        }

        public void SetFilter(PackageFilterTab filter)
        {
            PackageFiltering.instance.currentFilterTab = filter;
            filterMenu.text = GetFilterDisplayName(filter);
        }

        private void SetFilterFromMenu(PackageFilterTab filter)
        {
            SetFilter(filter);
            PackageManagerWindowAnalytics.SendEvent("changeFilter");
        }

        private void SetupAddMenu()
        {
            addMenu.menu.AppendAction("Add package from disk...", a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Select package on disk", "", new[] { "package.json file", "json" });
                if (Path.GetFileName(path) != "package.json")
                {
                    Debug.Log("Please select a valid package.json file in a package folder.");
                    return;
                }
                if (!string.IsNullOrEmpty(path) && !PackageDatabase.instance.isInstallOrUninstallInProgress)
                {
                    PackageDatabase.instance.InstallFromPath(Path.GetDirectoryName(path));
                    PackageManagerWindowAnalytics.SendEvent("addFromDisk");
                }
            }, a => DropdownMenuAction.Status.Normal);

            addMenu.menu.AppendAction("Add package from tarball...", a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Select package on disk", "", new[] { "Package tarball", "tgz" });
                if (!string.IsNullOrEmpty(path) && !PackageDatabase.instance.isInstallOrUninstallInProgress)
                {
                    PackageDatabase.instance.InstallFromPath(path);
                    PackageManagerWindowAnalytics.SendEvent("addFromTarball");
                }
            }, a => DropdownMenuAction.Status.Normal);

            addMenu.menu.AppendAction("Add package from git URL...", a =>
            {
                var addFromGitUrl = new PackagesAction("Add");
                addFromGitUrl.actionClicked += url =>
                {
                    addFromGitUrl.Hide();
                    if (!PackageDatabase.instance.isInstallOrUninstallInProgress)
                    {
                        PackageDatabase.instance.InstallFromUrl(url);
                        PackageManagerWindowAnalytics.SendEvent("addFromGitUrl");
                    }
                };

                parent.Add(addFromGitUrl);
                addFromGitUrl.Show();
            }, a => DropdownMenuAction.Status.Normal);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.MenuExtensions)
                    extension.OnAddMenuCreate(addMenu.menu);
            });
        }

        private void SetupFilterMenu()
        {
            filterMenu.menu.MenuItems().Clear();
            filterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.All), a =>
            {
                SetFilterFromMenu(PackageFilterTab.All);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.All ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.InProject), a =>
            {
                SetFilterFromMenu(PackageFilterTab.InProject);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.InProject ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.InDevelopment), a =>
            {
                SetFilterFromMenu(PackageFilterTab.InDevelopment);
            }, a =>
                {
                    if (!HasPackageInDevelopment)
                        return DropdownMenuAction.Status.Hidden;

                    return PackageFiltering.instance.currentFilterTab == PackageFilterTab.InDevelopment ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });

            filterMenu.menu.AppendSeparator();
            filterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.AssetStore), a =>
            {
                SetFilter(PackageFilterTab.AssetStore);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterMenu.menu.AppendSeparator();
            filterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.BuiltIn), a =>
            {
                SetFilterFromMenu(PackageFilterTab.BuiltIn);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.BuiltIn ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.MenuExtensions)
                    extension.OnFilterMenuCreate(filterMenu.menu);
            });
        }

        private void SetupAdvancedMenu()
        {
            advancedMenu.menu.AppendAction("Reset Packages to defaults", a =>
            {
                EditorApplication.ExecuteMenuItem(ApplicationUtil.k_ResetPackagesMenuPath);
                PageManager.instance.Refresh(RefreshOptions.UpmListOffline);
                PackageManagerWindowAnalytics.SendEvent("resetToDefaults");
            }, a => DropdownMenuAction.Status.Normal);

            advancedMenu.menu.AppendAction("Show dependencies", a =>
            {
                ToggleDependencies();
                PackageManagerWindowAnalytics.SendEvent("toggleDependencies");
            }, a => PackageManagerPrefs.instance.showPackageDependencies ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            advancedMenu.menu.AppendAction("Show preview packages", a =>
            {
                TogglePreviewPackages();
                PackageManagerWindowAnalytics.SendEvent("togglePreview");
            }, a => PackageManagerPrefs.instance.showPreviewPackages ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.MenuExtensions)
                    extension.OnAdvancedMenuCreate(advancedMenu.menu);
            });
        }

        private void ToggleDependencies()
        {
            PackageManagerPrefs.instance.showPackageDependencies = !PackageManagerPrefs.instance.showPackageDependencies;
        }

        private void TogglePreviewPackages()
        {
            var showPreviewPackages = PackageManagerPrefs.instance.showPreviewPackages;
            if (!showPreviewPackages && PackageManagerPrefs.instance.showPreviewPackagesWarning)
            {
                const string message = "Preview packages are not verified with Unity, may be unstable, and are unsupported in production. Are you sure you want to show preview packages?";
                if (!EditorUtility.DisplayDialog("Unity Package Manager", message, "Yes", "No"))
                    return;
                PackageManagerPrefs.instance.showPreviewPackagesWarning = false;
            }
            PackageManagerPrefs.instance.showPreviewPackages = !showPreviewPackages;
        }

        private VisualElementCache cache { get; set; }

        private ToolbarMenu addMenu { get { return cache.Get<ToolbarMenu>("toolbarAddMenu"); }}
        private ToolbarMenu filterMenu { get { return cache.Get<ToolbarMenu>("toolbarFilterMenu"); } }
        private ToolbarMenu advancedMenu { get { return cache.Get<ToolbarMenu>("toolbarAdvancedMenu"); } }
        private ToolbarSearchField searchToolbar { get { return cache.Get<ToolbarSearchField>("toolbarSearch"); } }
    }
}
