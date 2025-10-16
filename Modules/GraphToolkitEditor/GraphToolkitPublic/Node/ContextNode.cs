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
    }
}
