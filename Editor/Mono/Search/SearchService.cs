// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Profiling;
using UnityEngine;
using Attribute = System.Attribute;
using Object = UnityEngine.Object;

namespace UnityEditor.SearchService
{
    public enum SearchEngineScope
    {
        Scene,
        Project,
        ObjectSelector
    }

    public interface ISearchContext
    {
        Guid guid { get; }
        SearchEngineScope engineScope { get; }
        IEnumerable<Type> requiredTypes { get; }
        IEnumerable<string> requiredTypeNames { get; }
    }

    public interface ISearchEngineBase
    {
        string name { get; }

        void BeginSession(ISearchContext context);
        void EndSession(ISearchContext context);

        void BeginSearch(ISearchContext context, string query);
        void EndSearch(ISearchContext context);
    }

    public interface ISearchEngine<T> : ISearchEngineBase
    {
        IEnumerable<T> Search(ISearchContext context, string query, Action<IEnumerable<T>> asyncItemsReceived);
    }

    public interface IFilterEngine<in T> : ISearchEngineBase
    {
        bool Filter(ISearchContext context, string query, T objectToFilter);
    }

    public interface ISelectorEngine : ISearchEngineBase
    {
        bool SelectObject(ISearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated);
        void SetSearchFilter(ISearchContext context, string searchFilter);
    }

    struct SearchSessionOptions
    {
        public bool legacyOnly;
        public static readonly SearchSessionOptions Default = new SearchSessionOptions();
    }

    struct SearchSessionOptionsApplicator : IDisposable
    {
        bool m_Disposed;

        ISearchApi m_Api;
        SearchSessionOptions m_Options;

        string m_CurrentActiveEngine;

        public SearchSessionOptionsApplicator(ISearchApi api, SearchSessionOptions options)
        {
            m_Api = api;
            m_Options = options;
            m_Disposed = false;
            m_CurrentActiveEngine = m_Api?.activeSearchEngineName;

            if (m_Options.legacyOnly)
                m_Api?.SetActiveSearchEngine(LegacySearchEngineBase.k_Name, false);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            if (m_Options.legacyOnly)
                m_Api?.SetActiveSearchEngine(m_CurrentActiveEngine, false);

            m_Disposed = true;
        }
    }

    class SearchSessionHandler
    {
        SearchEngineScope m_EngineScope;
        bool m_WasSearching;
        protected ISearchApi m_Api;
        protected SearchSessionOptions m_Options;

        public ISearchContext context { get; private set; }

        public SearchSessionHandler(SearchEngineScope engineScope)
        {
            m_EngineScope = engineScope;
            Init();
        }

        void Init()
        {
            var index = SearchService.searchApis.FindIndex(api => api.engineScope == m_EngineScope);
            if (index >= 0)
            {
                m_Api = SearchService.searchApis[index];
                m_Api.activeEngineChanged += OnActiveEngineChanged;
            }
        }

        void OnActiveEngineChanged(string newSearchEngineName)
        {
            EndSession();
        }

        public void BeginSession(Func<ISearchContext> searchContextCreator, SearchSessionOptions options)
        {
            if (m_WasSearching)
                return;
            // Do a lazy initialize if the apis were not available during creation
            if (m_Api == null)
            {
                Init();
                if (m_Api == null)
                    throw new NullReferenceException("SearchService Apis were not initialized properly.");
            }
            m_Options = options;
            context = searchContextCreator();
            m_WasSearching = true;
            using (new SearchSessionOptionsApplicator(m_Api, options))
                m_Api?.BeginSession(context);
        }

        public void BeginSession(Func<ISearchContext> searchContextCreator)
        {
            BeginSession(searchContextCreator, SearchSessionOptions.Default);
        }

        public void EndSession()
        {
            if (!m_WasSearching)
                return;
            m_WasSearching = false;
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                m_Api?.EndSession(context);
            context = null;
            m_Options = SearchSessionOptions.Default;
        }

        public void BeginSearch(string query)
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                m_Api?.BeginSearch(query, context);
        }

        public void EndSearch()
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                m_Api?.EndSearch(context);
        }
    }

    interface ISearchApi
    {
        SearchEngineScope engineScope { get; }
        string displayName { get; }
        IEnumerable<ISearchEngineBase> engines { get; }
        string activeSearchEngineName { get; }
        ISearchEngineBase GetActiveSearchEngine();
        void SetActiveSearchEngine(string searchEngineName, bool notify = true);
        void RegisterEngine(ISearchEngineBase engine);
        void UnregisterEngine(ISearchEngineBase engine);
        bool HasEngineOverride();
        void BeginSession(ISearchContext context);
        void EndSession(ISearchContext context);
        void BeginSearch(string query, ISearchContext context);
        void EndSearch(ISearchContext context);
        ISearchEngineBase GetDefaultEngine();
        event Action<string> activeEngineChanged;
    }

    static class SearchService
    {
        public const string keyPrefix = "searchservice";
        public const string activeSearchEnginesPrefKey = keyPrefix + ".activeengines.";
        public static List<ISearchApi> searchApis = new List<ISearchApi>();

        public enum SyncSearchEvent
        {
            StartSession,
            SyncSearch,
            EndSession
        }

        public static event Action<SyncSearchEvent, string, string> syncSearchChanged;

        static SearchService()
        {
            Build.BuildDefines.getScriptCompilationDefinesDelegates += AddSearchServiceBuildDefines;
        }

        private static void AddSearchServiceBuildDefines(BuildTarget target, HashSet<string> defines)
        {
            defines.Add("USE_SEARCH_ENGINE_API");
            defines.Add("USE_SEARCH_TABLE");
            defines.Add("USE_SEARCH_MODULE");
            defines.Add("USE_PROPERTY_DATABASE");
        }

        public static void NotifySyncSearchChanged(SyncSearchEvent evt, string syncViewId, string searchQuery)
        {
            syncSearchChanged?.Invoke(evt, syncViewId, searchQuery);
        }

        public static void HandleSearchEvent(EditorWindow window, Event evt, string searchText)
        {
            OpenSearchHelper.HandleSearchEvent(window, evt, searchText);
        }

        public static void DrawOpenSearchButton(EditorWindow window, string searchText)
        {
            OpenSearchHelper.DrawOpenButton(window, searchText);
        }
    }

    class SearchApiBaseImp<TAttribute, TEngine> : ISearchApi
        where TAttribute : Attribute
        where TEngine : class, ISearchEngineBase
    {
        int m_SearchPerformanceTrackerHandle;

        public SearchEngineScope engineScope { get; }
        public string displayName { get; }

        List<TEngine> engines { get; } = new List<TEngine>();

        IEnumerable<ISearchEngineBase> ISearchApi.engines => engines.Cast<ISearchEngineBase>();

        public string activeSearchEngineName { get; private set; }

        TEngine m_ActiveSearchEngine;
        public TEngine activeSearchEngine
        {
            get
            {
                if (m_ActiveSearchEngine == null)
                    m_ActiveSearchEngine = GetActiveSearchEngine();
                return m_ActiveSearchEngine;
            }
            private set => m_ActiveSearchEngine = value;
        }

        public event Action<string> activeEngineChanged;

        public SearchApiBaseImp(SearchEngineScope engineScope, string displayName)
        {
            this.engineScope = engineScope;
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            RegisterAllEngines();
            LoadActiveSearchEngine();
            SearchService.searchApis.Add(this);
        }

        public TEngine GetActiveSearchEngine()
        {
            if (engines.Count == 0)
                return null;
            var actualActiveEngineName = string.IsNullOrEmpty(activeSearchEngineName) ? GetDefaultEngine().name : activeSearchEngineName;
            var activeEngineIndex = engines.FindIndex(engine => engine.name == actualActiveEngineName);
            if (activeEngineIndex < 0)
            {
                var defaultEngine = GetDefaultEngine() ?? engines.First();
                SetActiveSearchEngine(defaultEngine.name);
                return defaultEngine;
            }
            return engines[activeEngineIndex];
        }

        ISearchEngineBase ISearchApi.GetActiveSearchEngine()
        {
            return GetActiveSearchEngine();
        }

        public TEngine GetDefaultEngine()
        {
            if (engines.Count == 0)
                return null;

            var index = engines.FindIndex(engine => engine is LegacySearchEngineBase);
            if (index < 0)
                return null;
            return engines[index];
        }

        ISearchEngineBase ISearchApi.GetDefaultEngine()
        {
            return GetDefaultEngine();
        }

        public void SetActiveSearchEngine(string searchEngineName, bool notify = true)
        {
            if (string.Equals(activeSearchEngineName, searchEngineName, StringComparison.Ordinal))
                return;

            if (notify)
                activeEngineChanged?.Invoke(searchEngineName);
            activeSearchEngineName = searchEngineName;
            EditorPrefs.SetString(SearchService.activeSearchEnginesPrefKey + engineScope, searchEngineName);
            activeSearchEngine = null;
        }

        void ISearchApi.SetActiveSearchEngine(string searchEngineName, bool notify)
        {
            SetActiveSearchEngine(searchEngineName, notify);
        }

        public bool HasEngineOverride()
        {
            return !(activeSearchEngine is LegacySearchEngineBase);
        }

        public void BeginSession(ISearchContext context)
        {
            try
            {
                activeSearchEngine.BeginSession(context);
            }
            catch (Exception ex)
            {
                HandleUserException(ex);
            }
        }

        public void EndSession(ISearchContext context)
        {
            try
            {
                activeSearchEngine.EndSession(context);
            }
            catch (Exception ex)
            {
                HandleUserException(ex);
            }
        }

        public void BeginSearch(string query, ISearchContext context)
        {
            m_SearchPerformanceTrackerHandle = EditorPerformanceTracker.StartTracker($"Search Engine {activeSearchEngineName} ({displayName})");
            try
            {
                activeSearchEngine.BeginSearch(context, query);
            }
            catch (Exception ex)
            {
                HandleUserException(ex);
            }
        }

        public void EndSearch(ISearchContext context)
        {
            try
            {
                activeSearchEngine.EndSearch(context);
            }
            catch (Exception ex)
            {
                HandleUserException(ex);
            }
            if (EditorPerformanceTracker.IsTrackerActive(m_SearchPerformanceTrackerHandle))
                EditorPerformanceTracker.StopTracker(m_SearchPerformanceTrackerHandle);
        }

        void LoadActiveSearchEngine()
        {
            activeSearchEngineName = EditorPrefs.GetString(SearchService.activeSearchEnginesPrefKey + engineScope, GetDefaultEngine().name);
            m_ActiveSearchEngine = GetActiveSearchEngine();
        }

        public void RegisterEngine(TEngine engine)
        {
            if (engine != null && engines.FindIndex(e => e.name == engine.name) < 0)
                engines.Add(engine);
        }

        public void UnregisterEngine(TEngine engine)
        {
            if (engine == null)
                return;
            var index = engines.FindIndex(e => e.name == engine.name);
            if (index >= 0)
                engines.RemoveAt(index);
        }

        void ISearchApi.RegisterEngine(ISearchEngineBase engine)
        {
            RegisterEngine(engine as TEngine);
        }

        void ISearchApi.UnregisterEngine(ISearchEngineBase engine)
        {
            UnregisterEngine(engine as TEngine);
        }

        void RegisterAllEngines()
        {
            var types = TypeCache.GetTypesWithAttribute<TAttribute>();
            var instantiatedEngines = types.Select(type => Activator.CreateInstance(type));
            foreach (var instantiatedEngine in instantiatedEngines)
            {
                if (instantiatedEngine is TEngine typedEngine)
                {
                    RegisterEngine(typedEngine);
                }
                else
                {
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null,
                        $"Trying to register a search engine with the attribute {typeof(TAttribute)} but it does not implement the interface {typeof(TEngine)}.");
                }
            }
        }

        public void HandleUserException(Exception ex)
        {
            if (activeSearchEngine is LegacySearchEngineBase)
                throw ex;

            Debug.LogFormat(LogType.Error, LogOption.None, null, $"Exception caught with search engine ({displayName}){activeSearchEngine.name}:\n{ex}");
            Debug.LogFormat(LogType.Error, LogOption.None, null, $"Setting {GetDefaultEngine().name} engine as active.");

            SetActiveSearchEngine(GetDefaultEngine().name);
        }
    }
}
