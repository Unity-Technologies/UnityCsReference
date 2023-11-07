// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents an enumerable over the children of an <see cref="HierarchyNode"/>.
    /// </summary>
    public readonly struct HierarchyFlattenedNodeChildren :
        IEnumerable<HierarchyNode>,
        IReadOnlyCollection<HierarchyNode>
    {
        readonly HierarchyFlattened m_HierarchyFlattened;
        readonly HierarchyNode m_Node;

        /// <summary>
        /// Gets the number of children.
        /// </summary>
        public int Count => m_HierarchyFlattened.GetChildrenCount(in m_Node);

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
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(m_HierarchyFlattened, in m_Node);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<HierarchyNode>
        {
            readonly HierarchyFlattened m_HierarchyFlattened;
            readonly HierarchyNode m_Node;
            HierarchyFlattenedNode m_Current;
            int m_CurrentIndex;
            int m_ChildrenIndex;
            int m_ChildrenCount;

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public HierarchyNode Current => m_Current.Node;

            object IEnumerator.Current => Current;

            internal Enumerator(HierarchyFlattened hierarchyFlattened, in HierarchyNode node)
            {
                m_HierarchyFlattened = hierarchyFlattened;
                m_Node = node;
                m_Current = HierarchyFlattenedNode.Null;
                m_CurrentIndex = -1;
                m_ChildrenIndex = 0;
                m_ChildrenCount = 0;
            }

            [ExcludeFromDocs]
            public void Dispose() { }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            public bool MoveNext()
            {
                if (m_CurrentIndex == -1)
                {
                    var index = m_HierarchyFlattened.IndexOf(in m_Node);
                    if (index == -1)
                        return false;

                    var flatNode = m_HierarchyFlattened.ElementAt(index);
                    if (flatNode == HierarchyFlattenedNode.Null || flatNode.ChildrenCount <= 0)
                        return false;

                    if (index + 1 >= m_HierarchyFlattened.Count)
                        return false;

                    m_CurrentIndex = index + 1;
                    m_Current = m_HierarchyFlattened.ElementAt(m_CurrentIndex);
                    m_ChildrenIndex = 0;
                    m_ChildrenCount = flatNode.ChildrenCount;
                    return true;
                }

                if (m_ChildrenIndex + 1 >= m_ChildrenCount || m_Current.NextSiblingOffset <= 0)
                {
                    return false;
                }
                else
                {
                    m_CurrentIndex += m_Current.NextSiblingOffset;
                    m_Current = m_HierarchyFlattened.ElementAt(m_CurrentIndex);
                    m_ChildrenIndex++;
                    return true;
                }
            }

            /// <summary>
            /// Reset iteration at the beginning.
            /// </summary>
            public void Reset() => m_CurrentIndex = -1;

            /// <summary>
            /// Check if iteration is done.
            /// </summary>
            /// <returns>Returns true if iteration is done.</returns>
            public bool Done() => m_ChildrenIndex == m_ChildrenCount;
        }

        IEnumerator<HierarchyNode> IEnumerable<HierarchyNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
