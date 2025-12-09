// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private bool? m_isReadOnlyQuery;
        internal bool IsReadOnlyQuery
        {
            get
            {
                if (m_isReadOnlyQuery == null)
                {
                    var path = AssetDatabase.GetAssetPath(this);
                    m_isReadOnlyQuery = Utils.IsAssetReadOnly(path);
                }
                return m_isReadOnlyQuery.Value;
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

        private long m_LastUsedTime;
        public long lastUsedTime
        {
            get
            {
                m_LastUsedTime = SearchQuery.GetLastUsedTime(this);
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
                    var recordKey = PropertyDatabase.CreateRecordKey(guid, SearchQuery.k_QueryItemsNumberPropertyName);
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

        private string m_FilePath;
        public string filePath
        {
            get
            {
                if (string.IsNullOrEmpty(m_FilePath))
                    return AssetDatabase.GetAssetPath(this);
                return m_FilePath;
            }

            internal set
            {
                m_FilePath = value;
            }
        }

        private string m_GUID;
        public string guid
        {
            get
            {
                if (string.IsNullOrEmpty(m_GUID))
                {
                    m_GUID = AssetDatabase.AssetPathToGUID(filePath);
                    if (string.IsNullOrEmpty(m_GUID))
                    {
                        var hash = Hash128.Compute(Encoding.Default.GetBytes(filePath));
                        m_GUID = hash.ToString();
                    }
                }
                return m_GUID;
            }
        }

        [FormerlySerializedAs("searchQuery")]
        public string text;

        [Multiline]
        public string description;
        public ICollection<string> providerIds
        {
            get => viewState?.providerIds ?? new string[0];
            set
            {
                if (viewState != null)
                    viewState.providerIds = value?.ToArray();
            }
        }
        public SearchViewState viewState;
        public Texture2D icon;

        [SerializeField] private bool m_IsSearchTemplate;

        public bool isSearchTemplate
        {
            get => m_IsSearchTemplate;
            set => m_IsSearchTemplate = value;
        }

        // TODO LightExplorer: we currently only support deriving from SearchQueryAsset, but it would be great if we could support any ISearchQuery. But how to serialize it?
        // TODO: is this the best mechanism to inherit a configured viewState?
        public SearchQueryAsset viewStateQuery;

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
            var hasUpdated = updated != null && updated.Any(p => !string.IsNullOrEmpty(p) && p.EndsWith(".asset"));
            var hasMoved = moved != null && moved.Any(p => !string.IsNullOrEmpty(p) && p.EndsWith(".asset"));
            var hasRemoved = removed != null && removed.Any(p => !string.IsNullOrEmpty(p) && p.EndsWith(".asset"));

            if (hasUpdated || hasMoved || hasRemoved)
            {
                ResetSearchQueryItems();

                if (hasUpdated)
                {
                    var updatedQueries = updated.Select(AssetDatabase.LoadAssetAtPath<SearchQueryAsset>).Where(q => q).ToList();
                    Dispatcher.Emit(SearchEvent.PostProcessProjectQueryAdded, new SearchEventPayload((ISearchElement)null, updatedQueries));
                }

                if (hasMoved)
                {
                    var movedQueries = moved.Select(AssetDatabase.LoadAssetAtPath<SearchQueryAsset>).Where(q => q).ToList();
                    Dispatcher.Emit(SearchEvent.PostProcessProjectQueryMoved, new SearchEventPayload((ISearchElement)null, movedQueries));
                }

                if (hasRemoved)
                    Dispatcher.Emit(SearchEvent.PostProcessProjectQueryRemoved, new SearchEventPayload((ISearchElement)null, removed));
            }

            // Old behaviour
            if (hasUpdated || hasRemoved)
            {
                ResetSearchQueryItems();
                Dispatcher.Emit(SearchEvent.ProjectQueryListChanged, new SearchEventPayload((ISearchElement)null));
            }
        }

        internal static IEnumerable<SearchQueryAsset> savedQueries
        {
            get
            {
                if (s_SavedQueries == null)
                {
                    if (!s_ListeningToAssetChanges)
                        ListenToAssetChanges();
                    s_SavedQueries = EnumerateAll().Where(asset => asset).ToList();
                }
                return s_SavedQueries.Where(s => s);
            }
        }

        internal static void ListenToAssetChanges()
        {
            if (!s_ListeningToAssetChanges)
            {
                SearchMonitor.contentRefreshed += ContentRefreshed;
                s_ListeningToAssetChanges = true;
            }
        }

        internal static SearchFilter CreateSearchQuerySearchFilter(string basePath = null)
        {
            var filter = new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                classNames = new[] { nameof(SearchQueryAsset) },
                showAllHits = false
            };

            if (basePath != null)
            {
                filter.searchArea = SearchFilter.SearchArea.SelectedFolders;
                filter.folders = new[] { basePath };
            }

            return filter;
        }

        internal static IEnumerable<SearchQueryAsset> EnumerateAll(string basePath = null)
        {
            using (var savedQueriesItr = AssetDatabase.EnumerateAllAssets(CreateSearchQuerySearchFilter(basePath)))
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
            return Utils.RemoveInvalidCharsFromFileName(Utils.Simplify(query).Replace(":", "_").Replace(" ", "_"));
        }

        public static bool SaveQuery(SearchViewState viewState, string path, out SearchQueryAsset query)
        {
            query = Create(viewState.context);
            query.viewState = viewState;
            var folder = Path.GetDirectoryName(path);
            var assetName = Path.GetFileNameWithoutExtension(path);
            return SaveQuery(query, viewState.context, folder, assetName);
        }

        public static bool SaveQuery(SearchQueryAsset asset, SearchContext context, string folder, string name = null)
        {
            return SaveQuery(asset, context, new SearchViewState(context), folder, name);
        }

        public static bool SaveQuery(SearchQueryAsset asset, SearchContext context, SearchViewState sourceViewState, string folder, string name = null)
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
            var eventPayload = sourceViewState != null ? new SearchEventPayload(sourceViewState, asset) : new SearchEventPayload(context, asset);
            if (createNew)
            {
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.ImportAsset(fullPath);
                Dispatcher.Emit(SearchEvent.ProjectQueryAdded, eventPayload);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                Dispatcher.Emit(SearchEvent.ProjectQueryChanged, eventPayload);
            }
            return createNew;
        }

        public static void RemoveQuery(SearchQueryAsset queryAsset)
        {
            RemoveQuery(queryAsset, false);
        }

        internal static void RemoveQuery(SearchQueryAsset queryAsset, bool skipDialog)
        {
            if (skipDialog || EditorDialog.DisplayDecisionDialog(
                titleText: $"Deleting search query {queryAsset.name}?",
                messageText: $"You are about to delete the search query {queryAsset.name}, are you sure?",
                yesButtonText: default,
                noButtonText: default))
            {
                var oldViewState = queryAsset.viewState;
                var queryId = queryAsset.guid;
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(queryAsset));
                Dispatcher.Emit(SearchEvent.ProjectQueryRemoved, new SearchEventPayload(oldViewState, queryId));
            }
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

        public static ISearchView Open(EntityId instanceId)
        {
            var query = EditorUtility.EntityIdToObject(instanceId) as SearchQueryAsset;
            if (query == null)
                return null;

            return SearchQuery.Open(query, SearchFlags.OpenDefault);
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
            if (viewStateQuery != null && viewStateQuery != this)
                return viewStateQuery.GetViewState();
            return viewState;
        }

        public SearchTable GetSearchTable()
        {
            if (viewStateQuery != null && viewStateQuery != this)
                return viewStateQuery.GetSearchTable();
            return viewState?.tableConfig;
        }
    }
}
