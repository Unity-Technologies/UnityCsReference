// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// The hierarchy data model that the screen reader uses to navigate and interact with a Unity application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a screen reader to navigate an application, it must receive information like what the accessible elements
    /// are, where they are placed on the screen, what role they have, and how the user can interact with them. This
    /// information needs to be organized in a hierarchy of data structures called the @@AccessibilityHierarchy@@.
    /// </para>
    /// <para>
    /// The accessibility hierarchy operates independently of the UI hierarchy. You can use these APIs with any UI
    /// system and even with non-UI elements, such as elements that are part of your game.
    /// </para>
    /// <para>
    /// The data structures that form the accessibility hierarchy are called <see cref="AccessibilityNode"/>s. Each node
    /// represents a visual element that needs to be accessible to the screen reader. A node can have zero or more
    /// <see cref="AccessibilityNode.children"/> and one <see cref="AccessibilityNode.parent"/>.
    /// <see cref="AccessibilityHierarchy.rootNodes"/> are the top-level nodes in the hierarchy, having no parent.
    /// </para>
    /// <para>
    /// Users can navigate the accessibility hierarchy sequentially by moving the screen reader focus from one node to
    /// another in a depth-first traversal order, so they navigate to a node's children before moving to the node's
    /// siblings. The position of the nodes on the screen (given by their <see cref="AccessibilityNode.frame"/>) does
    /// not affect navigation order.
    /// </para>
    /// <para>
    /// To enable the screen reader to navigate an accessibility hierarchy, you must assign the hierarchy to
    /// <see cref="AssistiveSupport.activeHierarchy"/> to activate it. To manage system resources efficiently, Unity
    /// does not save the active hierarchy while the screen reader is off. You must activate the hierarchy each time the
    /// screen reader is turned on (see <see cref="AssistiveSupport.screenReaderStatusChanged"/> and
    /// <see cref="AssistiveSupport.isScreenReaderEnabled"/>).
    /// </para>
    /// <para>
    /// If you modify the active hierarchy, then you must notify the screen reader by calling
    /// <see cref="AssistiveSupport.NotificationDispatcher.SendLayoutChanged"/> or
    /// <see cref="AssistiveSupport.NotificationDispatcher.SendScreenChanged"/> (depending on the scale of the changes).
    /// Modifications in the accessibility hierarchy consist of calls to:
    ///
    ///- <see cref="AccessibilityHierarchy.AddNode"/>
    ///- <see cref="AccessibilityHierarchy.Clear"/>
    ///- <see cref="AccessibilityHierarchy.InsertNode"/>
    ///- <see cref="AccessibilityHierarchy.MoveNode"/>
    ///- <see cref="AccessibilityHierarchy.RemoveNode"/>
    ///- Modifications to node <see cref="AccessibilityNode.frame"/> values.
    /// </para>
    /// <para>
    /// These APIs are currently supported on the following platforms:
    ///
    ///- <see cref="RuntimePlatform.Android"/> - starting with Android 8.0 (API level 26)
    ///- <see cref="RuntimePlatform.IPhonePlayer"/>
    ///- <see cref="RuntimePlatform.OSXPlayer"/>
    ///- <see cref="RuntimePlatform.WindowsPlayer"/>
    /// </para>
    /// <para>
    /// SA:
    ///
    ///- [[wiki:accessibility|Accessibility for mobile applications]]
    ///- &lt;a href="https://github.com/Unity-Technologies/a11y-public-sample" &gt;Sample project using the accessibility APIs&lt;/a&gt;
    /// </para>
    /// </remarks>
    public class AccessibilityHierarchy
    {
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
        /// The collection of nodes and associated data in the hierarchy that can be accessed by the node ID as a key.
        /// </summary>
        readonly IDictionary<int, AccessibilityNode> m_Nodes;

        List<AccessibilityNode> m_RootNodes;

        /// <summary>
        /// The root nodes of the hierarchy.
        /// </summary>
        public IReadOnlyList<AccessibilityNode> rootNodes => m_RootNodes;

        /// <summary>
        /// One of two reusable stacks for finding the lowest common ancestor in the hierarchy
        /// </summary>
        Stack<AccessibilityNode> m_FirstLowestCommonAncestorChain;

        /// <summary>
        /// One of two reusable stacks for finding the lowest common ancestor in the hierarchy
        /// </summary>
        Stack<AccessibilityNode> m_SecondLowestCommonAncestorChain;

        /// <summary>
        /// The next unique ID that can be assigned to a new node. This is a static value and
        /// therefore shared among all instances of AccessibilityHierarchy, in order to guarantee ID uniqueness and
        /// avoid confusion of looking for an ID in a hierarchy that does not contain the node that ID originally
        /// belonged to.
        /// </summary>
        internal static int nextUniqueNodeId;

        /// <summary>
        /// Initializes and returns an empty <see cref="AccessibilityHierarchy"/>.
        /// </summary>
        public AccessibilityHierarchy()
        {
            // Initialize the collections.
            m_FirstLowestCommonAncestorChain = new Stack<AccessibilityNode>();
            m_SecondLowestCommonAncestorChain = new Stack<AccessibilityNode>();
            m_Nodes = new Dictionary<int, AccessibilityNode>();
            m_RootNodes = new List<AccessibilityNode>();
        }

        void NotifyHierarchyChanged()
        {
            m_Changed?.Invoke(this);
        }

        /// <summary>
        /// Resets the hierarchy to an empty state, removing all nodes and the screen reader focus.
        /// </summary>
        public void Clear()
        {
            for (var i = m_RootNodes.Count - 1; i >= 0; i--)
            {
                RemoveNode(m_RootNodes[i], removeChildren: true);
            }
        }

        /// <summary>
        /// Tries to retrieve the <see cref="AccessibilityNode"/> with the given ID in the hierarchy.
        /// </summary>
        /// <param name="id">The ID of the node to retrieve.</param>
        /// <param name="node">The valid node with the associated ID, or @@null@@ if no such node exists in this
        /// hierarchy.</param>
        /// <returns>@@true@@ if a node is found and @@false@@ otherwise.</returns>
        public bool TryGetNode(int id, out AccessibilityNode node)
        {
            return m_Nodes.TryGetValue(id, out node);
        }

        /// <summary>
        /// Creates a new <see cref="AccessibilityNode"/> with the given label and adds it to the hierarchy under the
        /// given parent.
        /// </summary>
        /// <param name="label">A label that succinctly describes the node.</param>
        /// <param name="parent">The parent of the new node, or @@null@@ if the node should be placed at the root level.
        /// </param>
        /// <returns>The node created and added to the accessibility hierarchy.</returns>
        public AccessibilityNode AddNode(string label = null, AccessibilityNode parent = null)
        {
            return InsertNode(-1, label, parent);
        }

        /// <summary>
        /// Creates a new <see cref="AccessibilityNode"/> with the given label and inserts it at the given index in the
        /// hierarchy, under the given parent.
        /// </summary>
        /// <param name="childIndex">A zero-based index which provides the position of the new node in the parent's
        /// child list or in the list of root nodes if the node should be a root. If the index is invalid, then the node
        /// is added to the end of the parent's child list.</param>
        /// <param name="label">A label that succinctly describes the node.</param>
        /// <param name="parent">The parent of the new node, or @@null@@ if you want the node to be at the root level.
        /// </param>
        /// <returns>The node created and inserted into the accessibility hierarchy.</returns>
        public AccessibilityNode InsertNode(int childIndex, string label = null, AccessibilityNode parent = null)
        {
            // Only nodes intended as roots may have an invalid parent.
            if (parent != null)
                ValidateNodeInHierarchy(parent);

            // Generate a new node to return, then add it to the manager under its parent.
            var node = GenerateNewNode();
            m_Nodes[node.id] = node;

            if (label != null)
            {
                node.label = label;
            }

            SetParent(node, parent, null, parent == null ? m_RootNodes : parent.childList, childIndex);

            NotifyHierarchyChanged();
            return node;
        }

        /// <summary>
        /// Moves the node elsewhere in the accessibility hierarchy. For example, under a different parent or at a
        /// different position in the parent's child list.
        /// </summary>
        /// <remarks>
        /// **Warning**: The moving operation is costly because many checks have to be executed to guarantee the
        /// integrity of the hierarchy. If this method is called excessively, it might negatively affect performance.
        /// </remarks>
        /// <param name="node">The node to move.</param>
        /// <param name="newParent">The new parent of the node, or @@null@@ if the node should be placed at the root
        /// level.</param>
        /// <param name="newChildIndex">An optional zero-based index which provides the position of the moved node in
        /// the new parent's child list or in the list of root nodes if the node should be a root. If the index is not
        /// provided or is invalid, then the node is moved at the end of the new parent's child list.</param>
        /// <returns>@@true@@ if the node was successfully moved and @@false@@ otherwise.</returns>
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
        /// Removes the node from the accessibility hierarchy and removes or re-parents its descendants.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <param name="removeChildren">@@true@@ if the node's descendants should also be removed, or @@false@@ if they
        /// should be moved under the node's parent. Defaults to @@true@@.</param>
        public void RemoveNode(AccessibilityNode node, bool removeChildren = true)
        {
            ValidateNodeInHierarchy(node);

            if (removeChildren)
            {
                void RemoveFromNodes(AccessibilityNode child)
                {
                    m_Nodes.Remove(child.id);

                    for (var i = 0; i < child.childList.Count; i++)
                    {
                        RemoveFromNodes(child.childList[i]);
                    }
                }

                RemoveFromNodes(node);
            }
            else
            {
                m_Nodes.Remove(node.id);
            }

            if (m_RootNodes.Contains(node))
            {
                m_RootNodes.Remove(node);

                // If we aren't removing the children, add them as roots (AccessibilityNode.Destroy will handle updating
                // the parent value).
                if (!removeChildren)
                {
                    m_RootNodes.AddRange(node.childList);
                }
            }

            node.Destroy(removeChildren);
            NotifyHierarchyChanged();
        }

        /// <summary>
        /// Verifies whether the given node exists in the accessibility hierarchy.
        /// </summary>
        /// <param name="node">The node to search for in the hierarchy.</param>
        /// <returns>@@true@@ if the node exists in this hierarchy and @@false@@ otherwise.</returns>
        public bool ContainsNode(AccessibilityNode node)
        {
            return node != null && m_Nodes.ContainsKey(node.id) && m_Nodes[node.id] == node;
        }

        void CheckForLoopsAndSetParent(AccessibilityNode node, AccessibilityNode parent, int newChildIndex = -1)
        {
            // We don't validate the nodes are in the hierarchy here as this is a private method, and we guarantee this
            // is only called when we're sure both given parameters are valid.

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

            // If the node exists in any of the ancestral nodes of the parent, then we are creating a loop, which is
            // invalid.
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

        void SetParent(AccessibilityNode node, AccessibilityNode parent, IList<AccessibilityNode> previousParentChildren, IList<AccessibilityNode> newParentChildren, int newChildIndex = -1)
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
        /// Refreshes the <see cref="AccessibilityNode.frame"/> of all nodes in the accessibility hierarchy.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that updates the <see cref="AccessibilityNode.frame"/> of all nodes in the
        /// accessibility hierarchy (based on <see cref="AccessibilityNode.frameGetter"/>) and notifies the screen
        /// reader of these updates by calling <see cref="IAccessibilityNotificationDispatcher.SendLayoutChanged"/>
        /// (with a @@null@@ parameter).
        /// </para>
        /// <para>
        /// Call this method when most or all of the nodes on the screen require a layout update. For example, when the
        /// user scrolls the application's interface, or when the orientation of the screen changes.
        /// </para>
        /// </remarks>
        public void RefreshNodeFrames()
        {
            foreach (var node in m_Nodes.Values)
            {
                node.frame = node.frameGetter?.Invoke() ?? Rect.zero;
            }

            if (AssistiveSupport.activeHierarchy == this)
            {
                AssistiveSupport.notificationDispatcher.SendLayoutChanged();
            }
        }

        /// <summary>
        /// Tries to retrieve the <see cref="AccessibilityNode"/> at the given screen coordinates.
        /// </summary>
        /// <param name="horizontalPosition">The horizontal position on the screen.</param>
        /// <param name="verticalPosition">The vertical position on the screen.</param>
        /// <param name="node">The node found at the given screen coordinates, or @@null@@ if there is no node at that
        /// position.</param>
        /// <returns>@@true@@ if the node is found and @@false@@ otherwise.</returns>
        public bool TryGetNodeAt(float horizontalPosition, float verticalPosition, out AccessibilityNode node)
        {
            var position = new Vector2(horizontalPosition, verticalPosition);

            AccessibilityNode FindNodeContainingPoint(IList<AccessibilityNode> nodes, Vector2 pos)
            {
                // Perform BFS (Breadth First Search) PostOrder Traversal in reverse order to find the last (at the top)
                // node containing the specified point.
                for (var i = nodes.Count - 1; i >= 0; --i)
                {
                    var curNode = nodes[i];

                    var childNodeContainingPoint = FindNodeContainingPoint(curNode.childList, pos);

                    if (childNodeContainingPoint != null)
                    {
                        return childNodeContainingPoint;
                    }

                    // Ignore inactive node.
                    if (curNode.isActive && curNode.frame.Contains(pos))
                    {
                        return curNode;
                    }
                }

                return null;
            }

            node = FindNodeContainingPoint(m_RootNodes, position);
            return node != null;
        }

        /// <summary>
        /// Retrieves the lowest common ancestor of two nodes in the accessibility hierarchy.
        /// </summary>
        /// <remarks>
        /// The lowest common ancestor is the node that both nodes share in their path to the root node of their branch
        /// in the hierarchy.
        /// </remarks>
        /// <param name="firstNode">The first node to find the lowest common ancestor of.</param>
        /// <param name="secondNode">The second node to find the lowest common ancestor of.</param>
        /// <returns>The lowest common ancestor of the two given nodes, or @@null@@ if they don't have a common
        /// ancestor.</returns>
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

            // Set up the ID stacks
            m_FirstLowestCommonAncestorChain.Clear();
            m_SecondLowestCommonAncestorChain.Clear();

            BuildNodeIdStack(firstNode, ref m_FirstLowestCommonAncestorChain);
            BuildNodeIdStack(secondNode, ref m_SecondLowestCommonAncestorChain);

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

            // Use local function to build the ID chains for both nodes
            void BuildNodeIdStack(AccessibilityNode node, ref Stack<AccessibilityNode> nodeStack)
            {
                // Traverse up the hierarchy until we reach the root of the current node
                while (node != null)
                {
                    nodeStack.Push(node);
                    node = m_Nodes[node.id].parent;
                }
            }
        }

        /// <summary>
        /// Generates a new <see cref="AccessibilityNode"/> with a unique ID in the hierarchy.
        /// </summary>
        /// <returns>The new node.</returns>
        AccessibilityNode GenerateNewNode()
        {
            // Validate we can actually generate new nodes, meaning we still have a valid ID value to set to a node.
            if (nextUniqueNodeId >= int.MaxValue)
            {
                throw new Exception($"Could not generate unique node for hierarchy. A hierarchy may only have up to {int.MaxValue} nodes.");
            }

            // Create new instance of a node and increment the control for the ID so the next node created gets a new
            // and valid value.
            var node = new AccessibilityNode(nextUniqueNodeId, this);

            nextUniqueNodeId = node.id + 1;
            return node;
        }

        /// <summary>
        /// Validates that the given node is not @@null@@ and is part of the hierarchy.
        /// </summary>
        /// <param name="node">The node to validate as part of this hierarchy.</param>
        /// <exception cref="ArgumentException">If the node is not part of this hierarchy.</exception>
        void ValidateNodeInHierarchy(AccessibilityNode node)
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
