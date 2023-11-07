// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represent an enumerable of <see cref="HierarchyNode"/> with specific <see cref="HierarchyNodeFlags"/>.
    /// </summary>
    public readonly struct HierarchyViewNodesEnumerable : IEnumerable<HierarchyNode>
    {
        /// <summary>
        /// Delegate to filter <see cref="HierarchyNode"/> with specific <see cref="HierarchyNodeFlags"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if the node passes the predicate, <see langword="false"/> otherwise</returns>
        internal delegate bool Predicate(in HierarchyNode node, HierarchyNodeFlags flags);

        readonly HierarchyViewModel m_HierarchyViewModel;
        readonly HierarchyNodeFlags m_Flags;
        readonly Predicate m_Predicate;

        internal HierarchyViewNodesEnumerable(HierarchyViewModel viewModel, HierarchyNodeFlags flags, Predicate predicate)
        {
            m_HierarchyViewModel = viewModel;
            m_Flags = flags;
            m_Predicate = predicate;
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<HierarchyNode>
        {
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly HierarchyNode m_Root;
            readonly HierarchyNodeFlags m_Flags;
            readonly Predicate m_Predicate;
            int m_Index;

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public HierarchyNode Current => m_HierarchyFlattened[m_Index].Node;

            object IEnumerator.Current => Current;

            internal Enumerator(HierarchyViewNodesEnumerable enumerable)
            {
                m_HierarchyFlattened = enumerable.m_HierarchyViewModel.HierarchyFlattened;
                m_Root = m_HierarchyFlattened.Hierarchy.Root;
                m_Flags = enumerable.m_Flags;
                m_Predicate = enumerable.m_Predicate;
                m_Index = -1;
            }

            [ExcludeFromDocs]
            public void Dispose() { }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            public bool MoveNext()
            {
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

            /// <summary>
            /// Reset iteration at the beginning.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// Check if iteration is done.
            /// </summary>
            /// <returns><see langword="true"/> if iteration is done, <see langword="false"/> otherwise.</returns>
            public bool Done() => m_Index == m_HierarchyFlattened.Count;
        }

        IEnumerator<HierarchyNode> IEnumerable<HierarchyNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
