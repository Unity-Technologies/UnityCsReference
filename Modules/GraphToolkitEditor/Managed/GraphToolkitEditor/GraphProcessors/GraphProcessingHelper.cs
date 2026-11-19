// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Graph processing options.
    /// </summary>
    [UnityRestricted]
    internal enum RequestGraphProcessingOptions
    {
        /// <summary>
        /// Process the graph.
        /// </summary>
        Default,

        /// <summary>
        /// Save the graph and process it.
        /// </summary>
        SaveGraph,
    }

    /// <summary>
    /// Helper class for graph processing.
    /// </summary>
    [UnityRestricted]
    internal static class GraphProcessingHelper
    {
        /// <summary>
        /// Processes the graph using the graph processors provided by <see cref="GraphModel.GetGraphProcessorContainer"/>.
        /// </summary>
        /// <param name="graphModel">The graph to process.</param>
        /// <param name="changeset">A description of what changed in the graph. If null, the method assumes everything changed.</param>
        /// <param name="options">Graph processing options.</param>
        /// <returns>The results of the graph processing.</returns>
        public static IReadOnlyList<BaseGraphProcessingResult> ProcessGraph(
            GraphModel graphModel,
            GraphModelStateComponent.Changeset changeset,
            RequestGraphProcessingOptions options)
        {
            if (graphModel == null)
                return null;

            GraphChangeDescription changes = null;
            if (changeset != null)
            {
                changes = new GraphChangeDescription();
                changes.Initialize(changeset.NewModels, changeset.ChangedModelsAndHints, changeset.DeletedModels);
            }

            if (options == RequestGraphProcessingOptions.SaveGraph && graphModel.GraphObject != null)
                graphModel.GraphObject.Save();

            return graphModel.GetGraphProcessorContainer().ProcessGraph(changes);
        }
    }
}
