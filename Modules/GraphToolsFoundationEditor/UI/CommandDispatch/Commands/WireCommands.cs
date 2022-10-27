// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolsFoundation.Editor
{
    static class WireCommandConfig_Internal
    {
        public const int nodeOffset = 60;
    }

    /// <summary>
    /// Command to create a new wire.
    /// </summary>
    class CreateWireCommand : UndoableCommand
    {
        const string k_UndoString = "Create Wire";

        /// <summary>
        /// Destination port.
        /// </summary>
        public PortModel ToPortModel;
        /// <summary>
        /// Origin port.
        /// </summary>
        public PortModel FromPortModel;
        /// <summary>
        /// Align the node that owns the <see cref="FromPortModel"/> to the <see cref="ToPortModel"/>.
        /// </summary>
        public bool AlignFromNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWireCommand" /> class.
        /// </summary>
        public CreateWireCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWireCommand" /> class.
        /// </summary>
        /// <param name="toPortModel">Destination port.</param>
        /// <param name="fromPortModel">Origin port.</param>
        /// <param name="alignFromNode">Set to true if the node that owns the <paramref name="fromPortModel"/> should be aligned on the <paramref name="toPortModel"/>.</param>
        public CreateWireCommand(PortModel toPortModel, PortModel fromPortModel, bool alignFromNode = false)
            : this()
        {
            Assert.IsTrue(toPortModel == null || toPortModel.Direction == PortDirection.Input);
            Assert.IsTrue(fromPortModel == null || fromPortModel.Direction == PortDirection.Output);
            ToPortModel = toPortModel;
            FromPortModel = fromPortModel;
            AlignFromNode = alignFromNode;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="preferences">The tool preferences.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, Preferences preferences, CreateWireCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
                undoStateUpdater.SaveStates(selectionHelper.UndoableSelectionStates);
            }

            var createdElements = new List<GraphElementModel>();
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;

                var fromPortModel = command.FromPortModel;
                var toPortModel = command.ToPortModel;

                var wiresToDelete = GetDropWireModelsToDelete(command.FromPortModel)
                    .Concat(GetDropWireModelsToDelete(command.ToPortModel))
                    .ToList();

                if (wiresToDelete.Count > 0)
                {
                    var deletedElements = graphModel.DeleteWires(wiresToDelete);

                    foreach (var wireToDelete in wiresToDelete)
                    {
                        var toPort = wireToDelete.ToPort;
                        var fromPort = wireToDelete.FromPort;
                        if (toPort.NodeModel is SubgraphNodeModel && toPort.PortType == PortType.MissingPort && !toPort.GetConnectedWires().Any())
                        {
                            if (toPort.NodeModel.RemoveUnusedMissingPort(toPort))
                                graphUpdater.MarkChanged(toPort.NodeModel, ChangeHint.GraphTopology);
                        }

                        if (fromPort.NodeModel is SubgraphNodeModel && fromPort.PortType == PortType.MissingPort && !fromPort.GetConnectedWires().Any())
                        {
                            if (fromPort.NodeModel.RemoveUnusedMissingPort(fromPort))
                                graphUpdater.MarkChanged(fromPort.NodeModel, ChangeHint.GraphTopology);
                        }
                    }

                    graphUpdater.MarkDeleted(deletedElements);
                }

                WireModel wireModel;
                // Auto-itemization preferences will determine if a new node is created or not
                if ((fromPortModel.NodeModel is ConstantNodeModel && preferences.GetBool(BoolPref.AutoItemizeConstants)) ||
                    (fromPortModel.NodeModel is VariableNodeModel && preferences.GetBool(BoolPref.AutoItemizeVariables)))
                {
                    var itemizedNode = graphModel.CreateItemizedNode(WireCommandConfig_Internal.nodeOffset, ref fromPortModel);
                    if (itemizedNode != null)
                    {
                        graphUpdater.MarkNew(itemizedNode);
                        createdElements.Add(itemizedNode);
                    }
                    wireModel = graphModel.CreateWire(toPortModel, fromPortModel);
                }
                else
                {
                    wireModel = graphModel.CreateWire(toPortModel, fromPortModel);
                    createdElements.Add(wireModel);
                }
                graphUpdater.MarkNew(wireModel);

                if (toPortModel != null)
                {
                    graphUpdater.MarkChanged(toPortModel, ChangeHint.GraphTopology);
                }

                if (fromPortModel != null)
                {
                    graphUpdater.MarkChanged(fromPortModel, ChangeHint.GraphTopology);
                }

                if (command.AlignFromNode)
                {
                    graphUpdater.MarkModelToAutoAlign(wireModel);
                }
            }

            if (createdElements.Any())
            {
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElements(createdElements, true);
                }
            }
        }

        static IEnumerable<WireModel> GetDropWireModelsToDelete(PortModel portModel)
        {
            if (portModel == null || portModel.Capacity == PortCapacity.Multi)
                return Enumerable.Empty<WireModel>();
            return portModel.GetConnectedWires().Where(e => !(e is IGhostWire));
        }
    }

    /// <summary>
    /// Command to move one or more wires to a new port.
    /// </summary>
    class MoveWireCommand : ModelCommand<WireModel>
    {
        const string k_UndoStringSingular = "Move Wire";
        const string k_UndoStringPlural = "Move Wires";

        /// <summary>
        /// The port where to move the wire(s).
        /// </summary>
        public PortModel NewPortModel;

        /// <summary>
        /// The Side of the wire to move.
        /// </summary>
        /// <remarks>In most case this should be inferred from <see cref="NewPortModel"/>,
        /// unless its <see cref="PortDirection"/> is not explicitly <see cref="PortDirection.Input"/> or <see cref="PortDirection.Output"/>.</remarks>
        public WireSide WireSideToMove;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveWireCommand" /> class.
        /// </summary>
        public MoveWireCommand()
            : base(k_UndoStringSingular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveWireCommand" /> class.
        /// </summary>
        /// <param name="newPortModel">The port where to move the wire(s).</param>
        /// <param name="wireSide">The side of the wire(s) to move to the port.</param>
        /// <param name="wiresToMove">The list of wires to move.</param>
        public MoveWireCommand(PortModel newPortModel, WireSide wireSide, IReadOnlyList<WireModel> wiresToMove)
            : base(k_UndoStringSingular, k_UndoStringPlural, wiresToMove)
        {
            WireSideToMove = wireSide;
            NewPortModel = newPortModel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveWireCommand" /> class.
        /// </summary>
        /// <param name="newPortModel">The port where to move the wire(s).</param>
        /// <param name="wireSide">The side of the wire(s) to move to the port.</param>
        /// <param name="wiresToMove">The list of wires to move.</param>
        public MoveWireCommand(PortModel newPortModel, WireSide wireSide, params WireModel[] wiresToMove)
            : this(newPortModel, wireSide, (IReadOnlyList<WireModel>)(wiresToMove))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveWireCommand" /> class.
        /// </summary>
        /// <param name="newPortModel">The port where to move the wire(s).</param>
        /// <param name="wiresToMove">The list of wires to move.</param>
        public MoveWireCommand(PortModel newPortModel, IReadOnlyList<WireModel> wiresToMove)
            : this(newPortModel, WireSide.From, wiresToMove)
        {
            if (newPortModel != null)
            {
                Assert.IsNotNull(newPortModel);
                var newDir = newPortModel.Direction;
                WireSideToMove = newDir == PortDirection.Input ? WireSide.To : WireSide.From;
                Assert.IsFalse(newDir == PortDirection.None,
                    $"Can't move wires to a Port with direction {PortDirection.None}.");
                Assert.IsFalse(newDir.HasFlag(PortDirection.Input) == newDir.HasFlag(PortDirection.Output),
                    $"Can't infer port direction from port {newPortModel}, use the constructor for {nameof(MoveWireCommand)} with a specific direction.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveWireCommand" /> class.
        /// </summary>
        /// <param name="newPortModel">The port where to move the wire(s).</param>
        /// <param name="wiresToMove">The list of wires to move.</param>
        public MoveWireCommand(PortModel newPortModel, params WireModel[] wiresToMove)
            : this(newPortModel, (IReadOnlyList<WireModel>)(wiresToMove))
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            MoveWireCommand command)
        {
            if (command.Models == null || !command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                PortModel oldPort = null;
                var wiresToMove = command.Models;
                wiresToMove = wiresToMove.OrderBy(w => w, WiresOrderComparer.Default).ToList();

                foreach (var wire in wiresToMove)
                {
                    oldPort ??= wire.GetPort(command.WireSideToMove);
                    wire.SetPort(command.WireSideToMove, command.NewPortModel);

                    if (oldPort.PortType == PortType.MissingPort)
                    {
                        var nodeModel = oldPort.NodeModel;
                        if (nodeModel != null && nodeModel.RemoveUnusedMissingPort(oldPort))
                            graphUpdater.MarkChanged(nodeModel, ChangeHint.GraphTopology);
                    }
                    graphUpdater.MarkChanged(wire, ChangeHint.GraphTopology);
                }

                graphUpdater.MarkChanged(oldPort, ChangeHint.GraphTopology);
                graphUpdater.MarkChanged(command.NewPortModel, ChangeHint.GraphTopology);
            }
        }

        class WiresOrderComparer : IComparer<WireModel>
        {
            public static WiresOrderComparer Default = new WiresOrderComparer();

            public int Compare(WireModel a, WireModel b)
            {
                if (a == null || a.FromPort == null || b == null || !ReferenceEquals(b.FromPort, a.FromPort))
                    return 0;
                return a.FromPort.GetWireOrder(a) - a.FromPort.GetWireOrder(b);
            }
        }
    }

    /// <summary>
    /// Command to delete one or more wires.
    /// </summary>
    class DeleteWireCommand : ModelCommand<WireModel>
    {
        const string k_UndoStringSingular = "Delete Wire";
        const string k_UndoStringPlural = "Delete Wires";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteWireCommand" /> class.
        /// </summary>
        public DeleteWireCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteWireCommand" /> class.
        /// </summary>
        /// <param name="wiresToDelete">The list of wires to delete.</param>
        public DeleteWireCommand(IReadOnlyList<WireModel> wiresToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural, wiresToDelete)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteWireCommand" /> class.
        /// </summary>
        /// <param name="wiresToDelete">The list of wires to delete.</param>
        public DeleteWireCommand(params WireModel[] wiresToDelete)
            : this((IReadOnlyList<WireModel>)wiresToDelete)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, DeleteWireCommand command)
        {
            if (command.Models == null || !command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var wire in command.Models)
                {
                    if (wire.ToPort != null)
                    {
                        graphUpdater.MarkChanged(wire.ToPort, ChangeHint.GraphTopology);
                    }

                    if (wire.FromPort != null)
                    {
                        graphUpdater.MarkChanged(wire.FromPort, ChangeHint.GraphTopology);
                    }
                }

                var deletedElements = graphModelState.GraphModel.DeleteWires(command.Models);
                graphUpdater.MarkDeleted(deletedElements);
            }
        }
    }

    /// <summary>
    /// Command to change the order of a wire.
    /// </summary>
    class ReorderWireCommand : UndoableCommand
    {
        /// <summary>
        /// The wire to reorder.
        /// </summary>
        public readonly WireModel WireModel;
        /// <summary>
        /// The reorder operation to apply.
        /// </summary>
        public readonly ReorderType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderWireCommand"/> class.
        /// </summary>
        public ReorderWireCommand()
        {
            UndoString = "Reorder Wire";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderWireCommand"/> class.
        /// </summary>
        /// <param name="wireModel">The wire to reorder.</param>
        /// <param name="type">The reorder operation to apply.</param>
        public ReorderWireCommand(WireModel wireModel, ReorderType type) : this()
        {
            WireModel = wireModel;
            Type = type;

            switch (Type)
            {
                case ReorderType.MoveFirst:
                    UndoString = "Move Wire First";
                    break;
                case ReorderType.MoveUp:
                    UndoString = "Move Wire Up";
                    break;
                case ReorderType.MoveDown:
                    UndoString = "Move Wire Down";
                    break;
                case ReorderType.MoveLast:
                    UndoString = "Move Wire Last";
                    break;
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ReorderWireCommand command)
        {
            var fromPort = command.WireModel?.FromPort;
            if (fromPort != null && fromPort.HasReorderableWires)
            {
                var siblingWires = fromPort.GetConnectedWires().ToList();
                if (siblingWires.Count < 2)
                    return;

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var graphUpdater = graphModelState.UpdateScope)
                {
                    fromPort.ReorderWire(command.WireModel, command.Type);

                    graphUpdater.MarkChanged(siblingWires, ChangeHint.GraphTopology);
                    graphUpdater.MarkChanged(fromPort.NodeModel, ChangeHint.GraphTopology);
                }
            }
        }
    }

    /// <summary>
    /// Command to insert a node in the middle of a wire.
    /// </summary>
    class SplitWireAndInsertExistingNodeCommand : UndoableCommand
    {
        public readonly WireModel WireModel;
        public readonly InputOutputPortsNodeModel NodeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitWireAndInsertExistingNodeCommand"/> class.
        /// </summary>
        public SplitWireAndInsertExistingNodeCommand()
        {
            UndoString = "Insert Node On Wire";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitWireAndInsertExistingNodeCommand"/> class.
        /// </summary>
        /// <param name="wireModel">The wire on which to insert a node.</param>
        /// <param name="nodeModel">The node to insert.</param>
        public SplitWireAndInsertExistingNodeCommand(WireModel wireModel, InputOutputPortsNodeModel nodeModel) : this()
        {
            WireModel = wireModel;
            NodeModel = nodeModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SplitWireAndInsertExistingNodeCommand command)
        {
            Assert.IsTrue(command.NodeModel.InputsById.Count > 0);
            Assert.IsTrue(command.NodeModel.OutputsById.Count > 0);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;
                var wireInput = command.WireModel.ToPort;
                var wireOutput = command.WireModel.FromPort;
                var deletedModels = graphModel.DeleteWire(command.WireModel);
                var wire1 = graphModel.CreateWire(wireInput, command.NodeModel.OutputsByDisplayOrder.First(p => p?.PortType == wireInput?.PortType));
                var wire2 = graphModel.CreateWire(command.NodeModel.InputsByDisplayOrder.First(p => p?.PortType == wireOutput?.PortType), wireOutput);

                graphUpdater.MarkDeleted(deletedModels);
                graphUpdater.MarkNew(wire1);
                graphUpdater.MarkNew(wire2);
            }
        }
    }

    /// <summary>
    /// Command to convert wires to portal nodes.
    /// </summary>
    class ConvertWiresToPortalsCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Convert Wire to Portal";
        const string k_UndoStringPlural = "Convert Wires to Portals";

        static readonly Vector2 k_EntryPortalBaseOffset = Vector2.right * 75;
        static readonly Vector2 k_ExitPortalBaseOffset = Vector2.left * 250;
        const int k_PortalHeight = 24;

        /// <summary>
        /// Data describing which wire to transform and the position of the portals.
        /// </summary>
        public List<(WireModel wire, Vector2 startPortPos, Vector2 endPortPos)> WireData;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertWiresToPortalsCommand"/> class.
        /// </summary>
        public ConvertWiresToPortalsCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertWiresToPortalsCommand"/> class.
        /// </summary>
        /// <param name="wireData">A list of tuple, each tuple containing the wire to convert, the position of the entry portal node and the position of the exit portal node.</param>
        public ConvertWiresToPortalsCommand(IReadOnlyList<(WireModel, Vector2, Vector2)> wireData) : this()
        {
            WireData = wireData?.ToList();
            UndoString = (WireData?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, ConvertWiresToPortalsCommand command)
        {
            if (command.WireData == null || !command.WireData.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var createdElements = new List<GraphElementModel>();
            using (var updater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;
                var existingPortalEntries = new Dictionary<PortModel, WirePortalModel>();
                var existingPortalExits = new Dictionary<PortModel, List<WirePortalModel>>();

                var newModels = new List<GraphElementModel>(command.WireData.Count * 5); // Estimate of the list size to avoid reallocation. There will usually be 5 elements created per wire: two portals, two new wires and a portal declaration model.
                var modelsToDelete = new List<GraphElementModel>();
                var modelsToChange = new List<GraphElementModel>();
                var changeHintList = new List<ChangeHint>();

                foreach (var changeDescription in command.WireData.Select(wireModel =>
                    graphModel.CreatePortalsFromWire(
                        wireModel.wire,
                        wireModel.startPortPos + k_EntryPortalBaseOffset,
                        wireModel.endPortPos + k_ExitPortalBaseOffset,
                        k_PortalHeight, existingPortalEntries, existingPortalExits)))
                {
                    newModels.AddRange(changeDescription.NewModels);

                    if (changeDescription.DeletedModels != null)
                        modelsToDelete.AddRange(changeDescription.DeletedModels);

                    if (changeDescription.ChangedModels != null)
                    {
                        var (model, changeHints) = changeDescription.ChangedModels.FirstOrDefault();
                        if (model != null && changeHints != null)
                        {
                            modelsToChange.Add(model);
                            changeHintList.AddRange(changeHints);
                        }
                    }
                }

                // Adjust placement in case of multiple incoming exit portals so they don't overlap
                foreach (var portalList in existingPortalExits.Values.Where(l => l.Count > 1))
                {
                    var cnt = portalList.Count;
                    bool isEven = cnt % 2 == 0;
                    int offset = isEven ? k_PortalHeight / 2 : 0;
                    for (int i = (cnt - 1) / 2; i >= 0; i--)
                    {
                        portalList[i].Position = new Vector2(portalList[i].Position.x, portalList[i].Position.y - offset);
                        portalList[cnt - 1 - i].Position = new Vector2(portalList[cnt - 1 - i].Position.x, portalList[cnt - 1 - i].Position.y + offset);
                        offset += k_PortalHeight;
                    }
                }

                updater.MarkDeleted(modelsToDelete);
                updater.MarkChanged(modelsToChange, changeHintList);
                updater.MarkNew(newModels);
                createdElements.AddRange(newModels.OfType<AbstractNodeModel>());
            }

            if (createdElements.Any())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElements(createdElements, true);
                }
            }
        }
    }
}
