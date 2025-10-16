// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to collapse or expand transitions.
    /// </summary>
    [UnityRestricted]
    internal class CollapseTransitionsCommand : ICommand
    {
        /// <summary>
        /// The transition support models to collapse or expand.
        /// </summary>
        public IReadOnlyList<TransitionSupportModel> TransitionSupportModels;

        /// <summary>
        /// The transition models to collapse or expand.
        /// </summary>
        public IReadOnlyList<TransitionModel> TransitionModels;

        /// <summary>
        /// Should we collapse or expand.
        /// </summary>
        public bool IsCollapsed;

        /// <summary>
        /// Whether the collapse status if for state inspectors or transition inspectors.
        /// </summary>
        public bool OnState;

        /// <summary>
        /// Creates a new instance of <see cref="CollapseTransitionsCommand"/>.
        /// </summary>
        /// <param name="transitionModels">The transition models to be collapsed or expanded.</param>
        /// <param name="isCollapsed">Should we collapse or expand.</param>
        /// <param name="onState">Whether the collapse status if for state inspectors or transition inspectors.</param>
        public CollapseTransitionsCommand(IReadOnlyList<TransitionModel> transitionModels, bool isCollapsed, bool onState) :
            this(null, transitionModels, isCollapsed, onState)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="CollapseTransitionsCommand"/>.
        /// </summary>
        /// <param name="transitionSupportModels">The transition support models to be collapsed or expanded.</param>
        /// <param name="transitionModels">The transition models to be collapsed or expanded.</param>
        /// <param name="isCollapsed">Should we collapse or expand.</param>
        /// <param name="onState">Whether the collapse status if for state inspectors or transition inspectors.</param>
        public CollapseTransitionsCommand(IReadOnlyList<TransitionSupportModel> transitionSupportModels, IReadOnlyList<TransitionModel> transitionModels, bool isCollapsed, bool onState)
        {
            TransitionSupportModels = transitionSupportModels;
            TransitionModels = transitionModels;
            IsCollapsed = isCollapsed;
            OnState = onState;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CollapseTransitionsCommand"/>.
        /// </summary>
        /// <param name="transitionModel">The transition model to be collapsed or expanded.</param>
        /// <param name="isCollapsed">Should we collapse or expand.</param>
        /// <param name="onState">Whether the collapse status if for state inspectors or transition inspectors.</param>
        public CollapseTransitionsCommand(TransitionModel transitionModel, bool isCollapsed, bool onState) :
            this(null, new[] { transitionModel }, isCollapsed, onState)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="CollapseTransitionsCommand"/>.
        /// </summary>
        /// <param name="transitionSupportModel">The transition support model to be collapsed or expanded.</param>
        /// <param name="isCollapsed">Should we collapse or expand.</param>
        /// <param name="onState">Whether the collapse status if for state inspectors or transition inspectors.</param>
        public CollapseTransitionsCommand(TransitionSupportModel transitionSupportModel, bool isCollapsed, bool onState) :
            this(new[] { transitionSupportModel }, null, isCollapsed, onState)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="modelInspectorState">The undo state component.</param>
        /// <param name="transitionInspectorState">The state of the blackboard view.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(ModelInspectorStateComponent modelInspectorState, TransitionInspectorStateComponent transitionInspectorState, CollapseTransitionsCommand command)
        {
            if ((command.TransitionSupportModels == null || command.TransitionSupportModels.Count == 0) && (command.TransitionModels == null || command.TransitionModels.Count == 0))
                return;

            using var updater = transitionInspectorState.UpdateScope;
            if (command.TransitionModels != null)
            {
                foreach (var transitionModel in command.TransitionModels)
                {
                    updater.SetTransitionCollapsed(transitionModel, command.IsCollapsed, command.OnState);
                }
            }
            if (command.TransitionSupportModels != null)
            {
                foreach (var transitionSupportModel in command.TransitionSupportModels)
                {
                    updater.SetTransitionSupportCollapsed(transitionSupportModel, command.IsCollapsed, command.OnState);
                }
            }
        }
    }

    /// <summary>
    /// Command to add a transition.
    /// </summary>
    [UnityRestricted]
    internal class AddTransitionCommand : ModelCommand<TransitionSupportModel>
    {
        const string k_UndoStringSingular = "Add Transition";

        /// <summary>
        /// Creates a new instance of <see cref="AddTransitionCommand"/>.
        /// </summary>
        /// <param name="transitionSupportModel">The <see cref="TransitionSupportModel"/> on which to add a new transition.</param>
        public AddTransitionCommand(TransitionSupportModel transitionSupportModel)
            : base(k_UndoStringSingular, k_UndoStringSingular, transitionSupportModel != null ? new[] { transitionSupportModel } : Array.Empty<TransitionSupportModel>()) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AddTransitionCommand command)
        {
            if (command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = command.Models[0].GraphModel.ChangeDescriptionScope)
            {
                var newTransition = command.Models[0].CreateTransition();
                command.Models[0].AddTransition(newTransition);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to remove a transition from a <see cref="TransitionSupportModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class RemoveTransitionCommand : ModelCommand<TransitionSupportModel>
    {
        const string k_UndoStringSingular = "Remove Transition";
        const string k_UndoStringPlural = "Remove Transitions";

        /// <summary>
        /// The Transitions to remove.
        /// </summary>
        public IReadOnlyList<TransitionModel> TransitionsToRemove;

        /// <summary>
        /// Creates a new instance of <see cref="RemoveTransitionCommand"/>.
        /// </summary>
        /// <param name="transitionSupportModel">The <see cref="TransitionSupportModel"/> on which to remove transitions.</param>
        /// <param name="transitionsToRemove">The transitions to remove.</param>
        public RemoveTransitionCommand(TransitionSupportModel transitionSupportModel, IReadOnlyList<TransitionModel> transitionsToRemove)
            : base(transitionsToRemove?.Count == 1 ? k_UndoStringSingular : k_UndoStringPlural, k_UndoStringPlural, new[] { transitionSupportModel })
        {
            TransitionsToRemove = transitionsToRemove;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, RemoveTransitionCommand command)
        {
            if (command.Models.Count == 0 || command.TransitionsToRemove.Count == 0 || command.Models[0].Transitions.Count <= 1)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = command.Models[0].GraphModel.ChangeDescriptionScope)
            {
                var transitionModel = command.Models[0];
                transitionModel.RemoveTransitions(command.TransitionsToRemove);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to remove both transitions support models and transitions from different support models.
    /// </summary>
    [UnityRestricted]
    internal class RemoveTransitionElementsCommand : ModelCommand<TransitionSupportModel>
    {
        static readonly string k_UndoStringSingular = L10n.Tr("Remove Transition");
        static readonly string k_UndoStringPlural = L10n.Tr("Remove Transitions");

        /// <summary>
        /// The transitions to remove, grouped by the support model they belong to.
        /// </summary>
        public IReadOnlyDictionary<TransitionSupportModel, List<TransitionModel>> TransitionsToRemove;

        /// <summary>
        /// Creates a new instance of <see cref="RemoveTransitionElementsCommand"/>.
        /// </summary>
        /// <param name="transitionSupportModels">The transitions support models to be removed.</param>
        /// <param name="transitionsToRemove">The transitions to remove, grouped by the support model they belong to.</param>
        public RemoveTransitionElementsCommand(IReadOnlyList<TransitionSupportModel> transitionSupportModels, IReadOnlyDictionary<TransitionSupportModel, List<TransitionModel>> transitionsToRemove)
            : base(transitionsToRemove?.Count + transitionSupportModels?.Count == 1 ? k_UndoStringSingular : k_UndoStringPlural, k_UndoStringPlural, transitionSupportModels)
        {
            TransitionsToRemove = transitionsToRemove;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, RemoveTransitionElementsCommand command)
        {
            if (command.Models.Count == 0 && command.TransitionsToRemove.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                List<GraphElementModel> supportModels = new List<GraphElementModel>();
                foreach (var kv in command.TransitionsToRemove)
                {
                    if (kv.Key.Transitions.Count > kv.Value.Count)
                    {
                        kv.Key.RemoveTransitions(kv.Value);
                    }
                    else
                    {
                        supportModels.Add(kv.Key);
                    }
                }
                supportModels.AddRange(command.Models);
                if (supportModels.Count > 0)
                    graphModelState.GraphModel.DeleteElements(supportModels);

                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to Move transitions in a <see cref="TransitionSupportModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class MoveTransitionCommand : ModelCommand<TransitionSupportModel>
    {
        const string k_UndoStringSingular = "Move Transition";
        const string k_UndoStringPlural = "Move Transitions";

        /// <summary>
        /// The transitions to move.
        /// </summary>
        public IReadOnlyList<TransitionModel> TransitionsToMove;

        /// <summary>
        /// The index at which the transition will be moved. This is the index before the transitions are moved.
        /// </summary>
        public int Index;

        /// <summary>
        /// Creates a new instance of <see cref="MoveTransitionCommand"/>.
        /// </summary>
        /// <param name="transitionSupportModel">The transition support model in which the transitions are.</param>
        /// <param name="transitionsToMove">The transitions to move.</param>
        /// <param name="index">The index at which the transition will be moved. This is the index before the transitions are moved.</param>
        public MoveTransitionCommand(TransitionSupportModel transitionSupportModel, IReadOnlyList<TransitionModel> transitionsToMove, int index)
            : base(transitionsToMove?.Count == 1 ? k_UndoStringSingular : k_UndoStringPlural, k_UndoStringPlural, new[] { transitionSupportModel })
        {
            TransitionsToMove = transitionsToMove;
            Index = index;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, MoveTransitionCommand command)
        {
            if (command.Models.Count == 0 || command.TransitionsToMove.Count == 0 || command.Models[0].Transitions.Count <= 1)
                return;

            if (command.Index < 0 || command.Index >= command.Models[0].Transitions.Count)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                var transitionModel = command.Models[0];
                transitionModel.ReorderTransitions(command.TransitionsToMove, command.Index);
                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to add a <see cref="ConditionModel"/> to a <see cref="GroupConditionModel"/>.
    /// </summary>
    /// <remarks>
    /// 'AddConditionCommand' is a command that adds a <see cref="ConditionModel"/> to a <see cref="GroupConditionModel"/>. This operation allows you to add a condition
    /// to a group, which enables more complex condition logic and flexible logic management within a transition model.
    /// </remarks>
    [UnityRestricted]
    internal class AddConditionCommand : ModelCommand<GroupConditionModel>
    {
        const string k_UndoStringSingular = "Add Condition";

        /// <summary>
        /// The condition to add.
        /// </summary>
        public ConditionModel NewConditionModel;

        /// <summary>
        /// The index at which the condition will be added.
        /// </summary>
        public int Index;

        /// <summary>
        /// Creates a new instance of <see cref="AddConditionCommand"/>.
        /// </summary>
        /// <param name="groupConditionModel">The group condition that will contain the new condition.</param>
        /// <param name="newCondition">The condition to add.</param>
        /// <param name="index">The index at which the condition will be added.</param>
        public AddConditionCommand(GroupConditionModel groupConditionModel, ConditionModel newCondition, int index)
            : base(k_UndoStringSingular, k_UndoStringSingular, new[] { groupConditionModel })
        {
            NewConditionModel = newCondition;
            Index = index;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AddConditionCommand command)
        {
            if (command.Models.Count != 1 || command.NewConditionModel == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.Models[0].InsertCondition(command.NewConditionModel, command.Index);

                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to delete conditions.
    /// </summary>
    [UnityRestricted]
    internal class DeleteConditionsCommand : ModelCommand<ConditionModel>
    {
        const string k_UndoStringSingular = "Delete Condition";
        const string k_UndoStringPlural = "Delete Conditions";

        /// <summary>
        /// Creates a new instance of <see cref="DeleteConditionsCommand"/>.
        /// </summary>
        /// <param name="conditionModels">The conditions that will be deleted.</param>
        public DeleteConditionsCommand(IReadOnlyList<ConditionModel> conditionModels)
            : base(conditionModels?.Count > 1 ? k_UndoStringPlural : k_UndoStringSingular, k_UndoStringPlural, conditionModels)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, DeleteConditionsCommand command)
        {
            if (command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var conditionModel in command.Models)
                {
                    var parentGroup = conditionModel.Parent;
                    ConditionModel current = parentGroup;
                    bool parentDeletedToo = false;
                    while (current != null)
                    {
                        if (command.Models.Contains(current))
                        {
                            parentDeletedToo = true;
                            break;
                        }

                        current = current.Parent;
                    }

                    if (parentDeletedToo)
                        continue;

                    parentGroup.RemoveCondition(conditionModel);

                    updater.MarkChanged(parentGroup.Guid, ChangeHint.Data);
                }
                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Duplicate conditions.
    /// </summary>
    [UnityRestricted]
    internal class DuplicateConditionsCommand : ModelCommand<ConditionModel>
    {
        const string k_UndoStringSingular = "Duplicate Condition";
        const string k_UndoStringPlural = "Duplicate Conditions";

        /// <summary>
        /// Creates a new instance of <see cref="DuplicateConditionsCommand"/>.
        /// </summary>
        /// <param name="conditionModels">The conditions to be duplicated.</param>
        public DuplicateConditionsCommand(IReadOnlyList<ConditionModel> conditionModels)
            : base(conditionModels?.Count > 1 ? k_UndoStringPlural : k_UndoStringSingular, k_UndoStringPlural, conditionModels)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, DuplicateConditionsCommand command)
        {
            if (command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var conditionModel in command.Models)
                {
                    var parentGroup = conditionModel.Parent;

                    parentGroup.InsertCondition(conditionModel.Clone(), conditionModel.IndexInParent + 1);
                }
                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to move conditions.
    /// </summary>
    [UnityRestricted]
    internal class MoveConditionCommand : ModelCommand<GroupConditionModel>
    {
        const string k_UndoStringSingular = "Move Condition";
        const string k_UndoStringPlural = "Move Conditions";

        /// <summary>
        /// The condition to be moved. They might be in different groups.
        /// </summary>
        public List<ConditionModel> InsertedConditionModels;

        /// <summary>
        /// The index to which the conditions will be added.
        /// </summary>
        public int Index;

        /// <summary>
        /// Creates a new instance of <see cref="MoveConditionCommand"/>.
        /// </summary>
        /// <param name="groupConditionModel">The target group condition.</param>
        /// <param name="conditionModels">The condition to be moved. They might be in different groups.</param>
        /// <param name="index">The index to which the conditions will be added.</param>
        public MoveConditionCommand(GroupConditionModel groupConditionModel, List<ConditionModel> conditionModels, int index)
            : base(conditionModels?.Count == 1 ? k_UndoStringSingular : k_UndoStringPlural, k_UndoStringPlural, new[] { groupConditionModel })
        {
            InsertedConditionModels = conditionModels;
            Index = index;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, MoveConditionCommand command)
        {
            if (command.Models.Count == 0 || command.InsertedConditionModels.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.Models[0].MoveConditions(command.InsertedConditionModels, command.Index);

                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to set the operation of a <see cref="GroupConditionModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class SetGroupConditionOperationCommand : ModelCommand<GroupConditionModel>
    {
        const string k_UndoStringSingular = "Set Condition Operation";

        /// <summary>
        /// The new operation to apply.
        /// </summary>
        public GroupConditionModel.Operation NewOperation;

        /// <summary>
        /// Creates a new instance of <see cref="SetGroupConditionOperationCommand"/>.
        /// </summary>
        /// <param name="conditionModel">The target condition.</param>
        /// <param name="operation">The new operation to apply.</param>
        public SetGroupConditionOperationCommand(GroupConditionModel conditionModel, GroupConditionModel.Operation operation)
            : base(k_UndoStringSingular, k_UndoStringSingular, conditionModel != null ? new[] { conditionModel } : Array.Empty<GroupConditionModel>())
        {
            NewOperation = operation;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetGroupConditionOperationCommand command)
        {
            if (command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var model in command.Models)
                {
                    model.GroupOperation = command.NewOperation;
                    updater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }
}
