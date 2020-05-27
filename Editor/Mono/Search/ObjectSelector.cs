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

    [AttributeUsage(AttributeTargets.Method)]
    public class ObjectSelectorHandlerAttribute : Attribute
    {
        public Type attributeType { get; }

        public ObjectSelectorHandlerAttribute(Type attributeType)
        {
            this.attributeType = attributeType;
        }
    }

    [Flags]
    public enum VisibleObjects
    {
        None = 0,
        Assets = 1,
        Scene = 1 << 1,
        All = Assets | Scene
    }

    public struct ObjectSelectorTargetInfo
    {
        public GlobalObjectId globalObjectId { get; }
        public Object targetObject { get; }
        public Type type { get; }

        public ObjectSelectorTargetInfo(GlobalObjectId globalObjectId, Object targetObject = null, Type type = null)
        {
            this.globalObjectId = globalObjectId;
            this.targetObject = targetObject;
            this.type = type;
        }

        public Object LoadObject()
        {
            return targetObject ?? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
        }
    }

    public class ObjectSelectorSearchContext : ISearchContext
    {
        public Guid guid { get; } = Guid.NewGuid();
        public SearchEngineScope engineScope { get; protected set; } = ObjectSelector.EngineScope;
        public Object currentObject { get; set; }
        public Object[] editedObjects { get; set; }
        public IEnumerable<Type> requiredTypes { get; set; }
        public IEnumerable<string> requiredTypeNames { get; set; }
        public VisibleObjects visibleObjects { get; set; }
        public IEnumerable<int> allowedInstanceIds { get; set; }
        public Func<ObjectSelectorTargetInfo, Object[], ObjectSelectorSearchContext, bool> selectorConstraint { get; set; }
    }

    public interface IObjectSelectorEngine : ISelectorEngine {}

    [InitializeOnLoad]
    public static class ObjectSelector
    {
        static readonly SearchApiBaseImp<ObjectSelectorEngineAttribute, IObjectSelectorEngine> k_EngineImp;

        public const SearchEngineScope EngineScope = SearchEngineScope.ObjectSelector;

        static ObjectSelector()
        {
            k_EngineImp = new SearchApiBaseImp<ObjectSelectorEngineAttribute, IObjectSelectorEngine>(EngineScope, "Object Selector");
        }

        internal static bool SelectObject(ObjectSelectorSearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated)
        {
            var activeEngine = k_EngineImp.activeSearchEngine;
            try
            {
                return activeEngine.SelectObject(context, onObjectSelectorClosed, onObjectSelectedUpdated);
            }
            catch (Exception ex)
            {
                k_EngineImp.HandleUserException(ex);
                return false;
            }
        }

        internal static void SetSearchFilter(string searchFilter, ObjectSelectorSearchContext context)
        {
            var activeEngine = k_EngineImp.activeSearchEngine;
            activeEngine.SetSearchFilter(context, searchFilter);
        }

        internal static bool HasEngineOverride()
        {
            return k_EngineImp.HasEngineOverride();
        }

        internal static void BeginSession(ObjectSelectorSearchContext context)
        {
            k_EngineImp.BeginSession(context);
        }

        internal static void EndSession(ObjectSelectorSearchContext context)
        {
            k_EngineImp.EndSession(context);
        }

        internal static void BeginSearch(string query, ObjectSelectorSearchContext context)
        {
            k_EngineImp.BeginSearch(query, context);
        }

        internal static void EndSearch(ObjectSelectorSearchContext context)
        {
            k_EngineImp.EndSearch(context);
        }

        internal static IObjectSelectorEngine GetActiveSearchEngine()
        {
            return k_EngineImp.GetActiveSearchEngine();
        }

        internal static void SetActiveSearchEngine(string searchEngineName)
        {
            k_EngineImp.SetActiveSearchEngine(searchEngineName);
        }

        public static void RegisterEngine(IObjectSelectorEngine engine)
        {
            k_EngineImp.RegisterEngine(engine);
        }

        public static void UnregisterEngine(IObjectSelectorEngine engine)
        {
            k_EngineImp.UnregisterEngine(engine);
        }
    }
}
