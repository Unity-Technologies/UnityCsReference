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
        public SearchEngineScope engineScope { get; protected set; } = Scene.EngineScope;
        public IEnumerable<Type> requiredTypes { get; set; }
        public IEnumerable<string> requiredTypeNames { get; set; }
        public HierarchyProperty rootProperty { get; set; }
    }

    public interface ISceneSearchEngine : IFilterEngine<HierarchyProperty>
    {}

    [InitializeOnLoad]
    public static class Scene
    {
        static readonly SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngine> k_EngineImp;

        public const SearchEngineScope EngineScope = SearchEngineScope.Scene;

        static Scene()
        {
            k_EngineImp = new SearchApiBaseImp<SceneSearchEngineAttribute, ISceneSearchEngine>(EngineScope, "Scene");
        }

        internal static bool Filter(string query, HierarchyProperty objectToFilter, SceneSearchContext context)
        {
            var activeEngine = k_EngineImp.activeSearchEngine;
            try
            {
                return activeEngine.Filter(context, query, objectToFilter);
            }
            catch (Exception ex)
            {
                k_EngineImp.HandleUserException(ex);
                return false;
            }
        }

        internal static bool HasEngineOverride()
        {
            return k_EngineImp.HasEngineOverride();
        }

        internal static void BeginSession(SceneSearchContext context)
        {
            k_EngineImp.BeginSession(context);
        }

        internal static void EndSession(SceneSearchContext context)
        {
            k_EngineImp.EndSession(context);
        }

        internal static void BeginSearch(string query, SceneSearchContext context)
        {
            k_EngineImp.BeginSearch(query, context);
        }

        internal static void EndSearch(SceneSearchContext context)
        {
            k_EngineImp.EndSearch(context);
        }

        internal static ISceneSearchEngine GetActiveSearchEngine()
        {
            return k_EngineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            k_EngineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(ISceneSearchEngine engine)
        {
            k_EngineImp.RegisterEngine(engine);
        }

        public static void UnregisterEngine(ISceneSearchEngine engine)
        {
            k_EngineImp.UnregisterEngine(engine);
        }
    }
}
