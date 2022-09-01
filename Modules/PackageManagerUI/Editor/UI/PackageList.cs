// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageList> {}

        private ResourceLoader m_ResourceLoader;
        private UnityConnectProxy m_UnityConnect;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PageManager m_PageManager;
        private AssetStoreCallQueue m_AssetStoreCallQueue;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PageManager = container.Resolve<PageManager>();
            m_AssetStoreCallQueue = container.Resolve<AssetStoreCallQueue>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
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
            m_PageManager.onRefreshOperationStart += OnRefreshOperationStartOrFinish;
            m_PageManager.onRefreshOperationFinish += OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange += OnVisualStateChange;
            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_AssetStoreCallQueue.onCheckUpdateProgress += OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_PackageFiltering.onFilterTabChanged += OnFilterTabChanged;

            listView.OnEnable();
            packageLoadBar.OnEnable();

            if (!Unsupported.IsDeveloperBuild() && m_SettingsProxy.seeAllPackageVersions)
            {
                m_SettingsProxy.seeAllPackageVersions = false;
                m_SettingsProxy.Save();
            }

            // manually build the items on initialization to refresh the UI
            OnListRebuild(m_PageManager.GetCurrentPage());
        }

        public void OnDisable()
        {
            m_PageManager.onRefreshOperationStart -= OnRefreshOperationStartOrFinish;
            m_PageManager.onRefreshOperationFinish -= OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange -= OnVisualStateChange;
            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_AssetStoreCallQueue.onCheckUpdateProgress -= OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_PackageFiltering.onFilterTabChanged -= OnFilterTabChanged;

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

        private void OnSelectionChanged(PageSelection selection)
        {
            var currentView = this.currentView;
            foreach (var item in selection.previousSelections.Where(s => !selection.Contains(s.packageUniqueId)).Concat(selection))
                currentView.GetPackageItem(item.packageUniqueId)?.RefreshSelection();
        }

        private void OnCheckUpdateProgress()
        {
            UpdateListVisibility(true);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore && UpdateListVisibility())
                m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        private void HideListShowMessage(bool isRefreshInProgress, bool isInitialFetchingDone, bool isCheckUpdateInProgress)
        {
            string message;
            var contentType = m_PageManager.GetCurrentPage().contentType ?? L10n.Tr("packages");
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
                    m_PageManager.GetCurrentPage().ClearFilters();
                    m_AssetStoreCallQueue.CancelCheckUpdates();
                });
                return;
            }
            else if (string.IsNullOrEmpty(m_PackageFiltering.currentSearchText))
            {
                message = string.Format(L10n.Tr("There are no {0}."), contentType);
            }
            else
            {
                const int maxSearchTextToDisplay = 64;
                var searchText = m_PackageFiltering.currentSearchText;
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

            m_PageManager.ClearSelection();
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
                currentView.OnListRebuild(m_PageManager.GetCurrentPage());
        }

        // Returns true if the list of packages is visible (either listView or scrollView), false otherwise
        private bool UpdateListVisibility(bool skipListRebuild = false)
        {
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore && !m_UnityConnect.isUserLoggedIn)
            {
                HideListShowLogin();
                return false;
            }

            var page = m_PageManager.GetCurrentPage();
            var isListEmpty = !page.visualStates.Any(v => v.visible);
            var isInitialFetchingDone = m_PageManager.IsInitialFetchingDone();
            var isCheckUpdateInProgress = m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore &&
                m_AssetStoreCallQueue.isCheckUpdateInProgress && page.filters.updateAvailableOnly;
            if (isListEmpty || !isInitialFetchingDone || isCheckUpdateInProgress)
            {
                HideListShowMessage(m_PageManager.IsRefreshInProgress(), isInitialFetchingDone, isCheckUpdateInProgress);
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
                m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        internal void OnFocus()
        {
            currentView.ScrollToSelection();
        }

        private void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (UpdateListVisibility())
                currentView.OnVisualStateChange(visualStates);
        }

        private void OnListRebuild(IPage page)
        {
            if (UpdateListVisibility(true))
                currentView.OnListRebuild(page);
        }

        private void OnListUpdate(ListUpdateArgs args)
        {
            if (UpdateListVisibility())
                currentView.OnListUpdate(args);
        }

        private void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            if (m_PackageFiltering.previousFilterTab == PackageFilterTab.AssetStore)
                m_PageManager.CancelRefresh();

            if (UpdateListVisibility())
                currentView.OnFilterTabChanged(filterTab);
        }

        internal IPackageListView currentView => m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore ? (IPackageListView)listView : scrollView;

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
