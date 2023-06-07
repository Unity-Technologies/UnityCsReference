// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [NativeType(Header = "Modules/HierarchyCore/HierarchyTestsHelper.h")]
    internal static class HierarchyTestsHelper
    {
        [Flags]
        internal enum Capabilities
        {
            None = 0,
            GetNodeTypeName = 1 << 0,
            ChangesPending = 1 << 1,
            IntegrateChanges = 1 << 2,
            AcceptParent = 1 << 3,
            AcceptChild = 1 << 4,
            SearchMatch = 1 << 5,
            SearchEnd = 1 << 6,
            Dispose = 1 << 7
        }

        internal static extern void SetNextHierarchyNodeId(Hierarchy hierarchy, int id);

        internal static int GetNodeType<T>() where T : HierarchyNodeTypeHandlerBase => GetNodeType(typeof(T));

        static extern int GetNodeType(Type type);

        internal static extern int[] GetRegisteredNodeTypes(Hierarchy hierarchy);

        internal static extern int GetCapacity(Hierarchy hierarchy);

        internal static bool SearchMatch(HierarchyViewModel model, in HierarchyNode node)
        {
            var handler = model.Hierarchy.GetNodeTypeHandlerBase(in node);
            return handler?.SearchMatch(in node) ?? false;
        }

        internal static extern void SetCapabilitiesScriptingHandler(Hierarchy hierarchy, string nodeTypeName, int cap);
        internal static extern void SetCapabilitiesNativeHandler(Hierarchy hierarchy, string nodeTypeName, int cap);
        internal static extern int GetCapabilitiesScriptingHandler(Hierarchy hierarchy, string nodeTypeName);
        internal static extern int GetCapabilitiesNativeHandler(Hierarchy hierarchy, string nodeTypeName);
    }
}
