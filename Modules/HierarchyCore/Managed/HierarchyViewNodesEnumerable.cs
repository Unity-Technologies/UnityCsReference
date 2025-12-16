// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Unity.Hierarchy
{
    #region Marked as obsolete warning in 6.3
    /// <summary>
    /// Represent an enumerable of <see cref="HierarchyNode"/> with specific <see cref="HierarchyNodeFlags"/>.
    /// </summary>
    [Obsolete("HierarchyViewNodesEnumerable is obsolete, it has been renamed to HierarchyViewModelNodesEnumerable.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct HierarchyViewNodesEnumerable
    {
        /// <summary>
        /// Delegate to filter <see cref="HierarchyNode"/> with specific <see cref="HierarchyNodeFlags"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if the node passes the predicate, <see langword="false"/> otherwise</returns>
        internal delegate bool PredicateCallback(in HierarchyNode node, HierarchyNodeFlags flags);

        readonly HierarchyViewModel m_HierarchyViewModel;
        readonly PredicateCallback m_Predicate;
        readonly HierarchyNodeFlags m_Flags;

        internal HierarchyViewNodesEnumerable(HierarchyViewModel viewModel, HierarchyNodeFlags flags, PredicateCallback predicate)
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
            readonly HierarchyViewModel m_HierarchyViewModel;
            readonly PredicateCallback m_Predicate;
            readonly HierarchyNodeFlags m_Flags;
            readonly ReadOnlyNativeVector<HierarchyFlattenedNode> m_FlattenedNodes;
            readonly int m_Version;
            int m_Index;

            internal Enumerator(HierarchyViewNodesEnumerable enumerable)
            {
                m_HierarchyViewModel = enumerable.m_HierarchyViewModel;
                m_Predicate = enumerable.m_Predicate;
                m_Flags = enumerable.m_Flags;
                m_FlattenedNodes = m_HierarchyViewModel.FlattenedNodes;
                m_Version = m_HierarchyViewModel.Version;
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
                    return ref HierarchyFlattenedNode.GetNodeByRef(in m_FlattenedNodes[m_Index]);
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
                while (true)
                {
                    if (++m_Index >= m_FlattenedNodes.Count)
                        return false;

                    if (m_Predicate(in HierarchyFlattenedNode.GetNodeByRef(in m_FlattenedNodes[m_Index]), m_Flags))
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
    #endregion
}
