// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represent an enumerable of <see cref="HierarchyNode"/> with specific <see cref="HierarchyNodeFlags"/>.
    /// </summary>
    public readonly struct HierarchyViewNodesEnumerable
    {
        /// <summary>
        /// Delegate to filter <see cref="HierarchyNode"/> with specific <see cref="HierarchyNodeFlags"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if the node passes the predicate, <see langword="false"/> otherwise</returns>
        internal delegate bool Predicate(in HierarchyNode node, HierarchyNodeFlags flags);

        readonly HierarchyViewModel m_HierarchyViewModel;
        readonly Predicate m_Predicate;
        readonly HierarchyNodeFlags m_Flags;

        internal HierarchyViewNodesEnumerable(HierarchyViewModel viewModel, HierarchyNodeFlags flags, Predicate predicate)
        {
            m_HierarchyViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            m_Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            m_Flags = flags;
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>.
        /// </summary>
        public unsafe struct Enumerator
        {
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly Predicate m_Predicate;
            readonly HierarchyNodeFlags m_Flags;
            readonly HierarchyFlattenedNode* m_NodesPtr;
            readonly int m_NodesCount;
            readonly int m_Version;
            int m_Index;

            internal Enumerator(HierarchyViewNodesEnumerable enumerable)
            {
                m_HierarchyFlattened = enumerable.m_HierarchyViewModel.HierarchyFlattened;
                m_Predicate = enumerable.m_Predicate;
                m_Flags = enumerable.m_Flags;
                m_NodesPtr = m_HierarchyFlattened.NodesPtr;
                m_NodesCount = m_HierarchyFlattened.Count;
                m_Version = m_HierarchyFlattened.Version;
                m_Index = 0; // We initialize at 0 instead of -1 to skip the root node in the hierarchy flattened
            }

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public ref readonly HierarchyNode Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ThrowIfVersionChanged();
                    return ref HierarchyFlattenedNode.GetNodeByRef(in m_NodesPtr[m_Index]);
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ThrowIfVersionChanged();
                for (;;)
                {
                    if (++m_Index >= m_NodesCount)
                        return false;

                    if (m_Predicate(in HierarchyFlattenedNode.GetNodeByRef(in m_NodesPtr[m_Index]), m_Flags))
                        return true;
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
}
