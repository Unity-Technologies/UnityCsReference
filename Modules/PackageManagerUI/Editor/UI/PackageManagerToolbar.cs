// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerToolbar : VisualElement
    {
        public static readonly string k_ResetPackagesMenuName = "Reset Packages to defaults";
        public static readonly string k_ResetPackagesMenuPath = "Help/" + k_ResetPackagesMenuName;

        internal new class UxmlFactory : UxmlFactory<PackageManagerToolbar> {}

        private long m_SearchTextChangeTimestamp;

        private const long k_SearchEventDelayTicks = TimeSpan.TicksPerSecond / 3;

        private ResourceLoader m_ResourceLoader;
        internal ApplicationProxy m_Application;
        private UnityConnectProxy m_UnityConnect;
        private PackageFiltering m_PackageFiltering;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private UpmClient m_UpmClient;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
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
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
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
            SetupInProgressSpinner();
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
            m_PageManager.onFiltersChange += UpdateFiltersMenuText;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            RefreshInProgressSpinner();
        }

        public void OnDisable()
        {
            searchToolbar.UnregisterValueChangedCallback(OnSearchTextChanged);
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PackageFiltering.onFilterTabChanged -= SetFilter;
            m_PageManager.onFiltersChange -= UpdateFiltersMenuText;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            RefreshInProgressSpinner(false);
        }

        public void FocusOnSearch()
        {
            searchToolbar.Focus();
            // Line below is required to make sure focus is in textfield
            searchToolbar.Q<TextField>()?.visualInput?.Focus();
        }

        private void RefreshInProgressSpinner(bool? showSpinner = null)
        {
            if (showSpinner ?? (m_AssetStoreDownloadManager.IsAnyDownloadInProgress() || m_UpmClient.packageIdsOrNamesInstalling.Any()))
                inProgressSpinner.Start();
            else
                inProgressSpinner.Stop();
            UIUtils.SetElementDisplay(spinnerButtonContainer, inProgressSpinner.started);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            UpdateOrdering(m_PageManager.GetCurrentPage());

            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.MyRegistries)
            {
                // We can skip the whole database scan to save some time if non of the packages changed are related to scoped registry
                // Note that we also check `preUpdate` to catch the cases where packages are move from ScopedRegistry to UnityRegistry
                if (args.added.Concat(args.removed).Concat(args.updated).Concat(args.preUpdate).All(p => p.versions.primary.availableRegistry != RegistryType.MyRegistries))
                    return;

                if (m_PackageDatabase.allPackages.All(p => p.versions.primary.availableRegistry != RegistryType.MyRegistries))
                    SetFilter(PackageFilterTab.UnityRegistry);
            }

            if (args.progressUpdated.Any() || args.added.Any() || args.removed.Any())
                RefreshInProgressSpinner();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            var page = m_PageManager.GetCurrentPage();
            EnableMenuForCapability(page.capability);
            UpdateOrdering(page);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            var page = m_PageManager.GetCurrentPage();
            EnableMenuForCapability(page.capability);
        }

        internal void SetCurrentSearch(string text)
        {
            var value = text?.Trim() ?? string.Empty;
            searchToolbar.SetValueWithoutNotify(value);
            m_PackageFiltering.currentSearchText = value;
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
                var value = searchToolbar.value.Trim();
                m_PackageFiltering.currentSearchText = value;
                if (!string.IsNullOrEmpty(value))
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
                if (!string.IsNullOrEmpty(filters.status))
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

        private void SetupInProgressSpinner()
        {
            spinnerButtonContainer.tooltip = L10n.Tr("Click to see progress details");
            spinnerButtonContainer.OnLeftClick(() =>
            {
                if (!inProgressSpinner.started)
                    return;

                var rect = GUIUtility.GUIToScreenRect(spinnerButtonContainer.worldBound);
                var dropdown = new InProgressDropdown(m_ResourceLoader, m_PackageFiltering, m_UpmClient, m_AssetStoreDownloadManager, m_PackageDatabase, m_PageManager) { position = rect };
                DropdownContainer.ShowDropdown(dropdown);
            });
        }

        private void SetupAdvancedMenu()
        {
            toolbarSettingsMenu.tooltip = L10n.Tr("Advanced");

            var dropdownItem = toolbarSettingsMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Project Settings");
            dropdownItem.action = () =>
            {
                if (!m_SettingsProxy.advancedSettingsExpanded)
                {
                    m_SettingsProxy.advancedSettingsExpanded = true;
                    m_SettingsProxy.Save();
                }
                SettingsWindow.Show(SettingsScope.Project, PackageManagerProjectSettingsProvider.k_PackageManagerSettingsPath);
                PackageManagerWindowAnalytics.SendEvent("advancedProjectSettings");
            };

            dropdownItem = toolbarSettingsMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Preferences");
            dropdownItem.action = () =>
            {
                SettingsWindow.Show(SettingsScope.User, PackageManagerUserSettingsProvider.k_PackageManagerUserSettingsPath);
                PackageManagerWindowAnalytics.SendEvent("packageManagerUserSettings");
            };

            dropdownItem = toolbarSettingsMenu.AddBuiltInDropdownItem();
            dropdownItem.insertSeparatorBefore = true;
            dropdownItem.text = L10n.Tr("Manual resolve");
            dropdownItem.action = () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    m_UpmClient.Resolve();
                }
            };

            dropdownItem = toolbarSettingsMenu.AddBuiltInDropdownItem();
            dropdownItem.insertSeparatorBefore = true;
            dropdownItem.text = L10n.Tr("Reset Packages to defaults");
            dropdownItem.action = () =>
            {
                EditorApplication.ExecuteMenuItem(k_ResetPackagesMenuPath);
                m_PageManager.Refresh(RefreshOptions.UpmListOffline);
                PackageManagerWindowAnalytics.SendEvent("resetToDefaults");
            };

            if (Unsupported.IsDeveloperBuild())
            {
                dropdownItem = toolbarSettingsMenu.AddBuiltInDropdownItem();
                dropdownItem.insertSeparatorBefore = true;
                dropdownItem.text = L10n.Tr("Internal/Reset Package Database");
                dropdownItem.action = () =>
                {
                    PackageManagerWindow.instance?.Close();
                    m_PageManager.Reload();
                    ServicesContainer.instance.Resolve<AssetStoreCallQueue>().ClearFetchDetails();
                };
            }
        }

        private void SetupAddMenu()
        {
            var dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Add package from disk...");
            dropdownItem.userData = "AddFromDisk";
            dropdownItem.action = () =>
            {
                var path = m_Application.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "package.json file", "json" });
                if (string.IsNullOrEmpty(path))
                    return;

                try
                {
                    if (m_IOProxy.GetFileName(path) != "package.json")
                    {
                        Debug.Log(L10n.Tr("[Package Manager Window] Please select a valid package.json file in a package folder."));
                        return;
                    }


                    if (!m_PackageDatabase.isInstallOrUninstallInProgress)
                    {
                        m_PackageDatabase.InstallFromPath(m_IOProxy.GetParentDirectory(path), out var tempPackageId);
                        PackageManagerWindowAnalytics.SendEvent("addFromDisk");

                        var package = m_PackageDatabase.GetPackage(tempPackageId);
                        if (package != null)
                        {
                            m_PackageFiltering.currentFilterTab = PackageFilterTab.InProject;
                            m_PageManager.SetSelected(package);
                        }
                    }
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager Window] Cannot add package from disk {path}: {e.Message}");
                }
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Add package from tarball...");
            dropdownItem.userData = "AddFromTarball";
            dropdownItem.action = () =>
            {
                var path = m_Application.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "Package tarball", "tgz, tar.gz" });
                if (!string.IsNullOrEmpty(path) && !m_PackageDatabase.isInstallOrUninstallInProgress)
                {
                    m_PackageDatabase.InstallFromPath(path, out var tempPackageId);
                    PackageManagerWindowAnalytics.SendEvent("addFromTarball");

                    var package = m_PackageDatabase.GetPackage(tempPackageId);
                    if (package != null)
                    {
                        m_PackageFiltering.currentFilterTab = PackageFilterTab.InProject;
                        m_PageManager.SetSelected(package);
                    }
                }
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Add package from git URL...");
            dropdownItem.userData = "AddFromGit";
            dropdownItem.action = () =>
            {
                var args = new InputDropdownArgs
                {
                    title = L10n.Tr("Add package from git URL"),
                    iconUssClass = "git",
                    placeholderText = L10n.Tr("URL"),
                    submitButtonText = L10n.Tr("Add"),
                    onInputSubmitted = url =>
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
                    },
                    windowSize = new Vector2(resolvedStyle.width, 50)
                };
                addMenu.ShowInputDropdown(args);
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Add package by name...");
            dropdownItem.userData = "AddByName";
            dropdownItem.action = () =>
            {
                // Same as above, the worldBound of the toolbar is used rather than the addMenu
                var rect = GUIUtility.GUIToScreenRect(worldBound);
                var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_PackageFiltering, m_UpmClient, m_PackageDatabase, m_PageManager, PackageManagerWindow.instance) { position = rect };
                DropdownContainer.ShowDropdown(dropdown);
            };
        }

        private void AddFilterTabToDropdownMenu(PackageFilterTab tab, Action<DropdownMenuAction> action = null, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback = null)
        {
            action = action ?? (a => SetFilterFromMenu(tab));
            actionStatusCallback = actionStatusCallback ?? (a => m_PackageFiltering.currentFilterTab == tab ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            filterTabsMenu.menu.AppendAction(GetFilterDisplayName(tab), action, actionStatusCallback, tab);
        }

        private void SetupFilterTabsMenu()
        {
            filterTabsMenu.menu.MenuItems().Clear();
            filterTabsMenu.ShowTextTooltipOnSizeChange(-16);

            AddFilterTabToDropdownMenu(PackageFilterTab.UnityRegistry);
            AddFilterTabToDropdownMenu(PackageFilterTab.MyRegistries, null, a =>
            {
                if (!m_PackageDatabase.allPackages.Any(p => PackageFiltering.FilterByTab(p, PackageFilterTab.MyRegistries, true)))
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

            var shouldDisplayOrdering = page?.capability.orderingValues?.Any() ?? false;
            if (!shouldDisplayOrdering)
            {
                UIUtils.SetElementDisplay(orderingMenu, false);
                return;
            }

            UIUtils.SetElementDisplay(orderingMenu, true);

            var matchCurrentFilter = false;
            if (shouldDisplayOrdering)
            {
                foreach (var ordering in page.capability.orderingValues)
                    matchCurrentFilter |= AddOrdering(page, ordering);
            }

            if (!matchCurrentFilter)
            {
                var filters = page.filters?.Clone();
                var firstOrdering = page.capability?.orderingValues?.FirstOrDefault();
                if (filters != null && firstOrdering != null)
                {
                    orderingMenu.text = $"Sort: {L10n.Tr(firstOrdering.displayName)}";
                    filters.orderBy = firstOrdering.orderBy;
                    filters.isReverseOrder = firstOrdering.order == PageCapability.Order.Descending;

                    if(page.UpdateFilters(filters))
                        PackageManagerFiltersAnalytics.SendEvent(filters);
                }
            }
        }

        private bool AddOrdering(IPage page, PageCapability.Ordering ordering)
        {
            var matchCurrentFilter = false;
            orderingMenu.menu.AppendAction($"{L10n.Tr(ordering.displayName)}", a =>
            {
                orderingMenu.text = $"{L10n.Tr("Sort: ")} {a.name}";

                var filters = page.filters.Clone();
                filters.orderBy = ordering.orderBy;
                filters.isReverseOrder = ordering.order == PageCapability.Order.Descending;
                if (page.UpdateFilters(filters))
                    PackageManagerFiltersAnalytics.SendEvent(filters);

            }, a =>
                {
                    return page.filters.orderBy == ordering.orderBy &&
                    (page.filters.isReverseOrder ? ordering.order == PageCapability.Order.Descending : ordering.order != PageCapability.Order.Descending)
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal;
                });

            if (page.filters?.orderBy == ordering.orderBy &&
                (page.filters?.isReverseOrder ?? false ? ordering.order == PageCapability.Order.Descending : ordering.order != PageCapability.Order.Descending)
            )
            {
                matchCurrentFilter = true;
                orderingMenu.text = $"Sort: {L10n.Tr(ordering.displayName)}";
            }

            return matchCurrentFilter;
        }

        private void SetupFilters()
        {
            filtersMenu.ShowTextTooltipOnSizeChange(-16);

            filtersMenu.clicked += () =>
            {
                if (PackageManagerFiltersWindow.instance != null)
                    PackageManagerFiltersWindow.instance.Close();

                var page = m_PageManager.GetCurrentPage();
                if (page != null && PackageManagerFiltersWindow.ShowAtPosition(GUIUtility.GUIToScreenRect(filtersMenu.worldBound), page))
                {
                    filtersMenu.pseudoStates |= PseudoStates.Active;
                    PackageManagerFiltersWindow.instance.OnFiltersChanged += filters =>
                    {
                        if (page.UpdateFilters(filters))
                            PackageManagerFiltersAnalytics.SendEvent(filters);
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
            };
        }

        private VisualElementCache cache { get; set; }

        internal ExtendableToolbarMenu addMenu { get { return cache.Get<ExtendableToolbarMenu>("toolbarAddMenu"); }}
        private ToolbarMenu filterTabsMenu { get { return cache.Get<ToolbarMenu>("toolbarFilterTabsMenu"); } }
        private ToolbarMenu orderingMenu { get { return cache.Get<ToolbarMenu>("toolbarOrderingMenu"); } }
        private ToolbarWindowMenu filtersMenu { get { return cache.Get<ToolbarWindowMenu>("toolbarFiltersMenu"); } }
        private ToolbarButton clearFiltersButton { get { return cache.Get<ToolbarButton>("toolbarClearFiltersButton"); } }
        private ToolbarSearchField searchToolbar { get { return cache.Get<ToolbarSearchField>("toolbarSearch"); } }
        internal ExtendableToolbarMenu toolbarSettingsMenu { get { return cache.Get<ExtendableToolbarMenu>("toolbarSettingsMenu"); } }
        internal VisualElement spinnerButtonContainer => cache.Get<VisualElement>("spinnerButtonContainer");
        internal LoadingSpinner inProgressSpinner => cache.Get<LoadingSpinner>("inProgressSpinner");
    }
}
