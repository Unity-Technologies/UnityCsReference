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
        public static readonly string k_ResetPackagesMenuName = "Reset Packages to defaults";
        public static readonly string k_ResetPackagesMenuPath = "Help/" + k_ResetPackagesMenuName;

        internal new class UxmlFactory : UxmlFactory<PackageManagerToolbar> {}

        private long m_SearchTextChangeTimestamp;

        private const long k_SearchEventDelayTicks = TimeSpan.TicksPerSecond / 3;

        private static readonly string k_Ascending = "↓";
        private static readonly string k_Descending = "↑";

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private UnityConnectProxy m_UnityConnect;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
        }

        public PackageManagerToolbar()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageManagerToolbar.uxml");
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
            SetFilter(m_PackageFiltering.currentFilterTab);
            searchToolbar.SetValueWithoutNotify(m_PackageFiltering.currentSearchText);
            searchToolbar.RegisterValueChangedCallback(OnSearchTextChanged);

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PackageFiltering.onFilterTabChanged += SetFilter;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
        }

        public void OnDisable()
        {
            searchToolbar.UnregisterValueChangedCallback(OnSearchTextChanged);
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PackageFiltering.onFilterTabChanged -= SetFilter;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
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

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            var page = m_PageManager.GetCurrentPage();
            EnableMenuForCapability(page.capability);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            var page = m_PageManager.GetCurrentPage();
            EnableMenuForCapability(page.capability);
        }

        internal void SetCurrentSearch(string text)
        {
            searchToolbar.SetValueWithoutNotify(text);
            m_PackageFiltering.currentSearchText = text;
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
                m_PackageFiltering.currentSearchText = searchToolbar.value;
                if (!string.IsNullOrEmpty(searchToolbar.value))
                    PackageManagerWindowAnalytics.SendEvent("search");
            }
        }

        private static string GetFilterDisplayName(PackageFilterTab filter)
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
            return displayName;
        }

        internal void SetFilter(PackageFilterTab filter)
        {
            m_PackageFiltering.currentFilterTab = filter;
            filterTabsMenu.text = L10n.Tr(string.Format("Packages: {0}", GetFilterDisplayName(filter)));

            var page = m_PageManager.GetCurrentPage();
            UpdateOrdering(page);

            var supportFilters = page.capability.supportFilters;
            UIUtils.SetElementDisplay(filtersMenu, supportFilters);
            UIUtils.SetElementDisplay(clearFiltersButton, supportFilters);
            UpdateFiltersMenuText(page.filters);
            EnableMenuForCapability(page.capability);
        }

        private void EnableMenuForCapability(PageCapability capability)
        {
            var enable = !(capability.requireUserLoggedIn && !m_UnityConnect.isUserLoggedIn) &&
                !(capability.requireNetwork && !m_Application.isInternetReachable);
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
                    filtersSet.Add(L10n.Tr("Status"));
                if (filters.categories?.Any() ?? false)
                    filtersSet.Add(L10n.Tr("Category"));
                if (filters.labels?.Any() ?? false)
                    filtersSet.Add(L10n.Tr("Label"));
            }
            filtersMenu.text = filtersSet.Any() ? $"{L10n.Tr("Filters")} ({string.Join(",", filtersSet.ToArray())})" :  L10n.Tr("Filters");
        }

        private void SetFilterFromMenu(PackageFilterTab filter)
        {
            if (filter == m_PackageFiltering.currentFilterTab)
                return;

            SetCurrentSearch(string.Empty);
            SetFilter(filter);
            PackageManagerWindowAnalytics.SendEvent("changeFilter");
        }

        private void SetupAddMenu()
        {
            addMenu.menu.AppendAction(L10n.Tr("Add package from disk..."), a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "package.json file", "json" });
                if (Path.GetFileName(path) != "package.json")
                {
                    Debug.Log(L10n.Tr("Please select a valid package.json file in a package folder."));
                    return;
                }
                if (!string.IsNullOrEmpty(path) && !m_PackageDatabase.isInstallOrUninstallInProgress)
                {
                    m_PackageDatabase.InstallFromPath(Path.GetDirectoryName(path));
                    PackageManagerWindowAnalytics.SendEvent("addFromDisk");
                }
            }, a => DropdownMenuAction.Status.Normal);

            addMenu.menu.AppendAction(L10n.Tr("Add package from tarball..."), a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "Package tarball", "tgz, tar.gz" });
                if (!string.IsNullOrEmpty(path) && !m_PackageDatabase.isInstallOrUninstallInProgress)
                {
                    m_PackageDatabase.InstallFromPath(path);
                    PackageManagerWindowAnalytics.SendEvent("addFromTarball");
                }
            }, a => DropdownMenuAction.Status.Normal);

            addMenu.menu.AppendAction(L10n.Tr("Add package from git URL..."), a =>
            {
                var addFromGitUrl = new PackagesAction(L10n.Tr("Add"));
                addFromGitUrl.actionClicked += url =>
                {
                    addFromGitUrl.Hide();
                    if (!m_PackageDatabase.isInstallOrUninstallInProgress)
                    {
                        m_PackageDatabase.InstallFromUrl(url);
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

            filterTabsMenu.menu.AppendAction(L10n.Tr(GetFilterDisplayName(PackageFilterTab.All)), a =>
            {
                SetFilterFromMenu(PackageFilterTab.All);
            }, a => m_PackageFiltering.currentFilterTab == PackageFilterTab.All ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterTabsMenu.menu.AppendAction(L10n.Tr(GetFilterDisplayName(PackageFilterTab.InProject)), a =>
            {
                SetFilterFromMenu(PackageFilterTab.InProject);
            }, a => m_PackageFiltering.currentFilterTab == PackageFilterTab.InProject ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterTabsMenu.menu.AppendSeparator();
            filterTabsMenu.menu.AppendAction(L10n.Tr(GetFilterDisplayName(PackageFilterTab.AssetStore)), a =>
            {
                SetFilterFromMenu(PackageFilterTab.AssetStore);
            }, a => m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            filterTabsMenu.menu.AppendSeparator();
            filterTabsMenu.menu.AppendAction(L10n.Tr(GetFilterDisplayName(PackageFilterTab.BuiltIn)), a =>
            {
                SetFilterFromMenu(PackageFilterTab.BuiltIn);
            }, a => m_PackageFiltering.currentFilterTab == PackageFilterTab.BuiltIn ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

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
                orderingMenu.menu.AppendAction($"{L10n.Tr(ordering.displayName)} {k_Ascending}", a =>
                {
                    orderingMenu.text = L10n.Tr("Sort: ") + a.name;

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

                orderingMenu.menu.AppendAction($"{L10n.Tr(ordering.displayName)} {k_Descending}", a =>
                {
                    orderingMenu.text = L10n.Tr("Sort: ") + a.name;

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
                    orderingMenu.text = $"Sort: {L10n.Tr(ordering.displayName)} {(page.filters.isReverseOrder?k_Descending:k_Ascending)}";
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

                var page = m_PageManager.GetCurrentPage();
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
                var page = m_PageManager.GetCurrentPage();
                page.ClearFilters();
                UpdateFiltersMenuText(page.filters);
            };
        }

        private void SetupAdvancedMenu()
        {
            advancedMenu.menu.AppendAction(L10n.Tr("Show dependencies"), a =>
            {
                ToggleDependencies();
                PackageManagerWindowAnalytics.SendEvent("toggleDependencies");
            }, a => m_PackageManagerPrefs.showPackageDependencies ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            advancedMenu.menu.AppendSeparator();

            advancedMenu.menu.AppendAction(L10n.Tr("Reset Packages to defaults"), a =>
            {
                EditorApplication.ExecuteMenuItem(k_ResetPackagesMenuPath);
                m_PageManager.Refresh(RefreshOptions.UpmListOffline);
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
            m_PackageManagerPrefs.showPackageDependencies = !m_PackageManagerPrefs.showPackageDependencies;
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
