// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
        }

        public PackageList()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            loginButton.clickable.clicked += OnLoginClicked;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            focusable = true;
        }

        public void OnEnable()
        {
            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_PageManager.onRefreshOperationStart += OnRefreshOperationStartOrFinish;
            m_PageManager.onRefreshOperationFinish += OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange += OnVisualStateChange;
            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_SettingsProxy.onSeeAllVersionsChanged += OnSeeAllPackageVersionsChanged;

            m_PackageFiltering.onFilterTabChanged += OnFilterTabChanged;

            // manually build the items on initialization to refresh the UI
            OnListRebuild(m_PageManager.GetCurrentPage());

            if (!Unsupported.IsDeveloperBuild() && m_SettingsProxy.seeAllPackageVersions)
            {
                m_SettingsProxy.seeAllPackageVersions = false;
                m_SettingsProxy.Save();
            }
        }

        public void OnDisable()
        {
            m_PackageDatabase.onPackageProgressUpdate -= OnPackageProgressUpdate;

            m_PageManager.onRefreshOperationStart -= OnRefreshOperationStartOrFinish;
            m_PageManager.onRefreshOperationFinish -= OnRefreshOperationStartOrFinish;

            m_PageManager.onVisualStateChange -= OnVisualStateChange;
            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_SettingsProxy.onSeeAllVersionsChanged -= OnSeeAllPackageVersionsChanged;
            m_PackageFiltering.onFilterTabChanged -= OnFilterTabChanged;
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            if (panel == null)
                return;
            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            RegisterCallback<KeyDownEvent>(IgnoreEscapeKeyDown, TrickleDown.TrickleDown);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (panel == null)
                return;
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            UnregisterCallback<KeyDownEvent>(IgnoreEscapeKeyDown, TrickleDown.TrickleDown);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            currentView.OnKeyDownShortcut(evt);
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

        private void OnSelectionChanged(IPackageVersion version)
        {
            currentView.GetPackageItem(version?.packageUniqueId)?.RefreshState();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore && UpdateListVisibility())
                m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid();
        }

        private void HideListShowLogin()
        {
            UIUtils.SetElementDisplay(listContainer, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, false);
            // when the editor first starts, we detect the user as not logged in (even though they are) because userInfo is not ready yet
            // in this case, we want to delay showing the login window until the userInfo is ready
            UIUtils.SetElementDisplay(loginContainer, m_UnityConnect.isUserInfoReady);

            m_PageManager.ClearSelection();
        }

        public void HideListShowMessage(bool isRefreshInProgress, bool isInitialFetchingDone, string messageWhenInitialFetchNotDone = "")
        {
            UIUtils.SetElementDisplay(listContainer, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, true);
            UIUtils.SetElementDisplay(loginContainer, false);

            if (isRefreshInProgress)
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = L10n.Tr("Fetching packages...");
                else
                    noPackagesLabel.text = L10n.Tr("Refreshing packages...");
            }
            else if (string.IsNullOrEmpty(m_PackageFiltering.currentSearchText))
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = messageWhenInitialFetchNotDone;
                else
                    noPackagesLabel.text = L10n.Tr("There are no packages.");
            }
            else
            {
                const int maxSearchTextToDisplay = 64;
                var searchText = m_PackageFiltering.currentSearchText;
                if (searchText?.Length > maxSearchTextToDisplay)
                    searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                noPackagesLabel.text = string.Format(L10n.Tr("No results for \"{0}\""), searchText);
            }

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
            if (isListEmpty || !isInitialFetchingDone)
            {
                HideListShowMessage(m_PageManager.IsRefreshInProgress(), isInitialFetchingDone);
                return false;
            }

            HideEmptyAreaShowList(skipListRebuild);
            return true;
        }

        private void OnLoginClicked()
        {
            m_UnityConnect.ShowLogin();
        }

        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            float containerHeight = resolvedStyle.height;
            if (!float.IsNaN(containerHeight))
                m_PackageManagerPrefs.numItemsPerPage = (int)(containerHeight / PackageItem.k_MainItemHeight);
        }

        private void OnPackageProgressUpdate(IPackage package)
        {
            currentView.GetPackageItem(package?.uniqueId)?.RefreshState();
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
            if (UpdateListVisibility())
                currentView.OnFilterTabChanged(filterTab);
        }

        private void OnSeeAllPackageVersionsChanged(bool value)
        {
            currentView.OnSeeAllPackageVersionsChanged(value);
        }

        internal IPackageListView currentView => m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore ? (IPackageListView)listView : scrollView;

        private VisualElementCache cache { get; set; }
        private PackageListView listView => cache.Get<PackageListView>("listView");
        private PackageListScrollView scrollView => cache.Get<PackageListScrollView>("scrollView");
        private VisualElement listContainer => cache.Get<VisualElement>("listContainer");
        private VisualElement emptyArea => cache.Get<VisualElement>("emptyArea");
        private Label noPackagesLabel => cache.Get<Label>("noPackagesLabel");
        private VisualElement loginContainer => cache.Get<VisualElement>("loginContainer");
        private Button loginButton => cache.Get<Button>("loginButton");
    }
}
