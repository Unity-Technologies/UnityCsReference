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
        [Obsolete("Obsolete. Use rootIterator instead.", true)]
        public HierarchyProperty rootProperty { get => HierarchyProperty.UnsafeCastFrom(rootIterator); set => rootIterator = HierarchyIterator.UnsafeCastFrom(value); }
        public HierarchyIterator rootIterator { get; set; }
        internal SearchFilter searchFilter { get; set; }
    }

    [Obsolete("ISceneSearchEngine is deprecated. Use ISceneSearchEngineV2 instead.", true)]
    public interface ISceneSearchEngine : IFilterEngine<HierarchyProperty>
    {}

    public interface ISceneSearchEngineV2 : IFilterEngine<HierarchyIterator>
    {}

    [Obsolete]
    internal class SearchEngineInterop : ISceneSearchEngineV2
    {
        ISceneSearchEngine m_Engine;
        public SearchEngineInterop(ISceneSearchEngine engine) => m_Engine = engine;
        public bool Filter(ISearchContext context, string query, HierarchyIterator objectToFilter) => m_Engine.Filter(context, query, HierarchyProperty.UnsafeCastFrom(objectToFilter));
        public string name => m_Engine.name;
        public void BeginSession(ISearchContext context) => m_Engine.BeginSession(context);
        public void EndSession(ISearchContext context) => m_Engine.EndSession(context);
        public void BeginSearch(ISearchContext context, string query) => m_Engine.BeginSearch(context, query);
        public void EndSearch(ISearchContext context) => m_Engine.EndSearch(context);
    }

    [InitializeOnLoad]
    public static class SceneSearch
    {
        static SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngineV2> s_EngineImp;
        static SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngineV2> engineImp
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
            s_EngineImp = s_EngineImp ?? new SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngineV2>(EngineScope, "Scene");
        }

        internal static bool Filter(string query, HierarchyIterator objectToFilter, SceneSearchContext context)
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

        internal static ISceneSearchEngineV2 GetActiveSearchEngine()
        {
            return engineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            engineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(ISceneSearchEngineV2 engine) => engineImp.RegisterEngine(engine);
        [Obsolete("RegisterEngine is deprecated. Use RegisterEngine(ISceneSearchEngineV2 engine) instead.", true)]
        public static void RegisterEngine(ISceneSearchEngine engine) => engineImp.RegisterEngine(new SearchEngineInterop(engine));

        public static void UnregisterEngine(ISceneSearchEngineV2 engine) => engineImp.UnregisterEngine(engine);
        [Obsolete("UnregisterEngine is deprecated. Use UnregisterEngine(ISceneSearchEngineV2 engine) instead.", true)]
        public static void UnregisterEngine(ISceneSearchEngine engine) => engineImp.UnregisterEngine(new SearchEngineInterop(engine)); // works because name is used to find the engine to unregister, instead of actual object reference
    }

    class SceneSearchSessionHandler : SearchSessionHandler
    {
        public SceneSearchSessionHandler()
            : base(SearchEngineScope.Scene) {}

        public bool Filter(string query, HierarchyIterator objectToFilter)
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                return SceneSearch.Filter(query, objectToFilter, (SceneSearchContext)context);
        }
    }
}
