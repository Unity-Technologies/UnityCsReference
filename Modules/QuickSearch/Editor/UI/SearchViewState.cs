// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Search;

namespace UnityEditor.Search
{
    static class SearchViewFlagsExtensions
    {
        public static bool HasAny(this SearchViewFlags flags, SearchViewFlags f) => (flags & f) != 0;
        public static bool HasAll(this SearchViewFlags flags, SearchViewFlags all) => (flags & all) == all;
        public static bool HasNone(this SearchViewFlags flags, SearchViewFlags f) => (flags & f) == 0;
    }

    [Serializable]
    public class SearchViewState : ISerializationCallbackReceiver
    {
        public static readonly Vector2 defaultSize = new Vector2(850f, 539f);
        static readonly string[] emptyProviders = new string[0];

        [NonSerialized] private SearchContext m_Context;
        [NonSerialized] private bool m_WasDeserialized;
        [SerializeField] internal string[] providerIds;
        [SerializeField] internal SearchFlags searchFlags;
        [SerializeField] internal string searchText; // Also used as the initial query when the view was created
        [SerializeField] internal bool forceViewMode;
        [SerializeField] private SearchFunctor<Action<SearchItem, bool>> m_SelectHandler;
        [SerializeField] private SearchFunctor<Action<SearchItem>> m_TrackingHandler;
        [SerializeField] private SearchFunctor<Func<SearchItem, bool>> m_FilterHandler;
        [SerializeField] private SearchFunctor<Action<SearchContext, string, string>> m_GroupChanged;
        [NonSerialized] private SearchGlobalEventHandlerManager m_GlobalEventManager = new SearchGlobalEventHandlerManager();
        internal SearchGlobalEventHandlerManager globalEventManager => m_GlobalEventManager;

        [SerializeField] private string m_ActiveQueryGuid;
        private ISearchQuery m_ActiveQuery;
        internal ISearchQuery activeQuery
        {
            get
            {
                if (m_ActiveQuery == null && !string.IsNullOrEmpty(m_ActiveQueryGuid))
                {
                    ISearchQuery loadedActiveQuery = SearchQuery.searchQueries.FirstOrDefault(query => query.guid == m_ActiveQueryGuid)
                        ?? (ISearchQuery)SearchQueryAsset.savedQueries.FirstOrDefault(query => query.guid == m_ActiveQueryGuid);
                    m_ActiveQuery = loadedActiveQuery;
                    m_ActiveQueryGuid = null;
                }
                return m_ActiveQuery;
            }
            set
            {
                m_ActiveQuery = value;
                Dispatcher.Emit(SearchEvent.ActiveQueryChanged, new SearchEventPayload(this, value));
            }
        }

        public bool hideTabs;
        public string sessionId;
        public string sessionName;
        public bool excludeClearItem;
        public SearchTable tableConfig;
        public bool ignoreSaveSearches;
        public bool hideAllGroup;
        public GUIContent windowTitle;
        public string title;
        public float itemSize;
        public Rect position;
        public SearchViewFlags flags;
        public string group;

        internal int[] m_SelectedIds;
        internal int[] selectedIds
        {
            get
            {
                if (m_SelectedIds == null)
                    m_SelectedIds = new int[0];
                return m_SelectedIds;
            }
            set
            {
                m_SelectedIds = value;
            }
        }

        [SerializeField] public bool queryBuilderEnabled;

        public Action<SearchItem, bool> selectHandler { get => m_SelectHandler?.handler; set => m_SelectHandler = new SearchFunctor<Action<SearchItem, bool>>(value); }
        public Action<SearchItem> trackingHandler { get => m_TrackingHandler?.handler; set => m_TrackingHandler = new SearchFunctor<Action<SearchItem>>(value); }
        internal Func<SearchItem, bool> filterHandler { get => m_FilterHandler?.handler; set => m_FilterHandler = new SearchFunctor<Func<SearchItem, bool>>(value); }
        public Action<SearchContext, string, string> groupChanged { get => m_GroupChanged?.handler; set => m_GroupChanged = new SearchFunctor<Action<SearchContext, string, string>>(value); }

        public bool hasWindowSize => position.width > 0f && position.height > 0;
        public Vector2 windowSize => hasWindowSize ? position.size : defaultSize;


        [SerializeField] bool m_ContextUseExplicitProvidersAsNormalProviders;

        public SearchContext context
        {
            get
            {
                if (m_Context == null && m_WasDeserialized && Utils.IsMainProcess())
                    BuildContext();
                return m_Context;
            }

            internal set
            {
                m_Context = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        internal bool hasContext => m_Context != null;

        public string text
        {
            get
            {
                if (m_Context != null)
                    return m_Context.searchText;
                return searchText;
            }

            set
            {
                searchText = value;
                if (m_Context != null)
                    m_Context.searchText = value;
            }
        }

        internal string initialQuery { get; set; }

        internal SearchViewState() : this(null, null) {}
        public SearchViewState(SearchContext context) : this(context, null) {}

        public SearchViewState(SearchContext context, SearchViewFlags flags)
            : this(context, null)
        {
            SetSearchViewFlags(flags);
        }

        public SearchViewState(SearchContext context, Action<SearchItem, bool> selectHandler)
        {
            m_Context = context;
            sessionId = Guid.NewGuid().ToString("N");
            this.selectHandler = selectHandler;
            trackingHandler = null;
            filterHandler = null;
            title = "item";
            itemSize = (float)DisplayMode.Grid;
            position = Rect.zero;
            initialQuery = searchText = context?.searchText ?? string.Empty;
            tableConfig = null;
            providerIds = emptyProviders;
        }

        public SearchViewState(SearchContext context,
                                 Action<UnityEngine.Object, bool> selectObjectHandler,
                                 Action<UnityEngine.Object> trackingObjectHandler,
                                 string typeName, Type filterType)
            : this(context, null)
        {
            if (filterType == null && !string.IsNullOrEmpty(typeName))
            {
                filterType = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => t.Name == typeName);
                if (filterType is null)
                    throw new ArgumentNullException(nameof(filterType));
            }
            context.filterType = filterType;

            selectHandler = (item, canceled) => selectObjectHandler?.Invoke(Utils.ToObject(item, filterType), canceled);
            filterHandler = (item) => item == SearchItem.clear || (IsObjectMatchingType(item ?? SearchItem.clear, filterType ?? typeof(UnityEngine.Object)));
            trackingHandler = (item) => trackingObjectHandler?.Invoke(Utils.ToObject(item, filterType));
            title = filterType?.Name ?? typeName;
        }

        public SearchViewState(SearchContext context, SearchTable tableConfig, SearchViewFlags flags = SearchViewFlags.None)
            : this(context, flags | SearchViewFlags.TableView)
        {
            group = null;
            this.tableConfig = tableConfig;
        }

        internal SearchViewState SetSearchViewFlags(SearchViewFlags flags)
        {
            if (m_Context != null)
            {
                context.options |= ToSearchFlags(flags);
            }
            this.flags = flags;

            if (flags.HasAny(SearchViewFlags.CompactView))
            {
                itemSize = 0;
                forceViewMode = true;
            }
            if (flags.HasAny(SearchViewFlags.ListView))
            {
                itemSize = (float)DisplayMode.List;
                forceViewMode = true;
            }
            if (flags.HasAny(SearchViewFlags.GridView))
            {
                itemSize = (float)DisplayMode.Grid;
                forceViewMode = true;
            }
            if (flags.HasAny(SearchViewFlags.TableView))
            {
                itemSize = (float)DisplayMode.Table;
                forceViewMode = true;
            }
            if (flags.HasAny(SearchViewFlags.OpenInBuilderMode)) queryBuilderEnabled = true;
            if (flags.HasAny(SearchViewFlags.OpenInTextMode)) queryBuilderEnabled = false;
            return this;
        }

        internal void Assign(SearchViewState state)
        {
            // Be sure to create a copy of the context
            Assign(state, state.context != null ? new SearchContext(state.context) : null);
        }

        internal void Assign(SearchViewState state, SearchContext searchContext)
        {
            var previousSearchView = context?.searchView;
            if (searchContext != null)
            {
                context = searchContext;
                if (context != null)
                    context.searchView = previousSearchView;
            }
            searchFlags = searchContext?.options ?? state.searchFlags;
            searchText = searchContext?.searchText ?? state.searchText;
            providerIds = searchContext?.GetProviders().Select(p => p.id).ToArray() ?? state.providerIds.ToArray();

            if (tableConfig != null && state.tableConfig?.columns?.Length > 0)
                tableConfig.columns = state.tableConfig?.columns.ToArray();
            else
                tableConfig = state.tableConfig?.Clone();

            if (this == state)
                return;

            sessionId = state.sessionId;
            sessionName = state.sessionName;
            excludeClearItem = state.excludeClearItem;
            ignoreSaveSearches = state.ignoreSaveSearches;
            activeQuery = state.activeQuery;
            initialQuery = state.initialQuery;
            title = state.title;
            itemSize = state.itemSize;
            position = state.position;
            flags = state.flags;
            forceViewMode = state.forceViewMode;
            group = state.group;

            queryBuilderEnabled = state.queryBuilderEnabled;
        }

        internal void BuildContext()
        {
            if (providerIds != null && providerIds.Length > 0)
                m_Context = SearchService.CreateContext(providerIds, searchText ?? string.Empty, searchFlags);
            else
                m_Context = SearchService.CreateContext(searchText ?? string.Empty, searchFlags | SearchFlags.OpenDefault);
            m_Context.useExplicitProvidersAsNormalProviders = m_ContextUseExplicitProvidersAsNormalProviders;
            m_WasDeserialized = false;
        }

        internal static SearchFlags ToSearchFlags(SearchViewFlags flags)
        {
            var sf = SearchFlags.None;
            if (flags.HasAny(SearchViewFlags.Debug)) sf |= SearchFlags.Debug;
            if (flags.HasAny(SearchViewFlags.NoIndexing)) sf |= SearchFlags.NoIndexing;
            if (flags.HasAny(SearchViewFlags.Packages)) sf |= SearchFlags.Packages;
            return sf;
        }

        static bool IsObjectMatchingType(in SearchItem item, in Type filterType)
        {
            if (item == SearchItem.clear)
                return true;
            var objType = item.ToType(filterType);
            if (objType == null)
                return false;
            return filterType.IsAssignableFrom(objType);
        }

        public static SearchViewState LoadDefaults()
        {
            var viewState = new SearchViewState();
            return viewState.LoadDefaults();
        }

        internal SearchViewState LoadDefaults(SearchFlags additionalFlags = SearchFlags.None)
        {
            var runningTests = Utils.IsRunningTests();
            if (string.IsNullOrEmpty(title))
                title = "Unity";
            if (!forceViewMode && !runningTests)
                itemSize = SearchSettings.itemIconSize;
            hideTabs = SearchSettings.hideTabs;

            if (!runningTests && flags.HasNone(SearchViewFlags.OpenInBuilderMode) && flags.HasNone(SearchViewFlags.OpenInTextMode))
                queryBuilderEnabled = SearchSettings.queryBuilder;

            if (context != null)
            {
                context.options |= additionalFlags;
                if (runningTests)
                    context.options |= SearchFlags.Dockable;
            }
            return this;
        }

        public void OnBeforeSerialize()
        {
            if (context == null)
                return;
            searchFlags = context.options;
            searchText = context.searchText;
            providerIds = GetProviderIds().ToArray();
            m_ActiveQueryGuid = m_ActiveQuery?.guid;
            m_ContextUseExplicitProvidersAsNormalProviders = context.useExplicitProvidersAsNormalProviders;
        }

        public void OnAfterDeserialize()
        {
            m_WasDeserialized = true;
            initialQuery = searchText;
            if (tableConfig != null && tableConfig.columns?.Length == 0)
                tableConfig = null;
        }

        public IEnumerable<string> GetProviderIds()
        {
            if (m_Context != null)
                return m_Context.GetProviders().Select(p => p.id);
            return providerIds;
        }

        internal SearchProvider GetProviderById(string providerId)
        {
            if (m_Context != null)
                return m_Context.GetProviders().FirstOrDefault(p => p.active && p.id == providerId);
            return null;
        }

        public IEnumerable<string> GetProviderTypes()
        {
            var providers = m_Context != null ? m_Context.GetProviders() : SearchService.GetProviders(providerIds);
            return providers.Select(p => p.type).Distinct();
        }

        public bool HasFlag(SearchViewFlags flags) => (this.flags & flags) != 0;

        public override string ToString()
        {
            return $"[{sessionId}] {text} ({flags})";
        }
    }
}
