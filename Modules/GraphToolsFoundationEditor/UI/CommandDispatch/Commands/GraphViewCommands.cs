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
    /// Command to change the position of graph elements.
    /// </summary>
    class MoveElementsCommand : ModelCommand<IMovable, Vector2>
    {
        const string k_UndoStringSingular = "Move Element";
        const string k_UndoStringPlural = "Move Elements";

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementsCommand"/> class.
        /// </summary>
        public MoveElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementsCommand"/> class.
        /// </summary>
        /// <param name="delta">The amount of the move.</param>
        /// <param name="models">The models to move.</param>
        public MoveElementsCommand(Vector2 delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, delta, models) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementsCommand"/> class.
        /// </summary>
        /// <param name="delta">The amount of the move.</param>
        /// <param name="models">The models to move.</param>
        public MoveElementsCommand(Vector2 delta, params IMovable[] models)
            : this(delta, (IReadOnlyList<IMovable>)models) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, MoveElementsCommand command)
        {
            if (command.Models == null || command.Value == Vector2.zero)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var movable in command.Models)
                {
                    movable.Move(command.Value);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to change the position of graph elements as the result of an automatic
    /// placement (auto-spacing or auto-align).
    /// </summary>
    // PF FIXME merge with MoveElementsCommand?
    class AutoPlaceElementsCommand : ModelCommand<IMovable>
    {
        const string k_UndoStringSingular = "Auto Place Element";
        const string k_UndoStringPlural = "Auto Place Elements";

        /// <summary>
        /// The delta to apply to the model positions.
        /// </summary>
        public IReadOnlyList<Vector2> Deltas;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPlaceElementsCommand"/> class.
        /// </summary>
        public AutoPlaceElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPlaceElementsCommand"/> class.
        /// </summary>
        /// <param name="delta">The amount of the move.</param>
        /// <param name="models">The models to move.</param>
        public AutoPlaceElementsCommand(IReadOnlyList<Vector2> delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Deltas = delta;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AutoPlaceElementsCommand command)
        {
            if (command.Models == null || command.Deltas == null || command.Models.Count != command.Deltas.Count)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                for (int i = 0; i < command.Models.Count; ++i)
                {
                    IMovable model = command.Models[i];
                    Vector2 delta = command.Deltas[i];
                    model.Move(delta);
                }
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to delete graph elements.
    /// </summary>
    class DeleteElementsCommand : ModelCommand<GraphElementModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteElementsCommand"/> class.
        /// </summary>
        public DeleteElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteElementsCommand"/> class.
        /// </summary>
        /// <param name="elementsToDelete">The elements to delete.</param>
        public DeleteElementsCommand(IReadOnlyList<GraphElementModel> elementsToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToDelete)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteElementsCommand"/> class.
        /// </summary>
        /// <param name="elementsToDelete">The elements to delete.</param>
        public DeleteElementsCommand(params GraphElementModel[] elementsToDelete)
            : this((IReadOnlyList<GraphElementModel>)elementsToDelete)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, DeleteElementsCommand command)
        {
            if (!command.Models.Any())
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
                graphModelState.GraphModel.DeleteElements(command.Models);
                var deletedModels = changeScope.ChangeDescription.DeletedModels.ToList();

                if (deletedModels.Any(model => model is VariableDeclarationModel variable && variable.IsInputOrOutput()))
                {
                    foreach (var recursiveSubgraphNode in graphModelState.GraphModel.GetRecursiveSubgraphNodes())
                        recursiveSubgraphNode.Update();
                }

                var selectedModels = deletedModels.Where(selectionState.IsSelected).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.SelectElements(selectedModels, false);
                }

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to start the graph compilation.
    /// </summary>
    class BuildAllEditorCommand : UndoableCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAllEditorCommand"/> class.
        /// </summary>
        public BuildAllEditorCommand()
        {
            UndoString = "Compile Graph";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(BuildAllEditorCommand command)
        {
        }
    }

    /// <summary>
    /// Command to paste elements in the graph.
    /// </summary>
    class PasteSerializedDataCommand : UndoableCommand
    {
        /// <summary>
        /// The delta to apply to the pasted models.
        /// </summary>
        public Vector2 Delta;
        /// <summary>
        /// The data representing the graph element models to paste.
        /// </summary>
        public readonly CopyPasteData Data;

        /// <summary>
        /// The selected group, if any.
        /// </summary>
        public GroupModel SelectedGroup;

        /// <summary>
        /// The operation that triggers this command.
        /// </summary>
        public PasteOperation Operation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteSerializedDataCommand"/> class.
        /// </summary>
        public PasteSerializedDataCommand()
        {
            UndoString = "Paste";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteSerializedDataCommand"/> class.
        /// </summary>
        /// <param name="operation">The operation that triggers this command.</param>
        /// <param name="undoString">The name of the paste operation (Paste, Duplicate, etc.).</param>
        /// <param name="delta">The delta to apply on the pasted elements position.</param>
        /// <param name="data">The elements to paste.</param>
        /// <param name="selectedGroup">The selected group, If any.</param>
        public PasteSerializedDataCommand(PasteOperation operation, string undoString, Vector2 delta, CopyPasteData data, GroupModel selectedGroup = null) : this()
        {
            if (!string.IsNullOrEmpty(undoString))
                UndoString = undoString;

            Delta = delta;
            Data = data;
            SelectedGroup = selectedGroup;
            Operation = operation;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, PasteSerializedDataCommand command)
        {
            if (!command.Data.IsEmpty())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    var undoableStates = selectionHelper.UndoableSelectionStates.Append(graphModelState);
                    undoStateUpdater.SaveStates(undoableStates);
                }

                using (var graphModelStateUpdater = graphModelState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection();
                    }

                    CopyPasteData.PasteSerializedData(command.Operation, command.Delta, graphModelStateUpdater,
                        null, selectionUpdaters.MainUpdateScope, command.Data, graphModelState.GraphModel, command.SelectedGroup);
                }
            }
        }
    }

    /// <summary>
    /// A command to change the graph view position and zoom and optionally change the selection.
    /// </summary>
    class ReframeGraphViewCommand : ICommand
    {
        /// <summary>
        /// The new position.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The new zoom factor.
        /// </summary>
        public Vector3 Scale;
        /// <summary>
        /// The elements to select, in replacement of the current selection.
        /// </summary>
        public List<GraphElementModel> NewSelection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReframeGraphViewCommand" /> class.
        /// </summary>
        public ReframeGraphViewCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReframeGraphViewCommand" /> class.
        /// </summary>
        /// <param name="position">The new position.</param>
        /// <param name="scale">The new zoom factor.</param>
        /// <param name="newSelection">If not null, the elements to select, in replacement of the current selection.
        /// If null, the selection is not changed.</param>
        public ReframeGraphViewCommand(Vector3 position, Vector3 scale, List<GraphElementModel> newSelection = null) : this()
        {
            Position = position;
            Scale = scale;
            NewSelection = newSelection;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="graphModelStateComponent">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, GraphModelStateComponent graphModelStateComponent, SelectionStateComponent selectionState, ReframeGraphViewCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                graphUpdater.Position = command.Position;
                graphUpdater.Scale = command.Scale;

                if (command.NewSelection != null)
                {
                    using (var selectionUpdaters = selectionHelper.UpdateScopes)
                    {
                        foreach (var selectionUpdater in selectionUpdaters)
                        {
                            selectionUpdater.ClearSelection();
                        }
                        selectionUpdaters.MainUpdateScope.SelectElements(command.NewSelection, true);
                    }
                }
            }
        }
    }
}
