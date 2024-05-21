// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search.Providers;
using UnityEditor.SearchService;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    class SearchApiSession : IDisposable
    {
        public delegate IEnumerable<string> SearchItemConverter(IEnumerable<SearchItem> items);

        bool m_Disposed;

        public SearchContext context { get; private set; }

        public object userData { get; set; }

        public Action<IEnumerable<string>> onAsyncItemsReceived { get; set; }

        SearchItemConverter m_SearchItemConverter;

        public SearchApiSession(ISearchContext searchContext, params SearchProvider[] providers)
            : this(searchContext, DefaultSearchItemConverter, providers)
        {}

        public SearchApiSession(ISearchContext searchContext, SearchItemConverter searchItemConverter, params SearchProvider[] providers)
        {
            context = new SearchContext(providers);
            context.runtimeContext = new RuntimeSearchContext() { searchEngineContext = searchContext };
            m_SearchItemConverter = searchItemConverter;
        }

        ~SearchApiSession()
        {
            Dispose(false);
        }

        public void StopAsyncResults()
        {
            if (context.searchInProgress)
            {
                context.sessions.StopAllAsyncSearchSessions();
            }
            context.asyncItemReceived -= OnAsyncItemsReceived;
        }

        public void StartAsyncResults()
        {
            context.asyncItemReceived += OnAsyncItemsReceived;
        }

        private void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            onAsyncItemsReceived?.Invoke(m_SearchItemConverter(items));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                StopAsyncResults();
                context?.Dispose();
                m_Disposed = true;
                context = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static IEnumerable<string> DefaultSearchItemConverter(IEnumerable<SearchItem> items)
        {
            return items.Select(item => item.id);
        }
    }

    abstract class QuickSearchEngine : ISearchEngineBase, IDisposable
    {
        bool m_Disposed;

        public Dictionary<Guid, SearchApiSession> searchSessions = new Dictionary<Guid, SearchApiSession>();

        ~QuickSearchEngine()
        {
            Dispose(false);
        }

        public virtual void BeginSession(ISearchContext context)
        {
            if (searchSessions.ContainsKey(context.guid))
                return;

            var provider = SearchService.Providers.First(p => p.id == providerId);
            searchSessions.Add(context.guid, new SearchApiSession(context, provider));
        }

        public virtual void EndSession(ISearchContext context)
        {
            if (!searchSessions.ContainsKey(context.guid))
                return;

            searchSessions[context.guid].StopAsyncResults();
            searchSessions[context.guid].Dispose();
            searchSessions.Remove(context.guid);
        }

        public virtual void BeginSearch(ISearchContext context, string query)
        {
            if (!searchSessions.ContainsKey(context.guid))
                return;
            searchSessions[context.guid].StopAsyncResults();
            searchSessions[context.guid].StartAsyncResults();
        }

        public virtual void EndSearch(ISearchContext context) {}

        internal static string k_Name = "Advanced";

        public string name => k_Name;

        public abstract string providerId { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                foreach (var kvp in searchSessions)
                {
                    kvp.Value.Dispose();
                }

                m_Disposed = true;
                searchSessions = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    [ProjectSearchEngine]
    class ProjectSearchEngine : QuickSearchEngine, IProjectSearchEngine
    {
        public override string providerId => "asset";
        private static QueryEngine s_QueryEngine = new QueryEngine(validateFilters: false);
        private static List<IFilterNode> s_Filters = new();
        private static List<ISearchNode> s_Searches = new();

        public override void BeginSession(ISearchContext context)
        {
            if (searchSessions.ContainsKey(context.guid))
                return;

            var engineProvider = SearchService.GetProvider(providerId);

            var adbProvider = SearchService.GetProvider(AdbProvider.type);
            var searchSession = new SearchApiSession(context, SearchItemConverter, adbProvider, engineProvider);
            searchSessions.Add(context.guid, searchSession);
        }

        public virtual IEnumerable<string> Search(ISearchContext context, string query, Action<IEnumerable<string>> asyncItemsReceived)
        {
            if (!searchSessions.ContainsKey(context.guid))
                return new string[] {};

            var searchSession = searchSessions[context.guid];
            var projectSearchContext = (ProjectSearchContext)context;

            if (asyncItemsReceived != null)
            {
                searchSession.onAsyncItemsReceived = asyncItemsReceived;
            }
            SetSearchContext(query, projectSearchContext, searchSession.context);
            var items = SearchService.GetItems(searchSession.context);
            return SearchItemConverter(items);
        }

        public static string ConvertContainsToEqual(string query)
        {
            if (!query.Contains(":"))
                return query;

            var parsedQuery = s_QueryEngine.ParseQuery(query);
            s_Filters.Clear();
            s_Searches.Clear();
            SearchUtils.GetQueryParts(parsedQuery.queryGraph.root, s_Filters, s_Searches);
            var processedQuery = query.ToCharArray();
            foreach (var filter in s_Filters)
            {
                if (filter.operatorId != ":")
                    continue;
                processedQuery[filter.token.position + filter.filterId.Length] = '=';
            }
            return new string(processedQuery);
        }

        public static void SetSearchContext(string query, ProjectSearchContext project, SearchContext context)
        {
            if (project.requiredTypeNames != null && project.requiredTypeNames.FirstOrDefault() != null)
            {
                context.filterType = Utils.GetTypeFromName(project.requiredTypeNames.First());
            }
            else
            {
                context.filterType = null;
            }

            context.options &= ~SearchFlags.Packages;
            var processedQuery = ConvertContainsToEqual(query);
            if (project.searchFilter != null)
            {
                context.userData = project.searchFilter;
                
                // Note: SearchFilter aggregates filter strangely. It uses the typed query and adds filter without those be represented
                // in text form. For now use the result of FilterToSearchFieldString to get all filters. It would be best if we could combine those filter ourself.
                processedQuery = project.searchFilter.FilterToSearchFieldString();
                if (project.searchFilter.searchArea == SearchFilter.SearchArea.InAssetsOnly)
                {
                    processedQuery = $"{processedQuery} a:assets";
                }
                else if (project.searchFilter.searchArea == SearchFilter.SearchArea.InPackagesOnly)
                {
                    context.options |= SearchFlags.Packages;
                    processedQuery = $"{processedQuery} a:packages";
                }
                else if (project.searchFilter.searchArea == SearchFilter.SearchArea.AllAssets)
                {
                    context.options |= SearchFlags.Packages;
                }
            }
            context.searchText = processedQuery;
        }

        static IEnumerable<string> SearchItemConverter(IEnumerable<SearchItem> items)
        {
            return items.Select(ToPath);
        }

        static string ToPath(SearchItem item)
        {
            if (item.data is AssetProvider.AssetMetaInfo ami)
            {
                if (!string.IsNullOrEmpty(ami.path))
                    return ami.path;
            }

            if (GlobalObjectId.TryParse(item.id, out var gid))
                return AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            return item.id;
        }
    }

    [SceneSearchEngine]
    class SceneSearchEngine : QuickSearchEngine, ISceneSearchEngine
    {
        private readonly Dictionary<Guid, HashSet<int>> m_SearchItemsBySession = new Dictionary<Guid, HashSet<int>>();

        public override string providerId => "scene";

        public override void BeginSearch(ISearchContext context, string query)
        {
            if (!searchSessions.ContainsKey(context.guid))
                return;
            base.BeginSearch(context, query);

            var searchSession = searchSessions[context.guid];
            if (searchSession.context.searchText == query)
                return;

            searchSession.context.searchText = query;
            if (context.requiredTypeNames != null && context.requiredTypeNames.FirstOrDefault() != null)
            {
                searchSession.context.filterType = Utils.GetTypeFromName(context.requiredTypeNames.First());
            }
            else
            {
                searchSession.context.filterType = typeof(GameObject);
            }

            if (!m_SearchItemsBySession.ContainsKey(context.guid))
                m_SearchItemsBySession.Add(context.guid, new HashSet<int>());
            var searchItemsSet = m_SearchItemsBySession[context.guid];
            searchItemsSet.Clear();

            foreach (var id in SearchService.GetItems(searchSession.context, SearchFlags.Synchronous).Select(item => Convert.ToInt32(item.id)))
            {
                searchItemsSet.Add(id);
            }
        }

        public override void EndSearch(ISearchContext context)
        {
            if (!searchSessions.ContainsKey(context.guid))
                return;
            base.EndSearch(context);
        }

        public override void EndSession(ISearchContext context)
        {
            if (m_SearchItemsBySession.ContainsKey(context.guid))
            {
                m_SearchItemsBySession[context.guid].Clear();
                m_SearchItemsBySession.Remove(context.guid);
            }
            base.EndSession(context);
        }

        public virtual bool Filter(ISearchContext context, string query, HierarchyProperty objectToFilter)
        {
            if (!m_SearchItemsBySession.ContainsKey(context.guid))
                return false;
            return m_SearchItemsBySession[context.guid].Contains(objectToFilter.instanceID);
        }
    }

    [ObjectSelectorEngine]
    class ObjectSelectorEngine : QuickSearchEngine, IObjectSelectorEngine
    {
        public override string providerId => "asset";

        public override void BeginSearch(ISearchContext context, string query) {}

        public override void BeginSession(ISearchContext context)
        {
            if (searchSessions.ContainsKey(context.guid))
                return;

            var objectSelectorContext = context as ObjectSelectorSearchContext;
            if (objectSelectorContext == null)
                return;

            if (!TryGetValidHandler(objectSelectorContext, out var selector))
                return;

            var session = new SearchApiSession(context) { userData = selector };
            searchSessions[context.guid] = session;
            var selectorArgs = new AdvancedObjectSelectorParameters(objectSelectorContext);
            selector.handler(AdvancedObjectSelectorEventType.BeginSession, selectorArgs);
        }
        public override void EndSearch(ISearchContext context) {}

        public override void EndSession(ISearchContext context)
        {
            if (!searchSessions.TryGetValue(context.guid, out var session))
                return;
            var selector = (AdvancedObjectSelector)session.userData;
            var selectorArgs = new AdvancedObjectSelectorParameters(context);
            selector.handler(AdvancedObjectSelectorEventType.EndSession, selectorArgs);

            base.EndSession(context);
        }

        public bool SelectObject(ISearchContext context,
            Action<Object, bool> selectHandler, Action<Object> trackingHandler)
        {
            if (!searchSessions.TryGetValue(context.guid, out var session))
                return false;
            var selector = (AdvancedObjectSelector)session.userData;
            var selectorArgs = new AdvancedObjectSelectorParameters(context, selectHandler, trackingHandler);
            selector.handler(AdvancedObjectSelectorEventType.OpenAndSearch, selectorArgs);
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, selector.id, "object", "ObjectSelectorEngine");
            return true;
        }

        public void SetSearchFilter(ISearchContext context, string searchFilter)
        {
            if (!searchSessions.TryGetValue(context.guid, out var session))
                return;
            var selector = (AdvancedObjectSelector)session.userData;
            var selectorArgs = new AdvancedObjectSelectorParameters(context, searchFilter);
            selector.handler(AdvancedObjectSelectorEventType.SetSearchFilter, selectorArgs);
        }

        static bool TryGetValidHandler(ObjectSelectorSearchContext context, out AdvancedObjectSelector selector)
        {
            selector = null;
            foreach (var searchSelector in GetActiveSelectors())
            {
                if (searchSelector.validator.handler(context))
                {
                    selector = searchSelector;
                    return true;
                }
            }

            return false;
        }

        static IEnumerable<AdvancedObjectSelector> GetActiveSelectors()
        {
            return SearchService.OrderedObjectSelectors.Where(p => p.active);
        }
    }
}
