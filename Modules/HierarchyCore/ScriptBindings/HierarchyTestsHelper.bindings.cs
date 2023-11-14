// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
            Initialize = 1 << 0,
            Dispose = 1 << 1,
            GetNodeTypeName = 1 << 2,
            GetDefaultNodeFlags = 1 << 3,
            ChangesPending = 1 << 4,
            IntegrateChanges = 1 << 5,
            SearchMatch = 1 << 6,
            SearchEnd = 1 << 7
        }

        [NativeType(Header = "Modules/HierarchyCore/HierarchyTestsHelper.h")]
        internal enum SortOrder
        {
            Ascending,
            Descending
        }

        internal delegate void ForEachDelegate(in HierarchyNode node, int index);

        internal static extern void GenerateNodes(Hierarchy hierarchy, in HierarchyNode root, int width, int depth, int maxCount = 0);
        internal static extern void GenerateSortIndex(Hierarchy hierarchy, in HierarchyNode root, SortOrder order);
        internal static void ForEach(Hierarchy hierarchy, in HierarchyNode root, ForEachDelegate func)
        {
            var stack = new Stack<HierarchyNode>();
            stack.Push(root);

            // Since we do not have NativeList, use the hierarchy count as the capacity
            using var buffer = new NativeArray<HierarchyNode>(hierarchy.Count, Allocator.Temp);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                unsafe
                {
                    // Can't use EnumerateChildren here, because the function may modify the hierarchy
                    var childrenCount = hierarchy.GetChildrenCount(in node);
                    var children = new Span<HierarchyNode>(buffer.GetUnsafePtr(), childrenCount);
                    var count = hierarchy.GetChildren(node, children);
                    if (count != childrenCount)
                        throw new InvalidOperationException($"Expected GetChildren to return {childrenCount}, but was {count}.");

                    for (int i = 0, c = children.Length; i < c; ++i)
                    {
                        var child = children[i];
                        func(in child, i);
                        stack.Push(child);
                    }
                }
            }
        }

        internal static extern void SetNextHierarchyNodeId(Hierarchy hierarchy, int id);

        internal static int GetNodeType<T>() where T : HierarchyNodeTypeHandlerBase => GetNodeType(typeof(T));

        static extern int GetNodeType(Type type);

        internal static extern int[] GetRegisteredNodeTypes(Hierarchy hierarchy);

        internal static extern int GetCapacity(Hierarchy hierarchy);

        internal static extern int GetVersion(Hierarchy hierarchy);

        internal static bool SearchMatch(HierarchyViewModel model, in HierarchyNode node)
        {
            var handler = model.Hierarchy.GetNodeTypeHandlerBase(in node);
            return handler?.Internal_SearchMatch(in node) ?? false;
        }

        internal static extern void SetCapabilitiesScriptingHandler(Hierarchy hierarchy, string nodeTypeName, int cap);
        internal static extern void SetCapabilitiesNativeHandler(Hierarchy hierarchy, string nodeTypeName, int cap);
        internal static extern int GetCapabilitiesScriptingHandler(Hierarchy hierarchy, string nodeTypeName);
        internal static extern int GetCapabilitiesNativeHandler(Hierarchy hierarchy, string nodeTypeName);

        internal static extern object GetHierarchyScriptingObject(Hierarchy hierarchy);
        internal static extern object GetHierarchyFlattenedScriptingObject(HierarchyFlattened hierarchyFlattened);
        internal static extern object GetHierarchyViewModelScriptingObject(HierarchyViewModel viewModel);
        internal static extern object GetHierarchyCommandListScriptingObject(HierarchyCommandList cmdList);

    }
}
