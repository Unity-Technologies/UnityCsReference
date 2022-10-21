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
        bool m_Disposed;

        public SearchContext context { get; private set; }

        public Action<IEnumerable<string>> onAsyncItemsReceived { get; set; }

        public SearchApiSession(ISearchContext searchContext, params SearchProvider[] providers)
        {
            context = new SearchContext(providers);
            context.runtimeContext = new RuntimeSearchContext() { searchEngineContext = searchContext };
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
            onAsyncItemsReceived?.Invoke(items.Select(item => item.id));
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

        public override void BeginSession(ISearchContext context)
        {
            if (searchSessions.ContainsKey(context.guid))
                return;

            var engineProvider = SearchService.GetProvider(providerId);

            var adbProvider = SearchService.GetProvider(AdbProvider.type);
            var searchSession = new SearchApiSession(context, adbProvider, engineProvider);
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

            if (context.requiredTypeNames != null && context.requiredTypeNames.Any())
            {
                searchSession.context.filterType = Utils.GetTypeFromName(context.requiredTypeNames.First());
            }
            else
            {
                searchSession.context.filterType = null;
            }

            searchSession.context.searchText = query;
            searchSession.context.options &= ~SearchFlags.Packages;
            if (projectSearchContext.searchFilter != null)
            {
                searchSession.context.userData = projectSearchContext.searchFilter;
                if (projectSearchContext.searchFilter.searchArea == SearchFilter.SearchArea.InAssetsOnly)
                {
                    searchSession.context.searchText = $"{query} a:assets";
                }
                else if (projectSearchContext.searchFilter.searchArea == SearchFilter.SearchArea.InPackagesOnly)
                {
                    searchSession.context.options |= SearchFlags.Packages;
                    searchSession.context.searchText = $"{query} a:packages";
                }
                else if (projectSearchContext.searchFilter.searchArea == SearchFilter.SearchArea.AllAssets)
                {
                    searchSession.context.options |= SearchFlags.Packages;
                }
            }

            var items = SearchService.GetItems(searchSession.context);
            return items.Select(item => ToPath(item));
        }

        private string ToPath(SearchItem item)
        {
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
            if (context.requiredTypeNames != null && context.requiredTypeNames.Any())
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

        AdvancedObjectSelector m_CurrentSelector;

        public override void BeginSearch(ISearchContext context, string query) {}

        public override void BeginSession(ISearchContext context)
        {
            m_CurrentSelector = null;
            var objectSelectorContext = context as ObjectSelectorSearchContext;
            if (objectSelectorContext == null)
                return;

            if (!TryGetValidHandler(objectSelectorContext, out m_CurrentSelector))
                return;

            var selectorArgs = new AdvancedObjectSelectorParameters(objectSelectorContext);
            m_CurrentSelector.handler(AdvancedObjectSelectorEventType.BeginSession, selectorArgs);
        }
        public override void EndSearch(ISearchContext context) {}

        public override void EndSession(ISearchContext context)
        {
            if (m_CurrentSelector == null)
                return;
            var selectorArgs = new AdvancedObjectSelectorParameters(context);
            m_CurrentSelector.handler(AdvancedObjectSelectorEventType.EndSession, selectorArgs);
            m_CurrentSelector = null;
        }

        public bool SelectObject(ISearchContext context,
            Action<Object, bool> selectHandler, Action<Object> trackingHandler)
        {
            if (m_CurrentSelector == null)
                return false;
            var selectorArgs = new AdvancedObjectSelectorParameters(context, selectHandler, trackingHandler);
            m_CurrentSelector.handler(AdvancedObjectSelectorEventType.OpenAndSearch, selectorArgs);
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, m_CurrentSelector.id, "object", "ObjectSelectorEngine");
            return true;
        }

        public void SetSearchFilter(ISearchContext context, string searchFilter)
        {
            if (m_CurrentSelector == null)
                return;
            var selectorArgs = new AdvancedObjectSelectorParameters(context, searchFilter);
            m_CurrentSelector.handler(AdvancedObjectSelectorEventType.SetSearchFilter, selectorArgs);
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
