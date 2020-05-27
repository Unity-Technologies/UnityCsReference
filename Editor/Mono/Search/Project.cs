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
        public SearchEngineScope engineScope { get; protected set; } = Project.EngineScope;
        public IEnumerable<Type> requiredTypes { get; set; }
        public IEnumerable<string> requiredTypeNames { get; set; }
    }

    public interface IProjectSearchEngine : ISearchEngine<string> {}

    [InitializeOnLoad]
    public static class Project
    {
        static readonly SearchApiBaseImp<ProjectSearchEngineAttribute, IProjectSearchEngine> k_EngineImp;

        public const SearchEngineScope EngineScope = SearchEngineScope.Project;

        static Project()
        {
            k_EngineImp = new SearchApiBaseImp<ProjectSearchEngineAttribute, IProjectSearchEngine>(EngineScope, "Project");
        }

        internal static IEnumerable<string> Search(string query, ProjectSearchContext context, Action<IEnumerable<string>> asyncItemsReceived)
        {
            var activeEngine = k_EngineImp.activeSearchEngine;
            try
            {
                return activeEngine.Search(context, query, asyncItemsReceived);
            }
            catch (Exception ex)
            {
                k_EngineImp.HandleUserException(ex);
                return null;
            }
        }

        internal static bool HasEngineOverride()
        {
            return k_EngineImp.HasEngineOverride();
        }

        internal static void BeginSession(ProjectSearchContext context)
        {
            k_EngineImp.BeginSession(context);
        }

        internal static void EndSession(ProjectSearchContext context)
        {
            k_EngineImp.EndSession(context);
        }

        internal static void BeginSearch(string query, ProjectSearchContext context)
        {
            k_EngineImp.BeginSearch(query, context);
        }

        internal static void EndSearch(ProjectSearchContext context)
        {
            k_EngineImp.EndSearch(context);
        }

        internal static IProjectSearchEngine GetActiveSearchEngine()
        {
            return k_EngineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            k_EngineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(IProjectSearchEngine engine)
        {
            k_EngineImp.RegisterEngine(engine);
        }

        public static void UnregisterEngine(IProjectSearchEngine engine)
        {
            k_EngineImp.UnregisterEngine(engine);
        }
    }
}
