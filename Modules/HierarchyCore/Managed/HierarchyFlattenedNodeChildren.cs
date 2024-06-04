// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents an enumerable over the children of an <see cref="HierarchyNode"/>.
    /// </summary>
    public readonly struct HierarchyFlattenedNodeChildren
    {
        readonly HierarchyFlattened m_HierarchyFlattened;
        readonly HierarchyNode m_Node;
        readonly int m_Version;
        readonly int m_Count;

        internal HierarchyFlattenedNodeChildren(HierarchyFlattened hierarchyFlattened, in HierarchyNode node)
        {
            if (hierarchyFlattened == null)
                throw new ArgumentNullException(nameof(hierarchyFlattened));

            if (node == HierarchyNode.Null)
                throw new ArgumentNullException(nameof(node));

            if (!hierarchyFlattened.Contains(in node))
                throw new InvalidOperationException($"node {node.Id}:{node.Version} not found");

            m_HierarchyFlattened = hierarchyFlattened;
            m_Node = node;
            m_Version = hierarchyFlattened.Version;
            m_Count = m_HierarchyFlattened.GetChildrenCount(in m_Node);
        }

        /// <summary>
        /// Gets the number of children.
        /// </summary>
        public int Count
        {
            get
            {
                ThrowIfVersionChanged();
                return m_Count;
            }
        }

        /// <summary>
        /// Gets the child at the specified index.
        /// </summary>
        /// <param name="index">The children index.</param>
        /// <returns>The child hierarchy node.</returns>
        public ref readonly HierarchyFlattenedNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ThrowIfVersionChanged();
                return ref m_HierarchyFlattened[index];
            }
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this, m_Node);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>.
        /// </summary>
        public struct Enumerator
        {
            readonly HierarchyFlattenedNodeChildren m_Enumerable;
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly HierarchyNode m_Node;
            int m_CurrentIndex;
            int m_ChildrenIndex;
            int m_ChildrenCount;

            internal Enumerator(HierarchyFlattenedNodeChildren enumerable, HierarchyNode node)
            {
                m_Enumerable = enumerable;
                m_HierarchyFlattened = enumerable.m_HierarchyFlattened;
                m_Node = node;
                m_CurrentIndex = -1;
                m_ChildrenIndex = 0;
                m_ChildrenCount = 0;
            }

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public ref readonly HierarchyNode Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    m_Enumerable.ThrowIfVersionChanged();
                    return ref HierarchyFlattenedNode.GetNodeByRef(in m_HierarchyFlattened[m_CurrentIndex]);
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            public bool MoveNext()
            {
                m_Enumerable.ThrowIfVersionChanged();

                if (m_CurrentIndex == -1)
                {
                    var index = m_HierarchyFlattened.IndexOf(in m_Node);
                    if (index == -1)
                        return false;

                    ref readonly var flatNode = ref m_HierarchyFlattened[index];
                    if (flatNode == HierarchyFlattenedNode.Null || flatNode.ChildrenCount <= 0)
                        return false;

                    if (index + 1 >= m_HierarchyFlattened.Count)
                        return false;

                    m_CurrentIndex = index + 1;

                    m_ChildrenIndex = 0;
                    m_ChildrenCount = flatNode.ChildrenCount;
                    return true;
                }

                ref readonly var currentFlatNode = ref m_HierarchyFlattened[m_CurrentIndex];
                if (m_ChildrenIndex + 1 >= m_ChildrenCount || currentFlatNode.NextSiblingOffset <= 0)
                {
                    return false;
                }
                else
                {
                    m_CurrentIndex += currentFlatNode.NextSiblingOffset;
                    m_ChildrenIndex++;

                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfVersionChanged()
        {
            if (m_Version != m_HierarchyFlattened.Version)
                throw new InvalidOperationException("HierarchyFlattened was modified.");
        }
    }
}
