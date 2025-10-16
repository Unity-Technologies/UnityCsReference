// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to create a node from a <see cref="GraphNodeModelLibraryItem"/>.
    /// </summary>
    [UnityRestricted]
    internal class CreateNodeCommand : UndoableCommand
    {
        /// <summary>
        /// Data used by <see cref="CreateNodeCommand"/> to create one node.
        /// </summary>
        [UnityRestricted]
        internal struct NodeData
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
            public VariableDeclarationModelBase VariableDeclaration;

            /// <summary>
            /// The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.
            /// </summary>
            public GraphNodeModelLibraryItem NodeLibraryItem;

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

            /// <summary>
            /// The <see cref="VariableCreationInfos"/> to create a variable.
            /// </summary>
            public VariableCreationInfos VariableCreationInfos;

            /// <summary>
            /// The guid to assign to the newly created variable declaration.
            /// </summary>
            public Hash128 VariableDeclarationGuid;
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
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a variable node on the graph.
        /// </summary>
        /// <param name="item">The <see cref="VariableLibraryItem"/> representing the node to create.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="group">The group to insert the variable in.</param>
        /// <param name="indexInGroup">The index in the group where the variable will be inserted.</param>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on the graph.</returns>
        public static CreateNodeCommand OnGraph(VariableLibraryItem item,
            Vector2 position = default,
            GroupModel group = null,
            int indexInGroup = -1,
            string variableName = "",
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnGraph(item, position, group, indexInGroup, variableName, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a variable on the graph.
        /// </summary>
        /// <param name="model">The declaration for the variable to create on the graph.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a variable on the graph.</returns>
        public static CreateNodeCommand OnGraph(VariableDeclarationModelBase model,
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
        public static CreateNodeCommand OnPort(VariableDeclarationModelBase model,
            PortModel portModel,
            Vector2 position = default,
            bool autoAlign = false,
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnPort(model, portModel, position, autoAlign, guid);
        }

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a variable node and its corresponding declaration, and connect it to an existing port.
        /// </summary>
        /// <param name="portModel">The port to connect the new node to. The data type and name of the variable is taken from the port.</param>
        /// <param name="group">The group to insert the variable in.</param>
        /// <param name="indexInGroup">The index in the group where the variable will be inserted.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="variableType">The type of variable to create.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the created node will be automatically aligned after being created.</param>
        /// <param name="variableDeclarationGuid">The guid to assign to the newly created variable declaration. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <param name="variableNodeGuid">The guid to assign to the newly created variable node. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on a wire.</returns>
        public static CreateNodeCommand OnPort(PortModel portModel,
            GroupModel group = null,
            int indexInGroup = -1,
            ModifierFlags modifierFlags = ModifierFlags.None,
            VariableScope scope = VariableScope.Local,
            Type variableType = null,
            Vector2 position = default,
            bool autoAlign = true,
            Hash128 variableDeclarationGuid = default,
            Hash128 variableNodeGuid = default)
        {
            var groupModel = group ?? portModel.GraphModel.GetSectionModel(GraphModel.DefaultSectionName);
            var variableCreationInfos = new VariableCreationInfos
            {
                VariableType = variableType,
                TypeHandle = portModel.DataTypeHandle,
                Name = !string.IsNullOrEmpty(portModel.Title) ? portModel.Title : portModel.Direction == PortDirection.Input ? "Input" : "Output",
                ModifierFlags = modifierFlags,
                Scope = scope,
                Group = groupModel,
                IndexInGroup = indexInGroup == -1 ? groupModel.Items.Count : indexInGroup,
            };

            return new CreateNodeCommand().WithNodeOnPort(portModel, variableCreationInfos, position, autoAlign, variableDeclarationGuid, variableNodeGuid);
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

        /// <summary>
        /// Initializes a new <see cref="CreateNodeCommand"/> to create a node and connect it to existing wires.
        /// </summary>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="wires">The wires to connect to the node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="group">The group to insert the variable in.</param>
        /// <param name="indexInGroup">The index in the group where the variable will be inserted.</param>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>A <see cref="CreateNodeCommand"/> that can be dispatched to create a node on a port.</returns>
        public static CreateNodeCommand OnWireSide(VariableLibraryItem item,
            IEnumerable<(WireModel, WireSide)> wires,
            Vector2 position,
            GroupModel group = null,
            int indexInGroup = -1,
            string variableName = "",
            Hash128 guid = default)
        {
            return new CreateNodeCommand().WithNodeOnWires(item, wires, position, group, indexInGroup, variableName, guid);
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
        /// <param name="autoPlacementState">The auto placement state.</param>
        /// <param name="preferences">The tool preferences.</param>
        /// <param name="command">The command to handle.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState,
            AutoPlacementStateComponent autoPlacementState, Preferences preferences, CreateNodeCommand command)
        {
            if (command.CreationData.Count <= 0)
            {
                Debug.LogError("Creation command dispatched with 0 item to create");
                return;
            }

            if (command.CreationData.All(nodeData => nodeData.VariableDeclaration == null) && command.CreationData.All(nodeData => nodeData.NodeLibraryItem == null) && command.CreationData.All(nodeData => nodeData.VariableCreationInfos == null))
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
                undoStateUpdater.SaveState(autoPlacementState);
            }

            var elementsToSelect = new List<GraphElementModel>();
            var graphModel = graphModelState.GraphModel;

            using (var autoPlacementUpdater = autoPlacementState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                foreach (var creationData in command.CreationData)
                {
                    var variableDeclaration = creationData.VariableDeclaration;
                    if ((variableDeclaration == null) == (creationData.NodeLibraryItem == null))
                    {
                        // If there is a port, try to create a variable from the variable creation infos
                        if (creationData.VariableCreationInfos != null)
                        {
                            var creationInfos = creationData.VariableCreationInfos;
                            if (creationInfos.VariableType != null)
                                variableDeclaration = graphModel.CreateGraphVariableDeclaration(creationInfos.VariableType, creationInfos.TypeHandle,
                                    creationInfos.Name, creationInfos.ModifierFlags, creationInfos.Scope, creationInfos.Group, creationInfos.IndexInGroup, null,
                                    creationData.VariableDeclarationGuid);
                            else
                                variableDeclaration = graphModel.CreateGraphVariableDeclaration(creationInfos.TypeHandle,
                                    creationInfos.Name, creationInfos.ModifierFlags, creationInfos.Scope, creationInfos.Group, creationInfos.IndexInGroup, null,
                                    creationData.VariableDeclarationGuid);
                        }

                        if (variableDeclaration == null)
                        {
                            Debug.LogWarning("Creation command dispatched with invalid item to create: either provide VariableDeclaration or LibraryItem. Ignoring this item.");
                            continue;
                        }
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

                    var guid = creationData.Guid.isValid ? creationData.Guid : Hash128Helpers.GenerateUnique();

                    // Create new element
                    GraphElementModel createdElement;
                    if (variableDeclaration != null)
                    {
                        if (graphModel.CanCreateVariableNode(variableDeclaration, graphModel))
                        {
                            createdElement = graphModel.CreateVariableNode(variableDeclaration, creationData.Position, guid);
                        }
                        else
                        {
                            Debug.LogWarning($"Could not create a new variable node for variable {variableDeclaration.Title}.");
                            continue;
                        }
                    }
                    else if (creationData.NodeLibraryItem.Data is NodeItemLibraryData nodeData && nodeData.SubgraphReference != default && !nodeData.SubgraphReference.HasAssetReference)
                    {
                        var subGraphModel = graphModel.ResolveGraphModelFromReference(nodeData.SubgraphReference);
                        // If the node to create is a local subgraph node.
                        var localSubgraph = graphModel.CreateLocalSubgraph(
                            subGraphModel.GetType(),
                            subGraphModel.Name);

                        if (localSubgraph is null)
                            continue;

                        localSubgraph.CloneGraph(subGraphModel, true);

                        createdElement = graphModel.CreateSubgraphNode(
                            localSubgraph,
                            creationData.Position,
                            guid);
                    }
                    else
                    {
                        createdElement = creationData.NodeLibraryItem.CreateElement(
                            new GraphNodeCreationData(graphModel, creationData.Position, guid: guid));
                    }

                    if (createdElement != null)
                    {
                        if (creationData.VariableCreationInfos != null && createdElement is VariableNodeModel variableNode)
                        {
                            // Variable node should not be selected, their declaration should be (which is done in GraphVariablesObserver)
                            if (variableNode.VariableDeclarationModel.IsRenamable())
                                graphUpdater.MarkForRename(createdElement);

                            // When a variable node is created on the graph with the "Create Variable" item,
                            // it should be expanded in the bb for the user to change its type more easily if needed
                            if (connectionsToMake == ConnectionsToMake.None)
                                graphUpdater.MarkForExpand(new[] { variableDeclaration });
                        }
                        else
                        {
                            elementsToSelect.Add(createdElement);
                            if (createdElement.IsRenamable())
                                graphUpdater.MarkForRename(createdElement);
                        }
                    }

                    var wiresToDelete = new List<WireModel>();
                    var portNodeModel = createdElement as PortNodeModel;
                    switch (connectionsToMake)
                    {
                        case ConnectionsToMake.None:
                            break;
                        case ConnectionsToMake.ExistingPort:
                            var existingPortToConnect = creationData.PortModel;
                            var portToConnectItem = creationData.NodeLibraryItem?.Data is NodeItemLibraryData data ? data.PortToConnect : null;
                            var newPortToConnect = portToConnectItem != null ?
                                portNodeModel?.GetPorts().FirstOrDefault(p => p.UniqueName == portToConnectItem.UniqueName) :
                                portNodeModel?.GetPortFitToConnectTo(existingPortToConnect);

                            if (newPortToConnect != null)
                            {
                                // Old wires to delete
                                wiresToDelete = WireCommandHelper.GetDropWireModelsToDelete(creationData.PortModel).ToList();

                                WireModel newWire;
                                if (existingPortToConnect.Direction == PortDirection.Output)
                                {
                                    if (existingPortToConnect.NodeModel is ConstantNodeModel && preferences.GetBool(BoolPref.AutoItemizeConstants)
                                        || existingPortToConnect.NodeModel is VariableNodeModel && preferences.GetBool(BoolPref.AutoItemizeVariables))
                                    {
                                        var newNode = graphModel.CreateItemizedNode(WireCommandConfig.nodeOffset,
                                            ref existingPortToConnect);
                                        elementsToSelect.Add(newNode);
                                    }

                                    newWire = graphModel.CreateWire(newPortToConnect, existingPortToConnect);
                                }
                                else
                                {
                                    newWire = graphModel.CreateWire(existingPortToConnect, newPortToConnect);
                                }

                                elementsToSelect.Add(newWire);

                                if (newWire != null && creationData.AutoAlign ||
                                    preferences.GetBool(BoolPref.AutoAlignDraggedWires))
                                {
                                    autoPlacementUpdater.MarkModelToRepositionAtCreation(
                                        (portNodeModel, newWire, newPortToConnect.Direction == PortDirection.Output ? WireSide.From : WireSide.To),
                                        AutoPlacementStateComponent.Changeset.RepositionType.FromPort,
                                        new List<GraphElementModel> { portNodeModel });
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
                                    autoPlacementUpdater.MarkModelToRepositionAtCreation(
                                        (createdElement, inputWire, WireSide.To),
                                        AutoPlacementStateComponent.Changeset.RepositionType.FromWire,
                                        new List<GraphElementModel> { outputWire });
                            }
                            break;
                        case ConnectionsToMake.ExistingWires:
                            foreach (var wire in creationData.WiresToConnect.ToList())
                            {
                                var portToConnect = creationData.NodeLibraryItem?.Data is NodeItemLibraryData nodeData ? nodeData.PortToConnect : null;
                                var newPort = portToConnect != null ?
                                    portNodeModel?.GetPorts().FirstOrDefault(p => p.UniqueName == portToConnect.UniqueName) :
                                    portNodeModel?.GetPortFitToConnectTo(wire.model.GetOtherPort(wire.side));

                                WireModel newWire = null;
                                if (newPort != null)
                                {
                                    // Old wires to delete
                                    wiresToDelete.AddRange(WireCommandHelper.GetDropWireModelsToDelete(wire.model.GetOtherPort(wire.side), exceptWires: new List<WireModel> { wire.model }));

                                    if (wire.model is IGhostWireModel)
                                    {
                                        if (wire.side == WireSide.To)
                                        {
                                            if (graphModel.TryGetModelFromGuid(wire.model.FromNodeGuid, out _))
                                                newWire = graphModel.CreateWire(newPort, wire.model.FromPort);
                                        }
                                        else
                                        {
                                            if (graphModel.TryGetModelFromGuid(wire.model.ToNodeGuid, out _))
                                                newWire = graphModel.CreateWire(wire.model.ToPort, newPort);
                                        }
                                    }
                                    else
                                    {
                                        wire.model.SetPort(wire.side, newPort);
                                    }
                                }
                                if (newWire is not null || wire.model is not IGhostWireModel)
                                    autoPlacementUpdater.MarkModelToRepositionAtCreation((createdElement, newWire ?? wire.model, wire.side), AutoPlacementStateComponent.Changeset.RepositionType.FromWire);
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

            if (elementsToSelect.Any())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var undoStateUpdater = undoState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElements(elementsToSelect, true);
                }
            }
        }
    }
}
