// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
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
        /// <param name="node">The node associated with the error.</param>
        /// <param name="quickFix">How to fix this error.</param>
        public void AddError(string description, AbstractNodeModel node = null, QuickFix quickFix = null)
        {
            AddError(description, node, false, quickFix);
        }

        /// <summary>
        /// Adds a warning.
        /// </summary>
        /// <param name="description">Warning description.</param>
        /// <param name="node">The node associated with the warning.</param>
        /// <param name="quickFix">How to fix this warning.</param>
        public void AddWarning(string description, AbstractNodeModel node = null, QuickFix quickFix = null)
        {
            AddError(description, node, true, quickFix);
        }

        void AddError(string desc, AbstractNodeModel node, bool isWarning, QuickFix quickFix)
        {
            var error = new GraphProcessingError
            {
                Description = desc,
                SourceNodeGuid = node?.Guid ?? default,
                IsWarning = isWarning,
                Fix = quickFix
            };

            if (!m_Errors.Contains(error))
            {
                m_Errors.Add(error);
            }
        }
    }
}
