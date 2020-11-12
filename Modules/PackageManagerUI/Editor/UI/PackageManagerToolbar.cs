// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System;
using UnityEngine;

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
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private UpmClient m_UpmClient;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private IOProxy m_IOProxy;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_UpmClient = container.Resolve<UpmClient>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_IOProxy = container.Resolve<IOProxy>();
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
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.MyRegistries)
            {
                // we can skip the whole database scan to save some time
                var changed = added.Concat(removed).Concat(preUpdate).Concat(postUpdate);
                if (!changed.Any(p => p.Is(PackageType.ScopedRegistry)))
                    return;

                if (!m_PackageDatabase.allPackages.Any(p => p.Is(PackageType.ScopedRegistry)))
                    SetFilter(PackageFilterTab.UnityRegistry);
            }
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
            switch (filter)
            {
                case PackageFilterTab.UnityRegistry:
                    return L10n.Tr("Unity Registry");
                case PackageFilterTab.MyRegistries:
                    return L10n.Tr("My Registries");
                case PackageFilterTab.InProject:
                    return L10n.Tr("In Project");
                case PackageFilterTab.BuiltIn:
                    return L10n.Tr("Built-in");
                case PackageFilterTab.AssetStore:
                    return L10n.Tr("My Assets");
            }
            return string.Empty;
        }

        internal void SetFilter(PackageFilterTab filter)
        {
            m_PackageFiltering.currentFilterTab = filter;
            filterTabsMenu.text = string.Format(L10n.Tr("Packages: {0}"), GetFilterDisplayName(filter));

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

        private void SetupAdvancedMenu()
        {
            toolbarSettingsMenu.tooltip = L10n.Tr("Advanced");
            toolbarSettingsMenu.menu.AppendAction(L10n.Tr("Advanced Project Settings"), a =>
            {
                if (!m_SettingsProxy.advancedSettingsExpanded)
                {
                    m_SettingsProxy.advancedSettingsExpanded = true;
                    m_SettingsProxy.Save();
                }
                SettingsWindow.Show(SettingsScope.Project, PackageManagerProjectSettingsProvider.k_PackageManagerSettingsPath);
                PackageManagerWindowAnalytics.SendEvent("advancedProjectSettings");
            });

            toolbarSettingsMenu.menu.AppendSeparator();

            toolbarSettingsMenu.menu.AppendAction(L10n.Tr("Reset Packages to defaults"), a =>
            {
                EditorApplication.ExecuteMenuItem(k_ResetPackagesMenuPath);
                m_PageManager.Refresh(RefreshOptions.UpmListOffline);
                PackageManagerWindowAnalytics.SendEvent("resetToDefaults");
            });

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.MenuExtensions)
                    extension.OnAdvancedMenuCreate(toolbarSettingsMenu.menu);
            });
        }

        private void SetupAddMenu()
        {
            addMenu.menu.AppendAction(L10n.Tr("Add package from disk..."), a =>
            {
                var path = m_Application.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "package.json file", "json" });
                if (string.IsNullOrEmpty(path))
                    return;

                try
                {
                    if (m_IOProxy.GetFileName(path) != "package.json")
                    {
                        Debug.Log(L10n.Tr("[Package Manager] Please select a valid package.json file in a package folder."));
                        return;
                    }


                    if (!m_PackageDatabase.isInstallOrUninstallInProgress)
                    {
                        m_PackageDatabase.InstallFromPath(m_IOProxy.GetParentDirectory(path));
                        PackageManagerWindowAnalytics.SendEvent("addFromDisk");
                    }
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager] Cannot add package from disk {path}: {e.Message}");
                }
            }, a => DropdownMenuAction.Status.Normal, "AddFromDisk");

            addMenu.menu.AppendAction(L10n.Tr("Add package from tarball..."), a =>
            {
                var path = m_Application.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "Package tarball", "tgz, tar.gz" });
                if (!string.IsNullOrEmpty(path) && !m_PackageDatabase.isInstallOrUninstallInProgress)
                {
                    m_PackageDatabase.InstallFromPath(path);
                    PackageManagerWindowAnalytics.SendEvent("addFromTarball");
                }
            }, a => DropdownMenuAction.Status.Normal, "AddFromTarball");

            addMenu.menu.AppendAction(L10n.Tr("Add package from git URL..."), a =>
            {
                var configs = new GenericInputDropdown.Configs
                {
                    title = L10n.Tr("Add package from git URL"),
                    iconUssClass = "git",
                    label = L10n.Tr("URL"),
                    submitButtonText = L10n.Tr("Add"),
                    inputSubmittedCallback = url =>
                    {
                        if (!m_PackageDatabase.isInstallOrUninstallInProgress)
                        {
                            m_PackageDatabase.InstallFromUrl(url);
                            PackageManagerWindowAnalytics.SendEvent("addFromGitUrl", url);

                            var package = m_PackageDatabase.GetPackage(url);
                            if (package != null)
                            {
                                m_PackageFiltering.currentFilterTab = PackageFilterTab.InProject;
                                m_PageManager.SetSelected(package);
                            }
                        }
                    }
                };
                // We are using the `worldBound` of the toolbar rather than the worldBound of the addMenu because addMenu have a `-1` left margin
                // And that makes the dropdown show in a bit of a misaligned place
                var rect = GUIUtility.GUIToScreenRect(worldBound);
                var dropdown = new GenericInputDropdown(m_ResourceLoader, PackageManagerWindow.instance, configs) { position = rect };
                DropdownContainer.ShowDropdown(dropdown);
            }, a => DropdownMenuAction.Status.Normal, "AddFromGit");

            addMenu.menu.AppendAction(L10n.Tr("Add package by name..."), a =>
            {
                // Same as above, the worldBound of the toolbar is used rather than the addMenu
                var rect = GUIUtility.GUIToScreenRect(worldBound);
                var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_PackageFiltering, m_UpmClient, m_PackageDatabase, m_PageManager, PackageManagerWindow.instance) { position = rect };
                DropdownContainer.ShowDropdown(dropdown);
            }, a => DropdownMenuAction.Status.Normal, "AddByName");

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.MenuExtensions)
                    extension.OnAddMenuCreate(addMenu.menu);
            });
        }

        private void AddFilterTabToDropdownMenu(PackageFilterTab tab, Action<DropdownMenuAction> action = null, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback = null)
        {
            action = action ?? (a => SetFilterFromMenu(tab));
            actionStatusCallback = actionStatusCallback ?? (a => m_PackageFiltering.currentFilterTab == tab ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            filterTabsMenu.menu.AppendAction(GetFilterDisplayName(tab), action, actionStatusCallback);
        }

        private void SetupFilterTabsMenu()
        {
            filterTabsMenu.menu.MenuItems().Clear();
            filterTabsMenu.ShowTextTooltipOnSizeChange(-16);

            AddFilterTabToDropdownMenu(PackageFilterTab.UnityRegistry);
            AddFilterTabToDropdownMenu(PackageFilterTab.MyRegistries, null, a =>
            {
                if (!m_PackageDatabase.allPackages.Any(p => p.Is(PackageType.ScopedRegistry)) && !m_PackageDatabase.allPackages.Any(p => p.Is(PackageType.MainNotUnity)))
                    return DropdownMenuAction.Status.Hidden;
                else if (m_PackageFiltering.currentFilterTab == PackageFilterTab.MyRegistries)
                    return DropdownMenuAction.Status.Checked;
                return DropdownMenuAction.Status.Normal;
            });
            AddFilterTabToDropdownMenu(PackageFilterTab.InProject);
            filterTabsMenu.menu.AppendSeparator();
            AddFilterTabToDropdownMenu(PackageFilterTab.AssetStore);
            filterTabsMenu.menu.AppendSeparator();
            AddFilterTabToDropdownMenu(PackageFilterTab.BuiltIn);

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

            filtersMenu.clicked += () =>
            {
                if (PackageManagerFiltersWindow.instance != null)
                    PackageManagerFiltersWindow.instance.Close();

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

            clearFiltersButton.clicked += () =>
            {
                var page = m_PageManager.GetCurrentPage();
                page.ClearFilters();
                UpdateFiltersMenuText(page.filters);
            };
        }

        private VisualElementCache cache { get; set; }

        internal ToolbarMenu addMenu { get { return cache.Get<ToolbarMenu>("toolbarAddMenu"); }}
        private ToolbarMenu filterTabsMenu { get { return cache.Get<ToolbarMenu>("toolbarFilterTabsMenu"); } }
        private ToolbarMenu orderingMenu { get { return cache.Get<ToolbarMenu>("toolbarOrderingMenu"); } }
        private ToolbarWindowMenu filtersMenu { get { return cache.Get<ToolbarWindowMenu>("toolbarFiltersMenu"); } }
        private ToolbarButton clearFiltersButton { get { return cache.Get<ToolbarButton>("toolbarClearFiltersButton"); } }
        private ToolbarSearchField searchToolbar { get { return cache.Get<ToolbarSearchField>("toolbarSearch"); } }
        private ToolbarMenu toolbarSettingsMenu { get { return cache.Get<ToolbarMenu>("toolbarSettingsMenu"); } }
    }
}
