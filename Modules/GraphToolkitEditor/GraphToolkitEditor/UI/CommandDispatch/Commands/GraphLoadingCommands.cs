// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to load a graph model.
    /// </summary>
    [UnityRestricted]
    internal class LoadGraphCommand : ICommand
    {
        /// <summary>
        /// The type of loading.
        /// </summary>
        [UnityRestricted]
        internal enum LoadStrategies
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
        /// The title associated with the graph to load.
        /// </summary>
        public readonly string Title;

        static bool HasMultipleWindowTypeInstances(GraphObject graphObject)
        {
            if (graphObject != null)
            {
                var windowType = GraphObjectFactory.GetWindowTypeForGraphObject(graphObject.GetType());
                if (windowType == null)
                {
                    if (EditorWindow.focusedWindow is GraphViewEditorWindow)
                    {
                        windowType = EditorWindow.focusedWindow.GetType();
                    }
                    else
                    {
                        Debug.LogError($"Could not get the window type associated with {graphObject.GetType()}. Consider linking {graphObject.GetType()} to the right window type using the attribute {typeof(GraphEditorWindowDefinitionAttribute)}.");
                        return false;
                    }
                }

                return Resources.FindObjectsOfTypeAll(windowType).Length > 1;
            }

            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadGraphCommand"/> class.
        /// </summary>
        /// <param name="graph">The graph model to load.</param>
        /// <param name="boundObject">The game object to which the graph should be bound.</param>
        /// <param name="loadStrategy">The type of loading and how it should affect the stack of loaded assets.</param>
        /// <param name="truncateHistoryIndex">Truncate the stack of loaded assets at this index.</param>
        /// <param name="title">The title associated with the graph to load, displayed on the <see cref="Blackboard"/>.</param>
        public LoadGraphCommand(GraphModel graph, GameObject boundObject = null,
            LoadStrategies loadStrategy = LoadStrategies.Replace, int truncateHistoryIndex = -1, string title = "")
        {
            GraphModel = graph;
            BoundObject = boundObject;
            LoadStrategy = loadStrategy;
            TruncateHistoryIndex = truncateHistoryIndex;
            Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadGraphCommand"/> class.
        /// </summary>
        /// <param name="graph">The graph model to load.</param>
        /// <param name="loadStrategy">The type of loading and how it should affect the stack of loaded assets.</param>
        /// <param name="truncateHistoryIndex">Truncate the stack of loaded assets at this index.</param>
        /// <param name="title">The title associated with the graph to load, displayed on the <see cref="Blackboard"/>.</param>
        public LoadGraphCommand(GraphModel graph, LoadStrategies loadStrategy, int truncateHistoryIndex = -1, string title = "") : this(graph, null, loadStrategy, truncateHistoryIndex, title)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(ToolStateComponent toolState, LoadGraphCommand command)
        {
            using (var toolStateUpdater = toolState.UpdateScope)
            {
                var graph = command.GraphModel;
                if (graph == null)
                {
                    Debug.LogError($"Could not load null graph.");
                    return;
                }

                // Nothing prevents graph tool from dispatching LoadGraphCommand wherever they want in their code.
                // If the graph tool doesn't allow multiple windows, we need to check if there are more than 1 window opened.
                if (!toolState.GraphTool.SupportsMultipleWindows && HasMultipleWindowTypeInstances(command.GraphModel.GraphObject))
                    throw new NotSupportedException($"Could not load graph {graph.Name}: Opening multiple windows is not supported.");

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

                toolStateUpdater.LoadGraph(graph, command.BoundObject, command.Title);

                var graphModel = toolState.GraphModel;
                graphModel?.OnLoadGraph();
            }
        }
    }

    /// <summary>
    /// Command to unload a graph model.
    /// </summary>
    /// <remarks>
    /// 'UnloadGraphCommand' is a command used to unload a graph model, clear the graph history stack (which includes opened subgraphs), and load a null graph.
    /// This operation resets the graph view to a blank page, presenting options to create a new graph or open an existing one.
    /// </remarks>
    [UnityRestricted]
    internal class UnloadGraphCommand : ICommand
    {
        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
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
