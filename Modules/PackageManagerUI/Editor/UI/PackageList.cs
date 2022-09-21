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
        protected new class UxmlFactory : UxmlFactory<PackageList, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PackageList"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a PackageList element that you can
        /// use in a UXML asset.
        /// </remarks>
        protected new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = true;
            }
        }

        private ResourceLoader m_ResourceLoader;
        private UnityConnectProxy m_UnityConnect;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PageManager m_PageManager;
        private AssetStoreCallQueue m_AssetStoreCallQueue;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private PageRefreshHandler m_PageRefreshHandler;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PageManager = container.Resolve<PageManager>();
            m_AssetStoreCallQueue = container.Resolve<AssetStoreCallQueue>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_PageRefreshHandler = container.Resolve<PageRefreshHandler>();
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

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_AssetStoreCallQueue.onCheckUpdateProgress += OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_PackageManagerPrefs.onFilterTabChanged += OnFilterTabChanged;

            listView.OnEnable();
            packageLoadBar.OnEnable();

            if (!Unsupported.IsDeveloperBuild() && m_SettingsProxy.seeAllPackageVersions)
            {
                m_SettingsProxy.seeAllPackageVersions = false;
                m_SettingsProxy.Save();
            }

            // manually build the items on initialization to refresh the UI
            OnListRebuild(m_PageManager.GetPage());
        }

        public void OnDisable()
        {
            m_PageRefreshHandler.onRefreshOperationStart -= OnRefreshOperationStartOrFinish;
            m_PageRefreshHandler.onRefreshOperationFinish -= OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange -= OnVisualStateChange;
            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_AssetStoreCallQueue.onCheckUpdateProgress -= OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_PackageManagerPrefs.onFilterTabChanged -= OnFilterTabChanged;

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
                evt.PreventDefault();
            }
        }

        private void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            var currentView = this.currentView;
            foreach (var item in args.selection.previousSelections.Where(s => !args.selection.Contains(s.packageUniqueId)).Concat(args.selection))
                currentView.GetPackageItem(item.packageUniqueId)?.RefreshSelection();
        }

        private void OnCheckUpdateProgress()
        {
            UpdateListVisibility(true);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore && UpdateListVisibility())
                m_PageManager.GetPage().UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        private void HideListShowMessage(bool isRefreshInProgress, bool isInitialFetchingDone, bool isCheckUpdateInProgress)
        {
            string message;
            var contentType = m_PageManager.GetPage().contentType ?? L10n.Tr("packages");
            if (isRefreshInProgress)
            {
                if (!isInitialFetchingDone)
                    message = string.Format(L10n.Tr("Fetching {0}..."), contentType);
                else
                    message = string.Format(L10n.Tr("Refreshing {0}..."), contentType);
            }
            else if (isCheckUpdateInProgress)
            {
                message = string.Format(L10n.Tr("Checking for updates {0}%..."), m_AssetStoreCallQueue.checkUpdatePercentage);
                HideListShowEmptyArea(message, L10n.Tr("Cancel"), () =>
                {
                    m_PageManager.GetPage().ClearFilters();
                    m_AssetStoreCallQueue.CancelCheckUpdates();
                });
                return;
            }
            else if (string.IsNullOrEmpty(m_PackageManagerPrefs.searchText))
            {
                message = string.Format(L10n.Tr("There are no {0}."), contentType);
            }
            else
            {
                const int maxSearchTextToDisplay = 64;
                var searchText = m_PackageManagerPrefs.searchText;
                if (searchText?.Length > maxSearchTextToDisplay)
                    searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                message = string.Format(L10n.Tr("No results for \"{0}\""), searchText);
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

            m_PageManager.GetPage().SetNewSelection(Enumerable.Empty<PackageAndVersionIdPair>());
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
            if (rebuild)
                currentView.OnListRebuild(m_PageManager.GetPage());
        }

        // Returns true if the list of packages is visible (either listView or scrollView), false otherwise
        private bool UpdateListVisibility(bool skipListRebuild = false)
        {
            if (m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore && !m_UnityConnect.isUserLoggedIn)
            {
                HideListShowLogin();
                return false;
            }

            var page = m_PageManager.GetPage();
            var isListEmpty = !page.visualStates.Any(v => v.visible);
            var isInitialFetchingDone = m_PageRefreshHandler.IsInitialFetchingDone();
            var isCheckUpdateInProgress = m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore &&
                m_AssetStoreCallQueue.isCheckUpdateInProgress && page.filters.updateAvailableOnly;
            if (isListEmpty || !isInitialFetchingDone || isCheckUpdateInProgress)
            {
                HideListShowMessage(m_PageRefreshHandler.IsRefreshInProgress(), isInitialFetchingDone, isCheckUpdateInProgress);
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
            float containerHeight = resolvedStyle.height;
            if (!float.IsNaN(containerHeight))
            {
                var numItems = ((int)containerHeight - heightCalculationBuffer - PackageLoadBar.k_FixedHeight) / PackageItem.k_MainItemHeight;
                m_PackageManagerPrefs.numItemsPerPage = numItems <= 0 ? null : (int?)numItems;
            }
        }

        private void OnRefreshOperationStartOrFinish()
        {
            if (UpdateListVisibility())
                m_PageManager.GetPage().UpdateSelectionIfCurrentSelectionIsInvalid();
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

        private void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            if (m_PackageManagerPrefs.previousFilterTab == PackageFilterTab.AssetStore)
                m_PageRefreshHandler.CancelRefresh();

            if (UpdateListVisibility())
                currentView.OnFilterTabChanged(filterTab);
        }

        internal IPackageListView currentView => m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore ? (IPackageListView)listView : scrollView;

        private VisualElementCache cache { get; set; }
        private PackageListView listView => cache.Get<PackageListView>("listView");
        private PackageListScrollView scrollView => cache.Get<PackageListScrollView>("scrollView");
        private VisualElement listContainer => cache.Get<VisualElement>("listContainer");
        private PackageLoadBar packageLoadBar => cache.Get<PackageLoadBar>("packageLoadBar");
        private VisualElement emptyArea => cache.Get<VisualElement>("emptyArea");
        private Label emptyAreaMessage => cache.Get<Label>("emptyAreaMessage");
        private Button emptyAreaButton => cache.Get<Button>("emptyAreaButton");
    }
}
