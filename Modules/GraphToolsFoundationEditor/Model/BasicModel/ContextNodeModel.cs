// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

        [SerializeField]
        List<SerializableGUID> m_BlockGuids = new List<SerializableGUID>();

        internal static string blocksFieldName_Internal = nameof(m_Blocks);

        List<BlockNodePlaceholder> m_BlockPlaceholders = new List<BlockNodePlaceholder>();

        public IReadOnlyList<BlockNodeModel> BlockPlaceholders => m_BlockPlaceholders;
        public IReadOnlyList<SerializableGUID> BlockGuids => m_BlockGuids;

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
            if (blockModel is BlockNodePlaceholder placeholder)
                m_BlockPlaceholders.Add(placeholder);
            else
            {
                if ((spawnFlags & SpawnFlags.Orphan) == 0)
                    GraphModel.RegisterElement(blockModel);

                if (blockModel.ContextNodeModel != null)
                    blockModel.ContextNodeModel.RemoveElements(new[] { blockModel });

                if (index > m_Blocks.Count)
                    throw new ArgumentException(nameof(index));

                if (!blockModel.IsCompatibleWith(this) && GetType() != typeof(ContextNodeModel)) // Blocks have to be compatible with the base ContextNodeModel because of the item library's "Dummy Context".
                    throw new ArgumentException(nameof(blockModel));

                if (index < 0 || index == m_Blocks.Count)
                {
                    m_Blocks.Add(blockModel);
                    m_BlockGuids.Add(blockModel.Guid);
                }
                else
                {
                    m_Blocks.Insert(index, blockModel);
                    m_BlockGuids.Insert(index, blockModel.Guid);
                }
            }

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
                GraphModel?.UnregisterElement(blockNodeModel);
                if (!RemoveBlock(blockNodeModel))
                {
                    throw new ArgumentException(nameof(blockNodeModel));
                }
                blockNodeModel.ContextNodeModel = null;
            }

            if (!m_BlockPlaceholders.Any())
                this.SetCapability(Editor.Capabilities.Copiable, true);
        }

        /// <inheritdoc/>
        protected override void OnDefineNode()
        {
            for (var i = 0; i < GraphElementModels.Count(); ++i)
            {
                var block = GraphElementModels.ElementAt(i);
                if (block is BlockNodeModel blockNodeModel)
                {
                    blockNodeModel.ContextNodeModel = this;
                    blockNodeModel.DefineNode();
                }
            }

            if (m_BlockPlaceholders.Any())
                this.SetCapability(Editor.Capabilities.Copiable, false);
        }

        /// <inheritdoc />
        public void Repair()
        {
            for (var i = m_Blocks.Count - 1; i >= 0; i--)
            {
                if (m_Blocks[i] == null)
                {
                    if (i < m_Blocks.Count)
                        m_Blocks.RemoveAt(i);
                    if (i < m_BlockGuids.Count)
                        m_BlockGuids.RemoveAt(i);
                }
            }
            m_BlockPlaceholders.Clear();
        }

        bool RemoveBlock(BlockNodeModel blockNodeModel)
        {
            int indexToRemove;

            if (blockNodeModel is BlockNodePlaceholder blockNodePlaceholder)
            {
                // When removing a placeholder block, we also remove the corresponding null block.
                indexToRemove = m_BlockGuids.IndexOf(blockNodePlaceholder.Guid);
                if (indexToRemove != -1)
                {
                    m_Blocks.RemoveAt(indexToRemove);
                    m_BlockGuids.RemoveAt(indexToRemove);
                    SerializationUtility.ClearManagedReferenceWithMissingType(GraphModel.Asset, blockNodePlaceholder.ReferenceId);
                }

                return m_BlockPlaceholders.Remove(blockNodePlaceholder);
            }

            indexToRemove = m_Blocks.IndexOf(blockNodeModel);
            if (indexToRemove != -1)
            {
                m_Blocks.RemoveAt(indexToRemove);
                m_BlockGuids.RemoveAt(indexToRemove);
                return true;
            }

            return false;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            // Remove outdated placeholders
            for (var i = m_BlockPlaceholders.Count - 1; i >= 0; i--)
            {
                if (m_Blocks.Any(b => b?.Guid == m_BlockPlaceholders[i].Guid))
                    m_BlockPlaceholders.RemoveAt(i);
            }

            // For compatibility with old version or corruption
            if (m_BlockGuids == null || m_BlockGuids.Count < m_Blocks.Count)
            {
                if (m_BlockGuids == null)
                    m_BlockGuids = new List<SerializableGUID>();

                m_BlockGuids.Clear();

                m_Blocks = m_Blocks.Where(t=>t != null).ToList();

                for (int i = 0; i < m_Blocks.Count; ++i)
                {
                    m_BlockGuids.Add(m_Blocks[i].Guid);
                }
            }
        }
    }
}
