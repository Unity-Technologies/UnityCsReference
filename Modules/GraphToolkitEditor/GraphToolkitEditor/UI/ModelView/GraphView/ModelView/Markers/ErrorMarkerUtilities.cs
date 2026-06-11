// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor;

static class ErrorMarkerUtilities
{
    const string k_ErrorString = "Error";
    const string k_WarningString = "Warning";
    const string k_InfoString = "Info";

    public static void LoadGraphAndFrameElement(GraphProcessingErrorModel error, GraphView graphView)
    {
        var targetGraph = error.SourceGraphReference;
        if (targetGraph == default || graphView?.GraphTool is null)
            return;

        var graphModel = graphView.GraphModel;
        if (error.Context is { Count: > 0 })
        {
            // Load each graph leading to the target graph in the breadcrumbs.
            var alreadyVisited = HashSetPool<Hash128>.Get();
            alreadyVisited.Add( graphModel.Guid );
            try
            {
                for (var i = 0; i < error.Context.Count; i++)
                {
                    var contextModel = error.Context[i];
                    if (contextModel.GraphModel.Guid != targetGraph.GraphModelGuid &&
                        alreadyVisited.Add(contextModel.GraphModel.Guid))
                    {
                        var title = contextModel is SubgraphNodeModel subgraphNode ? subgraphNode.Title : string.Empty;
                        graphView.GraphTool.Dispatch(new LoadGraphCommand(contextModel.GraphModel,
                            LoadGraphCommand.LoadStrategies.PushOnStack, title: title));
                    }
                }
            }
            finally
            {
                HashSetPool<Hash128>.Release(alreadyVisited);
            }
        }

        GraphViewEditorWindow.FrameGraphElement(error.ParentModelGuid, graphView,
            graphModel.ResolveGraphModelFromReference(targetGraph));
    }

    public static void PopulateDetailedGraphErrors(
        MarkerModel markerModel,
        GraphView graphView,
        List<ErrorMarkerModel> currentGraphErrors,
        Dictionary<GraphReference, List<ErrorMarkerModel>> subgraphErrorsDict)
    {
        currentGraphErrors.Clear();
        subgraphErrorsDict.Clear();


        switch (markerModel)
        {
            case MultipleGraphProcessingErrorsModel multipleErrorsModel:
            {
                var currentGraph = graphView.GraphModel;

                foreach (var errorModel in multipleErrorsModel.Errors)
                {
                    // Determine if this error is from the current graph or a subgraph
                    var isCurrentGraph = errorModel.SourceGraphReference.GraphModelGuid == currentGraph.Guid;

                    if (isCurrentGraph)
                    {
                        currentGraphErrors.Add(errorModel);
                    }
                    else
                    {
                        // Group subgraph errors by their graph reference
                        if (!subgraphErrorsDict.ContainsKey(errorModel.SourceGraphReference))
                        {
                            subgraphErrorsDict[errorModel.SourceGraphReference] = new List<ErrorMarkerModel>();
                        }

                        subgraphErrorsDict[errorModel.SourceGraphReference].Add(errorModel);
                    }
                }


                break;
            }
            case ErrorMarkerModel errorMarkerModel:
                currentGraphErrors.Add(errorMarkerModel);
                break;
        }
    }

    public static List<ErrorMarkerPopupModel.SubgraphErrorGroup> CreateSubgraphErrorGroups(Dictionary<GraphReference, List<ErrorMarkerModel>> subgraphErrorsDict)
    {
        var subgraphErrorGroups = new List<ErrorMarkerPopupModel.SubgraphErrorGroup>();
        foreach (var kvp in subgraphErrorsDict)
        {
            var graphPath = GetGraphPath(kvp.Key);
            kvp.Value.Sort((a, b) => GetSeverityPriority(a.ErrorType).CompareTo(GetSeverityPriority(b.ErrorType)));
            subgraphErrorGroups.Add(new ErrorMarkerPopupModel.SubgraphErrorGroup(graphPath, kvp.Value));
        }

        return subgraphErrorGroups;
    }

    public static string GetGraphPath(GraphReference graphReference)
    {
        // Try to resolve the actual graph model to get its name
        var graphModel = GraphReference.ResolveGraphModel(graphReference);
        if (graphModel != null && !string.IsNullOrEmpty(graphModel.Name))
        {
            return graphModel.Name;
        }

        // Fallback to file name
        var filePath = graphReference.FilePath;
        if (!string.IsNullOrEmpty(filePath))
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }

        // Last resort: use GUID
        return $"Subgraph ({graphReference.GraphModelGuid})";
    }
    public static int GetSeverityPriority(LogType logType)
    {
        return logType switch
        {
            LogType.Error => 0,
            LogType.Assert => 0,
            LogType.Exception => 0,
            LogType.Warning => 1,
            LogType.Log => 2,
            _ => 3
        };
    }

    public static string GetSubgraphMessageString(string graphPath, int count, string logString) =>
        $"{graphPath} has {count} {(count == 1 ? logString : logString + "s")}";

    public static string GetLogString(LogType errorType, bool localized = true)
    {
        return errorType switch
        {
            LogType.Error or LogType.Assert or LogType.Exception => localized ? L10n.Tr(k_ErrorString) : k_ErrorString,
            LogType.Warning => localized ? L10n.Tr(k_WarningString) : k_WarningString,
            LogType.Log => localized ? L10n.Tr(k_InfoString) : k_InfoString,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
