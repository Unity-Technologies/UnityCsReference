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

        public Action<IEnumerable<string>> onAsyncItemsReceived { get; set; }

        SearchItemConverter m_SearchItemConverter;

        public SearchApiSession(ISearchContext searchContext, params SearchProvider[] providers)
            : this(searchContext, DefaultSearchItemConverter, providers)
        {}

        public SearchApiSession(ISearchContext searchContext, SearchItemConverter searchItemConverter, params SearchProvider[] providers)
        {
            context = new SearchContext(providers);
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
            searchSession.context.options &= ~SearchFlags.Sorted;
            if (projectSearchContext.searchFilter != null)
            {
                searchSession.context.userData = projectSearchContext.searchFilter;
                if (projectSearchContext.searchFilter.searchArea == SearchFilter.SearchArea.InAssetsOnly)
                {
                    searchSession.context.searchText = $"a:assets {query}";
                }
                else if (projectSearchContext.searchFilter.searchArea == SearchFilter.SearchArea.InPackagesOnly)
                {
                    searchSession.context.options |= SearchFlags.Packages;
                    searchSession.context.searchText = $"a:packages {query}";
                }
                else if (projectSearchContext.searchFilter.searchArea == SearchFilter.SearchArea.AllAssets)
                {
                    searchSession.context.options |= SearchFlags.Packages;
                }
            }

            var items = SearchService.GetItems(searchSession.context/*, SearchFlags.Synchronous*/);
            return SearchItemConverter(items);
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
                searchQuery = "";
            else
                searchQuery += " ";
            var viewstate = new SearchViewState(
                SearchService.CreateContext(GetObjectSelectorProviders(selectContext), searchQuery, viewFlags), selectHandler, trackingHandler,
                selectContext.requiredTypeNames.First(), selectContext.requiredTypes.First());

            qsWindow = SearchService.ShowPicker(viewstate) as QuickSearch;
            return qsWindow != null;
        }

        internal static IEnumerable<SearchProvider> GetObjectSelectorProviders(ObjectSelectorSearchContext context)
        {
            bool allowAssetObjects = (context.visibleObjects & VisibleObjects.Assets) == VisibleObjects.Assets;
            bool allowSceneObjects = (context.visibleObjects & VisibleObjects.Scene) == VisibleObjects.Scene;

            if (allowAssetObjects)
            {
                yield return SearchService.GetProvider(AdbProvider.type);
                yield return SearchService.GetProvider(AssetProvider.type);
            }
            if (allowSceneObjects)
                yield return SearchService.GetProvider(BuiltInSceneObjectsProvider.type);
        }

        public void SetSearchFilter(ISearchContext context, string searchFilter)
        {
            if (qsWindow)
                qsWindow.SetSearchText(searchFilter);
        }
    }
}
