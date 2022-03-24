// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.SearchService
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SceneSearchEngineAttribute : Attribute
    {}

    public class SceneSearchContext : ISearchContext
    {
        public Guid guid { get; } = Guid.NewGuid();
        public SearchEngineScope engineScope { get; protected set; } = SceneSearch.EngineScope;
        public IEnumerable<Type> requiredTypes { get; set; }
        public IEnumerable<string> requiredTypeNames { get; set; }
        public HierarchyProperty rootProperty { get; set; }
    }

    public interface ISceneSearchEngine : IFilterEngine<HierarchyProperty>
    {}

    [InitializeOnLoad]
    public static class SceneSearch
    {
        static SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngine> s_EngineImp;
        static SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngine> engineImp
        {
            get
            {
                if (s_EngineImp == null)
                    StaticInit();
                return s_EngineImp;
            }
        }

        public const SearchEngineScope EngineScope = SearchEngineScope.Scene;

        static SceneSearch()
        {
            EditorApplication.tick += StaticInit;
        }

        private static void StaticInit()
        {
            EditorApplication.tick -= StaticInit;
            s_EngineImp = s_EngineImp ?? new SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngine>(EngineScope, "Scene");
        }

        internal static bool Filter(string query, HierarchyProperty objectToFilter, SceneSearchContext context)
        {
            var activeEngine = engineImp.activeSearchEngine;
            try
            {
                return activeEngine.Filter(context, query, objectToFilter);
            }
            catch (Exception ex)
            {
                engineImp.HandleUserException(ex);
                return false;
            }
        }

        internal static bool HasEngineOverride()
        {
            return engineImp.HasEngineOverride();
        }

        internal static void BeginSession(SceneSearchContext context)
        {
            engineImp.BeginSession(context);
        }

        internal static void EndSession(SceneSearchContext context)
        {
            engineImp.EndSession(context);
        }

        internal static void BeginSearch(string query, SceneSearchContext context)
        {
            engineImp.BeginSearch(query, context);
        }

        internal static void EndSearch(SceneSearchContext context)
        {
            engineImp.EndSearch(context);
        }

        internal static ISceneSearchEngine GetActiveSearchEngine()
        {
            return engineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            engineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(ISceneSearchEngine engine)
        {
            engineImp.RegisterEngine(engine);
        }

        public static void UnregisterEngine(ISceneSearchEngine engine)
        {
            engineImp.UnregisterEngine(engine);
        }
    }

    class SceneSearchSessionHandler : SearchSessionHandler
    {
        public SceneSearchSessionHandler()
            : base(SearchEngineScope.Scene) {}

        public bool Filter(string query, HierarchyProperty objectToFilter)
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                return SceneSearch.Filter(query, objectToFilter, (SceneSearchContext)context);
        }
    }
}
