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
    [UnityRestricted]
    internal class ErrorsAndWarningsResult : BaseGraphProcessingResult
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
        /// <param name="quickFix">How to fix this error.</param>
        public void AddError(string description, Model model = null, QuickFix quickFix = null)
        {
            AddError(description, model, LogType.Error, quickFix);
        }

        /// <summary>
        /// Adds an error with a context.
        /// </summary>
        /// <param name="description">Error description.</param>
        /// <param name="context">The context of the error.</param>
        /// <param name="quickFix">How to fix this error.</param>
        /// <remarks>A context is a path of models to the source of the error. The last element of the list is the source of the error.</remarks>
        public void AddError(string description, IReadOnlyList<GraphElementModel> context, QuickFix quickFix = null)
        {
            AddError(description, context?[^1], LogType.Error, quickFix, context);
        }

        /// <summary>
        /// Adds a warning.
        /// </summary>
        /// <param name="description">Warning description.</param>
        /// <param name="model">The model associated with the warning.</param>
        /// <param name="quickFix">How to fix this warning.</param>
        public void AddWarning(string description, Model model = null, QuickFix quickFix = null)
        {
            AddError(description, model, LogType.Warning, quickFix);
        }

        /// <summary>
        /// Adds a message.
        /// </summary>
        /// <param name="description">Message description.</param>
        /// <param name="model">The model associated with the message.</param>
        /// <param name="quickFix">How to fix this message.</param>
        public void AddMessage(string description, Model model = null, QuickFix quickFix = null)
        {
            AddError(description, model, LogType.Log, quickFix);
        }

        void AddError(string desc, Model model, LogType errorType, QuickFix quickFix, IReadOnlyList<GraphElementModel> context = null)
        {
            var error = new GraphProcessingError(
                desc,
                model?.Guid ?? default,
                errorType,
                (model as GraphElementModel)?.GraphModel.GetGraphReference() ?? default,
                context != null ? new List<GraphElementModel>(context) : null, // Make a copy of the list to make sure it is not modified
                quickFix);

            if (!m_Errors.Contains(error))
                m_Errors.Add(error);
        }
    }
}
