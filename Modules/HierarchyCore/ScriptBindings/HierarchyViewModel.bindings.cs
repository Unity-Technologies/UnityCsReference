// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A hierarchy view model is a read-only filtering view of a <see cref="HierarchyFlattened"/>.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyViewModel.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyViewModelBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyViewModel : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyViewModel viewModel) => viewModel.m_Ptr;
        }

        IntPtr m_Ptr;
        internal readonly Hierarchy m_Hierarchy;
        internal readonly HierarchyFlattened m_HierarchyFlattened;
        IntPtr m_NodesPtr;
        int m_NodesCount;
        IntPtr m_IndicesPtr;
        int m_IndicesCount;
        int m_Version;
        readonly bool m_IsOwner;

        /// <summary>
        /// Whether this object is valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The total number of nodes.
        /// </summary>
        /// <remarks>
        /// The total does not include the <see cref="Hierarchy.Root"/> node.
        /// </remarks>
        public int Count => m_IndicesCount;

        /// <summary>
        /// Whether the hierarchy view model is currently updating.
        /// </summary>
        /// <remarks>
        /// This happens when <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/> is used.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating", IsThreadSafe = true)] get; }

        /// <summary>
        /// Whether the hierarchy view model requires an update.
        /// </summary>
        /// <remarks>
        /// This happens when the underlying hierarchy changes topology.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded", IsThreadSafe = true)] get; }

        /// <summary>
        /// Whether the hierarchy view model is currently filtering nodes.
        /// </summary>
        /// <remarks>
        /// This happens when there is a non empty <see cref="HierarchySearchQueryDescriptor"/> set.
        /// </remarks>
        public extern bool Filtering { [NativeMethod("Filtering", IsThreadSafe = true)] get; }

        unsafe internal HierarchyFlattenedNode* NodesPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (HierarchyFlattenedNode*)m_NodesPtr;
        }

        internal int NodesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_NodesCount;
        }

        unsafe internal int* IndicesPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int*)m_IndicesPtr;
        }

        internal int IndicesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IndicesCount;
        }

        internal int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            get => m_Version;
        }

        internal extern float UpdateProgress
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod("UpdateProgress", IsThreadSafe = true)]
            get;
        }

        internal IHierarchySearchQueryParser QueryParser
        {
            [VisibleToOtherModules("UnityEditor.HierarchyModule")]
            get;
            [VisibleToOtherModules("UnityEditor.HierarchyModule")]
            set;
        }

        internal extern HierarchySearchQueryDescriptor Query
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod(IsThreadSafe = true)]
            get;
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod(IsThreadSafe = true)]
            set;
        }

        /// <summary>
        /// Cosntructs a new <see cref="HierarchyViewModel"/>.
        /// </summary>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        public HierarchyViewModel(HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None)
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), hierarchyFlattened, defaultFlags, out var nodesPtr, out var nodesCount, out var indicesPtr, out var indicesCount, out var version);
            m_Hierarchy = hierarchyFlattened.m_Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_NodesPtr = nodesPtr;
            m_NodesCount = nodesCount;
            m_IndicesPtr = indicesPtr;
            m_IndicesCount = indicesCount;
            m_Version = version;
            m_IsOwner = true;

            QueryParser = new DefaultHierarchySearchQueryParser();
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyViewModel"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="nodesPtr">The native pointer to the nodes.</param>
        /// <param name="nodesCount">The number of nodes.</param>
        /// <param name="indicesPtr">The native pointer to the indices.</param>
        /// <param name="indicesCount">The number of indices.</param>
        /// <param name="version">The hierarchy view model version.</param>
        HierarchyViewModel(IntPtr nativePtr, HierarchyFlattened hierarchyFlattened, IntPtr nodesPtr, int nodesCount, IntPtr indicesPtr, int indicesCount, int version)
        {
            m_Ptr = nativePtr;
            m_Hierarchy = hierarchyFlattened.m_Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_NodesPtr = nodesPtr;
            m_NodesCount = nodesCount;
            m_IndicesPtr = indicesPtr;
            m_IndicesCount = indicesCount;
            m_Version = version;
            m_IsOwner = false;

            QueryParser = new DefaultHierarchySearchQueryParser();
        }

        ~HierarchyViewModel()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes this object and releases its memory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                if (m_IsOwner)
                    Destroy(m_Ptr);

                m_Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> at a specified index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A hierarchy node.</returns>
        public ref readonly HierarchyNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= m_IndicesCount)
                    throw new ArgumentOutOfRangeException(nameof(index));

                unsafe
                {
                    var nodeIndex = IndicesPtr[index];
                    if (nodeIndex < 0 || nodeIndex >= m_NodesCount)
                        throw new IndexOutOfRangeException(nameof(nodeIndex));

                    ref readonly var flattenedNode = ref NodesPtr[nodeIndex];
                    return ref HierarchyFlattenedNode.GetNodeByRef(in flattenedNode);
                }
            }
        }

        /// <summary>
        /// Gets the zero-based index of a specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A zero-based index of the node if found, -1 otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int IndexOf(in HierarchyNode node);

        /// <summary>
        /// Determines if a specified node is in the hierarchy view model.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is found, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool Contains(in HierarchyNode node);

        /// <summary>
        /// Sets the root of the hierarchy view model.
        /// </summary>
        /// <remarks>
        /// This is purely visual and does not affect the underlying hierarchy data.
        /// </remarks>
        /// <param name="node">The hierarchy node.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SetRoot(in HierarchyNode node);

        /// <summary>
        /// Gets the root node of the hierarchy view model.
        /// </summary>
        /// <returns>A hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern HierarchyNode GetRoot();

        /// <summary>
        /// Gets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Gets the next sibling of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of children.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has, including children of children.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of child nodes, including children of children.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildrenCountRecursive(in HierarchyNode node);

        /// <summary>
        /// Gets the index of a hierarchy node in its parent's children list.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The node index, or -1 if invalid.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildIndex(in HierarchyNode node);

        /// <summary>
        /// Determines the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Gets all the flags set on a given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The flags set on the hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNodeFlags GetFlags(in HierarchyNode node);

        /// <summary>
        /// Sets the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void SetFlags(HierarchyNodeFlags flags) => SetFlagsAll(flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        public void SetFlags(in HierarchyNode node, HierarchyNodeFlags flags) => SetFlagsNode(in node, flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags set.</returns>
        public int SetFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => SetFlagsNodes(nodes, flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags set.</returns>
        public int SetFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => SetFlagsIndices(indices, flags);

        /// <summary>
        /// Sets the specified flags recursively on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void SetFlagsRecursive(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => SetFlagsRecursiveNode(in node, flags, direction);

        /// <summary>
        /// Sets the specified flags recursively on the hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void SetFlagsRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => SetFlagsRecursiveNodes(nodes, flags, direction);

        /// <summary>
        /// Gets whether or not all of the specified flags are set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if any node have all of the flags set, <see langword="false"/> otherwise.</returns>
        public bool HasAllFlags(HierarchyNodeFlags flags) => HasAllFlagsAny(flags);

        /// <summary>
        /// Gets whether or not any of the specified flags are set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if any node have any of the flags set, <see langword="false"/> otherwise.</returns>
        public bool HasAnyFlags(HierarchyNodeFlags flags) => HasAnyFlagsAny(flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if all of the flags are set, <see langword="false"/> otherwise.</returns>
        public bool HasAllFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasAllFlagsNode(in node, flags);

        /// <summary>
        /// Gets whether or not any of the specified flags are set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if any of the flags are set, <see langword="false"/> otherwise.</returns>
        public bool HasAnyFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasAnyFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that have all of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int HasAllFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets the number of nodes that have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that have any of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int HasAnyFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are not set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if none of the node have all of the flags set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveAllFlags(HierarchyNodeFlags flags) => DoesNotHaveAllFlagsAny(flags);

        /// <summary>
        /// Gets whether or not any of the specified flags are not set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if none of the node have any of the flags set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveAnyFlags(HierarchyNodeFlags flags) => DoesNotHaveAnyFlagsAny(flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are not set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if all of the flags are not set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveAllFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveAllFlagsNode(in node, flags);

        /// <summary>
        /// Gets whether or not any of the specified flags are not set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if none of the flags are set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveAnyFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveAnyFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that do not have all of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int DoesNotHaveAllFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets the number of nodes that do not have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that do not have any of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int DoesNotHaveAnyFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Clears the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ClearFlags(HierarchyNodeFlags flags) => ClearFlagsAll(flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ClearFlags(in HierarchyNode node, HierarchyNodeFlags flags) => ClearFlagsNode(in node, flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ClearFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => ClearFlagsNodes(nodes, flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ClearFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => ClearFlagsIndices(indices, flags);

        /// <summary>
        /// Clears the specified flags recursively on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ClearFlagsRecursive(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ClearFlagsRecursiveNode(in node, flags, direction);

        /// <summary>
        /// Clears the specified flags recursively on the hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ClearFlagsRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ClearFlagsRecursiveNodes(nodes, flags, direction);

        /// <summary>
        /// Toggles the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ToggleFlags(HierarchyNodeFlags flags) => ToggleFlagsAll(flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ToggleFlags(in HierarchyNode node, HierarchyNodeFlags flags) => ToggleFlagsNode(in node, flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ToggleFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => ToggleFlagsNodes(nodes, flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ToggleFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => ToggleFlagsIndices(indices, flags);

        /// <summary>
        /// Toggles the specified flags recursively on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ToggleFlagsRecursive(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ToggleFlagsRecursiveNode(in node, flags, direction);

        /// <summary>
        /// Toggles the specified flags recursively on the hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ToggleFlagsRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ToggleFlagsRecursiveNodes(nodes, flags, direction);

        /// <summary>
        /// Begins a batch of flags changes.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void BeginFlagsChange();

        /// <summary>
        /// Ends a batch of flags changes.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void EndFlagsChange();

        /// <summary>
        /// Gets all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithAllFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithAllFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithAnyFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithAnyFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithAllFlags(HierarchyNodeFlags flags)
        {
            var count = HasAllFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithAllFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets all hierarchy nodes that have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithAnyFlags(HierarchyNodeFlags flags)
        {
            var count = HasAnyFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithAnyFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewNodesEnumerable EnumerateNodesWithAllFlags(HierarchyNodeFlags flags) => new HierarchyViewNodesEnumerable(this, flags, HasAllFlags);

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewNodesEnumerable EnumerateNodesWithAnyFlags(HierarchyNodeFlags flags) => new HierarchyViewNodesEnumerable(this, flags, HasAnyFlags);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithAllFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithAllFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithAnyFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithAnyFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithAllFlags(HierarchyNodeFlags flags)
        {
            var count = HasAllFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithAllFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithAnyFlags(HierarchyNodeFlags flags)
        {
            var count = HasAnyFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithAnyFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Gets all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithoutAllFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutAllFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that do not have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithoutAnyFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutAnyFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithoutAllFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveAllFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithoutAllFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets all hierarchy nodes that do not have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithoutAnyFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveAnyFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithoutAnyFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewNodesEnumerable EnumerateNodesWithoutAllFlags(HierarchyNodeFlags flags) => new HierarchyViewNodesEnumerable(this, flags, DoesNotHaveAllFlags);

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that do not have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewNodesEnumerable EnumerateNodesWithoutAnyFlags(HierarchyNodeFlags flags) => new HierarchyViewNodesEnumerable(this, flags, DoesNotHaveAnyFlags);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithoutAllFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutAllFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithoutAnyFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutAnyFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithoutAllFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveAllFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithoutAllFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have any of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithoutAnyFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveAnyFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithoutAnyFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Sets the search query.
        /// </summary>
        /// <param name="query">The search query.</param>
        public void SetQuery(string query)
        {
            var newQuery = QueryParser.ParseQuery(query);
            if (newQuery == Query)
                return;
            Query = newQuery;
        }

        /// <summary>
        /// Updates the hierarchy view model and requests a rebuild of the list of <see cref="HierarchyNode"/> that filters the <see cref="HierarchyFlattened"/>.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Update();

        /// <summary>
        /// Updates the hierarchy view model incrementally.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncremental();

        /// <summary>
        /// Updates the hierarchy view model incrementally until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time period in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncrementalTimed(double milliseconds);

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>. Enumerates and filters items at the same time.
        /// </summary>
        public unsafe struct Enumerator
        {
            readonly HierarchyViewModel m_ViewModel;
            readonly HierarchyFlattenedNode* m_NodesPtr;
            readonly int m_NodesCount;
            readonly int* m_IndicesPtr;
            readonly int m_IndicesCount;
            readonly int m_Version;
            int m_Index;

            internal Enumerator(HierarchyViewModel hierarchyViewModel)
            {
                m_ViewModel = hierarchyViewModel;
                m_NodesPtr = hierarchyViewModel.NodesPtr;
                m_NodesCount = hierarchyViewModel.NodesCount;
                m_IndicesPtr = hierarchyViewModel.IndicesPtr;
                m_IndicesCount = hierarchyViewModel.IndicesCount;
                m_Version = hierarchyViewModel.Version;
                m_Index = -1;
            }

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public ref readonly HierarchyNode Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (m_Version != m_ViewModel.m_Version)
                        throw new InvalidOperationException("HierarchyViewModel was modified.");

                    var nodeIndex = m_IndicesPtr[m_Index];
                    ref readonly var flattenedNode = ref m_NodesPtr[nodeIndex];
                    return ref HierarchyFlattenedNode.GetNodeByRef(in flattenedNode);
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns>Returns true if Current item is valid</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_IndicesCount;
        }

        // Currently required to feed UI Toolkit containers itemsSource property, which requires the collection to
        // be an IList. We do not want HierarchyViewModel to be an IList, so we provide a read-only list wrapper.
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal class ReadOnlyList : IList
        {
            readonly HierarchyViewModel m_ViewModel;

            internal ReadOnlyList(HierarchyViewModel viewModel)
            {
                m_ViewModel = viewModel;
            }

            public bool IsFixedSize => true;
            public bool IsReadOnly => true;

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_ViewModel.IsCreated ? m_ViewModel.Count : throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
            }

            public object this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_ViewModel.IsCreated ? m_ViewModel[index] : throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
                set => throw new NotSupportedException();
            }

            public bool Contains(object value)
            {
                if (value is HierarchyNode node)
                {
                    return m_ViewModel.IsCreated ?
                        m_ViewModel.Contains(in node) :
                        throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
                }
                return false;
            }

            public int IndexOf(object value)
            {
                if (value is HierarchyNode node)
                {
                    return m_ViewModel.IsCreated ?
                        m_ViewModel.IndexOf(in node) :
                        throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
                }
                return -1;
            }

            public void CopyTo(Array array, int index)
            {
                for (var i = index; i < m_ViewModel.Count; ++i)
                    array.SetValue(m_ViewModel[i], i - index);
            }

            public Enumerator GetEnumerator() => new HierarchyViewModel.Enumerator(m_ViewModel);

            int IList.Add(object value) => throw new NotSupportedException();
            void IList.Clear() => throw new NotSupportedException();
            void IList.Insert(int index, object value) => throw new NotSupportedException();
            void IList.Remove(object value) => throw new NotSupportedException();
            void IList.RemoveAt(int index) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
            bool ICollection.IsSynchronized => throw new NotImplementedException();
            object ICollection.SyncRoot => throw new NotImplementedException();
        }

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal ReadOnlyList AsReadOnlyList() => new ReadOnlyList(this);

        [FreeFunction("HierarchyViewModelBindings::GetState", HasExplicitThis = true, IsThreadSafe = true)]
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal extern byte[] GetState();

        [FreeFunction("HierarchyViewModelBindings::SetState", HasExplicitThis = true, IsThreadSafe = true)]
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal extern void SetState(ReadOnlySpan<byte> bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyViewModel FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyViewModel)GCHandle.FromIntPtr(handlePtr).Target : null;

        [FreeFunction("HierarchyViewModelBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags, out IntPtr nodesPtr, out int nodesCount, out IntPtr indicesPtr, out int indicesCount, out int version);

        [FreeFunction("HierarchyViewModelBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int SetFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsRecursiveNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsRecursiveNode(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsRecursiveNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern void SetFlagsRecursiveNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int SetFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasAllFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasAllFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasAnyFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasAnyFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasAllFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasAllFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasAnyFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasAnyFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveAllFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveAllFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveAnyFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveAnyFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveAllFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveAllFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveAnyFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveAnyFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ClearFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ClearFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ClearFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ClearFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsRecursiveNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ClearFlagsRecursiveNode(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsRecursiveNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern void ClearFlagsRecursiveNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ToggleFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ToggleFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsRecursiveNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsRecursiveNode(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsRecursiveNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern void ToggleFlagsRecursiveNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithAllFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithAllFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithAnyFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithAnyFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithAllFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithAllFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithAnyFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithAnyFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithoutAllFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithoutAllFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithoutAnyFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithoutAnyFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithoutAllFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithoutAllFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithoutAnyFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithoutAnyFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateHierarchyViewModel(IntPtr nativePtr, IntPtr flattenedPtr, IntPtr nodesPtr, int nodesCount, IntPtr indicesPtr, int indicesCount, int version) =>
            GCHandle.ToIntPtr(GCHandle.Alloc(new HierarchyViewModel(nativePtr, HierarchyFlattened.FromIntPtr(flattenedPtr), nodesPtr, nodesCount, indicesPtr, indicesCount, version)));

        [RequiredByNativeCode]
        static void UpdateHierarchyViewModel(IntPtr handlePtr, IntPtr nodesPtr, int nodesCount, IntPtr indicesPtr, int indicesCount, int version)
        {
            var viewModel = FromIntPtr(handlePtr);
            viewModel.m_NodesPtr = nodesPtr;
            viewModel.m_NodesCount = nodesCount;
            viewModel.m_IndicesPtr = indicesPtr;
            viewModel.m_IndicesCount = indicesCount;
            viewModel.m_Version = version;
        }

        [RequiredByNativeCode]
        static void SearchBegin(IntPtr handlePtr)
        {
            var viewModel = FromIntPtr(handlePtr);
            foreach (var handler in viewModel.m_Hierarchy.EnumerateNodeTypeHandlersBase())
                handler.Internal_SearchBegin(viewModel.Query);
        }
        #endregion

        #region Obsolete public APIs to remove in 2024
        [Obsolete("The Hierarchy property will be removed in the future, remove its usage from your code.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Hierarchy Hierarchy => m_Hierarchy;

        [Obsolete("The HierarchyFlattened property will be removed in the future, remove its usage from your code.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyFlattened HierarchyFlattned => m_HierarchyFlattened;

        [Obsolete("SetFlags(node, flags, recurse) with a bool parameter is obsolete, please use SetFlags(node, flags) or SetFlags(node, flags, direction) instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse)
        {
            if (recurse)
                SetFlagsRecursiveNode(in node, flags, HierarchyTraversalDirection.Children);
            else
                SetFlagsNode(in node, flags);
        }

        [Obsolete("ClearFlags(node, flags, recurse) with a bool parameter is obsolete, please use ClearFlags(node, flags) or ClearFlags(node, flags, direction) instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ClearFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse)
        {
            if (recurse)
                ClearFlagsRecursiveNode(in node, flags, HierarchyTraversalDirection.Children);
            else
                ClearFlagsNode(in node, flags);
        }

        [Obsolete("ToggleFlags(node, flags, recurse) with a bool parameter is obsolete, please use ToggleFlags(node, flags) or ToggleFlags(node, flags, direction) instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ToggleFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse)
        {
            if (recurse)
                ToggleFlagsRecursiveNode(in node, flags, HierarchyTraversalDirection.Children);
            else
                ToggleFlagsNode(in node, flags);
        }

        [Obsolete("HasFlags is obsolete, please use HasAllFlags or HasAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasFlags(HierarchyNodeFlags flags) => HasAllFlagsAny(flags);

        [Obsolete("HasFlags is obsolete, please use HasAllFlags or HasAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasAllFlagsNode(in node, flags);

        [Obsolete("HasFlagsCount is obsolete, please use HasAllFlagsCount or HasAnyFlagsCount instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int HasFlagsCount(HierarchyNodeFlags flags) => HasAllFlagsCount(flags);

        [Obsolete("DoesNotHaveFlags is obsolete, please use DoesNotHaveAllFlags or DoesNotHaveAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DoesNotHaveFlags(HierarchyNodeFlags flags) => DoesNotHaveAllFlagsAny(flags);

        [Obsolete("DoesNotHaveFlags is obsolete, please use DoesNotHaveAllFlags or DoesNotHaveAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DoesNotHaveFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveAllFlagsNode(in node, flags);

        [Obsolete("DoesNotHaveFlagsCount is obsolete, please use DoesNotHaveAllFlagsCount or DoesNotHaveAnyFlagsCount instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int DoesNotHaveFlagsCount(HierarchyNodeFlags flags) => DoesNotHaveAllFlagsCount(flags);

        [Obsolete("GetNodesWithFlags is obsolete, please use GetNodesWithAllFlags or GetNodesWithAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetNodesWithFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithAllFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithFlags is obsolete, please use GetNodesWithAllFlags or GetNodesWithAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithFlags(HierarchyNodeFlags flags) => GetNodesWithAllFlags(flags);

        [Obsolete("EnumerateNodesWithFlags is obsolete, please use EnumerateNodesWithAllFlags or EnumerateNodesWithAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyViewNodesEnumerable EnumerateNodesWithFlags(HierarchyNodeFlags flags) => EnumerateNodesWithAllFlags(flags);

        [Obsolete("GetIndicesWithFlags is obsolete, please use GetIndicesWithAllFlags or GetIndicesWithAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetIndicesWithFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithAllFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithFlags is obsolete, please use GetIndicesWithAllFlags or GetIndicesWithAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] GetIndicesWithFlags(HierarchyNodeFlags flags) => GetIndicesWithAllFlags(flags);

        [Obsolete("GetNodesWithoutFlags is obsolete, please use GetNodesWithoutAllFlags or GetNodesWithoutAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetNodesWithoutFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutAllFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithoutFlags is obsolete, please use GetNodesWithoutAllFlags or GetNodesWithoutAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithoutFlags(HierarchyNodeFlags flags) => GetNodesWithoutAllFlags(flags);

        [Obsolete("EnumerateNodesWithoutFlags is obsolete, please use EnumerateNodesWithoutAllFlags or EnumerateNodesWithoutAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyViewNodesEnumerable EnumerateNodesWithoutFlags(HierarchyNodeFlags flags) => EnumerateNodesWithoutAllFlags(flags);

        [Obsolete("GetIndicesWithoutFlags is obsolete, please use GetIndicesWithoutAllFlags or GetIndicesWithoutAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetIndicesWithoutFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutAllFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithoutFlags is obsolete, please use GetIndicesWithoutAllFlags or GetIndicesWithoutAnyFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] GetIndicesWithoutFlags(HierarchyNodeFlags flags) => GetIndicesWithoutAllFlags(flags);
        #endregion
    }
}
