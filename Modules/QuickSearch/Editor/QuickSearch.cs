// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Search
{
    class QuickSearch : EditorWindow, ISearchView, IDisposable, IHasCustomMenu
    {
        internal const string k_TogleSyncShortcutName = "Search/Toggle Sync Search View";

        internal enum SearchEventStatus
        {
            DoNotSendEvent,
            WaitForEvent,
            EventSent,
        }

        [Flags]
        enum ShownPanels
        {
            None = 0,
            LeftSideBar      = 1 << 0,
            InspectorPreview = 1 << 1
        }

        [Serializable]
        class SplitterInfo
        {
            public enum Side
            {
                Left,
                Right
            }

            public Side side;
            public float pos;
            public bool active;
            public float lowerLimit;
            public float upperLimit;
            public EditorWindow host;

            public float width
            {
                get
                {
                    if (side == Side.Left)
                        return Mathf.Floor(pos);
                    return Mathf.Floor(host.position.width - pos);
                }
            }

            public SplitterInfo(Side side, float lowerLimit, float upperLimit, EditorWindow host)
            {
                this.side = side;
                pos = -1;
                active = false;
                this.lowerLimit = lowerLimit;
                this.upperLimit = upperLimit;
                this.host = host;
            }

            public void Init(float initialPosition)
            {
                if (pos < 0)
                    SetPosition(initialPosition, host.position.width);
            }

            public void SetPosition(float newPos)
            {
                SetPosition(newPos, host.position.width);
            }

            private void SetPosition(float newPos, float hostWidth)
            {
                if (newPos == -1)
                    return;
                var minSize = Mathf.Max(0, hostWidth * lowerLimit);
                var maxSize = Mathf.Min(hostWidth * upperLimit, hostWidth);
                var previousPos = pos;
                pos = Mathf.Round(Mathf.Max(minSize, Mathf.Min(newPos, maxSize)));
                if (previousPos != pos)
                    host.Repaint();
            }

            public void Draw(Event evt, Rect area)
            {
                var sliderRect = new Rect(pos - 2f, area.y, 3f, area.height);
                EditorGUIUtility.AddCursorRect(sliderRect, MouseCursor.ResizeHorizontal);

                if (evt.type == EventType.MouseDown && sliderRect.Contains(evt.mousePosition))
                {
                    active = true;
                    evt.Use();
                }

                if (active)
                {
                    SetPosition(evt.mousePosition.x, host.position.width);
                    if (evt.type == EventType.MouseDrag)
                        evt.Use();
                }

                if (active && evt.type == EventType.MouseUp)
                {
                    evt.Use();
                    active = false;
                }
            }

            public void Resize(Vector2 oldSize, Vector2 newSize)
            {
                var newWidth = newSize.x;
                if (side == Side.Left)
                    SetPosition(pos, newWidth);
                else
                {
                    var widthDiff = newSize.x - oldSize.x;
                    SetPosition(pos + widthDiff, newWidth);
                }
            }
        }

        const int k_ResetSelectionIndex = -1;
        const string k_LastSearchPrefKey = "last_search";
        const float k_DetailsViewShowMinSize = 550f;
        const int k_MinimumGroupVisible = 1;
        private static readonly string k_CheckWindowKeyName = $"{typeof(QuickSearch).FullName}h";
        private static readonly string[] k_Dots = { ".", "..", "..." };

        private static EditorWindow s_FocusedWindow;
        private static SearchContext s_GlobalContext = null;

        // Selection state
        private GroupedSearchList m_FilteredItems;
        private readonly List<int> m_Selection = new List<int>();
        private int m_DelayedCurrentSelection = k_ResetSelectionIndex;
        private SearchSelection m_SearchItemSelection;
        private string m_LastSelectedMoreGroup;

        private bool m_Disposed = false;
        private DetailView m_DetailView;
        private IResultView m_ResultView;
        private float m_PreviousItemSize = -1;
        private SearchQuery m_CurrentSearchQuery;
        private RefreshFlags m_DebounceRefreshFlags;
        internal double m_DebounceTime = 0.0;

        [SerializeField] private EditorWindow m_LastFocusedWindow;
        [SerializeField] private bool m_SearchBoxFocus;
        [SerializeField] private float m_ItemSize = 1;
        [SerializeField] private Vector3 m_WindowSize;
        [SerializeField] private SplitterInfo m_SideBarSplitter;
        [SerializeField] private SplitterInfo m_DetailsPanelSplitter;
        [SerializeField] private string[] m_ProviderIds;
        [SerializeField] private string m_WindowId;
        [SerializeField] private ShownPanels m_ShownPanels = ShownPanels.InspectorPreview;
        [SerializeField] private Vector2 m_SideBarScrollPosition;
        [SerializeField] private int m_ContextHash;
        [SerializeField] internal SearchEventStatus searchEventStatus;
        [SerializeField] internal bool saveFilters;
        [SerializeField] internal bool testWindow;
        [SerializeField] private string searchTopic;
        [SerializeField] private bool m_Multiselect;

        internal event Action nextFrame;
        internal event Action<Vector2, Vector2> resized;

        public Action<SearchItem, bool> selectCallback { get; set; }
        public Func<SearchItem, bool> filterCallback { get; set; }
        public Action<SearchItem> trackingCallback { get; set; }

        public SearchSelection selection
        {
            get
            {
                if (m_SearchItemSelection == null)
                    m_SearchItemSelection = new SearchSelection(m_Selection, m_FilteredItems);
                return m_SearchItemSelection;
            }
        }

        public SearchContext context { get; private set; }
        public ISearchList results => m_FilteredItems;
        public DisplayMode displayMode
        {
            get
            {
                if (m_ItemSize <= (int)DisplayMode.List)
                    return DisplayMode.List;

                return DisplayMode.Grid;
            }
        }
        public float itemIconSize { get => m_ItemSize; set => UpdateItemSize(value); }
        public bool multiselect { get => m_Multiselect; set => m_Multiselect = value; }

        private bool m_SyncSearch;
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

        internal string windowId => m_WindowId;

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.Default)
        {
            context.searchText = searchText ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(context.searchText))
                SearchField.UpdateLastSearchText(context.searchText);
            m_CurrentSearchQuery = null;
            DebouncedRefresh();
            nextFrame += () =>
            {
                var te = SearchField.GetTextEditor();
                te.text = searchText;
                SearchField.MoveCursor(moveCursor);
            };
        }

        public void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            m_DebounceRefreshFlags |= flags;
            DebouncedRefresh();
        }

        private void RefreshSearch()
        {
            EditorApplication.update -= DebouncedRefresh;

            if (context == null)
                return;
            SearchSettings.ApplyContextOptions(context);

            ClearCurrentErrors();
            var foundItems = SearchService.GetItems(context);
            if (selectCallback != null)
                foundItems.Add(SearchItem.none);

            SetItems(filterCallback == null ? foundItems : foundItems.Where(item => filterCallback(item)));

            if (syncSearch)
                NotifySyncSearch(m_FilteredItems.currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.SyncSearch);

            Utils.tick += UpdateAsyncResults;
        }

        public static QuickSearch Create(SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create(null, topic: "Unity", flags);
        }

        public static QuickSearch Create(SearchContext context, string topic = "Unity", SearchFlags flags = SearchFlags.OpenDefault)
        {
            s_GlobalContext = context ?? new SearchContext(SearchService.Providers.Where(p => p.active));
            s_FocusedWindow = focusedWindow;

            if (s_GlobalContext != null)
                s_GlobalContext.options |= flags;

            QuickSearch qsWindow;
            if (flags.HasFlag(SearchFlags.ReuseExistingWindow) && HasOpenInstances<QuickSearch>())
            {
                qsWindow = GetWindow<QuickSearch>(false, null, false);
                if (context != null)
                {
                    qsWindow.SetContext(context);
                    qsWindow.RefreshSearch();
                }
            }
            else
            {
                qsWindow = CreateInstance<QuickSearch>();
                qsWindow.m_WindowId = GUID.Generate().ToString();
            }

            qsWindow.multiselect = flags.HasFlag(SearchFlags.Multiselect);
            qsWindow.saveFilters = flags.HasFlag(SearchFlags.SaveFilters);
            qsWindow.searchTopic = topic;

            // Ensure we won't send events while doing a domain reload.
            qsWindow.searchEventStatus = SearchEventStatus.WaitForEvent;
            return qsWindow;
        }

        private void SetContext(SearchContext newContext)
        {
            if (newContext == null || newContext == context)
                return;
            context?.Dispose();
            context = newContext;

            context.searchView = this;
            context.focusedWindow = m_LastFocusedWindow;
            context.asyncItemReceived -= OnAsyncItemsReceived;
            context.asyncItemReceived += OnAsyncItemsReceived;

            m_ProviderIds = context.GetProviders().Select(p => p.id).ToArray();

            m_FilteredItems?.Dispose();
            m_FilteredItems = new GroupedSearchList(context);

            LoadContext();
        }

        public static QuickSearch Open(float defaultWidth = 950, float defaultHeight = 539, SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create(flags).ShowWindow(defaultWidth, defaultHeight, flags);
        }

        public static QuickSearch OpenWithContextualProvider(params string[] providerIds)
        {
            return OpenWithContextualProvider(null, providerIds, SearchFlags.OpenContextual);
        }

        internal static QuickSearch OpenWithContextualProvider(string searchQuery, string[] providerIds, SearchFlags flags, string topic = null)
        {
            var providers = providerIds.Select(id => SearchService.Providers.Find(p => p.id == id)).Where(p => p != null).ToArray();
            if (providers.Length != providerIds.Length)
            {
                Debug.LogWarning($"Cannot find one of these search providers {String.Join(", ", providerIds)}");
                return OpenDefaultQuickSearch();
            }

            if (providerIds.Length == 0)
                return Open(flags: flags);

            var context = SearchService.CreateContext(providers);
            topic = topic ?? string.Join(", ", providers.Select(p => p.name.ToLower()));
            var qsWindow = Create(context, topic, flags);
            qsWindow.SetSearchText(searchQuery ?? SearchSettings.GetScopeValue(k_LastSearchPrefKey, qsWindow.m_ContextHash, ""));

            var evt = SearchAnalytics.GenericEvent.Create(qsWindow.m_WindowId, SearchAnalytics.GenericEventType.QuickSearchOpen, "Contextual");
            evt.message = providers[0].id;
            if (providers.Length > 1)
                evt.description = providers[1].id;
            if (providers.Length > 2)
                evt.description = providers[2].id;
            if (providers.Length > 3)
                evt.stringPayload1 = providers[3].id;
            if (providers.Length > 4)
                evt.stringPayload1 = providers[4].id;

            SearchAnalytics.SendEvent(evt);

            return qsWindow.ShowWindow();
        }

        public QuickSearch ShowWindow(float defaultWidth = 950, float defaultHeight = 538, SearchFlags flags = SearchFlags.OpenDefault)
        {
            var windowSize = new Vector2(defaultWidth, defaultHeight);
            if (flags.HasFlag(SearchFlags.Dockable))
            {
                bool firstOpen = !EditorPrefs.HasKey(k_CheckWindowKeyName);
                if (firstOpen)
                {
                    var centeredPosition = Utils.GetMainWindowCenteredPosition(windowSize);
                    position = centeredPosition;
                    Utils.CallDelayed(() => position = centeredPosition);
                }
                Show(true);
            }
            else
            {
                this.ShowDropDown(windowSize);
            }
            Focus();
            return this;
        }

        public static QuickSearch ShowObjectPicker(
            Action<UnityEngine.Object, bool> selectHandler,
            Action<UnityEngine.Object> trackingHandler,
            string searchText, string typeName, Type filterType,
            float defaultWidth = 850, float defaultHeight = 539, SearchFlags flags = SearchFlags.OpenPicker)
        {
            if (selectHandler == null || typeName == null)
                return null;

            if (filterType == null)
                filterType = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>()
                    .FirstOrDefault(t => t.Name == typeName) ?? typeof(UnityEngine.Object);

            var qs = Create();
            qs.saveFilters = false;
            qs.searchTopic = "object";
            qs.searchEventStatus = SearchEventStatus.WaitForEvent;
            qs.titleContent.text = $"Select {filterType?.Name ?? typeName}...";
            qs.itemIconSize = 64;
            qs.multiselect = false;
            qs.filterCallback = (item) => item == SearchItem.none || (IsObjectMatchingType(item ?? SearchItem.none, filterType));
            qs.selectCallback = (item, canceled) => selectHandler?.Invoke(Utils.ToObject(item, filterType), canceled);
            qs.trackingCallback = (item) => trackingHandler?.Invoke(Utils.ToObject(item, filterType));
            qs.context.wantsMore = true;
            qs.context.filterType = filterType;
            qs.SetSearchText(searchText, TextCursorPlacement.MoveToStartOfNextWord);

            if (flags.HasFlag(SearchFlags.Dockable))
                qs.Show();
            else
                qs.ShowAuxWindow();

            qs.position = Utils.GetMainWindowCenteredPosition(new Vector2(defaultWidth, defaultHeight));
            qs.Focus();

            return qs;
        }

        public void SetSelection(params int[] selection)
        {
            SetSelection(true, selection);
        }

        private void SetSelection(bool trackSelection, params int[] selection)
        {
            if (!multiselect && selection.Length > 1)
                throw new Exception("Multi selection is not allowed.");

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

            if (lastIndexAdded != k_ResetSelectionIndex)
            {
                m_CurrentSearchQuery = null;
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

        public void ExecuteSearchQuery(SearchQuery query, SearchAnalytics.GenericEventType eventType = SearchAnalytics.GenericEventType.SearchQueryExecute)
        {
            SetSelection();

            context.SetProviders(query.providerIds.Select(id => SearchService.GetProvider(id)));
            if (query.viewState != null && query.viewState.isValid)
                SetViewState(query.viewState);
            SetSearchText(query.text);

            if (SearchSettings.savedSearchesSortOrder == SearchQuerySortOrder.RecentlyUsed)
                SearchQuery.SortQueries();

            m_CurrentSearchQuery = query;
            RefreshSearch();

            SendEvent(eventType, query.text);
        }

        internal void SetViewState(ResultViewState viewState)
        {
            itemIconSize = viewState.itemSize;
            m_ResultView.SetViewState(viewState);
            if (!string.IsNullOrEmpty(viewState.group))
                SelectGroup(viewState.group);
        }

        public void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = true)
        {
            var item = items.LastOrDefault();
            if (item == null)
                return;

            SendSearchEvent(item, action);
            EditorApplication.delayCall -= DelayTrackSelection;

            if (selectCallback != null)
            {
                selectCallback(item, false);
                selectCallback = null;
            }
            else
            {
                SearchField.UpdateLastSearchText(context.searchText);

                if (action.handler != null && items.Length == 1)
                    action.handler(item);
                else if (action.execute != null)
                    action.execute(items);
                else
                    action.handler?.Invoke(item);
            }

            if (endSearch && action.closeWindowAfterExecution && !docked)
                CloseSearchWindow();
        }

        public void ShowItemContextualMenu(SearchItem item, Rect position)
        {
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchShowActionMenu, item.provider.id);
            var menu = new GenericMenu();
            var shortcutIndex = 0;
            var currentSelection = new[] { item };
            foreach (var action in item.provider.actions.Where(a => a.enabled(currentSelection)))
            {
                var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                if (shortcutIndex == 0)
                    itemName += " _enter";
                else if (shortcutIndex == 1)
                    itemName += " _&enter";
                else if (shortcutIndex == 2)
                    itemName += " _&%enter";
                else if (shortcutIndex == 3)
                    itemName += " _&%#enter";
                menu.AddItem(new GUIContent(itemName, action.content.image), false, () => ExecuteAction(action, currentSelection, false));
                ++shortcutIndex;
            }

            if (position == default)
                menu.ShowAsContext();
            else
                menu.DropDown(position);
        }

        internal void OnEnable()
        {
            hideFlags |= HideFlags.DontSaveInEditor;
            m_LastFocusedWindow = m_LastFocusedWindow ?? s_FocusedWindow;
            wantsLessLayoutEvents = true;
            testWindow = Utils.IsRunningTests();

            m_SideBarSplitter = new SplitterInfo(SplitterInfo.Side.Left, 0.15f, 0.25f, this);
            m_DetailsPanelSplitter = new SplitterInfo(SplitterInfo.Side.Right, 0.5f, 0.80f, this);

            SearchSettings.SortActionsPriority();

            SetContext(CreateSearchViewContext());

            LoadSessionSettings();

            // Create search view state objects
            m_SearchBoxFocus = true;
            m_DetailView = new DetailView(this);
            m_DebounceTime = 1f;

            resized += OnWindowResized;

            UpdateWindowTitle();
        }

        private SearchContext CreateSearchViewContext()
        {
            // Create search view context
            if (s_GlobalContext == null)
            {
                if (m_ProviderIds == null || m_ProviderIds.Length == 0)
                    return new SearchContext(SearchService.Providers.Where(p => p.active));
                else
                    return new SearchContext(m_ProviderIds.Select(id => SearchService.GetProvider(id)).Where(p => p != null));
            }
            else
            {
                var tempContext = s_GlobalContext;
                s_GlobalContext = null;
                return tempContext;
            }
        }

        internal void OnDisable()
        {
            s_FocusedWindow = null;
            AutoComplete.Clear();

            syncSearch = false;

            resized = null;
            nextFrame = null;
            Utils.tick -= UpdateAsyncResults;
            EditorApplication.update -= DebouncedRefresh;
            EditorApplication.delayCall -= DelayTrackSelection;

            selectCallback?.Invoke(null, true);

            SaveSessionSettings();

            m_DetailView?.Dispose();
            m_ResultView?.Dispose();

            // End search session
            context.asyncItemReceived -= OnAsyncItemsReceived;
            context.Dispose();
            context = null;

            if (!Utils.IsRunningTests())
                Resources.UnloadUnusedAssets();
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
                if (!newWindowSize.Equals(m_WindowSize))
                {
                    if (m_WindowSize.x > 0)
                        resized?.Invoke(m_WindowSize, newWindowSize);
                    m_WindowSize = newWindowSize;
                }

                nextFrame?.Invoke();
                nextFrame = null;
            }

            HandleKeyboardNavigation(evt);
            if (context == null)
                return;

            using (new EditorGUILayout.VerticalScope(GUIStyle.none))
            {
                DrawToolbar(evt);
                DrawPanels(evt);
                DrawStatusBar();
                AutoComplete.Draw(context, this);
            }

            UpdateFocusControlState(evt);
        }

        private void NotifySyncSearch(string groupId, UnityEditor.SearchService.SearchService.SyncSearchEvent evt)
        {
            var syncViewId = groupId;
            switch (groupId)
            {
                case "asset":
                    syncViewId = typeof(UnityEditor.Search.ProjectSearchEngine).FullName;
                    break;
                case "scene":
                    syncViewId = typeof(UnityEditor.Search.SceneSearchEngine).FullName;
                    break;
            }
            UnityEditor.SearchService.SearchService.NotifySyncSearchChanged(evt, syncViewId, context.searchText);
        }


        private void DrawPanels(Event evt)
        {
            using (var s = new EditorGUILayout.HorizontalScope())
            {
                var shrinkedView = position.width <= k_DetailsViewShowMinSize || selectCallback != null;
                var showSideBar = !shrinkedView && m_ShownPanels.HasFlag(ShownPanels.LeftSideBar);
                var showDetails = !shrinkedView && m_ShownPanels.HasFlag(ShownPanels.InspectorPreview);

                var windowWidth = Mathf.Ceil(position.width);
                var resultViewSize = windowWidth;

                if (showSideBar)
                {
                    DrawSideBar(evt, s.rect);
                    resultViewSize -= m_SideBarSplitter.width + 1f;
                }

                if (showDetails)
                {
                    m_DetailsPanelSplitter.Init(windowWidth - 250f);
                    m_DetailsPanelSplitter.Draw(evt, s.rect);

                    resultViewSize -= m_DetailsPanelSplitter.width - 1;
                }

                DrawItems(evt, Mathf.Ceil(resultViewSize));

                if (showDetails)
                {
                    m_DetailView.Draw(context, m_DetailsPanelSplitter.width);
                }
            }
        }

        private void DrawSideBar(Event evt, Rect areaRect)
        {
            m_SideBarSplitter.Init(180f);
            m_SideBarSplitter.Draw(evt, areaRect);

            using (var s = new EditorGUILayout.ScrollViewScope(m_SideBarScrollPosition, false, false,
                GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, Styles.panelBackgroundLeft, GUILayout.Width(m_SideBarSplitter.width), GUILayout.ExpandHeight(true)))
            {
                m_ResultView?.DrawControlLayout(m_SideBarSplitter.width);
                DrawSavedSearches(evt);
                m_SideBarScrollPosition = s.scrollPosition;
            }
        }

        private void DrawSavedSearches(Event evt)
        {
            var maxWidth = GUILayout.MaxWidth(m_SideBarSplitter.width);
            GUILayout.Label("Saved Searches", Styles.panelHeader, maxWidth);

            EditorGUI.BeginChangeCheck();
            var sortOrder = EditorGUILayout.EnumPopup(SearchSettings.savedSearchesSortOrder, Styles.sidebarDropdown);
            if (EditorGUI.EndChangeCheck())
            {
                SearchSettings.savedSearchesSortOrder = (SearchQuerySortOrder)sortOrder;
                SearchSettings.Save();
                SearchQuery.SortQueries();
                SendEvent(SearchAnalytics.GenericEventType.QuickSearchSavedSearchesSorted, SearchSettings.savedSearchesSortOrder.ToString());
            }

            foreach (var query in SearchQuery.savedQueries)
            {
                var itemStyle = query == m_CurrentSearchQuery ? Styles.savedSearchItemSelected : Styles.savedSearchItem;
                if (EditorGUILayout.DropdownButton(new GUIContent(query.displayName, query.tooltip), FocusType.Keyboard, itemStyle, maxWidth))
                    ExecuteSearchQuery(query, SearchAnalytics.GenericEventType.QuickSearchSavedSearchesExecuted);
                var searchQueryButtonRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(searchQueryButtonRect, MouseCursor.Link);
                if (evt.type == EventType.MouseUp && evt.button == 1 && searchQueryButtonRect.Contains(evt.mousePosition))
                {
                    ShowSavedSearchQueryContextualMenu(query);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void ShowSavedSearchQueryContextualMenu(SearchQuery query)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Open"), false, () => ExecuteSearchQuery(query, SearchAnalytics.GenericEventType.QuickSearchSavedSearchesExecuted));
            menu.AddItem(new GUIContent("Edit"), false, () => Selection.activeObject = query);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteSavedSearchQuery(query));

            menu.ShowAsContext();
        }

        private void DeleteSavedSearchQuery(SearchQuery query)
        {
            if (!EditorUtility.DisplayDialog($"Deleting search query {query.name}?",
                $"You are about to delete the search query {query.name}, are you sure?", "Yes", "No"))
                return;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(query));
        }

        internal void OnLostFocus()
        {
            AutoComplete.Clear();
        }

        internal void Update()
        {
            if (focusedWindow != this)
                return;

            var time = EditorApplication.timeSinceStartup;
            var repaintRequested = hasFocus && SearchField.UpdateBlinkCursorState(time);
            if (repaintRequested)
                Repaint();
        }

        private void SetItems(IEnumerable<SearchItem> items)
        {
            m_SearchItemSelection = null;
            m_FilteredItems.Clear();
            m_FilteredItems.AddItems(items);
            SetSelection(trackSelection: false, m_Selection.ToArray());
        }

        private void RefreshViews(RefreshFlags additionalFlags = RefreshFlags.None)
        {
            UpdateWindowTitle();

            m_ResultView?.Refresh(m_DebounceRefreshFlags | additionalFlags);
            m_DetailView?.Refresh(m_DebounceRefreshFlags | additionalFlags);
            m_DebounceRefreshFlags = RefreshFlags.None;

            Repaint();
        }

        private void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            var filteredItems = items;
            if (filterCallback != null)
                filteredItems = filteredItems.Where(item => filterCallback(item));
            m_FilteredItems.AddItems(filteredItems);
            Utils.tick += UpdateAsyncResults;
        }

        private void UpdateAsyncResults()
        {
            Utils.tick -= UpdateAsyncResults;
            RefreshViews(RefreshFlags.ItemsChanged);
        }

        private bool ToggleFilter(string providerId)
        {
            var enabled = context.IsEnabled(providerId);
            if (providerId == m_FilteredItems.currentGroup)
                m_FilteredItems.currentGroup = null;
            context.SetFilter(providerId, !enabled);
            SendEvent(SearchAnalytics.GenericEventType.FilterWindowToggle, providerId, context.IsEnabled(providerId).ToString());
            Refresh();
            return !enabled;
        }

        private void TogglePanelView(ShownPanels panelOption)
        {
            var hasOptions = m_ShownPanels.HasFlag(panelOption);
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchOpenToggleToggleSidePanel, panelOption.ToString(), (!hasOptions).ToString());
            if (hasOptions)
                m_ShownPanels &= ~panelOption;
            else
                m_ShownPanels |= panelOption;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Preferences"), false, () => OpenPreferences());

            var savedSearchContent = new GUIContent("Saved Searches");
            var previewInspectorContent = new GUIContent("Preview Inspector");

            menu.AddItem(savedSearchContent, m_ShownPanels.HasFlag(ShownPanels.LeftSideBar), () => TogglePanelView(ShownPanels.LeftSideBar));
            menu.AddItem(previewInspectorContent, m_ShownPanels.HasFlag(ShownPanels.InspectorPreview), () => TogglePanelView(ShownPanels.InspectorPreview));
            menu.AddItem(new GUIContent("Options/Keep Open"), SearchSettings.keepOpen, () => ToggleKeepOpen());
            menu.AddItem(new GUIContent("Options/Show Status"), SearchSettings.showStatusBar, () => ToggleShowStatusBar());
            menu.AddItem(new GUIContent("Options/Show more results"), context?.wantsMore ?? false, () => ToggleWantsMore());
            if (Utils.isDeveloperBuild)
            {
                menu.AddItem(new GUIContent("Options/Debug"), context?.options.HasFlag(SearchFlags.Debug) ?? false, () => ToggleDebugQuery());
            }
        }

        private void ToggleShowStatusBar()
        {
            SearchSettings.showStatusBar = !SearchSettings.showStatusBar;
        }

        private void ToggleKeepOpen()
        {
            SearchSettings.keepOpen = !SearchSettings.keepOpen;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchSettings.keepOpen), SearchSettings.keepOpen.ToString());
        }

        private void ToggleWantsMore()
        {
            SearchSettings.wantsMore = context.wantsMore = !context?.wantsMore ?? false;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(context.wantsMore), context.wantsMore.ToString());
            Refresh();
        }

        private void ToggleDebugQuery()
        {
            if (context.options.HasFlag(SearchFlags.Debug))
                context.options &= ~SearchFlags.Debug;
            else
                context.options |= SearchFlags.Debug;
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
            SearchAnalytics.SendSearchEvent(evt, context);
        }

        private void UpdateWindowTitle()
        {
            if (!titleContent.image)
                titleContent.image = Icons.quicksearch;
            if (m_FilteredItems.Count == 0)
                titleContent.text = $"Search";
            else
                titleContent.text = $"Search ({m_FilteredItems.Count - (selectCallback != null ? 1 : 0)})";
        }

        private static string FormatStatusMessage(SearchContext context, int totalCount)
        {
            var providers = context.providers.ToList();
            if (providers.Count == 0)
                return "There is no activated search provider";

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
                msg += k_Dots[(int)EditorApplication.timeSinceStartup % k_Dots.Length];

            return msg;
        }

        private static string PrintTime(double timeMs)
        {
            if (timeMs >= 1000)
                return $"{Math.Round(timeMs / 1000.0)} seconds";
            return $"{Math.Round(timeMs)} ms";
        }

        private void DrawStatusBar()
        {
            using (new GUILayout.HorizontalScope(Styles.statusBarBackground))
            {
                if (SearchSettings.showStatusBar)
                {
                    var title = FormatStatusMessage(context, m_FilteredItems?.TotalCount ?? 0);
                    var tooltip = Utils.FormatProviderList(context.providers, fullTimingInfo: true);
                    var statusLabelContent = EditorGUIUtility.TrTextContent(title, tooltip);
                    GUILayout.Label(statusLabelContent, Styles.statusLabel, GUILayout.MaxWidth(position.width - 100));
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                var newItemIconSize = itemIconSize;
                var sliderRect = EditorGUILayout.GetControlRect(false, Styles.statusBarBackground.fixedHeight, GUILayout.Width(55f));
                sliderRect.y -= 1f;
                newItemIconSize = GUI.HorizontalSlider(sliderRect, newItemIconSize, 0f, (float)DisplayMode.Limit);
                if (EditorGUI.EndChangeCheck())
                {
                    newItemIconSize = Mathf.Round(newItemIconSize);
                    itemIconSize = newItemIconSize;
                    SearchSettings.itemIconSize = newItemIconSize;
                    m_ResultView.focusSelectedItem = true;
                }

                var hasProgress = context.searchInProgress;
                if (hasProgress)
                {
                    var searchInProgressRect = EditorGUILayout.GetControlRect(false,
                        Styles.searchInProgressButton.fixedHeight, Styles.searchInProgressButton, Styles.searchInProgressLayoutOptions);

                    int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 5, 11.99f);
                    if (GUI.Button(searchInProgressRect, Styles.statusWheel[frame], Styles.searchInProgressButton))
                    {
                        OpenPreferences();
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button(Styles.prefButtonContent, Styles.statusBarButton))
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
                SearchField.Focus();
                m_SearchBoxFocus = false;
            }
        }

        private bool HandleDefaultPressEnter(Event evt)
        {
            if (evt.type != EventType.KeyDown)
                return false;

            if (AutoComplete.enabled)
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

            if (AutoComplete.HandleKeyEvent(evt))
                return;

            if (HandleDefaultPressEnter(evt))
                return;

            if (SearchField.HandleKeyEvent(evt))
                return;

            if (evt.type == EventType.KeyDown)
            {
                var ctrl = evt.control || evt.command;
                if (evt.keyCode == KeyCode.Escape)
                {
                    SendEvent(SearchAnalytics.GenericEventType.QuickSearchDismissEsc);
                    selectCallback?.Invoke(null, true);
                    selectCallback = null;
                    evt.Use();
                    CloseSearchWindow();
                }
                else if (evt.keyCode == KeyCode.F1)
                {
                    SetSearchText("?");
                    SendEvent(SearchAnalytics.GenericEventType.QuickSearchToggleHelpProviderF1);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F5)
                {
                    Refresh();
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F4)
                {
                    TogglePanelView(ShownPanels.InspectorPreview);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.F3)
                {
                    TogglePanelView(ShownPanels.LeftSideBar);
                    evt.Use();
                }
                else if (evt.modifiers.HasFlag(EventModifiers.Alt) && evt.keyCode == KeyCode.LeftArrow)
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
                else if (evt.modifiers.HasFlag(EventModifiers.Alt) && evt.keyCode == KeyCode.RightArrow)
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
                    if (AutoComplete.Show(context, position))
                        evt.Use();
                }
            }

            if (evt.type != EventType.Used && m_ResultView != null)
                m_ResultView.HandleInputEvent(evt, m_Selection);

            if (m_FilteredItems.Count == 0)
                SelectSearch();
        }

        public void SelectSearch()
        {
            m_SearchBoxFocus = true;
        }

        private void CloseSearchWindow()
        {
            if (s_FocusedWindow)
                s_FocusedWindow.Focus();
            Close();
        }

        private void DrawHelpText()
        {
            GUILayout.Box(GetHelpString(), Styles.noResult, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }

        private string GetHelpString()
        {
            if (string.IsNullOrEmpty(context.searchText.Trim()))
            {
                const string help = "Search {0}!\r\n\r\n" +
                    "- <b>Alt + Up/Down Arrow</b> \u2192 Search history\r\n" +
                    "- <b>Alt + Left/Right Arrow</b> \u2192 Cycle search tabs\r\n" +
                    "- <b>Enter</b> \u2192 Default action\r\n" +
                    "- <b>Alt + Enter</b> \u2192 Secondary action\r\n" +
                    "- Drag items around\r\n" +
                    "- Type <b>?</b> to get help\r\n";
                return string.Format(help, searchTopic);
            }

            var provider = SearchService.GetProvider(m_FilteredItems.currentGroup);
            if (m_FilteredItems.TotalCount == 0 || provider == null)
                return $"No result for query \"{context.searchText}\"\nTry something else?";

            return $"There is no result for {provider.name}\nSelect another search tab?";
        }

        private void DrawItems(Event evt, float availableSpace)
        {
            using (new EditorGUILayout.VerticalScope(Styles.panelBackground))
            {
                DrawTabs(evt, availableSpace);
                if (m_FilteredItems.Count > 0 && m_ResultView != null)
                    m_ResultView.Draw(m_Selection, availableSpace);
                else
                    DrawHelpText();
            }
        }

        static class ComputedValues
        {
            public static float tabButtonsWidth { get; private set; }
            static ComputedValues()
            {
                tabButtonsWidth = Styles.tabMoreButton.CalcSize(Styles.moreProviderFiltersContent).x
                    + Styles.syncButton.CalcSize(Styles.syncSearchButtonContent).x
                    + Styles.syncButton.margin.horizontal
                    + Styles.tabMoreButton.margin.horizontal;
            }
        }

        public IEnumerable<IGroup> EnumerateGroups()
        {
            return m_FilteredItems.EnumerateGroups();
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
                for (var i = 0; i < allGroups.Count; ++i)
                {
                    var group = allGroups[i];
                    var formattedCount = Utils.FormatCount((ulong)group.count);
                    var content = new GUIContent($"{group.name} {string.Format(Styles.tabCountTextColorFormat, formattedCount)}");
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

                DrawSyncSearchButton();

                if (EditorGUILayout.DropdownButton(Styles.moreProviderFiltersContent, FocusType.Keyboard, Styles.tabMoreButton))
                    ShowFilters();
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
        }

        private void ShowFilters()
        {
            var activeProviders = SearchService.OrderedProviders.Where(p => !p.isExplicitProvider).ToList();
            var explicitProviders = SearchService.OrderedProviders.Where(p => p.isExplicitProvider).ToList();
            var filterMenu = new GenericMenu();

            if (activeProviders.Count > 0)
            {
                filterMenu.AddDisabledItem(new GUIContent("Search Providers"));
                filterMenu.AddSeparator("");

                foreach (var p in activeProviders)
                {
                    var filterContent = new GUIContent($"{p.name} ({p.filterId})");
                    filterMenu.AddItem(filterContent, context.IsEnabled(p.id), () => ToggleFilter(p.id));
                }
            }

            if (explicitProviders.Count > 0)
            {
                filterMenu.AddSeparator("");
                filterMenu.AddDisabledItem(new GUIContent("Special Search Providers"));
                filterMenu.AddSeparator("");

                foreach (var p in explicitProviders)
                {
                    var filterContent = new GUIContent($"{p.name} ({p.filterId})");
                    filterMenu.AddItem(filterContent, context.IsEnabled(p.id), () => ToggleExplicitProvider(p));
                }
            }

            filterMenu.ShowAsContext();
        }

        private void ToggleExplicitProvider(SearchProvider provider)
        {
            if (ToggleFilter(provider.id) && !context.searchText.Trim().StartsWith(provider.filterId))
                SetSearchText($"{provider.filterId} ", TextCursorPlacement.MoveLineEnd);
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
            if (m_FilteredItems.currentGroup == m_LastSelectedMoreGroup)
                m_FilteredItems.currentGroup = groupId;
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
            if (selectedProvider != null && selectedProvider.showDetailsOptions.HasFlag(ShowDetailsOptions.ListView))
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

            var evt = SearchAnalytics.GenericEvent.Create(m_WindowId, SearchAnalytics.GenericEventType.QuickSearchSwitchTab, groupId);
            evt.stringPayload1 = m_FilteredItems.currentGroup;
            evt.intPayload1 = m_FilteredItems.GetGroupById(groupId)?.count ?? 0;
            SearchAnalytics.SendEvent(evt);

            m_FilteredItems.currentGroup = groupId;
            if (syncSearch)
                NotifySyncSearch(m_FilteredItems.currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.SyncSearch);

            RefreshViews(RefreshFlags.GroupChanged);
        }

        private void OnWindowResized(Vector2 oldSize, Vector2 newSize)
        {
            m_SideBarSplitter.Resize(oldSize, newSize);
            m_DetailsPanelSplitter.Resize(oldSize, newSize);
        }

        private Rect DrawToolbar(Event evt)
        {
            if (context == null)
                return Rect.zero;

            var toolbarRect = Rect.zero;
            using (var s = new EditorGUILayout.VerticalScope(Styles.toolbar))
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);

                    var searchTextRect = GUILayoutUtility.GetRect(position.width - 300f, Styles.searchField.fixedHeight, Styles.searchField);
                    var searchClearButtonRect = Styles.searchFieldBtn.margin.Remove(searchTextRect);
                    searchClearButtonRect.xMin = searchClearButtonRect.xMax - 20f;

                    EditorGUIUtility.AddCursorRect(searchClearButtonRect, MouseCursor.Arrow);
                    if (evt.type == EventType.MouseUp && searchClearButtonRect.Contains(evt.mousePosition))
                        ClearSearch();

                    var previousSearchText = context.searchText;
                    if (evt.type != EventType.KeyDown || evt.keyCode != KeyCode.None || evt.character != '\r')
                        context.searchText = SearchField.Draw(searchTextRect, context.searchText, Styles.searchField);

                    if (string.IsNullOrEmpty(context.searchText))
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.TextArea(searchTextRect, $"Search {searchTopic}", Styles.placeholderTextStyle);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        GUI.Button(searchClearButtonRect, Icons.clear, Styles.searchFieldBtn);
                    }

                    DrawTabToFilterInfo(evt, searchTextRect);
                    DrawToolbarButtons();

                    if (string.Compare(previousSearchText, context.searchText, StringComparison.Ordinal) != 0)
                    {
                        SetSelection();
                        ClearCurrentErrors();
                        DebouncedRefresh();
                    }
                    else
                    {
                        // Only draw errors when you are done typing, to prevent cases where
                        // the cursor moved because of changes but we did not clear the errors yet.
                        DrawQueryErrors();
                    }
                }
                GUILayout.FlexibleSpace();

                toolbarRect = s.rect;
            }

            return toolbarRect;
        }

        private void DrawQueryErrors()
        {
            if (context.searchInProgress)
                return;

            if (!context.options.HasFlag(SearchFlags.ShowErrorsWithResults) && m_FilteredItems.Count > 0)
                return;

            IEnumerable<SearchQueryError> errors;
            if (m_FilteredItems.currentGroup == (m_FilteredItems as IGroup)?.id)
                errors = context.GetAllErrors();
            else
                errors = context.GetErrorsByProvider(m_FilteredItems.currentGroup);

            var te = SearchField.GetTextEditor();
            var shownErrors = new HashSet<int>();

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
                var errorHashCode = searchQueryError.GetHashCode();
                if (shownErrors.Contains(errorHashCode))
                    continue;

                shownErrors.Add(errorHashCode);

                if (searchQueryError.type == SearchQueryErrorType.Error)
                {
                    SearchField.DrawError(
                        queryErrorStart,
                        searchQueryError.length,
                        searchQueryError.reason);
                }
                else
                {
                    SearchField.DrawWarning(
                        queryErrorStart,
                        searchQueryError.length,
                        searchQueryError.reason);
                }
            }
        }

        private void DrawToolbarButtons()
        {
            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(context.searchQuery)))
            {
                if (GUILayout.Button(Styles.saveQueryButtonContent, Styles.toolbarButton))
                    CreateSearchQueryFromContext();
            }
            GUILayout.Space(8);
        }

        private void DrawTabToFilterInfo(Event evt, Rect searchTextRect)
        {
            if (evt.type != EventType.Repaint)
                return;

            var searchTextTrimmedRect = Styles.searchFieldTabToFilterBtn.margin.Remove(searchTextRect);
            var searchTextWidth = Styles.searchField.CalcSize(new GUIContent(context.searchText)).x;
            if (searchTextWidth < searchTextTrimmedRect.width - Styles.pressToFilterContentWidth)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUI.Label(searchTextTrimmedRect, Styles.pressToFilterContent, Styles.searchFieldTabToFilterBtn);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void ClearSearch()
        {
            m_CurrentSearchQuery = null;
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchClearSearch);
            AutoComplete.Clear();
            context.searchText = "";
            GUI.changed = true;
            GUI.FocusControl(null);
            SetSelection();
            DebouncedRefresh();
            GUIUtility.ExitGUI();
        }

        [CommandHandler("OpenQuickSearch")]
        internal static void OpenQuickSearchCommand(CommandExecuteContext c)
        {
            OpenDefaultQuickSearch();
        }

        private void CreateSearchQueryFromContext()
        {
            var initialFolder = SearchSettings.GetFullQueryFolderPath();
            var searchQueryFileName = SearchQuery.GetQueryName(context.searchQuery);
            var newSearchQueryPath = EditorUtility.SaveFilePanel("Save search query...", initialFolder, searchQueryFileName, "asset");
            if (string.IsNullOrEmpty(newSearchQueryPath))
                return;

            newSearchQueryPath = Utils.CleanPath(newSearchQueryPath);
            if (!System.IO.Directory.Exists(Path.GetDirectoryName(newSearchQueryPath)) || !Utils.IsPathUnderProject(newSearchQueryPath))
                return;

            var pathUnderProject = Utils.GetPathUnderProject(newSearchQueryPath);
            SearchSettings.queryFolder = Utils.CleanPath(Path.GetDirectoryName(pathUnderProject));

            var sq = AssetDatabase.LoadAssetAtPath<SearchQuery>(pathUnderProject) ?? SearchQuery.Create(context);
            if (!sq)
            {
                Debug.LogError($"Failed to save search query at {pathUnderProject}");
                return;
            }

            var queryName = Path.GetFileNameWithoutExtension(pathUnderProject);
            sq.viewState = m_ResultView.SaveViewState(queryName);
            sq.viewState.group = m_FilteredItems.currentGroup;

            if (SearchQuery.SaveQuery(sq, context, SearchSettings.queryFolder, queryName))
            {
                SearchSettings.AddRecentSearch(sq.text);
                SearchQuery.ResetSearchQueryItems();
                Selection.activeObject = sq;
            }

            SendEvent(SearchAnalytics.GenericEventType.QuickSearchCreateSearchQuery, sq.text, SearchSettings.queryFolder);
        }

        internal void DebouncedRefresh()
        {
            EditorApplication.update -= DebouncedRefresh;
            if (!this)
                return;

            if (SearchSettings.debounceMs == 0)
            {
                RefreshSearch();
                return;
            }

            var currentTime = EditorApplication.timeSinceStartup;
            if (m_DebounceTime != 0 && currentTime - m_DebounceTime > (SearchSettings.debounceMs / 1000.0f))
            {
                RefreshSearch();
                m_DebounceTime = 0;
            }
            else
            {
                if (m_DebounceTime == 0)
                    m_DebounceTime = currentTime;
                EditorApplication.update += DebouncedRefresh;
            }
        }

        private static bool IsObjectMatchingType(SearchItem item, Type filterType)
        {
            if (item == SearchItem.none)
                return true;
            var obj = Utils.ToObject(item, filterType);
            if (!obj)
                return false;
            var objType = obj.GetType();
            return filterType.IsAssignableFrom(objType);
        }

        private void LoadContext()
        {
            var contextHash = context.GetHashCode();
            if (context.options.HasFlag(SearchFlags.FocusContext))
            {
                var contextualProvider = GetContextualProvider();
                if (contextualProvider != null)
                    contextHash ^= contextualProvider.id.GetHashCode();
            }
            if (m_ContextHash == 0)
                m_ContextHash = contextHash;
        }

        private bool LoadSessionSettings()
        {
            if (testWindow)
                return false;

            if (context != null && string.IsNullOrEmpty(context.searchText))
                SetSearchText(SearchSettings.GetScopeValue(k_LastSearchPrefKey, m_ContextHash, ""));

            itemIconSize = SearchSettings.itemIconSize;

            m_ShownPanels = (ShownPanels)SearchSettings.GetScopeValue(nameof(m_ShownPanels), m_ContextHash, (int)GetDefaultShowPanelOptions());
            Utils.CallDelayed(() =>
            {
                m_SideBarSplitter.SetPosition(SearchSettings.GetScopeValue(nameof(m_SideBarSplitter), m_ContextHash, -1));
                m_DetailsPanelSplitter.SetPosition(SearchSettings.GetScopeValue(nameof(m_DetailsPanelSplitter), m_ContextHash, -1));
            });

            if (m_FilteredItems != null)
            {
                m_FilteredItems.currentGroup = SearchSettings.GetScopeValue(nameof(m_FilteredItems.currentGroup), m_ContextHash, ((IGroup)m_FilteredItems).id);
                if (context.options.HasFlag(SearchFlags.FocusContext))
                {
                    var contextualProvider = GetContextualProvider();
                    if (contextualProvider != null)
                        m_FilteredItems.currentGroup = contextualProvider.id;
                }
            }

            return true;
        }

        private ShownPanels GetDefaultShowPanelOptions()
        {
            if (context.options.HasFlag(SearchFlags.HidePanels))
                return ShownPanels.None;
            return ShownPanels.InspectorPreview | ShownPanels.LeftSideBar;
        }

        private void SaveSessionSettings()
        {
            if (testWindow)
                return;

            SearchSettings.SetScopeValue(k_LastSearchPrefKey, m_ContextHash, context.searchText);
            if (saveFilters)
            {
                foreach (var p in SearchService.Providers.Where(p => p.active && !p.isExplicitProvider))
                    SearchSettings.SetScopeValue(p.id, m_ContextHash, context.IsEnabled(p.id).ToString());
            }

            SearchSettings.SetScopeValue(nameof(m_ShownPanels), m_ContextHash, (int)m_ShownPanels);
            SearchSettings.SetScopeValue(nameof(m_SideBarSplitter), m_ContextHash, m_SideBarSplitter.pos);
            SearchSettings.SetScopeValue(nameof(m_DetailsPanelSplitter), m_ContextHash, m_DetailsPanelSplitter.pos);

            if (m_FilteredItems != null)
                SearchSettings.SetScopeValue(nameof(m_FilteredItems.currentGroup), m_ContextHash, m_FilteredItems.currentGroup);

            SearchSettings.Save();
        }

        private void UpdateItemSize(float value)
        {
            var oldMode = displayMode;
            m_ItemSize = value > (int)DisplayMode.Limit ? (int)DisplayMode.Limit : value;
            var newMode = displayMode;
            if (m_ResultView == null || oldMode != newMode)
            {
                if (newMode == DisplayMode.List)
                    m_ResultView = new ListView(this);
                else if (newMode == DisplayMode.Grid)
                    m_ResultView = new GridView(this);
                RefreshViews(RefreshFlags.DisplayModeChanged);
            }
        }

        internal void SendEvent(SearchAnalytics.GenericEventType category, string name = null, string message = null, string description = null)
        {
            SearchAnalytics.SendEvent(m_WindowId, category, name, message, description);
        }

        [MenuItem("Edit/Search All... %k", priority = 161)]
        private static QuickSearch OpenDefaultQuickSearch()
        {
            var window = Open(flags: SearchFlags.OpenGlobal);
            SearchAnalytics.SendEvent(window.m_WindowId, SearchAnalytics.GenericEventType.QuickSearchOpen, "Default");
            return window;
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

        private void DrawSyncSearchButton()
        {
            var providerSupportsSync = GetProviderById(m_FilteredItems.currentGroup)?.supportsSyncViewSearch ?? false;
            var searchViewSyncEnabled = providerSupportsSync && SearchViewSyncEnabled(m_FilteredItems.currentGroup);
            var supportsSync = providerSupportsSync && searchViewSyncEnabled;
            using (new EditorGUI.DisabledScope(!supportsSync))
            {
                EditorGUI.BeginChangeCheck();
                var syncButtonContent = !providerSupportsSync ? Styles.syncSearchProviderNotSupportedContent : !searchViewSyncEnabled ? Styles.syncSearchViewNotEnabledContent : syncSearch ? Styles.syncSearchOnButtonContent : Styles.syncSearchButtonContent;
                var sync = GUILayout.Toggle(syncSearch, syncButtonContent, Styles.syncButton);
                if (EditorGUI.EndChangeCheck())
                {
                    SetSyncSearchView(sync);
                }
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
            var flags = SearchFlags.OpenContextual | SearchFlags.ReuseExistingWindow;
            var qsWindow = Create(flags);
            SearchProvider contextualProvider = null;
            if (wasReused)
                contextualProvider = qsWindow.GetContextualProvider();
            qsWindow.ShowWindow(flags: flags);
            if (contextualProvider != null)
                qsWindow.SelectGroup(contextualProvider.id);
            qsWindow.SendEvent(SearchAnalytics.GenericEventType.QuickSearchJumpToSearch, qsWindow.m_FilteredItems.currentGroup, sourceContext);
            qsWindow.SetSearchText(query);
            qsWindow.syncSearch = true;
            c.result = true;
        }


        [Shortcut("Help/Search Contextual")]
        internal static void OpenContextual()
        {
            Open(flags: SearchFlags.OpenContextual);
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

        private SearchProvider GetContextualProvider()
        {
            return context.providers.FirstOrDefault(p => p.active && (p.isEnabledForContextualSearch?.Invoke() ?? false));
        }

        [WindowAction]
        internal static WindowAction CreateSearchHelpWindowAction()
        {
            // Developer-mode render doc button to enable capturing any HostView content/panels
            var action = WindowAction.CreateWindowActionButton("HelpSearch", OpenSearchHelp, null, ContainerWindow.kButtonWidth + 1, Icons.help);
            action.validateHandler = (window, _) => window.GetType() == typeof(QuickSearch);
            return action;
        }

        private static void OpenSearchHelp(EditorWindow window, WindowAction action)
        {
            var windowId = (window as QuickSearch)?.windowId ?? null;
            SearchAnalytics.SendEvent(windowId, SearchAnalytics.GenericEventType.QuickSearchOpenDocLink);
            EditorUtility.OpenWithDefaultApp("https://docs.unity3d.com/Packages/com.unity.quicksearch@3.0/manual/index.html");
        }

    }
}
