// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A read-only collection of an hierarchy node's children.
    /// </summary>
    /// <remarks>
    /// If the hierarchy is modified, the collection is invalidated.
    /// </remarks>
    public unsafe readonly struct HierarchyNodeChildren :
        IEnumerable<HierarchyNode>,
        IReadOnlyCollection<HierarchyNode>,
        IReadOnlyList<HierarchyNode>
    {
        const int k_HierarchyNodeChildrenIsAllocBit = 1 << 31;

        readonly Hierarchy m_Hierarchy;
        readonly int m_Version;
        readonly IntPtr m_Ptr;
        readonly HierarchyNode* m_NodePtr;
        readonly int m_Count;

        /// <summary>
        /// The number of children.
        /// </summary>
        public int Count
        {
            get
            {
                ThrowIfHierarchyChanged();
                return m_Count;
            }
        }

        /// <summary>
        /// Gets the child at the specified index.
        /// </summary>
        /// <param name="index">The children index.</param>
        /// <returns>The child hierarchy node.</returns>
        public HierarchyNode this[int index]
        {
            get
            {
                ThrowIfHierarchyChanged();

                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return m_NodePtr[index];
            }
        }

        internal HierarchyNodeChildren(Hierarchy hierarchy, int version, IntPtr ptr)
        {
            m_Hierarchy = hierarchy;
            m_Version = version;
            m_Ptr = ptr;

            ref var alloc = ref UnsafeUtility.AsRef<HierarchyNodeChildrenAlloc>(m_Ptr.ToPointer());
            if ((alloc.Reserved[0] & k_HierarchyNodeChildrenIsAllocBit) == k_HierarchyNodeChildrenIsAllocBit)
            {
                m_NodePtr = alloc.Ptr;
                m_Count = alloc.Size;
            }
            else
            {
                m_NodePtr = (HierarchyNode*)ptr.ToPointer();
                var i = 0;
                while (i < HierarchyNodeChildrenFixed.Capacity && m_NodePtr[i].Id != 0)
                    i++;
                m_Count = i;
            }
        }

        /// <summary>
        /// Gets an enumerator for the children.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator for an hierarchy node's children.
        /// </summary>
        public struct Enumerator : IEnumerator<HierarchyNode>
        {
            readonly HierarchyNodeChildren m_Children;
            int m_Index;

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public HierarchyNode Current => m_Children[m_Index];

            object IEnumerator.Current => Current;

            internal Enumerator(in HierarchyNodeChildren children)
            {
                m_Children = children;
                m_Index = -1;
            }

            [ExcludeFromDocs]
            public void Dispose() { }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, otherwise <see langword="false"/>.</returns>
            public bool MoveNext() => ++m_Index < m_Children.Count;

            /// <summary>
            /// Reset iteration at the beginning.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// Check if iteration is done.
            /// </summary>
            /// <returns><see langword="true"/> if iteration is done, otherwise <see langword="false"/>.</returns>
            public bool Done() => m_Index >= m_Children.Count;
        }

        IEnumerator<HierarchyNode> IEnumerable<HierarchyNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ThrowIfHierarchyChanged()
        {
            if (m_Version != m_Hierarchy.Version)
                throw new InvalidOperationException("Hierarchy was modified.");
        }
    }
}
