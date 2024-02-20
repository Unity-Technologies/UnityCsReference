// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A read-only collection of an hierarchy node's children.
    /// </summary>
    /// <remarks>
    /// If the hierarchy is modified, the collection is invalidated.
    /// </remarks>
    public unsafe readonly struct HierarchyNodeChildren
    {
        const int k_HierarchyNodeChildrenIsAllocBit = 1 << 31;

        readonly Hierarchy m_Hierarchy;
        readonly HierarchyNode* m_Ptr;
        readonly int m_Version;
        readonly int m_Count;

        /// <summary>
        /// The number of children.
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
        public ref readonly HierarchyNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ThrowIfVersionChanged();
                return ref m_Ptr[index];
            }
        }

        /// <summary>
        /// Gets an enumerator for the children.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        internal HierarchyNodeChildren(Hierarchy hierarchy, IntPtr ptr)
        {
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));

            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));

            m_Hierarchy = hierarchy;
            m_Version = hierarchy.Version;

            ref var alloc = ref UnsafeUtility.AsRef<HierarchyNodeChildrenAlloc>(ptr.ToPointer());
            if ((alloc.Reserved[0] & k_HierarchyNodeChildrenIsAllocBit) == k_HierarchyNodeChildrenIsAllocBit)
            {
                m_Ptr = alloc.Ptr;
                m_Count = alloc.Size;
            }
            else
            {
                m_Ptr = (HierarchyNode*)ptr.ToPointer();
                m_Count = 0;
                for (int i = 0; i < HierarchyNodeChildrenFixed.Capacity; i++)
                {
                    if (m_Ptr[i] != HierarchyNode.Null)
                        m_Count++;
                    else
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfVersionChanged()
        {
            if (m_Version != m_Hierarchy.Version)
                throw new InvalidOperationException("Hierarchy was modified.");
        }

        /// <summary>
        /// An enumerator for an hierarchy node's children.
        /// </summary>
        public struct Enumerator
        {
            readonly HierarchyNodeChildren m_Enumerable;
            int m_Index;

            internal Enumerator(in HierarchyNodeChildren enumerable)
            {
                m_Enumerable = enumerable;
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
                    m_Enumerable.ThrowIfVersionChanged();
                    return ref m_Enumerable.m_Ptr[m_Index];
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                m_Enumerable.ThrowIfVersionChanged();
                return ++m_Index < m_Enumerable.m_Count;
            }
        }
    }
}
