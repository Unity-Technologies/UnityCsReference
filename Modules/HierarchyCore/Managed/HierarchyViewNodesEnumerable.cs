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
        public struct Enumerator
        {
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly HierarchyViewModel m_HierarchyViewModel;
            readonly int m_Version;
            readonly HierarchyNode m_Root;
            readonly HierarchyNodeFlags m_Flags;
            readonly Predicate m_Predicate;
            int m_Index;

            internal Enumerator(HierarchyViewNodesEnumerable enumerable)
            {
                m_HierarchyFlattened = enumerable.m_HierarchyViewModel.HierarchyFlattened;
                m_HierarchyViewModel = enumerable.m_HierarchyViewModel;
                m_Version = m_HierarchyViewModel.Version;
                m_Root = m_HierarchyFlattened.Hierarchy.Root;
                m_Flags = enumerable.m_Flags;
                m_Predicate = enumerable.m_Predicate;
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
                    ThrowIfVersionChanged();
                    return ref HierarchyFlattenedNode.GetNodeByRef(in m_HierarchyFlattened[m_Index]);
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

                var count = m_HierarchyFlattened.Count;
                for (;;)
                {
                    if (++m_Index >= count)
                        return false;

                    var node = m_HierarchyFlattened[m_Index].Node;
                    if (node == m_Root)
                        continue;

                    if (m_Predicate(in node, m_Flags))
                        return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ThrowIfVersionChanged()
            {
                if (m_Version != m_HierarchyViewModel.Version)
                    throw new InvalidOperationException("HierarchyViewModel was modified.");
            }
        }
    }
}
