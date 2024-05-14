// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A hierarchy view model is a read-only filtering view of a <see cref="HierarchyFlattened"/>.
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyViewModel.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyViewModelBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyViewModel : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyViewModel viewModel) => viewModel.m_Ptr;
        }

        IntPtr m_Ptr;
        readonly Hierarchy m_Hierarchy;
        readonly HierarchyFlattened m_HierarchyFlattened;
        IntPtr m_NodesPtr;
        int m_NodesCount;
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
        public int Count => m_NodesCount;

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
        /// Accesses the <see cref="HierarchyFlattened"/>.
        /// </summary>
        public HierarchyFlattened HierarchyFlattened => m_HierarchyFlattened;

        /// <summary>
        /// Accesses the <see cref="Hierarchy"/>.
        /// </summary>
        public Hierarchy Hierarchy => m_Hierarchy;

        /// <summary>
        /// Gets the pointer to native memory for the nodes.
        /// </summary>
        internal unsafe int* NodesPtr => (int*)m_NodesPtr;

        /// <summary>
        /// Gets the version of this <see cref="HierarchyViewModel"/>.
        /// </summary>        
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
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), hierarchyFlattened, defaultFlags, out var nodesPtr, out var nodesCount, out var version);
            m_Hierarchy = hierarchyFlattened.Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_NodesPtr = nodesPtr;
            m_NodesCount = nodesCount;
            m_Version = version;
            m_IsOwner = true;

            QueryParser = new DefaultHierarchySearchQueryParser();
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyViewModel"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        HierarchyViewModel(IntPtr nativePtr, HierarchyFlattened hierarchyFlattened, IntPtr nodesPtr, int nodesCount, int version)
        {
            m_Ptr = nativePtr;
            m_Hierarchy = hierarchyFlattened.Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_NodesPtr = nodesPtr;
            m_NodesCount = nodesCount;
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
                if (index < 0 || index >= m_NodesCount)
                    throw new ArgumentOutOfRangeException(nameof(index));

                unsafe
                {
                    return ref HierarchyFlattenedNode.GetNodeByRef(in m_HierarchyFlattened[((int*)m_NodesPtr)[index]]);
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
        /// <param name="recurse">Whether or not to set the flags on all children recursively for that hierarchy node.</param>
        public void SetFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false) => SetFlagsNode(in node, flags, recurse);

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
        /// <param name="recurse">Whether or not to clear the flags on all children recursively for that hierarchy node.</param>
        public void ClearFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false) => ClearFlagsNode(in node, flags, recurse);

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
        /// Toggles the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ToggleFlags(HierarchyNodeFlags flags) => ToggleFlagsAll(flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="recurse">Whether or not to clear the flags on all children recursively for that hierarchy node.</param>
        public void ToggleFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false) => ToggleFlagsNode(in node, flags, recurse);

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
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly int* m_NodesPtr;
            readonly int m_NodesCount;
            readonly int m_Version;
            int m_Index;

            internal Enumerator(HierarchyViewModel hierarchyViewModel)
            {
                m_ViewModel = hierarchyViewModel;
                m_HierarchyFlattened = hierarchyViewModel.HierarchyFlattened;
                m_NodesPtr = (int*)hierarchyViewModel.m_NodesPtr;
                m_NodesCount = hierarchyViewModel.Count;
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

                    return ref HierarchyFlattenedNode.GetNodeByRef(in m_HierarchyFlattened[m_NodesPtr[m_Index]]);
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns>Returns true if Current item is valid</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_NodesCount;
        }

        [FreeFunction("HierarchyViewModelBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags, out IntPtr nodesPtr, out int nodesCount, out int version);

        [FreeFunction("HierarchyViewModelBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int SetFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

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
        extern void ClearFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ClearFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ClearFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ToggleFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ToggleFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

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
        static IntPtr CreateHierarchyViewModel(IntPtr nativePtr, HierarchyFlattened hierarchyFlattened, IntPtr nodesPtr, int nodesCount, int version) =>
            GCHandle.ToIntPtr(GCHandle.Alloc(new HierarchyViewModel(nativePtr, hierarchyFlattened, nodesPtr, nodesCount, version)));

        [RequiredByNativeCode]
        static void UpdateHierarchyViewModel(IntPtr handlePtr, IntPtr nodesPtr, int nodesCount, int version)
        {
            var viewModel = (HierarchyViewModel)GCHandle.FromIntPtr(handlePtr).Target;
            viewModel.m_NodesPtr = nodesPtr;
            viewModel.m_NodesCount = nodesCount;
            viewModel.m_Version = version;
        }

        [RequiredByNativeCode]
        void SearchBegin()
        {
            using var _ = ListPool<HierarchyNodeTypeHandlerBase>.Get(out var handlers);
            m_Hierarchy.GetAllNodeTypeHandlersBase(handlers);
            foreach (var handler in handlers)
                handler.Internal_SearchBegin(Query);
        }
        #endregion

        #region Obsolete public APIs to remove in 2024
        [Obsolete("HasFlags is obsolete, please use HasAllFlags or HasAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool HasFlags(HierarchyNodeFlags flags) => HasAllFlagsAny(flags);

        [Obsolete("HasFlags is obsolete, please use HasAllFlags or HasAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool HasFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasAllFlagsNode(in node, flags);

        [Obsolete("HasFlagsCount is obsolete, please use HasAllFlagsCount or HasAnyFlagsCount instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int HasFlagsCount(HierarchyNodeFlags flags) => HasAllFlagsCount(flags);

        [Obsolete("DoesNotHaveFlags is obsolete, please use DoesNotHaveAllFlags or DoesNotHaveAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool DoesNotHaveFlags(HierarchyNodeFlags flags) => DoesNotHaveAllFlagsAny(flags);

        [Obsolete("DoesNotHaveFlags is obsolete, please use DoesNotHaveAllFlags or DoesNotHaveAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool DoesNotHaveFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveAllFlagsNode(in node, flags);

        [Obsolete("DoesNotHaveFlagsCount is obsolete, please use DoesNotHaveAllFlagsCount or DoesNotHaveAnyFlagsCount instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int DoesNotHaveFlagsCount(HierarchyNodeFlags flags) => DoesNotHaveAllFlagsCount(flags);

        [Obsolete("GetNodesWithFlags is obsolete, please use GetNodesWithAllFlags or GetNodesWithAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int GetNodesWithFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithAllFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithFlags is obsolete, please use GetNodesWithAllFlags or GetNodesWithAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithFlags(HierarchyNodeFlags flags) => GetNodesWithAllFlags(flags);

        [Obsolete("EnumerateNodesWithFlags is obsolete, please use EnumerateNodesWithAllFlags or EnumerateNodesWithAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public HierarchyViewNodesEnumerable EnumerateNodesWithFlags(HierarchyNodeFlags flags) => EnumerateNodesWithAllFlags(flags);

        [Obsolete("GetIndicesWithFlags is obsolete, please use GetIndicesWithAllFlags or GetIndicesWithAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int GetIndicesWithFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithAllFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithFlags is obsolete, please use GetIndicesWithAllFlags or GetIndicesWithAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int[] GetIndicesWithFlags(HierarchyNodeFlags flags) => GetIndicesWithAllFlags(flags);

        [Obsolete("GetNodesWithoutFlags is obsolete, please use GetNodesWithoutAllFlags or GetNodesWithoutAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int GetNodesWithoutFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutAllFlagsSpan(flags, outNodes);

        [Obsolete("GetNodesWithoutFlags is obsolete, please use GetNodesWithoutAllFlags or GetNodesWithoutAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public HierarchyNode[] GetNodesWithoutFlags(HierarchyNodeFlags flags) => GetNodesWithoutAllFlags(flags);

        [Obsolete("EnumerateNodesWithoutFlags is obsolete, please use EnumerateNodesWithoutAllFlags or EnumerateNodesWithoutAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public HierarchyViewNodesEnumerable EnumerateNodesWithoutFlags(HierarchyNodeFlags flags) => EnumerateNodesWithoutAllFlags(flags);

        [Obsolete("GetIndicesWithoutFlags is obsolete, please use GetIndicesWithoutAllFlags or GetIndicesWithoutAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int GetIndicesWithoutFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutAllFlagsSpan(flags, outIndices);

        [Obsolete("GetIndicesWithoutFlags is obsolete, please use GetIndicesWithoutAllFlags or GetIndicesWithoutAnyFlags instead", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int[] GetIndicesWithoutFlags(HierarchyNodeFlags flags) => GetIndicesWithoutAllFlags(flags);
        #endregion
    }
}
