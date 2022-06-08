// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.SearchService
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ObjectSelectorEngineAttribute : Attribute
    {}

    [Flags]
    public enum VisibleObjects
    {
        None = 0,
        Assets = 1,
        Scene = 1 << 1,
        All = Assets | Scene
    }

    public partial class ObjectSelectorSearchContext : ISearchContext
    {
        public Guid guid { get; } = Guid.NewGuid();
        public SearchEngineScope engineScope { get; protected set; } = ObjectSelectorSearch.EngineScope;
        public Object currentObject { get; set; }
        public Object[] editedObjects { get; set; }
        public IEnumerable<Type> requiredTypes { get; set; }
        public IEnumerable<string> requiredTypeNames { get; set; }
        public VisibleObjects visibleObjects { get; set; }
        public IEnumerable<int> allowedInstanceIds { get; set; }
        internal SearchFilter searchFilter { get; set; }
    }

    public interface IObjectSelectorEngine : ISelectorEngine {}

    [InitializeOnLoad]
    public static class ObjectSelectorSearch
    {
        static SearchApiBaseImp<ObjectSelectorEngineAttribute, IObjectSelectorEngine> s_EngineImp;
        static SearchApiBaseImp<ObjectSelectorEngineAttribute, IObjectSelectorEngine> engineImp
        {
            get
            {
                if (s_EngineImp == null)
                    StaticInit();
                return s_EngineImp;
            }
        }

        public const SearchEngineScope EngineScope = SearchEngineScope.ObjectSelector;

        static ObjectSelectorSearch()
        {
            EditorApplication.tick += StaticInit;
        }

        private static void StaticInit()
        {
            EditorApplication.tick -= StaticInit;
            s_EngineImp = s_EngineImp ?? new SearchApiBaseImp<ObjectSelectorEngineAttribute, IObjectSelectorEngine>(EngineScope, "Object Selector");
        }

        internal static bool SelectObject(ObjectSelectorSearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated)
        {
            var activeEngine = engineImp.activeSearchEngine;
            try
            {
                return activeEngine.SelectObject(context, onObjectSelectorClosed, onObjectSelectedUpdated);
            }
            catch (Exception ex)
            {
                engineImp.HandleUserException(ex);
                return false;
            }
        }

        internal static void SetSearchFilter(string searchFilter, ObjectSelectorSearchContext context)
        {
            var activeEngine = engineImp.activeSearchEngine;
            activeEngine.SetSearchFilter(context, searchFilter);
        }

        internal static bool HasEngineOverride()
        {
            return engineImp.HasEngineOverride();
        }

        internal static void BeginSession(ObjectSelectorSearchContext context)
        {
            engineImp.BeginSession(context);
        }

        internal static void EndSession(ObjectSelectorSearchContext context)
        {
            engineImp.EndSession(context);
        }

        internal static void BeginSearch(string query, ObjectSelectorSearchContext context)
        {
            engineImp.BeginSearch(query, context);
        }

        internal static void EndSearch(ObjectSelectorSearchContext context)
        {
            engineImp.EndSearch(context);
        }

        internal static IObjectSelectorEngine GetActiveSearchEngine()
        {
            return engineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            engineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(IObjectSelectorEngine engine)
        {
            engineImp.RegisterEngine(engine);
        }

        public static void UnregisterEngine(IObjectSelectorEngine engine)
        {
            engineImp.UnregisterEngine(engine);
        }
    }

    class ObjectSelectorSearchSessionHandler : SearchSessionHandler
    {
        public ObjectSelectorSearchSessionHandler()
            : base(SearchEngineScope.ObjectSelector) {}

        public bool SelectObject(Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated)
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                return ObjectSelectorSearch.SelectObject((ObjectSelectorSearchContext)context, onObjectSelectorClosed, onObjectSelectedUpdated);
        }

        public void SetSearchFilter(string searchFilter)
        {
            using (new SearchSessionOptionsApplicator(m_Api, m_Options))
                ObjectSelectorSearch.SetSearchFilter(searchFilter, (ObjectSelectorSearchContext)context);
        }
    }
}
