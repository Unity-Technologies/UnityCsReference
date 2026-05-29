// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal sealed partial class ListArea : VisualElement
    {
        private Action m_ButtonAction;

        private PackageListView m_PackageListView;
        private ItemListScrollView m_ItemScrollView;

        private IItemListView m_CurrentView;

        public ListArea() : this(
            ServicesContainer.instance.Resolve<IResourceLoader>(),
            ServicesContainer.instance.Resolve<IApplicationProxy>(),
            ServicesContainer.instance.Resolve<IUnityConnectProxy>(),
            ServicesContainer.instance.Resolve<IPackageManagerPrefs>(),
            ServicesContainer.instance.Resolve<IPackageDatabase>(),
            ServicesContainer.instance.Resolve<IPageManager>(),
            ServicesContainer.instance.Resolve<IUpmCache>(),
            ServicesContainer.instance.Resolve<IAssetStoreCache>(),
            ServicesContainer.instance.Resolve<IBackgroundFetchHandler>(),
            ServicesContainer.instance.Resolve<IProjectSettingsProxy>(),
            ServicesContainer.instance.Resolve<IPageRefreshHandler>())
        {
        }

        internal IItemListView currentView
        {
            get
            {
                IItemListView newView = m_PageManager.activePage.id switch
                {
                    MyAssetsPage.k_Id => m_PackageListView ??= new PackageListView(m_ResourceLoader, m_Application, m_UnityConnect,
                        m_SettingsProxy, m_PageRefreshHandler, m_PackageDatabase, m_PageManager, m_AssetStoreCache, m_BackgroundFetchHandler),
                    _ => m_ItemScrollView ??= new ItemListScrollView(m_ResourceLoader, m_PageManager, m_PackageDatabase)
                };

                if (newView != m_CurrentView)
                {
                    m_CurrentView = newView;
                    listContainer.Clear();
                    listContainer.Add(m_CurrentView.element);
                }
                return m_CurrentView;
            }
        }

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IApplicationProxy m_Application;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IUpmCache m_UpmCache;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IPageRefreshHandler m_PageRefreshHandler;
        public ListArea(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IUnityConnectProxy unityConnect,
            IPackageManagerPrefs packageManagerPrefs,
            IPackageDatabase packageDatabase,
            IPageManager pageManager,
            IUpmCache upmCache,
            IAssetStoreCache assetStoreCache,
            IBackgroundFetchHandler backgroundFetchHandler,
            IProjectSettingsProxy settingsProxy,
            IPageRefreshHandler pageRefreshHandler)
        {
            m_ResourceLoader = resourceLoader;
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_UpmCache = upmCache;
            m_AssetStoreCache = assetStoreCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;
            m_SettingsProxy = settingsProxy;
            m_PageRefreshHandler = pageRefreshHandler;

            var root = m_ResourceLoader.GetTemplate("ListArea.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            messageAreaButton.clickable.clicked += OnButtonClicked;

            focusable = true;

            mainArea.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PageRefreshHandler.onRefreshOperationStart += OnRefreshOperationStartOrFinish;
            m_PageRefreshHandler.onRefreshOperationFinish += OnRefreshOperationStartOrFinish;

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onVisualStateChange += OnVisualStateChange;
            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;
            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_BackgroundFetchHandler.onCheckUpdateProgress += OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            if (!Unsupported.IsDeveloperBuild() && m_SettingsProxy.seeAllPackageVersions)
            {
                m_SettingsProxy.seeAllPackageVersions = false;
                m_SettingsProxy.Save();
            }

            // manually build the items on initialization to refresh the UI
            OnListRebuild(m_PageManager.activePage);

            Focus();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_PageRefreshHandler.onRefreshOperationStart -= OnRefreshOperationStartOrFinish;
            m_PageRefreshHandler.onRefreshOperationFinish -= OnRefreshOperationStartOrFinish;

            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_PageManager.onVisualStateChange -= OnVisualStateChange;
            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;
            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_BackgroundFetchHandler.onCheckUpdateProgress -= OnCheckUpdateProgress;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnActivePageChanged(IPage page)
        {
            OnListRebuild(page);
        }

        private void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActive)
                return;

            var currentView = this.currentView;
            var selection = args.selection;
            foreach (var item in selection.Join(args.selection.previousSelections.Filter(i => !selection.Contains(i))))
                currentView.GetListItem(item)?.RefreshSelection();

            if (!args.isDirectMouseSelection)
                currentView.ScrollToSelection();

            var lastSelectedPackage = args.selection.previousSelections.Count == 1 ? m_PackageDatabase.GetPackage(args.selection.previousSelections[0]) : null;
            if (!string.IsNullOrEmpty(lastSelectedPackage?.name))
                m_UpmCache.SetLoadAllVersions(lastSelectedPackage.name, false);
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
            UIUtils.SetElementDisplay(messageArea, true);

            messageAreaLabel.text = message ?? string.Empty;

            messageAreaButton.text = buttonText ?? string.Empty;
            m_ButtonAction = buttonAction;
            UIUtils.SetElementDisplay(messageAreaButton, buttonAction != null);

            m_PageManager.activePage.SetNewSelection(Array.Empty<string>(), false);
        }

        private void HideMessageShowList(bool skipListRebuild)
        {
            var rebuild = !skipListRebuild && !UIUtils.IsElementVisible(listContainer);
            UIUtils.SetElementDisplay(listContainer, true);
            UIUtils.SetElementDisplay(messageArea, false);

            var page = m_PageManager.activePage;
            var selection = page.GetSelection();
            if (selection.Count == 0 && selection.previousSelections.Count > 0)
                page.SetNewSelection(selection.previousSelections, false);

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

                if (m_BackgroundFetchHandler.isCheckUpdateInProgress && page.filters.status == PageFilterStatus.UpdateAvailable)
                {
                    HideListShowMessage(string.Format(L10n.Tr("Checking for updates {0}%..."), m_BackgroundFetchHandler.checkUpdatePercentage),
                        L10n.Tr("Cancel"), () => m_PageManager.activePage.ClearFilters());
                    return false;
                }
            }

            var isListEmpty = !page.visualStates.AnyMatches(v => v.visible);
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
            var containerHeight = mainArea.resolvedStyle.height;
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

        private void OnVisualStateChange(VisualStateChangeArgs args)
        {
            if (args.page.isActive && UpdateListVisibility())
                currentView.OnVisualStateChange(args.changed);
        }

        private void OnListRebuild(IPage page)
        {
            if (page.isActive && UpdateListVisibility(true))
                currentView.OnListRebuild(page);
        }

        private void OnListUpdate(ListUpdateArgs args)
        {
            if (args.page.isActive && UpdateListVisibility())
                currentView.OnListUpdate(args);
        }

        private VisualElementCache cache { get; }
        private VisualElement mainArea => cache.Get<VisualElement>("mainArea");
        private VisualElement listContainer => cache.Get<VisualElement>("listContainer");
        private VisualElement messageArea => cache.Get<VisualElement>("messageArea");
        private Label messageAreaLabel => cache.Get<Label>("messageAreaLabel");
        private Button messageAreaButton => cache.Get<Button>("messageAreaButton");
    }
}
