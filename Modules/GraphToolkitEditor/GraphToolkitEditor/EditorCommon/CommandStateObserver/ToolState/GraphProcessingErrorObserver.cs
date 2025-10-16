// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Generates error models from the graph processing results.
    /// </summary>
    [UnityRestricted]
    internal class GraphProcessingErrorObserver : StateObserver
    {
        GraphModelStateComponent m_GraphModelStateComponent;
        GraphProcessingStateComponent m_ResultsStateComponent;
        GraphProcessingErrorsStateComponent m_ErrorsStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphProcessingErrorObserver"/> class.
        /// </summary>
        public GraphProcessingErrorObserver(GraphModelStateComponent graphModelStateComponent,
                                            GraphProcessingStateComponent resultsStateComponent,
                                            GraphProcessingErrorsStateComponent errorsStateComponent)
            : base(new IStateComponent[] { graphModelStateComponent, resultsStateComponent },
                   new IStateComponent[] { errorsStateComponent })
        {
            m_GraphModelStateComponent = graphModelStateComponent;
            m_ResultsStateComponent = resultsStateComponent;
            m_ErrorsStateComponent = errorsStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using var graphObservation = this.ObserveState(m_GraphModelStateComponent);
            using var resultsObservation = this.ObserveState(m_ResultsStateComponent);

            var updateType = resultsObservation.UpdateType.Combine(graphObservation.UpdateType);
            if (updateType != UpdateType.None)
            {
                if (m_ResultsStateComponent.Results == null)
                    return;

                var graphModel = m_GraphModelStateComponent.GraphModel;
                if (graphModel == null)
                    return;

                // A dictionary of errors associated to the same model to convert them to MultipleGraphProcessingErrorsModel.
                var modelsWithErrors = new Dictionary<Hash128, List<GraphProcessingErrorModel>>();

                // A dictionary of errors that have the same source graph.
                var graphGuidsWithErrors = new Dictionary<Hash128, List<GraphProcessingErrorModel>>();

                for (var i = 0; i < m_ResultsStateComponent.Results.Count; i++)
                {
                    if (m_ResultsStateComponent.Results[i] is not ErrorsAndWarningsResult result)
                        continue;

                    for (var j = 0; j < result.Errors.Count; j++)
                    {
                        var error = graphModel.CreateProcessingErrorModel(result.Errors[j]);
                        if (error is null)
                            continue;

                        if (error.ParentModelGuid != default)
                        {
                            if (modelsWithErrors.TryGetValue(error.ParentModelGuid, out var modelErrors))
                                modelErrors.Add(error);
                            else
                                modelsWithErrors.Add(error.ParentModelGuid, new List<GraphProcessingErrorModel> { error });
                        }

                        var sourceGraphGuid = error.SourceGraphReference == default ? graphModel.Guid : (GraphReference.ResolveGraphModel(error.SourceGraphReference)?.Guid ?? default);
                        if (graphGuidsWithErrors.TryGetValue(sourceGraphGuid, out var errorList))
                            errorList.Add(error);
                        else
                            graphGuidsWithErrors.Add(sourceGraphGuid, new List<GraphProcessingErrorModel> { error });
                    }
                }

                // Check if subgraph nodes in the current graph should display errors from lower levels sub graphs.
                if (graphGuidsWithErrors.Count > 0)
                {
                    for (var i = 0; i < graphModel.NodeModels.Count; i++)
                    {
                        // Traverse each subgraph node in the current graph using DFS to keep track of the path from the current graph to the model with error.
                        if (graphModel.NodeModels[i] is not SubgraphNodeModel currentSubgraphNode)
                            continue;

                        var context = new List<GraphElementModel>();
                        var uniqueGraphs = new HashSet<GraphModel>();
                        VisitSubgraphNodes(currentSubgraphNode);

                        continue;

                        void VisitSubgraphNodes(SubgraphNodeModel subgraphNode)
                        {
                            var subgraph = subgraphNode.GetSubgraphModel();
                            if (subgraph is null || !uniqueGraphs.Add(subgraph))
                                return;

                            context.Add(subgraphNode);
                            AddSubgraphError(graphGuidsWithErrors, modelsWithErrors, subgraph.Guid, currentSubgraphNode.Guid, context);

                            for (var j = 0; j < subgraph.NodeModels.Count; j++)
                            {
                                if (subgraph.NodeModels[j] is not SubgraphNodeModel otherSubgraphNode)
                                    continue;

                                var otherSubgraph = otherSubgraphNode.GetSubgraphModel();
                                if (otherSubgraph is null)
                                    continue;

                                VisitSubgraphNodes(otherSubgraphNode);
                            }
                            context.Remove(subgraphNode);
                        }
                    }
                }

                var errorsModels = new List<MultipleGraphProcessingErrorsModel>();
                foreach (var (model, errors) in modelsWithErrors)
                {
                    errorsModels.Add(new MultipleGraphProcessingErrorsModel(model, errors));
                }

                using (var updater = m_ErrorsStateComponent.UpdateScope)
                {
                    updater.SetResults(errorsModels);
                }
            }
        }

        static void AddSubgraphError(IReadOnlyDictionary<Hash128, List<GraphProcessingErrorModel>> graphGuidsWithErrors, IDictionary<Hash128, List<GraphProcessingErrorModel>> modelsWithErrors, Hash128 graphToCheck, Hash128 currentSubgraphNodeGuid, List<GraphElementModel> context)
        {
            // Add errors from a subgraph to the current subgraph node in the main graph.
            if (graphGuidsWithErrors.TryGetValue(graphToCheck, out var subgraphErrors))
            {
                foreach (var subgraphError in subgraphErrors)
                {
                    // We don't show warnings from nested sub graphs. Only errors.
                    if (subgraphError.ErrorType is LogType.Warning)
                        continue;

                    // If the error has a context, only add the error if the current subgraph node is part of the context.
                    if (subgraphError.Context is { Count: > 0 })
                    {
                        var isPartOfContext = false;
                        for (var i = 0; i < subgraphError.Context.Count; i++)
                        {
                            var contextModel = subgraphError.Context[i];
                            if (contextModel.Guid == currentSubgraphNodeGuid)
                            {
                                isPartOfContext = true;
                                break;
                            }
                        }

                        if (!isPartOfContext)
                            continue;
                    }

                    var subgraphNodeError = new GraphProcessingErrorModel(new GraphProcessingError(
                        subgraphError.ErrorMessage,
                        subgraphError.ParentModelGuid,
                        LogType.Error, // We only show errors from lower levels sub graphs.
                        subgraphError.SourceGraphReference,
                        new List<GraphElementModel>(context) // Make a copy of the list to make sure it is not modified
                    ));

                    if (modelsWithErrors.TryGetValue(currentSubgraphNodeGuid, out var modelErrors))
                        modelErrors.Add(subgraphNodeError);
                    else
                        modelsWithErrors.Add(currentSubgraphNodeGuid, new List<GraphProcessingErrorModel> { subgraphNodeError });
                }
            }
        }
    }
}
