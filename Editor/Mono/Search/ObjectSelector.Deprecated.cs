// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.SearchService
{
    [Obsolete("ObjectSelector has been deprecated. Use ObjectSelectorSearch instead (UnityUpgradable) -> ObjectSelectorSearch", false)]
    public static class ObjectSelector
    {
        public const SearchEngineScope EngineScope = SearchEngineScope.ObjectSelector;

        public static void RegisterEngine(IObjectSelectorEngine engine)
        {
            ObjectSelectorSearch.RegisterEngine(engine);
        }

        public static void UnregisterEngine(IObjectSelectorEngine engine)
        {
            ObjectSelectorSearch.UnregisterEngine(engine);
        }
    }
}
