// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEngine.Serialization;

namespace UnityEditor.Search
{
    enum SearchQuerySortOrder
    {
        RecentlyUsed,
        AToZ,
        ZToA
    }

    /// <summary>
    /// Asset storing a query that will be executable by a SearchEngine.
    /// </summary>
    [Serializable, ExcludeFromPreset]
    class SearchQuery : ScriptableObject
    {
        static List<SearchQuery> s_SavedQueries;
        internal static IEnumerable<SearchQuery> savedQueries
        {
            get
            {
                if (s_SavedQueries == null || s_SavedQueries.Any(qs => !qs))
                {
                    s_SavedQueries = EnumerateAll().Where(asset => asset != null).ToList();
                    SortQueries();
                }

                return s_SavedQueries;
            }
        }

        private static IEnumerable<SearchQuery> EnumerateAll()
        {
            var savedQueriesItr = AssetDatabase.EnumerateAllAssets(new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.InAssetsOnly,
                classNames = new[] { nameof(SearchQuery) },
                showAllHits = false
            });

            s_SavedQueries = new List<SearchQuery>();
            while (savedQueriesItr.MoveNext())
                yield return savedQueriesItr.Current.pptrValue as SearchQuery;
        }

        public static SearchQuery Create(SearchContext context, string description = null, Texture2D icon = null)
        {
            return Create(context.searchText, context.filters.Select(f => f.provider), description, icon);
        }

        public static SearchQuery Create(string searchQuery, IEnumerable<string> providerIds, string description = null, Texture2D icon = null)
        {
            var queryAsset = CreateInstance<SearchQuery>();
            queryAsset.text = searchQuery;
            queryAsset.providerIds = providerIds.ToList();
            queryAsset.description = description;
            queryAsset.icon = icon;
            return queryAsset;
        }

        public static SearchQuery Create(string searchQuery, IEnumerable<SearchProvider> providers, string description = null, Texture2D icon = null)
        {
            return Create(searchQuery, providers.Select(p => p.id), description, icon);
        }

        public static string GetQueryName(string query)
        {
            return RemoveInvalidChars(query.Replace(":", "_").Replace(" ", "_"));
        }

        private static string RemoveInvalidChars(string filename)
        {
            filename = string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
            if (filename.Length > 0 && !char.IsLetterOrDigit(filename[0]))
                filename = filename.Substring(1);
            return filename;
        }

        public static bool SaveQuery(SearchQuery asset, SearchContext context, string folder, string name = null)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (string.IsNullOrEmpty(name))
                name = GetQueryName(asset.text);
            name += ".asset";

            asset.text = context.searchText;
            asset.providerIds = new List<string>(context.filters.Select(f => f.provider.id));

            var createNew = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset));
            var fullPath = Path.Combine(folder, name).Replace("\\", "/");
            if (createNew)
            {
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.ImportAsset(fullPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            return createNew;
        }

        public static IEnumerable<SearchQuery> GetFilteredSearchQueries(SearchContext context)
        {
            return savedQueries.Where(query => query && query.providerIds.Any(id => context.filters.Any(f => f.enabled && f.provider.id == id)));
        }

        public static IEnumerable<SearchItem> GetAllSearchQueryItems(SearchContext context)
        {
            var queryProvider = SearchService.GetProvider(Providers.Query.type);
            return GetFilteredSearchQueries(context).Select(query =>
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(query).ToString();
                var description = string.IsNullOrEmpty(query.description) ? $"{query.text}" : $"{query.description} ({query.text})";
                var thumbnail = query.icon ? query.icon : Icons.favorite;
                return queryProvider.CreateItem(context, id, query.name, description, thumbnail, query);
            }).OrderBy(item => item.label);
        }

        public static void SortQueries()
        {
            switch (SearchSettings.savedSearchesSortOrder)
            {
                case SearchQuerySortOrder.RecentlyUsed:
                {
                    var now = DateTime.Now.Ticks;
                    s_SavedQueries = savedQueries.OrderByDescending(asset =>
                    {
                        var recentSearchIndex = SearchSettings.recentSearches.IndexOf(asset.text);
                        if (recentSearchIndex != -1)
                        {
                            return now + SearchSettings.recentSearches.Count - recentSearchIndex;
                        }

                        return asset.creationTime;
                    }).ToList();
                }
                break;
                case SearchQuerySortOrder.AToZ:
                    s_SavedQueries = savedQueries.OrderBy(asset => asset.name).ToList();
                    break;
                case SearchQuerySortOrder.ZToA:
                    s_SavedQueries = savedQueries.OrderByDescending(asset => asset.name).ToList();
                    break;
            }
        }

        public static void ResetSearchQueryItems()
        {
            s_SavedQueries = null;
        }

        public static ISearchView Open(string path)
        {
            return Open(Utils.GetMainAssetInstanceID(path));
        }

        public static ISearchView Open(int instanceId)
        {
            var query = EditorUtility.InstanceIDToObject(instanceId) as SearchQuery;
            if (query == null)
                return null;
            var searchWindow = QuickSearch.OpenWithContextualProvider(null, query.providerIds.ToArray(), SearchFlags.ReuseExistingWindow, "Unity");
            ExecuteQuery(searchWindow, query, SearchAnalytics.GenericEventType.SearchQueryOpen);
            return searchWindow;
        }

        [OnOpenAsset]
        private static bool OpenQuery(int instanceID, int line)
        {
            return Open(instanceID) != null;
        }

        public static void ExecuteQuery(ISearchView view, SearchQuery query, SearchAnalytics.GenericEventType sourceEvt = SearchAnalytics.GenericEventType.SearchQueryExecute)
        {
            if (view is QuickSearch qs)
            {
                qs.SendEvent(sourceEvt, query.text);
                qs.ExecuteSearchQuery(query);
            }
        }

        private long m_CreationTime;
        public long creationTime
        {
            get
            {
                if (m_CreationTime == 0)
                {
                    var path = AssetDatabase.GetAssetPath(this);
                    var fileInfo = new FileInfo(path);
                    m_CreationTime = fileInfo.CreationTime.Ticks;
                }
                return m_CreationTime;
            }
        }
        public string description;
        public Texture2D icon;
        [FormerlySerializedAs("searchQuery")] public string text;
        public List<string> providerIds;
        public ResultViewState viewState;
    }
}
