// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;

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
        /// <param name="expand">True if the ports should be collapsed, false otherwise.</param>
        /// <param name="ports">The ports to expand or collapse.</param>
        public ExpandPortCommand(bool expand, IReadOnlyList<PortModel> ports)
            : base(expand ? k_ExpandUndoStringSingular : k_CollapseUndoStringSingular, expand ? k_ExpandUndoStringPlural : k_CollapseUndoStringPlural, expand, ports) { }

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
                        nodeModel.SetPortExpanded(model, command.Value);
                    else
                        model.SetPortExpanded(command.Value);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
