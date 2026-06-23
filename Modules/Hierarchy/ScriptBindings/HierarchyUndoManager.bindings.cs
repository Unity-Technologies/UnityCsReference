// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [NativeHeader("Modules/Hierarchy/Public/HierarchyUndoManager.h")]
    internal static class HierarchyUndoManager
    {
        [StaticAccessor("HierarchyUndoManager::Get()"), NativeMethod(IsThreadSafe = true)]
        public static extern bool IsUndoRedoSupported();

        [StaticAccessor("HierarchyUndoManager::Get()"), NativeMethod(IsThreadSafe = true)]
        public static extern void Register(int hierarchyId, Hierarchy hierarchy);

        [StaticAccessor("HierarchyUndoManager::Get()"), NativeMethod(IsThreadSafe = true)]
        public static extern void Unregister(int hierarchyId);

        // `preParents` / `preChildIndices` may be empty spans, signalling an external drop
        // (none of the nodes existed before). Otherwise they are parallel to `nodes`.
        // `postParent` is a single value because every node in a drop lands under the same target parent.
        [StaticAccessor("HierarchyUndoManager::Get()"), NativeMethod(IsThreadSafe = true)]
        public static extern void RegisterChildIndexUndo(
            Hierarchy hierarchy,
            ReadOnlySpan<HierarchyNode> nodes,
            ReadOnlySpan<HierarchyNode> preParents,
            ReadOnlySpan<int> preChildIndices,
            in HierarchyNode postParent,
            ReadOnlySpan<int> postChildIndices,
            string undoName);
    }
}
