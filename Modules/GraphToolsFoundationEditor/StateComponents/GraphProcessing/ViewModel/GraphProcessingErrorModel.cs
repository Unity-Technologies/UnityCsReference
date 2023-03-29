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
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class GraphProcessingErrorModel : ErrorMarkerModel
    {
        /// <inheritdoc/>
        public override GraphModel GraphModel
        {
            get => ParentModel.GraphModel;
            set => throw new InvalidOperationException($"Cannot set the {nameof(GraphModel)} on a {nameof(GraphProcessingErrorModel)}");
        }

        /// <inheritdoc/>
        public override Color Color { get; set; }

        /// <inheritdoc/>
        public override bool HasUserColor => false;

        /// <inheritdoc/>
        public override void ResetColor()
        {
        }

        /// <inheritdoc />
        public override GraphElementModel ParentModel { get; }

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
            ParentModel = error.SourceNode;
            ErrorMessage = error.Description;
            ErrorType = error.IsWarning ? LogType.Warning : LogType.Error;
            Fix = error.Fix;
        }
    }
}
