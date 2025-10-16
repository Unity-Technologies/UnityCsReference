// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents a block node.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class BlockNodeModel : NodeModel, IRenamable
    {
        [SerializeReference]
        ContextNodeModel m_ContextNodeModel;

        internal static string contextNodeModelFieldName = nameof(m_ContextNodeModel);

        /// <summary>
        /// Whether this block must display a title.
        /// </summary>
        /// <remarks>A title, in addition to the text itself, also includes an icon and a collapse button.</remarks>
        public virtual bool ShouldShowTitle => true;

        /// <summary>
        /// The context the node belongs to.
        /// </summary>
        public virtual ContextNodeModel ContextNodeModel
        {
            get => m_ContextNodeModel;
            set => m_ContextNodeModel = value;
        }

        /// <inheritdoc />
        public override IGraphElementContainer Container => ContextNodeModel;

        /// <inheritdoc />
        public override bool UseColorAlpha => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNodeModel" /> class.
        /// </summary>
        public BlockNodeModel()
        {
            SetCapability(Editor.Capabilities.Movable, false);
            SetCapability(Editor.Capabilities.Ascendable, false);
            SetCapability(Editor.Capabilities.NeedsContainer, true);
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!IsRenamable())
                return;

            Title = newName;
        }

        /// <summary>
        /// Checks whether this block node is compatible with the given context.
        /// </summary>
        /// <param name="context">The context node to test compatibility with.</param>
        /// <returns>Whether this block node is compatible with the given context.</returns>
        public virtual bool IsCompatibleWith(ContextNodeModel context) => true;

        /// <summary>
        /// Retrieves the index of the block's position within the context.
        /// </summary>
        /// <returns>The index.</returns>
        /// <remarks>
        /// 'GetIndex' retrieves the index of the block's position within the context. The index represents the block's order
        /// relative to other blocks in the same context node, which can be useful for organization or processing logic.
        /// Use this method when determining the position of a block.
        /// </remarks>
        public virtual int GetIndex()
        {
            int cpt = 0;
            foreach (var block in ContextNodeModel.GetGraphElementModels())
            {
                if (ReferenceEquals(block, this))
                    return cpt;
                cpt++;
            }

            return -1;
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var nodeMenuItems = base.ContextualMenuItems;
                var menuItems = new List<ContextualMenuItem>(nodeMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            new ContextualMenuItem(ContextualMenuHelpers.insertBlockAboveItem, 0),
            new ContextualMenuItem(ContextualMenuHelpers.insertBlockBelowItem, 1),
        };
    }
}
