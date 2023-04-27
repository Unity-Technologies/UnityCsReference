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
        public static void DefaultCommandHandler(ToolStateComponent toolState, LoadGraphCommand command)
        {
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

                toolStateUpdater.LoadGraph(graph, command.BoundObject);

                var graphModel = toolState.GraphModel;

                if (graphModel != null)
                {
                    ((Stencil)graphModel.Stencil)?.PreProcessGraph(graphModel);

                    graphModel.OnLoadGraph();
                }
            }
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
        public static void DefaultCommandHandler(ToolStateComponent toolState, UnloadGraphCommand command)
        {
            using (var toolStateUpdater = toolState.UpdateScope)
            {
                toolStateUpdater.ClearHistory();
                toolStateUpdater.LoadGraph(null, null);
            }
        }
    }
}
