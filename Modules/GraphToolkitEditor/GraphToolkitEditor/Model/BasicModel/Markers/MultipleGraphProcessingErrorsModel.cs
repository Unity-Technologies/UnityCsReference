// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model to hold <see cref="GraphProcessingError"/>s that are associated to the same parent model.
    /// </summary>
    [UnityRestricted]
    internal class MultipleGraphProcessingErrorsModel : ErrorMarkerModel
    {
        /// <summary>
        /// The GUID of the model to which the errors are associated with.
        /// </summary>
        public Hash128 ParentModelGuid { get; }

        /// <inheritdoc />
        public override LogType ErrorType { get; }

        /// <inheritdoc />
        public override string ErrorMessage { get; }

        /// <summary>
        /// The errors associated with the parent model.
        /// </summary>
        public List<GraphProcessingErrorModel> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleGraphProcessingErrorsModel" /> class.
        /// </summary>
        /// <param name="parentModelGuid">The GUID of the parent model.</param>
        /// <param name="errors">The <see cref="GraphProcessingError"/>s associated with the parent model.</param>
        public MultipleGraphProcessingErrorsModel(Hash128 parentModelGuid, List<GraphProcessingErrorModel> errors)
        {
            ParentModelGuid = parentModelGuid;
            Errors = errors;

            // If there is only 1 error, the error message is the error's verbatim. Else, it is a generic message with the error count.
            if (Errors.Count > 0)
                ErrorMessage = Errors.Count > 1 ? $"There are {Errors.Count} issues. Click the badge to see more." : Errors[0].ErrorMessage;

            ErrorType = LogType.Log;
            foreach (var error in Errors)
            {
                // If there is at least 1 issue that is an error, this model's type is error.
                if (error.ErrorType == LogType.Error)
                {
                    ErrorType = LogType.Error;
                    break;
                }

                // If there is no error and at least 1 issue that is a warning, this model's type is warning.
                if (error.ErrorType == LogType.Warning)
                {
                    ErrorType = LogType.Warning;
                }
            }
        }

        /// <inheritdoc />
        public override GraphElementModel GetParentModel(GraphModel graphModel)
        {
            return graphModel.GetModel(ParentModelGuid);
        }
    }
}
