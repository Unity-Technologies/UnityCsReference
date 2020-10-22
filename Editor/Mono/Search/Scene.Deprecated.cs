// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.SearchService
{
    [Obsolete("Scene has been deprecated. Use SceneSearch instead (UnityUpgradable) -> SceneSearch", false)]
    public static class Scene
    {
        public const SearchEngineScope EngineScope = SearchEngineScope.Scene;

        public static void RegisterEngine(ISceneSearchEngine engine)
        {
            SceneSearch.RegisterEngine(engine);
        }

        public static void UnregisterEngine(ISceneSearchEngine engine)
        {
            SceneSearch.UnregisterEngine(engine);
        }
    }
}
