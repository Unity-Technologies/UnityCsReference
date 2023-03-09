// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    using AssetStoreCachePathConfig = UnityEditorInternal.AssetStoreCachePathManager.CachePathConfig;
    using AssetStoreConfigStatus = UnityEditorInternal.AssetStoreCachePathManager.ConfigStatus;

    internal class PackageManagerWindowRoot : VisualElement, IWindow
    {
        private PackageAndPageSelectionArgs m_PendingPackageAndPageSelectionArgs;

        private const string k_SelectedInInspectorClassName = "selectedInInspector";
        public const string k_FocusedClassName = "focus";

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
        private SelectionProxy m_Selection;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private UnityConnectProxy m_UnityConnectProxy;
        private ApplicationProxy m_ApplicationProxy;
        private UpmClient m_UpmClient;
        private AssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        private PageRefreshHandler m_PageRefreshHandler;

        private void ResolveDependencies(ResourceLoader resourceLoader,
            ExtensionManager extensionManager,
            SelectionProxy selection,
            PackageManagerPrefs packageManagerPrefs,
            PackageDatabase packageDatabase,
            PageManager pageManager,
            PackageManagerProjectSettingsProxy settingsProxy,
            UnityConnectProxy unityConnectProxy,
            ApplicationProxy applicationProxy,
            UpmClient upmClient,
            AssetStoreCachePathProxy assetStoreCachePathProxy,
            PageRefreshHandler pageRefreshHandler)
        {
            m_ResourceLoader = resourceLoader;
            m_ExtensionManager = extensionManager;
            m_Selection = selection;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_UnityConnectProxy = unityConnectProxy;
            m_ApplicationProxy = applicationProxy;
            m_UpmClient = upmClient;
            m_AssetStoreCachePathProxy = assetStoreCachePathProxy;
            m_PageRefreshHandler = pageRefreshHandler;
        }

        public PackageManagerWindowRoot(ResourceLoader resourceLoader,
                                        ExtensionManager extensionManager,
                                        SelectionProxy selection,
                                        PackageManagerPrefs packageManagerPrefs,
                                        PackageDatabase packageDatabase,
                                        PageManager pageManager,
                                        PackageManagerProjectSettingsProxy settingsProxy,
                                        UnityConnectProxy unityConnectProxy,
                                        ApplicationProxy applicationProxy,
                                        UpmClient upmClient,
                                        AssetStoreCachePathProxy assetStoreCachePathProxy,
                                        PageRefreshHandler pageRefreshHandler)
        {
            ResolveDependencies(resourceLoader, extensionManager, selection, packageManagerPrefs, packageDatabase, pageManager, settingsProxy, unityConnectProxy, applicationProxy, upmClient, assetStoreCachePathProxy, pageRefreshHandler);
        }

        public void OnEnable()
        {
            styleSheets.Add(m_ResourceLoader.packageManagerWindowStyleSheet);

            var root = m_ResourceLoader.GetTemplate("PackageManagerWindow.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            packageDetails.OnEnable();
            packageSearchBar.OnEnable();
            packageList.OnEnable();
            packageManagerToolbar.OnEnable();
            packageStatusbar.OnEnable();
            sidebar.OnEnable();

            RegisterEventsToAdaptFocus();

            leftColumnContainer.style.flexGrow = m_PackageManagerPrefs.splitterFlexGrow;
            rightColumnContainer.style.flexGrow = 1 - m_PackageManagerPrefs.splitterFlexGrow;

            m_PageRefreshHandler.onRefreshOperationFinish += OnRefreshOperationFinish;
            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCachePathProxy.onConfigChanged += OnAssetStoreCacheConfigChange;

            EditorApplication.focusChanged += OnFocusChanged;
            m_Selection.onSelectionChanged += RefreshSelectedInInspectorClass;

            focusable = true;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RefreshSelectedInInspectorClass();
        }

        public void OnCreateGUI()
        {
            // Make sure extensions are added first so that the code following can find the registered extensions just fine
            m_ExtensionManager.OnWindowCreated(this, packageDetails.extensionContainer, packageDetails.toolbar.extensions);

            var pageFromLastUnitySession = m_PageManager.GetPage(m_PackageManagerPrefs.activePageIdFromLastUnitySession);
            if (pageFromLastUnitySession != null)
            {
                // Reset the lock icons when users open a new Package Manager window
                pageFromLastUnitySession.ResetUserUnlockedState();

                // set the current page value after all the callback system has been setup so that we don't miss any callbacks
                m_PageManager.activePage = pageFromLastUnitySession;
            }

            var activePage = m_PageManager.activePage;
            if (m_PageRefreshHandler.GetRefreshTimestamp(activePage) == 0)
                DelayRefresh(activePage);

            if (activePage.id != UnityRegistryPage.k_Id && m_ApplicationProxy.isUpmRunning)
            {
                var unityRegistryPage = m_PageManager.GetPage(UnityRegistryPage.k_Id);
                if (m_PageRefreshHandler.GetRefreshTimestamp(unityRegistryPage) == 0)
                    DelayRefresh(unityRegistryPage);
            }

            packageDetails.OnCreateGUI();
            sidebar.OnCreateGUI();
        }

        private void DelayRefresh(IPage page)
        {
            if (!m_ApplicationProxy.isUpmRunning)
            {
                if (!m_ApplicationProxy.isBatchMode)
                    Debug.Log(L10n.Tr("[Package Manager Window] UPM server is not running. Please check that your Editor was not launched with '-noUpm' command line option."));

                packageList.HideListShowEmptyArea(L10n.Tr("UPM server is not running"));
                packageStatusbar.DisableRefresh();
                return;
            }

            if (page.id == MyAssetsPage.k_Id &&
                (m_PackageManagerPrefs.numItemsPerPage == null || !m_UnityConnectProxy.isUserInfoReady))
            {
                EditorApplication.delayCall += () => DelayRefresh(page);
                return;
            }

            m_PageRefreshHandler.Refresh(page);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
            packageList.Focus();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
        }

        private void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (evt.commandName == EventCommandNames.Find)
                evt.StopPropagation();
        }

        private void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            if (evt.commandName == EventCommandNames.Find)
            {
                packageSearchBar.FocusOnSearchField();
                evt.StopPropagation();
            }
        }

        private void OnFocusChanged(bool focus)
        {
            var canRefresh = !EditorApplication.isPlaying && !EditorApplication.isCompiling;
            if (focus && canRefresh &&
                m_PageManager.activePage.id is MyAssetsPage.k_Id or InProjectPage.k_Id)
                m_PageRefreshHandler.Refresh(RefreshOptions.PurchasedOffline);
        }

        public void OnDisable()
        {
            m_PackageManagerPrefs.activePageIdFromLastUnitySession = m_PageManager.activePage.id;

            m_PageRefreshHandler.onRefreshOperationFinish -= OnRefreshOperationFinish;
            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged -= OnAssetStoreCacheConfigChange;

            packageDetails.OnDisable();
            packageSearchBar.OnDisable();
            packageList.OnDisable();
            packageManagerToolbar.OnDisable();
            packageStatusbar.OnDisable();
            sidebar.OnDisable();

            EditorApplication.focusChanged -= OnFocusChanged;
            m_Selection.onSelectionChanged -= RefreshSelectedInInspectorClass;

            m_PackageManagerPrefs.splitterFlexGrow = leftColumnContainer.resolvedStyle.flexGrow;
        }

        private void OnAssetStoreCacheConfigChange(AssetStoreCachePathConfig config)
        {
            if ((config.status == AssetStoreConfigStatus.Success || config.status == AssetStoreConfigStatus.ReadOnly) && m_PageRefreshHandler.GetRefreshTimestamp(m_PageManager.GetPage(MyAssetsPage.k_Id)) > 0)
                m_PageRefreshHandler.Refresh(RefreshOptions.PurchasedOffline);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!userInfoReady || m_PackageDatabase.isEmpty || !m_PageRefreshHandler.IsInitialFetchingDone(m_PageManager.activePage))
                return;

            var entitlements = m_PackageDatabase.allPackages.Where(package =>  package.hasEntitlements);
            if (loggedIn)
            {
                if (entitlements.Any(package => (package.versions?.primary.isInstalled ?? false) && (package.versions?.primary.hasEntitlementsError ?? false)))
                    m_UpmClient.Resolve();
                else
                {
                    m_PageRefreshHandler.Refresh(RefreshOptions.UpmList | RefreshOptions.UpmSearch);
                    m_PageManager.activePage.TriggerOnSelectionChanged();
                }
            }
            else
            {
                if (entitlements.Any())
                {
                    m_PageRefreshHandler.Refresh(RefreshOptions.UpmList | RefreshOptions.UpmSearch);
                    m_PageManager.activePage.TriggerOnSelectionChanged();
                }
            }
        }

        public void OnDestroy()
        {
            m_ExtensionManager.OnWindowDestroy();
            m_PageManager.OnWindowDestroy();
            LoadingSpinner.ClearAllSpinners();
        }

        private void OnRefreshOperationFinish()
        {
            if (m_PendingPackageAndPageSelectionArgs == null || m_PageRefreshHandler.GetRefreshTimestamp(m_PendingPackageAndPageSelectionArgs.page) <= 0)
                return;

            if (ApplyPackageAndPageSelection(m_PendingPackageAndPageSelectionArgs))
                m_PendingPackageAndPageSelectionArgs = null;
        }

        // Returns true if the selection is applied
        private bool ApplyPackageAndPageSelection(PackageAndPageSelectionArgs args)
        {
            if (args == null)
                return false;

            if (args.page.id == MyAssetsPage.k_Id)
            {
                m_PageManager.activePage = args.page;
                args.page.Load(args.packageToSelect);
                return true;
            }

            // The !IsInitialFetchingDone check was added to the start of this function in the past for the Entitlement Error checker,
            // But it caused `Open In Unity` to not work sometimes for the `My Assets` page. Hence we moved the check from the beginning
            // of this function to after the `My Assets` logic is done so that we don't break `My Assets` and the Entitlement Error checker.
            if (!m_PageRefreshHandler.IsInitialFetchingDone(m_PageManager.activePage))
                return false;

            m_PageManager.activePage = args.page;
            if (!string.IsNullOrEmpty(args.packageToSelect))
            {
                m_PackageDatabase.GetPackageAndVersionByIdOrName(args.packageToSelect, out var package, out var version, true);
                m_PageManager.activePage.SetNewSelection(package, version, true);
                packageList.OnFocus();
            }
            return true;
        }

        public void OnFocus()
        {
            if (!rightContainer.classList.Contains(k_FocusedClassName) && !sidebar.classList.Contains(k_FocusedClassName))
                rightContainer.AddToClassList(k_FocusedClassName);
            AddToClassList(k_FocusedClassName);
        }

        public void OnLostFocus()
        {
            RemoveFromClassList(k_FocusedClassName);
        }

        private void RegisterEventsToAdaptFocus()
        {
            // We have to use PointerDownEvent instead of MouseDownEvent because in some cases (i.e. selectable text fields)
            // the event won't reach our code. PointerDownEvent will always be triggered on click which guarantees full support.
            rightContainer.RegisterCallback<PointerDownEvent>(e =>
            {
                rightContainer.AddToClassList(k_FocusedClassName);
                sidebar.RemoveFromClassList(k_FocusedClassName);
            }, TrickleDown.TrickleDown);

            sidebar.RegisterCallback<PointerDownEvent>(e =>
            {
                rightContainer.RemoveFromClassList(k_FocusedClassName);
                sidebar.AddToClassList(k_FocusedClassName);
            }, TrickleDown.TrickleDown);
        }

        private void RefreshSelectedInInspectorClass()
        {
            if (m_Selection.activeObject is PackageSelectionObject)
                AddToClassList(k_SelectedInInspectorClassName);
            else
                RemoveFromClassList(k_SelectedInInspectorClassName);
        }

        public void SelectPackageAndPage(string packageToSelect = null, string pageId = null, bool refresh = false, string searchText = null)
        {
            if (string.IsNullOrEmpty(packageToSelect) && string.IsNullOrEmpty(pageId))
                return;

            var args = new PackageAndPageSelectionArgs
            {
                packageToSelect = packageToSelect,
                page = m_PageManager.GetPage(pageId)
            };
            if (args.page == null)
            {
                m_PackageDatabase.GetPackageAndVersionByIdOrName(packageToSelect, out var package, out var version, true);
                if (package != null)
                    args.page = m_PageManager.FindPage(package, version) ?? m_PageManager.activePage;
                else
                {
                    var packageToSelectSplit = packageToSelect.Split('@');
                    var versionString = packageToSelectSplit.Length == 2 ? packageToSelectSplit[1] : string.Empty;

                    // Package is not found in PackageDatabase but we can determine if it's a preview package or not with it's version string.
                    SemVersionParser.TryParse(versionString, out var semVersion);
                    if (!m_SettingsProxy.enablePreReleasePackages && semVersion.HasValue && (semVersion.Value.Major == 0 || semVersion.Value.Prerelease.StartsWith("preview")))
                    {
                        Debug.Log("You must check \"Enable Preview Packages\" in Project Settings > Package Manager in order to see this package.");
                        args.page = m_PageManager.activePage;
                        args.packageToSelect = null;
                    }
                    else
                        args.page = m_PageManager.GetPage(UnityRegistryPage.k_Id);
                }
            }
            if (args.page != null && (!string.IsNullOrEmpty(searchText) || !string.IsNullOrEmpty(packageToSelect)))
                args.page.searchText = searchText;

            if (refresh || m_PackageDatabase.isEmpty)
            {
                DelayRefresh(args.page);
                m_PendingPackageAndPageSelectionArgs = args;
            }
            else if (!ApplyPackageAndPageSelection(args))
                m_PendingPackageAndPageSelectionArgs = args;
        }

        public AddPackageByNameDropdown OpenAddPackageByNameDropdown(string url)
        {
            var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_UpmClient, m_PackageDatabase, m_PageManager, PackageManagerWindow.instance);

            var packageNameAndVersion = url.Replace(PackageManagerWindow.k_UpmUrl, string.Empty);
            var packageName = string.Empty;
            var packageVersion = string.Empty;

            if (packageNameAndVersion.Contains("@"))
            {
                var values = packageNameAndVersion.Split('@');
                if (values.Count() > 1)
                {
                    packageName = values[0];
                    packageVersion = values[1];
                }
            }
            else
                packageName = packageNameAndVersion;

            DropdownElement.ShowDropdown(this, dropdown);

            // We need to set the name and version after the dropdown is shown,
            // so that the OnTextFieldChange of placeholder gets called
            dropdown.packageNameField.value = packageName;
            dropdown.packageVersionField.value = packageVersion;
            return dropdown;
        }

        public IDetailsExtension AddDetailsExtension()
        {
            return m_ExtensionManager.CreateDetailsExtension();
        }

        public IPackageActionMenu AddPackageActionMenu()
        {
            return m_ExtensionManager.CreatePackageActionMenu();
        }

        public IPackageActionButton AddPackageActionButton()
        {
            return m_ExtensionManager.CreatePackageActionButton();
        }

        public void Select(string identifier)
        {
            SelectPackageAndPage(identifier);
        }

        public PackageSelectionArgs activeSelection
        {
            get
            {
                var selections = m_PageManager.activePage.GetSelection();

                // When there are multiple versions selected, we want to make the legacy single select arguments to be null
                // that way extension UI implemented for single package selection will not show for multi-select cases.
                var versions = selections.Select(selection =>
                {
                    m_PackageDatabase.GetPackageAndVersion(selection, out var package, out var version);
                    return version ?? package?.versions.primary;
                }).ToArray();

                var version = versions.Length > 1 ? null : versions.FirstOrDefault();
                return new PackageSelectionArgs { package = version?.package, packageVersion = version, versions = versions, window = this };
            }
        }

        public IMenu addMenu => packageManagerToolbar.addMenu;
        public IMenu advancedMenu => packageManagerToolbar.toolbarSettingsMenu;

        private VisualElementCache cache { set; get; }

        public PackageSearchBar packageSearchBar => cache.Get<PackageSearchBar>("packageSearchBar");
        public PackageList packageList => cache.Get<PackageList>("packageList");
        public PackageDetails packageDetails => cache.Get<PackageDetails>("packageDetails");
        public PackageManagerToolbar packageManagerToolbar => cache.Get<PackageManagerToolbar>("topMenuToolbar");
        public PackageStatusBar packageStatusbar => cache.Get<PackageStatusBar>("packageStatusBar");
        private VisualElement leftColumnContainer => cache.Get<VisualElement>("leftColumnContainer");
        private VisualElement rightColumnContainer => cache.Get<VisualElement>("rightColumnContainer");
        private VisualElement rightContainer => cache.Get<VisualElement>("rightSideContainer");
        private Sidebar sidebar => cache.Get<Sidebar>("sidebar");
    }

    internal class PackageAndPageSelectionArgs
    {
        public IPage page;
        public string packageToSelect;
    }
}
