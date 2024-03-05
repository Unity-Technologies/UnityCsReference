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
    /// <summary>
    /// Asset storing a query that will be executable by a SearchEngine.
    /// </summary>
    [Serializable, ExcludeFromPreset]
    [HelpURL("search-usage")]
    class SearchQueryAsset : ScriptableObject, ISearchQuery
    {
        static bool s_ListeningToAssetChanges = false;
        static List<SearchQueryAsset> s_SavedQueries;
        internal static event Action projectQueriesChanged;

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

        private long m_LastUsedTime;
        public long lastUsedTime
        {
            get
            {
                using (var view = SearchMonitor.GetView())
                {
                    var recordKey = PropertyDatabase.CreateRecordKey(guid, QuickSearch.k_LastUsedTimePropertyName);
                    if (view.TryLoadProperty(recordKey, out object data))
                        m_LastUsedTime = (long)data;
                }
                return m_LastUsedTime;
            }
        }

        private int m_ItemCount = -1;
        public int itemCount
        {
            get
            {
                using (var view = SearchMonitor.GetView())
                {
                    var recordKey = PropertyDatabase.CreateRecordKey(guid, QuickSearch.k_QueryItemsNumberPropertyName);
                    if (view.TryLoadProperty(recordKey, out object data))
                        m_ItemCount = (int)data;
                }

                return m_ItemCount;
            }
        }

        public string searchText => text;

        public string displayName
        {
            get => string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this)) : name;
            set => name = value;
        }

        public string details
        {
            get => description;
            set => description = value;
        }

        public Texture2D thumbnail
        {
            get => icon;
            set => icon = value;
        }
        public string filePath => AssetDatabase.GetAssetPath(this);

        private string m_GUID;
        public string guid
        {
            get
            {
                if (string.IsNullOrEmpty(m_GUID))
                {
                    m_GUID = AssetDatabase.GUIDFromAssetPath(filePath).ToString();
                }
                return m_GUID;
            }
        }

        [FormerlySerializedAs("searchQuery")]
        public string text;

        [Multiline]
        public string description;
        
        public List<string> providerIds;
        
        public SearchViewState viewState;
        public Texture2D icon;

        [SerializeField] private bool m_IsSearchTemplate;

        public bool isSearchTemplate
        {
            get
            {
                return m_IsSearchTemplate;
            }
            set
            {
                m_IsSearchTemplate = value;
            }
        }

        public string tooltip
        {
            get
            {
                if (string.IsNullOrEmpty(description))
                    return text;
                return $"{description}\n> {text}";
            }
        }

        internal static void ContentRefreshed(string[] updated, string[] removed, string[] moved)
        {
            if (updated.Any(p => p.EndsWith(".asset")) || removed.Any(p => p.EndsWith(".asset")))
            {
                ResetSearchQueryItems();
                projectQueriesChanged?.Invoke();
            }
        }

        internal static IEnumerable<SearchQueryAsset> savedQueries
        {
            get
            {
                if (s_SavedQueries == null)
                {
                    if (!s_ListeningToAssetChanges)
                    {
                        SearchMonitor.contentRefreshed += ContentRefreshed;
                        s_ListeningToAssetChanges = true;
                    }
                    s_SavedQueries = EnumerateAll().Where(asset => asset).ToList();
                }
                return s_SavedQueries.Where(s => s);
            }
        }

        private static SearchFilter CreateSearchQuerySearchFilter()
        {
            return new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                classNames = new[] { nameof(SearchQueryAsset) },
                showAllHits = false
            };
        }


        private static IEnumerable<SearchQueryAsset> EnumerateAll()
        {
            using (var savedQueriesItr = AssetDatabase.EnumerateAllAssets(CreateSearchQuerySearchFilter()))
            {
                while (savedQueriesItr.MoveNext())
                    yield return savedQueriesItr.Current.pptrValue as SearchQueryAsset;
            }
        }

        public string GetName()
        {
            return name;
        }

        public static SearchQueryAsset Create(SearchContext context, string description = null)
        {
            return Create(context.searchText, context.GetProviders(), description);
        }

        public static SearchQueryAsset Create(string searchQuery, IEnumerable<string> providerIds, string description = null)
        {
            var queryAsset = CreateInstance<SearchQueryAsset>();
            queryAsset.text = searchQuery;
            queryAsset.providerIds = providerIds.ToList();
            queryAsset.description = description;
            return queryAsset;
        }

        public static SearchQueryAsset Create(string searchQuery, IEnumerable<SearchProvider> providers, string description = null)
        {
            return Create(searchQuery, providers.Select(p => p.id), description);
        }

        public static string GetQueryName(string query)
        {
            return SearchUtils.RemoveInvalidChars(Utils.Simplify(query).Replace(":", "_").Replace(" ", "_"));
        }

        public static bool SaveQuery(SearchQueryAsset asset, SearchContext context, string folder, string name = null)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (string.IsNullOrEmpty(name))
                name = GetQueryName(asset.text);
            name += ".asset";

            asset.text = context.searchText;
            asset.providerIds = new List<string>(context.GetProviders().Except(SearchService.GetActiveProviders()).Select(p => p.id));

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

        public static IEnumerable<SearchQueryAsset> GetFilteredSearchQueries(SearchContext context)
        {
            return savedQueries.Where(query => query && (query.providerIds.Count == 0 || query.providerIds.Any(id => context.IsEnabled(id))));
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
            var query = EditorUtility.InstanceIDToObject(instanceId) as SearchQueryAsset;
            if (query == null)
                return null;

            return SearchQuery.Open(query, SearchFlags.Default);
        }

        public static void AddToRecentSearch(ISearchQuery query)
        {
            SearchSettings.AddRecentSearch(query.searchText);
        }

        public SearchQuery ToSearchQuery()
        {
            var viewState = GetViewState();
            return new SearchQuery(viewState);
        }

        [OnOpenAsset]
        private static bool OpenQuery(int instanceID, int line)
        {
            return Open(instanceID) != null;
        }

        public IEnumerable<SearchProvider> GetProviders()
        {
            if (providerIds == null || providerIds.Count == 0)
                return SearchService.GetActiveProviders();

            return SearchService.GetProviders(providerIds);
        }

        public IEnumerable<string> GetProviderIds()
        {
            return providerIds ?? Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetProviderTypes()
        {
            return GetProviders().Select(p => p.type).Distinct();
        }

        public SearchViewState GetViewState()
        {
            return viewState;
        }

        public SearchTable GetSearchTable()
        {
            return viewState?.tableConfig;
        }
    }
}
