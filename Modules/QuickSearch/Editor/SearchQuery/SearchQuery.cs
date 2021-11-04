// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Search
{
    interface ISearchQuery
    {
        string searchText { get; }
        string displayName { get; set; }
        string details { get; set; }
        Texture2D thumbnail { get; }
        string filePath { get; }
        string guid { get; }
        long creationTime { get; }

        SearchViewState GetResultViewState();
        IEnumerable<string> GetProviderIds();
        IEnumerable<string> GetProviderTypes();
    }

    enum SearchQuerySortOrder
    {
        AToZ,
        ZToA,
        CreationTime,
    }

    [Serializable]
    class SearchQuery : ISearchQuery
    {
        public static string userSearchSettingsFolder => Utils.CleanPath(Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Search"));
        public string searchText
        {
            get
            {
                return viewState.context == null ? viewState.searchText : viewState.context.searchText;
            }
            set
            {
                viewState.searchText = value;
                if (viewState.context != null)
                    viewState.context.searchText = value;
            }
        }

        private static List<SearchQuery> s_SearchQueries;
        [SerializeField] private string m_GUID;
        [SerializeField] Texture2D m_Thumbnail;
        [SerializeField] bool m_IsSearchTemplate;
        private long m_CreationTime;

        public string description;
        public string name;
        public SearchViewState viewState;

        public string filePath { get; set; }

        public string guid => m_GUID;

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

        public string displayName
        {
            get => name;
            set => name = value;
        }

        public string details
        {
            get => description;
            set => description = value;
        }

        public Texture2D thumbnail
        {
            get => m_Thumbnail;
            set => m_Thumbnail = value;
        }

        public long creationTime
        {
            get
            {
                if (m_CreationTime == 0 && !string.IsNullOrEmpty(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    m_CreationTime = fileInfo.CreationTime.Ticks;
                }
                return m_CreationTime;
            }
        }

        public static IEnumerable<SearchQuery> searchQueries
        {
            get
            {
                if (s_SearchQueries == null)
                {
                    s_SearchQueries = new List<SearchQuery>();
                    LoadSearchQueries(SearchSettings.projectLocalSettingsFolder, s_SearchQueries);
                    LoadSearchQueries(userSearchSettingsFolder, s_SearchQueries);
                }

                return s_SearchQueries;
            }
        }

        public static IEnumerable<SearchQuery> userQueries => searchQueries.Where(IsUserQuery);

        public SearchQuery()
        {
            m_GUID = Guid.NewGuid().ToString("N");
            viewState = new SearchViewState();
        }

        public SearchQuery(SearchContext context)
            : this()
        {
            viewState = new SearchViewState(context);
        }

        public static SearchQuery Create(SearchViewState state)
        {
            var uq = new SearchQuery();
            uq.name = uq.description = Utils.Simplify(state.context.searchText);
            uq.Set(state);
            return uq;
        }

        public void Set(SearchViewState state)
        {
            if (viewState == null)
                viewState = new SearchViewState();
            viewState.Assign(state);
        }

        public IEnumerable<string> GetProviderIds()
        {
            return viewState.GetProviderIds();
        }

        public IEnumerable<string> GetProviderTypes()
        {
            return viewState.GetProviderTypes();
        }

        public SearchViewState GetResultViewState()
        {
            return viewState;
        }

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(filePath) ? filePath.GetHashCode() : m_GUID.GetHashCode();
        }

        #region UserQueryManagement
        public static bool IsUserQuery(SearchQuery query)
        {
            return !string.IsNullOrEmpty(query.filePath) && query.filePath.StartsWith(userSearchSettingsFolder);
        }

        public static SearchQuery AddUserQuery(SearchViewState state)
        {
            return AddSearchQuery(userSearchSettingsFolder, state);
        }

        public static SearchQuery AddSearchQuery(string folder, SearchViewState state)
        {
            var query = SearchQuery.Create(state);
            query.filePath = Path.Combine(folder, $"{query.guid}.query");
            s_SearchQueries.Add(query);
            SaveSearchQuery(query);
            return query;
        }

        public static void SaveSearchQuery(SearchQuery query)
        {
            var folder = Path.GetDirectoryName(query.filePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var queryJson = EditorJsonUtility.ToJson(query, true);
            Utils.WriteTextFileToDisk(query.filePath, queryJson);
        }

        public static void RemoveSearchQuery(SearchQuery query)
        {
            var index = s_SearchQueries.IndexOf(query);
            if (index != -1)
            {
                s_SearchQueries.RemoveAt(index);
                if (File.Exists(query.filePath))
                    File.Delete(query.filePath);
            }
        }

        private static void LoadSearchQueries(string folder, List<SearchQuery> queries)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var allQueryPaths = Directory.EnumerateFiles(folder, "*.query").Select(Utils.CleanPath);
            foreach (var path in allQueryPaths)
            {
                var query = LoadSearchQuery(path);
                if (query == null)
                    continue;
                queries.Add(query);
            }
        }

        private static SearchQuery LoadSearchQuery(string path)
        {
            if (!File.Exists(path))
                return null;
            try
            {
                var fileContent = File.ReadAllText(path);
                var query = new SearchQuery();
                EditorJsonUtility.FromJsonOverwrite(fileContent, query);
                query.filePath = path;
                if (string.IsNullOrEmpty(query.guid))
                {
                    query.m_GUID = GUID.Generate().ToString();
                }
                return query;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        public static ISearchView Open(ISearchQuery query, SearchFlags additionalFlags)
        {
            var providerIds = QuickSearch.GetMergedProviders(QuickSearch.GetCurrentSearchWindowProviders(), query.GetProviderIds()).Select(p => p.id);
            var searchWindow = QuickSearch.OpenWithContextualProvider(query.searchText, providerIds.ToArray(), additionalFlags, "Unity");
            searchWindow.ExecuteSearchQuery(query);
            return searchWindow;
        }

        public static Texture2D GetIcon(ISearchQuery query)
        {
            if (query.thumbnail)
                return query.thumbnail;
            var displayMode = QuickSearch.GetDisplayModeFromItemSize(query.GetResultViewState().itemSize);
            return QuickSearch.GetIconFromDisplayMode(displayMode);
        }
    }
}
