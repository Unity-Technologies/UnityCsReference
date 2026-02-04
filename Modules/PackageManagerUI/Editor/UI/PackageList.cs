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
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageList(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IUnityConnectProxy>(),
                    container.Resolve<IPackageManagerPrefs>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IUpmCache>(),
                    container.Resolve<IBackgroundFetchHandler>(),
                    container.Resolve<IProjectSettingsProxy>(),
                    container.Resolve<IPageRefreshHandler>());
            }
        }

        private Action m_ButtonAction;

        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPageManager m_PageManager;
        private readonly IUpmCache m_UpmCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IPageRefreshHandler m_PageRefreshHandler;
        public PackageList(
            IResourceLoader resourceLoader,
            IUnityConnectProxy unityConnect,
            IPackageManagerPrefs packageManagerPrefs,
            IPageManager pageManager,
            IUpmCache upmCache,
            IBackgroundFetchHandler backgroundFetchHandler,
            IProjectSettingsProxy settingsProxy,
            IPageRefreshHandler pageRefreshHandler)
        {
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PageManager = pageManager;
            m_UpmCache = upmCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;
            m_SettingsProxy = settingsProxy;
            m_PageRefreshHandler = pageRefreshHandler;

            var root = resourceLoader.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            messageAreaButton.clickable.clicked += OnButtonClicked;

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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var item in args.selection.previousSelections.Filter(s => !args.selection.Contains(s)).Join(args.selection))
#pragma warning restore UA2001
                currentView.GetPackageItem(item)?.RefreshSelection();

            if (!args.isExplicitUserSelection)
                currentView.ScrollToSelection();

            if (args.selection.previousSelections.Count == 1)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_UpmCache.SetLoadAllVersions(args.selection.previousSelections.FirstOrDefault(), false);
#pragma warning restore UA2001
        }

        private void OnCheckUpdateProgress()
        {
            UpdateListVisibility();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            var page = m_PageManager.activePage;
            if (page.id == MyAssetsPage.k_Id && UpdateListVisibility())
                page.UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        public void HideListShowMessage(string message, string buttonText = null, Action buttonAction = null)
        {
            UIUtils.SetElementDisplay(listContainer, false);
            UIUtils.SetElementDisplay(packageLoadBar, false);
            UIUtils.SetElementDisplay(messageArea, true);

            messageAreaLabel.text = message ?? string.Empty;

            messageAreaButton.text = buttonText ?? string.Empty;
            m_ButtonAction = buttonAction;
            UIUtils.SetElementDisplay(messageAreaButton, buttonAction != null);

            m_PageManager.activePage.SetNewSelection(Array.Empty<string>());
        }

        private void HideMessageShowList(bool skipListRebuild)
        {
            var rebuild = !skipListRebuild && !UIUtils.IsElementVisible(listContainer);
            UIUtils.SetElementDisplay(listContainer, true);
            UIUtils.SetElementDisplay(messageArea, false);

            var currentView = this.currentView;
            UIUtils.SetElementDisplay(listView, listView == currentView);
            UIUtils.SetElementDisplay(scrollView, scrollView == currentView);

            packageLoadBar.UpdateVisibility();

            var page = m_PageManager.activePage;
            var selection = page.GetSelection();
            if (selection.Count == 0 && selection.previousSelections.Count > 0)
                page.SetNewSelection(selection.previousSelections);

            if (rebuild)
                currentView.OnListRebuild(page);
        }

        // Returns true if the list of packages is visible (either listView or scrollView), false otherwise
        private bool UpdateListVisibility(bool skipListRebuild = false)
        {
            var page = m_PageManager.activePage;
            if (page.id == MyAssetsPage.k_Id)
            {
                if (!m_UnityConnect.isUserLoggedIn)
                {
                    if (m_UnityConnect.isUserInfoReady)
                        HideListShowMessage(L10n.Tr("Sign in to access your assets"), L10n.Tr("Sign in"), m_UnityConnect.ShowLogin);
                    else
                        HideListShowMessage(string.Empty);
                    return false;
                }

                if (m_BackgroundFetchHandler.isCheckUpdateInProgress && page.filters.status == PageFilters.Status.UpdateAvailable)
                {
                    HideListShowMessage(string.Format(L10n.Tr("Checking for updates {0}%..."), m_BackgroundFetchHandler.checkUpdatePercentage), L10n.Tr("Cancel"), () =>
                    {
                        m_PageManager.activePage.ClearFilters();
                        m_BackgroundFetchHandler.CancelCheckUpdates();
                    });
                    return false;
                }
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var isListEmpty = !page.visualStates.Any(v => v.visible);
#pragma warning restore UA2001
            var isInitialFetchingDone = m_PageRefreshHandler.IsInitialFetchingDone(page);
            if (isListEmpty || !isInitialFetchingDone)
            {
                if (m_PageRefreshHandler.IsRefreshInProgress(page))
                    HideListShowMessage(L10n.Tr("Refreshing list..."));
                else
                {
                    var searchText = page.searchText;
                    if (string.IsNullOrEmpty(searchText))
                        HideListShowMessage(page.filters.isFilterSet ? L10n.Tr("No results for the specified filters.") : L10n.Tr("No items to display."));
                    else
                    {
                        const int maxSearchTextToDisplay = 64;
                        if (searchText.Length > maxSearchTextToDisplay)
                            searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                        HideListShowMessage(string.Format(L10n.Tr("No results for \"{0}\""), searchText));
                    }
                }
                return false;
            }

            HideMessageShowList(skipListRebuild);
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

        private void OnFocus()
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
        private VisualElement messageArea => cache.Get<VisualElement>("messageArea");
        private Label messageAreaLabel => cache.Get<Label>("messageAreaLabel");
        private Button messageAreaButton => cache.Get<Button>("messageAreaButton");
    }
}
