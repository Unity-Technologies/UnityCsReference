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
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyViewModel : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyViewModel viewModel) => viewModel.m_Ptr;
        }

        IntPtr m_Ptr;
        internal readonly Hierarchy m_Hierarchy;
        internal readonly HierarchyFlattened m_HierarchyFlattened;
        ReadOnlyNativeVector<HierarchyFlattenedNode> m_FlattenedNodes;
        ReadOnlyNativeVector<HierarchyNode> m_Nodes;
        int m_Version;
        readonly bool m_IsOwner;

        /// <summary>
        /// Delegate that is invoked when flags on hierarchy nodes are changed.
        /// </summary>
        /// <param name="flags"></param>
        public delegate void FlagsChangedEventHandler(HierarchyNodeFlags flags);

        /// <summary>
        /// Event that is invoked when flags on hierarchy nodes are changed.
        /// </summary>
        public event FlagsChangedEventHandler FlagsChanged;

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
        public int Count => m_Nodes.Count;

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

        internal ReadOnlyNativeVector<HierarchyFlattenedNode> FlattenedNodes
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FlattenedNodes;
        }

        internal ReadOnlyNativeVector<HierarchyNode> Nodes
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Nodes;
        }

        internal int Version
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Constructs a new <see cref="HierarchyViewModel"/> from a flattened hierarchy.
        /// </summary>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        public HierarchyViewModel(HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None)
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), hierarchyFlattened, defaultFlags, out var flattenedNodesPtr, out var flattenedNodesCount, out var nodesPtr, out var nodesCount, out var version);
            m_Hierarchy = hierarchyFlattened.m_Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            m_Nodes = new ReadOnlyNativeVector<HierarchyNode>(nodesPtr, nodesCount);
            m_Version = version;
            m_IsOwner = true;

            QueryParser = new DefaultHierarchySearchQueryParser();
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyViewModel"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="flattenedNodesPtr">The native pointer to the flattened nodes.</param>
        /// <param name="flattenedNodesCount">The number of flattened nodes.</param>
        /// <param name="nodesPtr">The native pointer to the nodes.</param>
        /// <param name="nodesCount">The number of nodes.</param>
        /// <param name="version">The hierarchy view model version.</param>
        HierarchyViewModel(IntPtr nativePtr, HierarchyFlattened hierarchyFlattened, IntPtr flattenedNodesPtr, int flattenedNodesCount, IntPtr nodesPtr, int nodesCount, int version)
        {
            m_Ptr = nativePtr;
            m_Hierarchy = hierarchyFlattened.m_Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            m_Nodes = new ReadOnlyNativeVector<HierarchyNode>(nodesPtr, nodesCount);
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

            m_FlattenedNodes = default;
            m_Nodes = default;
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> at a specified index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A hierarchy node.</returns>
        public ref readonly HierarchyNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Nodes[index];
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
        /// Gets the child node at the specified index of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="index">The child index.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetChild(in HierarchyNode node, int index);

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
        /// Gets the node type handler instance for the specified node from this hierarchy view model.
        /// </summary>
        /// <returns>If the node has a type, the hierarchy node type handler base instance, <see langword="null"/> otherwise.</returns>
        public HierarchyNodeTypeHandlerBase GetNodeTypeHandlerBase(in HierarchyNode node) => HierarchyNodeTypeHandlerBase.FromIntPtr(GetNodeTypeHandlerFromNode(in node));

        /// <summary>
        /// Retrieve the hierarchy node type for the specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The hierarchy node type.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNodeType GetNodeType(in HierarchyNode node);

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
        public bool HasFlags(HierarchyNodeFlags flags) => HasFlagsAny(flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if all of the flags are set, <see langword="false"/> otherwise.</returns>
        public bool HasFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that have all of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int HasFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are not set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if none of the node have all of the flags set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveFlags(HierarchyNodeFlags flags) => DoesNotHaveFlagsAny(flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are not set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if all of the flags are not set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that do not have all of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int DoesNotHaveFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets the first index of a node that has the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The first index with the flags set, or -1 if no node found.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal extern int GetFirstIndexWithFlags(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets the last index of a node that has the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The last index with the flags set, or -1 if no node found.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal extern int GetLastIndexWithFlags(HierarchyNodeFlags flags);

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
        /// <returns>The flags that were changed during the batch.</returns>
        public HierarchyNodeFlags EndFlagsChange() => EndFlagsChange(true);

        /// <summary>
        /// Ends a batch of flags changes without notifying listeners.
        /// </summary>
        /// <returns>The flags that were changed during the batch.</returns>
        public HierarchyNodeFlags EndFlagsChangeWithoutNotify() => EndFlagsChange(false);

        /// <summary>
        /// Gets all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithFlags(HierarchyNodeFlags flags)
        {
            var count = HasFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithFlags(HierarchyNodeFlags flags) => new HierarchyViewModelNodesEnumerable(this, flags, HasFlagsNode);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithFlags(HierarchyNodeFlags flags)
        {
            var count = HasFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Gets all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithoutFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithoutFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithoutFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithoutFlags(HierarchyNodeFlags flags) => new HierarchyViewModelNodesEnumerable(this, flags, DoesNotHaveFlagsNode);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithoutFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithoutFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithoutFlagsSpan(flags, indices);
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
        public struct Enumerator
        {
            readonly HierarchyViewModel m_ViewModel;
            readonly ReadOnlyNativeVector<HierarchyNode> m_Nodes;
            readonly int m_Version;
            int m_Index;

            internal Enumerator(HierarchyViewModel hierarchyViewModel)
            {
                m_ViewModel = hierarchyViewModel;
                m_Nodes = hierarchyViewModel.m_Nodes;
                m_Version = hierarchyViewModel.m_Version;
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

                    return ref m_Nodes[m_Index];
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns>Returns true if Current item is valid</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Nodes.Count;
        }

        /// <summary>
        /// Returns a read-only span of all hierarchy nodes in the view model.
        /// </summary>
        /// <returns>A read-only span of hierarchy nodes.</returns>
        public ReadOnlySpan<HierarchyNode> AsReadOnlySpan() => m_Nodes.AsReadOnlySpan();

        // Currently required to feed UI Toolkit containers itemsSource property, which requires the collection to
        // be an IList. We do not want HierarchyViewModel to be an IList, so we provide a read-only list wrapper.
        [VisibleToOtherModules]
        internal sealed class ReadOnlyList : IList
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

        [VisibleToOtherModules]
        internal ReadOnlyList AsReadOnlyList() => new ReadOnlyList(this);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [FreeFunction("HierarchyViewModelBindings::GetState", HasExplicitThis = true, IsThreadSafe = true)]
        internal extern byte[] GetState();

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [FreeFunction("HierarchyViewModelBindings::SetState", HasExplicitThis = true, IsThreadSafe = true)]
        internal extern void SetState(ReadOnlySpan<byte> bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyViewModel FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyViewModel)GCHandle.FromIntPtr(handlePtr).Target : null;

        [FreeFunction("HierarchyViewModelBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags, out IntPtr nodesPtr, out int nodesCount, out IntPtr indicesPtr, out int indicesCount, out int version);

        [FreeFunction("HierarchyViewModelBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [FreeFunction("HierarchyViewModelBindings::GetNodeTypeHandlerFromNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr GetNodeTypeHandlerFromNode(in HierarchyNode node);

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

        [FreeFunction("HierarchyViewModelBindings::HasFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

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

        [FreeFunction("HierarchyViewModelBindings::EndFlagsChange", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNodeFlags EndFlagsChange(bool notify);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithoutFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithoutFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithoutFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithoutFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateHierarchyViewModel(IntPtr nativePtr, IntPtr flattenedPtr, IntPtr flattenedNodesPtr, int flattenedNodesCount, IntPtr nodesPtr, int nodesCount, int version) =>
            GCHandle.ToIntPtr(GCHandle.Alloc(new HierarchyViewModel(nativePtr, HierarchyFlattened.FromIntPtr(flattenedPtr), flattenedNodesPtr, flattenedNodesCount, nodesPtr, nodesCount, version)));

        [RequiredByNativeCode]
        static void UpdateHierarchyViewModel(IntPtr handlePtr, IntPtr flattenedNodesPtr, int flattenedNodesCount, IntPtr nodesPtr, int nodesCount, int version)
        {
            var viewModel = FromIntPtr(handlePtr);
            viewModel.m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            viewModel.m_Nodes = new ReadOnlyNativeVector<HierarchyNode>(nodesPtr, nodesCount);
            viewModel.m_Version = version;
        }

        [RequiredByNativeCode]
        static void InvokeFlagsChanged(IntPtr handlePtr, HierarchyNodeFlags flags)
        {
            var viewModel = FromIntPtr(handlePtr);
            viewModel.FlagsChanged?.Invoke(flags);
        }

        [RequiredByNativeCode]
        static void SearchBegin(IntPtr handlePtr)
        {
            var viewModel = FromIntPtr(handlePtr);
            foreach (var handler in viewModel.m_Hierarchy.EnumerateNodeTypeHandlersBase())
                handler.Internal_SearchBegin(viewModel.Query);
        }
        #endregion

        #region Marked as obsolete warning in 6.3
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
        #endregion

        #region Marked as obsolete warning in 6.5
        [Obsolete("HasAllFlags is obsolete, please use HasFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasAllFlags(HierarchyNodeFlags flags) => HasFlagsAny(flags);

        [Obsolete("HasAllFlags is obsolete, please use HasFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasAllFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasFlagsNode(in node, flags);

        [Obsolete("HasAnyFlags is obsolete, please use HasFlags instead.", false)]
        public bool HasAnyFlags(HierarchyNodeFlags flags) => HasFlagsAny(flags);

        [Obsolete("HasAnyFlags is obsolete, please use HasFlags instead.", false)]
        public bool HasAnyFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasFlagsNode(in node, flags);

        [Obsolete("HasAllFlagsCount is obsolete, HasFlagsCount instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int HasAllFlagsCount(HierarchyNodeFlags flags) => HasFlagsCount(flags);

        [Obsolete("HasAnyFlagsCount is obsolete, HasFlagsCount instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int HasAnyFlagsCount(HierarchyNodeFlags flags) => HasFlagsCount(flags);

        [Obsolete("DoesNotHaveAllFlags is obsolete, please use DoesNotHaveFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DoesNotHaveAllFlags(HierarchyNodeFlags flags) => DoesNotHaveFlagsAny(flags);

        [Obsolete("DoesNotHaveAllFlags is obsolete, please use DoesNotHaveFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DoesNotHaveAllFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveFlagsNode(in node, flags);

        [Obsolete("DoesNotHaveAnyFlags is obsolete, please use DoesNotHaveFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DoesNotHaveAnyFlags(HierarchyNodeFlags flags) => DoesNotHaveFlagsAny(flags);

        [Obsolete("DoesNotHaveAnyFlags is obsolete, please use DoesNotHaveFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DoesNotHaveAnyFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveFlagsNode(in node, flags);

        [Obsolete("DoesNotHaveAllFlagsCount is obsolete, please use DoesNotHaveFlagsCount instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int DoesNotHaveAllFlagsCount(HierarchyNodeFlags flags) => DoesNotHaveFlagsCount(flags);

        [Obsolete("DoesNotHaveAnyFlagsCount is obsolete, please use DoesNotHaveFlagsCount instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int DoesNotHaveAnyFlagsCount(HierarchyNodeFlags flags) => DoesNotHaveFlagsCount(flags);

        [Obsolete("GetNodesWithAllFlags is obsolete, please use GetNodesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetNodesWithAllFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithAllFlags is obsolete, please use GetNodesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithAllFlags(HierarchyNodeFlags flags) => GetNodesWithFlags(flags);

        [Obsolete("GetNodesWithAnyFlags is obsolete, please use GetNodesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetNodesWithAnyFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithAnyFlags is obsolete, please use GetNodesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithAnyFlags(HierarchyNodeFlags flags) => GetNodesWithFlags(flags);

        [Obsolete("EnumerateNodesWithAllFlags is obsolete, please use EnumerateNodesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithAllFlags(HierarchyNodeFlags flags) => EnumerateNodesWithFlags(flags);

        [Obsolete("EnumerateNodesWithAnyFlags is obsolete, please use EnumerateNodesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithAnyFlags(HierarchyNodeFlags flags) => EnumerateNodesWithFlags(flags);

        [Obsolete("GetIndicesWithAllFlags is obsolete, please use GetIndicesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetIndicesWithAllFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithAllFlags is obsolete, please use GetIndicesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] GetIndicesWithAllFlags(HierarchyNodeFlags flags) => GetIndicesWithFlags(flags);

        [Obsolete("GetIndicesWithAnyFlags is obsolete, please use GetIndicesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetIndicesWithAnyFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithAnyFlags is obsolete, please use GetIndicesWithFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] GetIndicesWithAnyFlags(HierarchyNodeFlags flags) => GetIndicesWithFlags(flags);

        [Obsolete("GetNodesWithoutAllFlags is obsolete, please use GetNodesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetNodesWithoutAllFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithoutAllFlags is obsolete, please use GetNodesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithoutAllFlags(HierarchyNodeFlags flags) => GetNodesWithoutFlags(flags);

        [Obsolete("GetNodesWithoutAnyFlags is obsolete, please use GetNodesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetNodesWithoutAnyFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithoutAnyFlags is obsolete, please use GetNodesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithoutAnyFlags(HierarchyNodeFlags flags) => GetNodesWithoutFlags(flags);

        [Obsolete("EnumerateNodesWithoutAllFlags is obsolete, please use EnumerateNodesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithoutAllFlags(HierarchyNodeFlags flags) => EnumerateNodesWithoutFlags(flags);

        [Obsolete("EnumerateNodesWithoutAnyFlags is obsolete, please use EnumerateNodesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithoutAnyFlags(HierarchyNodeFlags flags) => EnumerateNodesWithoutFlags(flags);

        [Obsolete("GetIndicesWithoutAllFlags is obsolete, please use GetIndicesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetIndicesWithoutAllFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithoutAllFlags is obsolete, please use GetIndicesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] GetIndicesWithoutAllFlags(HierarchyNodeFlags flags) => GetIndicesWithoutFlags(flags);

        [Obsolete("GetIndicesWithoutAnyFlags is obsolete, please use GetIndicesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetIndicesWithoutAnyFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithoutAnyFlags is obsolete, please use GetIndicesWithoutFlags instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] GetIndicesWithoutAnyFlags(HierarchyNodeFlags flags) => GetIndicesWithoutFlags(flags);
        #endregion
    }
}
