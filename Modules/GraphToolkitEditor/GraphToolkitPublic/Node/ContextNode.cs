// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A specialized node that serves as a dynamic container for compatible <see cref="BlockNode"/> instances.
    /// </summary>
    /// <remarks>
    /// Context nodes are node structures that group and control a sequence of <see cref="BlockNode"/> instances.
    /// A <see cref="ContextNode"/> owns a list of <see cref="BlockNode"/>s. These blocks cannot exist independently
    /// in the graph and must always be children of a context node.
    /// <br/>
    /// <br/>
    /// To control which block node types are valid for a specific context node type, use the
    /// <see cref="UseWithContextAttribute"/> on the corresponding <see cref="BlockNode"/> class.
    /// </remarks>
    [Serializable]
    public abstract class ContextNode : Node
    {
        internal override void CreateImplementation()
        {
            new UserContextNodeModelImp().InitCustomNode(this);
        }

        /// <summary>
        /// The number of <see cref="BlockNode"/>s contained in this context node.
        /// </summary>
        /// <remarks>
        /// The block count reflects the number of block nodes currently managed by the context node.
        /// This value may change dynamically as blocks are added or removed.
        /// </remarks>
        public int BlockCount => m_Implementation is UserContextNodeModelImp contextNodeModel ? contextNodeModel.BlockCount : 0;

        /// <summary>
        /// Retrieves the <see cref="BlockNode"/> at the specified index in the context node's block list.
        /// </summary>
        /// <param name="index">The zero-based index of the block to retrieve.</param>
        /// <returns>The <see cref="BlockNode"/> at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the <paramref name="index"/> is less than 0 or greater than or equal to the <see cref="BlockCount"/>.
        /// </exception>
        /// <remarks>
        /// The index is zero-based and corresponds to the block’s position within <see cref="BlockNodes"/>.
        /// </remarks>
        public BlockNode GetBlock(int index)
        {
            if (m_Implementation is UserContextNodeModelImp contextNodeModel)
            {
                return contextNodeModel.Blocks[index];
            }
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index is out of range for the blocks in this context node.");
        }

        /// <summary>
        /// An <c>IEnumerable</c> collection of the <see cref="BlockNode"/>s contained in this context node.
        /// </summary>
        /// <remarks>
        /// The returned collection reflects the current order and content of the context's block list.
        /// </remarks>
        public IEnumerable<BlockNode> BlockNodes => m_Implementation is UserContextNodeModelImp contextNodeModel ? contextNodeModel.Blocks : Array.Empty<BlockNode>();

        /// <summary>
        /// Adds an existing <see cref="BlockNode"/> to this context node.
        /// </summary>
        /// <param name="blockNode">The <see cref="BlockNode"/> to add.</param>
        /// <remarks>
        /// Use this method to add an existing block node to the end of this context node.
        /// Consider using <see cref="InsertBlockNode(int, BlockNode)"/> to insert at a specific position,
        /// or <see cref="CreateBlockNode{TBlockNode}(int)"/> to create and add a new block node.
        /// When the block node already belongs to another context node, it is removed from that context and added to this one.
        /// Throws <see cref="ArgumentException"/> if the block node is not compatible with this context node.
        /// </remarks>
        public void AddBlockNode(BlockNode blockNode)
        {
            if (GetImplementation() is not UserContextNodeModelImp contextNodeModel)
                return;

            contextNodeModel.AddBlockNode(blockNode);
        }

        /// <summary>
        /// Inserts an existing <see cref="BlockNode"/> at the specified index in this context node.
        /// </summary>
        /// <param name="index">The zero-based index where to insert the block node.</param>
        /// <param name="blockNode">The <see cref="BlockNode"/> to insert.</param>
        /// <remarks>
        /// Use this method when you have an existing block node instance that you want to add to the context at a specific position.
        /// Consider using <see cref="CreateBlockNode{TBlockNode}(int)"/> to create and add a new block node,
        /// or <see cref="AddBlockNode(BlockNode)"/> to append the block at the end.
        /// Blocks are ordered vertically: index 0 is the topmost block, and higher indices go toward the bottom.
        /// When <paramref name="index"/> is less than 0 or equal to the current block count, the block is added at the bottom.
        /// When the block node already belongs to another context node, it is removed from that context and added to this one.
        /// Throws <see cref="ArgumentException"/> when <paramref name="index"/> is greater than the current block count.
        /// </remarks>
        public void InsertBlockNode(int index, BlockNode blockNode)
        {
            if (GetImplementation() is not UserContextNodeModelImp contextNodeModel)
                return;

            contextNodeModel.AddBlockNode(blockNode, index);
        }

        /// <summary>
        /// Creates and inserts a new <see cref="BlockNode"/> into this context node.
        /// </summary>
        /// <param name="index">Optional. The zero-based index where to insert the block node. Defaults to -1, which appends the block at the bottom of the context node.</param>
        /// <typeparam name="TBlockNode">The type of the block node to create and insert. Must inherit from <see cref="BlockNode"/>.</typeparam>
        /// <remarks>
        /// Use this method when you want to create and add a new block node of a specific type to the context.
        /// Consider using <see cref="AddBlockNode(BlockNode)"/> or <see cref="InsertBlockNode(int, BlockNode)"/> when you already have an existing block node instance,
        /// Blocks are ordered vertically: index 0 is the topmost block, and higher indices go toward the bottom.
        /// When <paramref name="index"/> is less than 0 or equal to the current block count, the block is added at the bottom.
        /// Throws <see cref="ArgumentException"/> if <paramref name="index"/> is greater than the current block count.
        /// </remarks>
        public void CreateBlockNode<TBlockNode>(int index = -1) where TBlockNode : BlockNode, new()
        {
            CreateBlockNode(typeof(TBlockNode), index);
        }

        /// <summary>
        /// Creates and inserts a new <see cref="BlockNode"/> into this context node.
        /// </summary>
        /// <param name="blockNodeType">The type of the block node to create and insert. Must inherit from <see cref="BlockNode"/>..</param>
        /// <param name="index">Optional. The zero-based index where to insert the block node. Defaults to -1, which appends the block at the bottom of the context node.</param>
        /// <remarks>
        /// Use this method when you want to create and add a new block node of a specific type to the context.
        /// Consider using <see cref="AddBlockNode(BlockNode)"/> or <see cref="InsertBlockNode(int, BlockNode)"/> when you already have an existing block node instance,
        /// Blocks are ordered vertically: index 0 is the topmost block, and higher indices go toward the bottom.
        /// When <paramref name="index"/> is less than 0 or equal to the current block count, the block is added at the bottom.
        /// Throws <see cref="ArgumentException"/> if <paramref name="index"/> is greater than the current block count.
        /// </remarks>
        public void CreateBlockNode(Type blockNodeType, int index = -1)
        {
            if (GetImplementation() is not UserContextNodeModelImp contextNodeModel)
                return;

            contextNodeModel.AddBlockNode((BlockNode)Activator.CreateInstance(blockNodeType), index);
        }

        /// <summary>
        /// Removes the specified <see cref="BlockNode"/> from this context node.
        /// </summary>
        /// <param name="blockNode">The <see cref="BlockNode"/> to remove.</param>
        /// <remarks>
        /// If the specified block node is not part of this context node, the method does nothing.
        /// </remarks>
        public void RemoveBlockNode(BlockNode blockNode)
        {
            if (GetImplementation() is not UserContextNodeModelImp contextNodeModel)
                return;

            contextNodeModel.RemoveBlockNode(blockNode);
        }

        /// <summary>
        /// Removes all <see cref="BlockNode"/>s from this context node.
        /// </summary>
        public void ClearBlockNodes()
        {
            if (GetImplementation() is not UserContextNodeModelImp contextNodeModel)
                return;

            contextNodeModel.ClearBlockNodes();
        }
    }
}
