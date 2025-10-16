// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to create a placemat.
    /// </summary>
    [UnityRestricted]
    internal class CreatePlacematCommand : UndoableCommand
    {
        /// <summary>
        /// The position and size of the new placemat.
        /// </summary>
        public Rect Position;
        /// <summary>
        /// The placemat title.
        /// </summary>
        public string Title;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        public CreatePlacematCommand()
        {
            UndoString = "Create Placemat";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        /// <param name="position">The position of the new placemat.</param>
        /// <param name="title">The title of the new placemat.</param>
        public CreatePlacematCommand(Rect position, string title = null) : this()
        {
            Position = position;
            Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        /// <param name="position">The position of the new placemat.</param>
        /// <param name="title">The title of the new placemat.</param>
        public CreatePlacematCommand(Vector2 position, string title = null) : this(new Rect(position, Placemat.DefaultPlacematSize), title)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreatePlacematCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            PlacematModel placematModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                placematModel = graphModelState.GraphModel.CreatePlacemat(command.Position);
                if (command.Title != null)
                    placematModel.Title = command.Title;

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
                graphUpdater.MarkForRename(placematModel);
            }

            if (placematModel != null)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var undoStateUpdater = undoState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElement(placematModel, true);
                }
            }
        }
    }

    /// <summary>
    /// Command to change the Z order of placemats.
    /// </summary>
    [UnityRestricted]
    internal class ChangePlacematOrderCommand : ModelCommand<PlacematModel>
    {
        const string k_MovePlacematForwardStringSingular = "Move Placemat Forward";
        const string k_MovePlacematForwardStringPlural = "Move Placemats Forward";

        const string k_MovePlacematBackwardStringSingular = "Move Placemat Backward";
        const string k_MovePlacematBackwardStringPlural = "Move Placemats Backward";

        const string k_MovePlacematTopStringSingular = "Move Placemat To Front";
        const string k_MovePlacematTopStringPlural = "Move Placemats To Front";

        const string k_MovePlacematBottomStringSingular = "Move Placemat To Back";
        const string k_MovePlacematBottomStringPlural = "Move Placemats To Back";

        /// <summary>
        /// The type of reordering required.
        /// </summary>
        public ZOrderMove OrderingAction;

        /// <summary>
        /// Initializes a new instance of the ChangePlacematOrderCommand class.
        /// </summary>
        /// <param name="orderingAction">The type of reordering required.</param>
        /// <param name="models">The models to reorder.</param>
        public ChangePlacematOrderCommand(ZOrderMove orderingAction, IReadOnlyList<PlacematModel> models) :
            base("Change placemat order", "Change placemats order", models)
        {
            OrderingAction = orderingAction;
            switch (orderingAction)
            {
                case ZOrderMove.Forward:
                    UndoString = models?.Count > 1 ? k_MovePlacematForwardStringPlural : k_MovePlacematForwardStringSingular;
                    break;
                case ZOrderMove.Backward:
                    UndoString = models?.Count > 1 ? k_MovePlacematBackwardStringPlural : k_MovePlacematBackwardStringSingular;
                    break;
                case ZOrderMove.ToFront:
                    UndoString = models?.Count > 1 ? k_MovePlacematTopStringPlural : k_MovePlacematTopStringSingular;
                    break;
                case ZOrderMove.ToBack:
                    UndoString = models?.Count > 1 ? k_MovePlacematBottomStringPlural : k_MovePlacematBottomStringSingular;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderingAction), orderingAction, null);
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangePlacematOrderCommand command)
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
                graphModelState.GraphModel.ReorderPlacemats(command.Models, command.OrderingAction);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to delete placemats and select their contents.
    /// </summary>
    [UnityRestricted]
    internal class DeleteAndSelectPlacematContentCommand : UndoableCommand
    {
        const string k_UndoString = "Delete and Select Contents";

        public IReadOnlyList<PlacematModel> PlacematModels;
        public GraphView GraphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteAndSelectPlacematContentCommand"/> class.
        /// </summary>
        public DeleteAndSelectPlacematContentCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteAndSelectPlacematContentCommand"/> class.
        /// </summary>
        /// <param name="placematModels">The placemats to delete.</param>
        /// <param name="graphView">The graph view.</param>
        public DeleteAndSelectPlacematContentCommand(IReadOnlyList<PlacematModel> placematModels, GraphView graphView) : this()
        {
            PlacematModels = placematModels;
            GraphView = graphView;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, DeleteAndSelectPlacematContentCommand command)
        {
            if (command.PlacematModels.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
                undoStateUpdater.SaveState(selectionState);
            }

            using (var selectionUpdater = selectionState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                var elementsToSelect = new List<GraphElementModel>();
                var placematsToDelete = new List<PlacematModel>(command.PlacematModels.Count);

                // Iterate through each placemat and collect the elements inside it.
                foreach (var placematModel in command.PlacematModels)
                {
                    if (!placematModel.IsDeletable())
                        continue;

                    placematsToDelete.Add(placematModel);

                    var placematView = placematModel.GetView<Placemat>(command.GraphView);
                    placematView?.ActOnGraphElementsInside(element =>
                    {
                        elementsToSelect.Add(element.GraphElementModel);
                        return false;
                    });
                }

                // Delete the placemats.
                graphModelState.GraphModel.DeleteElements(placematsToDelete);

                // Clear the selection and select the elements inside the placemats.
                selectionUpdater.ClearSelection();
                selectionUpdater.SelectElements(elementsToSelect, true);

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
