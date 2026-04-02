// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Search;
using UnityEngine.Serialization;

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
        static readonly string[] emptyProviders = Array.Empty<string>();

        [NonSerialized] private SearchContext m_Context;
        [NonSerialized] private bool m_WasDeserialized;
        [SerializeField] internal string[] providerIds;
        [SerializeField] internal SearchFlags searchFlags;
        [SerializeField] internal string searchText; // Also used as the initial query when the view was created
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
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    ISearchQuery loadedActiveQuery = SearchQuery.searchQueries.FirstOrDefault(query => query.guid == m_ActiveQueryGuid)
#pragma warning restore UA2001
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        ?? (ISearchQuery)SearchQueryAsset.savedQueries.FirstOrDefault(query => query.guid == m_ActiveQueryGuid);
#pragma warning restore UA2001
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

        [Obsolete]
        public float itemSize;

        [FormerlySerializedAs("itemSize")]
        [SerializeField] private float m_ItemIconSize;
        public float itemIconSize {
            get
            {
                return m_ItemIconSize;
            }
            set
            {
                SetItemIconSize(value);
            }

        }

        public Rect position;
        public SearchViewFlags flags;
        public string group;

        internal bool isPicker => HasFlag(SearchViewFlags.ObjectPicker);
        internal bool isSimplePicker => isPicker && !HasFlag(SearchViewFlags.ObjectPickerAdvancedUI) && !SearchSettings.pickerAdvancedUI;
        public bool hasQueryPanel
        {
            get
            {
                if (HasFlag(SearchViewFlags.DisableSavedSearchQuery))
                    return false;
                if (isPicker)
                    return !isSimplePicker;
                return true;
            }
        }

        internal bool isInspectorPanelVisible
        {
            get
            {
                if (flags.HasAny(SearchViewFlags.OpenInspectorPreview) && !context.options.HasAny(SearchFlags.HidePanels))
                    return true;
                return false;
            }
        }

        internal bool hasQueryBuilderToggle
        {
            get
            {
                if (HasFlag(SearchViewFlags.DisableBuilderModeToggle))
                    return false;
                if (isPicker)
                    return !isSimplePicker;
                return true;
            }
        }

        public bool isQueryPanelVisible => hasQueryPanel && HasFlag(SearchViewFlags.OpenLeftSidePanel) && !context.options.HasAny(SearchFlags.HidePanels);

        internal EntityId[] m_SelectedIds;
        internal EntityId[] selectedIds
        {
            get
            {
                if (m_SelectedIds == null)
                    m_SelectedIds = Array.Empty<EntityId>();
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

        // TODO: We should remove this member and assumed that if we are in Picker mode the initial query will perform the filtering.
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

        [SerializeField] internal SearchQueryTreeConfig queryTreeConfig;
        [SerializeField] internal SearchWindowCustomPanelConfig customPanelConfig;

        // Assuming this is not something that uis serialzied or else it would change the behavior of the search window when executing queries.
        private SearchFunctor<Func<ISearchView, string, SearchQueryError, bool>> m_DisplaySearchErrors;
        internal Func<ISearchView, string, SearchQueryError, bool> displaySearchErrors
        {
            get
            {
                return m_DisplaySearchErrors == null ? null : m_DisplaySearchErrors.handler;
            }
            set
            {
                m_DisplaySearchErrors = new SearchFunctor<Func<ISearchView, string, SearchQueryError, bool>>(value);
            }
        }

        internal void SetDisplayMode(DisplayMode displayMode)
        {
            // Keep the itemSize in sync.
            if (m_ResultViewDescriptorList == null || !m_ResultViewDescriptorList.isValid)
            {
                m_ItemIconSize = SearchUtils.GetItemSizeFromDisplayMode(displayMode);
            }
            else if (resultViewDescriptorList.SetCurrentFromDisplayMode(displayMode))
                m_ItemIconSize = SearchUtils.GetItemSizeFromDisplayMode(displayMode);
        }

        internal void SetItemIconSize(float itemSize)
        {
            if (m_ResultViewDescriptorList == null || !m_ResultViewDescriptorList.isValid)
            {
                // ResultViewList has been instantiated yet, only assign itemSize that will be used to init the list.
                m_ItemIconSize = itemSize;
            }
            else if (resultViewDescriptorList.SetCurrentFromItemSize(itemSize))
                m_ItemIconSize = itemSize;
        }

        internal void SetResultView(string viewId)
        {
            // Keep the itemSize in sync.
            resultViewDescriptorList.CurrentViewId = viewId;
            m_ItemIconSize = resultViewDescriptorList.Current.SizeDefault;
        }

        [SerializeField] private SearchResultViewDescriptorList m_ResultViewDescriptorList;
        internal SearchResultViewDescriptorList resultViewDescriptorList
        {
            get
            {
                // Lazy create the descriptor list.
                if (m_ResultViewDescriptorList == null || !m_ResultViewDescriptorList.isValid)
                {
                    m_ResultViewDescriptorList = SearchResultViewDescriptorList.CreateDefaultList();
                    m_ResultViewDescriptorList.SetCurrentFromItemSize(m_ItemIconSize);
                }
                return m_ResultViewDescriptorList;
            }

            [VisibleToOtherModules]
            set
            {
                m_ResultViewDescriptorList = value;
                if (m_ResultViewDescriptorList != null && m_ResultViewDescriptorList.Count > 0)
                {
                    m_ItemIconSize = m_ResultViewDescriptorList.Current.SizeDefault;
                }
            }
        }

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
            m_ItemIconSize = SearchUtils.GetItemSizeFromDisplayMode(DisplayMode.Grid);
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
                filterType = SearchUtils.FindType<UnityEngine.Object>(typeName);
                if (filterType is null)
                    throw new ArgumentNullException(nameof(filterType));
            }
            context.filterType = filterType;

            selectHandler = (item, canceled) => selectObjectHandler?.Invoke(Utils.ToObject(item, filterType), canceled);
            filterHandler = (item) => IsFilteredIn(this, item);
            trackingHandler = (item) => trackingObjectHandler?.Invoke(Utils.ToObject(item, filterType));
            title = filterType?.Name ?? typeName;
        }

        public SearchViewState(SearchContext context, SearchTable tableConfig, SearchViewFlags flags = SearchViewFlags.None)
            : this(context, flags | SearchViewFlags.TableView)
        {
            group = null;
            this.tableConfig = tableConfig;
        }

        public static SearchViewState CreatePickerState(string title, SearchContext context,
                                 Action<UnityEngine.Object, bool> selectObjectHandler,
                                 Action<UnityEngine.Object> trackingObjectHandler,
                                 string typeName, Type filterType, SearchViewFlags flags = SearchViewFlags.None)
        {
            return new SearchViewState(context, selectObjectHandler, trackingObjectHandler, typeName, filterType)
            {
                title = title
            }.SetSearchViewFlags(flags | SearchViewFlags.ObjectPicker);
        }

        internal static SearchViewState CreatePickerState(string title, SearchContext context,
            Action<UnityEngine.Object, bool> selectObjectHandler,
            Action<UnityEngine.Object> trackingObjectHandler,
            Type[] filterTypes, SearchViewFlags flags = SearchViewFlags.None)
        {
            if (filterTypes.Length == 1)
            {
                // Fast path where all the handlers are created to rapidly handle single type filtering:
                return new SearchViewState(context, selectObjectHandler, trackingObjectHandler, filterTypes[0].Name, filterTypes[0])
                {
                    title = title
                }.SetSearchViewFlags(flags | SearchViewFlags.ObjectPicker);
            }

            // Supports arbitrary number of types. Assuming types are handle in an "OR" fashion.
            bool FilterHandler(SearchItem item)
            {
                var i = item ?? SearchItem.clear;
                if (item == SearchItem.clear)
                    return true;

                foreach (var type in filterTypes)
                {
                    if (IsObjectMatchingType(item, type))
                        return true;
                }

                return false;
            }

            void SelectHandler(SearchItem item, bool canceled) => selectObjectHandler?.Invoke(Utils.ToObject(item, filterTypes), canceled);
            void TrackingHandler(SearchItem item) => trackingObjectHandler?.Invoke(Utils.ToObject(item, filterTypes));

            return new SearchViewState(context, SelectHandler)
            {
                title = title,
                trackingHandler = TrackingHandler,
                filterHandler = FilterHandler
            }.SetSearchViewFlags(flags | SearchViewFlags.ObjectPicker);
        }

        public static SearchViewState CreatePickerState(
            string title,
            SearchContext context,
            Action<SearchItem, bool> selectHandler,
            Action<SearchItem> trackingHandler = null,
            Func<SearchItem, bool> filterHandler = null,
            SearchViewFlags flags = SearchViewFlags.None)
        {
            return new SearchViewState(context, selectHandler)
            {
                title = title,
                trackingHandler = trackingHandler,
                filterHandler = filterHandler
            }.SetSearchViewFlags(flags | SearchViewFlags.ObjectPicker);
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
                SetDisplayMode(DisplayMode.Compact);
            }
            else if (flags.HasAny(SearchViewFlags.ListView))
            {
                SetDisplayMode(DisplayMode.List);
            }
            else if (flags.HasAny(SearchViewFlags.GridView))
            {
                SetDisplayMode(DisplayMode.Grid);
            }
            else if (flags.HasAny(SearchViewFlags.TableView))
            {
                SetDisplayMode(DisplayMode.Table);
            }

            if (flags.HasAny(SearchViewFlags.IgnoreSavedSearches))
            {
                ignoreSaveSearches = true;
            }
            if (flags.HasAny(SearchViewFlags.OpenInTextMode))
                queryBuilderEnabled = false;
            else if (flags.HasAny(SearchViewFlags.OpenInBuilderMode) || isSimplePicker)
                queryBuilderEnabled = true;
            return this;
        }

        internal bool CanAssignCustomPanelConfig()
        {
            return customPanelConfig == null || !customPanelConfig.isValid || !customPanelConfig.isLocked;
        }

        internal void Assign(SearchViewState sourceState)
        {
            // Be sure to create a copy of the context
            Assign(sourceState, sourceState.context != null ? new SearchContext(sourceState.context) : null);
        }

        internal void Assign(SearchViewState sourceState, SearchContext searchContext)
        {
            var previousSearchView = context?.searchView;
            if (searchContext != null)
            {
                context = searchContext;
                if (context != null)
                    context.searchView = previousSearchView;
            }
            searchFlags = searchContext?.options ?? sourceState.searchFlags;
            searchText = searchContext?.searchText ?? sourceState.searchText;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            providerIds = searchContext?.GetProviders().Select(p => p.id).ToArray() ?? sourceState.providerIds.ToArray();
#pragma warning restore UA2001

            if (tableConfig != null && sourceState.tableConfig?.columns?.Length > 0)
            {
                tableConfig.Assign(sourceState.tableConfig);
            }
            else
            {
                tableConfig = sourceState.tableConfig?.Clone();
            }

            if (CanAssignCustomPanelConfig())
                customPanelConfig = sourceState.customPanelConfig;

            if (this == sourceState)
                return;

            hideTabs = sourceState.hideTabs;
            sessionId = sourceState.sessionId;
            sessionName = sourceState.sessionName;
            excludeClearItem = sourceState.excludeClearItem;
            ignoreSaveSearches = sourceState.ignoreSaveSearches;
            hideAllGroup = sourceState.hideAllGroup;
            windowTitle = sourceState.windowTitle;

            // NOTE: Active query is only used to persist during domain reload. And it shouldn't be assigned during SearchQueryExecution
            initialQuery = sourceState.initialQuery;
            title = sourceState.title;

            // If the m_ResultViewDescriptorList hasn't been instantiated: create a new one from the sourceState.
            if (m_ResultViewDescriptorList == null)
            {
                m_ResultViewDescriptorList = new SearchResultViewDescriptorList(sourceState.resultViewDescriptorList.Enumerate());
            }
            else
            {
                // If instantiated be sure to merge all new descriptors in our current state.
                resultViewDescriptorList.MergeInto(sourceState.resultViewDescriptorList.Enumerate());
            }

            // Ensure the CurrentView is correctly setup and that itemSize is valid.
            resultViewDescriptorList.CurrentViewId = sourceState.resultViewDescriptorList.CurrentViewId;
            m_ItemIconSize = sourceState.m_ItemIconSize;

            position = sourceState.position;
            flags = sourceState.flags;
            group = sourceState.group;

            queryBuilderEnabled = sourceState.queryBuilderEnabled;

            ValidateState();
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

        void ValidateItemSize()
        {
            var currentViewDesc = resultViewDescriptorList.Current;
            if (m_ItemIconSize < currentViewDesc.SizeMin || m_ItemIconSize > currentViewDesc.SizeMax)
            {
                m_ItemIconSize = currentViewDesc.SizeDefault;
            }
        }

        internal void ValidateState()
        {
            ValidateItemSize();
        }

        internal static SearchFlags ToSearchFlags(SearchViewFlags flags)
        {
            var sf = SearchFlags.None;
            if (flags.HasAny(SearchViewFlags.Debug)) sf |= SearchFlags.Debug;
            if (flags.HasAny(SearchViewFlags.NoIndexing)) sf |= SearchFlags.NoIndexing;
            if (flags.HasAny(SearchViewFlags.Packages)) sf |= SearchFlags.Packages;
            return sf;
        }

        static bool IsFilteredIn(SearchViewState state, SearchItem item)
        {
            return item == SearchItem.clear || (IsObjectMatchingType(item ?? SearchItem.clear, state.context.filterType ?? typeof(UnityEngine.Object)));
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
            // If we were init with a specific view, do not fetch item size from settings.
            if (!flags.HasAny(SearchViewFlags.CompactView | SearchViewFlags.ListView | SearchViewFlags.GridView | SearchViewFlags.TableView) && !runningTests)
                m_ItemIconSize = SearchSettings.itemIconSize;
            hideTabs = SearchSettings.hideTabs;

            if (!runningTests && flags.HasNone(SearchViewFlags.OpenInBuilderMode) && flags.HasNone(SearchViewFlags.OpenInTextMode))
                queryBuilderEnabled = SearchSettings.queryBuilder;

            if (hasContext)
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            providerIds = GetProviderIds().ToArray();
#pragma warning restore UA2001
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
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return m_Context.GetProviders().Select(p => p.id);
#pragma warning restore UA2001
            return providerIds;
        }

        internal SearchProvider GetProviderById(string providerId)
        {
            if (m_Context != null)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return m_Context.GetProviders().FirstOrDefault(p => p.active && p.id == providerId);
#pragma warning restore UA2001
            return null;
        }

        public IEnumerable<string> GetProviderTypes()
        {
            var providers = m_Context != null ? m_Context.GetProviders() : SearchService.GetProviders(providerIds);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return providers.Select(p => p.type).Distinct();
#pragma warning restore UA2001
        }

        public bool HasFlag(SearchViewFlags flags) => (this.flags & flags) != 0;

        public override string ToString()
        {
            return $"[{sessionId}] {text} ({flags})";
        }
    }
}
