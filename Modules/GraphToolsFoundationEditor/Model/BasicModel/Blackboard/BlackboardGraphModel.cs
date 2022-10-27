// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a blackboard for a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class BlackboardGraphModel : Model
    {
        /// <summary>
        /// The graph model to which the element belongs.
        /// </summary>
        public GraphModel GraphModel { get; set; }

        /// <summary>
        /// Whether the model is valid.
        /// </summary>
        public virtual bool Valid => GraphModel != null;

        /// <summary>
        /// Gets the title of the blackboard.
        /// </summary>
        /// <returns>The title of the blackboard.</returns>
        public virtual string GetBlackboardTitle()
        {
            return GraphModel?.GetFriendlyScriptName() ?? "";
        }

        /// <summary>
        /// Gets the sub-title of the blackboard.
        /// </summary>
        /// <returns>The sub-title of the blackboard.</returns>
        public virtual string GetBlackboardSubTitle()
        {
            return "Class Library";
        }
    }
}
