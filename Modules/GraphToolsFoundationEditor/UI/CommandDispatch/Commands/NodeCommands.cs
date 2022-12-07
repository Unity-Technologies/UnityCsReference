// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to collapse and expand nodes.
    /// </summary>
    class CollapseNodeCommand : ModelCommand<AbstractNodeModel, bool>
    {
        const string k_CollapseUndoStringSingular = "Collapse Node";
        const string k_CollapseUndoStringPlural = "Collapse Nodes";
        const string k_ExpandUndoStringSingular = "Expand Node";
        const string k_ExpandUndoStringPlural = "Expand Nodes";

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        public CollapseNodeCommand()
            : base("Collapse Or Expand Node") {}

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        /// <param name="value">True if the nodes should be collapsed, false otherwise.</param>
        /// <param name="nodes">The nodes to expand or collapse.</param>
        public CollapseNodeCommand(bool value, IReadOnlyList<AbstractNodeModel> nodes)
            : base(value ? k_CollapseUndoStringSingular : k_ExpandUndoStringSingular,
                   value ? k_CollapseUndoStringPlural : k_ExpandUndoStringPlural, value, nodes)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        /// <param name="value">True if the nodes should be collapsed, false otherwise.</param>
        /// <param name="nodes">The nodes to expand or collapse.</param>
        public CollapseNodeCommand(bool value, params AbstractNodeModel[] nodes)
            : this(value, (IReadOnlyList<AbstractNodeModel>)nodes)
        {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, CollapseNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var model in command.Models.OfType<ICollapsible>())
                {
                    model.Collapsed = command.Value;
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to change the name of a graph element.
    /// </summary>
    class RenameElementCommand : UndoableCommand
    {
        /// <summary>
        /// The graph element to rename.
        /// </summary>
        public IRenamable Model;
        /// <summary>
        /// The new name.
        /// </summary>
        public string ElementName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameElementCommand"/> class.
        /// </summary>
        public RenameElementCommand()
        {
            UndoString = "Rename Element";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameElementCommand"/> class.
        /// </summary>
        /// <param name="model">The graph element to rename.</param>
        /// <param name="name">The new name.</param>
        public RenameElementCommand(IRenamable model, string name) : this()
        {
            Model = model;
            ElementName = name;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, RenameElementCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.Model.Rename(command.ElementName);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to update the value of a constant.
    /// </summary>
    class UpdateConstantValueCommand : UndoableCommand
    {
        /// <summary>
        /// The constant to update.
        /// </summary>
        public Constant Constant;
        /// <summary>
        /// The new value.
        /// </summary>
        public object Value;
        /// <summary>
        /// The node model that owns the constant, if any.
        /// </summary>
        public GraphElementModel OwnerModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConstantValueCommand"/> class.
        /// </summary>
        public UpdateConstantValueCommand()
        {
            UndoString = "Update Value";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConstantValueCommand"/> class.
        /// </summary>
        /// <param name="constant">The constant to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="owner">The model that owns the constant, if any.</param>
        public UpdateConstantValueCommand(Constant constant, object value, GraphElementModel owner) : this()
        {
            Constant = constant;
            Value = value;
            OwnerModel = owner;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateConstantValueCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.Constant.ObjectValue = command.Value;
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to update the values of some constants.
    /// </summary>
    class UpdateConstantsValueCommand : UndoableCommand
    {
        /// <summary>
        /// The constants to update.
        /// </summary>
        public IReadOnlyList<Constant> Constants;
        /// <summary>
        /// The new value.
        /// </summary>
        public object Value;
        /// <summary>
        /// The node models that owns the constants, if any.
        /// </summary>
        public IReadOnlyList<GraphElementModel> OwnerModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConstantsValueCommand"/> class.
        /// </summary>
        public UpdateConstantsValueCommand()
        {
            UndoString = "Update Values";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConstantsValueCommand"/> class.
        /// </summary>
        /// <param name="constants">The constants to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="owners">The models that owns the constants, if any.</param>
        public UpdateConstantsValueCommand(IEnumerable<Constant> constants, object value, IEnumerable<GraphElementModel> owners) : this()
        {
            Constants = constants?.ToList() ?? new List<Constant>();
            Value = value;
            OwnerModels = owners?.ToList() ?? new List<GraphElementModel>();
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateConstantsValueCommand command)
        {
            if (command.Constants.Any())
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var graphUpdater = graphModelState.UpdateScope)
                using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
                {
                    foreach (var constant in command.Constants)
                    {
                        constant.ObjectValue = command.Value;
                    }
                    graphUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// Command to remove all wires on nodes.
    /// </summary>
    class DisconnectNodeCommand : ModelCommand<AbstractNodeModel>
    {
        const string k_UndoStringSingular = "Disconnect Node";
        const string k_UndoStringPlural = "Disconnect Nodes";

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectNodeCommand"/> class.
        /// </summary>
        public DisconnectNodeCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectNodeCommand"/> class.
        /// </summary>
        /// <param name="nodeModels">The nodes to disconnect.</param>
        public DisconnectNodeCommand(IReadOnlyList<AbstractNodeModel> nodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodeModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectNodeCommand"/> class.
        /// </summary>
        /// <param name="nodeModels">The nodes to disconnect.</param>
        public DisconnectNodeCommand(params AbstractNodeModel[] nodeModels)
            : this((IReadOnlyList<AbstractNodeModel>)nodeModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, DisconnectNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var graphModel = graphModelState.GraphModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    var connectedWires = nodeModel.GetConnectedWires().ToList();
                    graphModel.DeleteWires(connectedWires);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to bypass nodes using wires. Optionally deletes the nodes.
    /// </summary>
    class BypassNodesCommand : ModelCommand<AbstractNodeModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        /// <summary>
        /// The nodes to bypass.
        /// </summary>
        public readonly IReadOnlyList<InputOutputPortsNodeModel> NodesToBypass;

        /// <summary>
        /// Initializes a new instance of the <see cref="BypassNodesCommand"/> class.
        /// </summary>
        public BypassNodesCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="BypassNodesCommand"/> class.
        /// </summary>
        /// <param name="nodesToBypass">The nodes to bypass.</param>
        /// <param name="elementsToRemove">The nodes to delete.</param>
        public BypassNodesCommand(IReadOnlyList<InputOutputPortsNodeModel> nodesToBypass, IReadOnlyList<AbstractNodeModel> elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
            NodesToBypass = nodesToBypass;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state of the graph view.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BypassNodesCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
                undoStateUpdater.SaveState(selectionState);
            }

            var graphModel = graphModelState.GraphModel;

            using (var selectionUpdater = selectionState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                foreach (var model in command.NodesToBypass)
                {
                    var inputWireModel = new List<WireModel>();
                    foreach (var portModel in model.InputsByDisplayOrder)
                    {
                        inputWireModel.AddRange(graphModel.GetWiresForPort(portModel));
                    }

                    if (!inputWireModel.Any())
                        continue;

                    var outputWireModels = new List<WireModel>();
                    foreach (var portModel in model.OutputsByDisplayOrder)
                    {
                        outputWireModels.AddRange(graphModel.GetWiresForPort(portModel));
                    }

                    if (!outputWireModels.Any())
                        continue;

                    graphModel.DeleteWires(inputWireModel);
                    graphModel.DeleteWires(outputWireModels);

                    graphModel.CreateWire(outputWireModels[0].ToPort, inputWireModel[0].FromPort);
                }

                // [GTF-663] We delete nodes with deleteConnection = true because it may happens that one of the newly
                // added wire is connected to a node that will be deleted.
                graphModel.DeleteNodes(command.Models, deleteConnections: true);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);

                var selectedModels = changeScope.ChangeDescription.DeletedModels.Where(selectionState.IsSelected).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.SelectElements(selectedModels, false);
                }
            }
        }
    }

    /// <summary>
    /// Command to change the state of nodes.
    /// </summary>
    class ChangeNodeStateCommand : ModelCommand<AbstractNodeModel, ModelState>
    {
        const string k_UndoStringSingular = "Change Node State";
        const string k_UndoStringPlural = "Change Nodes State";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNodeStateCommand"/> class.
        /// </summary>
        public ChangeNodeStateCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNodeStateCommand"/> class.
        /// </summary>
        /// <param name="state">The new node state.</param>
        /// <param name="nodeModels">The nodes to modify.</param>
        public ChangeNodeStateCommand(ModelState state, IReadOnlyList<AbstractNodeModel> nodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, state, nodeModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNodeStateCommand"/> class.
        /// </summary>
        /// <param name="state">The new node state.</param>
        /// <param name="nodeModels">The nodes to modify.</param>
        public ChangeNodeStateCommand(ModelState state, params AbstractNodeModel[] nodeModels)
            : this(state, (IReadOnlyList<AbstractNodeModel>)nodeModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeNodeStateCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.State = command.Value;
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
