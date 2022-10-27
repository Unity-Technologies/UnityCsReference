// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to load a graph asset.
    /// </summary>
    class LoadGraphCommand : ICommand
    {
        /// <summary>
        /// The type of loading.
        /// </summary>
        public enum LoadStrategies
        {
            /// <summary>
            /// Clears the history of loaded stack.
            /// </summary>
            Replace,
            /// <summary>
            /// Keeps the history and push the currently loaded graph to it.
            /// </summary>
            PushOnStack,
            /// <summary>
            /// Keeps the history and do not modify it.
            /// </summary>
            KeepHistory
        }

        /// <summary>
        /// The graph model to load.
        /// </summary>
        public readonly GraphModel GraphModel;
        /// <summary>
        /// The GameObject to which to bind the graph.
        /// </summary>
        public readonly GameObject BoundObject;
        /// <summary>
        /// The type of loading. Affects the stack of loaded assets.
        /// </summary>
        public readonly LoadStrategies LoadStrategy;
        /// <summary>
        /// The index at which the history should be truncated.
        /// </summary>
        public readonly int TruncateHistoryIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadGraphCommand"/> class.
        /// </summary>
        /// <param name="graph">The graph model to load.</param>
        /// <param name="boundObject">The game object to which the graph should be bound.</param>
        /// <param name="loadStrategy">The type of loading and how it should affect the stack of loaded assets.</param>
        /// <param name="truncateHistoryIndex">Truncate the stack of loaded assets at this index.</param>
        public LoadGraphCommand(GraphModel graph, GameObject boundObject = null,
            LoadStrategies loadStrategy = LoadStrategies.Replace, int truncateHistoryIndex = -1)
        {
            GraphModel = graph;
            BoundObject = boundObject;
            LoadStrategy = loadStrategy;
            TruncateHistoryIndex = truncateHistoryIndex;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(ToolStateComponent toolState, GraphProcessingStateComponent graphProcessingState, LoadGraphCommand command)
        {
            if (toolState.GraphModel != null)
            {
                // force queued graph processing to happen now when unloading a graph
                if (graphProcessingState.GraphProcessingPending)
                {
                    // Do not force graph processing if it's the same graph
                    if ((command.GraphModel != null && toolState.GraphModel != command.GraphModel))
                    {
                        GraphProcessingHelper.ProcessGraph(toolState.GraphModel, null, RequestGraphProcessingOptions.Default);
                    }
                }
            }

            using (var toolStateUpdater = toolState.UpdateScope)
            {
                if (command.TruncateHistoryIndex >= 0)
                    toolStateUpdater.TruncateHistory(command.TruncateHistoryIndex);

                switch (command.LoadStrategy)
                {
                    case LoadStrategies.Replace:
                        toolStateUpdater.ClearHistory();
                        break;
                    case LoadStrategies.PushOnStack:
                        toolStateUpdater.PushCurrentGraph();
                        break;
                    case LoadStrategies.KeepHistory:
                        break;
                }

                var graph = command.GraphModel;
                if (graph == null)
                {
                    Debug.LogError($"Could not load null graph.");
                    return;
                }

                if (!CheckGraphIntegrity(graph))
                    return;

                toolStateUpdater.LoadGraph(graph, command.BoundObject);

                var graphModel = toolState.GraphModel;

                if (graphModel != null)
                {
                    ((Stencil)graphModel.Stencil)?.PreProcessGraph(graphModel);

                    graphModel.OnLoadGraph();
                }
            }

            using (var graphProcessingStateUpdater = graphProcessingState.UpdateScope)
            {
                graphProcessingStateUpdater.Clear();
            }
        }

        static bool CheckGraphIntegrity(GraphModel graphModel)
        {
            if (graphModel == null)
                return true;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidWireCount = graphModel.WireModels.Count(n => n == null);
            var invalidStickyCount = graphModel.StickyNoteModels.Count(n => n == null);
            var invalidVariableCount = graphModel.VariableDeclarations.Count(v => v == null);
            var invalidBadgeCount = graphModel.BadgeModels.Count(b => b == null);
            var invalidPlacematCount = graphModel.PlacematModels.Count(p => p == null);
            var invalidPortalCount = graphModel.PortalDeclarations.Count(p => p == null);
            var invalidSectionCount = graphModel.SectionModels.Count(s => s == null);

            var countMessage = new StringBuilder();
            countMessage.Append(invalidNodeCount == 0 ? string.Empty : $"{invalidNodeCount} invalid node(s) found.\n");
            countMessage.Append(invalidWireCount == 0 ? string.Empty : $"{invalidWireCount} invalid wire(s) found.\n");
            countMessage.Append(invalidStickyCount == 0 ? string.Empty : $"{invalidStickyCount} invalid sticky note(s) found.\n");
            countMessage.Append(invalidVariableCount == 0 ? string.Empty : $"{invalidVariableCount} invalid variable declaration(s) found.\n");
            countMessage.Append(invalidBadgeCount == 0 ? string.Empty : $"{invalidBadgeCount} invalid badge(s) found.\n");
            countMessage.Append(invalidPlacematCount == 0 ? string.Empty : $"{invalidPlacematCount} invalid placemat(s) found.\n");
            countMessage.Append(invalidPortalCount == 0 ? string.Empty : $"{invalidPortalCount} invalid portal(s) found.\n");
            countMessage.Append(invalidSectionCount == 0 ? string.Empty : $"{invalidSectionCount} invalid section(s) found.\n");

            if (countMessage.ToString() != string.Empty)
                if (EditorUtility.DisplayDialog("Invalid graph",
                        $"Invalid elements found:\n{countMessage}\n" +
                        $"Click the Clean button to remove all the invalid elements from the graph.",
                        "Clean",
                        "Cancel"))
                    graphModel.Repair();
                else
                    return false;

            return true;
        }
    }

    class UnloadGraphCommand : ICommand
    {
        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(ToolStateComponent toolState, GraphProcessingStateComponent graphProcessingState, UnloadGraphCommand command)
        {
            if (toolState.GraphModel != null)
            {
                // force queued graph processing to happen now when unloading a graph
                if (graphProcessingState.GraphProcessingPending)
                {
                    GraphProcessingHelper.ProcessGraph(toolState.GraphModel, null, RequestGraphProcessingOptions.Default);
                }
            }

            using (var toolStateUpdater = toolState.UpdateScope)
            {
                toolStateUpdater.ClearHistory();
                toolStateUpdater.LoadGraph(null, null);
            }

            using (var graphProcessingStateUpdater = graphProcessingState.UpdateScope)
            {
                graphProcessingStateUpdater.Clear();
            }
        }
    }
}
