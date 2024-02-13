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
    public interface ISearchQuery
    {
        string searchText { get; }
        string displayName { get; set; }
        string details { get; set; }
        Texture2D thumbnail { get; set; }
        string filePath { get; }
        string guid { get; }
        long creationTime { get; }
        long lastUsedTime { get; }
        int itemCount { get; }
        bool isSearchTemplate { get; }

        string GetName();
        SearchTable GetSearchTable();
        SearchViewState GetViewState();
        IEnumerable<string> GetProviderIds();
        IEnumerable<string> GetProviderTypes();
    }

    enum SearchQuerySortOrder
    {
        AToZ,
        ZToA,
        CreationTime,
        MostRecentlyUsed,
        ItemCount
    }

    [Serializable]
    class SearchQuery : ISearchQuery
    {
        internal const string k_LastUsedTimePropertyName = "LastUsedTime";
        internal const string k_QueryItemsNumberPropertyName = "TotalQueryItemsNumber";

        public static string userSearchSettingsFolder => Utils.CleanPath(Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Search"));
        public string searchText
        {
            get
            {
                return viewState.text;
            }
            set
            {
                viewState.text = value;
            }
        }

        private static List<SearchQuery> s_SearchQueries;
        [SerializeField] private string m_GUID;
        [SerializeField] Texture2D m_Thumbnail;
        [SerializeField] bool m_IsSearchTemplate;
        private long m_CreationTime;
        private long m_LastUsedTime = 0;
        private int m_ItemCount = -1;

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

        public long lastUsedTime
        {
            get
            {
                using (var view = SearchMonitor.GetView())
                {
                    var recordKey = PropertyDatabase.CreateRecordKey(guid, k_LastUsedTimePropertyName);
                    if (view.TryLoadProperty(recordKey, out object data))
                        m_LastUsedTime = (long)data;
                }
                return m_LastUsedTime;
            }
        }

        public int itemCount
        {
            get
            {
                using (var view = SearchMonitor.GetView())
                {
                    var recordKey = PropertyDatabase.CreateRecordKey(guid, k_QueryItemsNumberPropertyName);
                    if (view.TryLoadProperty(recordKey, out object data))
                        m_ItemCount = (int)data;
                }

                return m_ItemCount;
            }
        }

        public static List<SearchQuery> searchQueries
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

        public SearchQuery(SearchViewState state)
            : this()
        {
            Set(state);
            name = description = Utils.Simplify(state.context.searchText);
        }

        public string GetName()
        {
            return name;
        }

        public void Set(SearchViewState state)
        {
            if (viewState == null)
                viewState = new SearchViewState();
            viewState.Assign(state, new SearchContext(state.context));
        }

        public IEnumerable<string> GetProviderIds()
        {
            return viewState.GetProviderIds();
        }

        public IEnumerable<string> GetProviderTypes()
        {
            return viewState.GetProviderTypes();
        }

        public SearchViewState GetViewState()
        {
            return viewState;
        }

        public SearchTable GetSearchTable()
        {
            return viewState?.tableConfig;
        }

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(filePath) ? filePath.GetHashCode() : m_GUID.GetHashCode();
        }

        public override string ToString()
        {
            return $"{searchText}";
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
            var query = new SearchQuery(state);
            query.filePath = Path.Combine(folder, $"{query.guid}.query");
            searchQueries.Add(query);
            SaveSearchQuery(query);
            Dispatcher.Emit(SearchEvent.UserQueryAdded, new SearchEventPayload(state, query));
            return query;
        }

        public static void SaveSearchQuery(SearchQuery query)
        {
            var folder = Path.GetDirectoryName(query.filePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var queryJson = EditorJsonUtility.ToJson(query, true);
            Utils.WriteTextFileToDisk(query.filePath, queryJson);
            Dispatcher.Emit(SearchEvent.SearchQueryChanged, new SearchEventPayload(query.viewState, query));
        }

        public static void RemoveSearchQuery(SearchQuery query)
        {
            var index = searchQueries.IndexOf(query);
            if (index != -1)
            {
                searchQueries.RemoveAt(index);
                if (File.Exists(query.filePath))
                    File.Delete(query.filePath);
                Dispatcher.Emit(SearchEvent.UserQueryRemoved, new SearchEventPayload(query.viewState));
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
            var viewState = new SearchViewState();
            viewState.Assign(query.GetViewState());
            viewState.text = query.searchText;
            viewState.activeQuery = query;
            viewState.searchFlags |= additionalFlags;
            viewState.searchFlags &= ~SearchFlags.ReuseExistingWindow;
            if (viewState.context != null)
                viewState.context.options = viewState.searchFlags;
            else
                viewState.BuildContext();
            return SearchService.ShowWindow(viewState);
        }

        public static void ShowQueryIconPicker(Action<Texture2D, bool> selectIcon)
        {
            var pickIconContext = SearchService.CreateContext(new[] { "adb", "asset" }, "", SearchFlags.WantsMore);
            var viewState = SearchViewState.CreatePickerState("Query Icon", pickIconContext,
                (newIcon, canceled) => selectIcon(newIcon as Texture2D, canceled),
                null,
                "Texture",
                typeof(Texture2D));
            SearchService.ShowPicker(viewState);
        }

        public static Texture2D GetIcon(ISearchQuery query)
        {
            if (query.thumbnail)
                return query.thumbnail;
            var displayMode = SearchUtils.GetDisplayModeFromItemSize(query.GetViewState().itemSize);
            return SearchUtils.GetIconFromDisplayMode(displayMode);
        }

        internal static void SaveLastUsedTimeToPropertyDatabase(ISearchQuery activeQuery)
        {
            if (activeQuery == null)
                return;

            using (var view = SearchMonitor.GetView())
            {
                var recordKey = PropertyDatabase.CreateRecordKey(activeQuery.guid, k_LastUsedTimePropertyName);
                view.StoreProperty(recordKey, DateTime.Now.Ticks);
            }
        }

        internal static long GetLastUsedTime(ISearchQuery query)
        {
            using (var view = SearchMonitor.GetView())
            {
                var recordKey = PropertyDatabase.CreateRecordKey(query.guid, k_LastUsedTimePropertyName);
                if (view.TryLoadProperty(recordKey, out object data))
                    return (long)data;
            }
            return 0;
        }
    }
}
