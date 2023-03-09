// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerToolbar : VisualElement
    {
        public const string k_ResetPackagesMenuName = "Reset Packages to defaults";
        public const string k_ResetPackagesMenuPath = "Help/" + k_ResetPackagesMenuName;

        internal new class UxmlFactory : UxmlFactory<PackageManagerToolbar> {}

        private ResourceLoader m_ResourceLoader;
        internal ApplicationProxy m_Application;
        private UnityConnectProxy m_UnityConnect;
        private PackageDatabase m_PackageDatabase;
        private PackageOperationDispatcher m_OperationDispatcher;
        private PageManager m_PageManager;
        private UpmClient m_UpmClient;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private IOProxy m_IOProxy;
        private PageRefreshHandler m_PageRefreshHandler;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_OperationDispatcher = container.Resolve<PackageOperationDispatcher>();
            m_PageManager = container.Resolve<PageManager>();
            m_UpmClient = container.Resolve<UpmClient>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_IOProxy = container.Resolve<IOProxy>();
            m_PageRefreshHandler = container.Resolve<PageRefreshHandler>();
        }

        public PackageManagerToolbar()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageManagerToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            SetupAddMenu();
            SetupSortingMenu();
            SetupFilters();
            SetupInProgressSpinner();
            SetupAdvancedMenu();
        }

        public void OnEnable()
        {
            OnActivePageChanged(m_PageManager.activePage);

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onFiltersChange += OnFiltersChange;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            RefreshInProgressSpinner();
        }

        public void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_PageManager.onFiltersChange -= OnFiltersChange;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            RefreshInProgressSpinner(false);
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
            if (args.progressUpdated.Any() || args.added.Any() || args.removed.Any())
                RefreshInProgressSpinner();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            UpdateMenuEnableStatus(m_PageManager.activePage.capability);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            UpdateMenuEnableStatus(m_PageManager.activePage.capability);
        }

        private void UpdateFilterMenuText(PageFilters filters)
        {
            var filtersSet = new[] { filters.status.GetDisplayName() }.Concat(filters.categories).Concat(filters.labels)
                .Where(s => !string.IsNullOrEmpty(s)).ToArray();
            filtersMenu.text = filtersSet.Any() ? string.Format(L10n.Tr("Filters ({0})"), string.Join(", ", filtersSet)) : L10n.Tr("Filters");
        }

        private void UpdateFiltersMenu(IPage page)
        {
            var showFiltersMenu = page.supportedStatusFilters.Any();
            UIUtils.SetElementDisplay(filtersMenu, showFiltersMenu);
            UIUtils.SetElementDisplay(clearFiltersButton, showFiltersMenu);
            if (!showFiltersMenu)
                return;
            UpdateFilterMenuText(page.filters);
        }

        private void UpdateSortMenuText(PageSortOption sortOption)
        {
            var displayName = sortOption.GetDisplayName();
            sortingMenu.text = string.IsNullOrEmpty(displayName) ? L10n.Tr("Sort") : string.Format(L10n.Tr("Sort: {0}"), displayName);
        }

        private void UpdateSortingMenu(IPage page)
        {
            var showSortingMenu = page.supportedSortOptions.Any();
            UIUtils.SetElementDisplay(sortingMenu, showSortingMenu);
            if (!showSortingMenu)
                return;

            sortingMenu.menu.MenuItems().Clear();
            foreach (var sortOption in page.supportedSortOptions)
            {
                sortingMenu.menu.AppendAction(sortOption.GetDisplayName(), a =>
                {
                    var filters = page.filters.Clone();
                    filters.sortOption = sortOption;
                    if (page.UpdateFilters(filters))
                        PackageManagerFiltersAnalytics.SendEvent(filters);
                },a => page.filters.sortOption == sortOption
                    ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            UpdateSortMenuText(page.filters.sortOption);
        }

        private void UpdateMenuEnableStatus(PageCapability capability)
        {
            var enable = (m_UnityConnect.isUserLoggedIn || (capability & PageCapability.RequireUserLoggedIn) == 0) &&
                         (m_Application.isInternetReachable || (capability & PageCapability.RequireNetwork) == 0);
            sortingMenu.SetEnabled(enable);
            filtersMenu.SetEnabled(enable);
            clearFiltersButton.SetEnabled(enable);
        }

        private void OnActivePageChanged(IPage page)
        {
            UpdateSortingMenu(page);
            UpdateFiltersMenu(page);
            UpdateMenuEnableStatus(page.capability);
        }

        private void OnFiltersChange(IPage page, PageFilters filters)
        {
            if (!page.isActivePage)
                return;
            UpdateFilterMenuText(filters);
            UpdateSortMenuText(filters.sortOption);
        }

        private void SetupInProgressSpinner()
        {
            spinnerButtonContainer.tooltip = L10n.Tr("Click to see progress details");
            spinnerButtonContainer.OnLeftClick(() =>
            {
                if (!inProgressSpinner.started)
                    return;

                var rect = GUIUtility.GUIToScreenRect(spinnerButtonContainer.worldBound);
                var dropdown = new InProgressDropdown(m_ResourceLoader, m_UpmClient, m_AssetStoreDownloadManager, m_PackageDatabase, m_PageManager) { position = rect };
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
                m_PageRefreshHandler.Refresh(RefreshOptions.UpmListOffline);
                PackageManagerWindowAnalytics.SendEvent("resetToDefaults");
            };

            if (Unsupported.IsDeveloperBuild())
            {
                dropdownItem = toolbarSettingsMenu.AddBuiltInDropdownItem();
                dropdownItem.insertSeparatorBefore = true;
                dropdownItem.text = L10n.Tr("Internal/Reset Package Manager UI");
                dropdownItem.action = () =>
                {
                    PackageManagerWindow.instance?.Close();
                    ServicesContainer.instance.OnDisable();
                    ServicesContainer.instance.Reload();
                };
            }
        }

        private void SetupAddMenu()
        {
            var dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Install package from disk...");
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


                    if (m_OperationDispatcher.isInstallOrUninstallInProgress)
                        return;

                    m_OperationDispatcher.InstallFromPath(m_IOProxy.GetParentDirectory(path), out var tempPackageId);
                    PackageManagerWindowAnalytics.SendEvent("addFromDisk");
                    SelectPackageInProject(tempPackageId);
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager Window] Cannot install package from disk {path}: {e.Message}");
                }
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Install package from tarball...");
            dropdownItem.userData = "AddFromTarball";
            dropdownItem.action = () =>
            {
                var path = m_Application.OpenFilePanelWithFilters(L10n.Tr("Select package on disk"), "", new[] { "Package tarball", "tgz, tar.gz" });
                if (string.IsNullOrEmpty(path) || m_OperationDispatcher.isInstallOrUninstallInProgress)
                    return;
                m_OperationDispatcher.InstallFromPath(path, out var tempPackageId);
                PackageManagerWindowAnalytics.SendEvent("addFromTarball");
                SelectPackageInProject(tempPackageId);
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Install package from git URL...");
            dropdownItem.userData = "AddFromGit";
            dropdownItem.action = () =>
            {
                var args = new InputDropdownArgs
                {
                    title = L10n.Tr("Install package from git URL"),
                    iconUssClass = "git",
                    placeholderText = L10n.Tr("URL"),
                    submitButtonText = L10n.Tr("Install"),
                    onInputSubmitted = url =>
                    {
                        if (m_OperationDispatcher.isInstallOrUninstallInProgress)
                            return;
                        m_OperationDispatcher.InstallFromUrl(url);
                        PackageManagerWindowAnalytics.SendEvent("addFromGitUrl", url);
                        SelectPackageInProject(url);
                    },
                    windowSize = new Vector2(resolvedStyle.width, 50)
                };
                // If a background GUI painted before, the coordinates got could be different.
                // Repaint the package manager to ensure the coordinates retrieved is from packmanager.
                PackageManagerWindow.GetWindow<PackageManagerWindow>().RepaintImmediately();
                addMenu.ShowInputDropdown(args);
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Install package by name...");
            dropdownItem.userData = "AddByName";
            dropdownItem.action = () =>
            {
                // If a background GUI painted before, the coordinates got could be different.
                // Repaint the package manager to ensure the coordinates retrieved is from packmanager.
                PackageManagerWindow.GetWindow<PackageManagerWindow>().RepaintImmediately();
                // Same as above, the worldBound of the toolbar is used rather than the addMenu
                var rect = GUIUtility.GUIToScreenRect(worldBound);
                var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_UpmClient, m_PackageDatabase, m_PageManager, PackageManagerWindow.instance) { position = rect };
                DropdownContainer.ShowDropdown(dropdown);
            };
        }

        private void SelectPackageInProject(string packageUniqueId)
        {
            var package = m_PackageDatabase.GetPackage(packageUniqueId);
            if (package == null)
                return;

            m_PageManager.activePage = m_PageManager.GetPage(InProjectPage.k_Id);
            m_PageManager.activePage.SetNewSelection(package);
        }

        private void SetupSortingMenu()
        {
            sortingMenu.menu.MenuItems().Clear();
            sortingMenu.ShowTextTooltipOnSizeChange(-16);
            UIUtils.SetElementDisplay(sortingMenu, false);
        }

        private void SetupFilters()
        {
            filtersMenu.ShowTextTooltipOnSizeChange(-16);
            filtersMenu.clicked += () =>
            {
                PackageManagerFiltersWindow.instance?.Close();

                var page = m_PageManager.activePage;
                if (page == null || !PackageManagerFiltersWindow.ShowAtPosition(GUIUtility.GUIToScreenRect(filtersMenu.worldBound), page))
                    return;

                filtersMenu.pseudoStates |= PseudoStates.Active;
                PackageManagerFiltersWindow.instance.OnFiltersChanged += filters =>
                {
                    if (page.UpdateFilters(filters))
                        PackageManagerFiltersAnalytics.SendEvent(filters);
                };
                PackageManagerFiltersWindow.instance.OnClose += () => filtersMenu.pseudoStates &= ~PseudoStates.Active;
            };

            clearFiltersButton.clicked += () => m_PageManager.activePage.ClearFilters();
        }

        private VisualElementCache cache { get; }

        internal ExtendableToolbarMenu addMenu => cache.Get<ExtendableToolbarMenu>("toolbarAddMenu");
        private ToolbarMenu sortingMenu => cache.Get<ToolbarMenu>("toolbarOrderingMenu");
        private ToolbarWindowMenu filtersMenu => cache.Get<ToolbarWindowMenu>("toolbarFiltersMenu");
        private ToolbarButton clearFiltersButton => cache.Get<ToolbarButton>("toolbarClearFiltersButton");
        internal ExtendableToolbarMenu toolbarSettingsMenu => cache.Get<ExtendableToolbarMenu>("toolbarSettingsMenu");
        internal VisualElement spinnerButtonContainer => cache.Get<VisualElement>("spinnerButtonContainer");
        internal LoadingSpinner inProgressSpinner => cache.Get<LoadingSpinner>("inProgressSpinner");
    }
}
