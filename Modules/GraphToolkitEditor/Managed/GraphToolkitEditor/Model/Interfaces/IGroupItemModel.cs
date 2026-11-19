// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An item that can be stored in a group.
    /// </summary>
    [UnityRestricted]
    internal interface IGroupItemModel
    {
        /// <summary>
        /// The group that contains this item.
        /// </summary>
        GroupModelBase ParentGroup { get; set; }

        /// <summary>
        /// This model and the models that this model contains.
        /// </summary>
        IEnumerable<GraphElementModel> ContainedModels { get; }

        bool IsInGroup(GroupModelBase group)
        {
            var currentGroup = ParentGroup;
            while (currentGroup != null)
            {
                if (currentGroup == group) return true;
                currentGroup = currentGroup.ParentGroup;
            }

            return false;
        }

        /// <summary>
        /// Gets a IGroupItemModel representing this IGroupItemModel for the given targetModel.
        /// </summary>
        /// <param name="targetModel">The model the clone belongs to.</param>
        /// <param name="variableTranslation">The map between the source variables and the target variables.</param>
        /// <returns>The cloned model.</returns>
        IGroupItemModel GetGroupItemInTargetGraph(GraphModel targetModel, Dictionary<VariableDeclarationModelBase, VariableDeclarationModelBase> variableTranslation);
    }
}
