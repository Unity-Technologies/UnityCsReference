// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    using AssetStoreCachePathConfig = UnityEditorInternal.AssetStoreCachePathManager.CachePathConfig;
    using AssetStoreConfigStatus = UnityEditorInternal.AssetStoreCachePathManager.ConfigStatus;

    internal sealed class PackageManagerWindowRoot : VisualElement, IWindow
    {
        public const string k_FocusedClassName = "focus";

        private readonly IExtensionManager m_ExtensionManager;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnectProxy;
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IUpmClient m_UpmClient;
        private readonly IAssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        private readonly IPageRefreshHandler m_PageRefreshHandler;
        private readonly IDelayedSelectionHandler m_DelayedSelectionHandler;
        private readonly IDropdownHandler m_DropdownHandler;

        public PackageManagerWindowRoot(IResourceLoader resourceLoader,
            IExtensionManager extensionManager,
            IPackageManagerPrefs packageManagerPrefs,
            IPackageDatabase packageDatabase,
            IPageManager pageManager,
            IUnityConnectProxy unityConnectProxy,
            IApplicationProxy applicationProxy,
            IUpmClient upmClient,
            IAssetStoreCachePathProxy assetStoreCachePathProxy,
            IPageRefreshHandler pageRefreshHandler,
            IDelayedSelectionHandler delayedSelectionHandler,
            IDropdownHandler dropdownHandler)
        {
            m_ExtensionManager = extensionManager;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_UnityConnectProxy = unityConnectProxy;
            m_ApplicationProxy = applicationProxy;
            m_UpmClient = upmClient;
            m_AssetStoreCachePathProxy = assetStoreCachePathProxy;
            m_PageRefreshHandler = pageRefreshHandler;
            m_DelayedSelectionHandler = delayedSelectionHandler;
            m_DropdownHandler = dropdownHandler;

            styleSheets.Add(resourceLoader.packageManagerWindowStyleSheet);
            var root = resourceLoader.GetTemplate("PackageManagerWindow.uxml");
            Add(root);
            cache = new VisualElementCache(root);
            focusable = true;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_ExtensionManager.OnWindowCreated(this, detailArea.extensionContainer, detailArea.toolbarExtensions, detailArea.legacyExtensionContainer);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterEventsToAdaptFocus();

            globalSplitter.fixedPaneInitialDimension = m_PackageManagerPrefs.sidebarWidth;
            mainContainerSplitter.fixedPaneInitialDimension = m_PackageManagerPrefs.leftContainerWidth;

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            m_AssetStoreCachePathProxy.onConfigChanged += OnAssetStoreCacheConfigChange;

            m_ApplicationProxy.focusChanged += OnFocusChanged;

            m_PageManager.onSelectionChanged += OnPageSelectionChange;
            RefreshMainContainerContent();

            var pageFromLastUnitySession = m_PageManager.GetPage(m_PackageManagerPrefs.activePageIdFromLastUnitySession);
            if (pageFromLastUnitySession != null)
                m_PageManager.activePage = pageFromLastUnitySession;

            var activePage = m_PageManager.activePage;
            if (m_PageRefreshHandler.GetRefreshTimestamp(activePage) == 0)
                DelayRefresh(activePage);

            if (activePage.id != UnityRegistryPage.k_Id && m_ApplicationProxy.isUpmRunning)
            {
                var unityRegistryPage = m_PageManager.GetPage(UnityRegistryPage.k_Id);
                if (m_PageRefreshHandler.GetRefreshTimestamp(unityRegistryPage) == 0)
                    DelayRefresh(unityRegistryPage);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_PackageManagerPrefs.activePageIdFromLastUnitySession = m_PageManager.activePage.id;

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged -= OnAssetStoreCacheConfigChange;

            m_ApplicationProxy.focusChanged -= OnFocusChanged;
            m_PageManager.onSelectionChanged -= OnPageSelectionChange;

            m_PackageManagerPrefs.sidebarWidth = sidebar.layout.width;
            m_PackageManagerPrefs.leftContainerWidth = leftColumnContainer.layout.width;
        }

        private void DelayRefresh(IPage page)
        {
            if (!m_ApplicationProxy.isUpmRunning)
            {
                if (!m_ApplicationProxy.isBatchMode)
                    Debug.Log(L10n.Tr("[Package Manager Window] UPM server is not running. Please check that your Editor was not launched with '-noUpm' command line option."));

                listArea.HideListShowMessage(L10n.Tr("UPM server is not running"));
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

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!userInfoReady || m_PackageDatabase.allPackages.Count == 0 || !m_PageRefreshHandler.IsInitialFetchingDone(m_PageManager.activePage))
                return;

            if (loggedIn)
            {
                if (m_PackageDatabase.allPackages.AnyMatches(p => p is { hasEntitlements: true } && p.versions.primary is { isInstalled: true, hasEntitlementsError: true }))
                    m_UpmClient.Resolve();
                else
                {
                    m_PageRefreshHandler.Refresh(RefreshOptions.UpmList | RefreshOptions.UpmSearch);
                    m_PageManager.activePage.TriggerOnSelectionChanged(false);
                }
            }
            else
            {
                if (m_PackageDatabase.allPackages.AnyMatches(p => p is { hasEntitlements: true }))
                {
                    m_PageRefreshHandler.Refresh(RefreshOptions.UpmList | RefreshOptions.UpmSearch);
                    m_PageManager.activePage.TriggerOnSelectionChanged(false);
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
            mainContainerSplitter.RemoveFromClassList(k_FocusedClassName);
            sidebar.RemoveFromClassList(k_FocusedClassName);
            RemoveFromClassList(k_FocusedClassName);
        }

        private void RegisterEventsToAdaptFocus()
        {
            // We have to use PointerDownEvent instead of MouseDownEvent because in some cases (i.e. selectable text fields)
            // the event won't reach our code. PointerDownEvent will always be triggered on click which guarantees full support.
            mainContainerSplitter.RegisterCallback<PointerDownEvent>(_ =>
            {
                mainContainerSplitter.AddToClassList(k_FocusedClassName);
                sidebar.RemoveFromClassList(k_FocusedClassName);
            }, TrickleDown.TrickleDown);

            sidebar.RegisterCallback<PointerDownEvent>(_ =>
            {
                mainContainerSplitter.RemoveFromClassList(k_FocusedClassName);
                sidebar.AddToClassList(k_FocusedClassName);
            }, TrickleDown.TrickleDown);
        }

        public void OpenAddPackageByNameDropdown(string technicalName, string version)
        {
            m_DropdownHandler.ShowAddPackageByNameDropdown(packageManagerToolbar.addMenu, technicalName, version);
        }

        public void OpenCreatePackageDropdown()
        {
            m_DropdownHandler.ShowCreatePackageDropdown(packageManagerToolbar.addMenu);
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
                var packages = selections.SelectToNewArray(selection => m_PackageDatabase.GetPackage(selection));
                var package = packages.Length == 1 ? packages[0] : null;
                return new PackageSelectionArgs { package = package, packageVersion = package?.versions.primary, packages = packages, window = this };
            }
        }

        private void OnPageSelectionChange(PageSelectionChangeArgs _)
        {
            RefreshMainContainerContent();
        }

        private void RefreshMainContainerContent()
        {
            var page = m_PageManager.activePage;
            var isOverlayVisible = page.scopedRegistry?.compliance.status == RegistryComplianceStatus.NonCompliant;
            UIUtils.SetElementDisplay(mainContainerOverlay, isOverlayVisible);
            UIUtils.SetElementDisplay(mainContainerSplitter, !isOverlayVisible);

            if (!isOverlayVisible)
                return;

            mainContainerOverlay.titleLabel.text = page.scopedRegistry.name;

            var violation = page.scopedRegistry.compliance.violations[0];
            mainContainerOverlay.extendedHelpBox.customIcon = Icon.PackageErrorLarge;
            mainContainerOverlay.extendedHelpBox.text = violation?.message ?? string.Empty;
            mainContainerOverlay.extendedHelpBox.readMoreUrl = violation?.readMoreLink;
            mainContainerOverlay.extendedHelpBox.analyticsId = "non-compliant-registry-help-box";
        }

        public IMenu addMenu => packageManagerToolbar.addMenu;
        public IMenu advancedMenu => packageManagerToolbar.toolbarSettingsMenu;

        private VisualElementCache cache { set; get; }

        public ListArea listArea => cache.Get<ListArea>("listArea");
        public PackageManagerToolbar packageManagerToolbar => cache.Get<PackageManagerToolbar>("topMenuToolbar");
        public PackageStatusBar packageStatusbar => cache.Get<PackageStatusBar>("packageStatusBar");
        private DetailsArea detailArea => cache.Get<DetailsArea>("detailsArea");
        private VisualElement leftColumnContainer => cache.Get<VisualElement>("leftColumnContainer");
        private Sidebar sidebar => cache.Get<Sidebar>("sidebar");
        private TwoPaneSplitView globalSplitter => cache.Get<TwoPaneSplitView>("globalSplitter");
        private TwoPaneSplitView mainContainerSplitter => cache.Get<TwoPaneSplitView>("mainContainerSplitter");
        private MainContainerOverlay mainContainerOverlay => cache.Get<MainContainerOverlay>("mainContainerOverlay");
    }
}
