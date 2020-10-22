// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    internal class QueryGraph
    {
        public IQueryNode root { get; private set; }

        public bool empty => root == null;

        public QueryGraph(IQueryNode root)
        {
            this.root = root;
        }

        public void Optimize(bool propagateNotToLeaves, bool swapNotToRightHandSide)
        {
            if (empty)
                return;

            Optimize(root, propagateNotToLeaves, swapNotToRightHandSide);
        }

        private void Optimize(IQueryNode root, bool propagateNotToLeaves, bool swapNotToRightHandSide)
        {
            if (!(root is CombinedNode cn) || cn.leaf)
                return;

            void SwapParentChild(IQueryNode parent, IQueryNode oldChild, IQueryNode newChild)
            {
                var parentCombinedNode = parent as CombinedNode;
                var oldIndex = parentCombinedNode.children.IndexOf(oldChild);
                parentCombinedNode.children[oldIndex] = newChild;
                root.parent = null;
                newChild.parent = parentCombinedNode;
            }

            if (propagateNotToLeaves && cn.type == QueryNodeType.Not)
            {
                var parent = cn.parent;
                var oldNode = cn.children[0];
                if (oldNode is CombinedNode oldCombinedNode && (oldCombinedNode.type == QueryNodeType.And || oldCombinedNode.type == QueryNodeType.Or))
                {
                    CombinedNode newCombinedNode;
                    if (oldNode.type == QueryNodeType.And)
                        newCombinedNode = new OrNode();
                    else
                        newCombinedNode = new AndNode();

                    cn.RemoveNode(oldNode);

                    foreach (var child in oldNode.children)
                    {
                        var propagatedNotNode = new NotNode();
                        propagatedNotNode.AddNode(child);
                        newCombinedNode.AddNode(propagatedNotNode);
                    }
                    oldCombinedNode.Clear();

                    // If the old not is the root of the evaluationGraph, then the new combined node
                    // becomes the new root.
                    if (parent == null)
                        this.root = newCombinedNode;
                    else
                    {
                        // In order to not change the parent's enumeration, swap directly the old
                        // children with the new one
                        SwapParentChild(parent, root, newCombinedNode);
                    }
                    // Set the current tree root to the new combined node.
                    root = newCombinedNode;
                }
            }

            if (swapNotToRightHandSide && root.children[0] is NotNode)
            {
                cn.SwapChildNodes();
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < root.children.Count; ++i)
            {
                Optimize(root.children[i], propagateNotToLeaves, swapNotToRightHandSide);
            }

            // Reduce Not depth (do this as last step)
            if (root.type == QueryNodeType.Not && !root.leaf && root.children[0].type == QueryNodeType.Not)
            {
                var parent = root.parent;
                var notNode = root as NotNode;
                var childNotNode = root.children[0] as NotNode;
                var descendant = childNotNode.children[0];
                childNotNode.RemoveNode(descendant);
                notNode.RemoveNode(childNotNode);
                if (parent == null)
                    this.root = descendant;
                else
                {
                    SwapParentChild(parent, root, descendant);
                }
            }
        }
    }
}
