// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
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
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyFlattened.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyFlattenedBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyFlattened :
        IDisposable,
        IEnumerable<HierarchyFlattenedNode>,
        IReadOnlyCollection<HierarchyFlattenedNode>,
        IReadOnlyList<HierarchyFlattenedNode>
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyFlattened hierarchyFlattened) => hierarchyFlattened.m_Ptr;
        }

        [RequiredByNativeCode] IntPtr m_Ptr;
        [RequiredByNativeCode] readonly bool m_IsWrapper;
        [RequiredByNativeCode] readonly Hierarchy m_Hierarchy;

        [FreeFunction("HierarchyFlattenedBindings::Create")]
        static extern IntPtr Internal_Create(Hierarchy hierarchy);

        [FreeFunction("HierarchyFlattenedBindings::Destroy")]
        static extern void Internal_Destroy(IntPtr ptr);

        /// <summary>
        /// Whether this object is valid and uses memory or not.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The total number of nodes.
        /// </summary>
        /// <remarks>
        /// The total includes the <see cref="Hierarchy.Root"/> node.
        /// </remarks>
        public extern int Count { [NativeMethod("Count")] get; }

        /// <summary>
        /// Whether the flattened hierarchy is currently updating.
        /// </summary>
        /// <remarks>
        /// Happens during use of <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/>.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating")] get; }

        /// <summary>
        /// Determines if the flattened hierarchy needs an update.
        /// </summary>
        /// <remarks>
        /// Happens when the underlying hierarchy changes topology.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded")] get; }

        /// <summary>
        /// Accesses the hierarchy.
        /// </summary>
        public Hierarchy Hierarchy => m_Hierarchy;

        /// <summary>
        /// Creates a new <see cref="HierarchyFlattened"/> from a <see cref="Hierarchy"/>.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        public HierarchyFlattened(Hierarchy hierarchy)
        {
            m_Ptr = Internal_Create(hierarchy);
            m_Hierarchy = hierarchy;
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
                if (!m_IsWrapper)
                    Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the <see cref="HierarchyFlattenedNode"/> at a specified index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A flattened hierarchy node.</returns>
        public HierarchyFlattenedNode this[int index] => ElementAt(index);

        /// <summary>
        /// Gets the zero-based index of a specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A zero-based index of the node if found, and -1 otherwise.</returns>
        [NativeThrows]
        public extern int IndexOf(in HierarchyNode node);

        /// <summary>
        /// Determines if a specified node is in the hierarchy flattened.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is found, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Contains(in HierarchyNode node);

        /// <summary>
        /// Gets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Gets the next sibling of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Gets an enumerable of children <see cref="HierarchyNode"/> for the specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The children enumerable.</returns>
        public HierarchyFlattenedNodeChildren EnumerateChildren(in HierarchyNode node) => new HierarchyFlattenedNodeChildren(this, node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of children.</returns>
        [NativeThrows]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has, including children of children.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of child nodes, including children of children.</returns>
        [NativeThrows]
        public extern int GetChildrenCountRecursive(in HierarchyNode node);

        /// <summary>
        /// Determines the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node.</returns>
        [NativeThrows]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Updates the flattened hierarchy and requests a rebuild of the list of <see cref="HierarchyFlattenedNode"/> from the <see cref="Hierarchy"/> topology.
        /// </summary>
        public extern void Update();

        /// <summary>
        /// Updates the flattened hierarchy incrementally.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncremental();

        /// <summary>
        /// Incrementally updates the flattened hierarchy until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time period in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncrementalTimed(double milliseconds);

        /// <summary>
        /// Gets the <see cref="HierarchyFlattenedNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyFlattenedNode"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<HierarchyFlattenedNode>
        {
            readonly HierarchyFlattened m_HierarchyFlattened;
            int m_Index;

            /// <summary>
            /// Gets the current iterator item.
            /// </summary>
            public HierarchyFlattenedNode Current => m_HierarchyFlattened[m_Index];

            object IEnumerator.Current => Current;

            internal Enumerator(HierarchyFlattened hierarchyBaked)
            {
                m_HierarchyFlattened = hierarchyBaked;
                m_Index = -1;
            }

            [ExcludeFromDocs]
            public void Dispose() { }

            /// <summary>
            /// Moves iterator to the next item.
            /// </summary>
            /// <returns>Returns true if the current value is valid. </returns>
            public bool MoveNext() => ++m_Index < m_HierarchyFlattened.Count;

            /// <summary>
            /// Resets iterator to the beginning of the sequence.
            /// </summary>
            public void Reset() => m_Index = -1;
        }

        IEnumerator<HierarchyFlattenedNode> IEnumerable<HierarchyFlattenedNode>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [NativeThrows]
        internal extern HierarchyFlattenedNode ElementAt(int index);
    }
}
