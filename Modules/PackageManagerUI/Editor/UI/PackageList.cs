// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageList : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageList();
        }

        private IResourceLoader m_ResourceLoader;
        private IUnityConnectProxy m_UnityConnect;
        private IPackageManagerPrefs m_PackageManagerPrefs;
        private IPageManager m_PageManager;
        private IUpmCache m_UpmCache;
        private IBackgroundFetchHandler m_BackgroundFetchHandler;
        private IProjectSettingsProxy m_SettingsProxy;
        private IPageRefreshHandler m_PageRefreshHandler;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_UnityConnect = container.Resolve<IUnityConnectProxy>();
            m_PackageManagerPrefs = container.Resolve<IPackageManagerPrefs>();
            m_PageManager = container.Resolve<IPageManager>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_BackgroundFetchHandler = container.Resolve<IBackgroundFetchHandler>();
            m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
            m_PageRefreshHandler = container.Resolve<IPageRefreshHandler>();
        }

        private Action m_ButtonAction;

        public PackageList()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            emptyAreaButton.clickable.clicked += OnButtonClicked;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            focusable = true;
        }

        public void OnEnable()
        {
            m_PageRefreshHandler.onRefreshOperationStart += OnRefreshOperationStartOrFinish;
            m_PageRefreshHandler.onRefreshOperationFinish += OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange += OnVisualStateChange;
            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_BackgroundFetchHandler.onCheckUpdateProgress += OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            listView.OnEnable();
            packageLoadBar.OnEnable();

            if (!Unsupported.IsDeveloperBuild() && m_SettingsProxy.seeAllPackageVersions)
            {
                m_SettingsProxy.seeAllPackageVersions = false;
                m_SettingsProxy.Save();
            }

            // manually build the items on initialization to refresh the UI
            OnListRebuild(m_PageManager.activePage);
        }

        public void OnDisable()
        {
            m_PageRefreshHandler.onRefreshOperationStart -= OnRefreshOperationStartOrFinish;
            m_PageRefreshHandler.onRefreshOperationFinish -= OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange -= OnVisualStateChange;
            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_BackgroundFetchHandler.onCheckUpdateProgress -= OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            listView.OnDisable();
            packageLoadBar.OnDisable();
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            if (panel == null)
                return;
            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            RegisterCallback<KeyDownEvent>(IgnoreEscapeKeyDown, TrickleDown.TrickleDown);
            panel.visualTree.RegisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (panel == null)
                return;
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            UnregisterCallback<KeyDownEvent>(IgnoreEscapeKeyDown, TrickleDown.TrickleDown);
            panel.visualTree.UnregisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            currentView.OnKeyDownShortcut(evt);
        }

        private void OnNavigationMoveShortcut(NavigationMoveEvent evt)
        {
            currentView.OnNavigationMoveShortcut(evt);
        }

        // The default ListView escape key behaviour is to clear all selections, however, we want to always have something selected
        // therefore we register a TrickleDown callback handler to intercept escape key events to make it do nothing.
        private void IgnoreEscapeKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            var currentView = this.currentView;
            foreach (var item in args.selection.previousSelections.Where(s => !args.selection.Contains(s.packageUniqueId)).Concat(args.selection))
                currentView.GetPackageItem(item.packageUniqueId)?.RefreshSelection();

            if (args.selection.previousSelections.Count() == 1)
                m_UpmCache.SetLoadAllVersions(args.selection.previousSelections.FirstOrDefault().packageUniqueId, false);
        }

        private void OnCheckUpdateProgress()
        {
            UpdateListVisibility(true);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            var page = m_PageManager.activePage;
            if (page.id == MyAssetsPage.k_Id && UpdateListVisibility())
                page.UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        private void HideListShowMessage(bool isRefreshInProgress, bool isCheckUpdateInProgress)
        {
            string message;
            if (isRefreshInProgress)
            {
                message = L10n.Tr("Refreshing list...");
            }
            else if (isCheckUpdateInProgress)
            {
                message = string.Format(L10n.Tr("Checking for updates {0}%..."), m_BackgroundFetchHandler.checkUpdatePercentage);
                HideListShowEmptyArea(message, L10n.Tr("Cancel"), () =>
                {
                    m_PageManager.activePage.ClearFilters();
                    m_BackgroundFetchHandler.CancelCheckUpdates();
                });
                return;
            }
            else
            {
                var page = m_PageManager.activePage;
                var searchText = page.searchText;
                if (string.IsNullOrEmpty(searchText))
                    message = page.filters.isFilterSet ? L10n.Tr("No results for the specified filters.") : L10n.Tr("No items to display.");
                else
                {
                    const int maxSearchTextToDisplay = 64;
                    if (searchText?.Length > maxSearchTextToDisplay)
                        searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                    message = string.Format(L10n.Tr("No results for \"{0}\""), searchText);
                }
            }
            HideListShowEmptyArea(message);
        }

        private void HideListShowLogin()
        {
            if (!m_UnityConnect.isUserInfoReady)
                HideListShowEmptyArea(string.Empty);
            else
                HideListShowEmptyArea(L10n.Tr("Sign in to access your assets"), L10n.Tr("Sign in"), m_UnityConnect.ShowLogin);
        }

        public void HideListShowEmptyArea(string message, string buttonText = null, Action buttonAction = null)
        {
            UIUtils.SetElementDisplay(listContainer, false);
            UIUtils.SetElementDisplay(packageLoadBar, false);
            UIUtils.SetElementDisplay(emptyArea, true);

            emptyAreaMessage.text = message ?? string.Empty;

            emptyAreaButton.text = buttonText ?? string.Empty;
            m_ButtonAction = buttonAction;
            UIUtils.SetElementDisplay(emptyAreaButton, buttonAction != null);

            m_PageManager.activePage.SetNewSelection(Enumerable.Empty<PackageAndVersionIdPair>());
        }

        private void HideEmptyAreaShowList(bool skipListRebuild)
        {
            var rebuild = !skipListRebuild && !UIUtils.IsElementVisible(listContainer);
            UIUtils.SetElementDisplay(listContainer, true);
            UIUtils.SetElementDisplay(emptyArea, false);

            var currentView = this.currentView;
            UIUtils.SetElementDisplay(listView, listView == currentView);
            UIUtils.SetElementDisplay(scrollView, scrollView == currentView);

            packageLoadBar.UpdateVisibility();

            var page = m_PageManager.activePage;
            var selection = page.GetSelection();
            if (!selection.Any() && selection.previousSelections.Any())
                page.SetNewSelection(selection.previousSelections);

            if (rebuild)
                currentView.OnListRebuild(page);
        }

        // Returns true if the list of packages is visible (either listView or scrollView), false otherwise
        private bool UpdateListVisibility(bool skipListRebuild = false)
        {
            var page = m_PageManager.activePage;
            if (page.id == MyAssetsPage.k_Id && !m_UnityConnect.isUserLoggedIn)
            {
                HideListShowLogin();
                return false;
            }

            var isListEmpty = !page.visualStates.Any(v => v.visible);
            var isInitialFetchingDone = m_PageRefreshHandler.IsInitialFetchingDone(page);
            var isCheckUpdateInProgress = page.id == MyAssetsPage.k_Id &&
                                          m_BackgroundFetchHandler.isCheckUpdateInProgress && page.filters.status == PageFilters.Status.UpdateAvailable;
            if (isListEmpty || !isInitialFetchingDone || isCheckUpdateInProgress)
            {
                HideListShowMessage(m_PageRefreshHandler.IsRefreshInProgress(page), isCheckUpdateInProgress);
                return false;
            }

            HideEmptyAreaShowList(skipListRebuild);
            return true;
        }

        private void OnButtonClicked()
        {
            m_ButtonAction?.Invoke();
        }

        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            const int heightCalculationBuffer = 4;
            var containerHeight = resolvedStyle.height;
            if (float.IsNaN(containerHeight))
                return;
            var numItems = ((int)containerHeight - heightCalculationBuffer - PackageLoadBar.k_FixedHeight) / PackageItem.k_MainItemHeight;
            m_PackageManagerPrefs.numItemsPerPage = numItems <= 0 ? null : numItems;
        }

        private void OnRefreshOperationStartOrFinish()
        {
            if (UpdateListVisibility())
                m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        internal void OnFocus()
        {
            currentView.ScrollToSelection();
        }

        private void OnVisualStateChange(VisualStateChangeArgs args)
        {
            if (args.page.isActivePage && UpdateListVisibility())
                currentView.OnVisualStateChange(args.visualStates);
        }

        private void OnListRebuild(IPage page)
        {
            if (page.isActivePage && UpdateListVisibility(true))
                currentView.OnListRebuild(page);
        }

        private void OnListUpdate(ListUpdateArgs args)
        {
            if (args.page.isActivePage && UpdateListVisibility())
                currentView.OnListUpdate(args);
        }

        private void OnActivePageChanged(IPage page)
        {
            if (m_PageManager.lastActivePage != null)
                m_PageRefreshHandler.CancelRefresh(m_PageManager.lastActivePage.refreshOptions);

            if (UpdateListVisibility())
                currentView.OnActivePageChanged(page);
        }

        internal IPackageListView currentView => m_PageManager.activePage.id == MyAssetsPage.k_Id ? listView : scrollView;

        private VisualElementCache cache { get; }
        private PackageListView listView => cache.Get<PackageListView>("listView");
        private PackageListScrollView scrollView => cache.Get<PackageListScrollView>("scrollView");
        private VisualElement listContainer => cache.Get<VisualElement>("listContainer");
        private PackageLoadBar packageLoadBar => cache.Get<PackageLoadBar>("packageLoadBar");
        private VisualElement emptyArea => cache.Get<VisualElement>("emptyArea");
        private Label emptyAreaMessage => cache.Get<Label>("emptyAreaMessage");
        private Button emptyAreaButton => cache.Get<Button>("emptyAreaButton");
    }
}
