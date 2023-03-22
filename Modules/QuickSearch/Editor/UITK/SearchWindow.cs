// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Profiling;
using UnityEditor.ShortcutManagement;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    interface ISearchWindow
    {
        void Close();
        bool IsPicker();
        bool HasFocus();
        void Show();
        ISearchView ShowWindow();
        ISearchView ShowWindow(SearchFlags flags);
        ISearchView ShowWindow(float width, float height);
        ISearchView ShowWindow(float width, float height, SearchFlags flags);
        IEnumerable<SearchItem> FetchItems();
        void AddProvidersToMenu(GenericMenu menu);
    }

    [EditorWindowTitle(title="Search")]
    class SearchWindow : EditorWindow, ISearchView, ISearchQueryView, ISearchElement, IDisposable, IHasCustomMenu, ISearchWindow
    {
        internal const float defaultWidth = 700f;
        internal const float defaultHeight = 450f;

        private const string k_TogleSyncShortcutName = "Search/Toggle Sync Search View";
        private const string k_LastSearchPrefKey = "last_search";
        private const string k_SideBarWidthKey = "Search.SidebarWidth";
        private const string k_DetailsWidthKey = "Search.DetailsWidth";
        private static readonly string k_CheckWindowKeyName = $"{typeof(SearchWindow).FullName}h";

        private static EditorWindow s_FocusedWindow;
        private static SearchViewState s_GlobalViewState = null;

        private bool m_Disposed = false;
        private Action m_DebounceOff = null;
        private SearchMonitorView m_SearchMonitorView;
        private List<Action> m_SearchEventOffs;

        private TwoPaneSplitView m_LeftSplitter;
        private const string k_LeftSplitterViewDataKey = "search-left-splitter__view-data-key";
        private TwoPaneSplitView m_RightSplitter;
        private const string k_RightSplitterViewDataKey = "search-right-splitter__view-data-key";
        private SearchView m_SearchView = default;
        private SearchToolbar m_SearchToolbar;
        private SearchAutoCompleteWindow m_SearchAutoCompleteWindow;
        private VisualElement m_SearchQueryPanelContainer;
        private VisualElement m_DetailsPanelContainer;

        [SerializeField] protected int m_ContextHash;
        [SerializeField] private float m_PreviousItemSize = -1;
        [SerializeField] protected SearchViewState m_ViewState = null;
        [SerializeField] protected EditorWindow m_LastFocusedWindow;

        internal IResultView resultView => m_SearchView.resultView;
        internal QueryBuilder queryBuilder => m_SearchToolbar.queryBuilder;

        internal SearchAutoCompleteWindow autoComplete => m_SearchAutoCompleteWindow;
        internal SearchToolbar searchToolbar => m_SearchToolbar;

        public Action<SearchItem, bool> selectCallback
        {
            get => m_ViewState.selectHandler;
            set => m_ViewState.selectHandler = value;
        }

        Func<SearchItem, bool> ISearchView.filterCallback { get => (item) => m_ViewState.filterHandler?.Invoke(item) ?? true; }
        Action<SearchItem> ISearchView.trackingCallback => m_ViewState.trackingHandler;

        public bool searchInProgress => (m_SearchView?.searchInProgress ?? context?.searchInProgress ?? false) || m_DebounceOff != null;
        public string currentGroup { get => m_SearchView?.currentGroup ?? viewState.group; set => SelectGroup(value); }

        public SearchViewState state => m_ViewState;
        protected SearchViewState viewState => m_ViewState;
        SearchViewState ISearchElement.viewState => m_ViewState;

        public ISearchQuery activeQuery
        {
            get => m_ViewState.activeQuery;
            set => m_ViewState.activeQuery = value;
        }

        public SearchSelection selection => m_SearchView.selection;
        public SearchContext context => m_ViewState.context;
        public ISearchList results => m_SearchView.results;
        public DisplayMode displayMode => m_SearchView.displayMode;
        public float itemIconSize { get => m_SearchView?.itemSize ?? 0; set { if (m_SearchView != null) m_SearchView.itemSize = value; } }
        public bool multiselect { get => m_SearchView.multiselect; set => m_SearchView.multiselect = value; }

        private string windowId => m_ViewState.sessionId;
        int ISearchView.totalCount => m_SearchView.totalCount;
        bool ISearchView.syncSearch { get => m_SearchView.syncSearch; set { if (m_SearchView != null) m_SearchView.syncSearch = value; } }
        SearchPreviewManager ISearchView.previewManager => m_SearchView.previewManager;

        public void CreateGUI()
        {
            VisualElement body = rootVisualElement;

            body.style.flexGrow = 1.0f;
            body.RegisterCallback<KeyDownEvent>(OnGlobalKeyDownEvent, invokePolicy: InvokePolicy.IncludeDisabled, useTrickleDown: TrickleDown.TrickleDown);

            // Create main layout
            if (m_ViewState.flags.HasNone(SearchViewFlags.HideSearchBar))
                body.Add(m_SearchToolbar = new SearchToolbar("SearchToolbar", this));
            else
                m_SearchView?.RegisterCallback<AttachToPanelEvent>(SetFocusOnViewAttached);
            body.Add(CreateContent(m_SearchView));
            body.Add(new SearchStatusBar("SearchStatusBar", this));

            // Don't add it to the body, the SearchAutoCompleteWindow will take care of it when shown.
            m_SearchAutoCompleteWindow = new SearchAutoCompleteWindow(this, body);
        }

        int ISearchView.GetViewId()
        {
            return ((ISearchView)m_SearchView).GetViewId();
        }

        private void SetFocusOnViewAttached(AttachToPanelEvent evt)
        {
            SelectSearch();
        }

        private void OnGlobalKeyDownEvent(KeyDownEvent evt)
        {
            var e = evt.target as VisualElement;

            if (HandleKeyboardNavigation(e, evt, evt.imguiEvent))
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }

        private bool HandleDefaultPressEnter(Event evt)
        {
            if (evt.type != EventType.KeyDown)
                return false;

            if (evt.modifiers > 0)
                return false;

            if (GUIUtility.textFieldInput)
                return false;

            if (selection.Count != 0 || results.Count == 0)
                return false;

            if (evt.keyCode != KeyCode.KeypadEnter && evt.keyCode != KeyCode.Return)
                return false;

            SetSelection(0);
            evt.Use();
            GUIUtility.ExitGUI();
            return true;
        }

        private bool HandleKeyboardNavigation(VisualElement target, KeyDownEvent evt, Event imguiEvt)
        {
            if ((target is TextElement) && !SearchElement.IsPartOf<SearchToolbar>(target))
                return false;

            // Ignore tabbing and line return in quicksearch
            if (evt.keyCode == KeyCode.None && (evt.character == '\t' || (int)evt.character == 10))
                return true;

            if (SearchGlobalEventHandlerManager.HandleGlobalEventHandlers(m_ViewState.globalEventManager, evt))
                return true;

            if (imguiEvt != null && HandleDefaultPressEnter(imguiEvt))
                return true;

            if (evt is KeyDownEvent && !GUIUtility.textFieldInput)
            {
                var ctrl = evt.ctrlKey || evt.commandKey;
                if (evt.keyCode == KeyCode.Escape)
                {
                    HandleEscapeKeyDown(evt);
                    return true;
                }
                else if (evt.keyCode == KeyCode.F5)
                {
                    Refresh();
                    return true;
                }
                else if (evt.keyCode == KeyCode.F1)
                {
                    ToggleQueryBuilder();
                    return true;
                }
                else if (evt.keyCode == KeyCode.F4 && viewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
                {
                    TogglePanelView(SearchViewFlags.OpenInspectorPreview);
                    return true;
                }
                else if (evt.keyCode == KeyCode.F3 && IsSavedSearchQueryEnabled())
                {
                    TogglePanelView(SearchViewFlags.OpenLeftSidePanel);
                    return true;
                }
                else if (evt.keyCode == KeyCode.F10)
                {
                    ToggleWantsMore();
                    return true;
                }
                else if (evt.modifiers.HasAny(EventModifiers.Alt) && evt.keyCode == KeyCode.LeftArrow)
                {
                    string previousGroupId = null;
                    foreach (var group in EnumerateGroups())
                    {
                        if (previousGroupId != null && group.id == m_SearchView.currentGroup)
                        {
                            SelectGroup(previousGroupId);
                            break;
                        }
                        previousGroupId = group.id;
                    }
                    return true;
                }
                else if (evt.modifiers.HasAny(EventModifiers.Alt) && evt.keyCode == KeyCode.RightArrow)
                {
                    bool selectNext = false;
                    foreach (var group in EnumerateGroups())
                    {
                        if (selectNext)
                        {
                            SelectGroup(group.id);
                            break;
                        }
                        else if (group.id == m_SearchView.currentGroup)
                            selectNext = true;
                    }
                    return true;
                }
                else if (evt.keyCode == KeyCode.Tab && evt.modifiers == EventModifiers.None)
                {
                    m_SearchAutoCompleteWindow.Show(m_SearchToolbar);
                    return true;
                }
            }

            if (imguiEvt != null && imguiEvt.type == EventType.Used)
                return true;

            return false;
        }

        protected virtual void HandleEscapeKeyDown(EventBase evt)
        {
            if (!docked)
            {
                SendEvent(SearchAnalytics.GenericEventType.QuickSearchDismissEsc);
                selectCallback?.Invoke(null, true);
                selectCallback = null;
                CloseSearchWindow();
            }
            else
            {
                ClearSearch();
            }
        }

        private VisualElement CreateContent(SearchView resultView)
        {
            var groupBar = !viewState.hideTabs ? new SearchGroupBar("SearchGroupbar", this) : null;

            var resultContainer = SearchElement.Create<VisualElement>("SearchResultContainer", "search-panel", "search-result-container");
            if (groupBar != null)
                resultContainer.Add(groupBar);
            resultContainer.Add(resultView);

            m_SearchQueryPanelContainer = new VisualElement() { name = "SearchQueryPanelContainer" };
            m_SearchQueryPanelContainer.AddToClassList("search-panel-container");
            if (IsSavedSearchQueryEnabled() && m_ViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel))
                m_SearchQueryPanelContainer.Add(new SearchQueryPanelView("SearchQueryPanel", this, "search-panel", "search-query-panel"));

            m_DetailsPanelContainer = new VisualElement() { name = "SearchDetailViewContainer" };
            m_DetailsPanelContainer.AddToClassList("search-panel-container");
            if (m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview))
                m_DetailsPanelContainer.Add(new SearchDetailView("SearchDetailView", this, "search-panel", "search-detail-panel"));

            m_LeftSplitter = new TwoPaneSplitView(0, EditorPrefs.GetFloat(k_SideBarWidthKey, 120f), TwoPaneSplitViewOrientation.Horizontal) { name = "SearchLeftSidePanels" };
            m_LeftSplitter.viewDataKey = k_LeftSplitterViewDataKey;
            m_LeftSplitter.AddToClassList("search-splitter");
            m_LeftSplitter.Add(m_SearchQueryPanelContainer);
            m_LeftSplitter.Add(resultContainer);

            m_RightSplitter = new TwoPaneSplitView(1, EditorPrefs.GetFloat(k_DetailsWidthKey, 160f), TwoPaneSplitViewOrientation.Horizontal) { name = "SearchContent" };
            m_RightSplitter.viewDataKey = k_RightSplitterViewDataKey;
            m_RightSplitter.AddToClassList("search-content");
            m_RightSplitter.AddToClassList("search-splitter");
            m_RightSplitter.Add(m_LeftSplitter);
            m_RightSplitter.Add(m_DetailsPanelContainer);

            m_RightSplitter.RegisterCallback<GeometryChangedEvent>(UpdateLayout);
            return m_RightSplitter;
        }

        private void UpdateLayout(GeometryChangedEvent evt)
        {
            if (evt.target is CallbackEventHandler eh)
                eh.UnregisterCallback<GeometryChangedEvent>(UpdateLayout);
            UpdateSplitterPanes();
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.Default)
        {
            SetSearchText(searchText, moveCursor, -1);
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            if (context == null)
                return;

            // Always emit event as cursor might have changed even if the text didn't
            var oldText = context.searchText;
            m_SearchView.SetSearchText(searchText, moveCursor);
            Dispatcher.Emit(SearchEvent.SearchTextChanged, new SearchEventPayload(this, oldText, context.searchText, moveCursor, cursorInsertPosition));

            if (viewState.queryBuilderEnabled && queryBuilder != null && queryBuilder.BuildQuery() != searchText)
            {
                Dispatcher.Emit(SearchEvent.RefreshBuilder, new SearchEventPayload(this));
            }
        }

        public virtual void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            m_SearchView?.Refresh(flags);
        }

        private void SetContext(SearchContext newContext)
        {
            if (context == null || context != newContext)
            {
                var searchText = context?.searchText ?? string.Empty;
                context?.Dispose();
                m_ViewState.context = newContext ?? SearchService.CreateContext(searchText);
                Dispatcher.Emit(SearchEvent.SearchContextChanged, new SearchEventPayload(this));
            }

            m_SearchView?.Reset();
            LoadContext();
        }

        public void SetSelection(params int[] selection)
        {
            m_SearchView.SetSelection(selection);
        }

        public void SetColumns(IEnumerable<SearchColumn> columns)
        {
            if (viewState.tableConfig == null)
                throw new NotSupportedException("This result view cannot set columns");

            viewState.tableConfig.columns = columns.ToArray();

            m_SearchView.resultView.Refresh(RefreshFlags.DisplayModeChanged);
        }

        public void AddSelection(params int[] selection)
        {
            m_SearchView.AddSelection(selection);
        }

        public void ExecuteSearchQuery(ISearchQuery query)
        {
            SetSelection();

            // Capture previous view flags so we can keep the same state.
            var preservedViewFlags = viewState.flags & SearchViewFlags.ContextSwitchPreservedMask;
            var queryBuilderEnabled = viewState.queryBuilderEnabled;

            var queryContext = CreateQueryContext(query);
            SetContext(queryContext);
            viewState.Assign(query.GetViewState(), queryContext);
            viewState.flags &= ~SearchViewFlags.ContextSwitchPreservedMask;
            viewState.flags |= preservedViewFlags;
            viewState.queryBuilderEnabled = queryBuilderEnabled;
            itemIconSize = viewState.itemSize;
            if (!viewState.hideTabs && !string.IsNullOrEmpty(viewState.group))
                SelectGroup(viewState.group);

            activeQuery = query;
            SearchQueryAsset.AddToRecentSearch(query);

            var evt = CreateEvent(SearchAnalytics.GenericEventType.QuickSearchSavedSearchesExecuted, query.searchText, "", query is SearchQueryAsset ? "project" : "user");
            evt.intPayload1 = viewState.tableConfig != null ? 1 : 0;
            SearchAnalytics.SendEvent(evt);

            SearchQuery.SaveLastUsedTimeToPropertyDatabase(activeQuery);
        }

        private void HandleExecuteSearchQuery(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewState)
                return;
            var query = evt.GetArgument<ISearchQuery>(0);
            ExecuteSearchQuery(query);
            evt.Use();
        }

        protected virtual SearchContext CreateQueryContext(ISearchQuery query)
        {
            var providers = context?.GetProviders() ?? SearchService.GetActiveProviders();
            return SearchService.CreateContext(SearchUtils.GetMergedProviders(providers, query.GetProviderIds()), query.searchText, context?.options ?? SearchFlags.Default);
        }

        public virtual void ExecuteSelection()
        {
            ExecuteSelection(0);
        }

        private void ExecuteSelection(int actionIndex)
        {
            if (selection.Count == 0)
                return;
            // Execute default action
            var item = selection.First();
            if (item.provider.actions.Count > actionIndex)
                ExecuteAction(item.provider.actions.Skip(actionIndex).First(), selection.ToArray(), !SearchSettings.keepOpen);
        }

        public void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = true)
        {
            m_SearchView.ExecuteAction(action, items, endSearch);
        }

        public void ShowItemContextualMenu(SearchItem item, Rect position)
        {
            m_SearchView.ShowItemContextualMenu(item, position);
        }

        internal virtual void OnEnable()
        {
            using (new EditorPerformanceTracker("SearchView.OnEnable"))
            {
                minSize = new Vector2(200f, minSize.y);

                rootVisualElement.name = nameof(SearchWindow);
                rootVisualElement.AddToClassList("search-window");
                SearchElement.AppendStyleSheets(rootVisualElement);

                hideFlags |= HideFlags.DontSaveInEditor;
                wantsLessLayoutEvents = true;

                m_LastFocusedWindow = m_LastFocusedWindow ?? focusedWindow;
                m_ViewState = s_GlobalViewState ?? m_ViewState ?? SearchViewState.LoadDefaults();

                SetContext(m_ViewState.context);
                LoadSessionSettings(m_ViewState);

                SearchSettings.SortActionsPriority();

                m_SearchMonitorView = SearchMonitor.GetView();
                m_SearchView = new SearchView(m_ViewState, GetInstanceID()) { multiselect = true };

                UpdateWindowTitle();

                SearchSettings.providerActivationChanged += OnProviderActivationChanged;

                m_SearchEventOffs = new List<Action>()
                {
                    Dispatcher.On(SearchEvent.ExecuteSearchQuery, HandleExecuteSearchQuery),
                    Dispatcher.On(SearchEvent.SaveUserQuery, HandleSaveUserQuery),
                    Dispatcher.On(SearchEvent.SaveProjectQuery, HandleSaveProjectQuery),
                    Dispatcher.On(SearchEvent.SaveActiveSearchQuery, HandleSaveActiveSearchQuery),
                    Dispatcher.On(SearchEvent.TogglePackages, HandleTogglePackages),
                    Dispatcher.On(SearchEvent.ToggleWantsMore, HandleToggleWantsMore),
                    Dispatcher.On(SearchEvent.RefreshContent, RefreshContent)
                };

                s_GlobalViewState = null;
            }
        }

        void HandleToggleWantsMore(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewState)
                return;
            ToggleWantsMore();
        }

        void HandleTogglePackages(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewState)
                return;
            TogglePackages();
        }

        void HandleSaveActiveSearchQuery(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewState)
                return;
            SaveActiveSearchQuery();
        }

        void HandleSaveProjectQuery(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewState)
                return;

            string savePath = null;
            if (evt.HasArgument(0))
                savePath = evt.GetArgument<string>(0);
            SaveProjectSearchQuery(savePath);
        }

        void HandleSaveUserQuery(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewState)
                return;
            SaveUserSearchQuery();
        }

        private bool HasCustomTitle()
        {
            return viewState.windowTitle != null && !string.IsNullOrEmpty(viewState.windowTitle.text);
        }

        internal virtual void OnDisable()
        {
            s_FocusedWindow = null;

            m_DebounceOff?.Invoke();
            m_DebounceOff = null;
            m_SearchEventOffs?.ForEach(off => off());

            EditorApplication.delayCall -= m_SearchView.DelayTrackSelection;
            SearchSettings.providerActivationChanged -= OnProviderActivationChanged;

            selectCallback?.Invoke(selection?.FirstOrDefault(), selection == null || selection.Count == 0);
            selectCallback = null;

            SaveSessionSettings();

            m_SearchView.syncSearch = false;
            m_SearchView?.Dispose();
            m_SearchMonitorView.Dispose();

            // End search session
            context.Dispose();
        }

        internal protected virtual bool IsSavedSearchQueryEnabled()
        {
            if (m_ViewState.HasFlag(SearchViewFlags.DisableSavedSearchQuery))
                return false;
            return true;
        }

        public bool CanSaveQuery()
        {
            return !string.IsNullOrWhiteSpace(context.searchQuery);
        }

        private void SaveItemCountToPropertyDatabase(bool isSaving)
        {
            if (activeQuery == null || m_SearchView == null)
                return;

            if (activeQuery.searchText != context.searchText && !isSaving)
                return;

            using (var view = SearchMonitor.GetView())
            {
                var recordKey = PropertyDatabase.CreateRecordKey(activeQuery.guid, SearchQuery.k_QueryItemsNumberPropertyName);
                // Always save the total count, taking the count from the query providers can be misleading.
                var itemCount = m_SearchView.GetItemCount(null);
                view.StoreProperty(recordKey, itemCount);
                Dispatcher.Emit(SearchEvent.SearchQueryItemCountUpdated, new SearchEventPayload(this, activeQuery.guid, itemCount));
            }
        }

        protected virtual void UpdateAsyncResults()
        {
            if (!this)
                return;

            m_DebounceOff = null;

            UpdateWindowTitle();
            SaveItemCountToPropertyDatabase(false);
        }

        private void RefreshContent(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;
            m_DebounceOff?.Invoke();
            m_DebounceOff = Utils.CallDelayed(UpdateAsyncResults, 0.1d);
        }

        protected bool ToggleFilter(string providerId)
        {
            var toggledEnabled = !context.IsEnabled(providerId);
            var provider = SearchService.GetProvider(providerId);
            if (provider != null && toggledEnabled)
            {
                // Force activate a provider we would want to toggle ON
                SearchService.SetActive(providerId);
            }
            if (providerId == m_SearchView.currentGroup)
                SelectGroup(null);
            context.SetFilter(providerId, toggledEnabled);

            Dispatcher.Emit(SearchEvent.FilterToggled, new SearchEventPayload(this, providerId));

            SendEvent(SearchAnalytics.GenericEventType.FilterWindowToggle, providerId, context.IsEnabled(providerId).ToString());
            Refresh();
            return toggledEnabled;
        }

        internal void TogglePanelView(SearchViewFlags panelOption)
        {
            var hasOptions = !m_ViewState.flags.HasAny(panelOption);
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchOpenToggleToggleSidePanel, panelOption.ToString(), hasOptions.ToString());
            if (hasOptions)
                m_ViewState.flags |= panelOption;
            else
                m_ViewState.flags &= ~panelOption;

            if (panelOption == SearchViewFlags.OpenLeftSidePanel && IsSavedSearchQueryEnabled())
            {
                SearchSettings.showSavedSearchPanel = hasOptions;
                SearchSettings.Save();
            }

            UpdateSplitterPanes();
        }

        private void UpdateSplitterPanes()
        {
            // We collapse/uncollapse the splitter panes according to the state of the view
            // (showing left/right side panels). To reduce resource consumption, we also remove the
            // inner panels when they are hidden.
            if (IsSavedSearchQueryEnabled() && m_ViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel))
            {
                if (m_SearchQueryPanelContainer.childCount == 0)
                    m_SearchQueryPanelContainer.Add(new SearchQueryPanelView("SearchQueryPanel", this, "search-panel", "search-query-panel"));
                m_LeftSplitter?.UnCollapse();
            }
            else
            {
                m_SearchQueryPanelContainer.Clear();
                m_LeftSplitter?.CollapseChild(0);
            }

            if (m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview))
            {
                if (m_DetailsPanelContainer.childCount == 0)
                    m_DetailsPanelContainer.Add(new SearchDetailView("SearchDetailView", this, "search-panel", "search-detail-panel"));
                m_RightSplitter?.UnCollapse();
            }
            else
            {
                m_DetailsPanelContainer.Clear();
                m_RightSplitter?.CollapseChild(1);
            }

            Dispatcher.Emit(SearchEvent.ViewStateUpdated, new SearchEventPayload(this));
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            AddItemsToMenu(menu, "Options/");
        }

        public void AddItemsToMenu(GenericMenu menu, string optionPrefix)
        {
            if (!IsPicker())
            {
                menu.AddItem(new GUIContent(L10n.Tr("Preferences")), false, () => SearchUtils.OpenPreferences());
                menu.AddSeparator("");
            }

            if (Utils.isDeveloperBuild)
            {
                menu.AddItem(new GUIContent(L10n.Tr("Serialize SearchContext")), false, () => SerializeSearchContext());
                menu.AddSeparator("");
            }

            var savedSearchContent = new GUIContent(L10n.Tr("Searches"));
            var previewInspectorContent = new GUIContent(L10n.Tr("Inspector"));
            var wantsMoreContent = new GUIContent(L10n.Tr($"{optionPrefix}Show more results"));

            if (IsSavedSearchQueryEnabled())
                menu.AddItem(savedSearchContent, m_ViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel), () => TogglePanelView(SearchViewFlags.OpenLeftSidePanel));
            if (m_ViewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
                menu.AddItem(previewInspectorContent, m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview), () => TogglePanelView(SearchViewFlags.OpenInspectorPreview));
            if (m_ViewState.flags.HasNone(SearchViewFlags.DisableBuilderModeToggle))
                menu.AddItem(new GUIContent(L10n.Tr($"Query Builder\tF1")), viewState.queryBuilderEnabled, () => ToggleQueryBuilder());
            if (IsSavedSearchQueryEnabled() || m_ViewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
                menu.AddSeparator("");
            if (Utils.isDeveloperBuild)
            {
                menu.AddItem(new GUIContent(L10n.Tr($"{optionPrefix}Debug")), context?.options.HasAny(SearchFlags.Debug) ?? false, () => ToggleDebugQuery());
                menu.AddItem(new GUIContent(L10n.Tr($"{optionPrefix}Show Tabs")), !viewState.hideTabs, () => ToggleShowTabs());
            }
            menu.AddItem(new GUIContent(L10n.Tr($"{optionPrefix}Show Status")), SearchSettings.showStatusBar, () => ToggleShowStatusBar());
            menu.AddItem(new GUIContent(L10n.Tr($"{optionPrefix}Show Packages results")), context.options.HasAny(SearchFlags.Packages), () => TogglePackages());
            menu.AddItem(wantsMoreContent, context?.wantsMore ?? false, () => ToggleWantsMore());
            if (!IsPicker())
            {
                menu.AddSeparator(optionPrefix);
                menu.AddItem(new GUIContent(L10n.Tr($"{optionPrefix}Keep Open")), SearchSettings.keepOpen, () => ToggleKeepOpen());
            }
        }

        private void SerializeSearchContext()
        {
            var json = EditorJsonUtility.ToJson(context, prettyPrint: true);
            Debug.Log($"(JSON) Search Context: {context}\r\n{json}");
            Debug.Log(Utils.PrintObject($"A. {context}", context));

            {
                var ssvs = new SearchContext();
                EditorJsonUtility.FromJsonOverwrite(json, ssvs);
                Debug.Log(Utils.PrintObject($"B. {ssvs}", ssvs));
            }
        }

        private void ToggleShowTabs()
        {
            var currentTabs = rootVisualElement.Q<SearchGroupBar>();
            viewState.hideTabs = !viewState.hideTabs;
            SearchSettings.hideTabs = viewState.hideTabs;
            SearchSettings.Save();
            if (viewState.hideTabs)
                currentTabs?.RemoveFromHierarchy();
            else if (currentTabs == null)
                rootVisualElement.Q("SearchResultContainer")?.Insert(0, new SearchGroupBar("SearchGroupbar", this));
            SelectGroup(null);
            Refresh();
        }

        internal void ToggleQueryBuilder()
        {
            if (viewState.flags.HasAny(SearchViewFlags.DisableBuilderModeToggle))
                return;
            SearchSettings.queryBuilder = viewState.queryBuilderEnabled = !viewState.queryBuilderEnabled;
            SearchSettings.Save();
            var evt = CreateEvent(SearchAnalytics.GenericEventType.QuickSearchToggleBuilder, viewState.queryBuilderEnabled.ToString());
            evt.intPayload1 = viewState.queryBuilderEnabled ? 1 : 0;
            SearchAnalytics.SendEvent(evt);

            Dispatcher.Emit(SearchEvent.RefreshBuilder, new SearchEventPayload(this));
            Dispatcher.Emit(SearchEvent.ViewStateUpdated, new SearchEventPayload(this));
        }

        private void ToggleShowStatusBar()
        {
            SearchSettings.showStatusBar = !SearchSettings.showStatusBar;
            Dispatcher.Emit(SearchEvent.ViewStateUpdated, new SearchEventPayload(this));
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchSettings.showStatusBar), SearchSettings.showStatusBar.ToString());
        }

        private void ToggleKeepOpen()
        {
            SearchSettings.keepOpen = !SearchSettings.keepOpen;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchSettings.keepOpen), SearchSettings.keepOpen.ToString());
        }

        private void TogglePackages()
        {
            if (context.showPackages)
            {
                SearchSettings.defaultFlags &= ~SearchFlags.Packages;
                context.showPackages = false;
            }
            else
            {
                SearchSettings.defaultFlags |= SearchFlags.Packages;
                context.showPackages = true;
            }
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchFlags.Packages), context?.wantsMore.ToString());
            Refresh(RefreshFlags.StructureChanged);
        }

        internal void ToggleWantsMore()
        {
            if (context.wantsMore)
            {
                SearchSettings.defaultFlags &= ~SearchFlags.WantsMore;
                context.wantsMore = false;
            }
            else
            {
                SearchSettings.defaultFlags |= SearchFlags.WantsMore;
                context.wantsMore = true;
            }
            SearchSettings.wantsMore = context?.wantsMore ?? false;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(context.wantsMore), context?.wantsMore.ToString());
            Refresh(RefreshFlags.StructureChanged);
        }

        private void ToggleDebugQuery()
        {
            if (context.debug)
            {
                SearchSettings.defaultFlags &= ~SearchFlags.Debug;
                context.debug = false;
            }
            else
            {
                SearchSettings.defaultFlags |= SearchFlags.Debug;
                context.debug = true;
            }
            Refresh();
        }

        protected virtual void UpdateWindowTitle()
        {
            if (HasCustomTitle())
                titleContent = viewState.windowTitle;
            else
            {
                titleContent.image = activeQuery?.thumbnail ?? Icons.quickSearchWindow;

                if (!titleContent.image)
                    titleContent.image = Icons.quickSearchWindow;

                if (context == null)
                    return;

                if (m_SearchView == null || m_SearchView.results.Count == 0)
                    titleContent.text = L10n.Tr("Search");
                else
                    titleContent.text = $"Search ({m_SearchView.results.Count})";
            }

            if (Utils.isDeveloperBuild)
            {
                if (context?.options.HasAny(SearchFlags.Debug) ?? false)
                    titleContent.tooltip = $"{Profiling.EditorPerformanceTracker.GetAverageTime("SearchWindow.Paint") * 1000:0.#} ms";

                if (Utils.IsRunningTests())
                    titleContent.text = $"[TEST] {titleContent.text}";
            }
        }

        IEnumerable<SearchQueryError> ISearchView.GetAllVisibleErrors()
        {
            return m_SearchView.GetAllVisibleErrors();
        }

        public void SelectSearch()
        {
            FocusSearch();
            if (m_SearchToolbar != null)
                m_SearchToolbar.FocusSearchField();
        }

        public void FocusSearch()
        {
            rootVisualElement.Query<SearchElement>().Visible().Where(e => e.focusable).First()?.Focus();
        }

        internal protected void CloseSearchWindow()
        {
            if (s_FocusedWindow)
                s_FocusedWindow.Focus();
            Utils.CallDelayed(Close);
        }

        IEnumerable<IGroup> ISearchView.EnumerateGroups()
        {
            return EnumerateGroups();
        }

        private IEnumerable<IGroup> EnumerateGroups()
        {
            return m_SearchView.EnumerateGroups();
        }

        void ISearchWindow.AddProvidersToMenu(GenericMenu menu)
        {
            if (IsPicker())
            {
                // Only allow customization of provider in current context.
                foreach (var p in context.providers)
                {
                    var filterContent = new GUIContent($"{p.name} ({p.filterId})");
                    menu.AddItem(filterContent, context.IsEnabled(p.id), () => ToggleFilter(p.id));
                }
            }
            else
            {
                foreach (var p in SearchService.OrderedProviders)
                {
                    var filterContent = new GUIContent($"{p.name} ({p.filterId})");
                    menu.AddItem(filterContent, context.IsEnabled(p.id), () => ToggleFilter(p.id));
                }
            }
        }

        internal void SelectGroup(string groupId)
        {
            if (m_SearchView.currentGroup == groupId)
                return;

            var selectedProvider = SearchService.GetProvider(groupId);
            if (selectedProvider != null && selectedProvider.showDetailsOptions.HasAny(ShowDetailsOptions.ListView))
            {
                if (m_PreviousItemSize == -1f)
                    m_PreviousItemSize = itemIconSize;
                itemIconSize = 1;
            }
            else if (m_PreviousItemSize >= 0f)
            {
                itemIconSize = m_PreviousItemSize;
                m_PreviousItemSize = -1f;
            }

            var evt = SearchAnalytics.GenericEvent.Create(windowId, SearchAnalytics.GenericEventType.QuickSearchSwitchTab, groupId ?? string.Empty);
            evt.stringPayload1 = m_SearchView.currentGroup;
            evt.intPayload1 = m_SearchView.GetGroupById(groupId)?.count ?? 0;
            SearchAnalytics.SendEvent(evt);

            m_SearchView.currentGroup = groupId;
        }

        protected void ClearSearch()
        {
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchClearSearch);
            SetSearchText(IsPicker() ? m_ViewState.initialQuery : string.Empty);
            SetSelection();
            SelectSearch();
            Dispatcher.Emit(SearchEvent.RefreshBuilder, new SearchEventPayload(this));
        }

        public virtual bool IsPicker()
        {
            return false;
        }

        public void SaveActiveSearchQuery()
        {
            if (activeQuery is SearchQueryAsset sqa)
            {
                var searchQueryPath = AssetDatabase.GetAssetPath(sqa);
                if (!string.IsNullOrEmpty(searchQueryPath))
                {
                    SaveSearchQueryFromContext(searchQueryPath, false);
                }
            }
            else if (activeQuery is SearchQuery sq)
            {
                sq.Set(viewState);
                SearchQuery.SaveSearchQuery(sq);
                SaveItemCountToPropertyDatabase(true);
            }
        }

        void ISearchQueryView.SaveUserSearchQuery()
        {
            SaveUserSearchQuery();
        }

        private void SaveUserSearchQuery()
        {
            var query = SearchQuery.AddUserQuery(viewState);
            AddNewQuery(query);
        }

        void ISearchQueryView.SaveProjectSearchQuery()
        {
            SaveProjectSearchQuery();
        }

        private void SaveProjectSearchQuery(in string path = null)
        {
            var initialFolder = SearchSettings.GetFullQueryFolderPath();
            var searchQueryFileName = SearchQueryAsset.GetQueryName(context.searchQuery);
            var searchQueryPath = string.IsNullOrWhiteSpace(path) ? EditorUtility.SaveFilePanel("Save search query...", initialFolder, searchQueryFileName, "asset") : path;
            if (string.IsNullOrEmpty(searchQueryPath))
                return;
            if (!Paths.IsValidAssetPath(searchQueryPath, ".asset", out var errorMessage))
            {
                Debug.LogWarning($"Search query path: {searchQueryPath} is invalid: {errorMessage}.");
                return;
            }

            searchQueryPath = Utils.CleanPath(searchQueryPath);
            if (!System.IO.Directory.Exists(Path.GetDirectoryName(searchQueryPath)) || !Utils.IsPathUnderProject(searchQueryPath))
                return;

            searchQueryPath = Utils.GetPathUnderProject(searchQueryPath);
            SearchSettings.queryFolder = Utils.CleanPath(Path.GetDirectoryName(searchQueryPath));

            SaveSearchQueryFromContext(searchQueryPath, true);
        }
        private void SaveSearchQueryFromContext(string searchQueryPath, bool newQuery)
        {
            try
            {
                var searchQuery = AssetDatabase.LoadAssetAtPath<SearchQueryAsset>(searchQueryPath) ?? SearchQueryAsset.Create(context);
                if (!searchQuery)
                    throw new Exception($"Failed to create search query asset {searchQueryPath}");

                var folder = Utils.CleanPath(Path.GetDirectoryName(searchQueryPath));
                var queryName = Path.GetFileNameWithoutExtension(searchQueryPath);
                var newContext = new SearchContext(context);
                searchQuery.viewState ??= new SearchViewState(newContext);
                searchQuery.viewState.Assign(viewState);

                if (SearchQueryAsset.SaveQuery(searchQuery, context, viewState, folder, queryName) && newQuery)
                {
                    Selection.activeObject = searchQuery;
                    AddNewQuery(searchQuery);
                }
                else
                    SaveItemCountToPropertyDatabase(true);
            }
            catch
            {
                Debug.LogError($"Failed to save search query at {searchQueryPath}");
            }
        }

        private void AddNewQuery(ISearchQuery newQuery)
        {
            SearchSettings.AddRecentSearch(newQuery.searchText);
            SearchQueryAsset.ResetSearchQueryItems();
            activeQuery = newQuery;
            SendNewQueryAnalyticsEvent(newQuery);

            if (IsSavedSearchQueryEnabled() && m_ViewState.flags.HasNone(SearchViewFlags.OpenLeftSidePanel))
                TogglePanelView(SearchViewFlags.OpenLeftSidePanel);

            SaveItemCountToPropertyDatabase(true);
        }

        void SendNewQueryAnalyticsEvent(ISearchQuery newQuery)
        {
            SearchAnalytics.GenericEvent evt = default;
            if (newQuery is SearchQueryAsset sqa)
                evt = CreateEvent(SearchAnalytics.GenericEventType.QuickSearchCreateSearchQuery, sqa.searchText, sqa.filePath, "project");
            else if (newQuery is SearchQuery sq && SearchQuery.IsUserQuery(sq))
                evt = CreateEvent(SearchAnalytics.GenericEventType.QuickSearchCreateSearchQuery, sq.searchText, "", "user");

            if (!string.IsNullOrEmpty(evt.windowId))
            {
                evt.intPayload1 = newQuery.GetSearchTable() != null ? 1 : 0;
                SearchAnalytics.SendEvent(evt);
            }
        }

        private void LoadContext()
        {
            m_ContextHash = context.GetHashCode();
            if (context.options.HasAny(SearchFlags.FocusContext))
            {
                var contextualProvider = GetContextualProvider();
                if (contextualProvider != null)
                    m_ContextHash = Utils.CombineHashCodes(m_ContextHash, contextualProvider.id.GetHashCode());
            }

            m_ContextHash = Utils.CombineHashCodes(m_ContextHash, GetType().Name.GetHashCode());
            if (context.filterType != null)
                m_ContextHash = Utils.CombineHashCodes(m_ContextHash, context.filterType.Name.GetHashCode());
            if (context.runtimeContext != null && !string.IsNullOrEmpty(context.runtimeContext.contextId))
                m_ContextHash = Utils.CombineHashCodes(m_ContextHash, context.runtimeContext.contextId.GetHashCode());
            context.searchView = this;
            Refresh();
        }

        protected void UpdateViewState(SearchViewState args)
        {
            if (context?.options.HasAny(SearchFlags.Expression) ?? false)
                itemIconSize = (int)DisplayMode.Table;
            else
                itemIconSize = args.itemSize;
        }

        protected virtual void LoadSessionSettings(SearchViewState args)
        {
            if (!Utils.IsRunningTests())
            {
                RestoreSearchText(args);

                if (args.flags.HasNone(SearchViewFlags.OpenInspectorPreview | SearchViewFlags.OpenLeftSidePanel | SearchViewFlags.HideSearchBar))
                {
                    if (SearchSettings.GetScopeValue(nameof(SearchViewFlags.OpenInspectorPreview), m_ContextHash, 0) != 0)
                        args.flags |= SearchViewFlags.OpenInspectorPreview;

                    if (SearchSettings.showSavedSearchPanel)
                        args.flags |= SearchViewFlags.OpenLeftSidePanel;
                }
            }

            string loadGroup = null;
            if (!Utils.IsRunningTests())
                loadGroup = SearchSettings.GetScopeValue(nameof(m_SearchView.currentGroup), m_ContextHash, viewState.group);

            if (context.options.HasAny(SearchFlags.FocusContext))
            {
                var contextualProvider = GetContextualProvider();
                if (contextualProvider != null)
                    loadGroup = contextualProvider.id;
            }

            args.group = viewState.hideTabs ? null : (loadGroup ?? args.group);
            if (viewState.hideAllGroup && (args.group == null || string.Equals("all", args.group, StringComparison.Ordinal)))
                args.group = args.context?.GetProviders().FirstOrDefault()?.id;

            if (Utils.IsRunningTests() && !string.IsNullOrEmpty(args.searchText))
                args.flags |= SearchViewFlags.DisableQueryHelpers;

            UpdateViewState(args);
        }

        protected virtual void RestoreSearchText(SearchViewState args)
        {
            if (!args.ignoreSaveSearches && args.context != null && string.IsNullOrEmpty(args.context.searchText))
            {
                args.searchText = SearchSettings.GetScopeValue(k_LastSearchPrefKey, m_ContextHash, "");
                if (args.context != null)
                {
                    args.context.searchText = args.searchText;
                    SearchSettings.ApplyContextOptions(args.context);
                }
            }
        }

        protected virtual void SaveSessionSettings()
        {
            SearchSettings.SetScopeValue(k_LastSearchPrefKey, m_ContextHash, context.searchText);
            SearchSettings.SetScopeValue(nameof(SearchViewFlags.OpenInspectorPreview), m_ContextHash, m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview) ? 1 : 0);

            if (m_SearchView != null)
                SearchSettings.SetScopeValue(nameof(m_SearchView.currentGroup), m_ContextHash, viewState.group);

            SearchSettings.Save();
        }

        private SearchAnalytics.GenericEvent CreateEvent(SearchAnalytics.GenericEventType category, string name = null, string message = null, string description = null)
        {
            var e = SearchAnalytics.GenericEvent.Create(windowId, category, name);
            e.message = message;
            e.description = description;
            return e;
        }

        protected void SendEvent(SearchAnalytics.GenericEventType category, string name = null, string message = null, string description = null)
        {
            SearchAnalytics.SendEvent(windowId, category, name, message, description);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
                Close();

            m_Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private SearchProvider GetContextualProvider()
        {
            return context.providers.FirstOrDefault(p => p.active && (p.isEnabledForContextualSearch?.Invoke() ?? false));
        }

        private void OnProviderActivationChanged(string providerId, bool isActive)
        {
            // If a provider was enabled in the settings, we do not want to mess with the current context. User might have disabled it in the CONTEXT for a reason.
            if (isActive)
                return;

            // Already disabled
            if (!context.IsEnabled(providerId))
                return;

            ToggleFilter(providerId);
        }

        // TODO: Use ISearchWindow when possible
        public static SearchWindow Create(SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create<SearchWindow>(flags);
        }

        public static SearchWindow Create<T>(SearchFlags flags = SearchFlags.OpenDefault) where T : SearchWindow
        {
            return Create<T>(null, null, flags);
        }

        public static SearchWindow Create(SearchContext context, string topic = "Unity", SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create<SearchWindow>(context, topic, flags);
        }

        public static SearchWindow Create<T>(SearchContext context, string topic = "Unity", SearchFlags flags = SearchFlags.OpenDefault) where T : SearchWindow
        {
            context = context ?? SearchService.CreateContext("");
            if (context != null)
                context.options |= flags;
            var viewState = new SearchViewState(context) { title = topic };
            return Create<T>(viewState.LoadDefaults());
        }

        public static ISearchWindow Create(SearchViewState viewArgs)
        {
            return Create<SearchWindow>(viewArgs);
        }

        public static T Create<T>(SearchViewState viewArgs) where T : SearchWindow
        {
            s_GlobalViewState = viewArgs;
            s_FocusedWindow = focusedWindow;

            var context = viewArgs.context;
            var flags = viewArgs.context?.options ?? SearchFlags.OpenDefault;
            SearchWindow searchWindow;
            if (flags.HasAny(SearchFlags.ReuseExistingWindow) && HasOpenInstances<T>())
            {
                searchWindow = Resources.FindObjectsOfTypeAll<SearchWindow>()
                    .Where(w => w.viewState.searchFlags.HasAny(SearchFlags.ReuseExistingWindow)
                        || (w.context?.options.HasAny(SearchFlags.ReuseExistingWindow) ?? false))
                    .FirstOrDefault();
                if (!searchWindow)
                {
                    searchWindow = CreateInstance<T>();
                }
                else if (context != null)
                {
                    if (context.empty)
                        context.searchText = searchWindow.context?.searchText ?? string.Empty;
                    searchWindow.SetContext(context);
                }
            }
            else
            {
                searchWindow = CreateInstance<T>();
            }

            return (T)searchWindow;
        }

        internal static SearchWindow Open(float width = defaultWidth, float height= defaultHeight, SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create(flags).ShowWindow(width, height, flags);
        }

        [MenuItem("Edit/Search All... %k", priority = 161)]
        internal static ISearchView OpenDefaultQuickSearch()
        {
            var window = Open(flags: SearchFlags.OpenGlobal);
            SearchAnalytics.SendEvent(window.state.sessionId, SearchAnalytics.GenericEventType.QuickSearchOpen, "Default");
            return window;
        }

        public ISearchView ShowWindow()
        {
            return ShowWindow(defaultWidth, defaultHeight, SearchFlags.OpenDefault);
        }

        public ISearchView ShowWindow(SearchFlags flags)
        {
            return ShowWindow(defaultWidth, defaultHeight, flags);
        }

        public ISearchView ShowWindow(float width, float height)
        {
            return ShowWindow(width, height, SearchFlags.OpenDefault);
        }

        ISearchView ISearchWindow.ShowWindow(float width, float height, SearchFlags flags)
        {
            return ShowWindow(width, height, flags);
        }

        public SearchWindow ShowWindow(float width, float height, SearchFlags flags)
        {
            using (new EditorPerformanceTracker("SearchView.ShowWindow"))
            {
                var windowSize = new Vector2(width, height);
                if (flags.HasAny(SearchFlags.Dockable) && viewState.flags.HasNone(SearchViewFlags.Borderless))
                {
                    bool firstOpen = Utils.IsRunningTests() || !EditorPrefs.HasKey(k_CheckWindowKeyName);
                    Show(true);
                    if (firstOpen)
                    {
                        var centeredPosition = Utils.GetMainWindowCenteredPosition(windowSize);
                        position = centeredPosition;
                    }
                    else if (!firstOpen && !docked)
                    {
                        var newWindow = this;
                        var existingWindow = Resources.FindObjectsOfTypeAll<SearchWindow>().FirstOrDefault(w => w != newWindow);
                        if (existingWindow)
                        {
                            var cascadedWindowPosition = existingWindow.position.position;
                            cascadedWindowPosition += new Vector2(30f, 30f);
                            position = new Rect(cascadedWindowPosition, position.size);
                        }
                    }
                }
                else
                {
                    this.ShowDropDown(windowSize);
                }
                Focus();
                return this;
            }
        }

        void ISearchView.SetupColumns(IList<SearchField> fields)
        {
            m_SearchView.SetupColumns(fields);
        }

        [CommandHandler("OpenQuickSearch")]
        internal static void OpenQuickSearchCommand(CommandExecuteContext c)
        {
            OpenDefaultQuickSearch();
        }

        [MenuItem("Window/Search/New Window", priority = 0)]
        public static void OpenNewWindow()
        {
            var window = Open(flags: SearchFlags.OpenDefault);
            SearchAnalytics.SendEvent(window.state.sessionId, SearchAnalytics.GenericEventType.QuickSearchOpen, "NewWindow");
        }

        [MenuItem("Window/Search/Transient Window", priority = 1)]
        public static void OpenPopupWindow()
        {
            if (SearchService.ShowWindow(defaultWidth: 600, defaultHeight: 400, dockable: false) is SearchWindow window)
                SearchAnalytics.SendEvent(window.windowId, SearchAnalytics.GenericEventType.QuickSearchOpen, "PopupWindow");
        }

        [CommandHandler("OpenQuickSearchInContext")]
        internal static void OpenQuickSearchInContextCommand(CommandExecuteContext c)
        {
            var query = c.GetArgument<string>(0);
            var sourceContext = c.GetArgument<string>(1);
            var wasReused = HasOpenInstances<SearchWindow>();
            var flags = SearchFlags.OpenContextual | SearchFlags.ReuseExistingWindow;
            var context = SearchService.CreateContext(query);
            context.options |= flags;
            var viewState = new SearchViewState(context) { title = null };
            var ignoreRestoreContext = c.GetArgument(2, false);
            viewState.ignoreSaveSearches = ignoreRestoreContext;
            var searchWindow = Create(viewState) as SearchWindow;
            SearchProvider contextualProvider = null;
            if (wasReused)
                contextualProvider = searchWindow.GetContextualProvider();
            searchWindow.ShowWindow(flags: flags);
            if (contextualProvider != null)
                searchWindow.SelectGroup(contextualProvider.id);
            searchWindow.SendEvent(SearchAnalytics.GenericEventType.QuickSearchJumpToSearch, searchWindow.currentGroup, sourceContext);
            ((ISearchView)searchWindow).syncSearch = true;
            c.result = true;
        }

        [Shortcut("Help/Search Contextual")]
        internal static void OpenContextual()
        {
            Open(flags: SearchFlags.OpenContextual);
        }

        internal void ForceTrackSelection()
        {
            m_SearchView.DelayTrackSelection();
        }

        protected virtual IEnumerable<SearchItem> FetchItems()
        {
            return Enumerable.Empty<SearchItem>();
        }

        IEnumerable<SearchItem> ISearchWindow.FetchItems()
        {
            return FetchItems();
        }

        bool ISearchWindow.HasFocus()
        {
            return hasFocus;
        }

        [WindowAction]
        internal static WindowAction CreateSearchHelpWindowAction()
        {
            // Developer-mode render doc button to enable capturing any HostView content/panels
            var action = WindowAction.CreateWindowActionButton("HelpSearch", OpenSearchHelp, null, ContainerWindow.kButtonWidth + 1, Icons.help);
            action.validateHandler = (window, _) => window && window.GetType() == typeof(SearchWindow);
            return action;
        }

        internal static string GetHelpURL()
        {
            return Help.FindHelpNamed("search-overview");
        }

        private static void OpenSearchHelp(EditorWindow window, WindowAction action)
        {
            var windowId = (window as SearchWindow)?.windowId ?? null;
            SearchAnalytics.SendEvent(windowId, SearchAnalytics.GenericEventType.QuickSearchOpenDocLink);
            EditorUtility.OpenWithDefaultApp(GetHelpURL());
        }
    }
}
