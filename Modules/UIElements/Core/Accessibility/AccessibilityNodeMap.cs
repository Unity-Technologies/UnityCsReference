// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Accessibility;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The bidirectional association between <see cref="VisualElement"/>s and the
    /// <see cref="AccessibilityNode"/>s generated for them.
    /// </summary>
    /// <remarks>
    /// The element-to-node direction serves lookups from UI-side change signals; the node-to-element
    /// direction serves removals when part of the hierarchy is regenerated in place, where a node's
    /// element may already have been re-mapped under a new parent (reparenting regenerates both the
    /// old and the new context, in either order). Node object references are the durable identity —
    /// node ids are recycled and must never be persisted as keys.
    /// </remarks>
    internal class AccessibilityNodeMap
    {
        readonly Dictionary<VisualElement, AccessibilityNode> m_NodeByElement = new();
        readonly Dictionary<AccessibilityNode, VisualElement> m_ElementByNode = new();

        public int count => m_NodeByElement.Count;

        // Concrete collection types on purpose: foreach binds the dictionary's struct enumerator,
        // where the interface would box it on every iteration of the per-frame refresh path.
        public Dictionary<VisualElement, AccessibilityNode>.KeyCollection elements => m_NodeByElement.Keys;

        /// <summary>
        /// Enumerates the element-to-node associations (pattern-based, so the paired reverse map
        /// stays encapsulated while foreach stays allocation-free).
        /// </summary>
        public Dictionary<VisualElement, AccessibilityNode>.Enumerator GetEnumerator() => m_NodeByElement.GetEnumerator();

        public AccessibilityNode this[VisualElement element] => m_NodeByElement[element];

        public bool ContainsKey(VisualElement element) => m_NodeByElement.ContainsKey(element);

        public bool TryGetNode(VisualElement element, out AccessibilityNode node) =>
            m_NodeByElement.TryGetValue(element, out node);

        public bool TryGetElement(AccessibilityNode node, out VisualElement element) =>
            m_ElementByNode.TryGetValue(node, out element);

        public void Set(VisualElement element, AccessibilityNode node)
        {
            // Re-mapping an element (its subtree regenerated in place) leaves a stale reverse entry
            // behind; it is dropped here so RemoveNode can trust the reverse direction.
            if (m_NodeByElement.TryGetValue(element, out var previousNode))
                m_ElementByNode.Remove(previousNode);

            m_NodeByElement[element] = node;
            m_ElementByNode[node] = element;
        }

        /// <summary>
        /// Removes the node's mapping. The forward entry is only removed while it still points at
        /// this node — an element regenerated under a new context before its old node was removed
        /// keeps its fresh mapping. Returns true (with the element) only when the forward entry was
        /// removed, which is the caller's cue to release per-element hooks.
        /// </summary>
        public bool RemoveNode(AccessibilityNode node, out VisualElement element)
        {
            if (!m_ElementByNode.Remove(node, out element))
                return false;

            if (m_NodeByElement.TryGetValue(element, out var currentNode) && currentNode == node)
            {
                m_NodeByElement.Remove(element);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            m_NodeByElement.Clear();
            m_ElementByNode.Clear();
        }
    }
}
