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
    /// Command to create a node from a <see cref="GraphNodeModelLibraryItem"/>.
    /// </summary>
    class CreateNodeCommand : UndoableCommand
    {
        /// <summary>
        /// Data used by <see cref="CreateNodeCommand"/> to create one node.
        /// </summary>
        public struct NodeData
        {
            /// <summary>
            /// The position where to create the node.
            /// </summary>
            public Vector2 Position;

            /// <summary>
            /// The wire model on which to insert the newly created node.
            /// </summary>
            public WireModel WireToInsertOn;

            /// <summary>
            /// The port to which to connect the new node.
            /// </summary>
            public PortModel PortModel;

            /// <summary>
            /// The variable for which to create nodes.
            /// </summary>
            public VariableDeclarationModel VariableDeclaration;

            /// <summary>
            /// The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.
            /// </summary>
            public GraphNodeModelLibraryItem LibraryItem;

            /// <summary>
            /// True if the new node should be aligned to the connected port.
            /// </summary>
            public bool AutoAlign;

            /// <summary>
            /// The guid to assign to the newly created item.
            /// </summary>
            public Hash128 Guid;

            /// <summary>
            /// The wire models on which to connect the newly created node.
            /// </summary>
            public IEnumerable<(WireModel model, WireSide side)> WiresToConnect;
        }

        /// <summary>
        /// Data for all the nodes the command should create.
        /// </summary>
        public List<NodeData> CreationData;

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/>.
        /// </summary>
        public CreateNodeCommand()
        {
            UndoString = "Create Node";
            CreationData = new List<NodeData>();
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a node on the graph.
        /// </summary>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on the graph.</returns>
        public static CreateNodeCommand OnGraph(GraphNodeModelLibraryItem item,
            Vector2 position = default,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnGraph(item, position, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a variable on the graph.
        /// </summary>
        /// <param name="model">The declaration for the variable to create on the graph.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a variable on the graph.</returns>
        public static CreateNodeCommand OnGraph(VariableDeclarationModel model,
            Vector2 position = default,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnGraph(model, position, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to insert a node on an existing wire.
        /// </summary>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="wireModel">The wire to insert the new node one.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on a wire.</returns>
        public static CreateNodeCommand OnWire(GraphNodeModelLibraryItem item,
            WireModel wireModel,
            Vector2 position = default,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnWire(item, wireModel, position, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a node and connect it to an existing port.
        /// </summary>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="portModel">The port to connect the new node to.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the created node will be automatically aligned after being created.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on a port.</returns>
        public static CreateNodeCommand OnPort(GraphNodeModelLibraryItem item,
            PortModel portModel,
            Vector2 position = default,
            bool autoAlign = false,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnPort(item, portModel, position, autoAlign, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a variable and connect it to an existing port.
        /// </summary>
        /// <param name="model">The declaration for the variable to create on the graph.</param>
        /// <param name="portModel">The port to connect the new node to.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the created node will be automatically aligned after being created.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on a wire.</returns>
        public static CreateNodeCommand OnPort(VariableDeclarationModel model,
            PortModel portModel,
            Vector2 position = default,
            bool autoAlign = false,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnPort(model, portModel, position, autoAlign, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a node and connect it to existing wires.
        /// </summary>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="wires">The wires to connect to the node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on a port.</returns>
        public static CreateNodeCommand OnWireSide(GraphNodeModelLibraryItem item,
            IEnumerable<(WireModel, WireSide)> wires,
            Vector2 position,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnWires(item, wires, position, guid);
        }

        enum ConnectionsToMake
        {
            None,
            ExistingPort,
            InsertOnWire,
            ExistingWires
        };

        /// <summary>
        /// Default command handler for <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="preferences">The tool preferences.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState,
            AutoPlacementStateComponent autoPlacementState, Preferences preferences, CreateNodeCommand command)
        {
            if (command.CreationData.Count <= 0)
            {
                Debug.LogError("Creation command dispatched with 0 item to create");
                return;
            }

            if (command.CreationData.All(nodeData => nodeData.VariableDeclaration == null) && command.CreationData.All(nodeData => nodeData.LibraryItem == null))
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var createdElements = new List<GraphElementModel>();
            var graphModel = graphModelState.GraphModel;

            using (var autoPlacementUpdater = autoPlacementState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                foreach (var creationData in command.CreationData)
                {
                    if ((creationData.VariableDeclaration == null) == (creationData.LibraryItem == null))
                    {
                        Debug.LogWarning("Creation command dispatched with invalid item to create: either provide VariableDeclaration or LibraryItem. Ignoring this item.");
                        continue;
                    }

                    var connectionsToMake = ConnectionsToMake.None;

                    if (creationData.PortModel != null)
                        connectionsToMake = ConnectionsToMake.ExistingPort;
                    if (creationData.WiresToConnect != null || creationData.WireToInsertOn != null)
                    {
                        if (connectionsToMake != ConnectionsToMake.None)
                        {
                            Debug.LogError(
                                "Creation command dispatched with invalid item to create: Trying to connect new item in different ways. Ignoring this item.");
                            continue;
                        }

                        connectionsToMake = creationData.WiresToConnect != null
                            ? ConnectionsToMake.ExistingWires
                            : ConnectionsToMake.InsertOnWire;
                    }

                    var guid = creationData.Guid.isValid ? creationData.Guid : Hash128Extensions.Generate();

                    // Create new element
                    GraphElementModel createdElement;
                    if (creationData.VariableDeclaration != null)
                    {
                        if (graphModel.Stencil is Stencil stencil && stencil.CanCreateVariableNode(creationData.VariableDeclaration, graphModel))
                        {
                            createdElement = graphModel.CreateVariableNode(creationData.VariableDeclaration, creationData.Position, guid);
                        }
                        else
                        {
                            Debug.LogWarning($"Could not create a new variable node for variable {creationData.VariableDeclaration.Title}.");
                            continue;
                        }
                    }
                    else
                    {
                        createdElement = creationData.LibraryItem.CreateElement.Invoke(
                            new GraphNodeCreationData(graphModel, creationData.Position, guid: guid));
                    }

                    if (createdElement != null)
                    {
                        createdElements.Add(createdElement);
                        graphUpdater.MarkForRename(createdElement);
                    }

                    var wiresToDelete = new List<WireModel>();
                    var portNodeModel = createdElement as PortNodeModel;
                    switch (connectionsToMake)
                    {
                        case ConnectionsToMake.None:
                            break;
                        case ConnectionsToMake.ExistingPort:
                            var existingPortToConnect = creationData.PortModel;
                            var newPortToConnect = portNodeModel?.GetPortFitToConnectTo(existingPortToConnect);
                            if (newPortToConnect != null)
                            {
                                // Old wires to delete
                                wiresToDelete = WireCommandHelper_Internal.GetDropWireModelsToDelete(creationData.PortModel).ToList();

                                WireModel newWire;
                                if (existingPortToConnect.Direction == PortDirection.Output)
                                {
                                    if (existingPortToConnect.NodeModel is ConstantNodeModel && preferences.GetBool(BoolPref.AutoItemizeConstants)
                                        || existingPortToConnect.NodeModel is VariableNodeModel && preferences.GetBool(BoolPref.AutoItemizeVariables))
                                    {
                                        var newNode = graphModel.CreateItemizedNode(WireCommandConfig_Internal.nodeOffset,
                                            ref existingPortToConnect);
                                        createdElements.Add(newNode);
                                    }

                                    newWire = graphModel.CreateWire(newPortToConnect, existingPortToConnect);
                                }
                                else
                                {
                                    newWire = graphModel.CreateWire(existingPortToConnect, newPortToConnect);
                                }

                                createdElements.Add(newWire);

                                if (newWire != null && creationData.AutoAlign ||
                                    preferences.GetBool(BoolPref.AutoAlignDraggedWires))
                                {
                                    autoPlacementUpdater.MarkModelToAutoAlign(newWire);
                                }
                            }
                            break;
                        case ConnectionsToMake.InsertOnWire:
                            if (portNodeModel is InputOutputPortsNodeModel newModelToConnect)
                            {
                                var wireInput = creationData.WireToInsertOn.ToPort;
                                var wireOutput = creationData.WireToInsertOn.FromPort;

                                // Old wire to delete
                                wiresToDelete.Add(creationData.WireToInsertOn);

                                // Connect input port
                                var inputPortModel = newModelToConnect.GetPortFitToConnectTo(wireOutput);
                                var inputWire = inputPortModel == null ? null : graphModel.CreateWire(inputPortModel, wireOutput);

                                // Connect output port
                                var outputPortModel = newModelToConnect.GetPortFitToConnectTo(wireInput);
                                var outputWire = outputPortModel == null ? null : graphModel.CreateWire(wireInput, outputPortModel);

                                if (inputWire != null && outputWire != null)
                                    autoPlacementUpdater.MarkModelToRepositionAtCreation((createdElement, inputWire, WireSide.To), new List<GraphElementModel> { outputWire });
                            }
                            break;
                        case ConnectionsToMake.ExistingWires:
                            foreach (var wire in creationData.WiresToConnect.ToList())
                            {
                                var wireModel = wire.model;
                                var portToConnect = creationData.LibraryItem.Data is NodeItemLibraryData nodeData ? nodeData.PortToConnect : null;
                                var newPort = portToConnect != null ?
                                    portNodeModel?.Ports.FirstOrDefault(p => p.UniqueName == portToConnect.UniqueName) :
                                    portNodeModel?.GetPortFitToConnectTo(wire.model.GetOtherPort(wire.side));

                                if (newPort != null)
                                {
                                    // Old wires to delete
                                    wiresToDelete.AddRange(WireCommandHelper_Internal.GetDropWireModelsToDelete(wireModel.GetOtherPort(wire.side)).Except(new [] { wireModel }));

                                    if (wire.model is IGhostWire _)
                                    {
                                        var toPort = wire.side == WireSide.To ? newPort : wire.model.ToPort;
                                        var fromPort = wire.side == WireSide.From ? newPort : wire.model.FromPort;
                                        wireModel = graphModel.CreateWire(toPort, fromPort);
                                    }
                                    else
                                    {
                                        wire.model.SetPort(wire.side, newPort);
                                    }
                                }
                                autoPlacementUpdater.MarkModelToRepositionAtCreation((createdElement, wireModel, wire.side));
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Delete old wires
                    if (wiresToDelete.Any())
                        graphModel.DeleteWires(wiresToDelete);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (createdElements.Any())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var undoStateUpdater = undoState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElements(createdElements, true);
                }
            }
        }
    }
}
