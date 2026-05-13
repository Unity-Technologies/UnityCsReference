// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Represents the hierarchy data model that the screen reader uses for reading and navigating the UI.
    /// </summary>
    /// <remarks>
    /// A hierarchy must be set to active through <see cref="AssistiveSupport.activeHierarchy"/> when the screen
    /// reader is on for the screen reader to function. If a hierarchy is not set active, the screen reader cannot read and
    /// navigate through the UI. Once an active hierarchy is set, if the hierarchy is modified, the screen reader must
    /// be notified by calling <see cref="AssistiveSupport.NotificationDispatcher.SendLayoutChanged"/> or
    /// <see cref="AssistiveSupport.NotificationDispatcher.SendScreenChanged"/> (depending if the changes are only at
    /// the layout level, or a more considerable screen change). Modifications in the hierarchy consist of calls to:
    ///
    ///- <see cref="AccessibilityHierarchy.AddNode"/>
    ///- <see cref="AccessibilityHierarchy.Clear"/>
    ///- <see cref="AccessibilityHierarchy.InsertNode"/>
    ///- <see cref="AccessibilityHierarchy.MoveNode"/>
    ///- <see cref="AccessibilityHierarchy.RemoveNode"/>
    ///- Modifications to node <see cref="AccessibilityNode.frame"/> values.
    ///
    /// </remarks>
    public class AccessibilityHierarchy
    {
        /// <summary>
        /// The collection of nodes and associated data in the hierarchy that can be accessed by the node ID as a key.
        /// </summary>
        internal readonly Dictionary<int, AccessibilityNode> nodes = new();

        internal List<AccessibilityNode> m_RootNodes = new();

        /// <summary>
        /// The root nodes of the hierarchy.
        /// </summary>
        public IReadOnlyList<AccessibilityNode> rootNodes => m_RootNodes;

        /// <summary>
        /// One of two reusable stacks for finding the lowest common ancestor in the hierarchy
        /// </summary>
        Stack<AccessibilityNode> m_FirstLowestCommonAncestorChain = new();

        /// <summary>
        /// One of two reusable stacks for finding the lowest common ancestor in the hierarchy
        /// </summary>
        Stack<AccessibilityNode> m_SecondLowestCommonAncestorChain = new();

        /// <summary>
        /// The next unique ID that can be assigned to a new node. This is a static value and
        /// therefore shared among all instances of AccessibilityHierarchy, in order to guarantee ID uniqueness and
        /// avoid confusion of looking for an ID in a hierarchy that does not contain the node that ID originally
        /// belonged to.
        /// </summary>
        internal static int nextUniqueNodeId;

        /// <summary>
        /// The set of all node IDs currently in use across all instances of <see cref="AccessibilityHierarchy"/>.
        /// Used to guarantee global ID uniqueness even after <see cref="nextUniqueNodeId"/> wraps around
        /// <see cref="int.MaxValue"/>.
        /// </summary>
        internal static readonly HashSet<int> usedNodeIds = new();

        event Action<AccessibilityHierarchy> m_Changed;

        /// <summary>
        /// Event sent when the hierarchy changes.
        /// </summary>
        internal event Action<AccessibilityHierarchy> changed
        {
            [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
            add => m_Changed += value;
            [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
            remove => m_Changed -= value;
        }

        /// <summary>
        /// Finalizer that releases all node IDs back to the global pool when this hierarchy is garbage collected
        /// without having been explicitly cleared. Without this, IDs would accumulate in <see cref="usedNodeIds"/>
        /// permanently for every hierarchy that is created and then simply dropped (e.g. during scene transitions or
        /// in tests), eventually exhausting all available IDs.
        /// </summary>
        ~AccessibilityHierarchy()
        {
            lock (usedNodeIds)
            {
                foreach (var id in nodes.Keys)
                {
                    usedNodeIds.Remove(id);
                }
            }
        }

        internal void NotifyHierarchyChanged()
        {
            m_Changed?.Invoke(this);
        }

        /// <summary>
        /// Resets the hierarchy to an empty state, removing all the nodes and removing focus.
        /// </summary>
        public void Clear()
        {
            for (var i = m_RootNodes.Count - 1; i >= 0; i--)
            {
                RemoveNode(m_RootNodes[i]);
            }
        }

        /// <summary>
        /// Tries to get the node in this hierarchy that has the given ID.
        /// </summary>
        /// <param name="id">The ID of the node to retrieve.</param>
        /// <param name="node">The valid node with the associated ID, or @@null@@ if no such node exists in this hierarchy.</param>
        /// <returns>Returns true if a node is found and false otherwise.</returns>
        public bool TryGetNode(int id, out AccessibilityNode node)
        {
            return nodes.TryGetValue(id, out node);
        }

        /// <summary>
        /// Creates and adds a new node with the given label in this hierarchy under the given parent node. If no parent is
        /// provided, the new node is added as a root in the hierarchy.
        /// </summary>
        /// <param name="label">A label that succinctly describes the accessibility node.</param>
        /// <param name="parent">The parent of the node being added. When the value given is @@null@@, the created node
        /// is placed at the root level.</param>
        /// <returns>The node created and added.</returns>
        public AccessibilityNode AddNode(string label = null, AccessibilityNode parent = null)
        {
            return InsertNode(-1, label, parent);
        }


        /// <summary>
        /// Creates and inserts a new node with the given label at the given index in this hierarchy under the given parent node.
        /// If no parent is provided, the new node is inserted at the given index as a root in the hierarchy.
        /// </summary>
        /// <param name="childIndex">A zero-based index for positioning the inserted node in the parent's children list,
        /// or in the list of roots if the node is a root node. If the index is invalid, the inserted node will be the
        /// last child of its parent (or the last root node).</param>
        /// <param name="label">A label that succinctly describes the accessibility node.</param>
        /// <param name="parent">The parent of the node being added. When the value given is @@null@@, the created node
        /// is placed at the root level.</param>
        /// <returns>The node created and inserted.</returns>
        public AccessibilityNode InsertNode(int childIndex, string label = null, AccessibilityNode parent = null)
        {
            // Only nodes intended as roots may have an invalid parent.
            if (parent != null)
                ValidateNodeInHierarchy(parent);

            // Generate a new node to return, then add it to the manager under it's parent
            var node = CreateNode();
            nodes[node.id] = node;

            if (label != null)
            {
                node.label = label;
            }

            SetParent(node, parent, null, parent == null ? m_RootNodes : parent.childList, childIndex);

            NotifyHierarchyChanged();
            return node;
        }

        /// <summary>
        /// Moves the node elsewhere in the hierarchy, which causes the given node to be parented by a different node
        /// in the hierarchy. An optional index can be supplied for specifying the position within the list of children the
        /// moved node should take (zero-based). If no index is supplied, the node is added as the last child of the new parent by default.
        /// <para>Root nodes can be moved elsewhere in the hierarchy, therefore ceasing to be a root.
        /// Non-root nodes can be moved to become a root node by providing @@null@@ as the new parent node.</para>
        /// <para>__Warning:__ The moving operation is costly as many checks have to be executed to guarantee the integrity of
        /// the hierarchy. Therefore this operation should not be done excessively as it may affect performance.</para>
        /// </summary>
        /// <param name="node">The node to move.</param>
        /// <param name="newParent">The new parent of the moved node, or @@null@@ if the moved node should be made into a root node.</param>
        /// <param name="newChildIndex">An optional zero-based index for positioning the moved node in the new parent's children list, or in the list of
        /// roots if the node is becoming a root node. If the index is not provided or is invalid, the moved node will be the last child of its parent.</param>
        /// <returns>Whether the node was successfully moved.</returns>
        public bool MoveNode(AccessibilityNode node, AccessibilityNode newParent, int newChildIndex = -1)
        {
            ValidateNodeInHierarchy(node);

            // Check if node is valid to move
            if (node == newParent)
            {
                throw new ArgumentException($"Attempting to move the node {node} under itself.");
            }

            if (node.parent == newParent)
            {
                // If the node we are trying to move is already at the right location, we are done
                var parentList = newParent == null ? m_RootNodes : newParent.childList;
                if (newChildIndex == parentList.IndexOf(node))
                {
                    return false;
                }

                // Keep parent, move index
                CheckForLoopsAndSetParent(node, newParent, newChildIndex);
                return true;
            }

            // Making node a root
            if (newParent == null)
            {
                // If the node is already a root, there's nothing to do
                if (node.parent == null)
                {
                    return false;
                }

                CheckForLoopsAndSetParent(node, null, newChildIndex);
                return true;
            }

            ValidateNodeInHierarchy(newParent);

            // Update node relationships (SetParent checks for loops)
            CheckForLoopsAndSetParent(node, newParent, newChildIndex);

            NotifyHierarchyChanged();
            return true;
        }

        /// <summary>
        /// Removes the node from the hierarchy. Can also optionally remove nodes under the given node depending on the value of
        /// the <see cref="removeChildren"/> parameter.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <param name="removeChildren">Default value is @@true@@. If removeChildren is @@false@@, Unity grafts the child nodes to the parent.</param>
        public void RemoveNode(AccessibilityNode node, bool removeChildren = true)
        {
            ValidateNodeInHierarchy(node);

            if (removeChildren)
            {
                var nodeIdsToRemove = new List<int>();

                void RemoveFromNodes(AccessibilityNode child)
                {
                    nodes.Remove(child.id);
                    nodeIdsToRemove.Add(child.id);

                    foreach (var descendant in child.children)
                    {
                        RemoveFromNodes(descendant);
                    }
                };

                RemoveFromNodes(node);

                lock (usedNodeIds)
                {
                    foreach (var nodeId in nodeIdsToRemove)
                    {
                        usedNodeIds.Remove(nodeId);
                    }
                }
            }
            else
            {
                nodes.Remove(node.id);

                lock (usedNodeIds)
                {
                    usedNodeIds.Remove(node.id);
                }
            }

            if (m_RootNodes.Contains(node))
            {
                m_RootNodes.Remove(node);

                // If we aren't removing the children, add them as roots (AccessibilityNode.Destroy will handle updating the parent value).
                if (!removeChildren)
                {
                    m_RootNodes.AddRange(node.childList);
                }
            }

            node.Destroy(removeChildren);
            NotifyHierarchyChanged();
        }

        /// <summary>
        /// Returns whether a given node exists in the hierarchy.
        /// </summary>
        /// <param name="node">The node to search for in the hierarchy.</param>
        /// <returns>Whether the node exists in this hierarchy.</returns>
        public bool ContainsNode(AccessibilityNode node)
        {
            return node != null && nodes.TryGetValue(node.id, out var existingNode) && existingNode == node;
        }

        private void CheckForLoopsAndSetParent(AccessibilityNode node, AccessibilityNode parent, int newChildIndex = -1)
        {
            // We don't validate the nodes are in the hierarchy here as this is a private method and we guarantee this is
            // only called when we're sure both given parameters are valid.

            // Edge case: moving the node to be a root, so no need to check for loops
            if (parent == null)
            {
                SetParent(node, null, node.parent?.childList ?? m_RootNodes, m_RootNodes, newChildIndex);
                return;
            }

            // Edge case: potentially only moving the index of the child, keeping the same parent
            if (node.parent == parent)
            {
                SetParent(node, parent, parent.childList, parent.childList, newChildIndex);
                return;
            }

            // Edge case: both nodes are roots, so no need to check for loops
            if (node.parent == null && parent.parent == null)
            {
                SetParent(node, parent, m_RootNodes, parent.childList, newChildIndex);
                return;
            }

            var ancestor = parent.parent;

            // If the node exists in any of the ancestral nodes of the parent, then we are creating a loop, which is invalid.
            while (ancestor != null)
            {
                if (ancestor == node)
                {
                    throw new ArgumentException($"Trying to set the node {node} to have parent {parent}, but this would create a loop.");
                }

                ancestor = ancestor.parent;
            }

            SetParent(node, parent, node.parent?.childList ?? m_RootNodes, parent.childList, newChildIndex);
        }

        private void SetParent(AccessibilityNode node, AccessibilityNode parent, IList<AccessibilityNode> previousParentChildren, IList<AccessibilityNode> newParentChildren, int newChildIndex = -1)
        {
            // Update references for both old and new parents and child
            previousParentChildren?.Remove(node);
            node.SetParent(parent, newChildIndex);

            if (newChildIndex < 0 || newChildIndex > newParentChildren.Count)
            {
                newParentChildren.Add(node);
            }
            else
            {
                newParentChildren.Insert(newChildIndex, node);
            }
        }

        internal void AllocateNative()
        {
            foreach (var rootNode in m_RootNodes)
            {
                rootNode.AllocateNative();
            }
        }

        internal void FreeNative()
        {
            foreach (var rootNode in m_RootNodes)
            {
                // We're freeing the whole hierarchy, so children of roots also get freed.
                rootNode.FreeNative(freeChildren: true);
            }
        }

        /// <summary>
        /// Refreshes all the node frames (i.e. the screen elements' positions) for the hierarchy.
        /// </summary>
        /// <remarks>
        /// Calling this method sends a notification to the operating system that the layout has changed, by
        /// calling <see cref="IAccessibilityNotificationDispatcher.SendLayoutChanged"/> (with a @@null@@ parameter).
        /// </remarks>
        /// <seealso cref="AccessibilityNode.frame"/>
        /// <seealso cref="AccessibilityNode.frameGetter"/>
        public void RefreshNodeFrames()
        {
            foreach (var node in nodes.Values)
            {
                node.CalculateFrame();
            }

            AssistiveSupport.OnHierarchyNodeFramesRefreshed(this);
        }

        /// <summary>
        /// Tries to retrieve the node at the given position on the screen.
        /// </summary>
        /// <param name="horizontalPosition">The horizontal position on the screen.</param>
        /// <param name="verticalPosition">The vertical position on the screen.</param>
        /// <param name="node">The node found at that screen position, or @@null@@ if there are no nodes at that position.</param>
        /// <returns>Returns true if a node is found and false otherwise.</returns>
        public bool TryGetNodeAt(float horizontalPosition, float verticalPosition, out AccessibilityNode node)
        {
            var position = new Vector2(horizontalPosition, verticalPosition);

            AccessibilityNode FindNodeContainingPoint(IList<AccessibilityNode> nodes, Vector2 pos)
            {
                // Perform BFS (Breadth First Search) PostOrder Traversal in reverse order to find the last (at the top) node containing the specified point.
                for (var i = nodes.Count - 1; i >= 0; --i)
                {
                    var curNode = nodes[i];

                    var childNodeContainingPoint = FindNodeContainingPoint(curNode.childList, pos);

                    if (childNodeContainingPoint != null)
                        return childNodeContainingPoint;

                    // Ignore inactive node
                    if (curNode.isActive && curNode.frame.Contains(pos))
                        return curNode;
                }
                return null;
            }
            node = FindNodeContainingPoint(m_RootNodes, position);
            return node != null;
        }

        /// <summary>
        /// Retrieves the lowest common ancestor of two nodes in the hierarchy.<br/>
        /// The lowest common ancestor is the node that is the common node that both nodes share in their path to the root node
        /// of their branch in the hierarchy.
        /// </summary>
        /// <param name="firstNode">The first node to find the lowest common ancestor of.</param>
        /// <param name="secondNode">The second node to find the lowest common ancestor of.</param>
        /// <returns>The lowest common ancestor of the two given nodes, or @@null@@ if there is no common ancestor.</returns>
        public AccessibilityNode GetLowestCommonAncestor(AccessibilityNode firstNode, AccessibilityNode secondNode)
        {
            // Edge case: one of the given parameters is null.
            if (firstNode == null || secondNode == null)
            {
                return null;
            }

            // Edge case: both nodes are roots, so there is no common ancestor.
            if (firstNode.parent == null && secondNode.parent == null)
            {
                return null;
            }

            // Edge case: one of the nodes is not in this hierarchy.
            if (!ContainsNode(firstNode) || !ContainsNode(secondNode))
            {
                return null;
            }

            // Use local function to build the ID chains for both nodes
            void buildNodeIdStack(AccessibilityNode node, ref Stack<AccessibilityNode> nodeStack)
            {
                // Traverse up the hierarchy until we reach the root of the current node
                while (node != null)
                {
                    nodeStack.Push(node);
                    node = nodes[node.id].parent;
                }
            }

            // Set up the ID stacks
            m_FirstLowestCommonAncestorChain.Clear();
            m_SecondLowestCommonAncestorChain.Clear();
            buildNodeIdStack(firstNode, ref m_FirstLowestCommonAncestorChain);
            buildNodeIdStack(secondNode, ref m_SecondLowestCommonAncestorChain);

            // Start with no common ancestor, as nodes might not be in the same branch of the hierarchy
            AccessibilityNode commonAncestor = null;
            // Find the deepest level the lowest common ancestor can be on
            var hierarchyLevelsToSearch = Mathf.Min(m_FirstLowestCommonAncestorChain.Count, m_SecondLowestCommonAncestorChain.Count);

            // The first node that doesn't match or doesn't exist among the chains is the common ancestor
            while (hierarchyLevelsToSearch > 0)
            {
                var firstChainNodeId = m_FirstLowestCommonAncestorChain.Pop();
                var secondChainNodeId = m_SecondLowestCommonAncestorChain.Pop();

                if (firstChainNodeId != secondChainNodeId)
                {
                    // If the nodes no longer match, we have the lowest common ancestor
                    break;
                }

                // If the nodes do match, they share the same ancestor at this level
                commonAncestor = firstChainNodeId;
                hierarchyLevelsToSearch--;
            }

            return commonAncestor;
        }

        /// <summary>
        /// Creates a new <see cref="AccessibilityNode"/> with an ID unique across all hierarchies.
        /// </summary>
        /// <returns>The new node.</returns>
        AccessibilityNode CreateNode()
        {
            // Guard and select the next free ID. Both the count check and the Contains loop must run inside
            // the lock so that a concurrent finalizer Remove() cannot corrupt the HashSet while we read it.
            int nodeId;

            lock (usedNodeIds)
            {
                if (usedNodeIds.Count >= int.MaxValue)
                {
                    throw new InvalidOperationException("Could not create a new accessibility node. A maximum of " +
                        $"{int.MaxValue} nodes can exist at a time across all hierarchies. Try clearing unused " +
                        "hierarchies or removing unused nodes.");
                }

                // Skip over any IDs that are still in use by nodes in any hierarchy. This is important after
                // nextUniqueNodeId wraps around int.MaxValue.
                while (usedNodeIds.Contains(nextUniqueNodeId))
                {
                    nextUniqueNodeId = nextUniqueNodeId == int.MaxValue ? 0 : nextUniqueNodeId + 1;
                }

                // Reserve the ID.
                nodeId = nextUniqueNodeId;

                // Mark this ID as in use.
                usedNodeIds.Add(nodeId);

                // Loop the counter. We do not expect to have that many accessibility nodes at the same time.
                nextUniqueNodeId = nodeId == int.MaxValue ? 0 : nodeId + 1;
            }

            return new AccessibilityNode(nodeId, this);
        }

        /// <summary>
        /// Validates the given node is not @@null@@ and part of this hierarchy.
        /// </summary>
        /// <param name="node">The AccessibilityNode instance to be validated as part of this hierarchy.</param>
        /// <exception cref="ArgumentException">If the node is not part of this hierarchy.</exception>
        private void ValidateNodeInHierarchy(AccessibilityNode node)
        {
            if (node != null)
            {
                if (ContainsNode(node))
                {
                    return;
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(node));
            }

            throw new ArgumentException($"Trying to use an AccessibilityNode with ID {node.id} that is not part of this hierarchy.");
        }
    }
}
