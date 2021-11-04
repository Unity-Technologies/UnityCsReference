// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    /// <summary>
    /// Structure containing the different options used to optimize a query graph.
    /// </summary>
    public struct QueryGraphOptimizationOptions
    {
        /// <summary>
        /// Propagate "Not" operations to leaves, so only leaves can have "Not" operations as parents.
        /// </summary>
        public bool propagateNotToLeaves;

        /// <summary>
        /// Swaps "Not" operations to the right hand side of combining operations (i.e. "And", "Or"). Useful if a "Not" operation is slow.
        /// </summary>
        public bool swapNotToRightHandSide;

        /// <summary>
        /// Swaps filter functions to the right hand side of combining operations (i.e. "And", "Or"). Useful if those filter operations are slow.
        /// </summary>
        public bool swapFilterFunctionsToRightHandSide;
    }

    /// <summary>
    /// Class that represents a query graph.
    /// </summary>
    public class QueryGraph
    {
        /// <summary>
        /// Root node of the graph. Can be null.
        /// </summary>
        public IQueryNode root { get; private set; }

        /// <summary>
        /// Returns true if the graph is empty.
        /// </summary>
        public bool empty => root == null;

        /// <summary>
        /// Constructor. Creates a new query graph.
        /// </summary>
        /// <param name="root">Root node of the graph.</param>
        public QueryGraph(IQueryNode root)
        {
            this.root = root;
        }

        /// <summary>
        /// Optimize the graph.
        /// </summary>
        /// <param name="propagateNotToLeaves">Propagate "Not" operations to leaves, so only leaves can have "Not" operations as parents.</param>
        /// <param name="swapNotToRightHandSide">Swaps "Not" operations to the right hand side of combining operations (i.e. "And", "Or"). Useful if a "Not" operation is slow.</param>
        public void Optimize(bool propagateNotToLeaves, bool swapNotToRightHandSide)
        {
            if (empty)
                return;

            Optimize(root, new QueryGraphOptimizationOptions {propagateNotToLeaves = propagateNotToLeaves, swapNotToRightHandSide = swapNotToRightHandSide, swapFilterFunctionsToRightHandSide = false});
        }

        /// <summary>
        /// Optimize the graph.
        /// </summary>
        /// <param name="options">Optimization options.</param>
        public void Optimize(QueryGraphOptimizationOptions options)
        {
            if (empty)
                return;

            Optimize(root, options);
        }

        void Optimize(IQueryNode rootNode, QueryGraphOptimizationOptions options)
        {
            if (rootNode.leaf)
                return;

            if (options.propagateNotToLeaves)
            {
                PropagateNotToLeaves(ref rootNode);
            }

            if (options.swapNotToRightHandSide)
            {
                SwapNotToRightHandSide(rootNode);
            }

            if (options.swapFilterFunctionsToRightHandSide)
            {
                SwapFilterFunctionsToRightHandSide(rootNode);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < rootNode.children.Count; ++i)
            {
                Optimize(rootNode.children[i], options);
            }

            // Reduce Not depth (do this as last step)
            ReduceNotDepth(rootNode);
        }

        static void SwapChild(IQueryNode parent, IQueryNode oldChild, IQueryNode newChild)
        {
            if (parent?.children == null || parent.children.Count == 0)
                return;

            var oldIndex = parent.children.IndexOf(oldChild);
            parent.children[oldIndex] = newChild;
            oldChild.parent = null;
            newChild.parent = parent;
        }

        void PropagateNotToLeaves(ref IQueryNode rootNode)
        {
            if (rootNode.leaf || !(rootNode is CombinedNode cn))
                return;

            if (rootNode.type != QueryNodeType.Not)
                return;

            var parent = rootNode.parent;
            var oldNode = rootNode.children[0];
            if (!(oldNode is CombinedNode oldCombinedNode) || (oldCombinedNode.type != QueryNodeType.And && oldCombinedNode.type != QueryNodeType.Or))
                return;

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
                SwapChild(parent, rootNode, newCombinedNode);
            }
            // Set the current tree root to the new combined node.
            rootNode = newCombinedNode;
        }

        static void SwapNotToRightHandSide(IQueryNode rootNode)
        {
            if (rootNode.leaf || !(rootNode.children[0] is NotNode) || !(rootNode is CombinedNode cn))
                return;

            cn.SwapChildNodes();
        }

        static void SwapFilterFunctionsToRightHandSide(IQueryNode rootNode)
        {
            if (rootNode.leaf || !(rootNode is CombinedNode cn) || !(rootNode.children[0] is FilterNode fn) || !fn.filter.usesParameter)
                return;

            cn.SwapChildNodes();
        }

        void ReduceNotDepth(IQueryNode rootNode)
        {
            if (rootNode.leaf)
                return;

            if (rootNode.type != QueryNodeType.Not || rootNode.children[0].type != QueryNodeType.Not)
                return;

            var parent = rootNode.parent;
            if (!(rootNode is NotNode notNode) || !(rootNode.children[0] is NotNode childNotNode))
                return;

            var descendant = childNotNode.children[0];
            childNotNode.RemoveNode(descendant);
            notNode.RemoveNode(childNotNode);
            if (parent == null)
                this.root = descendant;
            else
            {
                SwapChild(parent, rootNode, descendant);
            }
        }
    }
}
