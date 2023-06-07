// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Class that implements the <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}"/> using a KdTree.
/// </summary>
/// <typeparam name="TElementKey">The type of a unique key identifying an element whose bounding box is partitioned.</typeparam>
class BoundingBoxKdTreePartitioning<TElementKey> : BaseBoundingBoxSpacePartitioning<TElementKey>
    where TElementKey : IEquatable<TElementKey>
{
    /// <summary>
    /// Enum representing the splitting plane axis.
    /// </summary>
    internal enum Axis_Internal
    {
        XMin,
        YMin,
        XMax,
        YMax
    }

    /// <summary>
    /// Enum representing the continuation state when iterating nodes.
    /// </summary>
    internal enum NodeIterationContinuation_Internal
    {
        ContinueAll,
        ContinueLeftOnly,
        ContinueRightOnly,
        ContinueNoChild,
        Stop
    }

    /// <summary>
    /// Class used as a context that is passed around when iterating nodes.
    /// </summary>
    internal class NodeVisitContext_Internal
    {
        /// <summary>
        /// The current <see cref="NodeIterationContinuation_Internal"/>.
        /// </summary>
        public NodeIterationContinuation_Internal Continuation = NodeIterationContinuation_Internal.ContinueAll;
    }

    /// <summary>
    /// Class used as context when processing nodes.
    /// </summary>
    protected class ProcessingContext
    {
        /// <summary>
        /// The currently affected node count.
        /// </summary>
        public int CurrentlyAffectedNodes;

        /// <summary>
        /// The affected node count threshold. Once the number of affected node reaches this threshold
        /// the operation is stopped and the tree is rebuilt.
        /// </summary>
        public int AffectedNodeThreshold;
    }

    /// <summary>
    /// Class representing a node in the KdTree.
    /// </summary>
    internal class Node_Internal
    {
        Node_Internal m_LeftChild;
        Node_Internal m_RightChild;

        /// <summary>
        /// The parent of this node.
        /// </summary>
        public Node_Internal Parent;

        /// <summary>
        /// The left child of this node. Null if there is no child.
        /// </summary>
        public Node_Internal LeftChild
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => m_LeftChild;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            set
            {
                m_LeftChild = value;
                if (m_LeftChild != null)
                    m_LeftChild.Parent = this;
            }
        }

        /// <summary>
        /// The right child of this node. Null if there is no child.
        /// </summary>
        public Node_Internal RightChild
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => m_RightChild;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            set
            {
                m_RightChild = value;
                if (m_RightChild != null)
                    m_RightChild.Parent = this;
            }
        }

        /// <summary>
        /// The element's key partitioned by this node.
        /// </summary>
        public TElementKey ElementKey;

        /// <summary>
        /// The splitting plane value.
        /// </summary>
        public float AxisValue;

        /// <summary>
        /// The bounding box of the element partitioned by this node.
        /// </summary>
        public readonly Rect BoundingBox;

        /// <summary>
        /// The splitting plane axis.
        /// </summary>
        public Axis_Internal Axis;

        /// <summary>
        /// Returns true if this node is a leaf.
        /// </summary>
        public bool IsLeaf
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => LeftChild == null && RightChild == null;
        }

        /// <summary>
        /// Creates a node.
        /// </summary>
        /// <param name="elementKey">The element's key partitioned by this node.</param>
        /// <param name="axis">The splitting plane axis.</param>
        /// <param name="axisValue">The splitting plane value.</param>
        /// <param name="boundingBox">The bounding box of the element partitioned by this node.</param>
        public Node_Internal(TElementKey elementKey, Axis_Internal axis, float axisValue, Rect boundingBox)
        {
            ElementKey = elementKey;
            AxisValue = axisValue;
            BoundingBox = boundingBox;
            Axis = axis;
        }

        /// <summary>
        /// Detaches this node from its parent. It also updates the parent's link to this node.
        /// </summary>
        public void DetachFromParent()
        {
            if (Parent == null)
                return;
            if (Parent.LeftChild == this)
                Parent.LeftChild = null;
            if (Parent.RightChild == this)
                Parent.RightChild = null;
            Parent = null;
        }
    }

    readonly struct NodeBuildInfo
    {
        public readonly Node_Internal Node;
        public readonly int PivotIndex;
        public readonly int SubTreeStart;
        public readonly int SubTreeLength;

        public NodeBuildInfo(Node_Internal node, int pivotIndex, int subTreeStart, int subTreeLength)
        {
            Node = node;
            PivotIndex = pivotIndex;
            SubTreeStart = subTreeStart;
            SubTreeLength = subTreeLength;
        }
    }

    /// <summary>
    /// The root node of the KdTree.
    /// </summary>
    protected Node_Internal m_RootNode;

    // These values were found empirically by finding out at which ratio it is faster
    // to rebuild the tree instead of doing the operation.
    protected float m_UpdateNodeThresholdRatio = 0.85f;
    protected float m_RemoveNodesThresholdRatio = 0.8f;

    // This value was found empirically by testing the speed of the method RemoveElements
    // by using an existing array vs creating and using an hashset.
    protected int m_MaxArraySizeBeforeHash = 20;

    // For testing only.
    internal Node_Internal Root_Internal => m_RootNode;

    /// <inheritdoc />
    public override bool Empty => m_RootNode == null || base.Empty;


    /// <inheritdoc />
    public override void Clear()
    {
        base.Clear();
        m_RootNode = null;
    }

    /// <inheritdoc />
    public override void AddOrUpdateElements(IReadOnlyCollection<BoundingBoxElement> elements)
    {
        if (elements == null)
            return;

        if (elements.Count == 0)
            return;

        var noRebuild = true;
        if (Empty)
            m_RootNode = BuildTree(ToMutableArray(elements), Axis_Internal.XMin);
        else
        {
            var threshold = Mathf.FloorToInt(Count * m_UpdateNodeThresholdRatio);
            var processingContext = new ProcessingContext { AffectedNodeThreshold = threshold, CurrentlyAffectedNodes = 0 };
            if (elements.Count <= m_MaxArraySizeBeforeHash)
                noRebuild = RemoveElements(elements, processingContext);
            else
                noRebuild = RemoveElements(ToElementKeysHashSet(elements), processingContext);
            if (Empty)
                m_RootNode = BuildTree(ToMutableArray(elements), Axis_Internal.XMin);
            else
            {
                if (noRebuild)
                    AddElements(elements);
                else
                    UpdateAndRebuild(elements);
            }
        }

        if (noRebuild)
            base.AddOrUpdateElements(elements);
    }

    /// <inheritdoc />
    public override void RemoveElements(IReadOnlyCollection<TElementKey> elements)
    {
        if (elements == null) return;
        var elementKeys = ToBestKeyCollection(elements, m_MaxArraySizeBeforeHash);
        var threshold = Mathf.FloorToInt(Count * m_RemoveNodesThresholdRatio);
        var processingContext = new ProcessingContext { AffectedNodeThreshold = threshold, CurrentlyAffectedNodes = 0 };
        if (!RemoveElements(elementKeys, processingContext))
            RemoveAndRebuild(elementKeys);
    }

    /// <summary>
    /// Removes the elements from the space partitioning. Stops after the specified threshold of nodes is reached.
    /// </summary>
    /// <param name="elementKeys">A set of element keys to remove from the partitioning.</param>
    /// <param name="context">The processing context.</param>
    /// <returns>True if the operation completed, false if it stopped because it reached the threshold.</returns>
    protected bool RemoveElements(ICollection<TElementKey> elementKeys, ProcessingContext context)
    {
        if (elementKeys == null || elementKeys.Count == 0)
            return true;

        var nodesToRemove = new List<Node_Internal>(capacity: elementKeys.Count);
        // Iterating over all nodes to see which ones are to be removed seems to be
        // faster than having a map of TElementKey <--> Node_Internal and checking which ones are in the map.
        IterateNodes(m_RootNode, (node, ctx) =>
        {
            if (ContainsKey(elementKeys, node.ElementKey))
                nodesToRemove.Add(node);
            ctx.Continuation = NodeIterationContinuation_Internal.ContinueAll;
        });

        if (!RemoveNodes(nodesToRemove, context))
            return false;

        RemovePartitionedElements(elementKeys);

        return true;
    }

    /// <summary>
    /// Removes the elements from the space partitioning. Stops after the specified threshold of nodes is reached.
    /// </summary>
    /// <param name="elements">A collection of elements to remove from the partitioning.</param>
    /// <param name="context">The processing context.</param>
    /// <returns>True if the operation completed, false if it stopped because it reached the threshold.</returns>
    protected bool RemoveElements(IReadOnlyCollection<BoundingBoxElement> elements, ProcessingContext context)
    {
        if (elements == null || elements.Count == 0)
            return true;

        var nodesToRemove = new List<Node_Internal>(capacity: elements.Count);
        // Iterating over all nodes to see which ones are to be removed seems to be
        // faster than having a map of TElementKey <--> Node_Internal and checking which ones are in the map.
        IterateNodes(m_RootNode, (node, ctx) =>
        {
            if (ContainsKey(elements, node.ElementKey))
                nodesToRemove.Add(node);
            ctx.Continuation = NodeIterationContinuation_Internal.ContinueAll;
        });

        if (!RemoveNodes(nodesToRemove, context))
            return false;

        RemovePartitionedElements(elements);

        return true;
    }

    /// <summary>
    /// Removes the KdTree nodes from the tree. Stops after the specified threshold of nodes is reached.
    /// </summary>
    /// <param name="nodesToRemove">The list of nodes to remove.</param>
    /// <param name="context">The processing context.</param>
    /// <returns>True if the operation completed, false if it stopped because it reached the threshold.</returns>
    protected bool RemoveNodes(List<Node_Internal> nodesToRemove, ProcessingContext context)
    {
        if (nodesToRemove.Count == 0)
            return true;

        if (nodesToRemove.Count == Count)
        {
            Clear();
            return true;
        }

        if (nodesToRemove.Count > context.AffectedNodeThreshold)
            return false;

        foreach (var node in nodesToRemove)
            if (!RemoveNode(node, context))
                return false;

        return true;
    }

    /// <inheritdoc />
    public override IReadOnlyCollection<TElementKey> GetElementsInRegion(Rect region, bool allowOverlap)
    {
        if (Empty)
            return Array.Empty<TElementKey>();

        var elementsInRegion = new List<TElementKey>();
        GetElementsInRegion(region, allowOverlap, elementsInRegion);
        return elementsInRegion;
    }

    /// <inheritdoc />
    public override void GetElementsInRegion(Rect region, bool allowOverlap, ICollection<TElementKey> outCollection)
    {
        if (Empty)
            return;

        IterateNodes(m_RootNode, (currentNode, context) =>
        {
            var currentNodeRect = currentNode.BoundingBox;
            var currentAxisValue = currentNode.AxisValue;
            var currentAxis = currentNode.Axis;

            if (allowOverlap)
            {
                if (currentNodeRect.Overlaps(region, true))
                    outCollection.Add(currentNode.ElementKey);

                // When overlapping is allowed, we have to check with the opposite axis
                var oppositeAxis = GetOppositeAxis(currentAxis);
                var rectAxisValue = GetBoundingBoxValueByAxis(region, oppositeAxis);

                switch (currentAxis)
                {
                    case Axis_Internal.XMin:
                    case Axis_Internal.YMin:
                        if (currentAxisValue > rectAxisValue)
                            context.Continuation = NodeIterationContinuation_Internal.ContinueLeftOnly;
                        break;
                    case Axis_Internal.XMax:
                    case Axis_Internal.YMax:
                        if (currentAxisValue < rectAxisValue)
                            context.Continuation = NodeIterationContinuation_Internal.ContinueRightOnly;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                if (region.Contains(currentNodeRect.min) && region.Contains(currentNodeRect.max))
                    outCollection.Add(currentNode.ElementKey);

                var rectAxisValue = GetBoundingBoxValueByAxis(region, currentAxis);

                switch (currentAxis)
                {
                    case Axis_Internal.XMin:
                    case Axis_Internal.YMin:
                        if (currentAxisValue < rectAxisValue)
                            context.Continuation = NodeIterationContinuation_Internal.ContinueRightOnly;
                        break;
                    case Axis_Internal.XMax:
                    case Axis_Internal.YMax:
                        if (currentAxisValue > rectAxisValue)
                            context.Continuation = NodeIterationContinuation_Internal.ContinueLeftOnly;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        });
    }

    /// <inheritdoc />
    public override IReadOnlyCollection<TElementKey> GetElementsAtPosition(Vector2 position)
    {
        if (Empty)
            return Array.Empty<TElementKey>();

        var elementsAtPosition = new List<TElementKey>();
        GetElementsAtPosition(position, elementsAtPosition);
        return elementsAtPosition;
    }

    /// <inheritdoc />
    public override void GetElementsAtPosition(Vector2 position, ICollection<TElementKey> outCollection)
    {
        if (Empty)
            return;

        IterateNodes(m_RootNode, (currentNode, context) =>
        {
            var currentNodeRect = currentNode.BoundingBox;
            var currentAxisValue = currentNode.AxisValue;
            var currentAxis = currentNode.Axis;

            if (currentNodeRect.Contains(position, true))
                outCollection.Add(currentNode.ElementKey);

            var vectorAxisValue = GetVector2ValueByAxis(position, currentAxis);

            switch (currentAxis)
            {
                case Axis_Internal.XMin:
                case Axis_Internal.YMin:
                    if (vectorAxisValue <= currentAxisValue)
                        context.Continuation = NodeIterationContinuation_Internal.ContinueLeftOnly;
                    break;
                case Axis_Internal.XMax:
                case Axis_Internal.YMax:
                    if (vectorAxisValue > currentAxisValue)
                        context.Continuation = NodeIterationContinuation_Internal.ContinueRightOnly;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
    }

    /// <summary>
    /// Adds the elements to the KdTree.
    /// </summary>
    /// <param name="elements">A collection of elements.</param>
    protected void AddElements(IReadOnlyCollection<BoundingBoxElement> elements)
    {
        foreach (var boundingBoxElement in elements)
        {
            AddElement(boundingBoxElement);
        }
    }

    /// <summary>
    /// Adds an element to the KdTree.
    /// </summary>
    /// <param name="element">An element.</param>
    protected void AddElement(in BoundingBoxElement element)
    {
        if (m_RootNode == null)
        {
            m_RootNode = CreateNode(element, Axis_Internal.XMin);
            return;
        }

        AddElement(element, m_RootNode);
    }

    /// <summary>
    /// Adds an element in the KdTree under a specific root node.
    /// </summary>
    /// <param name="element">An element.</param>
    /// <param name="rootNode">The root node.</param>
    protected void AddElement(in BoundingBoxElement element, Node_Internal rootNode)
    {
        var currentNode = rootNode;
        while (true)
        {
            var currentAxis = currentNode.Axis;
            var nextAxis = GetNextAxis(currentAxis);
            var currentAxisValue = GetBoundingBoxValueByAxis(element, currentNode.Axis);
            if (currentAxisValue <= currentNode.AxisValue)
            {
                if (currentNode.LeftChild == null)
                {
                    currentNode.LeftChild = CreateNode(element, nextAxis);
                    break;
                }
                currentNode = currentNode.LeftChild;
            }
            else
            {
                if (currentNode.RightChild == null)
                {
                    currentNode.RightChild = CreateNode(element, nextAxis);
                    break;
                }
                currentNode = currentNode.RightChild;
            }
        }
    }

    /// <summary>
    /// Removes and replaces a node in the KdTree. The operation stops after a threshold of updated/removed nodes is reached.
    /// </summary>
    /// <param name="node">The node to remove and replace.</param>
    /// <param name="context">The processing context.</param>
    /// <returns>True if the operation completed, false if it stopped because it reached the threshold.</returns>
    /// <exception cref="InvalidOperationException">An exception is thrown if no replacement was found for the node, which should not happen.</exception>
    protected bool RemoveNode(Node_Internal node, ProcessingContext context)
    {
        ++context.CurrentlyAffectedNodes;
        if (context.CurrentlyAffectedNodes > context.AffectedNodeThreshold)
        {
            return false;
        }

        var nodeStack = new Stack<Node_Internal>();

        nodeStack.Push(node);

        // Find all replacement nodes
        var currentNode = node;
        while (!currentNode.IsLeaf)
        {
            currentNode = FindReplacementNode(currentNode);
            if (currentNode == null)
                throw new InvalidOperationException("No replacement found when removing node!");
            nodeStack.Push(currentNode);
            ++context.CurrentlyAffectedNodes;
            if (context.CurrentlyAffectedNodes > context.AffectedNodeThreshold)
            {
                return false;
            }
        }

        if (currentNode.IsLeaf)
        {
            currentNode.DetachFromParent();
            if (m_RootNode == currentNode)
                m_RootNode = null;
            if (nodeStack.Count == 1)
                return true;
        }

        while (nodeStack.Count > 1)
        {
            currentNode = nodeStack.Pop();
            var currentIsReplacement = nodeStack.TryPeek(out var replacedNode);

            if (currentIsReplacement)
            {
                currentNode.LeftChild = replacedNode.LeftChild;
                currentNode.RightChild = replacedNode.RightChild;

                currentNode.Axis = replacedNode.Axis;
                currentNode.AxisValue = replacedNode.AxisValue;

                if (replacedNode.Parent == null)
                    m_RootNode = currentNode;
                else
                {
                    var oldParent = replacedNode.Parent;
                    var isLeftChild = replacedNode.Parent.LeftChild == replacedNode;
                    replacedNode.DetachFromParent();
                    if (isLeftChild)
                        oldParent.LeftChild = currentNode;
                    else
                        oldParent.RightChild = currentNode;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Finds a replacement for a node. If the node as a right child, the replacement is the node with the smallest
    /// value for the current axis in the right children. Otherwise it is the node with the highest value for the current
    /// axis in the left children.
    /// </summary>
    /// <param name="node">The node to find a replacement for.</param>
    /// <returns>The replacement node.</returns>
    protected Node_Internal FindReplacementNode(Node_Internal node)
    {
        if (node == null || node.IsLeaf)
            return null;

        var currentAxis = node.Axis;
        if (node.RightChild != null)
        {
            return FindNodeWithAggregator(node.RightChild, (bestNode, currentNode, context) =>
            {
                var bestNodeValue = GetBoundingBoxValueByAxis(bestNode, currentAxis);
                var currentNodeValue = GetBoundingBoxValueByAxis(currentNode, currentAxis);

                if (currentNode.Axis == currentAxis)
                {
                    if (currentNode.LeftChild != null)
                        context.Continuation = NodeIterationContinuation_Internal.ContinueLeftOnly;
                }

                if (currentNodeValue < bestNodeValue)
                    return currentNode;
                return bestNode;
            });
        }
        if (node.LeftChild != null)
        {
            return FindNodeWithAggregator(node.LeftChild, (bestNode, currentNode, context) =>
            {
                var bestNodeValue = GetBoundingBoxValueByAxis(bestNode, currentAxis);
                var currentNodeValue = GetBoundingBoxValueByAxis(currentNode, currentAxis);

                if (currentNode.Axis == currentAxis)
                {
                    if (currentNode.RightChild != null)
                        context.Continuation = NodeIterationContinuation_Internal.ContinueRightOnly;
                }

                if (currentNodeValue > bestNodeValue)
                    return currentNode;
                return bestNode;
            });
        }

        return null;
    }

    /// <summary>
    /// Builds the entire KdTree from a collection of elements.
    /// </summary>
    /// <param name="elements">The collection of elements to partition.</param>
    /// <param name="currentAxis">The starting splitting axis.</param>
    /// <returns>The root node of the KdTree.</returns>
    protected virtual Node_Internal BuildTree(Span<BoundingBoxElement> elements, Axis_Internal currentAxis)
    {
        var nodeStack = new Stack<NodeBuildInfo>();

        var nodeIndex = PartialMedianSort(elements, currentAxis);
        var rootNode = CreateNode(elements[nodeIndex], currentAxis);

        var nodeBuildInfo = new NodeBuildInfo(rootNode, nodeIndex, 0, elements.Length);
        nodeStack.Push(nodeBuildInfo);

        while (nodeStack.Count > 0)
        {
            var currentNodeBuildInfo = nodeStack.Pop();
            var currentNode = currentNodeBuildInfo.Node;
            currentAxis = currentNode.Axis;
            var nextAxis = GetNextAxis(currentAxis);

            var subTreeStart = currentNodeBuildInfo.SubTreeStart;
            var subTreeLength = currentNodeBuildInfo.SubTreeLength;
            var subTreePivot = currentNodeBuildInfo.PivotIndex;

            var subTreeSpan = elements.Slice(subTreeStart, subTreeLength);

            if (subTreePivot > 0)
            {
                var leftSubTreeSpan = subTreeSpan.Slice(0, subTreePivot);
                nodeIndex = PartialMedianSort(leftSubTreeSpan, nextAxis);
                var leftChild = CreateNode(leftSubTreeSpan[nodeIndex], nextAxis);
                currentNode.LeftChild = leftChild;
                nodeStack.Push(new NodeBuildInfo(leftChild, nodeIndex, subTreeStart, leftSubTreeSpan.Length));
            }

            if (subTreePivot < subTreeLength - 1)
            {
                var rightSubTreeSpan = subTreeSpan.Slice(subTreePivot + 1);
                nodeIndex = PartialMedianSort(rightSubTreeSpan, nextAxis);
                var rightChild = CreateNode(rightSubTreeSpan[nodeIndex], nextAxis);
                currentNode.RightChild = rightChild;
                nodeStack.Push(new NodeBuildInfo(rightChild, nodeIndex, subTreeStart + subTreePivot + 1, rightSubTreeSpan.Length));
            }
        }

        return rootNode;
    }

    /// <summary>
    /// Rebuilds the entire KdTree without the specified elements.
    /// </summary>
    /// <param name="elementKeys">The element keys to remove from the partitioning.</param>
    protected void RemoveAndRebuild(ICollection<TElementKey> elementKeys)
    {
        if (Empty)
            return;

        if (elementKeys == null || elementKeys.Count == 0)
            return;

        var keysToRemoveCount = 0;
        foreach (var elementKey in elementKeys)
        {
            if (m_PartitionedElements.Contains(elementKey))
                keysToRemoveCount++;
        }

        if (keysToRemoveCount == 0)
            return;

        var newElements = new BoundingBoxElement[Count - keysToRemoveCount];
        var index = 0;
        IterateNodes(m_RootNode, (node, context) =>
        {
            if (!ContainsKey(elementKeys, node.ElementKey))
                newElements[index++] = new BoundingBoxElement(node.ElementKey, node.BoundingBox);
            context.Continuation = NodeIterationContinuation_Internal.ContinueAll;
        });

        Clear();
        if (newElements.Length == 0)
            return;

        m_RootNode = BuildTree(newElements, Axis_Internal.XMin);
        base.AddOrUpdateElements(newElements);
    }

    class UpdateAndRebuildVisitContext : NodeVisitContext_Internal
    {
        public int Index;
    }

    /// <summary>
    /// Rebuilds the entire KdTree with new and updated elements.
    /// </summary>
    /// <param name="elements">The elements to add and update in the KdTree.</param>
    protected void UpdateAndRebuild(IReadOnlyCollection<BoundingBoxElement> elements)
    {
        if (elements == null)
            return;

        if (elements.Count == 0)
            return;

        var newOrUpdatedElementKeys = ToElementKeysHashSet(elements);

        var elementAddedCount = 0;
        foreach (var elementKey in newOrUpdatedElementKeys)
        {
            if (!m_PartitionedElements.Contains(elementKey))
                ++elementAddedCount;
        }

        var newCount = Count + elementAddedCount;
        var newElements = new BoundingBoxElement[newCount];
        var visitContext = new UpdateAndRebuildVisitContext
        {
            Index = 0
        };
        IterateNodes(m_RootNode, visitContext, (node, context) =>
        {
            if (!newOrUpdatedElementKeys.Contains(node.ElementKey))
                newElements[context.Index++] = new BoundingBoxElement(node.ElementKey, node.BoundingBox);
            context.Continuation = NodeIterationContinuation_Internal.ContinueAll;
        });

        foreach (var element in elements)
        {
            newElements[visitContext.Index++] = element;
        }

        Clear();
        if (newElements.Length == 0)
            return;

        m_RootNode = BuildTree(newElements, Axis_Internal.XMin);
        base.AddOrUpdateElements(newElements);
    }

    /// <summary>
    /// Creates a node to put inside the KdTree.
    /// </summary>
    /// <param name="element">The element to partition.</param>
    /// <param name="currentAxis">The current splitting axis partitioning the element.</param>
    /// <returns></returns>
    protected static Node_Internal CreateNode(in BoundingBoxElement element, Axis_Internal currentAxis)
    {
        return new Node_Internal(element.Key, currentAxis, GetBoundingBoxValueByAxis(element, currentAxis), element.BoundingBox);
    }

    /// <summary>
    /// Gets the next splitting axis after the current one.
    /// </summary>
    /// <param name="axis">The current splitting axis.</param>
    /// <returns>The next splitting axis.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws an exception if the current axis has an invalid value.</exception>
    protected static Axis_Internal GetNextAxis(Axis_Internal axis)
    {
        switch (axis)
        {
            case Axis_Internal.XMin:
                return Axis_Internal.YMin;
            case Axis_Internal.YMin:
                return Axis_Internal.XMax;
            case Axis_Internal.XMax:
                return Axis_Internal.YMax;
            case Axis_Internal.YMax:
                return Axis_Internal.XMin;
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }
    }

    /// <summary>
    /// Gets the opposite axis of the current one.
    /// </summary>
    /// <param name="axis">The current splitting axis</param>
    /// <returns>The opposite axis.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws an exception if the current axis has an invalid value.</exception>
    protected static Axis_Internal GetOppositeAxis(Axis_Internal axis)
    {
        switch (axis)
        {
            case Axis_Internal.XMin:
                return Axis_Internal.XMax;
            case Axis_Internal.YMin:
                return Axis_Internal.YMax;
            case Axis_Internal.XMax:
                return Axis_Internal.XMin;
            case Axis_Internal.YMax:
                return Axis_Internal.YMin;
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }
    }

    /// <summary>
    /// Gets the bounding box value for the splitting axis.
    /// </summary>
    /// <param name="boundingBox">The bounding box.</param>
    /// <param name="axis">The splitting axis.</param>
    /// <returns>The bounding box value for the axis.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws an exception if the axis has an invalid value.</exception>
    protected static float GetBoundingBoxValueByAxis(in Rect boundingBox, Axis_Internal axis)
    {
        switch (axis)
        {
            case Axis_Internal.XMin:
                return boundingBox.xMin;
            case Axis_Internal.YMin:
                return boundingBox.yMin;
            case Axis_Internal.XMax:
                return boundingBox.xMax;
            case Axis_Internal.YMax:
                return boundingBox.yMax;
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }
    }

    /// <summary>
    /// Gets the vector value for the splitting axis.
    /// </summary>
    /// <param name="vector2">The vector.</param>
    /// <param name="axis">The splitting axis.</param>
    /// <returns>The value of the vector for the axis.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws an exception if the axis has an invalid value.</exception>
    protected static float GetVector2ValueByAxis(in Vector2 vector2, Axis_Internal axis)
    {
        var index = (int)axis & 0x1;
        return vector2[index];
    }

    /// <summary>
    /// Gets the bounding box value for the splitting axis.
    /// </summary>
    /// <param name="bbe">The partitioned element.</param>
    /// <param name="axis">The splitting axis.</param>
    /// <returns>The bounding box value for the axis.</returns>
    protected static float GetBoundingBoxValueByAxis(in BoundingBoxElement bbe, Axis_Internal axis)
    {
        return GetBoundingBoxValueByAxis(bbe.BoundingBox, axis);
    }

    /// <summary>
    /// Gets the bounding box value for the splitting axis.
    /// </summary>
    /// <param name="node">The node in the KdTree.</param>
    /// <param name="axis">The splitting axis.</param>
    /// <returns>The bounding box value for the axis.</returns>
    protected static float GetBoundingBoxValueByAxis(Node_Internal node, Axis_Internal axis)
    {
        return GetBoundingBoxValueByAxis(node.BoundingBox, axis);
    }

    /// <summary>
    /// Iterates over the nodes with a default <see cref="NodeVisitContext_Internal"/>.
    /// </summary>
    /// <param name="rootNode">The starting node.</param>
    /// <param name="visitor">The visitor callback.</param>
    protected void IterateNodes(Node_Internal rootNode, Action<Node_Internal, NodeVisitContext_Internal> visitor)
    {
        var context = new NodeVisitContext_Internal();
        IterateNodes(rootNode, context, visitor);
    }

    /// <summary>
    /// Iterates over the nodes with a specific <typeparamref name="TContext"/> visitor context.
    /// </summary>
    /// <typeparam name="TContext">The type of the visitor context.</typeparam>
    /// <param name="rootNode">The starting node.</param>
    /// <param name="nodeVisitContext">The visitor context.</param>
    /// <param name="visitor">The visitor callback.</param>
    protected void IterateNodes<TContext>(Node_Internal rootNode, TContext nodeVisitContext, Action<Node_Internal, TContext> visitor)
        where TContext : NodeVisitContext_Internal
    {
        if (rootNode == null)
            return;

        // Estimate the depth of the tree using Base2 Log.
        Stack<Node_Internal> nodesToVist = new Stack<Node_Internal>((int)Math.Log(Count, 2));
        nodesToVist.Push(rootNode);

        while (nodesToVist.Count > 0)
        {
            // Reset continuation value
            nodeVisitContext.Continuation = NodeIterationContinuation_Internal.ContinueAll;
            var visitedNode = nodesToVist.Pop();
            visitor(visitedNode, nodeVisitContext);
            if (nodeVisitContext.Continuation == NodeIterationContinuation_Internal.Stop)
                return;

            var visitLeft = nodeVisitContext.Continuation == NodeIterationContinuation_Internal.ContinueAll || nodeVisitContext.Continuation == NodeIterationContinuation_Internal.ContinueLeftOnly;
            var visitRight = nodeVisitContext.Continuation == NodeIterationContinuation_Internal.ContinueAll || nodeVisitContext.Continuation == NodeIterationContinuation_Internal.ContinueRightOnly;
            if (visitRight && visitedNode.RightChild != null)
                nodesToVist.Push(visitedNode.RightChild);
            if (visitLeft && visitedNode.LeftChild != null)
                nodesToVist.Push(visitedNode.LeftChild);
        }
    }

    /// <summary>
    /// Finds the best node according to an aggregator.
    /// </summary>
    /// <param name="rootNode">The starting node.</param>
    /// <param name="aggregator">The aggregator callback.</param>
    /// <returns></returns>
    protected Node_Internal FindNodeWithAggregator(Node_Internal rootNode, Func<Node_Internal, Node_Internal, NodeVisitContext_Internal, Node_Internal> aggregator)
    {
        var bestNode = rootNode;
        IterateNodes(rootNode, (currentNode, context) =>
        {
            bestNode = aggregator(bestNode, currentNode, context);
        });

        return bestNode;
    }

    static int PartialMedianSort(Span<BoundingBoxElement> elements, Axis_Internal currentAxis)
    {
        // The NthElement (or QuickSelect) algorithm finds the proper value at position N, in our case
        // the median. However, equal values can be on either side of this position and since we want
        // all lesser or equal values to be on the left side of the subtree, we have to find all equal values
        // after the median and put them alongside, and then use the last swapped position as the true median index.
        var median = elements.Length / 2;
        median = NthElement(elements, median, currentAxis);
        var medianValue = GetBoundingBoxValueByAxis(elements[median], currentAxis);

        var p = median + 1;
        for (var i = median + 1; i < elements.Length; ++i)
        {
            if (GetBoundingBoxValueByAxis(elements[i], currentAxis) <= medianValue)
            {
                if (i != p)
                {
                    Swap(elements, p, i);
                }
                median = p;
                ++p;
            }
        }

        return median;
    }

    static int NthElement(Span<BoundingBoxElement> elements, int n, Axis_Internal currentAxis)
    {
        var startIndex = 0;
        var endIndex = elements.Length - 1;

        // Selecting initial pivot
        var pivotIndex = n;

        while (endIndex > startIndex)
        {
            pivotIndex = QuickSelectPartition(elements, startIndex, endIndex, pivotIndex, currentAxis, out var allEqual);

            // If the partition contains all equal values, we can return early because the element at position n is part
            // of that partition.
            if (allEqual)
                return n;

            if (pivotIndex == n)
                // We found our n:th smallest value - it is stored at pivot index
                break;
            if (pivotIndex > n)
                // The n:th value is located before the pivot
                endIndex = pivotIndex - 1;
            else
                // The n:th value is located after the pivot
                startIndex = pivotIndex + 1;

            pivotIndex = (startIndex + endIndex) / 2;
        }
        return n;
    }

    static int QuickSelectPartition(Span<BoundingBoxElement> array, int startIndex, int endIndex, int pivotIndex, Axis_Internal currentAxis, out bool allEqual)
    {
        // This is a modified Lomuto's partition scheme to detect all equal values.
        allEqual = true;
        var pivotValue = GetBoundingBoxValueByAxis(array[pivotIndex], currentAxis);
        // Assume that the value at pivot index is the largest - move it to end
        Swap(array, pivotIndex, endIndex);
        for (var i = startIndex; i < endIndex; i++)
        {
            var currentValue = GetBoundingBoxValueByAxis(array[i], currentAxis);
            if (currentValue >= pivotValue)
            {
                if (currentValue > pivotValue)
                    allEqual = false;
                continue;
            }

            allEqual = false;
            // Value stored to i was smaller than the pivot value - let's move it to start
            Swap(array, i, startIndex);
            // Move start one index forward
            startIndex++;
        }
        // Start index is now pointing to index where we should store our pivot value from end of array
        Swap(array, endIndex, startIndex);
        return startIndex;
    }

    static void Swap(Span<BoundingBoxElement> array, int index1, int index2)
    {
        if (index1 == index2)
            return;

        var temp = array[index1];
        array[index1] = array[index2];
        array[index2] = temp;
    }

    /// <summary>
    /// Converts a key collection to a hashset if the collection is sufficiently large, otherwise
    /// it keeps it as is to avoid allocation.
    /// </summary>
    /// <param name="elementKeys">The key collection.</param>
    /// <param name="maxArraySize">The maximum size of the collection before it is converted to a hashset.</param>
    /// <returns>An ICollection of <typeparamref name="TElementKey"/>.</returns>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="elementKeys"/> is null.</exception>
    protected ICollection<TElementKey> ToBestKeyCollection(IReadOnlyCollection<TElementKey> elementKeys, int maxArraySize)
    {
        if (elementKeys == null)
            throw new ArgumentNullException(nameof(elementKeys));

        // If it is already an hashset, keep it as is.
        if (elementKeys is HashSet<TElementKey> hashSet)
            return hashSet;

        // If it is a small array or list, keep it as is.
        if (elementKeys is List<TElementKey> list && list.Count <= maxArraySize)
            return list;
        if (elementKeys is TElementKey[] arr && arr.Length <= maxArraySize)
            return arr;

        // Transform it into a hashset otherwise.
        return ToElementKeysHashSet(elementKeys);
    }

    /// <summary>
    /// Checks if a key is present in a collection of keys. This methods optimizes Contains for arrays and lists.
    /// </summary>
    /// <param name="elementKeys">The collection of key.</param>
    /// <param name="key">The key to find.</param>
    /// <returns>True if the key is found, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="elementKeys"/> is null.</exception>
    protected bool ContainsKey(ICollection<TElementKey> elementKeys, TElementKey key)
    {
        if (elementKeys == null) throw new ArgumentNullException(nameof(elementKeys));

        // Optimized version of Contains for array and list bypassing the default EqualityComparer,
        // since TElementKey is IEquatable<TElementKey>.
        if (elementKeys is TElementKey[] arr)
        {
            for (var i = 0; i < arr.Length; ++i)
            {
                if (key.Equals(arr[i]))
                    return true;
            }
            return false;
        }

        if (elementKeys is List<TElementKey> list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                if (key.Equals(list[i]))
                    return true;
            }
            return false;
        }

        return elementKeys.Contains(key);
    }

    /// <summary>
    /// Checks if a key is present in a list of <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}.BoundingBoxElement"/>.
    /// </summary>
    /// <param name="elements">The list of <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}.BoundingBoxElement"/>.</param>
    /// <param name="key">The key to find.</param>
    /// <returns>True if the key is found, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="elements"/> is null.</exception>
    protected bool ContainsKey(IReadOnlyCollection<BoundingBoxElement> elements, TElementKey key)
    {
        if (elements == null) throw new ArgumentNullException(nameof(elements));

        foreach (var boundingBoxElement in elements)
        {
            if (key.Equals(boundingBoxElement.Key))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Creates an array of <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}.BoundingBoxElement"/> from a read-only collection
    /// of <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}.BoundingBoxElement"/>.
    /// </summary>
    /// <param name="elements">The read-only collection of <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}.BoundingBoxElement"/>.</param>
    /// <returns>A mutable array of <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}.BoundingBoxElement"/>.</returns>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="elements"/> is null.</exception>
    protected BoundingBoxElement[] ToMutableArray(IReadOnlyCollection<BoundingBoxElement> elements)
    {
        if (elements == null) throw new ArgumentNullException(nameof(elements));
        var elementsArray = new BoundingBoxElement[elements.Count];
        var index = 0;
        foreach (var boundingBoxElement in elements)
        {
            elementsArray[index++] = boundingBoxElement;
        }
        return elementsArray;
    }
}
