// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ShortcutManagement;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Search;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Search
{
    static class EventModifiersExtensions
    {
        public static bool HasAny(this EventModifiers flags, EventModifiers f) => (flags & f) != 0;
        public static bool HasAll(this EventModifiers flags, EventModifiers all) => (flags & all) == all;
    }

    [EditorWindowTitle(title = "Search")]
    class QuickSearch : EditorWindow, ISearchView, IDisposable, IHasCustomMenu
    {
        internal const string k_TogleSyncShortcutName = "Search/Toggle Sync Search View";
        internal const string k_QueryItemsNumberPropertyName = "TotalQueryItemsNumber";
        internal const string k_LastUsedTimePropertyName = "LastUsedTime";

        internal enum SearchEventStatus
        {
            DoNotSendEvent,
            WaitForEvent,
            EventSent,
        }

        const int k_ResetSelectionIndex = -1;
        const string k_LastSearchPrefKey = "last_search";
        const string k_SideBarWidthKey = "Search.SidebarWidth";
        const string k_DetailsWidthKey = "Search.DetailsWidth";
        const float k_DetailsViewShowMinSize = 450f;
        const int k_MinimumGroupVisible = 1;
        private static readonly string k_CheckWindowKeyName = $"{typeof(QuickSearch).FullName}h";
        private static int s_SavedSearchFieldHash = "SavedSearchField".GetHashCode();

        private static EditorWindow s_FocusedWindow;
        private static SearchViewState s_GlobalViewState = null;

        protected GroupedSearchList m_FilteredItems;
        private readonly List<int> m_Selection = new List<int>();
        private int m_DelayedCurrentSelection = k_ResetSelectionIndex;
        private SearchSelection m_SearchItemSelection;
        private string m_LastSelectedMoreGroup;

        private bool m_Disposed = false;
        private DetailView m_DetailView;
        private IResultView m_ResultView;
        private float m_PreviousItemSize = -1;
        private RefreshFlags m_DebounceRefreshFlags;
        private Action m_DebounceOff = null;
        private bool m_SyncSearch;
        private SearchQueryTreeView m_QueryTreeView;
        private UI.SearchField m_SearchField;
        private bool m_ShowSideBar;
        private bool m_ShowDetails;
        private Action m_WaitAsyncResults;
        private List<SearchProvider> m_AvailableProviders;

        private SearchMonitorView m_SearchMonitorView;

        private QueryBuilder m_QueryBuilder;
        private Rect m_QueryHelperRect;
        private QueryHelperWidget m_QueryHelper;

        
        internal QueryBuilder queryBuilder => m_QueryBuilder;
        private QueryHelperWidget queryHelper
        {
            get
            {
                if (m_QueryHelper == null)
                    CreateQueryHelper();
                return m_QueryHelper;
            }
        }

        [SerializeField] private TreeViewState m_QueryTreeViewState;
        [SerializeField] protected SearchViewState m_ViewState = null;
        [SerializeField] protected EditorWindow m_LastFocusedWindow;
        [SerializeField] private bool m_SearchBoxFocus;
        [SerializeField] private SplitterInfo m_SideBarSplitter;
        [SerializeField] private SplitterInfo m_DetailsPanelSplitter;
        [SerializeField] protected int m_ContextHash;
        [SerializeField] internal SearchEventStatus searchEventStatus;
        [SerializeField] private bool m_FilterSearchQueryToggle;
        [SerializeField] internal SearchQuery activeSearchQuery;
        [SerializeField] private Vector2 m_LeftPanelScrollPosition;
        [SerializeField] private bool m_FocusSavedSearchField;
        [SerializeField] private UndoManager m_UndoManager;

        private event Action nextFrame;
        private event Action<Vector2, Vector2> resized;

        public Action<SearchItem, bool> selectCallback { get => (item, canceled) => m_ViewState.selectHandler?.Invoke(item, canceled); set => m_ViewState.selectHandler = value; }
        public Func<SearchItem, bool> filterCallback { get => (item) => m_ViewState.filterHandler?.Invoke(item) ?? true; set => m_ViewState.filterHandler = value; }
        public Action<SearchItem> trackingCallback { get => m_ViewState.trackingHandler; set => m_ViewState.trackingHandler = value; }


        internal bool searchInProgress => (context?.searchInProgress ?? false) || m_DebounceOff != null || m_WaitAsyncResults != null;
        internal string currentGroup { get => m_FilteredItems.currentGroup; set => SelectGroup(value); }
        internal SearchViewState viewState => m_ViewState;
        internal IReadOnlyCollection<SearchProvider> availableProviders => m_AvailableProviders;
        internal static readonly int generalWindowContextHash = SearchFlags.GeneralSearchWindow.ToString().GetHashCode();
        internal int contextHash => m_ContextHash;

        internal ISearchQuery activeQuery
        {
            get => m_QueryTreeView.GetCurrentQuery();
            set => m_QueryTreeView.SetCurrentQuery(value);
        }

        public SearchSelection selection
        {
            get
            {
                if (m_SearchItemSelection == null)
                    m_SearchItemSelection = new SearchSelection(m_Selection, m_FilteredItems);
                return m_SearchItemSelection;
            }
        }

        public SearchContext context => m_ViewState.context;
        public ISearchList results => m_FilteredItems;
        public DisplayMode displayMode => GetDisplayModeFromItemSize(m_ViewState.itemSize);
        public float itemIconSize { get => m_ViewState.itemSize; set => UpdateItemSize(value); }
        public bool multiselect
        {
            get => m_ViewState.context.options.HasAny(SearchFlags.Multiselect);
            set
            {
                if (value)
                    m_ViewState.context.options |= SearchFlags.Multiselect;
                else
                    m_ViewState.context.options &= ~SearchFlags.Multiselect;
            }
        }

        public bool syncSearch
        {
            get => m_SyncSearch;
            set
            {
                if (value == m_SyncSearch)
                    return;

                m_SyncSearch = value;
                if (value)
                    NotifySyncSearch(m_FilteredItems.currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.StartSession);
                else
                    NotifySyncSearch(m_FilteredItems.currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.EndSession);
            }
        }

        internal string windowId => m_ViewState.sessionId;
        internal IResultView resultView => m_ResultView;
        int ISearchView.cursorIndex => m_SearchField?.cursorIndex ?? 0;


        internal bool searchFieldHasFocus => m_SearchField.hasFocus;

        private string searchTopicPlaceHolder => $"Search {m_ViewState.title}";

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.Default)
        {
            SetSearchText(searchText, moveCursor, -1);
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            if (context == null)
                return;
            if (string.Equals(context.searchText, searchText, StringComparison.Ordinal))
                return;
            context.searchText = searchText ?? string.Empty;
            RefreshSearch();
            if (moveCursor != TextCursorPlacement.None)
                SetTextEditorState(searchText, te => m_SearchField.MoveCursor(moveCursor, cursorInsertPosition));

            if (viewState.queryBuilderEnabled && queryBuilder != null && queryBuilder.BuildQuery() != searchText)
            {
                RefreshBuilder();
            }
        }

        internal bool IsGeneralSearchWindow()
        {
            return context.options.HasFlag(SearchFlags.GeneralSearchWindow);
        }

        internal bool HasSessionSettings()
        {
            return m_ContextHash != 0;
        }

        private void SetTextEditorState(string searchText, Action<TextEditor> handler, bool selectAll = false)
        {
            nextFrame += () =>
            {
                var te = m_SearchField.GetTextEditor();
                if (searchText != null)
                    te.text = searchText;
                handler?.Invoke(te);
                if (selectAll)
                    te.SelectAll();
                Repaint();
            };
            Repaint();
        }

        public virtual void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            m_DebounceRefreshFlags |= flags;
            DebounceRefresh();
        }

        protected void RefreshSearch()
        {
            m_DebounceOff?.Invoke();
            m_DebounceOff = null;

            if (context == null)
                return;

            ClearCurrentErrors();
            SetItems(FetchItems());
            SaveItemCountToPropertyDatabase(false);

            if (syncSearch)
                NotifySyncSearch(m_FilteredItems.currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.SyncSearch);

            WaitForAsynResults();
        }

        protected virtual IEnumerable<SearchItem> FetchItems()
        {
            SearchSettings.ApplyContextOptions(context);
            return SearchService.GetItems(context, context.options);
        }

        public static QuickSearch Create(SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create<QuickSearch>(flags);
        }

        public static QuickSearch Create<T>(SearchFlags flags = SearchFlags.OpenDefault) where T : QuickSearch
        {
            return Create<T>(null, null, flags);
        }

        public static QuickSearch Create(SearchContext context, string topic = "Unity", SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create<QuickSearch>(context, topic, flags);
        }

        public static QuickSearch Create<T>(SearchContext context, string topic = "Unity", SearchFlags flags = SearchFlags.OpenDefault) where T : QuickSearch
        {
            context = context ?? SearchService.CreateContext("");
            if (context != null)
                context.options |= flags;
            var viewState = new SearchViewState(context) { title = topic };
            return Create<T>(viewState.LoadDefaults());
        }

        public static QuickSearch Create(SearchViewState viewArgs)
        {
            return Create<QuickSearch>(viewArgs);
        }

        public static QuickSearch Create<T>(SearchViewState viewArgs) where T : QuickSearch
        {
            s_GlobalViewState = viewArgs;
            s_FocusedWindow = focusedWindow;

            var context = viewArgs.context;
            var flags = viewArgs.context?.options ?? SearchFlags.OpenDefault;
            QuickSearch qsWindow;
            if (flags.HasAny(SearchFlags.ReuseExistingWindow) && HasOpenInstances<T>())
            {
                qsWindow = Resources.FindObjectsOfTypeAll<QuickSearch>()
                    .Where(w => w.viewState.searchFlags.HasAny(SearchFlags.ReuseExistingWindow)
                        || (w.context?.options.HasAny(SearchFlags.ReuseExistingWindow) ?? false))
                    .FirstOrDefault();
                if (!qsWindow)
                {
                    qsWindow = CreateInstance<T>();
                }
                else if (context != null)
                {
                    if (context.empty)
                        context.searchText = qsWindow.context?.searchText ?? string.Empty;
                    qsWindow.SetContext(context);
                    qsWindow.RefreshSearch();
                }
            }
            else
            {
                qsWindow = CreateInstance<T>();
            }

            // Ensure we won't send events while doing a domain reload.
            qsWindow.searchEventStatus = SearchEventStatus.WaitForEvent;
            return qsWindow;
        }

        private void SetContext(SearchContext newContext)
        {
            if (context == null || context != newContext)
            {
                var searchText = context?.searchText ?? string.Empty;
                context?.Dispose();
                m_ViewState.context = newContext ?? SearchService.CreateContext(searchText, SearchFlags.None);
            }

            context.searchView = this;
            context.focusedWindow = m_LastFocusedWindow;
            context.asyncItemReceived -= OnAsyncItemsReceived;
            context.asyncItemReceived += OnAsyncItemsReceived;

            m_FilteredItems?.Dispose();
            m_FilteredItems = new GroupedSearchList(context);
            m_FilteredItems.currentGroup = m_ViewState.group;

            ComputeContextHash();
        }

        public static QuickSearch Open(float defaultWidth = 950, float defaultHeight = 539, SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create(flags).ShowWindow(defaultWidth, defaultHeight, flags);
        }

        public QuickSearch ShowWindow(float defaultWidth = 950, float defaultHeight = 538, SearchFlags flags = SearchFlags.OpenDefault)
        {
            var windowSize = new Vector2(defaultWidth, defaultHeight);
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
                    var existingWindow = Resources.FindObjectsOfTypeAll<QuickSearch>().FirstOrDefault(w => w != newWindow);
                    if (existingWindow)
                    {
                        var cascadedWindowPosition = existingWindow.position.position;
                        cascadedWindowPosition += new Vector2(30f, 30f);
                        this.position = new Rect(cascadedWindowPosition, this.position.size);
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

        public void SetSelection(params int[] selection)
        {
            SetSelection(true, selection, false);
        }

        public void SetColumns(IEnumerable<SearchColumn> columns)
        {
            if (viewState.tableConfig == null)
                throw new NotSupportedException("This result view cannot set columns");

            viewState.tableConfig.columns = columns.ToArray();
        }

        internal static IEnumerable<SearchProvider> GetCurrentSearchWindowProviders()
        {
            if (HasOpenInstances<QuickSearch>())
            {
                return GetWindow<QuickSearch>(false, null, false).context.GetProviders();
            }

            return SearchService.GetActiveProviders();
        }

        internal static IEnumerable<SearchProvider> GetMergedProviders(IEnumerable<SearchProvider> initialProviders, IEnumerable<string> providerIds)
        {
            var providers = SearchService.GetProviders(providerIds);
            if (initialProviders == null)
                return providers;

            return initialProviders.Concat(providers).Distinct();
        }

        private void SetSelection(bool trackSelection, int[] selection, bool forceChange = false)
        {
            if (!multiselect && selection.Length > 1)
                selection = new int[] { selection[selection.Length - 1] };

            var lastIndexAdded = k_ResetSelectionIndex;

            m_Selection.Clear();
            m_SearchItemSelection = null;
            foreach (var idx in selection)
            {
                if (!IsItemValid(idx))
                    continue;

                m_Selection.Add(idx);
                lastIndexAdded = idx;
            }

            if (lastIndexAdded != k_ResetSelectionIndex || forceChange)
            {
                m_SearchItemSelection = null;
                if (trackSelection)
                    TrackSelection(lastIndexAdded);
            }
        }

        public void AddSelection(params int[] selection)
        {
            if (!multiselect && m_Selection.Count == 1)
                throw new Exception("Multi selection is not allowed.");

            var lastIndexAdded = k_ResetSelectionIndex;

            foreach (var idx in selection)
            {
                if (!IsItemValid(idx))
                    continue;

                if (m_Selection.Contains(idx))
                {
                    m_Selection.Remove(idx);
                }
                else
                {
                    m_Selection.Add(idx);
                    lastIndexAdded = idx;
                }
            }

            if (lastIndexAdded != k_ResetSelectionIndex)
            {
                m_SearchItemSelection = null;
                TrackSelection(lastIndexAdded);
            }
        }

        public void ExecuteSearchQuery(ISearchQuery query)
        {
            SetSelection();

            var queryContext = CreateQueryContext(query);
            SetContext(queryContext);
            var possibleTextQuery = query as SearchQuery;
            if (possibleTextQuery == null || !possibleTextQuery.isTextOnlyQuery)
            {
                var viewState = query.GetViewState();
                SetViewState(viewState);
            }

            RefreshSearch();
            SetTextEditorState(queryContext.searchText, te => te.MoveLineEnd(), selectAll: false);
            SearchQueryAsset.AddToRecentSearch(query);

            var evt = CreateEvent(SearchAnalytics.GenericEventType.QuickSearchSavedSearchesExecuted, query.searchText, "", query is SearchQueryAsset ? "project" : "user");
            evt.intPayload1 = viewState.tableConfig != null ? 1 : 0;
            SearchAnalytics.SendEvent(evt);

            activeQuery = query;

            SaveItemCountToPropertyDatabase(false);
            SaveLastUsedTimeToPropertyDatabase();
        }

        internal virtual SearchContext CreateQueryContext(ISearchQuery query)
        {
            var providers = context?.GetProviders() ?? SearchService.GetActiveProviders();
            return SearchService.CreateContext(GetMergedProviders(providers, query.GetProviderIds()), query.searchText, context?.options ?? SearchFlags.Default);
        }

        internal void SetViewState(SearchViewState viewState)
        {
            itemIconSize = viewState.itemSize;
            m_ResultView.SetViewState(viewState);
            if (!viewState.hideTabs && !string.IsNullOrEmpty(viewState.group))
                SelectGroup(viewState.group);
            if (viewState.queryBuilderEnabled)
                m_ViewState.queryBuilderEnabled = viewState.queryBuilderEnabled;
            RefreshBuilder();
        }

        public virtual void ExecuteSelection()
        {
            ExecuteSelection(0);
        }

        internal void ExecuteSelection(int actionIndex)
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
            var item = items.LastOrDefault();
            if (item == null)
                return;

            SendSearchEvent(item, action);
            EditorApplication.delayCall -= DelayTrackSelection;

            if (action.handler != null && items.Length == 1)
                action.handler(item);
            else if (action.execute != null)
                action.execute(items);
            else
                action.handler?.Invoke(item);

            if (endSearch && action.closeWindowAfterExecution && !docked)
                CloseSearchWindow();
        }

        public void ShowItemContextualMenu(SearchItem item, Rect position)
        {
            if (IsPicker())
                return;

            SendEvent(SearchAnalytics.GenericEventType.QuickSearchShowActionMenu, item.provider.id);
            var menu = new GenericMenu();
            var shortcutIndex = 0;

            var useSelection = context?.selection?.Any(e => string.Equals(e.id, item.id, StringComparison.OrdinalIgnoreCase)) ?? false;
            var currentSelection = useSelection ? context.selection : new SearchSelection(new[] { item });
            foreach (var action in item.provider.actions.Where(a => a.enabled?.Invoke(currentSelection) ?? true))
            {
                var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                if (shortcutIndex == 0)
                    itemName += " _enter";
                else if (shortcutIndex == 1)
                    itemName += " _&enter";

                menu.AddItem(new GUIContent(itemName, action.content.image), false, () => ExecuteAction(action, currentSelection.ToArray(), false));
                ++shortcutIndex;
            }

            menu.AddSeparator("");
            if (SearchSettings.searchItemFavorites.Contains(item.id))
                menu.AddItem(new GUIContent("Remove from Favorites"), false, () => SearchSettings.RemoveItemFavorite(item));
            else
                menu.AddItem(new GUIContent("Add to Favorites"), false, () => SearchSettings.AddItemFavorite(item));

            if (position == default)
                menu.ShowAsContext();
            else
                menu.DropDown(position);
        }

        internal virtual void OnEnable()
        {
            hideFlags |= HideFlags.DontSaveInEditor;
            m_LastFocusedWindow = m_LastFocusedWindow ?? s_FocusedWindow;
            wantsLessLayoutEvents = true;

            m_ViewState = s_GlobalViewState ?? m_ViewState ?? SearchViewState.LoadDefaults();
            titleContent = HasCustomTitle() ? m_ViewState.windowTitle : EditorGUIUtility.TrTextContent("Search", Icons.quickSearchWindow);

            m_SearchMonitorView = SearchMonitor.GetView();

            InitializeSplitters();
            InitializeSavedSearches();

            SetContext(m_ViewState.context);
            LoadSessionSettings();
            activeQuery = m_ViewState.activeQuery;
            Refresh();

            SearchSettings.SortActionsPriority();
            m_SearchField = new UI.SearchField();
            m_DetailView = new DetailView(this);
            m_UndoManager = new UndoManager(context.searchText);

            resized += OnWindowResized;

            RefreshBuilder();

            UpdateWindowTitle();
            var contextProviders = context.GetProviders();
            m_AvailableProviders = SearchUtils.SortProvider(IsGeneralSearchWindow()
                    ? contextProviders.Concat(SearchService.Providers).Distinct()
                    : contextProviders).ToList();

            s_GlobalViewState = null;
        }

        private void InitializeSavedSearches()
        {
            m_QueryTreeViewState = m_QueryTreeViewState ?? new TreeViewState() { expandedIDs = SearchSettings.expandedQueries.ToList() };
            m_QueryTreeView = new SearchQueryTreeView(m_QueryTreeViewState, this);
        }

        private bool HasCustomTitle()
        {
            return viewState.windowTitle != null && !string.IsNullOrEmpty(viewState.windowTitle.text);
        }

        private void InitializeSplitters()
        {
            m_SideBarSplitter = m_SideBarSplitter ?? new SplitterInfo(SplitterInfo.Side.Left, 0.15f, 0.25f, this);
            m_SideBarSplitter.host = this;

            m_DetailsPanelSplitter = m_DetailsPanelSplitter ?? new SplitterInfo(SplitterInfo.Side.Right, 0.5f, 0.80f, this);
            m_DetailsPanelSplitter.host = this;

            Utils.CallDelayed(() =>
            {
                if (m_SideBarSplitter.pos <= 0f)
                    m_SideBarSplitter.SetPosition(EditorPrefs.GetFloat(k_SideBarWidthKey, -1f));
                if (m_DetailsPanelSplitter.pos <= 0f)
                    m_DetailsPanelSplitter.SetPosition(EditorPrefs.GetFloat(k_DetailsWidthKey, -1f));
            });
        }

        internal virtual void OnDisable()
        {
            s_FocusedWindow = null;
            AutoComplete.Clear();

            syncSearch = false;

            resized = null;
            nextFrame = null;
            m_DebounceOff?.Invoke();
            m_DebounceOff = null;
            m_WaitAsyncResults?.Invoke();
            m_WaitAsyncResults = null;
            EditorApplication.delayCall -= DelayTrackSelection;

            try
            {
                selectCallback?.Invoke(selection?.FirstOrDefault(), selection == null || selection.Count == 0);
            }
            catch
            {
            }

            SaveSessionSettings();

            m_DetailView?.Dispose();
            m_ResultView?.Dispose();

            m_SearchMonitorView.Dispose();

            // End search session
            context.asyncItemReceived -= OnAsyncItemsReceived;
            context.Dispose();
        }

        internal void OnGUI()
        {
            if (context == null)
                return;

            var evt = Event.current;
            var eventType = evt.rawType;
            if (eventType == EventType.Repaint)
            {
                var newWindowSize = position.size;
                if (!newWindowSize.Equals(m_ViewState.position.size))
                {
                    if (m_ViewState.position.size.x > 0)
                        resized?.Invoke(m_ViewState.position.size, newWindowSize);
                    m_ViewState.position.size = newWindowSize;
                }
            }

            HandleKeyboardNavigation(evt);
            if (context == null)
                return;

            using (new EditorGUILayout.VerticalScope(GUIStyle.none))
            {
                var hideSearchBar = m_ViewState.flags.HasAny(SearchViewFlags.HideSearchBar) || position.width < 150f;
                if (!hideSearchBar)
                {
                    UpdateFocusControlState(evt);
                    DrawToolbar(evt);
                }

                DrawPanels(evt);
                DrawStatusBar();
                if (!hideSearchBar)
                {
                    EditorGUI.BeginChangeCheck();
                    var newSearchText = AutoComplete.Draw(m_SearchField);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_QueryBuilder != null)
                        {
                            m_QueryBuilder.wordText = newSearchText;
                        }
                        else
                        {
                            context.searchText = newSearchText;
                            DebounceRefresh();
                        }
                    }
                }
            }

            if (eventType == EventType.Repaint)
            {
                nextFrame?.Invoke();
                nextFrame = null;
            }

            if (evt.type == EventType.Repaint && (m_Parent?.window?.isPopup ?? false))
            {
                Styles.panelBorder.Draw(new Rect(0, 0, position.width, position.height), GUIContent.none, 0);
            }
        }

        private void NotifySyncSearch(string groupId, UnityEditor.SearchService.SearchService.SyncSearchEvent evt)
        {
            var syncViewId = groupId;
            switch (groupId)
            {
                case "asset":
                    syncViewId = typeof(ProjectSearchEngine).FullName;
                    break;
                case "scene":
                    syncViewId = typeof(SceneSearchEngine).FullName;
                    break;
            }
            UnityEditor.SearchService.SearchService.NotifySyncSearchChanged(evt, syncViewId, context.searchText);
        }


        protected virtual bool IsSavedSearchQueryEnabled()
        {
            if (m_ViewState.HasFlag(SearchViewFlags.DisableSavedSearchQuery))
                return false;
            return true;
        }

        private void DrawPanels(Event evt)
        {
            using (var s = new EditorGUILayout.HorizontalScope())
            {
                var shrinkedView = position.width <= k_DetailsViewShowMinSize;
                m_ShowSideBar = !shrinkedView && m_ViewState.isQueryPanelVisible;
                m_ShowDetails = !shrinkedView && m_ViewState.isInspectorPanelVisible;

                var windowWidth = position.width;
                var resultViewSize = windowWidth;

                if (m_ShowSideBar)
                {
                    DrawSideBar(evt, s.rect);
                    resultViewSize -= m_SideBarSplitter.width + 1f;
                }

                if (m_ShowDetails)
                {
                    m_DetailsPanelSplitter.Init(windowWidth - 250f);
                    m_DetailsPanelSplitter.Draw(evt, s.rect);

                    resultViewSize -= m_DetailsPanelSplitter.width - 1;
                }

                DrawItems(evt, Mathf.Round(resultViewSize));

                if (m_ShowDetails)
                {
                    m_DetailView.Draw(context, m_DetailsPanelSplitter.width);
                }
            }
        }

        private void DrawSideBar(Event evt, Rect areaRect)
        {
            m_SideBarSplitter.Init(180f);
            m_SideBarSplitter.Draw(evt, areaRect);

            m_LeftPanelScrollPosition = Utils.BeginPanelView(m_LeftPanelScrollPosition, Styles.panelBackgroundLeft);
            m_ResultView?.DrawControlLayout(m_SideBarSplitter.width);

            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.Height(24f));
            {
                GUILayout.Label(Styles.saveSearchesIconContent, Styles.panelHeaderIcon);
                GUILayout.Label(Styles.saveSearchesContent, Styles.panelHeader);
                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                m_FilterSearchQueryToggle = GUILayout.Toggle(m_FilterSearchQueryToggle, Styles.toggleSavedSearchesTextfieldContent, Styles.savedSearchesHeaderButton);
                if (EditorGUI.EndChangeCheck())
                {
                    m_QueryTreeView.searchString = string.Empty;
                    s_SavedSearchFieldHash = Guid.NewGuid().ToString().GetHashCode();
                    if (m_FilterSearchQueryToggle)
                        m_FocusSavedSearchField = true;
                    else
                        FocusSearch();
                }

                if (EditorGUILayout.DropdownButton(Styles.sortButtonContent, FocusType.Passive, Styles.savedSearchesHeaderButton))
                {
                    var filterMenu = new GenericMenu();
                    var currentSortingOrder = SearchSettings.savedSearchesSortOrder;
                    var enumData = EnumDataUtility.GetCachedEnumData(typeof(SearchQuerySortOrder));
                    var options = EditorGUI.EnumNamesCache.GetEnumTypeLocalizedGUIContents(typeof(SearchQuerySortOrder), enumData);
                    for (var i = 0; i < options.Length; ++i)
                    {
                        var sortOrder = (SearchQuerySortOrder)i;
                        filterMenu.AddItem(options[i], sortOrder == currentSortingOrder, () => FilterQueries(sortOrder));
                    }
                    filterMenu.ShowAsContext();
                }
                GUILayout.Space(2);
            }
            GUILayout.EndHorizontal();

            if (m_FilterSearchQueryToggle)
            {
                Rect rect = GUILayoutUtility.GetRect(-1, 18f, Styles.toolbarSearchField);
                int searchFieldControlID = GUIUtility.GetControlID(s_SavedSearchFieldHash, FocusType.Passive, rect);

                if (m_FocusSavedSearchField)
                {
                    GUIUtility.keyboardControl = searchFieldControlID;
                    EditorGUIUtility.editingTextField = true;
                    if (Event.current.type == EventType.Repaint)
                        m_FocusSavedSearchField = false;
                }

                m_QueryTreeView.searchString = EditorGUI.ToolbarSearchField(searchFieldControlID, rect, m_QueryTreeView.searchString, false);
            }
            var treeViewRect = EditorGUILayout.GetControlRect(false, -1, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.MaxWidth(Mathf.Ceil(m_SideBarSplitter.width - 1)));
            m_QueryTreeView.OnGUI(treeViewRect);
            Utils.EndPanelView();
        }

        internal bool CanSaveQuery()
        {
            return !string.IsNullOrWhiteSpace(context.searchQuery);
        }

        internal void FilterQueries(SearchQuerySortOrder order)
        {
            SearchSettings.savedSearchesSortOrder = order;
            SearchSettings.Save();

            m_QueryTreeView.SortBy(order);
        }

        internal void OnLostFocus()
        {
            AutoComplete.Clear();
        }

        internal void Update()
        {
            if (context == null || focusedWindow != this)
                return;

            if (context.options.HasAny(SearchFlags.Debug))
                return;

            var time = EditorApplication.timeSinceStartup;
            var repaintRequested = hasFocus && (m_SearchField?.UpdateBlinkCursorState(time) ?? false);
            if (repaintRequested)
                Repaint();

            m_UndoManager.Save(time, context.searchText, m_SearchField.GetTextEditor());
        }

        private void SetItems(IEnumerable<SearchItem> items)
        {
            m_SearchItemSelection = null;
            m_FilteredItems.Clear();
            if (m_ViewState.filterHandler != null)
                items = items.Where(item => m_ViewState.filterHandler(item));
            m_FilteredItems.AddItems(items);
            if (!string.IsNullOrEmpty(context.filterId))
                m_FilteredItems.AddGroup(context.providers.First());
            SetSelection(trackSelection: false, m_Selection.ToArray(), false);
        }

        private void RefreshViews(RefreshFlags additionalFlags = RefreshFlags.None)
        {
            UpdateWindowTitle();

            m_ResultView?.Refresh(m_DebounceRefreshFlags | additionalFlags);
            m_DetailView?.Refresh(m_DebounceRefreshFlags | additionalFlags);
            m_DebounceRefreshFlags = RefreshFlags.None;

            Repaint();
        }

        private void SaveItemCountToPropertyDatabase(bool isSaving)
        {
            if (activeQuery == null)
                return;

            if (activeQuery.searchText != context.searchText && !isSaving)
                return;

            using (var view = SearchMonitor.GetView())
            {
                var recordKey = PropertyDatabase.CreateRecordKey(activeQuery.guid, k_QueryItemsNumberPropertyName);
                view.StoreProperty(recordKey, m_FilteredItems.GetItemCount(activeQuery.GetProviderTypes()));
            }
        }

        private void SaveLastUsedTimeToPropertyDatabase()
        {
            using (var view = SearchMonitor.GetView())
            {
                if (activeQuery == null)
                    return;

                var recordKey = PropertyDatabase.CreateRecordKey(activeQuery.guid, k_LastUsedTimePropertyName);
                view.StoreProperty(recordKey, DateTime.Now.Ticks);
            }
        }

        protected virtual void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            if (m_ViewState.filterHandler != null)
                items = items.Where(item => m_ViewState.filterHandler(item));
            m_FilteredItems.AddItems(items);
            if (context.searchInProgress)
                WaitForAsynResults();
            else
            {
                m_WaitAsyncResults?.Invoke();
                UpdateAsyncResults();
            }
        }

        private void WaitForAsynResults()
        {
            m_WaitAsyncResults?.Invoke();
            m_WaitAsyncResults = Utils.CallDelayed(UpdateAsyncResults, 0.1d);
        }

        protected virtual void UpdateAsyncResults()
        {
            if (!this)
                return;

            m_WaitAsyncResults = null;
            RefreshViews(RefreshFlags.ItemsChanged);
            SaveItemCountToPropertyDatabase(false);
        }

        internal bool ToggleFilter(string providerId)
        {
            var toggledEnabled = !context.IsEnabled(providerId);
            var provider = SearchService.GetProvider(providerId);
            if (provider != null && HasSessionSettings())
            {
                SearchService.SetActive(providerId, toggledEnabled);
                SearchSettings.Save();
            }
            if (providerId == m_FilteredItems.currentGroup)
                SelectGroup(null);
            context.SetFilter(providerId, toggledEnabled);
            if (toggledEnabled && provider == null && !context.providers.Any(p => p.id == providerId))
            {
                // Provider that are not stored in the SearchService, might only exists in the m_AvailableProviders (local providers created directly in the context).
                var localProvider = m_AvailableProviders.FirstOrDefault(p => p.id == providerId);
                if (localProvider != null)
                {
                    var newProviderList = context.GetProviders().Concat(new[] { localProvider }).ToArray();
                    context.SetProviders(newProviderList);
                }
            }

            ClearQueryHelper();
            SendEvent(SearchAnalytics.GenericEventType.FilterWindowToggle, providerId, context.IsEnabled(providerId).ToString());
            Refresh();
            return toggledEnabled;
        }

        private void TogglePanelView(SearchViewFlags panelOption)
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
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            AddItemsToMenu(menu, "");
        }

        public void AddItemsToMenu(GenericMenu menu, string optionPrefix)
        {
            if (!IsPicker())
            {
                menu.AddItem(new GUIContent("Preferences"), false, () => OpenPreferences());
                menu.AddSeparator("");
            }

            var savedSearchContent = new GUIContent("Searches");
            var previewInspectorContent = new GUIContent("Inspector");
            var wantsMoreContent = new GUIContent($"{optionPrefix}Show more results");

            if (IsSavedSearchQueryEnabled())
                menu.AddItem(savedSearchContent, m_ViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel), () => TogglePanelView(SearchViewFlags.OpenLeftSidePanel));
            if (m_ViewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
                menu.AddItem(previewInspectorContent, m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview), () => TogglePanelView(SearchViewFlags.OpenInspectorPreview));
            if (!m_ViewState.hasQueryBuilderToggle)
                menu.AddItem(new GUIContent($"Query Builder\tF1"), viewState.queryBuilderEnabled, () => ToggleQueryBuilder());
            if (IsSavedSearchQueryEnabled() || m_ViewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
                menu.AddSeparator("");
            if (Utils.isDeveloperBuild)
                menu.AddItem(new GUIContent($"{optionPrefix}Debug"), context?.options.HasAny(SearchFlags.Debug) ?? false, () => ToggleDebugQuery());

            if (!IsPicker())
            {
                menu.AddItem(new GUIContent($"{optionPrefix}Keep Open"), SearchSettings.keepOpen, () => ToggleKeepOpen());
                menu.AddItem(new GUIContent($"{optionPrefix}Show Tabs"), !viewState.hideTabs, () => ToggleShowTabs());
            }
            menu.AddItem(new GUIContent($"{optionPrefix}Show Status"), SearchSettings.showStatusBar, () => ToggleShowStatusBar());
            menu.AddItem(new GUIContent($"{optionPrefix}Show Packages results"), context.options.HasAny(SearchFlags.Packages), () => TogglePackages());
            menu.AddItem(wantsMoreContent, context?.wantsMore ?? false, () => ToggleWantsMore());
        }

        private void ToggleShowTabs()
        {
            viewState.hideTabs = !viewState.hideTabs;
            SearchSettings.hideTabs = viewState.hideTabs;
            SelectGroup(null);
            Refresh();
        }

        private void ToggleQueryBuilder()
        {
            if (!viewState.hasQueryBuilderToggle)
                return;
            SearchSettings.queryBuilder = viewState.queryBuilderEnabled = !viewState.queryBuilderEnabled;
            SearchSettings.Save();
            var evt = CreateEvent(SearchAnalytics.GenericEventType.QuickSearchToggleBuilder, viewState.queryBuilderEnabled.ToString());
            evt.intPayload1 = viewState.queryBuilderEnabled ? 1 : 0;
            SearchAnalytics.SendEvent(evt);

            RefreshBuilder();
        }

        private void RefreshBuilder()
        {
            m_QueryBuilder = viewState.queryBuilderEnabled ? CreateBuilder(context, m_SearchField) : null;
            SelectSearch();
            SetTextEditorState(m_QueryBuilder?.wordText ?? context.searchText, te => UpdateFocusState(te), m_QueryBuilder != null);
            ClearQueryHelper();
        }

        private void ClearQueryHelper()
        {
            if (m_QueryHelper != null)
            {
                m_QueryHelper.queryExecuted -= OnQueryHelperExecute;
                m_QueryHelper = null;
            }
        }


        private void ToggleShowStatusBar()
        {
            SearchSettings.showStatusBar = !SearchSettings.showStatusBar;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchSettings.showStatusBar), SearchSettings.showStatusBar.ToString());
        }

        private void ToggleKeepOpen()
        {
            SearchSettings.keepOpen = !SearchSettings.keepOpen;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchSettings.keepOpen), SearchSettings.keepOpen.ToString());
        }

        internal void TogglePackages()
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

            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchFlags.Packages), context.wantsMore.ToString());
            Refresh(RefreshFlags.StructureChanged);
        }

        private void ToggleWantsMore()
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
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(context.wantsMore), context.wantsMore.ToString());
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

        private void SendSearchEvent(SearchItem item, SearchAction action = null)
        {
            if (searchEventStatus == SearchEventStatus.DoNotSendEvent)
                return;

            var evt = new SearchAnalytics.SearchEvent();
            if (item != null)
                evt.Success(item, action);

            if (evt.success)
            {
                evt.Done();
                searchEventStatus = SearchEventStatus.EventSent;
            }
            evt.searchText = context.searchText;

            evt.useQueryBuilder = m_QueryBuilder != null;
            SearchAnalytics.SendSearchEvent(evt, context);
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

                if (m_FilteredItems.Count == 0)
                    titleContent.text = L10n.Tr("Search");
                else
                    titleContent.text = $"Search ({m_FilteredItems.Count})";
            }

            if (Utils.isDeveloperBuild)
            {
                if (Utils.IsRunningTests())
                    titleContent.text = $"[TEST] {titleContent.text}";
            }
        }

        private static string FormatStatusMessage(SearchContext context, int totalCount)
        {
            var providers = context.providers.ToList();
            if (providers.Count == 0)
                return L10n.Tr("There is no activated search provider");

            var msg = "Searching ";
            if (providers.Count > 1)
                msg += Utils.FormatProviderList(providers.Where(p => !p.isExplicitProvider), showFetchTime: !context.searchInProgress);
            else
                msg += Utils.FormatProviderList(providers);

            if (totalCount > 0)
            {
                msg += $" and found <b>{totalCount}</b> result";
                if (totalCount > 1)
                    msg += "s";
                if (!context.searchInProgress)
                {
                    if (context.searchElapsedTime > 1.0)
                        msg += $" in {PrintTime(context.searchElapsedTime)}";
                }
                else
                    msg += " so far";
            }
            else if (!string.IsNullOrEmpty(context.searchQuery))
            {
                if (!context.searchInProgress)
                    msg += " and found nothing";
            }

            if (context.searchInProgress)
                msg += SearchUtils.k_Dots[(int)EditorApplication.timeSinceStartup % SearchUtils.k_Dots.Length];

            return msg;
        }

        private static string PrintTime(double timeMs)
        {
            if (timeMs >= 1000)
                return $"{Math.Round(timeMs / 1000.0)} seconds";
            return $"{Math.Round(timeMs)} ms";
        }

        private IEnumerable<SearchQueryError> GetAllVisibleErrors()
        {
            var visibleProviders = m_FilteredItems.EnumerateGroups().Select(g => g.id).ToArray();
            var defaultProvider = SearchService.GetDefaultProvider();
            return context.GetAllErrors().Where(e => visibleProviders.Contains(e.provider.type) || e.provider.type == defaultProvider.type);
        }

        private void DrawStatusBar()
        {
            using (new GUILayout.HorizontalScope(Styles.statusBarBackground))
            {
                var hasProgress = context.searchInProgress;
                var ignoreErrors = m_FilteredItems.Count > 0 || hasProgress;
                var currentGroup = m_FilteredItems.currentGroup;
                var alwaysPrintError = currentGroup == null ||
                    !string.IsNullOrEmpty(context.filterId) ||
                    (m_FilteredItems.TotalCount == 0 && string.Equals(GroupedSearchList.allGroupId, currentGroup, StringComparison.Ordinal));
                if (!ignoreErrors && GetAllVisibleErrors().FirstOrDefault(e => alwaysPrintError || e.provider.type == m_FilteredItems.currentGroup) is SearchQueryError err)
                {
                    var errStyle = err.type == SearchQueryErrorType.Error ? Styles.statusError : Styles.statusWarning;
                    var firstLineReason = err.reason;
                    var firstLineIndex = err.reason.IndexOf("\n");
                    if (firstLineIndex >= 0)
                        firstLineReason = err.reason.Substring(0, firstLineIndex);
                    EditorGUILayout.LabelField(Utils.GUIContentTemp(Utils.TrimText(firstLineReason), $"({err.provider.name}) {err.reason}"), errStyle, GUILayout.ExpandWidth(true));
                    var labelRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                    {
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, err.reason);
                    }
                }
                else if (SearchSettings.showStatusBar && position.width >= 340f)
                {
                    var status = FormatStatusMessage(context, m_FilteredItems?.TotalCount ?? 0);
                    EditorGUILayout.LabelField(Utils.GUIContentTemp(status, status + '\n'), Styles.statusLabel, GUILayout.ExpandWidth(true));
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }

                EditorGUI.BeginChangeCheck();
                var newItemIconSize = itemIconSize;
                if (itemIconSize <= (int)DisplayMode.Limit && position.width >= 140f)
                {
                    var sliderRect = EditorGUILayout.GetControlRect(false, Styles.statusBarBackground.fixedHeight, GUILayout.Width(55f));
                    sliderRect.y -= 1f;
                    newItemIconSize = GUI.HorizontalSlider(sliderRect, newItemIconSize, 0f, (float)DisplayMode.Limit);
                }

                var isList = displayMode == DisplayMode.List;
                if (GUILayout.Toggle(isList, Styles.listModeContent, Styles.statusBarButton) != isList)
                {
                    newItemIconSize = (float)DisplayMode.List;
                    SendEvent(SearchAnalytics.GenericEventType.QuickSearchSizeRadioButton, DisplayMode.List.ToString());
                }
                var isGrid = displayMode == DisplayMode.Grid;
                if (GUILayout.Toggle(isGrid, Styles.gridModeContent, Styles.statusBarButton) != isGrid)
                {
                    newItemIconSize = (float)DisplayMode.Grid;
                    SendEvent(SearchAnalytics.GenericEventType.QuickSearchSizeRadioButton, DisplayMode.Grid.ToString());
                }
                var isTable = displayMode == DisplayMode.Table;
                if (GUILayout.Toggle(isTable, Styles.tableModeContent, Styles.statusBarButton) != isTable)
                {
                    newItemIconSize = (float)DisplayMode.Table;
                    SendEvent(SearchAnalytics.GenericEventType.QuickSearchSizeRadioButton, DisplayMode.Table.ToString());
                }

                if (EditorGUI.EndChangeCheck())
                {
                    newItemIconSize = Mathf.Round(newItemIconSize);
                    itemIconSize = newItemIconSize;
                    if (!m_ViewState.forceViewMode)
                        SearchSettings.itemIconSize = newItemIconSize;
                    m_ResultView.focusSelectedItem = true;
                }

                if (hasProgress)
                {
                    var searchInProgressRect = EditorGUILayout.GetControlRect(false,
                        Styles.searchInProgressButton.fixedHeight, Styles.searchInProgressButton, Styles.searchInProgressLayoutOptions);

                    int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 5, 11.99f);
                    if (IsPicker())
                    {
                        GUI.Label(searchInProgressRect, Styles.statusWheel[frame], Styles.searchInProgressButton);
                    }
                    else
                    {
                        if (GUI.Button(searchInProgressRect, Styles.statusWheel[frame], Styles.searchInProgressButton))
                        {
                            OpenPreferences();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                else if (!IsPicker())
                {
                    if (GUILayout.Button(Styles.prefButtonContent, Styles.statusBarPrefsButton))
                    {
                        OpenPreferences();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void OpenPreferences()
        {
            SettingsService.OpenUserPreferences(SearchSettings.settingsPreferencesKey);
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchOpenPreferences);
        }

        private bool IsItemValid(int index)
        {
            if (index < 0 || index >= m_FilteredItems.Count)
                return false;
            return true;
        }

        private void DelayTrackSelection()
        {
            if (m_FilteredItems.Count == 0)
                return;

            if (!IsItemValid(m_DelayedCurrentSelection))
                return;

            var selectedItem = m_FilteredItems[m_DelayedCurrentSelection];
            if (trackingCallback == null)
                selectedItem?.provider?.trackSelection?.Invoke(selectedItem, context);
            else
                trackingCallback(selectedItem);

            m_DelayedCurrentSelection = k_ResetSelectionIndex;
        }

        internal void ForceTrackSelection()
        {
            DelayTrackSelection();
        }

        private void TrackSelection(int currentSelection)
        {
            if (!SearchSettings.trackSelection)
                return;

            m_DelayedCurrentSelection = currentSelection;
            EditorApplication.delayCall -= DelayTrackSelection;
            EditorApplication.delayCall += DelayTrackSelection;
        }

        private void UpdateFocusControlState(Event evt)
        {
            if (evt.type != EventType.Repaint)
                return;

            if (m_SearchBoxFocus)
            {
                m_SearchField.Focus();
                var te = m_SearchField.GetTextEditor();
                te.text = context.searchText;
                UpdateFocusState(te);
                m_SearchBoxFocus = false;
            }
        }

        protected virtual void UpdateFocusState(TextEditor te)
        {
            te.SelectAll();
        }

        private bool HandleDefaultPressEnter(Event evt)
        {
            if (evt.type != EventType.KeyDown)
                return false;

            if (evt.modifiers > 0)
                return false;

            if (AutoComplete.enabled)
                return false;

            if (GUIUtility.textFieldInput)
                return false;

            if (m_Selection.Count != 0 || results.Count == 0)
                return false;

            if (evt.keyCode != KeyCode.KeypadEnter && evt.keyCode != KeyCode.Return)
                return false;

            SetSelection(0);
            evt.Use();
            GUIUtility.ExitGUI();
            return true;
        }

        private void HandleKeyboardNavigation(Event evt)
        {
            if (!evt.isKey)
                return;

            // Ignore tabbing and line return in quicksearch
            if (evt.keyCode == KeyCode.None && (evt.character == '\t' || (int)evt.character == 10))
                evt.Use();

            if (m_QueryTreeView.isRenaming)
                return;

            if (AutoComplete.HandleKeyEvent(evt))
                return;

            if (m_SearchField != null && m_UndoManager.HandleEvent(evt, out var undoText, out var cursorPos, out _))
            {
                UpdateSearchText(undoText, cursorPos);
                evt.Use();
                return;
            }

            if (m_QueryBuilder?.HandleKeyEvent(evt) ?? false)
                return;

            if (HandleDefaultPressEnter(evt))
                return;

            if (m_SearchField?.HandleKeyEvent(evt) ?? false)
                return;

            if (evt.type == EventType.KeyDown
                && !GUIUtility.textFieldInput
            )
            {
                var ctrl = evt.control || evt.command;
                if (evt.keyCode == KeyCode.Escape)
                {
                    HandleEscapeKeyDown(evt);
                }
                else if (evt.keyCode == KeyCode.F5)
                {
                    Refresh();
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F1)
                {
                    ToggleQueryBuilder();
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F4 && m_ViewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
                {
                    TogglePanelView(SearchViewFlags.OpenInspectorPreview);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F3 && IsSavedSearchQueryEnabled())
                {
                    TogglePanelView(SearchViewFlags.OpenLeftSidePanel);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F10)
                {
                    ToggleWantsMore();
                    evt.Use();
                }
                else if (!viewState.hideTabs && evt.modifiers.HasAny(EventModifiers.Alt) && evt.keyCode == KeyCode.LeftArrow)
                {
                    string previousGroupId = null;
                    foreach (var group in EnumerateGroups())
                    {
                        if (previousGroupId != null && group.id == m_FilteredItems.currentGroup)
                        {
                            SelectGroup(previousGroupId);
                            break;
                        }
                        previousGroupId = group.id;
                    }
                    evt.Use();
                }
                else if (!viewState.hideTabs && evt.modifiers.HasAny(EventModifiers.Alt) && evt.keyCode == KeyCode.RightArrow)
                {
                    bool selectNext = false;
                    foreach (var group in EnumerateGroups())
                    {
                        if (selectNext)
                        {
                            SelectGroup(group.id);
                            break;
                        }
                        else if (group.id == m_FilteredItems.currentGroup)
                            selectNext = true;
                    }
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Tab && evt.modifiers == EventModifiers.None)
                {
                    if (AutoComplete.Show(context, position, m_SearchField))
                        evt.Use();
                }
            }

            if (evt.type != EventType.Used && m_ResultView != null)
                m_ResultView.HandleInputEvent(evt, m_Selection);

            if (m_FilteredItems.Count == 0 && !IsEditingTextField())
                m_SearchField.Focus();
        }

        protected virtual void HandleEscapeKeyDown(Event evt)
        {
            if (!docked)
            {
                SendEvent(SearchAnalytics.GenericEventType.QuickSearchDismissEsc);
                selectCallback?.Invoke(null, true);
                selectCallback = null;
                evt.Use();
                CloseSearchWindow();
            }
            else
            {
                ClearSearch();
                evt.Use();
            }
        }

        private void UpdateSearchText(string undoText, int cursorPos)
        {
            if (m_QueryBuilder != null)
            {
                context.searchText = undoText;
                RefreshBuilder();
                RefreshSearch();
            }
            else
            {
                SetSearchText(undoText, TextCursorPlacement.Default, cursorPos);
            }
        }

        private bool IsEditingTextField()
        {
            return m_QueryTreeView.isRenaming || Utils.IsEditingTextField();
        }

        public void SelectSearch()
        {
            m_SearchBoxFocus = true;
            Repaint();
        }

        public void FocusSearch()
        {
            m_SearchField?.Focus();
        }

        protected void CloseSearchWindow()
        {
            if (s_FocusedWindow)
                s_FocusedWindow.Focus();
            Close();
        }

        private void DrawHelpText(float availableSpace)
        {
            if (string.IsNullOrEmpty(context.searchText.Trim()))
            {
                DrawQueryHelper(availableSpace);
            }
            else
            {
                GUILayout.Box(GetNoResultsHelpString(), Styles.noResult, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }
        }

        private void CreateQueryHelper()
        {
            m_QueryHelper = new QueryHelperWidget(viewState.queryBuilderEnabled, this)
            {
                drawBorder = false
            };
            m_QueryHelper.queryExecuted += OnQueryHelperExecute;
        }

        void OnQueryHelperExecute(ISearchQuery query)
        {
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchHelperWidgetExecuted, viewState.queryBuilderEnabled ? "queryBuilder" : "text");
            ClearQueryHelper();
        }

        private void DrawQueryHelper(float availableSpace)
        {
            var offset = viewState.hideTabs ? 0 : GUILayoutUtility.GetLastRect().yMax;
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            var resultViewSize = 0f;
            if (m_ShowSideBar)
                resultViewSize = m_SideBarSplitter.width + 1f;
            m_QueryHelperRect = new Rect(resultViewSize, 0, availableSpace, position.height - offset - 21);
            queryHelper.Draw(Event.current, m_QueryHelperRect);
            GUILayout.EndVertical();
        }

        private string GetNoResultsHelpString()
        {
            var provider = SearchService.GetProvider(m_FilteredItems.currentGroup);
            if (m_FilteredItems.TotalCount == 0 || provider == null)
                return $"No results found for <b>{context.searchQuery}</b>\nTry something else?";

            return $"There is no result in {provider.name}\nSelect another search tab?";
        }

        private void DrawItems(Event evt, float availableSpace)
        {
            using (new EditorGUILayout.VerticalScope(Styles.panelBackground, GUILayout.Width(availableSpace)))
            {
                if (!viewState.hideTabs)
                    DrawTabs(evt, availableSpace);
                if (m_ResultView != null && (!m_ResultView.showNoResultMessage || m_FilteredItems.Count > 0))
                {
                    var rect = m_ResultView.Draw(m_Selection, availableSpace);
                    if (viewState.hideTabs && evt.type == EventType.Repaint)
                    {
                        Styles.topBorder.Draw(rect, GUIContent.none, 0);
                    }
                    ClearQueryHelper();
                }
                else
                    DrawHelpText(availableSpace);
            }
        }

        static class ComputedValues
        {
            public static float tabButtonsWidth { get; private set; }
            static ComputedValues()
            {
                tabButtonsWidth = Styles.tabMoreButton.CalcSize(Styles.moreProviderFiltersContent).x
                    + Styles.tabButton.CalcSize(Styles.syncSearchButtonContent).x
                    + Styles.tabButton.margin.horizontal
                    + Styles.tabMoreButton.margin.horizontal;
            }
        }

        public IEnumerable<IGroup> EnumerateGroups()
        {
            var groups = m_FilteredItems.EnumerateGroups(!viewState.hideAllGroup);
            if (!viewState.hideAllGroup)
                groups = groups.Where(g => !string.Equals(g.id, "default", StringComparison.Ordinal));
            return groups;
        }

        private void DrawTabs(Event evt, float availableSpace)
        {
            const float tabMarginLeft = 3f;
            const float tabMarginRight = 3f;
            var maxBarWidth = availableSpace - ComputedValues.tabButtonsWidth;
            var moreTabButtonSize = Styles.searchTabMoreButton.CalcSize(GUIContent.none);
            var realTabMarginRight = Mathf.Max(tabMarginRight, Styles.searchTabMoreButton.margin.right);
            var tabExtraSpace = tabMarginLeft + moreTabButtonSize.x + Styles.searchTabMoreButton.margin.left + realTabMarginRight;
            using (new EditorGUILayout.HorizontalScope(Styles.searchTabBackground, GUILayout.MaxWidth(availableSpace)))
            {
                var maxTabWidth = 100f;
                Dictionary<string, GUIContent> groupsContent = new Dictionary<string, GUIContent>();
                var allGroups = EnumerateGroups().ToList();
                var currentGroupIndex = -1;
                var lastSelectedMoreGroupIndex = -1;

                var isEmptyQuery = string.IsNullOrEmpty(context.searchText.Trim());
                for (var i = 0; i < allGroups.Count; ++i)
                {
                    var group = allGroups[i];
                    GUIContent content;
                    if (isEmptyQuery)
                    {
                        content = new GUIContent($"{group.name}");
                    }
                    else
                    {
                        var formattedCount = Utils.FormatCount((ulong)group.count);
                        content = new GUIContent($"{group.name} {string.Format(Styles.tabCountTextColorFormat, formattedCount)}");
                    }

                    if (!groupsContent.TryAdd(group.name, content))
                        continue;
                    maxTabWidth = Mathf.Max(Styles.searchTab.CalcSize(content).x + tabExtraSpace, maxTabWidth);
                    if (group.id == m_FilteredItems.currentGroup)
                        currentGroupIndex = i;
                    if (group.id == m_LastSelectedMoreGroup)
                        lastSelectedMoreGroupIndex = i;
                }

                // Get the maximum visible group visible
                var visibleGroupCount = Math.Min(Math.Max(Mathf.FloorToInt(maxBarWidth / maxTabWidth), k_MinimumGroupVisible), allGroups.Count);
                var needTabDropdown = visibleGroupCount < allGroups.Count;
                var availableRect = GUILayoutUtility.GetRect(
                    maxTabWidth * visibleGroupCount,
                    Styles.searchTab.fixedHeight,
                    Styles.searchTab,
                    GUILayout.MaxWidth(maxBarWidth));

                var visibleGroups = new List<IGroup>(visibleGroupCount);
                var hiddenGroups = new List<IGroup>(Math.Max(allGroups.Count - visibleGroupCount, 0));
                GetVisibleAndHiddenGroups(allGroups, visibleGroups, hiddenGroups, visibleGroupCount, currentGroupIndex, lastSelectedMoreGroupIndex);

                var groupIndex = 0;
                var groupStartPosition = availableRect.x;
                foreach (var group in visibleGroups)
                {
                    var oldColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, group.count == 0 ? 0.5f : 1f);
                    var isCurrentGroup = m_FilteredItems.currentGroup == group.id;
                    var content = groupsContent[group.name];
                    var tabRect = new Rect(availableRect);
                    tabRect.x = groupStartPosition + groupIndex * maxTabWidth;
                    tabRect.width = maxTabWidth;
                    var moreTabButtonRect = new Rect(tabRect);
                    moreTabButtonRect.x = tabRect.xMax - moreTabButtonSize.x - realTabMarginRight;
                    moreTabButtonRect.width = moreTabButtonSize.x;
                    var hovered = tabRect.Contains(evt.mousePosition);
                    var hoveredMoreTab = moreTabButtonRect.Contains(evt.mousePosition);
                    var showMoreTabButton = needTabDropdown && groupIndex == visibleGroupCount - 1;
                    if (evt.type == EventType.Repaint)
                    {
                        Styles.searchTab.Draw(tabRect, content, hovered, isCurrentGroup, false, false);

                        if (showMoreTabButton)
                        {
                            Styles.searchTabMoreButton.Draw(moreTabButtonRect, hoveredMoreTab, isCurrentGroup, true, false);
                        }
                    }
                    else if (evt.type == EventType.MouseUp && evt.button == 1 && tabRect.Contains(evt.mousePosition))
                    {
                        ShowFilters();
                        evt.Use();
                    }
                    else if (evt.type == EventType.MouseUp && hovered)
                    {
                        if (showMoreTabButton && hoveredMoreTab)
                            ShowHiddenGroups(hiddenGroups);
                        else
                            SelectGroup(group.id);
                        evt.Use();
                    }
                    GUI.color = oldColor;

                    ++groupIndex;
                }

                GUILayout.FlexibleSpace();
                DrawTabsButton();
            }
        }

        protected virtual void DrawTabsButton()
        {
            m_ResultView?.DrawTabsButtons();
            DrawSyncSearchButton();
            DrawTabsMoreOptions();
        }

        protected virtual void DrawTabsMoreOptions()
        {
            if (EditorGUILayout.DropdownButton(Styles.moreProviderFiltersContent, FocusType.Keyboard, Styles.tabMoreButton))
                ShowFilters();
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
        }

        private void ShowFilters()
        {
            var filterMenu = new GenericMenu();

            AddItemsToMenu(filterMenu, "");

            if (!IsPicker())
            {
                var dbs = SearchDatabase.EnumerateAll().ToList();
                if (dbs.Count > 1)
                {
                    foreach (var db in dbs)
                        filterMenu.AddItem(new GUIContent($"Indexes/{db.name} ({db.settings.type} index)"), !db.settings.options.disabled, () => ToggleIndexEnabled(db));
                }
            }

            filterMenu.AddSeparator("");
            filterMenu.AddDisabledItem(new GUIContent("Search Providers"));
            filterMenu.AddSeparator("");
            AddProvidersToMenu(filterMenu);
            filterMenu.ShowAsContext();
        }

        private void AddProvidersToMenu(GenericMenu menu)
        {
            var allEnabledProviders = m_AvailableProviders.Where(p => context.IsEnabled(p.id));
            var singleProviderEnabled = allEnabledProviders.Count() == 1 ? allEnabledProviders.First() : null;
            foreach (var p in m_AvailableProviders)
            {
                var filterContent = new GUIContent($"{p.name} ({p.filterId})");
                if (singleProviderEnabled == p)
                {
                    menu.AddDisabledItem(filterContent, context.IsEnabled(p.id));
                }
                else
                {
                    menu.AddItem(filterContent, context.IsEnabled(p.id), () => ToggleFilter(p.id));
                }
            }
        }

        private void ToggleIndexEnabled(SearchDatabase db)
        {
            db.settings.options.disabled = !db.settings.options.disabled;
            Refresh(RefreshFlags.GroupChanged);
        }

        private void ShowHiddenGroups(IEnumerable<IGroup> hiddenGroups)
        {
            var groupMenu = new GenericMenu();
            foreach (var g in hiddenGroups)
            {
                var groupContent = new GUIContent($"{g.name} ({g.count})");
                groupMenu.AddItem(groupContent, false, () => SetLastTabGroup(g.id));
            }
            groupMenu.ShowAsContext();
        }

        private void SetLastTabGroup(string groupId)
        {
            if (m_FilteredItems.currentGroup != groupId)
                SelectGroup(groupId);
            m_LastSelectedMoreGroup = groupId;
        }

        private void GetVisibleAndHiddenGroups(List<IGroup> allGroups, List<IGroup> visibleGroups, List<IGroup> hiddenGroups, int visibleGroupCount, int currentGroupIndex, int lastSelectedMoreGroupIndex)
        {
            var nonEssentialGroupCount = Math.Max(visibleGroupCount - k_MinimumGroupVisible, 0);
            if (allGroups.Count > 0)
            {
                var currentIndex = 0;
                for (; currentIndex < nonEssentialGroupCount && currentIndex < allGroups.Count; ++currentIndex)
                {
                    visibleGroups.Add(allGroups[currentIndex]);
                }

                // For the last group, we have to determine which we should show
                if (currentGroupIndex != -1 && currentGroupIndex >= currentIndex)
                {
                    m_LastSelectedMoreGroup = m_FilteredItems.currentGroup;
                    lastSelectedMoreGroupIndex = currentGroupIndex;
                }
                var lastGroupIndex = lastSelectedMoreGroupIndex < currentIndex ? currentIndex : lastSelectedMoreGroupIndex;

                for (var i = currentIndex; i < allGroups.Count; ++i)
                {
                    if (i == lastGroupIndex)
                        visibleGroups.Add(allGroups[i]);
                    else
                        hiddenGroups.Add(allGroups[i]);
                }
            }
        }

        internal void SelectGroup(string groupId)
        {
            if (m_FilteredItems.currentGroup == groupId)
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
            evt.stringPayload1 = m_FilteredItems.currentGroup;
            evt.intPayload1 = m_FilteredItems.GetGroupById(groupId)?.count ?? 0;
            SearchAnalytics.SendEvent(evt);

            var selectedItems = m_Selection.Count > 0 ? selection.ToArray() : Array.Empty<SearchItem>();
            var newSelectedIndices = new int[selectedItems.Length];

            var oldGroupId = m_FilteredItems.currentGroup;
            m_FilteredItems.currentGroup = groupId;
            m_ViewState.group = groupId;
            viewState.groupChanged?.Invoke(context, groupId, oldGroupId);
            m_ResultView?.OnGroupChanged(oldGroupId, groupId);

            if (syncSearch && groupId != null)
                NotifySyncSearch(m_FilteredItems.currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.SyncSearch);

            for (var i = 0; i < selectedItems.Length; ++i)
            {
                var selectedItem = selectedItems[i];
                newSelectedIndices[i] = m_FilteredItems.IndexOf(selectedItem);
            }
            SetSelection(trackSelection: true, newSelectedIndices, forceChange: true);

            RefreshViews(RefreshFlags.GroupChanged);
        }

        private void OnWindowResized(Vector2 oldSize, Vector2 newSize)
        {
            m_SideBarSplitter.Resize(oldSize, newSize);
            m_DetailsPanelSplitter.Resize(oldSize, newSize);
        }

        private void DrawToolbar(Event evt)
        {
            if (context == null)
                return;

            var toolbarRect = new Rect(0, 0, position.width, 0f);
            var buttonStyle = Styles.toolbarButton;
            var buttonRect = new Rect(toolbarRect.x, toolbarRect.y + buttonStyle.margin.top + 2f, 0f, buttonStyle.fixedHeight);

            // Draw left side buttons
            if (IsSavedSearchQueryEnabled())
            {
                buttonRect.x += buttonStyle.margin.left;
                buttonRect.width = buttonStyle.fixedWidth;

                EditorGUI.BeginChangeCheck();
                GUI.Toggle(buttonRect, m_ViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel), Styles.openSaveSearchesIconContent, Styles.openSearchesPanelButton);
                if (EditorGUI.EndChangeCheck())
                    TogglePanelView(SearchViewFlags.OpenLeftSidePanel);
            }

            // Draw text/block toggle
            if (viewState.hasQueryBuilderToggle)
            {
                buttonRect.x += buttonRect.width + 4f;
                buttonRect.width = buttonStyle.fixedWidth;

                EditorGUI.BeginChangeCheck();
                GUI.Toggle(buttonRect, viewState.queryBuilderEnabled, Styles.queryBuilderIconContent, Styles.openSearchesPanelButton);
                if (EditorGUI.EndChangeCheck())
                    ToggleQueryBuilder();
            }

            var searchTextRect = new Rect(
                buttonRect.xMax + Styles.searchField.margin.left,
                toolbarRect.y + Styles.searchField.margin.top,
                position.width - buttonRect.xMax - Styles.searchField.margin.horizontal,
                UI.SearchField.searchFieldSingleLineHeight);

            // Draw right side buttons (rendered right to left)
            buttonRect = new Rect(toolbarRect.xMax, buttonRect.y - 1f, buttonStyle.fixedWidth, buttonStyle.fixedHeight);
            if (position.width > k_DetailsViewShowMinSize && m_ViewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
            {
                buttonRect.x -= buttonStyle.margin.right + buttonStyle.fixedWidth;
                EditorGUI.BeginChangeCheck();
                GUI.Toggle(buttonRect, m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview), Styles.previewInspectorButtonContent, Styles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                    TogglePanelView(SearchViewFlags.OpenInspectorPreview);

                searchTextRect.xMax = buttonRect.xMin - Styles.searchField.margin.right;
            }

            if (IsSavedSearchQueryEnabled())
            {
                buttonRect = DrawSaveQueryDropdown(buttonRect, Styles.toolbarDropdownButton);
                searchTextRect.xMax = buttonRect.xMin - Styles.searchField.margin.right;
            }

            DrawSearchField(evt, toolbarRect, searchTextRect);
        }

        private Rect DrawSaveQueryDropdown(Rect buttonRect, in GUIStyle buttonStyle)
        {
            buttonRect.x -= buttonStyle.margin.right + buttonStyle.fixedWidth;

            EditorGUI.BeginDisabledGroup(!CanSaveQuery());
            if (EditorGUI.DropdownButton(buttonRect, Styles.saveQueryButtonContent, FocusType.Passive, Styles.toolbarDropdownButton))
                OnSaveQuery();
            EditorGUI.EndDisabledGroup();

            return buttonRect;
        }

        private void DrawSearchField(in Event evt, in Rect toolbarRect, Rect searchTextRect)
        {
            var showClearButton = IsPicker() ? string.CompareOrdinal(context.searchText, viewState.initialQuery) != 0 : !string.IsNullOrEmpty(context.searchText);
            var searchClearButtonRect = new Rect(0, 0, 1, 1);

            if (showClearButton)
            {
                searchClearButtonRect = Styles.searchFieldBtn.margin.Remove(searchTextRect);
                searchClearButtonRect.xMin = searchClearButtonRect.xMax - UI.SearchField.cancelButtonWidth;
                searchClearButtonRect.y -= 1f;
            }

            var previousSearchText = context.searchText;
            if (showClearButton && evt.type == EventType.MouseUp && searchClearButtonRect.Contains(evt.mousePosition))
            {
                ClearSearch();
                evt.Use();
            }
            else
            {
                if (evt.type != EventType.KeyDown || evt.keyCode != KeyCode.None || evt.character != '\r')
                {
                    if (m_QueryBuilder != null && viewState.queryBuilderEnabled)
                    {
                        m_QueryBuilder.Draw(evt, searchTextRect);
                    }
                    else
                    {
                        searchTextRect = m_SearchField.AdjustRect(context.searchText, searchTextRect);

                        var newSearchText = m_SearchField.Draw(searchTextRect, context.searchText, Styles.searchField);
                        if (!string.Equals(newSearchText, previousSearchText, StringComparison.Ordinal))
                            context.searchText = newSearchText;

                        DrawTabToFilterInfo(evt, searchTextRect);

                        GUILayoutUtility.GetRect(toolbarRect.width, searchTextRect.height + Styles.searchField.margin.vertical, Styles.toolbar);
                    }
                }
            }

            if (!showClearButton)
            {
                if (m_QueryBuilder == null)
                {
                    if (context.empty)
                    {
                        var oc = GUI.color;
                        GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * 0.5f);
                        GUI.Label(searchTextRect, searchTopicPlaceHolder, Styles.placeholderTextStyle);
                        GUI.color = oc;
                    }
                }
            }
            else
            {
                GUI.SetNextControlName("QuickSearchClearButton");
                GUI.Button(searchClearButtonRect, Icons.clear, Styles.searchFieldBtn);
                EditorGUIUtility.AddCursorRect(searchClearButtonRect, MouseCursor.Arrow);
            }

            if (!string.Equals(previousSearchText, context.searchText, StringComparison.Ordinal))
            {
                SetSelection();
                ClearCurrentErrors();
                DebounceRefresh();
            }
            else if (m_QueryBuilder == null)
            {
                // Only draw errors when you are done typing, to prevent cases where
                // the cursor moved because of changes but we did not clear the errors yet.
                DrawQueryErrors();
            }
        }

        private void DrawQueryErrors()
        {
            if (context.searchInProgress)
                return;

            if (!context.options.HasAny(SearchFlags.ShowErrorsWithResults) && m_FilteredItems.Count > 0)
                return;

            List<SearchQueryError> errors;
            if (m_FilteredItems.currentGroup == (m_FilteredItems as IGroup)?.id)
                errors = GetAllVisibleErrors().ToList();
            else
                errors = context.GetErrorsByProvider(m_FilteredItems.currentGroup).ToList();

            if (errors.Count == 0 || context.markers?.Length > 0)
                return;

            var te = m_SearchField.GetTextEditor();
            errors.Sort(SearchQueryError.Compare);
            DrawQueryErrors(errors, te);
        }

        private void DrawQueryErrors(IEnumerable<SearchQueryError> errors, TextEditor te)
        {
            var alreadyShownErrors = new List<SearchQueryError>();
            foreach (var searchQueryError in errors)
            {
                var queryErrorStart = searchQueryError.index;
                var queryErrorEnd = queryErrorStart + searchQueryError.length;

                // Do not show error if the cursor is inside the error itself, or if the error intersect with
                // the current token
                if (te.cursorIndex >= queryErrorStart && te.cursorIndex <= queryErrorEnd)
                    continue;
                SearchPropositionOptions.GetTokenBoundariesAtCursorPosition(context.searchText, te.cursorIndex, out var tokenStartPos, out var tokenEndPos);
                if (queryErrorStart >= tokenStartPos && queryErrorStart <= tokenEndPos)
                    continue;
                if (queryErrorEnd >= tokenStartPos && queryErrorEnd <= tokenEndPos)
                    continue;

                // Do not stack errors on top of each other
                if (alreadyShownErrors.Any(e => e.Overlaps(searchQueryError)))
                    continue;

                alreadyShownErrors.Add(searchQueryError);

                if (searchQueryError.type == SearchQueryErrorType.Error)
                {
                    m_SearchField.DrawError(
                        queryErrorStart,
                        searchQueryError.length,
                        searchQueryError.reason);
                }
                else
                {
                    m_SearchField.DrawWarning(
                        queryErrorStart,
                        searchQueryError.length,
                        searchQueryError.reason);
                }
            }
        }

        private void DrawTabToFilterInfo(in Event evt, in Rect searchTextRect)
        {
            if (evt.type != EventType.Repaint)
                return;

            var searchTextTrimmedRect = Styles.searchFieldTabToFilterBtn.margin.Remove(searchTextRect);
            var searchTextWidth = Styles.searchField.CalcSize(Utils.GUIContentTemp(context.searchText)).x;
            var pressTabToFilterContextStart = searchTextTrimmedRect.width - Styles.pressToFilterContentWidth;
            var showPressTabToFilter = searchTextWidth < pressTabToFilterContextStart;

            // Prevent overlap with search topic place holder only when it is visible
            if (showPressTabToFilter && string.IsNullOrEmpty(context.searchText))
            {
                var searchTopicPlaceHolderWidth = Styles.placeholderTextStyle.CalcSize(Utils.GUIContentTemp(searchTopicPlaceHolder)).x;
                var searchTopicPlaceHolderEnd = searchTextRect.center.x + searchTopicPlaceHolderWidth / 2;
                if (searchTopicPlaceHolderEnd >= pressTabToFilterContextStart)
                    showPressTabToFilter = false;
            }

            if (showPressTabToFilter)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUI.Label(searchTextTrimmedRect, Styles.pressToFilterContent, Styles.searchFieldTabToFilterBtn);
                EditorGUI.EndDisabledGroup();
            }
        }

        protected void ClearSearch()
        {
            m_QueryTreeView.SetSelection(new int[0]);
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchClearSearch);
            AutoComplete.Clear();
            context.searchText = IsPicker() ? m_ViewState.initialQuery : string.Empty;
            GUI.changed = true;
            SetSelection();
            DebounceRefresh();
            SelectSearch();
            m_QueryBuilder?.Build();
            ProcessNewBuilder(m_QueryBuilder);
        }

        [CommandHandler("OpenQuickSearch")]
        internal static void OpenQuickSearchCommand(CommandExecuteContext c)
        {
            OpenDefaultQuickSearch();
        }

        protected virtual bool IsPicker()
        {
            return false;
        }

        bool ISearchView.IsPicker()
        {
            return IsPicker();
        }

        protected virtual void OnSaveQuery()
        {
            var saveQueryMenu = new GenericMenu();
            if (activeQuery != null)
            {
                saveQueryMenu.AddItem(new GUIContent($"Save {activeQuery.displayName}"), false, SaveActiveSearchQuery);
                saveQueryMenu.AddSeparator("");
            }

            AddSaveQueryMenuItems(saveQueryMenu);
            saveQueryMenu.ShowAsContext();
        }

        protected virtual void AddSaveQueryMenuItems(GenericMenu saveQueryMenu)
        {
            saveQueryMenu.AddItem(new GUIContent("Save User"), false, () => SaveUserSearchQuery());
            saveQueryMenu.AddItem(new GUIContent("Save Project..."), false, () => SaveProjectSearchQuery());
            if (!string.IsNullOrEmpty(context.searchText))
            {
                saveQueryMenu.AddSeparator("");
                saveQueryMenu.AddItem(new GUIContent("Clipboard"), false, () => SaveQueryToClipboard(context.searchText));
            }

            m_ResultView?.AddSaveQueryMenuItems(context, saveQueryMenu);
        }

        private void SaveQueryToClipboard(in string query)
        {
            var trimmedQuery = Utils.TrimText(query);
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, trimmedQuery);
            EditorGUIUtility.systemCopyBuffer = Utils.TrimText(trimmedQuery);
        }

        internal SearchViewState SaveViewState(string name)
        {
            var viewState = m_ResultView.SaveViewState(name);
            var tableConfig = viewState.tableConfig;
            viewState.Assign(m_ViewState);
            // Note: in 2022 viewState.tableConfig might be null while the table view holds a newly created version (this is fixed in 2023)
            // Reassign the tableConfig from result view:
            viewState.tableConfig = tableConfig;
            m_ViewState.group = m_FilteredItems.currentGroup;
            return viewState;
        }

        internal void SaveActiveSearchQuery()
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

        internal SearchQuery SaveUserSearchQuery()
        {
            // Note: Use SaveViewState to provide a new query with proper tableConfig
            var consolidatedQuery = SaveViewState("UserQuery");
            var query = SearchQuery.AddUserQuery(consolidatedQuery);
            AddNewQuery(query);
            return query;
        }

        internal SearchQueryAsset SaveProjectSearchQuery(string searchQueryPath = null)
        {
            var initialFolder = SearchSettings.GetFullQueryFolderPath();
            var searchQueryFileName = SearchQueryAsset.GetQueryName(context.searchQuery);
            if (searchQueryPath == null)
                searchQueryPath = EditorUtility.SaveFilePanel("Save search query...", initialFolder, searchQueryFileName, "asset");
            if (string.IsNullOrEmpty(searchQueryPath))
                return null;
            if (!SearchUtils.ValidateAssetPath(ref searchQueryPath, ".asset", out var errorMessage))
            {
                Debug.LogWarning($"Save Search Query has failed. {errorMessage}.");
                return null;
            }

            SearchSettings.queryFolder = Utils.CleanPath(Path.GetDirectoryName(searchQueryPath));
            return SaveSearchQueryFromContext(searchQueryPath, true);
        }

        private SearchQueryAsset SaveSearchQueryFromContext(string searchQueryPath, bool newQuery)
        {
            try
            {
                var searchQuery = AssetDatabase.LoadAssetAtPath<SearchQueryAsset>(searchQueryPath) ?? SearchQueryAsset.Create(context);
                if (!searchQuery)
                {
                    Debug.LogError($"Failed to save search query at {searchQueryPath}");
                    return null;
                }

                var folder = Utils.CleanPath(Path.GetDirectoryName(searchQueryPath));
                var queryName = Path.GetFileNameWithoutExtension(searchQueryPath);
                searchQuery.viewState = SaveViewState(queryName);

                if (SearchQueryAsset.SaveQuery(searchQuery, context, folder, queryName) && newQuery)
                {
                    Selection.activeObject = searchQuery;
                    AddNewQuery(searchQuery);
                }
                else
                    SaveItemCountToPropertyDatabase(true);

                return searchQuery;
            }
            catch
            {
                Debug.LogError($"Failed to save search query at {searchQueryPath}");
            }
            return null;
        }

        private void AddNewQuery(ISearchQuery newQuery)
        {
            SearchSettings.AddRecentSearch(newQuery.searchText);
            SearchQueryAsset.ResetSearchQueryItems();
            m_QueryTreeView.Add(newQuery);

            if (IsSavedSearchQueryEnabled() && m_ViewState.flags.HasNone(SearchViewFlags.OpenLeftSidePanel))
                TogglePanelView(SearchViewFlags.OpenLeftSidePanel);

            SaveItemCountToPropertyDatabase(true);
        }

        private void DebounceRefresh()
        {
            if (!this)
                return;

            if (SearchSettings.debounceMs == 0)
                RefreshSearch();
            else if (m_DebounceOff == null)
                m_DebounceOff = Utils.CallDelayed(RefreshSearch, SearchSettings.debounceMs / 1000.0);
        }

        protected virtual void ComputeContextHash()
        {
            if (IsGeneralSearchWindow())
            {
                m_ContextHash = generalWindowContextHash;
            }
            else
            {
                m_ContextHash = 0;
            }
        }

        protected void UpdateViewState(SearchViewState args)
        {
            if (args.hideAllGroup && (args.group == null || string.Equals(GroupedSearchList.allGroupId, args.group, StringComparison.Ordinal)))
                args.group = args.context?.GetProviders().FirstOrDefault()?.id;

            if (context?.options.HasAny(SearchFlags.Expression) ?? false)
                args.itemSize = (int)DisplayMode.Table;
            itemIconSize = args.itemSize;


            m_FilteredItems.currentGroup = m_ViewState.group;
        }

        protected virtual void LoadSessionSettings()
        {
            string loadGroup = null;
            if (!Utils.IsRunningTests())
            {
                RestoreSearchText();

                if (m_ViewState.flags.HasNone(SearchViewFlags.OpenInspectorPreview | SearchViewFlags.OpenLeftSidePanel | SearchViewFlags.HideSearchBar))
                {
                    if (HasSessionSettings() && SearchSettings.GetScopeValue(nameof(SearchViewFlags.OpenInspectorPreview), m_ContextHash, 0) != 0)
                        m_ViewState.flags |= SearchViewFlags.OpenInspectorPreview;

                    if (SearchSettings.showSavedSearchPanel)
                        m_ViewState.flags |= SearchViewFlags.OpenLeftSidePanel;

                    if (HasSessionSettings())
                        loadGroup = SearchSettings.GetScopeValue(nameof(m_FilteredItems.currentGroup), m_ContextHash, viewState.group);
                }
            }
            else if (!string.IsNullOrEmpty(m_ViewState.searchText))
            {
                m_ViewState.flags |= SearchViewFlags.DisableQueryHelpers;
            }

            if (loadGroup == null && context.providers.Count() == 1)
            {
                loadGroup = context.providers.First().id;
            }

            m_ViewState.group = viewState.hideTabs ? null : (loadGroup ?? m_ViewState.group);
            UpdateViewState(m_ViewState);
        }

        protected virtual void RestoreSearchText()
        {
            if (!m_ViewState.ignoreSaveSearches &&
                 m_ViewState.context != null &&
                 string.IsNullOrEmpty(m_ViewState.context.searchText) &&
                 HasSessionSettings())
            {
                m_ViewState.searchText = SearchSettings.GetScopeValue(k_LastSearchPrefKey, m_ContextHash, "").TrimStart();
                if (m_ViewState.context != null)
                {
                    m_ViewState.context.searchText = m_ViewState.searchText;
                    SearchSettings.ApplyContextOptions(m_ViewState.context);
                }
            }
        }

        protected virtual void SaveSessionSettings()
        {
            if (!HasSessionSettings())
                return;

            if (!viewState.ignoreSaveSearches)
                SearchSettings.SetScopeValue(k_LastSearchPrefKey, m_ContextHash, context.searchText.TrimStart());
            SearchSettings.SetScopeValue(nameof(SearchViewFlags.OpenInspectorPreview), m_ContextHash, m_ViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview) ? 1 : 0);

            if (m_FilteredItems != null)
                SearchSettings.SetScopeValue(nameof(m_FilteredItems.currentGroup), m_ContextHash, m_FilteredItems.currentGroup);

            SearchSettings.Save();
            SaveGlobalSettings();
        }

        protected virtual void SaveGlobalSettings()
        {
            EditorPrefs.SetFloat(k_SideBarWidthKey, m_SideBarSplitter.pos);
            EditorPrefs.SetFloat(k_DetailsWidthKey, m_DetailsPanelSplitter.pos);
        }

        private void UpdateItemSize(float value)
        {
            var oldMode = displayMode;
            m_ViewState.itemSize = value > (int)DisplayMode.Table ? (int)DisplayMode.Limit : value;
            var newMode = displayMode;
            if (m_ResultView == null || oldMode != newMode)
                SetResultView(newMode);
        }

        private void SetResultView(DisplayMode mode)
        {
            if (mode == DisplayMode.List)
                m_ResultView = new ListView(this);
            else if (mode == DisplayMode.Grid)
                m_ResultView = new GridView(this);
            else if (mode == DisplayMode.Table)
                m_ResultView = new TableView(this, viewState.tableConfig);
            RefreshViews(RefreshFlags.DisplayModeChanged);
        }

        internal SearchAnalytics.GenericEvent CreateEvent(SearchAnalytics.GenericEventType category, string name = null, string message = null, string description = null)
        {
            var e = SearchAnalytics.GenericEvent.Create(windowId, category, name);
            e.message = message;
            e.description = description;
            return e;
        }

        internal void SendEvent(SearchAnalytics.GenericEventType category, string name = null, string message = null, string description = null)
        {
            SearchAnalytics.SendEvent(windowId, category, name, message, description);
        }

        [MenuItem("Edit/Search All... %k", priority = 161)]
        internal static void OpenDefaultQuickSearch()
        {
            SearchUtils.OpenDefaultQuickSearch();
        }

        [MenuItem("Window/Search/New Window", priority = 0)]
        public static void OpenNewWindow()
        {
            SearchUtils.OpenNewWindow();
        }

        [MenuItem("Window/Search/Transient Window", priority = 1)]
        public static void OpenPopupWindow()
        {
            if (SearchService.ShowWindow(defaultWidth: 600, defaultHeight: 400, dockable: false) is QuickSearch window)
                SearchAnalytics.SendEvent(window.windowId, SearchAnalytics.GenericEventType.QuickSearchOpen, "PopupWindow");
        }


        private SearchProvider GetProviderById(string providerId)
        {
            return context.providers.FirstOrDefault(p => p.active && p.id == providerId);
        }

        [Shortcut(k_TogleSyncShortcutName, typeof(QuickSearch), KeyCode.L, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        internal static void ToggleSyncSearchView(ShortcutArguments args)
        {
            var window = args.context as QuickSearch;
            if (window == null)
                return;
            window.SetSyncSearchView(!window.syncSearch);
        }

        private void SetSyncSearchView(bool sync)
        {
            var providerSupportsSync = GetProviderById(m_FilteredItems.currentGroup)?.supportsSyncViewSearch ?? false;
            var searchViewSyncEnabled = providerSupportsSync && SearchViewSyncEnabled(m_FilteredItems.currentGroup);
            var supportsSync = providerSupportsSync && searchViewSyncEnabled;
            if (!supportsSync)
                return;

            syncSearch = sync;
            if (syncSearch)
                SendEvent(SearchAnalytics.GenericEventType.QuickSearchSyncViewButton, m_FilteredItems.currentGroup);
            Refresh();
        }

        protected virtual void DrawSyncSearchButton()
        {
            var providerSupportsSync = GetProviderById(m_FilteredItems.currentGroup)?.supportsSyncViewSearch ?? false;
            var searchViewSyncEnabled = providerSupportsSync && SearchViewSyncEnabled(m_FilteredItems.currentGroup);
            var supportsSync = providerSupportsSync && searchViewSyncEnabled;
            if (!supportsSync)
                return;
            EditorGUI.BeginChangeCheck();
            var syncButtonContent = m_FilteredItems.currentGroup == GroupedSearchList.allGroupId ? Styles.syncSearchAllGroupTabContent : !providerSupportsSync ? Styles.syncSearchProviderNotSupportedContent : !searchViewSyncEnabled ? Styles.syncSearchViewNotEnabledContent : syncSearch ? Styles.syncSearchOnButtonContent : Styles.syncSearchButtonContent;
            var sync = GUILayout.Toggle(syncSearch, syncButtonContent, Styles.tabButton);
            if (EditorGUI.EndChangeCheck())
            {
                SetSyncSearchView(sync);
            }
        }

        private static bool SearchViewSyncEnabled(string groupId)
        {
            switch (groupId)
            {
                case "asset":
                    return UnityEditor.SearchService.ProjectSearch.HasEngineOverride();
                case "scene":
                    return UnityEditor.SearchService.SceneSearch.HasEngineOverride();
                default:
                    return false;
            }
        }

        [CommandHandler("OpenQuickSearchInContext")]
        static void OpenQuickSearchInContextCommand(CommandExecuteContext c)
        {
            var query = c.GetArgument<string>(0);
            var sourceContext = c.GetArgument<string>(1);
            var wasReused = HasOpenInstances<QuickSearch>();
            var ignoreRestoreContext = c.GetArgument(2, false);
            SearchUtils.OpenFromContextWindow(query, sourceContext, ignoreRestoreContext, true);
            c.result = true;
        }


        [Shortcut("Help/Search Contextual")]
        internal static void OpenContextual()
        {
            SearchUtils.OpenFromContextWindow();
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

        private void ClearCurrentErrors()
        {
            context.ClearErrors();
        }

        internal static Texture2D GetIconFromDisplayMode(DisplayMode displayMode)
        {
            switch (displayMode)
            {
                case DisplayMode.Grid:
                    return Styles.gridModeContent.image as Texture2D;
                case DisplayMode.Table:
                    return Styles.tableModeContent.image as Texture2D;
                default:
                    return Styles.listModeContent.image as Texture2D;
            }
        }

        internal static DisplayMode GetDisplayModeFromItemSize(float itemSize)
        {
            if (itemSize <= (int)DisplayMode.List)
                return DisplayMode.List;

            if (itemSize >= (int)DisplayMode.Table)
                return DisplayMode.Table;

            return DisplayMode.Grid;
        }

        [WindowAction]
        internal static WindowAction CreateSearchHelpWindowAction()
        {
            // Developer-mode render doc button to enable capturing any HostView content/panels
            var action = WindowAction.CreateWindowActionButton("HelpSearch", OpenSearchHelp, null, ContainerWindow.kButtonWidth + 1, Icons.help);
            action.validateHandler = (window, _) => window && window.GetType() == typeof(QuickSearch);
            return action;
        }

        private static void OpenSearchHelp(EditorWindow window, WindowAction action)
        {
            var windowId = (window as QuickSearch)?.windowId ?? null;
            SearchAnalytics.SendEvent(windowId, SearchAnalytics.GenericEventType.QuickSearchOpenDocLink);
            EditorUtility.OpenWithDefaultApp("https://docs.unity3d.com/Manual/search-overview.html");
        }


        private QueryBuilder CreateBuilder(SearchContext context, UI.SearchField textField)
        {
            var builder = new QueryBuilder(context, textField);
            ProcessNewBuilder(builder);
            return builder;
        }

        protected virtual void ProcessNewBuilder(QueryBuilder builder)
        {
            // Nothing to do here.
        }

    }
}
