// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a read-only array of <see cref="HierarchyFlattenedNode"/> over a <see cref="Hierarchy"/>. Used as an acceleration structure for query purposes.
    /// </summary>
    /// <remarks>
    /// Querying information about nodes completes much faster than the same methods
    /// on <see cref="Hierarchy"/> because they are stored during the updates.
    /// </remarks>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyFlattened.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyFlattenedBindings.h")]
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyFlattened : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyFlattened hierarchyFlattened) => hierarchyFlattened.m_Ptr;
        }

        IntPtr m_Ptr;
        internal readonly Hierarchy m_Hierarchy;
        ReadOnlyNativeVector<HierarchyFlattenedNode> m_FlattenedNodes;
        int m_Version;
        readonly bool m_IsOwner;

        /// <summary>
        /// Whether this object is valid and uses memory or not.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The total number of flattened nodes.
        /// </summary>
        /// <remarks>
        /// The total includes the <see cref="Hierarchy.Root"/> node.
        /// </remarks>
        public int Count => m_FlattenedNodes.Count;

        /// <summary>
        /// Whether the flattened hierarchy is currently updating.
        /// </summary>
        /// <remarks>
        /// Happens during use of <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/>.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating", IsThreadSafe = true)] get; }

        /// <summary>
        /// Determines if the flattened hierarchy needs an update.
        /// </summary>
        /// <remarks>
        /// Happens when the underlying hierarchy changes topology.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded", IsThreadSafe = true)] get; }

        internal ReadOnlyNativeVector<HierarchyFlattenedNode> FlattenedNodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FlattenedNodes;
        }

        internal int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Version;
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyFlattened"/> from a hierarchy.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        public HierarchyFlattened(Hierarchy hierarchy)
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), hierarchy, out var nodesPtr, out var nodesCount, out var version);
            m_Hierarchy = hierarchy;
            m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(nodesPtr, nodesCount);
            m_Version = version;
            m_IsOwner = true;
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyFlattened"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="flattenedNodesPtr">The flattened nodes native pointer.</param>
        /// <param name="flattenedNodesCount">The flattened node count.</param>
        /// <param name="version">The version.</param>
        HierarchyFlattened(IntPtr nativePtr, Hierarchy hierarchy, IntPtr flattenedNodesPtr, int flattenedNodesCount, int version)
        {
            m_Ptr = nativePtr;
            m_Hierarchy = hierarchy;
            m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            m_Version = version;
            m_IsOwner = false;
        }

        ~HierarchyFlattened()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes this object to release its memory.
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
        }

        /// <summary>
        /// Gets the <see cref="HierarchyFlattenedNode"/> at a specified index.
        /// </summary>
        /// <param name="index">The flattened node index.</param>
        /// <returns>A flattened hierarchy node.</returns>
        public ref readonly HierarchyFlattenedNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_FlattenedNodes[index];
        }

        /// <summary>
        /// Gets the zero-based index of a specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A zero-based index of the node if found, and -1 otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int IndexOf(in HierarchyNode node);

        /// <summary>
        /// Determines if a specified node is in the hierarchy flattened.
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
        /// Gets an enumerable of children <see cref="HierarchyFlattenedNode"/> for the specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The children enumerable.</returns>
        public HierarchyFlattenedChildrenEnumerable EnumerateChildren(in HierarchyNode node) => new HierarchyFlattenedChildrenEnumerable(this, node);

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
        /// Updates the flattened hierarchy and requests a rebuild of the list of <see cref="HierarchyFlattenedNode"/> from the <see cref="Hierarchy"/> topology.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Update();

        /// <summary>
        /// Updates the flattened hierarchy incrementally.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncremental();

        /// <summary>
        /// Incrementally updates the flattened hierarchy until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time period in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncrementalTimed(double milliseconds);

        /// <summary>
        /// Gets the <see cref="HierarchyFlattenedNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyFlattenedNode"/>.
        /// </summary>
        public struct Enumerator
        {
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly ReadOnlyNativeVector<HierarchyFlattenedNode> m_FlattenedNodes;
            readonly int m_Version;
            int m_Index;

            internal Enumerator(HierarchyFlattened hierarchyFlattened)
            {
                m_HierarchyFlattened = hierarchyFlattened;
                m_FlattenedNodes = hierarchyFlattened.m_FlattenedNodes;
                m_Version = hierarchyFlattened.m_Version;
                m_Index = -1;
            }

            /// <summary>
            /// Gets the current iterator item.
            /// </summary>
            public ref readonly HierarchyFlattenedNode Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (m_Version != m_HierarchyFlattened.m_Version)
                        throw new InvalidOperationException("HierarchyFlattened was modified.");

                    return ref m_FlattenedNodes[m_Index];
                }
            }

            /// <summary>
            /// Moves iterator to the next item.
            /// </summary>
            /// <returns>Returns true if the current value is valid. </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_FlattenedNodes.Count;
        }

        /// <summary>
        /// Returns a read-only span of all hierarchy flattened nodes in the hierarchy flattened.
        /// </summary>
        /// <returns>A read-only span of hierarchy flattened nodes.</returns>
        public ReadOnlySpan<HierarchyFlattenedNode> AsReadOnlySpan() => m_FlattenedNodes.AsReadOnlySpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyFlattened FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyFlattened)GCHandle.FromIntPtr(handlePtr).Target : null;

        [FreeFunction("HierarchyFlattenedBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, Hierarchy hierarchy, out IntPtr nodesPtr, out int nodesCount, out int version);

        [FreeFunction("HierarchyFlattenedBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateHierarchyFlattened(IntPtr nativePtr, IntPtr hierarchyPtr, IntPtr flattenedNodesPtr, int flattenedNodesCount, int version) =>
            GCHandle.ToIntPtr(GCHandle.Alloc(new HierarchyFlattened(nativePtr, Hierarchy.FromIntPtr(hierarchyPtr), flattenedNodesPtr, flattenedNodesCount, version)));

        [RequiredByNativeCode]
        static void UpdateHierarchyFlattened(IntPtr handlePtr, IntPtr flattenedNodesPtr, int flattenedNodesCount, int version)
        {
            var hierarchyFlattened = FromIntPtr(handlePtr);
            hierarchyFlattened.m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            hierarchyFlattened.m_Version = version;
        }
        #endregion

        #region Marked as obsolete error in 6.6
        [Obsolete("The Hierarchy property will be removed in the future, remove its usage from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Hierarchy Hierarchy { get => throw null; }
        #endregion
    }
}
