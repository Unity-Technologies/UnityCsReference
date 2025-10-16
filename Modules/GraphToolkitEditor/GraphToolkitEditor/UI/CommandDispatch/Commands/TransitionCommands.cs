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
    /// Command to create a single state transition.
    /// </summary>
    [UnityRestricted]
    internal class CreateSingleStateTransitionSupportCommand : UndoableCommand
    {
        /// <summary>
        /// The state machine to create the transition in.
        /// </summary>
        public readonly GraphModel StateMachine;

        /// <summary>
        /// The state to create the transition on.
        /// </summary>
        public readonly StateModel State;

        /// <summary>
        /// The type of transition to create. Must be one of the following:
        /// - <see cref="TransitionSupportKind.Local"/>
        /// - <see cref="TransitionSupportKind.Self"/>
        /// - <see cref="TransitionSupportKind.OnEnter"/>
        /// </summary>
        public readonly TransitionSupportKind Kind;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSingleStateTransitionSupportCommand"/> class.
        /// </summary>
        /// <param name="stateMachine">The state machine to create the transition in.</param>
        /// <param name="state">The state to create the transition on.</param>
        /// <param name="kind">The type of transition to create.</param>
        public CreateSingleStateTransitionSupportCommand(GraphModel stateMachine, StateModel state, TransitionSupportKind kind)
        {
            StateMachine = stateMachine;
            State = state;
            Kind = kind;
            UndoString = kind switch
            {
                TransitionSupportKind.Local => "Create Local Transition",
                TransitionSupportKind.OnEnter => "Create On Enter Transition",
                TransitionSupportKind.Self => "Create Self Transition",
                _ => throw new ArgumentException("Invalid transition type", nameof(kind))
            };
        }

        /// <summary>
        /// Default command handler for <see cref="CreateSingleStateTransitionSupportCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to execute.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateSingleStateTransitionSupportCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = new IUndoableStateComponent[] { graphModelState, selectionState };
                undoStateUpdater.SaveStates(undoableStates);
            }

            TransitionSupportModel existingTargetTransitionSupport = null;
            foreach (var wire in command.State.GetInPort().GetConnectedWires())
            {
                if (wire is TransitionSupportModel transitionSupportModel && transitionSupportModel.TransitionSupportKind == command.Kind)
                {
                    existingTargetTransitionSupport = transitionSupportModel;
                    break;
                }
            }

            GraphElementModel modelToSelect;
            using (var stateUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                if (existingTargetTransitionSupport != null)
                {
                    var newTransition = existingTargetTransitionSupport.CreateTransition();
                    existingTargetTransitionSupport.AddTransition(newTransition);
                    modelToSelect = existingTargetTransitionSupport;
                }
                else
                {
                    var newTransitionSupport = command.StateMachine.CreateSingleStateTransitionSupport(command.State, command.Kind);
                    modelToSelect = newTransitionSupport;
                }

                stateUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (modelToSelect != null)
            {
                using var selectionUpdater = selectionState.UpdateScope;
                selectionUpdater.ClearSelection();
                selectionUpdater.SelectElement(modelToSelect, true);
            }
        }
    }

    /// <summary>
    /// Command to create a transition between two states.
    /// </summary>
    [UnityRestricted]
    internal class CreateTransitionSupportCommand : UndoableCommand
    {
        /// <summary>
        /// The state model from which the transition originates.
        /// </summary>
        public readonly StateModel FromStateModel;

        /// <summary>
        /// The anchor side on the state from which the transition originates.
        /// </summary>
        public readonly AnchorSide FromStateAnchorSide;

        /// <summary>
        /// The anchor offset on the state from which the transition originates.
        /// </summary>
        public readonly float FromStateAnchorOffset;

        /// <summary>
        /// The state model to which the transition goes.
        /// </summary>
        public readonly StateModel ToStateModel;

        /// <summary>
        /// The anchor side on the state to which the transition goes.
        /// </summary>
        public readonly AnchorSide ToStateAnchorSide;

        /// <summary>
        /// The anchor offset on the state to which the transition goes.
        /// </summary>
        public readonly float ToStateAnchorOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTransitionSupportCommand"/> class.
        /// </summary>
        /// <param name="fromStateModel">The state model from which the transition originates.</param>
        /// <param name="fromStateAnchorSide">The anchor side on the state from which the transition originates.</param>
        /// <param name="fromStateAnchorOffset">The anchor offset on the state from which the transition originates.</param>
        /// <param name="toStateModel">The state model to which the transition goes.</param>
        /// <param name="toStateAnchorSide">The anchor side on the state to which the transition goes.</param>
        /// <param name="toStateAnchorOffset">The anchor offset on the state to which the transition goes.</param>
        public CreateTransitionSupportCommand(
            StateModel fromStateModel, AnchorSide fromStateAnchorSide, float fromStateAnchorOffset,
            StateModel toStateModel, AnchorSide toStateAnchorSide, float toStateAnchorOffset)
        {
            FromStateModel = fromStateModel;
            FromStateAnchorSide = fromStateAnchorSide;
            FromStateAnchorOffset = fromStateAnchorOffset;
            ToStateModel = toStateModel;
            ToStateAnchorSide = toStateAnchorSide;
            ToStateAnchorOffset = toStateAnchorOffset;
            UndoString = "Create Transition";
        }

        /// <summary>
        /// Default command handler for <see cref="CreateTransitionSupportCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to execute.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateTransitionSupportCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = new IUndoableStateComponent[] { graphModelState, selectionState };
                undoStateUpdater.SaveStates(undoableStates);
            }

            using (var stateUpdater = graphModelState.UpdateScope)
            using (var selectionUpdater = selectionState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                var newTransition = graphModelState.GraphModel.CreateTransitionSupport(command.ToStateModel.GetInPort(), command.ToStateAnchorSide, command.ToStateAnchorOffset,
                    command.FromStateModel.GetOutPort(), command.FromStateAnchorSide, command.FromStateAnchorOffset,
                    TransitionSupportKind.StateToState);

                if (newTransition != null)
                {
                    selectionUpdater.ClearSelection();
                    selectionUpdater.SelectElement(newTransition, true);
                }

                stateUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to create a state node from a dragged transition. If the dragged transition is a ghost wire, a new transition will also be created.
    /// </summary>
    [UnityRestricted]
    internal class CreateStateFromTransitionCommand : UndoableCommand
    {
        /// <summary>
        /// The guid to assign to the newly created state node.
        /// </summary>
        public Hash128 Guid;

        /// <summary>
        /// The position where to create the state node.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// The <see cref="GraphNodeModelLibraryItem"/> representing the state node to create.
        /// </summary>
        public GraphNodeModelLibraryItem LibraryItem;

        /// <summary>
        /// The transition that was dragged out on which to connect the newly created state node.
        /// </summary>
        public readonly TransitionSupportModel DraggedTransitionModel;

        /// <summary>
        /// The state model from which the transition was dragged out.
        /// </summary>
        public readonly StateModel FromStateModel;

        /// <summary>
        /// The anchor side from which the transition was dragged out.
        /// </summary>
        public readonly AnchorSide FromStateAnchorSide;

        /// <summary>
        /// The anchor offset from which the transition was dragged out.
        /// </summary>
        public readonly float FromStateAnchorOffset;

        /// <summary>
        /// The anchor side on the newly created state node.
        /// </summary>
        public readonly AnchorSide ToStateAnchorSide;

        /// <summary>
        /// The anchor offset on the newly created state node.
        /// </summary>
        public readonly float ToStateAnchorOffset;

        /// <summary>
        /// Initializes a new <see cref="CreateStateFromTransitionCommand"/>.
        /// </summary>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="draggedTransitionModel">The transition that was dragged out on which to connect the newly created state node.</param>
        /// <param name="fromStateModel">The state model from which the transition was dragged out.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="fromStateAnchorSide">The anchor side from which the transition was dragged out.</param>
        /// <param name="fromStateAnchorOffset">The anchor offset from which the transition was dragged out.</param>
        /// <param name="toStateAnchorSide">The anchor side on the newly created state node.</param>
        /// <param name="toStateAnchorOffset">The anchor offset on the newly created state node.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        public CreateStateFromTransitionCommand(
            GraphNodeModelLibraryItem item, TransitionSupportModel draggedTransitionModel, StateModel fromStateModel,
            Vector2 position,
            AnchorSide fromStateAnchorSide, float fromStateAnchorOffset,
            AnchorSide toStateAnchorSide, float toStateAnchorOffset,
            Hash128 guid = default)
        {
            LibraryItem = item;
            DraggedTransitionModel = draggedTransitionModel;
            FromStateModel = fromStateModel;
            Position = position;
            FromStateAnchorSide = fromStateAnchorSide;
            FromStateAnchorOffset = fromStateAnchorOffset;
            ToStateAnchorSide = toStateAnchorSide;
            ToStateAnchorOffset = toStateAnchorOffset;
            Guid = guid;
            UndoString = "Create State";
        }

        /// <summary>
        /// Default command handler for <see cref="CreateStateFromTransitionCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="autoPlacementState">The auto-placement state.</param>
        /// <param name="command">The command to handle.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, AutoPlacementStateComponent autoPlacementState, CreateStateFromTransitionCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                undoStateUpdater.SaveStates(selectionHelper.SelectionStates);

                undoStateUpdater.SaveStates(graphModelState);
                undoStateUpdater.SaveStates(autoPlacementState);
            }

            using (var autoPlacementUpdater = autoPlacementState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var selectionUpdater = selectionState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                var graphModel = graphModelState.GraphModel;

                var createdModel = command.LibraryItem.CreateElement(new GraphNodeCreationData(graphModel, command.Position, guid: command.Guid));
                selectionUpdater.ClearSelection();
                selectionUpdater.SelectElement(createdModel, true);

                if (createdModel is not StateModel newStateModel)
                    return;

                var wireSide = WireSide.From;

                if (command.DraggedTransitionModel is IGhostWireModel)
                {
                    wireSide = WireSide.To;
                    graphModelState.GraphModel.CreateTransitionSupport(newStateModel.GetInPort(), command.ToStateAnchorSide, command.ToStateAnchorOffset,
                        command.FromStateModel.GetOutPort(), command.FromStateAnchorSide, command.FromStateAnchorOffset, TransitionSupportKind.StateToState);
                }
                else
                {
                    if (command.DraggedTransitionModel.FromPort == command.FromStateModel.GetOutPort())
                    {
                        wireSide = WireSide.To;
                        command.DraggedTransitionModel.ToPort = newStateModel.GetInPort();
                        command.DraggedTransitionModel.SetToAnchor(command.ToStateAnchorSide, command.ToStateAnchorOffset);
                    }
                    else
                    {
                        wireSide = WireSide.From;
                        command.DraggedTransitionModel.FromPort = newStateModel.GetOutPort();
                        command.DraggedTransitionModel.SetFromAnchor(command.ToStateAnchorSide, command.ToStateAnchorOffset);
                    }
                }

                autoPlacementUpdater.MarkModelToRepositionAtCreation((newStateModel, command.DraggedTransitionModel, wireSide), AutoPlacementStateComponent.Changeset.RepositionType.FromWire);
                graphUpdater.MarkForRename(newStateModel);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to move the anchor of a transition, either on the current state or on the target state.
    /// </summary>
    [UnityRestricted]
    internal class MoveTransitionSupportCommand : UndoableCommand
    {
        /// <summary>
        /// The transition model to update.
        /// </summary>
        public readonly TransitionSupportModel TransitionModel;

        /// <summary>
        /// The anchor offset to set on the transition.
        /// </summary>
        public readonly float AnchorOffset;

        /// <summary>
        /// The anchor side to set on the transition.
        /// </summary>
        public readonly AnchorSide AnchorSide;

        /// <summary>
        /// The side of the transition to update.
        /// </summary>
        public readonly FromTo Side;

        /// <summary>
        /// The state onto which the transition should be attached. If null, the transition will stay attached to its current states.
        /// </summary>
        public readonly StateModel TargetStateModel;

        [UnityRestricted]
        internal enum FromTo
        {
            FromState,
            ToState
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveTransitionSupportCommand"/> class.
        /// </summary>
        /// <param name="transitionModel">The transition model to update.</param>
        /// <param name="anchorSide">The anchor side to set on the transition.</param>
        /// <param name="anchorOffset">The anchor offset to set on the transition.</param>
        /// <param name="fromTo">Which side of the transition should be updated.</param>
        /// <param name="targetStateModel">The state onto which the transition should be attached. If null, the transition will stay attached to its current states.</param>
        public MoveTransitionSupportCommand(TransitionSupportModel transitionModel, AnchorSide anchorSide, float anchorOffset,
                                            FromTo fromTo, StateModel targetStateModel = null)
        {
            TransitionModel = transitionModel;
            AnchorSide = anchorSide;
            AnchorOffset = anchorOffset;
            Side = fromTo;
            TargetStateModel = targetStateModel;
            UndoString = "Move Transition";
        }

        /// <summary>
        /// Default command handler for <see cref="MoveTransitionSupportCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to execute.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, MoveTransitionSupportCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = new IUndoableStateComponent[] { graphModelState, selectionState };
                undoStateUpdater.SaveStates(undoableStates);
            }

            using (var stateUpdater = graphModelState.UpdateScope)
            using (var selectionUpdater = selectionState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                if (command.TargetStateModel != null)
                {
                    switch (command.Side)
                    {
                        case FromTo.FromState when command.TransitionModel.FromPort != command.TargetStateModel.GetOutPort():
                            command.TransitionModel.FromPort = command.TargetStateModel.GetOutPort();
                            break;
                        case FromTo.ToState when command.TransitionModel.ToPort != command.TargetStateModel.GetInPort():
                            command.TransitionModel.ToPort = command.TargetStateModel.GetInPort();
                            break;
                    }
                }

                if (command.Side == FromTo.FromState)
                    command.TransitionModel.SetFromAnchor(command.AnchorSide, command.AnchorOffset);
                else
                    command.TransitionModel.SetToAnchor(command.AnchorSide, command.AnchorOffset);

                selectionUpdater.ClearSelection();
                selectionUpdater.SelectElement(command.TransitionModel, true);

                stateUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to paste single state transition supports on a state.
    /// </summary>
    [UnityRestricted]
    internal class PasteSingleStateTransitionSupportsCommand : UndoableCommand
    {
        const string k_SingularUndoString = "Paste Transition";
        const string k_PluralUndoString = "Paste Transitions";

        /// <summary>
        /// The state machine to create the transition supports in.
        /// </summary>
        public readonly GraphModel StateMachine;

        /// <summary>
        /// The state to create the transition supports on.
        /// </summary>
        public readonly StateModel State;

        /// <summary>
        /// The transition supports to paste.
        /// </summary>
        public readonly IReadOnlyList<TransitionSupportModel> PastedTransitionSupportModels;

        /// <summary>
        /// Whether to paste the transition from the source transition supports additively or to replace the existing transitions.
        /// </summary>
        public readonly bool IsAdditivePaste;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteSingleStateTransitionSupportsCommand"/> class.
        /// </summary>
        /// <param name="stateMachine">The state machine to create the transition supports in.</param>
        /// <param name="state">The state to create the transition supports on.</param>
        /// <param name="transitionSupportModels">The transition supports to paste.</param>
        /// <param name="isAdditivePaste">Whether to paste the transition from the source transition supports additively or to replace the existing transitions.</param>
        public PasteSingleStateTransitionSupportsCommand(GraphModel stateMachine, StateModel state, IReadOnlyList<TransitionSupportModel> transitionSupportModels, bool isAdditivePaste)
        {
            StateMachine = stateMachine;
            State = state;
            PastedTransitionSupportModels = transitionSupportModels;
            IsAdditivePaste = isAdditivePaste;
            UndoString = PastedTransitionSupportModels?.Count > 1 ? k_PluralUndoString : k_SingularUndoString;
        }

        /// <summary>
        /// Default command handler for <see cref="CreateSingleStateTransitionSupportCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to execute.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, PasteSingleStateTransitionSupportsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = new IUndoableStateComponent[] { graphModelState, selectionState };
                undoStateUpdater.SaveStates(undoableStates);
            }

            TransitionSupportModel existingTargetTransition = null;
            var modelsToSelect = new List<GraphElementModel>();
            var connectedWires = command.State.GetInPort().GetConnectedWires();

            using (var stateUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var pastedTransitionSupport in command.PastedTransitionSupportModels)
                {
                    var kind = pastedTransitionSupport.TransitionSupportKind;
                    if (kind == TransitionSupportKind.StateToState)
                        kind = TransitionSupportKind.Self;

                    foreach (var wire in connectedWires)
                    {
                        if (wire is TransitionSupportModel baseTransitionModel && baseTransitionModel.TransitionSupportKind == kind)
                        {
                            existingTargetTransition = baseTransitionModel;
                            break;
                        }
                    }

                    if (existingTargetTransition != null)
                    {
                        if (command.IsAdditivePaste)
                        {
                            existingTargetTransition.CopyTransitions(pastedTransitionSupport);
                        }
                        else
                        {
                            existingTargetTransition.ReplaceTransitions(pastedTransitionSupport);
                        }

                        modelsToSelect.Add(existingTargetTransition);
                    }
                    else
                    {
                        var newTransition = command.StateMachine.CreateSingleStateTransitionSupport(command.State, kind);
                        newTransition.ReplaceTransitions(pastedTransitionSupport);
                        modelsToSelect.Add(newTransition);
                    }
                }

                stateUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (modelsToSelect.Count > 0)
            {
                using var selectionUpdater = selectionState.UpdateScope;
                selectionUpdater.ClearSelection();
                selectionUpdater.SelectElements(modelsToSelect, true);
            }
        }
    }

    /// <summary>
    /// Command to paste transitions from a transition support model to multiple transition support models.
    /// </summary>
    [UnityRestricted]
    internal class PasteTransitionSupportsCommand : UndoableCommand
    {
        /// <summary>
        /// The source transition support models to copy transitions from.
        /// </summary>
        public readonly IReadOnlyList<TransitionSupportModel> SourceTransitionSupportModels;

        /// <summary>
        /// The destination transition support models to paste transitions to.
        /// </summary>
        public readonly IReadOnlyList<TransitionSupportModel> DestinationTransitionSupportModels;

        /// <summary>
        /// Whether to paste the transition from the source transition supports additively or to replace the existing transitions.
        /// </summary>
        public readonly bool AdditivePaste;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteTransitionSupportsCommand"/> class.
        /// </summary>
        /// <param name="undoString">The string that should appear in the Edit/Undo menu after this command is executed.</param>
        /// <param name="destinationTransitionSupportModels">The destination transition support models to paste transitions to.</param>
        /// <param name="sourceTransitionSupportModels">The source transition support models to copy transitions from.</param>
        /// <param name="additivePaste">Whether to paste the transition from the source transition supports additively or to replace the existing transitions.</param>
        public PasteTransitionSupportsCommand(string undoString, IReadOnlyList<TransitionSupportModel> destinationTransitionSupportModels, IReadOnlyList<TransitionSupportModel> sourceTransitionSupportModels, bool additivePaste)
        {
            UndoString = !string.IsNullOrEmpty(undoString) ? undoString : "Paste";

            DestinationTransitionSupportModels = destinationTransitionSupportModels;
            SourceTransitionSupportModels = sourceTransitionSupportModels;
            AdditivePaste = additivePaste;
        }

        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, PasteTransitionSupportsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphModelStateUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var destination in command.DestinationTransitionSupportModels)
                {
                    foreach (var source in command.SourceTransitionSupportModels)
                    {
                        if (command.AdditivePaste)
                            destination.CopyTransitions(source);
                        else
                            destination.ReplaceTransitions(source);
                    }
                }

                graphModelStateUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
