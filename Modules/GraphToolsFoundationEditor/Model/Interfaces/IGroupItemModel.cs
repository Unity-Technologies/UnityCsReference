// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An item that can be stored in a group.
    /// </summary>
    interface IGroupItemModel
    {
        /// <summary>
        /// The group that contains this item.
        /// </summary>
        GroupModel ParentGroup { get; set; }

        /// <summary>
        /// This model and the models that this model contains.
        /// </summary>
        IEnumerable<GraphElementModel> ContainedModels { get; }
    }
}
