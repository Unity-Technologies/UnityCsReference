// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Attribute = System.Attribute;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public static class SearchService
    {
        public interface ISearchContext
        {
            Guid guid { get; }
            string searchType { get; }
            IEnumerable<Type> requiredTypes { get; }
            IEnumerable<string> requiredTypeNames { get; }
        }

        public interface ISearchEngineBase
        {
            string id { get; }
            string displayName { get; }

            void BeginSession(ISearchContext context);
            void EndSession(ISearchContext context);

            void BeginSearch(string query, ISearchContext context);
            void EndSearch(ISearchContext context);
        }

        public interface ISearchEngine<T> : ISearchEngineBase
        {
            IEnumerable<T> Search(string query, ISearchContext context, Action<IEnumerable<T>> asyncItemsReceived);
        }

        public interface IFilterEngine<in T> : ISearchEngineBase
        {
            bool Filter(string query, T objectToFilter, ISearchContext context);
        }

        public interface ISelectorEngine : ISearchEngineBase
        {
            bool SelectObject(ISearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated);
        }

        internal class SearchSessionHandler
        {
            bool m_WasSearching;
            readonly ISearchApi m_Api;

            public ISearchContext context { get; private set; }

            public SearchSessionHandler(string searchType)
            {
                var index = s_SearchApis.FindIndex(api => api.id == searchType);
                if (index >= 0)
                {
                    m_Api = s_SearchApis[index];
                    m_Api.activeEngineChanged += OnActiveEngineChanged;
                }
            }

            void OnActiveEngineChanged(string newSearchEngineId)
            {
                EndSession();
            }

            public void BeginSession(Func<ISearchContext> searchContextCreator)
            {
                if (m_WasSearching)
                    return;
                context = searchContextCreator();
                m_WasSearching = true;
                m_Api?.BeginSession(context);
            }

            public void EndSession()
            {
                if (!m_WasSearching)
                    return;
                m_WasSearching = false;
                m_Api?.EndSession(context);
                context = null;
            }

            public void BeginSearch(string query)
            {
                m_Api?.BeginSearch(query, context);
            }

            public void EndSearch()
            {
                m_Api?.EndSearch(context);
            }
        }

        internal interface ISearchApi
        {
            string id { get; }
            string displayName { get; }
            IEnumerable<ISearchEngineBase> engines { get; }
            string activeSearchEngineId { get; }
            ISearchEngineBase GetActiveSearchEngine();
            void SetActiveSearchEngine(string searchEngineId);
            void RegisterEngine(ISearchEngineBase engine);
            void UnregisterEngine(ISearchEngineBase engine);
            bool HasEngineOverride();
            void BeginSession(ISearchContext context);
            void EndSession(ISearchContext context);
            void BeginSearch(string query, ISearchContext context);
            void EndSearch(ISearchContext context);
            event Action<string> activeEngineChanged;
        }

        const string k_KeyPrefix = "searchservice";
        const string k_ActiveSearchEnginesPrefKey = k_KeyPrefix + ".activeengines.";

        static List<ISearchApi> s_SearchApis = new List<ISearchApi>();

        class SearchApiBaseImp<TAttribute, TEngine> : ISearchApi
            where TAttribute : Attribute
            where TEngine : class, ISearchEngineBase
        {
            public string id { get; }
            public string displayName { get; }

            List<TEngine> engines { get; } = new List<TEngine>();

            IEnumerable<ISearchEngineBase> ISearchApi.engines => engines.Cast<ISearchEngineBase>();

            public string activeSearchEngineId { get; private set; }

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

            public SearchApiBaseImp(string id, string displayName = null)
            {
                this.id = id;
                this.displayName = string.IsNullOrEmpty(displayName) ? id.ToPascalCase() : displayName;
                RegisterAllEngines();
                LoadActiveSearchEngine();
                s_SearchApis.Add(this);
            }

            public TEngine GetActiveSearchEngine()
            {
                if (engines.Count == 0)
                    return null;
                var activeEngineName = string.IsNullOrEmpty(activeSearchEngineId) ? DefaultSearchEngineBase.engineId : activeSearchEngineId;
                var activeEngineIndex = engines.FindIndex(engine => engine.id == activeEngineName);
                if (activeEngineIndex < 0)
                {
                    var defaultEngine = GetDefaultEngine() ?? engines.First();
                    SetActiveSearchEngine(defaultEngine.id);
                    return defaultEngine;
                }
                return engines[activeEngineIndex];
            }

            ISearchEngineBase ISearchApi.GetActiveSearchEngine()
            {
                return GetActiveSearchEngine();
            }

            TEngine GetDefaultEngine()
            {
                if (engines.Count == 0)
                    return null;

                var index = engines.FindIndex(engine => engine is DefaultSearchEngineBase);
                if (index < 0)
                    return null;
                return engines[index];
            }

            public void SetActiveSearchEngine(string searchEngineId)
            {
                activeEngineChanged?.Invoke(searchEngineId);
                activeSearchEngineId = searchEngineId;
                EditorPrefs.SetString(k_ActiveSearchEnginesPrefKey + id, searchEngineId);
                activeSearchEngine = null;
            }

            void ISearchApi.SetActiveSearchEngine(string searchEngineId)
            {
                SetActiveSearchEngine(searchEngineId);
            }

            public bool HasEngineOverride()
            {
                var activeEngine = GetActiveSearchEngine();
                return !(activeEngine is DefaultSearchEngineBase);
            }

            public void BeginSession(ISearchContext context)
            {
                activeSearchEngine.BeginSession(context);
            }

            public void EndSession(ISearchContext context)
            {
                activeSearchEngine.EndSession(context);
            }

            public void BeginSearch(string query, ISearchContext context)
            {
                activeSearchEngine.BeginSearch(query, context);
            }

            public void EndSearch(ISearchContext context)
            {
                activeSearchEngine.EndSearch(context);
            }

            void LoadActiveSearchEngine()
            {
                activeSearchEngineId = EditorPrefs.GetString(k_ActiveSearchEnginesPrefKey + id, DefaultSearchEngineBase.engineId);
            }

            public void RegisterEngine(TEngine engine)
            {
                if (engine != null && engines.FindIndex(e => e.id == engine.id) < 0)
                    engines.Add(engine);
            }

            public void UnregisterEngine(TEngine engine)
            {
                if (engine == null)
                    return;
                var index = engines.FindIndex(e => e.id == engine.id);
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
        }

        [InitializeOnLoad]
        public static class Scene
        {
            public class EngineAttribute : Attribute
            {}

            public class SearchContext : ISearchContext
            {
                public Guid guid { get; } = Guid.NewGuid();
                public string searchType { get; protected set; } = Scene.searchType;
                public IEnumerable<Type> requiredTypes { get; set; }
                public IEnumerable<string> requiredTypeNames { get; set; }
                public HierarchyProperty rootProperty { get; set; }
            }

            public interface IEngine : IFilterEngine<HierarchyProperty> {}

            static readonly SearchApiBaseImp<EngineAttribute, IEngine> k_EngineImp;

            public const string searchType = "scene";

            static Scene()
            {
                k_EngineImp = new SearchApiBaseImp<EngineAttribute, IEngine>(searchType);
            }

            internal static bool Filter(string query, HierarchyProperty objectToFilter, SearchContext context)
            {
                var activeEngine = k_EngineImp.activeSearchEngine;
                return activeEngine.Filter(query, objectToFilter, context);
            }

            internal static bool HasEngineOverride()
            {
                return k_EngineImp.HasEngineOverride();
            }

            internal static void BeginSession(SearchContext context)
            {
                k_EngineImp.BeginSession(context);
            }

            internal static void EndSession(SearchContext context)
            {
                k_EngineImp.EndSession(context);
            }

            internal static void BeginSearch(string query, SearchContext context)
            {
                k_EngineImp.BeginSearch(query, context);
            }

            internal static void EndSearch(SearchContext context)
            {
                k_EngineImp.EndSearch(context);
            }

            internal static IEngine GetActiveSearchEngine()
            {
                return k_EngineImp.GetActiveSearchEngine();
            }

            internal static void SetActiveSearchEngine(string searchEngineId)
            {
                k_EngineImp.SetActiveSearchEngine(searchEngineId);
            }

            public static void RegisterEngine(IEngine engine)
            {
                k_EngineImp.RegisterEngine(engine);
            }

            public static void UnregisterEngine(IEngine engine)
            {
                k_EngineImp.UnregisterEngine(engine);
            }
        }

        [InitializeOnLoad]
        public static class Project
        {
            public class EngineAttribute : Attribute
            {}

            public class SearchContext : ISearchContext
            {
                public Guid guid { get; } = Guid.NewGuid();
                public string searchType { get; protected set; } = Project.searchType;
                public IEnumerable<Type> requiredTypes { get; set; }
                public IEnumerable<string> requiredTypeNames { get; set; }
            }

            public interface IEngine : ISearchEngine<string> {}

            static readonly SearchApiBaseImp<EngineAttribute, IEngine> k_EngineImp;

            public const string searchType = "project";

            static Project()
            {
                k_EngineImp = new SearchApiBaseImp<EngineAttribute, IEngine>(searchType);
            }

            internal static IEnumerable<string> Search(string query, SearchContext context, Action<IEnumerable<string>> asyncItemsReceived)
            {
                var activeEngine = k_EngineImp.activeSearchEngine;
                return activeEngine.Search(query, context, asyncItemsReceived);
            }

            internal static bool HasEngineOverride()
            {
                return k_EngineImp.HasEngineOverride();
            }

            internal static void BeginSession(SearchContext context)
            {
                k_EngineImp.BeginSession(context);
            }

            internal static void EndSession(SearchContext context)
            {
                k_EngineImp.EndSession(context);
            }

            internal static void BeginSearch(string query, SearchContext context)
            {
                k_EngineImp.BeginSearch(query, context);
            }

            internal static void EndSearch(SearchContext context)
            {
                k_EngineImp.EndSearch(context);
            }

            internal static IEngine GetActiveSearchEngine()
            {
                return k_EngineImp.GetActiveSearchEngine();
            }

            internal static void SetActiveSearchEngine(string searchEngineId)
            {
                k_EngineImp.SetActiveSearchEngine(searchEngineId);
            }

            public static void RegisterEngine(IEngine engine)
            {
                k_EngineImp.RegisterEngine(engine);
            }

            public static void UnregisterEngine(IEngine engine)
            {
                k_EngineImp.UnregisterEngine(engine);
            }
        }

        [InitializeOnLoad]
        public static class ObjectSelector
        {
            public class EngineAttribute : Attribute
            {}

            [Flags]
            public enum ShowedTypes
            {
                Assets = 1,
                Scene  = 1 << 1,
                All    = Assets | Scene
            }

            public class SearchContext : ISearchContext
            {
                public Guid guid { get; } = Guid.NewGuid();
                public string searchType { get; protected set; } = ObjectSelector.searchType;
                public Object currentObject { get; set; }
                public Object editedObject { get; set; }
                public IEnumerable<Type> requiredTypes { get; set; }
                public IEnumerable<string> requiredTypeNames { get; set; }
                public ShowedTypes showedTypes { get; set; }
                public IEnumerable<int> allowedInstanceIds { get; set; }
            }

            public interface IEngine : ISelectorEngine {}

            static readonly SearchApiBaseImp<EngineAttribute, IEngine> k_EngineImp;

            public const string searchType = "object-selector";

            static ObjectSelector()
            {
                k_EngineImp = new SearchApiBaseImp<EngineAttribute, IEngine>(searchType, "Object Selector");
            }

            internal static bool SelectObject(SearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated)
            {
                var activeEngine = k_EngineImp.activeSearchEngine;
                return activeEngine.SelectObject(context, onObjectSelectorClosed, onObjectSelectedUpdated);
            }

            internal static bool HasEngineOverride()
            {
                return k_EngineImp.HasEngineOverride();
            }

            internal static void BeginSession(SearchContext context)
            {
                k_EngineImp.BeginSession(context);
            }

            internal static void EndSession(SearchContext context)
            {
                k_EngineImp.EndSession(context);
            }

            internal static void BeginSearch(string query, SearchContext context)
            {
                k_EngineImp.BeginSearch(query, context);
            }

            internal static void EndSearch(SearchContext context)
            {
                k_EngineImp.EndSearch(context);
            }

            internal static IEngine GetActiveSearchEngine()
            {
                return k_EngineImp.GetActiveSearchEngine();
            }

            internal static void SetActiveSearchEngine(string searchEngineId)
            {
                k_EngineImp.SetActiveSearchEngine(searchEngineId);
            }

            public static void RegisterEngine(IEngine engine)
            {
                k_EngineImp.RegisterEngine(engine);
            }

            public static void UnregisterEngine(IEngine engine)
            {
                k_EngineImp.UnregisterEngine(engine);
            }
        }

        internal static List<ISearchApi> GetSearchApis()
        {
            return s_SearchApis;
        }
    }
}
