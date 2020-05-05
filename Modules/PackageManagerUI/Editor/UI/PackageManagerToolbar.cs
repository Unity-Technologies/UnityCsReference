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

        private static readonly string k_Ascending = "↓";
        private static readonly string k_Descending = "↑";

        public PackageManagerToolbar()
        {
            var root = Resources.GetTemplate("PackageManagerToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            SetupAddMenu();
            SetupFilterTabsMenu();
            SetupOrdering();
            SetupFilters();
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
            ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
            ApplicationUtil.instance.onInternetReachabilityChange += OnInternetReachabilityChange;
        }

        public void OnDisable()
        {
            searchToolbar.UnregisterValueChangedCallback(OnSearchTextChanged);
            PackageDatabase.instance.onPackagesChanged -= OnPackagesChanged;
            PackageFiltering.instance.onFilterTabChanged -= SetFilter;
            ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
            ApplicationUtil.instance.onInternetReachabilityChange -= OnInternetReachabilityChange;
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
        }

        private void OnUserLoginStateChange(bool loggedIn)
        {
            var page = PageManager.instance.GetCurrentPage();
            EnableMenuForCapability(page.capability);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            var page = PageManager.instance.GetCurrentPage();
            EnableMenuForCapability(page.capability);
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

        private static string GetFilterDisplayName(PackageFilterTab filter, bool translated = true)
        {
            var displayName = string.Empty;
            switch (filter)
            {
                case PackageFilterTab.All:
                    displayName = "All";
                    break;
                case PackageFilterTab.InProject:
                    displayName = "In Project";
                    break;
                case PackageFilterTab.BuiltIn:
                    displayName = "Built-in";
                    break;
                case PackageFilterTab.AssetStore:
                    displayName = "My Assets";
                    break;
            }

            return translated ? ApplicationUtil.instance.GetTranslationForText(displayName) : displayName;
        }

        internal void SetFilter(PackageFilterTab filter)
        {
            PackageFiltering.instance.currentFilterTab = filter;
            filterTabsMenu.text = ApplicationUtil.instance.GetTranslationForText(string.Format("Packages: {0}", GetFilterDisplayName(filter, false)));

            var page = PageManager.instance.GetCurrentPage();
            UpdateOrdering(page);

            var supportFilters = page.capability.supportFilters;
            UIUtils.SetElementDisplay(filtersMenu, supportFilters);
            UIUtils.SetElementDisplay(clearFiltersButton, supportFilters);
            UpdateFiltersMenuText(page.filters);
            EnableMenuForCapability(page.capability);
        }

        private void EnableMenuForCapability(PageCapability capability)
        {
            var enable = !(capability.requireUserLoggedIn && !ApplicationUtil.instance.isUserLoggedIn) &&
                !(capability.requireNetwork && !ApplicationUtil.instance.isInternetReachable);
            orderingMenu.SetEnabled(enable);
            filtersMenu.SetEnabled(enable);
            clearFiltersButton.SetEnabled(enable);
            searchToolbar.SetEnabled(enable);
        }

        private void UpdateFiltersMenuText(PageFilters filters)
        {
            var filtersSet = new List<string>();
            if (filters?.isFilterSet ?? false)
            {
                if (filters.statuses?.Any() ?? false)
                    filtersSet.Add(ApplicationUtil.instance.GetTranslationForText("Status"));
                if (filters.categories?.Any() ?? false)
                    filtersSet.Add(ApplicationUtil.instance.GetTranslationForText("Category"));
                if (filters.labels?.Any() ?? false)
                    filtersSet.Add(ApplicationUtil.instance.GetTranslationForText("Label"));
            }
            filtersMenu.text = filtersSet.Any() ? $"{ApplicationUtil.instance.GetTranslationForText("Filters")} ({string.Join(",", filtersSet.ToArray())})" :  ApplicationUtil.instance.GetTranslationForText("Filters");
        }

        private void SetFilterFromMenu(PackageFilterTab filter)
        {
            if (filter == PackageFiltering.instance.currentFilterTab)
                return;

            SetCurrentSearch(string.Empty);
            SetFilter(filter);
            PackageManagerWindowAnalytics.SendEvent("changeFilter");
        }

        private void SetupAddMenu()
        {
            addMenu.menu.AppendAction(ApplicationUtil.instance.GetTranslationForText("Add package from disk..."), a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters(ApplicationUtil.instance.GetTranslationForText("Select package on disk"), "", new[] { "package.json file", "json" });
                if (Path.GetFileName(path) != "package.json")
                {
                    Debug.Log(ApplicationUtil.instance.GetTranslationForText("Please select a valid package.json file in a package folder."));
                    return;
                }
                if (!string.IsNullOrEmpty(path) && !PackageDatabase.instance.isInstallOrUninstallInProgress)
                {
                    PackageDatabase.instance.InstallFromPath(Path.GetDirectoryName(path));
                    PackageManagerWindowAnalytics.SendEvent("addFromDisk");
                }
            }, a => DropdownMenuAction.Status.Normal);

            addMenu.menu.AppendAction(ApplicationUtil.instance.GetTranslationForText("Add package from tarball..."), a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters(ApplicationUtil.instance.GetTranslationForText("Select package on disk"), "", new[] { "Package tarball", "tgz, tar.gz" });
                if (!string.IsNullOrEmpty(path) && !PackageDatabase.instance.isInstallOrUninstallInProgress)
                {
                    PackageDatabase.instance.InstallFromPath(path);
                    PackageManagerWindowAnalytics.SendEvent("addFromTarball");
                }
            }, a => DropdownMenuAction.Status.Normal);

            addMenu.menu.AppendAction(ApplicationUtil.instance.GetTranslationForText("Add package from git URL..."), a =>
            {
                var addFromGitUrl = new PackagesAction(ApplicationUtil.instance.GetTranslationForText("Add"));
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

        private void SetupFilterTabsMenu()
        {
            filterTabsMenu.menu.MenuItems().Clear();
            filterTabsMenu.ShowTextTooltipOnSizeChange(-16);

            filterTabsMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.All), a =>
            {
                SetFilterFromMenu(PackageFilterTab.All);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.All ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterTabsMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.InProject), a =>
            {
                SetFilterFromMenu(PackageFilterTab.InProject);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.InProject ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterTabsMenu.menu.AppendSeparator();
            filterTabsMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.AssetStore), a =>
            {
                SetFilterFromMenu(PackageFilterTab.AssetStore);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterTabsMenu.menu.AppendSeparator();
            filterTabsMenu.menu.AppendAction(GetFilterDisplayName(PackageFilterTab.BuiltIn), a =>
            {
                SetFilterFromMenu(PackageFilterTab.BuiltIn);
            }, a => PackageFiltering.instance.currentFilterTab == PackageFilterTab.BuiltIn ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.MenuExtensions)
                    extension.OnFilterMenuCreate(filterTabsMenu.menu);
            });
        }

        private void SetupOrdering()
        {
            orderingMenu.menu.MenuItems().Clear();
            orderingMenu.ShowTextTooltipOnSizeChange(-16);
            UIUtils.SetElementDisplay(orderingMenu, false);
        }

        private void UpdateOrdering(IPage page)
        {
            orderingMenu.menu.MenuItems().Clear();
            if (!page?.capability.orderingValues?.Any() ?? true)
            {
                UIUtils.SetElementDisplay(orderingMenu, false);
                return;
            }

            UIUtils.SetElementDisplay(orderingMenu, true);

            foreach (var ordering in page.capability.orderingValues)
            {
                orderingMenu.menu.AppendAction($"{ApplicationUtil.instance.GetTranslationForText(ordering.displayName)} {k_Ascending}", a =>
                {
                    orderingMenu.text = ApplicationUtil.instance.GetTranslationForText("Sort: ") + a.name;

                    var filters = page.filters.Clone();
                    filters.orderBy = ordering.orderBy;
                    filters.isReverseOrder = false;
                    page.UpdateFilters(filters);
                }, a =>
                    {
                        return page.filters.orderBy == ordering.orderBy && !page.filters.isReverseOrder
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal;
                    });

                orderingMenu.menu.AppendAction($"{ApplicationUtil.instance.GetTranslationForText(ordering.displayName)} {k_Descending}", a =>
                {
                    orderingMenu.text = ApplicationUtil.instance.GetTranslationForText("Sort: ") + a.name;

                    var filters = page.filters.Clone();
                    filters.orderBy = ordering.orderBy;
                    filters.isReverseOrder = true;
                    page.UpdateFilters(filters);
                }, a =>
                    {
                        return page.filters.orderBy == ordering.orderBy && page.filters.isReverseOrder
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal;
                    });

                if (page.filters?.orderBy == ordering.orderBy)
                {
                    orderingMenu.text = $"Sort: {ApplicationUtil.instance.GetTranslationForText(ordering.displayName)} {(page.filters.isReverseOrder?k_Descending:k_Ascending)}";
                }
            }
        }

        private void SetupFilters()
        {
            filtersMenu.ShowTextTooltipOnSizeChange(-16);

            filtersMenu.clickable.clicked += () =>
            {
                if (PackageManagerFiltersWindow.instance != null)
                    return;

                var page = PageManager.instance.GetCurrentPage();
                if (page != null && PackageManagerFiltersWindow.ShowAtPosition(GUIUtility.GUIToScreenRect(filtersMenu.worldBound), page.tab, page.filters))
                {
                    filtersMenu.pseudoStates |= PseudoStates.Active;
                    PackageManagerFiltersWindow.instance.OnFiltersChanged += filters =>
                    {
                        UpdateFiltersMenuText(filters);
                        page.UpdateFilters(filters);
                    };
                    PackageManagerFiltersWindow.instance.OnClose += () =>
                    {
                        filtersMenu.pseudoStates &= ~PseudoStates.Active;
                    };
                }
            };
            clearFiltersButton.clickable.clicked += () =>
            {
                var page = PageManager.instance.GetCurrentPage();
                page.ClearFilters();
                UpdateFiltersMenuText(page.filters);
            };
        }

        private void SetupAdvancedMenu()
        {
            advancedMenu.menu.AppendAction(ApplicationUtil.instance.GetTranslationForText("Show dependencies"), a =>
            {
                ToggleDependencies();
                PackageManagerWindowAnalytics.SendEvent("toggleDependencies");
            }, a => PackageManagerPrefs.instance.showPackageDependencies ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            advancedMenu.menu.AppendSeparator();

            advancedMenu.menu.AppendAction(ApplicationUtil.instance.GetTranslationForText("Reset Packages to defaults"), a =>
            {
                EditorApplication.ExecuteMenuItem(ApplicationUtil.k_ResetPackagesMenuPath);
                PageManager.instance.Refresh(RefreshOptions.UpmListOffline);
                PackageManagerWindowAnalytics.SendEvent("resetToDefaults");
            }, a => DropdownMenuAction.Status.Normal);

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

        private VisualElementCache cache { get; set; }

        private ToolbarMenu addMenu { get { return cache.Get<ToolbarMenu>("toolbarAddMenu"); }}
        private ToolbarMenu filterTabsMenu { get { return cache.Get<ToolbarMenu>("toolbarFilterTabsMenu"); } }
        private ToolbarMenu orderingMenu { get { return cache.Get<ToolbarMenu>("toolbarOrderingMenu"); } }
        private ToolbarWindowMenu filtersMenu { get { return cache.Get<ToolbarWindowMenu>("toolbarFiltersMenu"); } }
        private ToolbarButton clearFiltersButton { get { return cache.Get<ToolbarButton>("toolbarClearFiltersButton"); } }
        private ToolbarMenu advancedMenu { get { return cache.Get<ToolbarMenu>("toolbarAdvancedMenu"); } }
        private ToolbarSearchField searchToolbar { get { return cache.Get<ToolbarSearchField>("toolbarSearch"); } }
    }
}
