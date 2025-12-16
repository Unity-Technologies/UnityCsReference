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
    public readonly struct HierarchyFlattenedChildrenEnumerable
    {
        readonly HierarchyFlattened m_HierarchyFlattened;
        readonly HierarchyFlattenedNode m_ParentNode;
        readonly int m_ParentIndex;

        /// <summary>
        /// Constructs a new <see cref="HierarchyFlattenedChildrenEnumerable"/>.
        /// </summary>
        /// <param name="hierarchyFlattened">The hierarchy flattened.</param>
        /// <param name="node">The node whose children we want to enumerate.</param>
        internal HierarchyFlattenedChildrenEnumerable(HierarchyFlattened hierarchyFlattened, in HierarchyNode node)
        {
            if (hierarchyFlattened == null || !hierarchyFlattened.IsCreated)
                throw new ArgumentNullException(nameof(hierarchyFlattened));

            if (node == HierarchyNode.Null)
                throw new ArgumentNullException(nameof(node));

            if (!hierarchyFlattened.Contains(in node))
                throw new InvalidOperationException($"{node} not found");

            m_HierarchyFlattened = hierarchyFlattened;
            m_ParentIndex = m_HierarchyFlattened.IndexOf(in node);
            m_ParentNode = m_HierarchyFlattened[m_ParentIndex];
        }

        /// <summary>
        /// Gets the number of children.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ParentNode.ChildrenCount;
        }

        /// <summary>
        /// Gets the <see cref="HierarchyFlattenedNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>.
        /// </summary>
        public struct Enumerator
        {
            readonly HierarchyFlattenedChildrenEnumerable m_Enumerable;
            readonly int m_End;
            readonly int m_Depth;
            readonly int m_Version;
            int m_Current;

            internal Enumerator(HierarchyFlattenedChildrenEnumerable enumerable)
            {
                m_Enumerable = enumerable;
                m_End = m_Enumerable.m_ParentIndex + m_Enumerable.m_ParentNode.NextSiblingOffset;
                m_Depth = m_Enumerable.m_ParentNode.Depth + 1;
                m_Version = m_Enumerable.m_HierarchyFlattened.Version;
                m_Current = m_Enumerable.m_ParentIndex;
            }

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public ref readonly HierarchyFlattenedNode Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ThrowIfVersionChanged();
                    return ref m_Enumerable.m_HierarchyFlattened[m_Current];
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ThrowIfVersionChanged();
                if (m_Current == m_Enumerable.m_ParentIndex)
                    m_Current++; // First MoveNext
                else
                    m_Current += m_Enumerable.m_HierarchyFlattened[m_Current].NextSiblingOffset;
                return m_Current < m_End;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                ThrowIfVersionChanged();
                m_Current = m_Enumerable.m_ParentIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ThrowIfVersionChanged()
            {
                if (m_Version != m_Enumerable.m_HierarchyFlattened.Version)
                    throw new InvalidOperationException("HierarchyFlattened was modified during enumeration.");
            }
        }
    }
}
