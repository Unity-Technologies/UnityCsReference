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
            InspectorPreview = 1 << 1,
            StatusBar        = 1 << 2
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
        private static readonly string k_CheckWindowKeyName = $"{typeof(QuickSearch).FullName}h";
        private static readonly string[] k_Dots = { ".", "..", "..." };

        private static EditorWindow s_FocusedWindow;
        private static SearchContext s_GlobalContext = null;

        // Selection state
        private GroupedSearchList m_FilteredItems;
        private readonly List<int> m_Selection = new List<int>();
        private int m_DelayedCurrentSelection = k_ResetSelectionIndex;
        private SearchSelection m_SearchItemSelection;

        private bool m_Disposed = false;
        private DetailView m_DetailView;
        private IResultView m_ResultView;
        private float m_PreviousItemSize = -1;
        private SearchQuery m_CurrentSearchQuery;
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
        [SerializeField] private string searchTopic;

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
        public DisplayMode displayMode { get => IsDisplayGrid() ? DisplayMode.Grid : DisplayMode.List; }
        public float itemIconSize { get => m_ItemSize; set => UpdateItemSize(value); }
        [SerializeField] public bool multiselect { get; set; }

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

        public void Refresh()
        {
            if (context == null)
                return;
            SearchSettings.ApplyContextOptions(context);

            var foundItems = SearchService.GetItems(context);
            if (selectCallback != null)
                foundItems.Add(SearchItem.none);

            SetItems(filterCallback == null ? foundItems : foundItems.Where(item => filterCallback(item)));

            EditorApplication.tick -= UpdateAsyncResults;
            EditorApplication.tick += UpdateAsyncResults;
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
                qsWindow = GetWindow<QuickSearch>();
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

        public static QuickSearch Open(float defaultWidth = 950, float defaultHeight = 539, SearchFlags flags = SearchFlags.OpenDefault)
        {
            return Create(flags).ShowWindow(defaultWidth, defaultHeight, flags);
        }

        public static QuickSearch OpenWithContextualProvider(params string[] providerIds)
        {
            return OpenWithContextualProvider(null, providerIds, SearchFlags.OpenContextual);
        }

        internal static QuickSearch OpenWithContextualProvider(string searchQuery, string[] providerIds, SearchFlags flags)
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
            var qsWindow = Create(context, topic: string.Join(", ", providers.Select(p => p.name.ToLower())), flags);
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
                    EditorApplication.CallDelayed(() => position = centeredPosition);
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

        public void ExecuteSearchQuery(SearchQuery query)
        {
            SetSelection();
            SetSearchText(query.searchQuery);
            m_CurrentSearchQuery = query;

            if (SearchSettings.savedSearchesSortOrder == SearchQuerySortOrder.RecentlyUsed)
                SearchQuery.SortQueries();
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
                var itemName = action.content.tooltip;
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

            m_SideBarSplitter = new SplitterInfo(SplitterInfo.Side.Left, 0.15f, 0.25f, this);
            m_DetailsPanelSplitter = new SplitterInfo(SplitterInfo.Side.Right, 0.5f, 0.80f, this);

            SearchSettings.SortActionsPriority();

            // Create search view context
            if (s_GlobalContext == null)
            {
                if (m_ProviderIds == null || m_ProviderIds.Length == 0)
                    context = new SearchContext(SearchService.Providers.Where(p => p.active));
                else
                    context = new SearchContext(m_ProviderIds.Select(id => SearchService.GetProvider(id)).Where(p => p != null));
            }
            else
            {
                context = s_GlobalContext;
                s_GlobalContext = null;

                // Save provider ids to restore search window state after a domain reload.
                m_ProviderIds = context.filters.Select(f => f.provider.id).ToArray();
            }
            context.searchView = this;
            context.focusedWindow = m_LastFocusedWindow;
            context.asyncItemReceived -= OnAsyncItemsReceived;
            context.asyncItemReceived += OnAsyncItemsReceived;

            m_FilteredItems = new GroupedSearchList(context);
            itemIconSize = SearchSettings.itemIconSize;

            LoadContext();
            LoadSessionSettings();

            // Create search view state objects
            m_SearchBoxFocus = true;
            m_DetailView = new DetailView(this);
            m_DebounceTime = 1f;

            resized += OnWindowResized;

            UpdateWindowTitle();
        }

        internal void OnDisable()
        {
            s_FocusedWindow = null;
            AutoComplete.Clear();

            resized = null;
            nextFrame = null;
            EditorApplication.tick -= UpdateAsyncResults;
            EditorApplication.update -= DebouncedRefresh;
            EditorApplication.delayCall -= DelayTrackSelection;

            selectCallback?.Invoke(null, true);

            if (searchEventStatus == SearchEventStatus.WaitForEvent)
                SendSearchEvent(null); // Track canceled searches

            SaveSessionSettings();

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

            var windowBorder = m_Parent.window.showMode == ShowMode.NormalWindow || docked ? GUIStyle.none : Styles.panelBorder;
            using (new EditorGUILayout.VerticalScope(windowBorder))
            {
                DrawToolbar(evt);
                DrawPanels(evt);
                DrawStatusBar();
                AutoComplete.Draw(context, this);
            }

            UpdateFocusControlState(evt);
        }

        private void DrawPanels(Event evt)
        {
            using (var s = new EditorGUILayout.HorizontalScope())
            {
                var shrinkedView = position.width <= k_DetailsViewShowMinSize || selectCallback != null;
                var showSideBar = !shrinkedView && m_ShownPanels.HasFlag(ShownPanels.LeftSideBar);
                var showDetails = !shrinkedView && m_ShownPanels.HasFlag(ShownPanels.InspectorPreview);

                var resultViewSize = position.width;

                if (showSideBar)
                {
                    DrawSideBar(evt, s.rect);
                    resultViewSize -= m_SideBarSplitter.width;
                }

                if (showDetails)
                {
                    m_DetailsPanelSplitter.Init(position.width - 250f);
                    m_DetailsPanelSplitter.Draw(evt, s.rect);

                    resultViewSize -= m_DetailsPanelSplitter.width;
                }

                DrawItems(evt, resultViewSize);

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
                DrawSavedSearches();
                m_SideBarScrollPosition = s.scrollPosition;
            }
        }

        private void DrawSavedSearches()
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

            foreach (var query in SearchQuery.GetFilteredSearchQueries(context))
            {
                var itemStyle = query == m_CurrentSearchQuery ? Styles.savedSearchItemSelected : Styles.savedSearchItem;
                if (EditorGUILayout.DropdownButton(GUIContent.Temp($"{query.name}", $"{query.searchQuery}"), FocusType.Keyboard, itemStyle, maxWidth))
                    SearchQuery.ExecuteQuery(this, query, SearchAnalytics.GenericEventType.QuickSearchSavedSearchesExecuted);
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
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

        internal void SetItems(IEnumerable<SearchItem> items)
        {
            m_SearchItemSelection = null;
            m_FilteredItems.Clear();
            m_FilteredItems.AddItems(items);
            SetSelection(m_Selection.ToArray());
            UpdateWindowTitle();
        }

        private void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            var filteredItems = items;
            if (filterCallback != null)
                filteredItems = filteredItems.Where(item => filterCallback(item));
            m_FilteredItems.AddItems(filteredItems);
            EditorApplication.tick -= UpdateAsyncResults;
            EditorApplication.tick += UpdateAsyncResults;
        }

        private void UpdateAsyncResults()
        {
            EditorApplication.tick -= UpdateAsyncResults;

            UpdateWindowTitle();
            Repaint();
        }

        private void ToggleFilter(SearchContext.FilterDesc filter)
        {
            context.SetFilter(filter.provider.id, !filter.enabled);
            SendEvent(SearchAnalytics.GenericEventType.FilterWindowToggle, filter.provider.id, filter.enabled.ToString());
            Refresh();
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
            menu.AddItem(new GUIContent("Options/Show Status"), m_ShownPanels.HasFlag(ShownPanels.StatusBar), () => TogglePanelView(ShownPanels.StatusBar));
            menu.AddItem(new GUIContent("Options/Show more results"), context?.wantsMore ?? false, () => ToggleWantsMore());

            foreach (var f in context.filters.Where(f => !f.provider.isExplicitProvider).OrderBy(f => f.provider.priority))
            {
                var filterContent = new GUIContent($"Providers/{f.provider.name} ({f.provider.filterId})");
                menu.AddItem(filterContent, f.enabled, () => ToggleFilter(f));
            }
        }

        private static void ToggleKeepOpen()
        {
            SearchSettings.keepOpen = !SearchSettings.keepOpen;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchSettings.keepOpen), SearchSettings.keepOpen.ToString());
        }

        private void ToggleWantsMore()
        {
            SearchSettings.wantsMore = context.wantsMore = !context?.wantsMore ?? false;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, nameof(context.wantsMore), context.wantsMore.ToString());
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
            evt.searchText = item != null ? $"{context.searchText} => {item.id}" : context.searchText;
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

        private static string FormatStatusMessage(SearchContext context, ICollection<SearchItem> items)
        {
            var providers = context.providers.ToList();
            if (providers.Count == 0)
                return "There is no activated search provider";

            var msg = context.actionId != null ? $"Executing action for {context.actionId} " : "Searching ";
            if (providers.Count > 1)
                msg += Utils.FormatProviderList(providers.Where(p => !p.isExplicitProvider), showFetchTime: !context.searchInProgress);
            else
                msg += Utils.FormatProviderList(providers);

            if (items != null && items.Count > 0)
            {
                msg += $" and found <b>{items.Count}</b> result";
                if (items.Count > 1)
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
                if (m_ShownPanels.HasFlag(ShownPanels.StatusBar))
                {
                    var title = FormatStatusMessage(context, m_FilteredItems);
                    var tooltip = Utils.FormatProviderList(context.providers, fullTimingInfo: true);
                    var statusLabelContent = EditorGUIUtility.TrTextContent(title, tooltip);
                    GUILayout.Label(statusLabelContent, Styles.statusLabel, GUILayout.MaxWidth(position.width - 100));
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                var sliderRect = EditorGUILayout.GetControlRect(false, Styles.statusBarBackground.fixedHeight, GUILayout.Width(55f));
                sliderRect.y -= 1f;
                var newItemIconSize = GUI.HorizontalSlider(sliderRect, itemIconSize, 0f, 165f);
                if (EditorGUI.EndChangeCheck())
                {
                    itemIconSize = newItemIconSize;
                    SearchSettings.itemIconSize = newItemIconSize;
                    SearchAnalytics.DebounceSendEvent(() => SendNewItemIconSize());
                    m_ResultView.focusSelectedItem = true;
                }

                var hasProgress = context.searchInProgress;
                if (hasProgress)
                {
                    var searchInProgressRect = EditorGUILayout.GetControlRect(false,
                        Styles.searchInProgressButton.fixedHeight, Styles.searchInProgressButton, Styles.searchInProgressLayoutOptions);

                    int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 5, 11.99f);
                    if (GUI.Button(searchInProgressRect, Styles.statusWheel[frame], Styles.searchInProgressButton))
                        OpenPreferences();
                }
                else
                {
                    if (GUILayout.Button(Styles.prefButtonContent, Styles.prefButton))
                        OpenPreferences();
                }
            }
        }

        private SearchAnalytics.GenericEvent SendNewItemIconSize()
        {
            var evt = SearchAnalytics.GenericEvent.Create(m_WindowId, SearchAnalytics.GenericEventType.QuickSearchListSizeChanged);
            evt.floatPayload1 = itemIconSize;
            return evt;
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

        private bool IsDisplayGrid()
        {
            return m_ItemSize >= 32;
        }

        private void DrawHelpText()
        {
            const string help = "Search {0}!\r\n\r\n" +
                "- <b>Alt + Up/Down Arrow</b> \u2192 Search history\r\n" +
                "- <b>Alt + Right</b> \u2192 Actions menu\r\n" +
                "- <b>Enter</b> \u2192 Default action\r\n" +
                "- <b>Alt + Enter</b> \u2192 Secondary action\r\n" +
                "- Drag items around\r\n" +
                "- Type <b>?</b> to get help\r\n";

            if (string.IsNullOrEmpty(context.searchText.Trim()))
            {
                GUILayout.Box(string.Format(help, searchTopic), Styles.noResult, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }
            else if (context.actionId != null)
            {
                GUILayout.Box("Waiting for a command...", Styles.noResult, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }
            else
            {
                GUILayout.Box("No result for query \"" + context.searchText + "\"\n" + "Try something else?",
                    Styles.noResult, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }
        }

        private Rect DrawItems(Event evt, float availableSpace)
        {
            Rect areaRect;
            using (var s = new EditorGUILayout.VerticalScope(Styles.panelBackground))
            {
                if (HasTabBar())
                    DrawTabs(evt, availableSpace);
                if (m_FilteredItems.Count > 0 && m_ResultView != null)
                    m_ResultView.Draw(m_Selection, availableSpace);
                else
                    DrawHelpText();
                areaRect = s.rect;
            }
            return areaRect;
        }

        private bool HasTabBar()
        {
            return context.filters.Count(f => !f.provider.isExplicitProvider) > 1;
        }

        private void DrawTabs(Event evt, float availableSpace)
        {
            var groupCount = m_FilteredItems.GetGroupCount();
            var maxBarWidth = availableSpace - Styles.actionButton.CalcSize(Styles.moreProviderFiltersContent).x - 4f;
            using (new EditorGUILayout.HorizontalScope(Styles.searchTabBackground, GUILayout.ExpandWidth(true)))
            {
                var maxTabWidth = 100f;
                foreach (var group in m_FilteredItems.EnumerateGroups())
                {
                    var content = GUIContent.Temp($"{group.name} ({group.count})");
                    maxTabWidth = Mathf.Max(Styles.searchTab.CalcSize(content).x + 6f, maxTabWidth);
                }

                var expandedSize = Mathf.Floor(maxBarWidth / groupCount);
                var shrinked = maxTabWidth > expandedSize;
                if (shrinked)
                    maxTabWidth = expandedSize;

                foreach (var group in m_FilteredItems.EnumerateGroups())
                {
                    var content = shrinked ? GUIContent.Temp(group.name) : GUIContent.Temp($"{group.name} ({group.count})");
                    var tabRect = GUILayoutUtility.GetRect(content, Styles.searchTab, GUILayout.Width(maxTabWidth));
                    var hovered = tabRect.Contains(evt.mousePosition);
                    if (evt.type == EventType.Repaint)
                        Styles.searchTab.Draw(tabRect, content, hovered, m_FilteredItems.currentGroup == group.id, false, false);
                    else if (evt.type == EventType.MouseUp && hovered)
                    {
                        SelectGroup(group.id);
                        evt.Use();
                    }
                }

                GUILayout.FlexibleSpace();
                if (EditorGUILayout.DropdownButton(Styles.moreProviderFiltersContent, FocusType.Keyboard, Styles.tabMoreButton))
                    ShowFilters();
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
        }

        private void ShowFilters()
        {
            var filterMenu = new GenericMenu();
            filterMenu.AddDisabledItem(new GUIContent("Search Providers"));
            filterMenu.AddSeparator("");

            foreach (var f in context.filters.Where(f => !f.provider.isExplicitProvider).OrderBy(f => f.provider.priority))
            {
                var filterContent = new GUIContent($"{f.provider.name} ({f.provider.filterId})");
                filterMenu.AddItem(filterContent, f.enabled, () => ToggleFilter(f));
            }

            filterMenu.AddSeparator("");
            filterMenu.AddDisabledItem(new GUIContent("Special Search Providers"));
            filterMenu.AddSeparator("");

            foreach (var f in context.filters.Where(f => f.provider.isExplicitProvider).OrderBy(f => f.provider.priority))
            {
                var filterContent = new GUIContent($"{f.provider.name} ({f.provider.filterId})");
                filterMenu.AddItem(filterContent, false, () => SetSearchText($"{f.provider.filterId} ", TextCursorPlacement.MoveLineEnd));
            }

            filterMenu.ShowAsContext();
        }

        private void SelectGroup(string groupId)
        {
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
            m_FilteredItems.currentGroup = groupId;
        }

        private void OnWindowResized(Vector2 oldSize, Vector2 newSize)
        {
            m_SideBarSplitter.Resize(oldSize, newSize);
            m_DetailsPanelSplitter.Resize(oldSize, newSize);
        }

        [WindowAction]
        internal static WindowAction OpenSearchHelp()
        {
            // Developer-mode render doc button to enable capturing any HostView content/panels
            var action = WindowAction.CreateWindowActionButton("HelpSearch", (window, _) =>
            {
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchOpenDocLink);
                EditorUtility.OpenWithDefaultApp("https://docs.unity3d.com/Packages/com.unity.quicksearch@2.1/manual/index.html");
            }, null, ContainerWindow.kButtonWidth + 1, EditorGUI.GUIContents.helpIcon.image as Texture2D);
            action.validateHandler = (window, _) => window.GetType() == typeof(QuickSearch);
            return action;
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
                        DebouncedRefresh();
                    }
                }
                GUILayout.FlexibleSpace();

                toolbarRect = s.rect;
            }

            return toolbarRect;
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
            var searchTextWidth = Styles.searchField.CalcSize(GUIContent.Temp(context.searchText)).x;
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
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchClearSearch);
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
            var searchQueryFileName = SearchQuery.GetQueryName(context.searchQuery);
            var initialFolder = Utils.CleanPath(new DirectoryInfo(SearchSettings.queryFolder).FullName);
            if (!System.IO.Directory.Exists(Path.GetDirectoryName(initialFolder)) || !Utils.IsPathUnderProject(initialFolder))
                initialFolder = new DirectoryInfo("Assets").FullName;
            var newSearchQueryPath = EditorUtility.SaveFilePanel("Save Search Query", initialFolder, searchQueryFileName, "asset");
            if (string.IsNullOrEmpty(newSearchQueryPath))
                return;

            newSearchQueryPath = Utils.CleanPath(newSearchQueryPath);
            if (!System.IO.Directory.Exists(Path.GetDirectoryName(newSearchQueryPath)) || !Utils.IsPathUnderProject(newSearchQueryPath))
                return;

            var pathUnderProject = Utils.GetPathUnderProject(newSearchQueryPath);
            SearchSettings.queryFolder = Utils.CleanPath(Path.GetDirectoryName(pathUnderProject));

            var sq = SearchQuery.Create(context);
            SendEvent(SearchAnalytics.GenericEventType.QuickSearchCreateSearchQuery, sq.searchQuery, SearchSettings.queryFolder);
            SearchQuery.SaveQuery(sq, SearchSettings.queryFolder, Path.GetFileNameWithoutExtension(newSearchQueryPath));
            Selection.activeObject = sq;
            SearchSettings.AddRecentSearch(sq.searchQuery);
            SearchQuery.ResetSearchQueryItems();
        }

        private void DebouncedRefresh()
        {
            EditorApplication.update -= DebouncedRefresh;
            if (!this)
                return;

            if (SearchSettings.debounceMs == 0)
            {
                Refresh();
                return;
            }

            var currentTime = EditorApplication.timeSinceStartup;
            if (m_DebounceTime != 0 && currentTime - m_DebounceTime > (SearchSettings.debounceMs / 1000.0f))
            {
                Refresh();
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
                var contextualProvider = context.providers.FirstOrDefault(p => p.active && (p.isEnabledForContextualSearch?.Invoke() ?? false));
                if (contextualProvider != null)
                {
                    contextHash ^= contextualProvider.id.GetHashCode();
                    m_FilteredItems.currentGroup = contextualProvider.id;
                }
            }
            if (m_ContextHash == 0)
                m_ContextHash = contextHash;
        }

        private bool LoadSessionSettings()
        {
            if (Utils.IsRunningTests())
                return false;
            context.ResetFilter(true);
            foreach (var f in context.filters)
            {
                if (bool.TryParse(SearchSettings.GetScopeValue(f.provider.id, m_ContextHash, "True"), out var enabled))
                    context.SetFilter(f.provider.id, enabled);
            }
            if (context != null && string.IsNullOrEmpty(context.searchText))
                SetSearchText(SearchSettings.GetScopeValue(k_LastSearchPrefKey, m_ContextHash, ""));

            m_ShownPanels = (ShownPanels)SearchSettings.GetScopeValue(nameof(m_ShownPanels), m_ContextHash, (int)GetDefaultShowPanelOptions());
            EditorApplication.CallDelayed(() =>
            {
                m_SideBarSplitter.SetPosition(SearchSettings.GetScopeValue(nameof(m_SideBarSplitter), m_ContextHash, -1));
                m_DetailsPanelSplitter.SetPosition(SearchSettings.GetScopeValue(nameof(m_DetailsPanelSplitter), m_ContextHash, -1));
            });

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
            if (Utils.IsRunningTests())
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

            SearchSettings.Save();
        }

        private void UpdateItemSize(float value)
        {
            var oldMode = displayMode;
            m_ItemSize = value;
            var newMode = displayMode;
            if (m_ResultView == null || oldMode != newMode)
            {
                if (newMode == DisplayMode.List)
                    m_ResultView = new ListView(this);
                else if (newMode == DisplayMode.Grid)
                    m_ResultView = new GridView(this);
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

        [Shortcut("Help/Search Contextual", KeyCode.C, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
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

        [InitializeOnLoadMethod]
        internal static void SetupSearchFirstUse()
        {
            if (!AssetPostprocessorIndexer.IsMainProcess())
                return;

            if (SearchSettings.onBoardingDoNotAskAgain || Utils.IsRunningTests())
                return;

            EditorApplication.delayCall += () =>
            {
                if (SearchDatabase.EnumeratePaths(SearchDatabase.IndexLocation.assets).Count() == 0)
                    SearchDatabase.CreateDefaultIndex();

                SearchSettings.onBoardingDoNotAskAgain = true;
                SearchSettings.Save();
            };
        }
    }
}
