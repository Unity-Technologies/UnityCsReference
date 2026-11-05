// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to collapse and expand ports.
    /// </summary>
    [UnityRestricted]
    internal class ExpandPortCommand : ModelCommand<PortModel, bool>
    {
        const string k_CollapseUndoStringSingular = "Collapse Port";
        const string k_CollapseUndoStringPlural = "Collapse Ports";
        const string k_ExpandUndoStringSingular = "Expand Port";
        const string k_ExpandUndoStringPlural = "Expand Ports";

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        public ExpandPortCommand()
            : base(k_CollapseUndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandPortCommand"/> class.
        /// </summary>
        /// <param name="expand">True if the ports should be expanded, false otherwise.</param>
        /// <param name="ports">The ports to expand or collapse.</param>
        /// <param name="recursive">Whether the expandable descendants of the ports are also expanded or collapsed.</param>
        public ExpandPortCommand(bool expand, IReadOnlyList<PortModel> ports, bool recursive = false)
            : base(expand ? k_ExpandUndoStringSingular : k_CollapseUndoStringSingular, expand ? k_ExpandUndoStringPlural : k_CollapseUndoStringPlural, expand, ports)
        {
            Recursive = recursive;
            if (recursive)
                UndoString = expand ? k_ExpandUndoStringPlural : k_CollapseUndoStringPlural;
        }

        /// <summary>
        /// Whether the expandable descendants of the ports are also expanded or collapsed.
        /// </summary>
        public bool Recursive;

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ExpandPortCommand command)
        {
            if (command.Models == null || command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var model in command.Models)
                {
                    if (model.NodeModel is NodeModel nodeModel)
                    {
                        if (command.Recursive)
                            nodeModel.SetPortExpandedRecursively(model, command.Value);
                        else
                            nodeModel.SetPortExpanded(model, command.Value);
                    }
                    // Only a NodeModel defines sub ports, so there is nothing to recurse into here.
                    else
                        model.SetPortExpanded(command.Value);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to temporarily expand and collapse ports, for example while a wire is dragged over them.
    /// </summary>
    /// <remarks>The temporary state is not serialized and the command is not undoable.</remarks>
    [UnityRestricted]
    internal class TemporarilyExpandPortCommand : ICommand
    {
        /// <summary>
        /// The ports to temporarily expand or collapse.
        /// </summary>
        public IReadOnlyList<PortModel> Ports;

        /// <summary>
        /// Whether to temporarily expand or collapse the ports.
        /// </summary>
        public bool Expand;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporarilyExpandPortCommand"/> class.
        /// </summary>
        public TemporarilyExpandPortCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporarilyExpandPortCommand"/> class.
        /// </summary>
        /// <param name="expand">True if the ports should be temporarily expanded, false otherwise.</param>
        /// <param name="ports">The ports to temporarily expand or collapse.</param>
        public TemporarilyExpandPortCommand(bool expand, IReadOnlyList<PortModel> ports)
        {
            Expand = expand;
            Ports = ports;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, TemporarilyExpandPortCommand command)
        {
            if (command.Ports == null || command.Ports.Count == 0)
                return;

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var port in command.Ports)
                {
                    if (port.NodeModel is NodeModel nodeModel)
                        nodeModel.SetPortTemporarilyExpanded(port, command.Expand);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
