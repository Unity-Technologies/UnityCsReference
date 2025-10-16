// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Represents a specialized node that can only exist within a <see cref="ContextNode"/>.
    /// </summary>
    /// <remarks>
    /// Use block nodes to define logic or operations that are scoped within a <see cref="ContextNode"/>.
    /// A block node cannot be added directly to the graph; it must be a child of a valid context node type.
    /// Attempting to instantiate or add a <c>BlockNode</c> outside of a <see cref="ContextNode"/> will result in invalid behavior.
    /// To limit which context types can contain a block node, use the <see cref="UseWithContextAttribute"/>.
    /// </remarks>
    [Serializable]
    public abstract class BlockNode : Node
    {
        /// <summary>
        /// The <see cref="ContextNode"/> that contains this block node.
        /// </summary>
        /// <remarks>
        /// Every <see cref="BlockNode"/> must be part of a <see cref="ContextNode"/>. This property provides access to that parent context.
        /// </remarks>
        public ContextNode ContextNode => m_Implementation is UserBlockNodeModelImp blockNodeModel ? (blockNodeModel.ContextNodeModel as UserContextNodeModelImp)?.Node : null;

        /// <summary>
        /// The index of this block node within the list of blocks in the context node.
        /// </summary>
        /// <remarks>
        /// The index reflects the block's position in the parent <see cref="ContextNode"/>'s block list. The index is zero-based and
        /// determines the order in which blocks are displayed.
        /// </remarks>
        public int Index => m_Implementation is UserBlockNodeModelImp blockNodeModel ? blockNodeModel.GetIndex() : -1;
    }
}
