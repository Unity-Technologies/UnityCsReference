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

        public SearchApiSession(params SearchProvider[] providers)
        {
            context = new SearchContext(providers);
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
            searchSessions.Add(context.guid, new SearchApiSession(provider));
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

        public string name => "Advanced";

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

        static SearchProvider s_AssetDatabaseLegacyProvider;

        public static SearchProvider CreateAssetDatabaseLegacyProvider() => new SearchProvider("adb", FetchItems);
        private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            var searchFilter = new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                showAllHits = true,
                originalText = context.searchQuery
            };
            SearchUtility.ParseSearchString(context.searchQuery, searchFilter);
            searchFilter.originalText = context.searchQuery;

            var rIt = AssetDatabase.EnumerateAllAssets(searchFilter);
            while (rIt.MoveNext())
            {
                if (rIt.Current.pptrValue)
                    yield return provider.CreateItem(context, GlobalObjectId.GetGlobalObjectIdSlow(rIt.Current.instanceID).ToString());
            }
        }


        public override void BeginSession(ISearchContext context)
        {
            if (searchSessions.ContainsKey(context.guid))
                return;

            var engineProvider = SearchService.GetProvider(providerId);

            if (s_AssetDatabaseLegacyProvider == null)
                s_AssetDatabaseLegacyProvider = CreateAssetDatabaseLegacyProvider();
            searchSessions.Add(context.guid, new SearchApiSession(s_AssetDatabaseLegacyProvider, engineProvider));
        }

        public virtual IEnumerable<string> Search(ISearchContext context, string query, Action<IEnumerable<string>> asyncItemsReceived)
        {
            if (!searchSessions.ContainsKey(context.guid))
                return new string[] {};

            var searchSession = searchSessions[context.guid];

            if (asyncItemsReceived != null)
            {
                searchSession.onAsyncItemsReceived = asyncItemsReceived;
            }

            if (context.requiredTypeNames != null && context.requiredTypeNames.Any())
            {
                searchSession.context.wantsMore = true;
                searchSession.context.filterType = Utils.GetTypeFromName(context.requiredTypeNames.First());
            }
            else
            {
                searchSession.context.wantsMore = false;
                searchSession.context.filterType = null;
            }
            searchSession.context.searchText = query;
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
            searchSession.context.wantsMore = true;
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

    // TODO: To renenable when SearchPicker epics is being worked on.
    // [ObjectSelectorEngine]
    class ObjectSelectorEngine : QuickSearchEngine, IObjectSelectorEngine
    {
        // Internal for tests purposes.
        internal QuickSearch qsWindow;
        public override string providerId => "asset";

        public override void BeginSearch(ISearchContext context, string query) {}
        public override void BeginSession(ISearchContext context) {}
        public override void EndSearch(ISearchContext context) {}

        public override void EndSession(ISearchContext context)
        {
            qsWindow = null;
        }

        public bool SelectObject(ISearchContext context,
            Action<Object, bool> selectHandler, Action<Object> trackingHandler)
        {
            var selectContext = (ObjectSelectorSearchContext)context;
            var viewFlags = SearchFlags.OpenPicker;
            if (Utils.IsRunningTests())
                viewFlags |= SearchFlags.Dockable;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, "", "object", "ObjectSelectorEngine");
            var searchQuery = string.Join(" ", context.requiredTypeNames.Select(tn => tn == null ? "" : $"t:{tn.ToLowerInvariant()}"));
            if (string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = "";
            }
            else
            {
                searchQuery += " ";
            }
            var viewstate = new SearchViewState(
                SearchService.CreateContext(searchQuery, viewFlags), selectHandler, trackingHandler,
                selectContext.requiredTypeNames.First(), selectContext.requiredTypes.First());

            qsWindow = SearchService.ShowPicker(viewstate) as QuickSearch;

            return qsWindow != null;
        }

        public void SetSearchFilter(ISearchContext context, string searchFilter)
        {
            if (qsWindow)
                qsWindow.SetSearchText(searchFilter);
        }
    }
}
