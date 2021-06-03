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
    class SearchViewState : ISerializationCallbackReceiver
    {
        static Vector2 defaultSize = new Vector2(850f, 539f);

        public SearchContext context
        {
            get
            {
                if (m_Context == null && m_WasDeserialized)
                    BuildContext();
                return m_Context;
            }

            set
            {
                m_Context = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
        [NonSerialized] private SearchContext m_Context;
        [NonSerialized] private bool m_WasDeserialized;
        [SerializeField] private string[] providerIds;
        [SerializeField] private SearchFlags searchFlags;
        [SerializeField] internal string searchText; // Also used as the initial query when the view was created

        public string sessionId;
        public string sessionName;

        public string title;
        public float itemSize;
        public Rect position;
        public bool centered;
        public bool forceViewMode;
        public SearchViewFlags flags = SearchViewFlags.EnableSearchQuery;

        [NonSerialized] public Action<SearchItem, bool> selectHandler;
        [NonSerialized] public Action<SearchItem> trackingHandler;
        [NonSerialized] public Func<SearchItem, bool> filterHandler;

        public bool hasWindowSize => position.width > 0f && position.height > 0;
        public Vector2 windowSize => hasWindowSize ? position.size : defaultSize;

        public SearchViewState()
            : this(null, null) {}

        public SearchViewState(SearchContext context)
            : this(context, null) {}

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
            searchText = context?.searchText ?? string.Empty;
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
            filterHandler = (item) => item == SearchItem.none || (IsObjectMatchingType(item ?? SearchItem.none, filterType ?? typeof(UnityEngine.Object)));
            trackingHandler = (item) => trackingObjectHandler?.Invoke(Utils.ToObject(item, filterType));
            title = filterType?.Name ?? typeName;
        }

        public SearchViewState SetSearchViewFlags(SearchViewFlags flags)
        {
            context.options |= ToSearchFlags(flags);

            // Do not use |= as flags that were not set (like EnableSearchQuery) must properly propagate
            this.flags = flags;
            if (flags.HasAny(SearchViewFlags.Centered))
                centered = true;

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
            return this;
        }

        public void Assign(SearchViewState state)
        {
            providerIds = state.context.providers.Select(p => p.id).ToArray();
            searchFlags = state.searchFlags;
            searchText = state.context.searchText;
            sessionId = state.sessionId;
            sessionName = state.sessionName;

            title = state.title;
            itemSize = state.itemSize;
            position = state.position;
            flags = state.flags;
            centered = state.centered;
            forceViewMode = state.forceViewMode;

            BuildContext();
        }

        private void BuildContext()
        {
            if (providerIds != null && providerIds.Length > 0)
                m_Context = SearchService.CreateContext(providerIds, searchText ?? string.Empty, searchFlags);
            else
                m_Context = SearchService.CreateContext(searchText ?? string.Empty, searchFlags | SearchFlags.OpenDefault);
            m_WasDeserialized = false;
        }

        public static SearchFlags ToSearchFlags(SearchViewFlags flags)
        {
            var sf = SearchFlags.None;
            if (flags.HasAny(SearchViewFlags.Debug)) sf |= SearchFlags.Debug;
            if (flags.HasAny(SearchViewFlags.NoIndexing)) sf |= SearchFlags.NoIndexing;
            if (flags.HasAny(SearchViewFlags.Packages)) sf |= SearchFlags.Packages;
            return sf;
        }

        static bool IsObjectMatchingType(SearchItem item, Type filterType)
        {
            if (item == SearchItem.none)
                return true;
            var obj = item.ToObject(filterType);
            if (!obj)
                return false;
            var objType = obj.GetType();
            return filterType.IsAssignableFrom(objType);
        }

        public static SearchViewState LoadDefaults()
        {
            var viewState = new SearchViewState();
            return viewState.LoadDefaults();
        }

        public SearchViewState LoadDefaults(SearchFlags additionalFlags = SearchFlags.None)
        {
            if (string.IsNullOrEmpty(title))
                title = "Unity";
            if (!forceViewMode)
                itemSize = SearchSettings.itemIconSize;

            if (context != null)
            {
                context.options |= additionalFlags;
                if (Utils.IsRunningTests())
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
        }

        public void OnAfterDeserialize()
        {
            m_WasDeserialized = true;
        }

        public IEnumerable<string> GetProviderIds()
        {
            return context.GetProviders().Select(p => p.id);
        }
    }
}
