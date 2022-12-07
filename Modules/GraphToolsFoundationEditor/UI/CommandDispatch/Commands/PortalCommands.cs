// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to create a portal that complements an existing portal.
    /// </summary>
    class CreateOppositePortalCommand : ModelCommand<WirePortalModel>
    {
        const string k_UndoStringSingular = "Create Opposite Portal";
        const string k_UndoStringPlural = "Create Opposite Portals";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        public CreateOppositePortalCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals for which an opposite portal should be created.</param>
        public CreateOppositePortalCommand(IReadOnlyList<WirePortalModel> portalModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, portalModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals for which an opposite portal should be created.</param>
        public CreateOppositePortalCommand(params WirePortalModel[] portalModels)
            : this((IReadOnlyList<WirePortalModel>)portalModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateOppositePortalCommand command)
        {
            if (command.Models == null)
                return;

            var portalsToOpen = command.Models.Where(p => p.CanCreateOppositePortal()).ToList();
            if (!portalsToOpen.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            var createdElements = new List<GraphElementModel>();
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
