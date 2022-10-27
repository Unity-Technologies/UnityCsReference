// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A section (top-level group) in the blackboard.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class SectionModel : GroupModel
    {
        /// <summary>
        /// Returns whether the given item can be dragged in this section.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <returns>Whether the given item can be dragged in this section.</returns>
        public virtual bool AcceptsDraggedModel(IGroupItemModel itemModel)
        {
            return itemModel.GetSection() == this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionModel" /> class.
        /// </summary>
        public SectionModel()
        {
            this.SetCapability(Editor.Capabilities.Deletable, false);
            this.SetCapability(Editor.Capabilities.Droppable, false);
            this.SetCapability(Editor.Capabilities.Selectable, false);
            this.SetCapability(Editor.Capabilities.Renamable, false);
            this.SetCapability(Editor.Capabilities.Copiable, false);
        }

        /// <inheritdoc />
        public override IGraphElementContainer Container => GraphModel;
    }
}
