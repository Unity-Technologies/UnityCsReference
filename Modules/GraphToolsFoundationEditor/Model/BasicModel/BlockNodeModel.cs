// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a block node.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    abstract class BlockNodeModel : NodeModel
    {
        [SerializeReference]
        ContextNodeModel m_ContextNodeModel;

        /// <summary>
        /// The context the node belongs to
        /// </summary>
        public virtual ContextNodeModel ContextNodeModel
        {
            get => m_ContextNodeModel;
            set => m_ContextNodeModel = value;
        }

        /// <inheritdoc />
        public override IGraphElementContainer Container => ContextNodeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNodeModel" /> class.
        /// </summary>
        public BlockNodeModel()
        {
            this.SetCapability(Editor.Capabilities.Movable, false);
            this.SetCapability(Editor.Capabilities.Ascendable, false);
            this.SetCapability(Editor.Capabilities.NeedsContainer, true);
        }

        /// <summary>
        /// Checks whether this block node is compatible with the given context.
        /// </summary>
        /// <param name="context">The context node to test compatibility with.</param>
        /// <returns>Whether this block node is compatible with the given context.</returns>
        public virtual bool IsCompatibleWith(ContextNodeModel context) => true;

        /// <summary>
        /// The index of the position in the context.
        /// </summary>
        public virtual int GetIndex()
        {
            int cpt = 0;
            foreach (var block in ContextNodeModel.GraphElementModels)
            {
                if (ReferenceEquals(block, this))
                    return cpt;
                cpt++;
            }

            return -1;
        }
    }
}
