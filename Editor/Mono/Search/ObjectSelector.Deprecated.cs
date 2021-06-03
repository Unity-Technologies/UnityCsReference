// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Object = UnityEngine.Object;

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

    [Obsolete("ObjectSelectorHandlerAttribute has been deprecated. Use SearchContextAttribute instead.")]
    [AttributeUsage(AttributeTargets.Method)]
    public class ObjectSelectorHandlerAttribute : Attribute
    {
        public Type attributeType { get; }

        public ObjectSelectorHandlerAttribute(Type attributeType)
        {
            this.attributeType = attributeType;
        }
    }

    [Obsolete("ObjectSelectorTargetInfo has been deprecated.")]
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

    public partial class ObjectSelectorSearchContext : ISearchContext
    {
        [Obsolete("selectorConstraint has been deprecated.")]
        public Func<ObjectSelectorTargetInfo, Object[], ObjectSelectorSearchContext, bool> selectorConstraint { get; set; }
    }
}
