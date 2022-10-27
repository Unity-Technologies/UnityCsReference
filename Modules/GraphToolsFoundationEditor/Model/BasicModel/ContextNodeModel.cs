// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The model for context nodes.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class ContextNodeModel : NodeModel, IGraphElementContainer
    {
        [SerializeReference]
        List<BlockNodeModel> m_Blocks = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextNodeModel"/> class.
        /// </summary>
        public ContextNodeModel()
        {
            this.SetCapability(Editor.Capabilities.Collapsible, false);
        }

        /// <summary>
        /// Inserts a block in the context.
        /// </summary>
        /// <param name="blockModel">The block model to insert.</param>
        /// <param name="index">The index at which insert the block. -1 means at the end of the list.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        public void InsertBlock(BlockNodeModel blockModel, int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (blockModel.ContextNodeModel != null)
                blockModel.ContextNodeModel.RemoveElements(new[] { blockModel });

            if (index > m_Blocks.Count)
                throw new ArgumentException(nameof(index));
            if (!blockModel.IsCompatibleWith(this) && GetType() != typeof(ContextNodeModel)) // Blocks have to be compatible with the base ContextNodeModel because of the item library's "Dummy Context".
                throw new ArgumentException(nameof(blockModel));

            if ((spawnFlags & SpawnFlags.Orphan) == 0)
                GraphModel.RegisterElement(blockModel);

            if (index < 0 || index == m_Blocks.Count)
                m_Blocks.Add(blockModel);
            else
                m_Blocks.Insert(index, blockModel);

            blockModel.GraphModel = GraphModel;
            blockModel.ContextNodeModel = this;
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            foreach (var block in m_Blocks)
            {
                block.GraphModel = GraphModel;
            }
            base.OnDuplicateNode(sourceNode);

        }

        /// <summary>
        /// Creates a new block and inserts it in the context.
        /// </summary>
        /// <param name="blockType">The type of block to instantiate.</param>
        /// <param name="index">The index at which insert the block. -1 means at the end of the list.</param>
        /// <param name="guid">The GUID of the new block.</param>
        /// <param name="initializationCallback">A callback called once the block is ready.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created block.</returns>
        public BlockNodeModel CreateAndInsertBlock(Type blockType, int index = -1, SerializableGUID guid = default, Action<AbstractNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            //use SpawnFlags.Orphan to prevent adding the node to the GraphModel
            var block = (BlockNodeModel)GraphModel.CreateNode(blockType, blockType.Name, Vector2.zero, guid, initializationCallback, spawnFlags | SpawnFlags.Orphan);

            InsertBlock(block, index, spawnFlags);

            return block;
        }

        /// <summary>
        /// Creates a new block and inserts it in the context.
        /// </summary>
        /// <typeparam name="T">The type of block to instantiate.</typeparam>
        /// <param name="index">The index at which insert the block. -1 means at the end of the list</param>
        /// <param name="guid">The GUID of the new block</param>
        /// <param name="initializationCallback">A callback called once the block is ready</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created block.</returns>
        public T CreateAndInsertBlock<T>(int index = -1, SerializableGUID guid = default,
            Action<AbstractNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default) where T : BlockNodeModel, new()
        {
            return (T)CreateAndInsertBlock(typeof(T), index, guid, initializationCallback, spawnFlags);
        }

        public IEnumerable<GraphElementModel> GraphElementModels => m_Blocks;

        public void RemoveElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            foreach (var blockNodeModel in elementModels.OfType<BlockNodeModel>())
            {
                GraphModel.UnregisterElement(blockNodeModel);
                if (!m_Blocks.Remove(blockNodeModel))
                    throw new ArgumentException(nameof(blockNodeModel));
                blockNodeModel.ContextNodeModel = null;
            }
        }

        /// <inheritdoc/>
        protected override void OnDefineNode()
        {
            foreach (var block in GraphElementModels)
            {
                (block as BlockNodeModel)?.DefineNode();
            }
        }

        /// <inheritdoc />
        public void Repair()
        {
            m_Blocks.RemoveAll(t => t == null);
        }
    }
}
