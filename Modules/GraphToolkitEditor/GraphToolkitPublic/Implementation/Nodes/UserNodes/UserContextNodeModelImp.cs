// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor.Implementation
{
    partial class UserContextNodeModelImp
    {
        [NonSerialized]
        new List<BlockNode> m_Blocks;

        public IReadOnlyList<BlockNode> Blocks
        {
            get
            {
                BuildBlocksList();
                return m_Blocks;
            }
        }

        void BuildBlocksList()
        {
            if (m_Blocks == null)
            {
                m_Blocks = new List<BlockNode>(base.m_Blocks.Count);
                foreach (var block in base.m_Blocks)
                {
                    if (block != null)
                        m_Blocks.Add(((UserBlockNodeModelImp)block).Node);
                }
            }
        }

        public void AddBlock(UserBlockNodeModelImp userBlockNodeModelImp)
        {
            if (m_Blocks == null)
                BuildBlocksList();
            else
            {
                //The node is added to the block list before this is called so BuildBlocksList() will add the added block as well.
                var index = userBlockNodeModelImp.GetIndex();
                m_Blocks.Insert(index, userBlockNodeModelImp.Node);
            }
        }

        public void RemoveBlock(UserBlockNodeModelImp userBlockNodeModelImp)
        {
            ((IUserNodeModelImp)userBlockNodeModelImp).CallOnDisable();
            m_Blocks?.Remove(userBlockNodeModelImp.Node);
        }

        void IUserNodeModelImp.CallOnEnable()
        {
            m_OnEnableCalled = true;
            Node?.OnEnable();
            foreach (var block in GetGraphElementModels())
            {
                if (block is IUserNodeModelImp userBlockNodeModelImp)
                {
                    userBlockNodeModelImp.CallOnEnable();
                }
            }
        }

        void IUserNodeModelImp.CallOnDisable()
        {
            m_OnEnableCalled = false;
            foreach (var block in GetGraphElementModels())
            {
                if (block is IUserNodeModelImp userBlockNodeModelImp)
                {
                    userBlockNodeModelImp.CallOnDisable();
                }
            }
            Node?.OnDisable();
        }

        public TBlockNode CreateBlockNode<TBlockNode>(int index = -1) where TBlockNode : BlockNode, new()
        {
            if (m_Node?.Graph is null)
                return null;

            CheckModificationLock();

            var validBlockTypes = PublicGraphFactory.GetBlockTypes(m_Node.Graph.GetType(), m_Node.GetType());

            if (!validBlockTypes.Contains(typeof(TBlockNode).GetGenericTypeDefinition()))
                throw new ArgumentException($"The type '{typeof(TBlockNode).Name}' is not supported by context '{m_Node.m_Implementation.Title}'.");

            var data = new GraphBlockCreationData(GraphModel, contextNodeModel: this, orderInContext:index);

            var blockModel = GraphModelImp.CreateContextFromBlockData(data, typeof(TBlockNode), null);

            // Find the created block in our list and return it.
            foreach (var block in Blocks)
            {
                if (block.m_Implementation == blockModel)
                    return (TBlockNode)block;
            }

            return null;
        }

        public void RemoveBlockNode(BlockNode blockNode)
        {
            if (m_Node?.Graph is null)
                return;

            CheckModificationLock();

            // Ensure the block belongs to this context.
            if (blockNode.m_Implementation is not UserBlockNodeModelImp blockNodeModel || blockNodeModel.ContextNodeModel != this)
                return;

            RemoveContainerElements([blockNodeModel]);
        }

        public void AddBlockNode(BlockNode blockNode, int index = -1)
        {
            // No need to check if m_Node?.Graph is null. It should be possible to add a block to a context that is not yet in a graph.

            CheckModificationLock();

            var nodeImp = (BlockNodeModel)blockNode.GetImplementation();

            InsertBlock(nodeImp, index);
        }

        public void ClearBlockNodes()
        {
            while (Blocks.Count > 0)
            {
                RemoveBlockNode(Blocks[^1]);
            }
        }
    }
}
