// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A container for graph processing errors and warnings.
    /// </summary>
    class ErrorsAndWarningsResult : BaseGraphProcessingResult
    {
        readonly List<GraphProcessingError> m_Errors = new();

        /// <summary>
        /// The errors.
        /// </summary>
        public IReadOnlyList<GraphProcessingError> Errors => m_Errors;

        /// <summary>
        /// Adds an error.
        /// </summary>
        /// <param name="description">Error description.</param>
        /// <param name="model">The model associated with the error.</param>
        /// <param name="graphLogAction">How to fix this error.</param>
        /// <param name="userData">User-provided data associated with the error.</param>
        public void AddError(string description, Model model = null, GraphLogAction graphLogAction = null, object userData = null)
        {
            AddError(description, model, LogType.Error, graphLogAction, userData: userData);
        }

        /// <summary>
        /// Adds an error with a context.
        /// </summary>
        /// <param name="description">Error description.</param>
        /// <param name="context">The context of the error.</param>
        /// <param name="graphLogAction">How to fix this error.</param>
        /// <param name="userData">User-provided data associated with the error.</param>
        /// <remarks>A context is a path of models to the source of the error. The last element of the list is the source of the error.</remarks>
        public void AddError(string description, IReadOnlyList<GraphElementModel> context, GraphLogAction graphLogAction = null, object userData = null)
        {
            AddError(description, context?[^1], LogType.Error, graphLogAction, context, userData);
        }

        /// <summary>
        /// Adds a warning with a context.
        /// </summary>
        /// <param name="description">Warning description.</param>
        /// <param name="context">The context of the warning.</param>
        /// <param name="graphLogAction">An action to invoke on the given context.</param>
        /// <param name="userData">User-provided data associated with the warning.</param>
        /// <remarks>A context is a path of models to the source of the warning. The last element of the list is the source of the warning.</remarks>
        public void AddWarning(string description, IReadOnlyList<GraphElementModel> context,
            GraphLogAction graphLogAction = null, object userData = null)
        {
            AddError(description, context?[^1], LogType.Warning, graphLogAction, context, userData);
        }

        /// <summary>
        /// Adds a message with a context.
        /// </summary>
        /// <param name="description">Message description.</param>
        /// <param name="context">The context of the message.</param>
        /// <param name="graphLogAction">An action to invoke on the given context.</param>
        /// <param name="userData">User-provided data associated with the message.</param>
        /// <remarks>A context is a path of models to the source of the message. The last element of the list is the source of the message.</remarks>
        public void AddMessage(string description, IReadOnlyList<GraphElementModel> context,
            GraphLogAction graphLogAction = null, object userData = null)
        {
            AddError(description, context?[^1], LogType.Log, graphLogAction, context, userData);
        }

        /// <summary>
        /// Adds a warning.
        /// </summary>
        /// <param name="description">Warning description.</param>
        /// <param name="model">The model associated with the warning.</param>
        /// <param name="graphLogAction">How to fix this warning.</param>
        /// <param name="userData">User-provided data associated with the warning.</param>
        public void AddWarning(string description, Model model = null, GraphLogAction graphLogAction = null, object userData = null)
        {
            AddError(description, model, LogType.Warning, graphLogAction, userData: userData);
        }

        /// <summary>
        /// Adds a message.
        /// </summary>
        /// <param name="description">Message description.</param>
        /// <param name="model">The model associated with the message.</param>
        /// <param name="graphLogAction">How to fix this message.</param>
        /// <param name="userData">User-provided data associated with the message.</param>
        public void AddMessage(string description, Model model = null, GraphLogAction graphLogAction = null, object userData = null)
        {
            AddError(description, model, LogType.Log, graphLogAction, userData: userData);
        }

        void AddError(string desc, Model model, LogType errorType, GraphLogAction graphLogAction, IReadOnlyList<GraphElementModel> context = null, object userData = null)
        {
            var error = new GraphProcessingError(
                desc,
                model?.Guid ?? default,
                errorType,
                (model as GraphElementModel)?.GraphModel.GetGraphReference() ?? default,
                context != null
                    ? new List<GraphElementModel>(context)
                    : null, // Make a copy of the list to ensure it isn't modified
                graphLogAction,
                userData);

            if (!m_Errors.Contains(error))
                m_Errors.Add(error);
        }
    }
}
