// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to create a new blackboard group
    /// </summary>
    [UnityRestricted]
    internal class BlackboardGroupCreateCommand : UndoableCommand
    {
        const string k_UndoString = "Create Group";

        /// <summary>
        /// The group in which the new group will be added.
        /// </summary>
        public GroupModel ContainingGroup;

        /// <summary>
        /// The variable in the <see cref="ContainingGroup"/> after which the new group should be inserted.
        /// </summary>
        public readonly IGroupItemModel InsertAfter;

        /// <summary>
        /// The title of the new group.
        /// </summary>
        public string Title;

        /// <summary>
        /// Items that are added in the new group.
        /// </summary>
        public IReadOnlyList<IGroupItemModel> GroupItemModels;

        BlackboardGroupCreateCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Creates a instance of a <see cref="BlackboardGroupCreateCommand"/>.
        /// </summary>
        /// <param name="containingGroup">The group in which the new group will be added. Must be non null.</param>
        /// <param name="insertAfter">The variable in the <see cref="ContainingGroup"/> after which the new group should be inserted.
        /// If null will add at the beginning of the group.</param>
        /// <param name="title">The title of the new group.</param>
        /// <param name="groupItemModels">Items that are added in the new group.</param>
        public BlackboardGroupCreateCommand(GroupModel containingGroup, IGroupItemModel insertAfter = null, string title = null,
                                            IReadOnlyList<IGroupItemModel> groupItemModels = null) : this()
        {
            ContainingGroup = containingGroup;
            InsertAfter = insertAfter;
            Title = title;
            GroupItemModels = groupItemModels;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="blackboardViewState">The state of the blackboard view.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, BlackboardViewStateComponent blackboardViewState,
            GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BlackboardGroupCreateCommand command)
        {
            using var graphUpdater = graphModelState.UpdateScope;
            using var changeScope = graphModelState.GraphModel.ChangeDescriptionScope;

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using var selectionUpdaters = selectionHelper.UpdateScopes;

            if (command.ContainingGroup == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
                undoStateUpdater.SaveState(blackboardViewState);
                undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
            }

            var title = string.IsNullOrEmpty(command.Title) ? "New Group" : command.Title.Trim();

            var existingNames = command.ContainingGroup.Items.OfType<IHasTitle>().Select(t => t.Title).ToArray();
            title = ObjectNames.GetUniqueName(existingNames, title);

            foreach (var selectionUpdater in selectionUpdaters)
            {
                selectionUpdater.ClearSelection();
            }

            GroupModel newGroup = graphModelState.GraphModel.CreateGroup(title, command.GroupItemModels);

            int index = command.ContainingGroup.Items.IndexOf(command.InsertAfter);
            command.ContainingGroup.InsertItem(newGroup, index < 0 ? int.MaxValue : index + 1);

            graphModelState.GraphModel.UpdateSubGraphs();

            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetGroupModelExpanded(command.ContainingGroup, true);
            }

            graphUpdater.MarkForRename(newGroup);
            graphUpdater.MarkUpdated(changeScope.ChangeDescription);

            selectionUpdaters.MainUpdateScope.SelectElement(newGroup, true);

            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                var current = newGroup.ParentGroup;
                while (current != null)
                {
                    bbUpdater.SetGroupModelExpanded(current, true);
                    current = current.ParentGroup;
                }
            }
        }
    }

    /// <summary>
    /// Command to expand or collapse a variable in the blackboard.
    /// </summary>
    [UnityRestricted]
    internal class ExpandVariableDeclarationCommand : ModelCommand<VariableDeclarationModelBase>
    {
        const string k_UndoStringSingular = "Expand Blackboard Variable";
        const string k_UndoStringPlural = "Expand Blackboard Variables";

        /// <summary>
        /// The variable to expand or collapse.
        /// </summary>
        public VariableDeclarationModelBase VariableDeclarationModel;
        /// <summary>
        /// True if the variable should be expanded, false if it should be collapsed.
        /// </summary>
        public bool Expand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandVariableDeclarationCommand"/> class.
        /// </summary>
        public ExpandVariableDeclarationCommand(VariableDeclarationModelBase variableDeclarationModel, bool expand)
            : base(k_UndoStringSingular, k_UndoStringPlural, new[] { variableDeclarationModel })
        {
            VariableDeclarationModel = variableDeclarationModel;
            Expand = expand;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state.</param>
        /// <param name="blackboardViewState">The blackboard state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, BlackboardViewStateComponent blackboardViewState, ExpandVariableDeclarationCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(blackboardViewState);
            }

            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetVariableDeclarationModelExpanded(command.VariableDeclarationModel, command.Expand);
            }
        }
    }

    /// <summary>
    /// Command to expand or collapse a variable group in the blackboard.
    /// </summary>
    [UnityRestricted]
    internal class ExpandVariableGroupCommand : ModelCommand<GroupModelBase>
    {
        const string k_UndoStringSingular = "Expand Blackboard Variable Group";
        const string k_UndoStringPlural = "Expand Blackboard Variable Groups";


        ///// <summary>
        ///// The variable group to expand or collapse.
        ///// </summary>
        //public GroupModelBase GroupModel;
        /// <summary>
        /// True if the variable group should be expanded, false if it should be collapsed.
        /// </summary>
        public bool Expand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandVariableGroupCommand"/> class.
        /// </summary>
        public ExpandVariableGroupCommand(GroupModelBase groupModel, bool expand)
            : base(k_UndoStringSingular, k_UndoStringPlural, new[] { groupModel })
        {
            //GroupModel = groupModel;
            Expand = expand;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state.</param>
        /// <param name="blackboardViewState">The blackboard state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, BlackboardViewStateComponent blackboardViewState, ExpandVariableGroupCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(blackboardViewState);
            }
            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetGroupModelExpanded(command.Models, command.Expand);
            }
        }
    }

    /// <summary>
    /// Command to reorder group items.
    /// </summary>
    [UnityRestricted]
    internal class ReorderGroupItemsCommand : UndoableCommand
    {
        /// <summary>
        /// The group items to move.
        /// </summary>
        public readonly IReadOnlyList<IGroupItemModel> GroupItemModels;
        /// <summary>
        /// The variable after which the moved group items should be inserted.
        /// </summary>
        public readonly IGroupItemModel InsertAfter;

        /// <summary>
        /// The group in which the group items will be added.
        /// </summary>
        public readonly GroupModel Group;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGroupItemsCommand"/> class.
        /// </summary>
        public ReorderGroupItemsCommand()
        {
            UndoString = "Reorder Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGroupItemsCommand"/> class.
        /// </summary>
        /// <param name="group">The group in which the group items are reordered. Must be non null.</param>
        /// <param name="insertAfter">The item after which the moved group items should be inserted.</param>
        /// <param name="groupItemModels">The group items to move.</param>
        public ReorderGroupItemsCommand(GroupModel group, IGroupItemModel insertAfter,
                                        IReadOnlyList<IGroupItemModel> groupItemModels)
            : this()
        {
            GroupItemModels = groupItemModels;
            InsertAfter = insertAfter;
            Group = group;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGroupItemsCommand"/> class.
        /// </summary>
        /// <param name="group">The group in which the group items are reordered. Must be non null.</param>
        /// <param name="insertAfter">The group item after which the moved group items should be inserted.</param>
        /// <param name="groupItemModels">The group items to move.</param>
        public ReorderGroupItemsCommand(GroupModel group, IGroupItemModel insertAfter,
                                        params IGroupItemModel[] groupItemModels)
            : this(group, insertAfter, (IReadOnlyList<IGroupItemModel>)groupItemModels)
        {
        }

        // Copies the original list of Group Items. If the items do not belong in the same section as the target group, duplicate groups and convert variables.
        // So that the resulting copy contains only elements from the target group section.
        // return whether at least one variable was converted.
        bool TransferOrCopyGroupItemHierarchy(GraphModel graphModel, IReadOnlyList<IGroupItemModel> original, List<GroupModel> duplicatedGroups, List<IGroupItemModel> copy)
        {
            bool duplicated = false;
            var sectionName = Group.GetSection().Title;
            foreach (var item in original.ToList()) // duplicated originals list as it might be modified when removing a variable
            {
                if (item is GroupModel group)
                {
                    // If the section is the target section simply add the group.
                    if (item.GetSection() == Group.GetSection())
                        copy.Add(group);
                    else
                    {
                        // if the section is different recursively call this method on the content of the group.
                        List<IGroupItemModel> listCopy = new List<IGroupItemModel>();
                        if (!TransferOrCopyGroupItemHierarchy(graphModel, group.Items, duplicatedGroups, listCopy))
                            copy.Add(group);
                        else
                        {
                            // If the group contains at least one variable : create a new group to hold the new items.
                            var newGroup = graphModel.CreateGroup(group.Title, listCopy);
                            copy.Add(newGroup);
                            duplicatedGroups.Add(group);
                            duplicated = true;
                        }
                    }
                }
                else if (item is VariableDeclarationModelBase variable)
                {
                    // If the section is the target section simply add the variable.
                    if (item.GetSection() == Group.GetSection())
                        copy.Add(variable);
                    else if (graphModel.CanConvertVariable(variable, sectionName))
                    {
                        graphModel.DeleteVariableDeclarations(new[] {variable});

                        //If the variable can be converted to the new section, convert the variable then mark the original for deletion.
                        var newVariable = graphModel.ConvertVariable(variable, sectionName);
                        duplicated = true;

                        copy.Add(newVariable);
                    }
                    else
                        duplicated = true;
                }
                else
                {
                    copy.Add(item);
                }
            }

            return duplicated;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="blackboardViewState">The state of the blackboard view.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="selectionState">The state of the selection.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, BlackboardViewStateComponent blackboardViewState, SelectionStateComponent selectionState, ReorderGroupItemsCommand command)
        {
            if (command.Group == null)
                return;
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                // Group expanded state is not part of the undo state
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                var duplicatedGroups = new List<GroupModel>();
                var newItems = new List<IGroupItemModel>();
                command.TransferOrCopyGroupItemHierarchy(graphModelState.GraphModel, command.GroupItemModels, duplicatedGroups, newItems);

                command.Group.MoveItemsAfter(newItems, command.InsertAfter);

                graphModelState.GraphModel.DeleteGroups(duplicatedGroups.Where(g => !g.Items.HasAny() && g.IsDeletable()).ToList());
                graphModelState.GraphModel.UpdateSubGraphs();

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetGroupModelExpanded(command.Group, true);
            }

            using (var selectionUpdater = selectionState.UpdateScope)
            {
                selectionUpdater.DisplayInInspector();
            }
        }
    }
}
