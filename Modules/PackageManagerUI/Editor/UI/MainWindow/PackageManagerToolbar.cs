// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using System;
using System.Text;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerToolbar : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageManagerToolbar(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IUnityConnectProxy>(),
                    container.Resolve<IPackageDatabase>(),
                    container.Resolve<IPackageOperationDispatcher>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IUpmClient>(),
                    container.Resolve<IAssetStoreDownloadManager>(),
                    container.Resolve<IProjectSettingsProxy>(),
                    container.Resolve<IDropdownHandler>());
            }
        }

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IApplicationProxy m_Application;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPageManager m_PageManager;
        private readonly IUpmClient m_UpmClient;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IDropdownHandler m_DropdownHandler;
        public PackageManagerToolbar(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IUnityConnectProxy unityConnect,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IPageManager pageManager,
            IUpmClient upmClient,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IProjectSettingsProxy settingsProxy,
            IDropdownHandler dropdownHandler)
        {
            m_ResourceLoader = resourceLoader;
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_UpmClient = upmClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_SettingsProxy = settingsProxy;
            m_DropdownHandler = dropdownHandler;

            var root = m_ResourceLoader.GetTemplate("PackageManagerToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            SetupAddMenu();
            SetupSortingMenu();
            SetupFilters();
            SetupInProgressSpinner();
            SetupAdvancedMenu();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onFiltersChange += OnFiltersChange;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            OnActivePageChanged(m_PageManager.activePage);
            RefreshInProgressSpinner();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
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
            if (showSpinner ?? (m_AssetStoreDownloadManager.IsAnyDownloadInProgress() || m_UpmClient.packageIdsOrNamesInstalling.Count > 0))
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

        private void UpdateFilterMenuText(IPageFilters filters)
        {
            const string separator = ", ";
            var filtersSelected = new StringBuilder();
            if (filters.status != PageFilterStatus.None)
                filtersSelected.Append(filters.status.GetDisplayName()).Append(separator);
            foreach (var filter in filters.categories.Join(filters.labels))
                filtersSelected.Append(filter).Append(separator);
            foreach (var filter in filters.packageUniqueIds.SelectNonEmpty(i => m_PackageDatabase.GetPackage(i)?.displayName ?? i))
                filtersSelected.Append(filter).Append(separator);
            // Since we always append the separator at the end, we want to make sure to move the ending separator
            filtersSelected.Length = Math.Max(0, filtersSelected.Length - separator.Length);
            filtersMenu.text = filtersSelected.Length == 0 ? L10n.Tr("Filters") : string.Format(L10n.Tr("Filters ({0})"), filtersSelected);
        }

        private void UpdateSortMenuText(PageSortOption sortOption)
        {
            var displayName = sortOption.GetDisplayName();
            sortingMenu.text = string.IsNullOrEmpty(displayName) ? L10n.Tr("Sort") : string.Format(L10n.Tr("Sort: {0}"), displayName);
        }

        private void UpdateSortingMenu(IPage page)
        {
            var showSortingMenu = page.filters.supportedSortOptions?.Count > 0;
            UIUtils.SetElementDisplay(sortingMenu, showSortingMenu);
            if (!showSortingMenu)
                return;

            sortingMenu.menu.MenuItems().Clear();
            foreach (var sortOption in page.filters.supportedSortOptions)
            {
                sortingMenu.menu.AppendAction(sortOption.GetDisplayName(), a =>
                {
                    if (page.UpdateSortOption(sortOption))
                        PackageManagerFiltersAnalytics.SendEvent(page.filters);
                }, a => page.filters.sortOption == sortOption
                    ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            UpdateSortMenuText(page.filters.sortOption);
        }

        private void UpdateMenuEnableStatusAndTooltip(IPage page)
        {
            if (!m_Application.isInternetReachable && (page.capability & PageCapability.RequireNetwork) != 0)
            {
                var tooltipText = L10n.Tr("You need to be online before you can sort or filter packages.");
                DisableElementsWithTooltip(tooltipText, sortingMenu, filtersMenu, clearFiltersButton);
            }
            else if (!m_UnityConnect.isUserLoggedIn && (page.capability & PageCapability.RequireUserLoggedIn) != 0)
            {
                var tooltipText = L10n.Tr("You need to sign in before you can sort or filter packages.");
                DisableElementsWithTooltip(tooltipText, sortingMenu, filtersMenu, clearFiltersButton);
            }
            else if (page.filters is not { anySupportedFilters: true })
            {
                var tooltipText = L10n.Tr("There are no applicable filters to display for this context.");
                DisableElementsWithTooltip(tooltipText, filtersMenu, clearFiltersButton);
            }
            else
            {
                sortingMenu.SetEnabled(true);
                sortingMenu.tooltip = sortingMenu.text;

                filtersMenu.SetEnabled(true);
                filtersMenu.tooltip = filtersMenu.text;

                clearFiltersButton.SetEnabled(page.filters.isFilterSet);
                clearFiltersButton.tooltip = clearFiltersButton.enabledSelf ? clearFiltersButton.text : L10n.Tr("There are no filters applied.");
            }
        }

        private static void DisableElementsWithTooltip(string tooltip, params VisualElement[] elements)
        {
            foreach (var e in elements)
            {
                e.SetEnabled(false);
                e.tooltip = tooltip;
            }
        }

        private void OnActivePageChanged(IPage page)
        {
            UpdateSortingMenu(page);
            UpdateFilterMenuText(page.filters);
            UpdateMenuEnableStatusAndTooltip(page);
        }

        private void OnFiltersChange(PageFiltersChangeArgs args)
        {
            if (!args.page.isActive)
                return;
            var filters = args.page.filters;
            UpdateSortMenuText(filters.sortOption);
            UpdateFilterMenuText(filters);
            UpdateMenuEnableStatusAndTooltip(args.page);
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
                    if (IOUtils.GetFileName(path) != "package.json")
                    {
                        Debug.Log(L10n.Tr("[Package Manager Window] Please select a valid package.json file in a package folder."));
                        return;
                    }

                    if (!m_OperationDispatcher.InstallFromPath(IOUtils.GetParentDirectory(path), out var tempPackageId))
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
            m_PageManager.activePage.SetNewSelection(packageUniqueId, false);
        }

        private void SetupSortingMenu()
        {
            sortingMenu.menu.MenuItems().Clear();
            UIUtils.SetElementDisplay(sortingMenu, false);
        }

        private void SetupFilters()
        {
            filtersMenu.clicked += () =>
            {
                PageFiltersWindow.instance?.Close();

                var page = m_PageManager.activePage;
                if (page == null)
                    return;

                var content = new PageFiltersWindow.Content(m_ResourceLoader, m_PackageDatabase, page);
                var position = EditorMenuExtensions.GUIToScreenRect(filtersMenu, filtersMenu.worldBound);
                if (!PageFiltersWindow.ShowAtPosition(position, content))
                    return;

                filtersMenu.SetActivePseudoState(true);
                content.onFiltersChanged += filters =>
                {
                    if (page.UpdateFilters(filters))
                        PackageManagerFiltersAnalytics.SendEvent(filters);
                };
                content.onClose += () => filtersMenu.SetActivePseudoState(false);
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
