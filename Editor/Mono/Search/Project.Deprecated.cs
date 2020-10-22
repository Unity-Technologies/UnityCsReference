// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.SearchService
{
    [Obsolete("Project has been deprecated. Use ProjectSearch instead (UnityUpgradable) -> ProjectSearch", false)]
    public static class Project
    {
        public const SearchEngineScope EngineScope = SearchEngineScope.Project;

        public static void RegisterEngine(IProjectSearchEngine engine)
        {
            ProjectSearch.RegisterEngine(engine);
        }

        public static void UnregisterEngine(IProjectSearchEngine engine)
        {
            ProjectSearch.UnregisterEngine(engine);
        }
    }
}
