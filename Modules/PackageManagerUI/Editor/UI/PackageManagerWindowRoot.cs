// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    using AssetStoreCachePathConfig = UnityEditorInternal.AssetStoreCachePathManager.CachePathConfig;
    using AssetStoreConfigStatus = UnityEditorInternal.AssetStoreCachePathManager.ConfigStatus;

    internal class PackageManagerWindowRoot : VisualElement, IWindow
    {
        private const string k_SelectedInInspectorClassName = "selectedInInspector";
        public const string k_FocusedClassName = "focus";

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IExtensionManager m_ExtensionManager;
        private readonly ISelectionProxy m_Selection;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnectProxy;
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IUpmClient m_UpmClient;
        private readonly IAssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        private readonly IPageRefreshHandler m_PageRefreshHandler;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IDelayedSelectionHandler m_DelayedSelectionHandler;
        public PackageManagerWindowRoot(IResourceLoader resourceLoader,
            IExtensionManager extensionManager,
            ISelectionProxy selection,
            IPackageManagerPrefs packageManagerPrefs,
            IPackageDatabase packageDatabase,
            IPageManager pageManager,
            IUnityConnectProxy unityConnectProxy,
            IApplicationProxy applicationProxy,
            IUpmClient upmClient,
            IAssetStoreCachePathProxy assetStoreCachePathProxy,
            IPageRefreshHandler pageRefreshHandler,
            IPackageOperationDispatcher packageOperationDispatcher,
            IDelayedSelectionHandler delayedSelectionHandler)
        {
            m_ResourceLoader = resourceLoader;
            m_ExtensionManager = extensionManager;
            m_Selection = selection;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_UnityConnectProxy = unityConnectProxy;
            m_ApplicationProxy = applicationProxy;
            m_UpmClient = upmClient;
            m_AssetStoreCachePathProxy = assetStoreCachePathProxy;
            m_PageRefreshHandler = pageRefreshHandler;
            m_OperationDispatcher = packageOperationDispatcher;
            m_DelayedSelectionHandler = delayedSelectionHandler;
        }

        public void OnEnable()
        {
            styleSheets.Add(m_ResourceLoader.packageManagerWindowStyleSheet);

            var root = m_ResourceLoader.GetTemplate("PackageManagerWindow.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            packageDetails.OnEnable();
            packageSearchBar.OnEnable();
            nonCompliantRegistryMessage.OnEnable();
            signInBar.OnEnable();
            packageList.OnEnable();
            packageManagerToolbar.OnEnable();
            packageStatusbar.OnEnable();
            sidebar.OnEnable();

            RegisterEventsToAdaptFocus();

            globalSplitter.fixedPaneInitialDimension = m_PackageManagerPrefs.sidebarWidth;
            mainContainerSplitter.fixedPaneInitialDimension = m_PackageManagerPrefs.leftContainerWidth;

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

                packageList.HideListShowMessage(L10n.Tr("UPM server is not running"));
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
            if (focus)
                RescanLocalInfos();
        }

        private void OnAssetStoreCacheConfigChange(AssetStoreCachePathConfig config)
        {
            if (config.status == AssetStoreConfigStatus.Success || config.status == AssetStoreConfigStatus.ReadOnly)
                RescanLocalInfos();
        }

        private void RescanLocalInfos()
        {
            if (m_PageRefreshHandler.GetRefreshTimestamp(RefreshOptions.LocalInfo) == 0)
                return;
            m_PageRefreshHandler.SetRefreshTimestampSingleFlag(RefreshOptions.LocalInfo, 0);
            // We don't trigger a refresh right away for all pages because if a non-active page's refresh options contain LocalInfo,
            // a refresh will be triggered once the user switches to that page. This way we delay the refresh to when it's needed.
            // If the user never switches to that page then a local info refresh would never be triggerred.
            if (m_PageManager.activePage.refreshOptions.Contains(RefreshOptions.LocalInfo))
                m_PageRefreshHandler.Refresh(RefreshOptions.LocalInfo);
        }

        public void OnDisable()
        {
            m_PackageManagerPrefs.activePageIdFromLastUnitySession = m_PageManager.activePage.id;

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged -= OnAssetStoreCacheConfigChange;

            packageDetails.OnDisable();
            packageSearchBar.OnDisable();
            nonCompliantRegistryMessage.OnDisable();
            signInBar.OnDisable();
            packageList.OnDisable();
            packageManagerToolbar.OnDisable();
            packageStatusbar.OnDisable();
            sidebar.OnDisable();

            EditorApplication.focusChanged -= OnFocusChanged;
            m_Selection.onSelectionChanged -= RefreshSelectedInInspectorClass;

            m_PackageManagerPrefs.sidebarWidth = sidebar.layout.width;
            m_PackageManagerPrefs.leftContainerWidth = leftColumnContainer.layout.width;
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

        public void OnFocus()
        {
            AddToClassList(k_FocusedClassName);
        }

        public void OnLostFocus()
        {
            rightContainer.RemoveFromClassList(k_FocusedClassName);
            sidebar.RemoveFromClassList(k_FocusedClassName);
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

        public AddPackageByNameDropdown OpenAddPackageByNameDropdown(string url, EditorWindow anchorWindow)
        {
            var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_UpmClient, m_PackageDatabase, m_PageManager, m_OperationDispatcher, m_ApplicationProxy, anchorWindow);

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
            // We use DelayedSelectionHandler to handle the case where the package is not yet available when the
            // selection is set. That could happen when we want to open Package Manager and select a package, but
            // the refresh call is not yet finished. It could also happen when we create a package and the newly
            // crated package is not yet in the database until after package resolution.
            m_DelayedSelectionHandler.SelectPackage(identifier);
        }

        public PackageSelectionArgs activeSelection
        {
            get
            {
                var selections = m_PageManager.activePage.GetSelection();

                // When there are multiple versions selected, we want to make the legacy single select arguments to be null
                // that way extension UI implemented for single package selection will not show for multi-select cases.
                var packages = selections.Select(selection => m_PackageDatabase.GetPackage(selection)).ToArray();
                var package = packages.Length > 1 ? null : packages.FirstOrDefault();
                return new PackageSelectionArgs { package = package, packageVersion = package?.versions.primary, packages = packages, window = this };
            }
        }

        public IMenu addMenu => packageManagerToolbar.addMenu;
        public IMenu advancedMenu => packageManagerToolbar.toolbarSettingsMenu;

        private VisualElementCache cache { set; get; }

        public PackageSearchBar packageSearchBar => cache.Get<PackageSearchBar>("packageSearchBar");
        public PartiallyNonCompliantRegistryMessage nonCompliantRegistryMessage => cache.Get<PartiallyNonCompliantRegistryMessage>("partiallyNonCompliantRegistryMessage");
        public SignInBar signInBar => cache.Get<SignInBar>("signInBar");
        public PackageList packageList => cache.Get<PackageList>("packageList");
        public PackageDetails packageDetails => cache.Get<PackageDetails>("packageDetails");
        public PackageManagerToolbar packageManagerToolbar => cache.Get<PackageManagerToolbar>("topMenuToolbar");
        public PackageStatusBar packageStatusbar => cache.Get<PackageStatusBar>("packageStatusBar");
        private VisualElement leftColumnContainer => cache.Get<VisualElement>("leftColumnContainer");
        private VisualElement rightContainer => cache.Get<VisualElement>("rightSideContainer");
        private Sidebar sidebar => cache.Get<Sidebar>("sidebar");
        private TwoPaneSplitView globalSplitter => cache.Get<TwoPaneSplitView>("globalSplitter");
        private TwoPaneSplitView mainContainerSplitter => cache.Get<TwoPaneSplitView>("mainContainer");
    }
}
