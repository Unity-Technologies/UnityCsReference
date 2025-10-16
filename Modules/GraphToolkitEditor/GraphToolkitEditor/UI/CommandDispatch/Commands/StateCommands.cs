// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to set a node as the entry point for the graph.
    /// </summary>
    [UnityRestricted]
    internal class SetEntryPointCommand : UndoableCommand
    {
        /// <summary>
        /// The graph to set the entry point on.
        /// </summary>
        public readonly GraphModel GraphModel;

        /// <summary>
        /// The node to set as the entry point.
        /// </summary>
        public readonly AbstractNodeModel Node;

        /// <summary>
        /// Whether to set or clear the entry point.
        /// </summary>
        public readonly bool Set;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntryPointCommand"/> class.
        /// </summary>
        /// <param name="graphModel">The graph to set the entry point on.</param>
        /// <param name="node">The node to set as the entry point.</param>
        /// <param name="set">Whether to set or clear the entry point.</param>
        public SetEntryPointCommand(GraphModel graphModel, AbstractNodeModel node, bool set)
        {
            GraphModel = graphModel;
            Node = node;
            Set = set;
            UndoString = Set ? "Set Default Enter State" : "Clear Default Enter State";
        }

        /// <summary>
        /// The default command handler for <see cref="SetEntryPointCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command to execute.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetEntryPointCommand command)
        {
            // If we are clearing the entry point, we don't need to do anything if the node is not the current entry point.
            if (!command.Set && command.GraphModel.EntryPoint != command.Node)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var stateUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.GraphModel.EntryPoint = command.Set ? command.Node : null;
                stateUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
