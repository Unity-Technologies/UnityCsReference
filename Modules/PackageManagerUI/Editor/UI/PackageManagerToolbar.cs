// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using UnityEngine;
using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerToolbar : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageManagerToolbar();
        }

        private IResourceLoader m_ResourceLoader;
        private IApplicationProxy m_Application;
        private IUnityConnectProxy m_UnityConnect;
        private IPackageDatabase m_PackageDatabase;
        private IPackageOperationDispatcher m_OperationDispatcher;
        private IPageManager m_PageManager;
        private IUpmClient m_UpmClient;
        private IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private IProjectSettingsProxy m_SettingsProxy;
        private IIOProxy m_IOProxy;
        private IDropdownHandler m_DropdownHandler;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_Application = container.Resolve<IApplicationProxy>();
            m_UnityConnect = container.Resolve<IUnityConnectProxy>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_OperationDispatcher = container.Resolve<IPackageOperationDispatcher>();
            m_PageManager = container.Resolve<IPageManager>();
            m_UpmClient = container.Resolve<IUpmClient>();
            m_AssetStoreDownloadManager = container.Resolve<IAssetStoreDownloadManager>();
            m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
            m_IOProxy = container.Resolve<IIOProxy>();
            m_DropdownHandler = container.Resolve<IDropdownHandler>();
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
            m_PageManager.onSupportedStatusFiltersChanged += OnSupportedStatusFiltersChanged;

            RefreshInProgressSpinner();
        }

        public void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_PageManager.onFiltersChange -= OnFiltersChange;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
            m_PageManager.onSupportedStatusFiltersChanged -= OnSupportedStatusFiltersChanged;

            RefreshInProgressSpinner(false);
        }

        private void RefreshInProgressSpinner(bool? showSpinner = null)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (showSpinner ?? (m_AssetStoreDownloadManager.IsAnyDownloadInProgress() || m_UpmClient.packageIdsOrNamesInstalling.Any()))
#pragma warning restore RS0030
                inProgressSpinner.Start();
            else
                inProgressSpinner.Stop();
            UIUtils.SetElementDisplay(spinnerButtonContainer, inProgressSpinner.started);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            if (args.progressUpdated.Count == 0 && args.added.Count == 0 && args.removed.Count == 0)
                return;

            RefreshInProgressSpinner();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            UpdateMenuEnableStatusAndTooltip(m_PageManager.activePage);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            UpdateMenuEnableStatusAndTooltip(m_PageManager.activePage);
        }

        private void OnSupportedStatusFiltersChanged(IPage page)
        {
            if (!page.isActivePage)
                return;

            UpdateMenuEnableStatusAndTooltip(page);
        }

        private void UpdateFilterMenuText(PageFilters filters)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var filtersSet = new[] { filters.status.GetDisplayName() }.Concat(filters.categories).Concat(filters.labels)
#pragma warning restore RS0030
                .Where(s => !string.IsNullOrEmpty(s)).ToArray();
            filtersMenu.text = filtersSet.Length > 0 ? string.Format(L10n.Tr("Filters ({0})"), string.Join(", ", filtersSet)) : L10n.Tr("Filters");
        }

        private void UpdateSortMenuText(PageSortOption sortOption)
        {
            var displayName = sortOption.GetDisplayName();
            sortingMenu.text = string.IsNullOrEmpty(displayName) ? L10n.Tr("Sort") : string.Format(L10n.Tr("Sort: {0}"), displayName);
        }

        private void UpdateSortingMenu(IPage page)
        {
            var showSortingMenu = page.supportedSortOptions?.Count > 0;
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
                }, a => page.filters.sortOption == sortOption
                    ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            UpdateSortMenuText(page.filters.sortOption);
        }

        private void UpdateMenuEnableStatusAndTooltip(IPage page)
        {
            SetEnabledStatusAndTooltip(true, string.Empty, sortingMenu, filtersMenu, clearFiltersButton);

            if (!m_Application.isInternetReachable && (page.capability & PageCapability.RequireNetwork) != 0)
            {
                var tooltipText = L10n.Tr("You need to be online before you can sort or filter packages.");
                SetEnabledStatusAndTooltip(false, tooltipText, sortingMenu, filtersMenu, clearFiltersButton);
            }
            else if (!m_UnityConnect.isUserLoggedIn && (page.capability & PageCapability.RequireUserLoggedIn) != 0)
            {
                var tooltipText = L10n.Tr("You need to sign in before you can sort or filter packages.");
                SetEnabledStatusAndTooltip(false, tooltipText, sortingMenu, filtersMenu, clearFiltersButton);
            }
            else if (page.supportedStatusFilters == null || page.supportedStatusFilters.Count == 0)
            {
                var tooltipText = L10n.Tr("There are no applicable filters to display for this context.");
                SetEnabledStatusAndTooltip(false, tooltipText, filtersMenu, clearFiltersButton);
            }

            if (clearFiltersButton.enabledSelf)
                UpdateClearFiltersButtonEnableStatusAndTooltip(page.filters);
        }

        private void UpdateClearFiltersButtonEnableStatusAndTooltip(PageFilters filters)
        {
            clearFiltersButton.enabledSelf = filters.isFilterSet;
            clearFiltersButton.tooltip = filters.isFilterSet ? string.Empty : L10n.Tr("There are no filters applied.");
        }

        private static void SetEnabledStatusAndTooltip(bool enabled, string tooltip, params VisualElement[] elements)
        {
            foreach (var e in elements)
            {
                e.SetEnabled(enabled);
                e.tooltip = tooltip;
            }
        }

        private void OnActivePageChanged(IPage page)
        {
            UpdateSortingMenu(page);
            UpdateFilterMenuText(page.filters);
            UpdateMenuEnableStatusAndTooltip(page);
        }

        private void OnFiltersChange(IPage page, PageFilters filters)
        {
            if (!page.isActivePage)
                return;
            UpdateFilterMenuText(filters);
            UpdateSortMenuText(filters.sortOption);
            UpdateClearFiltersButtonEnableStatusAndTooltip(filters);
        }

        private void SetupInProgressSpinner()
        {
            spinnerButtonContainer.tooltip = L10n.Tr("Click to see progress details");
            spinnerButtonContainer.OnLeftClick(() =>
            {
                if (!inProgressSpinner.started)
                    return;

                m_DropdownHandler.ShowInProgressDropdown(spinnerButtonContainer);
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

                    if (!m_OperationDispatcher.InstallFromPath(m_IOProxy.GetParentDirectory(path), out var tempPackageId))
                        return;

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

                if ((string.IsNullOrEmpty(path)) || !m_OperationDispatcher.InstallFromPath(path, out var tempPackageId))
                    return;

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
                        if (!m_OperationDispatcher.InstallFromUrl(url))
                            return;

                        PackageManagerWindowAnalytics.SendEvent("addFromGitUrl", url);
                        SelectPackageInProject(url);
                    },
                    windowSize = new Vector2(resolvedStyle.width, 50)
                };
                addMenu.ShowInputDropdown(args);
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Install package by technical name...");
            dropdownItem.userData = "AddByName";
            dropdownItem.action = () =>
            {
                m_DropdownHandler.ShowAddPackageByNameDropdown(addMenu);
            };

            dropdownItem = addMenu.AddBuiltInDropdownItem();
            dropdownItem.text = L10n.Tr("Create package...");
            dropdownItem.userData = "CreatePackage";
            dropdownItem.insertSeparatorBefore = true;
            dropdownItem.action = () =>
            {
                m_DropdownHandler.ShowCreatePackageDropdown(addMenu);
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
                if (page == null || !PackageManagerFiltersWindow.ShowAtPosition(EditorMenuExtensions.GUIToScreenRect(filtersMenu, filtersMenu.worldBound), page))
                    return;

                filtersMenu.SetActivePseudoState(true);
                PackageManagerFiltersWindow.instance.OnFiltersChanged += filters =>
                {
                    if (page.UpdateFilters(filters))
                        PackageManagerFiltersAnalytics.SendEvent(filters);
                };
                PackageManagerFiltersWindow.instance.OnClose += () => filtersMenu.SetActivePseudoState(false);
            };

            clearFiltersButton.clicked += () => m_PageManager.activePage.ClearFilters();
        }

        private VisualElementCache cache { get; }

        public ExtendableToolbarMenu addMenu => cache.Get<ExtendableToolbarMenu>("toolbarAddMenu");
        private ToolbarMenu sortingMenu => cache.Get<ToolbarMenu>("toolbarOrderingMenu");
        private ToolbarWindowMenu filtersMenu => cache.Get<ToolbarWindowMenu>("toolbarFiltersMenu");
        private ToolbarButton clearFiltersButton => cache.Get<ToolbarButton>("toolbarClearFiltersButton");
        public ExtendableToolbarMenu toolbarSettingsMenu => cache.Get<ExtendableToolbarMenu>("toolbarSettingsMenu");
        private VisualElement spinnerButtonContainer => cache.Get<VisualElement>("spinnerButtonContainer");
        private LoadingSpinner inProgressSpinner => cache.Get<LoadingSpinner>("inProgressSpinner");
    }
}
