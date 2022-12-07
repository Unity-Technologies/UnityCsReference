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
    /// Command to create a new blackboard group
    /// </summary>
    class BlackboardGroupCreateCommand : UndoableCommand
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
        /// <param name="blackboardViewState">The state of the blackboard.</param>
        /// <param name="graphModelState">The state of the graph model.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, BlackboardViewStateComponent blackboardViewState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BlackboardGroupCreateCommand command)
        {
            using var graphUpdater = graphModelState.UpdateScope;
            using var changeScope = graphModelState.GraphModel.ChangeDescriptionScope;

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using var selectionUpdaters = selectionHelper.UpdateScopes;

            if (command.ContainingGroup == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = selectionHelper.UndoableSelectionStates.Concat(new IUndoableStateComponent[] { graphModelState, blackboardViewState });
                undoStateUpdater.SaveStates(undoableStates);
            }

            var title = command.Title;


            if (string.IsNullOrEmpty(title))
            {
                var trimmedName = "New Group";
                title = trimmedName;

                int cpt = 1;
                while (command.ContainingGroup.Items.Any(t => (t is IHasTitle titled) && titled.Title == title))
                    title = trimmedName.FormatWithNamingScheme(cpt++);
            }

            foreach (var selectionUpdater in selectionUpdaters)
            {
                selectionUpdater.ClearSelection();
            }

            GroupModel newGroup = command.ContainingGroup.GraphModel.CreateGroup(title, command.GroupItemModels);

            int index = command.ContainingGroup.Items.IndexOf_Internal(command.InsertAfter);
            command.ContainingGroup.InsertItem(newGroup, index < 0 ? (command.InsertAfter == null ? 0 : int.MaxValue) : index + 1);

            foreach (var recursiveSubgraphNode in graphModelState.GraphModel.GetRecursiveSubgraphNodes())
                recursiveSubgraphNode.Update();

            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetGroupModelExpanded(command.ContainingGroup,true);
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
    class ExpandVariableDeclarationCommand_Internal : ICommand
    {
        /// <summary>
        /// The variable to expand or collapse.
        /// </summary>
        public VariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// True if the variable should be expanded, false if it should be collapsed.
        /// </summary>
        public bool Expand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandVariableDeclarationCommand_Internal"/> class.
        /// </summary>
        public ExpandVariableDeclarationCommand_Internal(VariableDeclarationModel variableDeclarationModel, bool expand)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Expand = expand;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="blackboardViewState">The blackboard state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(BlackboardViewStateComponent blackboardViewState, ExpandVariableDeclarationCommand_Internal command)
        {
            using var bbUpdater = blackboardViewState.UpdateScope;
            bbUpdater.SetVariableDeclarationModelExpanded(command.VariableDeclarationModel, command.Expand);
        }
    }

    /// <summary>
    /// Command to expand or collapse a variable group in the blackboard.
    /// </summary>
    class ExpandVariableGroupCommand_Internal : ICommand
    {
        /// <summary>
        /// The variable group to expand or collapse.
        /// </summary>
        public GroupModel GroupModel;
        /// <summary>
        /// True if the variable group should be expanded, false if it should be collapsed.
        /// </summary>
        public bool Expand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandVariableGroupCommand_Internal"/> class.
        /// </summary>
        public ExpandVariableGroupCommand_Internal(GroupModel groupModel, bool expand)
        {
            GroupModel = groupModel;
            Expand = expand;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="blackboardViewState">The blackboard state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(BlackboardViewStateComponent blackboardViewState, ExpandVariableGroupCommand_Internal command)
        {
            using var bbUpdater = blackboardViewState.UpdateScope;
            bbUpdater.SetGroupModelExpanded(command.GroupModel, command.Expand);
        }
    }

    /// <summary>
    /// Command to reorder group items.
    /// </summary>
    class ReorderGroupItemsCommand : UndoableCommand
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
                                                      IReadOnlyList<IGroupItemModel> groupItemModels) : this()
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
        bool TransferOrCopyGroupItemHierarchy(IReadOnlyList<IGroupItemModel> original, List<GroupModel> duplicatedGroups, List<IGroupItemModel> copy)
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
                        if (!TransferOrCopyGroupItemHierarchy(group.Items, duplicatedGroups, listCopy))
                            copy.Add(group);
                        else
                        {
                            // If the group contains at least one variable : create a new group to hold the new items.
                            var newGroup = Group.GraphModel.CreateGroup(group.Title,listCopy);
                            copy.Add(newGroup);
                            duplicatedGroups.Add(group);
                            duplicated = true;
                        }

                    }
                }
                else if (item is VariableDeclarationModel variable)
                {
                    // If the section is the target section simply add the variable.
                    if (item.GetSection() == Group.GetSection())
                        copy.Add(variable);
                    else if(Group.GraphModel.Stencil.CanConvertVariable(variable, sectionName))
                    {
                        Group.GraphModel.DeleteVariableDeclarations(new[] {variable});

                        //If the variable can be converted to the new section, convert the variable then mark the original for deletion.
                        var newVariable = Group.GraphModel.Stencil.ConvertVariable(variable, sectionName);
                        duplicated = true;

                        copy.Add(newVariable);
                    }
                    else
                        duplicated = true;
                }
            }

            return duplicated;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="blackboardViewState">The state of the blackboard.</param>
        /// <param name="graphModelState">The state of the graph view.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, BlackboardViewStateComponent blackboardViewState, ReorderGroupItemsCommand command)
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
                command.TransferOrCopyGroupItemHierarchy(command.GroupItemModels, duplicatedGroups, newItems);

                command.Group.MoveItemsAfter(newItems, command.InsertAfter);

                graphModelState.GraphModel.DeleteGroups(duplicatedGroups.Where(g => !g.Items.Any() && g.IsDeletable()).ToList());

                foreach (var recursiveSubgraphNode in graphModelState.GraphModel.GetRecursiveSubgraphNodes())
                    recursiveSubgraphNode.Update();

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetGroupModelExpanded(command.Group,true);
            }
        }
    }
}
