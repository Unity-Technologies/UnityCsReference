// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A section (top-level group) in the blackboard.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class SectionModel : GroupModel
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
            SetCapability(Editor.Capabilities.Deletable, false);
            SetCapability(Editor.Capabilities.Droppable, false);
            SetCapability(Editor.Capabilities.Selectable, false);
            SetCapability(Editor.Capabilities.Renamable, false);
            SetCapability(Editor.Capabilities.Copiable, false);
        }

        /// <inheritdoc />
        public override IGraphElementContainer Container => GraphModel;

        /// <inheritdoc />
        public override SectionModel GetSection() => this;
    }
}
