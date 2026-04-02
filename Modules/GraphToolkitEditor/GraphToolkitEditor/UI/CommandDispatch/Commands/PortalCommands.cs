// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to create a portal that complements an existing portal.
    /// </summary>
    [UnityRestricted]
    internal class CreateOppositePortalCommand : ModelCommand<WirePortalModel>
    {
        const string k_UndoStringSingular = "Create Opposite Portal";
        const string k_UndoStringPlural = "Create Opposite Portals";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        public CreateOppositePortalCommand()
            : base(k_UndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals for which an opposite portal should be created.</param>
        public CreateOppositePortalCommand(IReadOnlyList<WirePortalModel> portalModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, portalModels) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals for which an opposite portal should be created.</param>
        public CreateOppositePortalCommand(params WirePortalModel[] portalModels)
            : this((IReadOnlyList<WirePortalModel>)portalModels) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateOppositePortalCommand command)
        {
            if (!graphModelState.GraphModel.AllowPortalCreation)
                return;

            if (command.Models == null)
                return;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var portalsToOpen = command.Models.Where(p => p.CanCreateOppositePortal()).ToList();
#pragma warning restore UA2001
            if (portalsToOpen.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var createdElements = new List<GraphElementModel>(portalsToOpen.Count);
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var portalModel in portalsToOpen)
                {
                    var newPortal = graphModelState.GraphModel.CreateOppositePortal(portalModel);
                    createdElements.Add(newPortal);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (createdElements.Count > 0)
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

    /// <summary>
    /// Command to revert a portal to a wire.
    /// </summary>
    [UnityRestricted]
    internal class RevertPortalsToWireCommand : ModelCommand<WirePortalModel>
    {
        const string k_UndoString = "Revert to Wire";

        /// <summary>
        /// Initializes a new instance of the <see cref="RevertPortalsToWireCommand"/> class.
        /// </summary>
        public RevertPortalsToWireCommand()
            : base(k_UndoString) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RevertPortalsToWireCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals that are to be reverted to wires.</param>
        public RevertPortalsToWireCommand(IReadOnlyList<WirePortalModel> portalModels)
            : base(k_UndoString, k_UndoString, portalModels) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RevertPortalsToWireCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals that are to be reverted to wires.</param>
        public RevertPortalsToWireCommand(params WirePortalModel[] portalModels)
            : this((IReadOnlyList<WirePortalModel>)portalModels) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, RevertPortalsToWireCommand command)
        {
            if (!graphModelState.GraphModel.AllowPortalCreation)
                return;

            if (command.Models == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var createdElements = new List<GraphElementModel>();
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                for (var i = 0; i < command.Models.Count; i++)
                {
                    var wireModels = graphModelState.GraphModel.RevertPortalsToWires(command.Models[i], false);
                    if (wireModels != null)
                        createdElements.AddRange(wireModels);
                }

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (createdElements.Count > 0)
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

    /// <summary>
    /// Command to revert all portals of the same declaration to wires.
    /// </summary>
    [UnityRestricted]
    internal class RevertAllPortalsToWireCommand : ModelCommand<WirePortalModel>
    {
        const string k_UndoString = "Revert All to Wires";

        /// <summary>
        /// Initializes a new instance of the <see cref="RevertAllPortalsToWireCommand"/> class.
        /// </summary>
        public RevertAllPortalsToWireCommand()
            : base(k_UndoString) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RevertAllPortalsToWireCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals that are to be reverted to wires.</param>
        public RevertAllPortalsToWireCommand(IReadOnlyList<WirePortalModel> portalModels)
            : base(k_UndoString, k_UndoString, portalModels) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RevertAllPortalsToWireCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals that are to be reverted to wires.</param>
        public RevertAllPortalsToWireCommand(params WirePortalModel[] portalModels)
            : this((IReadOnlyList<WirePortalModel>)portalModels) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, RevertAllPortalsToWireCommand command)
        {
            if (!graphModelState.GraphModel.AllowPortalCreation)
                return;

            if (command.Models == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var createdElements = new List<GraphElementModel>();
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                for (var i = 0; i < command.Models.Count; i++)
                {
                    var wireModels = graphModelState.GraphModel.RevertPortalsToWires(command.Models[i], true);
                    if (wireModels != null)
                        createdElements.AddRange(wireModels);
                }

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (createdElements.Count > 0)
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
