// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.SearchService
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProjectSearchEngineAttribute : Attribute
    {}

    public class ProjectSearchContext : ISearchContext
    {
        public Guid guid { get; } = Guid.NewGuid();
        public SearchEngineScope engineScope { get; protected set; } = ProjectSearch.EngineScope;
        public IEnumerable<Type> requiredTypes { get; set; }
        public IEnumerable<string> requiredTypeNames { get; set; }
        internal SearchFilter searchFilter { get; set; }
    }

    public interface IProjectSearchEngine : ISearchEngine<string> {}

    [InitializeOnLoad]
    public static class ProjectSearch
    {
        static SearchApiBaseImp<ProjectSearchEngineAttribute, IProjectSearchEngine> s_EngineImp;
        static SearchApiBaseImp<ProjectSearchEngineAttribute, IProjectSearchEngine> engineImp
        {
            get
            {
                if (s_EngineImp == null)
                    StaticInit();
                return s_EngineImp;
            }
        }

        public const SearchEngineScope EngineScope = SearchEngineScope.Project;

        static ProjectSearch()
        {
            EditorApplication.tick += StaticInit;
        }

        private static void StaticInit()
        {
            EditorApplication.tick -= StaticInit;
            s_EngineImp = s_EngineImp ?? new SearchApiBaseImp<ProjectSearchEngineAttribute, IProjectSearchEngine>(EngineScope, "Project");
        }

        internal static IEnumerable<string> Search(string query, ProjectSearchContext context, Action<IEnumerable<string>> asyncItemsReceived)
        {
            var activeEngine = engineImp.activeSearchEngine;
            try
            {
                return activeEngine.Search(context, query, asyncItemsReceived);
            }
            catch (Exception ex)
            {
                engineImp.HandleUserException(ex);
                return null;
            }
        }

        internal static bool HasEngineOverride()
        {
            return engineImp.HasEngineOverride();
        }

        internal static void BeginSession(ProjectSearchContext context)
        {
            engineImp.BeginSession(context);
        }

        internal static void EndSession(ProjectSearchContext context)
        {
            engineImp.EndSession(context);
        }

        internal static void BeginSearch(string query, ProjectSearchContext context)
        {
            engineImp.BeginSearch(query, context);
        }

        internal static void EndSearch(ProjectSearchContext context)
        {
            engineImp.EndSearch(context);
        }

        internal static IProjectSearchEngine GetActiveSearchEngine()
        {
            return engineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            engineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(IProjectSearchEngine engine)
        {
            engineImp.RegisterEngine(engine);
        }

        public static void UnregisterEngine(IProjectSearchEngine engine)
        {
            engineImp.UnregisterEngine(engine);
        }
    }

    class ProjectSearchSessionHandler : SearchSessionHandler
    {
        public ProjectSearchSessionHandler()
            : base(SearchEngineScope.Project) {}

        public IEnumerable<string> Search(string query, Action<IEnumerable<string>> asyncItemsReceived)
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                return ProjectSearch.Search(query, (ProjectSearchContext)context, asyncItemsReceived);
        }
    }
}
