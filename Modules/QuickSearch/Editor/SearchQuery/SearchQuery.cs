// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Search;

namespace UnityEditor.Search
{
    interface ISearchQuery
    {
        string searchText { get; }
        string displayName { get; set; }
        Texture2D thumbnail { get; }
        string filePath { get; }
        string guid { get; }
        long creationTime { get; }

        ResultViewState GetResultViewState();
        IEnumerable<string> GetProviderIds();
    }

    enum SearchQuerySortOrder
    {
        AToZ,
        ZToA,
        CreationTime
    }

    [Serializable]
    class SearchQuery : ISearchQuery
    {
        public static string userSearchSettingsFolder => Utils.CleanPath(Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Search"));
        public string searchText => viewState.context.searchText;

        public string displayName
        {
            get => name;
            set => name = value;
        }

        [SerializeField] Texture2D m_Thumbnail;
        public Texture2D thumbnail
        {
            get => m_Thumbnail;
            set => m_Thumbnail = value;
        }
        [SerializeField] private string m_GUID;
        public string guid => m_GUID;
        public string filePath { get; set; }

        private long m_CreationTime;
        public long creationTime
        {
            get
            {
                if (m_CreationTime == 0)
                {
                    var fileInfo = new FileInfo(filePath);
                    m_CreationTime = fileInfo.CreationTime.Ticks;
                }
                return m_CreationTime;
            }
        }

        public string description;
        public string name;
        public SearchViewState viewState;
        public SearchTable tableConfig;

        public static SearchQuery Create(SearchViewState state, SearchTable table)
        {
            var uq = new SearchQuery();
            uq.m_GUID = GUID.Generate().ToString();
            uq.name = uq.description = state.context.searchText;
            uq.Set(state, table);
            return uq;
        }

        public void Set(SearchViewState state, SearchTable table)
        {
            if (viewState == null)
                viewState = new SearchViewState();
            viewState.Assign(state);
            tableConfig = table?.Clone();
        }

        public override int GetHashCode()
        {
            return filePath.GetHashCode();
        }

        private static List<SearchQuery> s_SearchQueries;
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

        public static bool IsUserQuery(SearchQuery query)
        {
            return query.filePath.StartsWith(userSearchSettingsFolder);
        }

        public static SearchQuery AddUserQuery(SearchViewState state, SearchTable table = null)
        {
            return AddSearchQuery(userSearchSettingsFolder, state, table);
        }

        public static SearchQuery AddSearchQuery(string folder, SearchViewState state, SearchTable table = null)
        {
            var query = SearchQuery.Create(state, table);
            query.filePath = Path.Combine(folder, $"{query.guid}.query");
            s_SearchQueries.Add(query);
            SaveSearchQuery(query);
            return query;
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

        public static void SaveSearchQuery(SearchQuery query)
        {
            var folder = Path.GetDirectoryName(query.filePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var queryJson = EditorJsonUtility.ToJson(query, true);
            File.WriteAllText(query.filePath, queryJson);
        }

        public IEnumerable<string> GetProviderIds()
        {
            return viewState.GetProviderIds();
        }

        public ResultViewState GetResultViewState()
        {
            return new ResultViewState(tableConfig)
            {
                group = null,
                itemSize = viewState.itemSize
            };
        }

        public static ISearchView Open(ISearchQuery query, SearchFlags additionalFlags)
        {
            var providerIds = QuickSearch.GetMergedProviders(QuickSearch.GetCurrentSearchWindowProviders(), query.GetProviderIds()).Select(p => p.id);
            var searchWindow = QuickSearch.OpenWithContextualProvider(query.searchText, providerIds.ToArray(), additionalFlags, "Unity");
            searchWindow.ExecuteSearchQuery(query);
            return searchWindow;
        }

        public static void ShowQueryIconPicker(Action<UnityEngine.Texture2D, bool> selectIcon)
        {
            var pickIconContext = SearchService.CreateContext(new[] { "adb", "asset" }, "", SearchFlags.WantsMore);
            var viewState = new SearchViewState(pickIconContext,
                (newIcon, canceled) => selectIcon(newIcon as Texture2D, canceled),
                null,
                "Texture",
                typeof(Texture));
            viewState.title = "Query Icon";
            viewState.SetSearchViewFlags(SearchViewFlags.GridView);
            SearchService.ShowPicker(viewState);
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
