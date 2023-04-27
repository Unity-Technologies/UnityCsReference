// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model to hold the result of the graph processing.
    /// </summary>
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class GraphProcessingErrorModel : ErrorMarkerModel
    {
        /// <summary>
        /// The GUID of the node to which the error is related.
        /// </summary>
        public SerializableGUID ParentNodeGuid { get; }

        /// <inheritdoc />
        public override LogType ErrorType { get; }

        /// <inheritdoc />
        public override string ErrorMessage { get; }

        /// <summary>
        /// The <see cref="QuickFix"/> for the error.
        /// </summary>
        public virtual QuickFix Fix { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphProcessingErrorModel" /> class.
        /// </summary>
        /// <param name="error">The <see cref="GraphProcessingError"/> used to initialize the instance.</param>
        public GraphProcessingErrorModel(GraphProcessingError error)
        {
            ParentNodeGuid = error.SourceNodeGuid;
            ErrorMessage = error.Description;
            ErrorType = error.IsWarning ? LogType.Warning : LogType.Error;
            Fix = error.Fix;
        }

        /// <inheritdoc />
        public override GraphElementModel GetParentModel(GraphModel graphModel)
        {
            return graphModel.GetModel(ParentNodeGuid);
        }
    }
}
