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
    /// Command to create a variable.
    /// </summary>
    [UnityRestricted]
    internal class CreateGraphVariableDeclarationCommand : UndoableCommand
    {
        /// <summary>
        /// The name of the variable to create.
        /// </summary>
        public string VariableName;

        /// <summary>
        /// The scope of the variable.
        /// </summary>
        public VariableScope scope;

        /// <summary>
        /// The type of variable to create.
        /// </summary>
        public Type VariableType;

        /// <summary>
        /// The type of the variable to create.
        /// </summary>
        public TypeHandle TypeHandle;

        /// <summary>
        /// The guid to assign to the newly created variable.
        /// </summary>
        public Hash128 Guid;

        /// <summary>
        /// The modifiers to apply to the newly created variable.
        /// </summary>
        public ModifierFlags ModifierFlags;

        /// <summary>
        /// The group to insert the variable in.
        /// </summary>
        public GroupModel Group;

        /// <summary>
        /// The index in the group where the variable will be inserted.
        /// </summary>
        public int IndexInGroup;

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        public CreateGraphVariableDeclarationCommand()
        {
            UndoString = "Create Variable";
        }

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="typeHandle">The type of data the new variable declaration to create represents.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, VariableScope scope, TypeHandle typeHandle,
                                                     GroupModel group = null, int indexInGroup = int.MaxValue,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, Hash128 guid = default) : this()
        {
            VariableName = name;
            this.scope = scope;
            TypeHandle = typeHandle;
            Guid = guid.isValid ? guid : Hash128Helpers.GenerateUnique();
            ModifierFlags = modifierFlags;
            Group = group;
            IndexInGroup = indexInGroup;
        }

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="typeHandle">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableType">The type of variable declaration to create.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, VariableScope scope, TypeHandle typeHandle, Type variableType,
                                                     GroupModel group = null, int indexInGroup = int.MaxValue,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, Hash128 guid = default)
            : this(name, scope, typeHandle, group, indexInGroup, modifierFlags, guid)
        {
            VariableType = variableType;
        }

        /// <summary>
        /// Default command handler for CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="blackboardContentState">The blackboard content state component.</param>
        /// <param name="blackboardViewState">The blackboard view state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command to handle.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, BlackboardContentStateComponent blackboardContentState,
            BlackboardViewStateComponent blackboardViewState, SelectionStateComponent selectionState, CreateGraphVariableDeclarationCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                // Group expanded state is not part of the undo state
                undoStateUpdater.SaveState(graphModelState);
            }

            VariableDeclarationModelBase newVariableDeclaration;
            var graphModel = graphModelState.GraphModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                if (command.VariableType != null)
                    newVariableDeclaration = graphModel.CreateGraphVariableDeclaration(command.VariableType, command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.scope, command.Group, command.IndexInGroup, null, command.Guid);
                else
                    newVariableDeclaration = graphModel.CreateGraphVariableDeclaration(command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.scope, command.Group, command.IndexInGroup, null, command.Guid);

                if (newVariableDeclaration == null)
                {
                    return;
                }

                blackboardContentState.BlackboardModel.LastVariableInfos = new VariableCreationInfos
                {
                    Name = command.VariableName,
                    Group = command.Group,
                    IndexInGroup = command.IndexInGroup,
                    ModifierFlags = command.ModifierFlags,
                    Scope = command.scope,
                    TypeHandle = command.TypeHandle,
                    VariableType = command.VariableType
                };

                graphUpdater.MarkForRename(newVariableDeclaration);

                graphModel.UpdateSubGraphs();

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                var current = newVariableDeclaration.ParentGroup;
                while (current != null)
                {
                    bbUpdater.SetGroupModelExpanded(current, true);
                    current = current.ParentGroup;
                }
            }

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using (var selectionUpdaters = selectionHelper.UpdateScopes)
            {
                foreach (var updater in selectionUpdaters)
                    updater.ClearSelection();
                selectionUpdaters.MainUpdateScope.SelectElement(newVariableDeclaration, true);
            }
        }
    }

    /// <summary>
    /// Command to create the initialization value of a variable.
    /// </summary>
    [UnityRestricted]
    internal class InitializeVariableCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to initialize.
        /// </summary>
        public VariableDeclarationModelBase VariableDeclarationModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeVariableCommand"/> class.
        /// </summary>
        public InitializeVariableCommand()
        {
            UndoString = "Initialize Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeVariableCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to initialize.</param>
        public InitializeVariableCommand(VariableDeclarationModelBase variableDeclarationModel)
            : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, InitializeVariableCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.VariableDeclarationModel.CreateInitializationValue();
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to change the type of a variable.
    /// </summary>
    [UnityRestricted]
    internal class ChangeVariableTypeCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> VariableDeclarationModels;

        /// <summary>
        /// The new variable type.
        /// </summary>
        public TypeHandle Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        public ChangeVariableTypeCommand()
        {
            UndoString = "Change Variable Type";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="type">The new variable type.</param>
        public ChangeVariableTypeCommand(VariableDeclarationModelBase variableDeclarationModel, TypeHandle type) : this()
        {
            VariableDeclarationModels = new[] { variableDeclarationModel };
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="type">The new variable type.</param>
        public ChangeVariableTypeCommand(IReadOnlyList<VariableDeclarationModelBase> variableDeclarationModels, TypeHandle type) : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            Type = type;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeVariableTypeCommand command)
        {
            if (command.Type.IsValid)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var graphUpdater = graphModelState.UpdateScope)
                using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
                {
                    for (int i = 0; i < command.VariableDeclarationModels.Count; ++i)
                    {
                        command.VariableDeclarationModels[i].DataType = command.Type;
                    }
                    graphUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// Command to change the Scope of some variables.
    /// </summary>
    [UnityRestricted]
    internal class ChangeVariableScopeCommand : UndoableCommand
    {
        /// <summary>
        /// The variables to update.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> VariableDeclarationModels;

        /// <summary>
        /// The variable scope.
        /// </summary>
        public VariableScope Scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        public ChangeVariableScopeCommand()
        {
            UndoString = "Change Variable Scope";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="scope">The scope of the variable.</param>
        public ChangeVariableScopeCommand(VariableScope scope, IReadOnlyList<VariableDeclarationModelBase> variableDeclarationModels)
            : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            Scope = scope;

            UndoString = "Set Variable Scope To " + scope;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variables to update.</param>
        /// <param name="scope">The new variables scope.</param>
        public ChangeVariableScopeCommand(VariableScope scope, params VariableDeclarationModelBase[] variableDeclarationModel)
            : this(scope, (IReadOnlyList<VariableDeclarationModelBase>)variableDeclarationModel) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="scopeCommand">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeVariableScopeCommand scopeCommand)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var variable in scopeCommand.VariableDeclarationModels)
                {
                    variable.Scope = scopeCommand.Scope;
                }

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to change the modifiers of some variables.
    /// </summary>
    [UnityRestricted]
    internal class ChangeVariableModifiersCommand : UndoableCommand
    {
        /// <summary>
        /// The variables to update.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> VariableDeclarationModels;

        /// <summary>
        /// The variable scope.
        /// </summary>
        public ModifierFlags Modifiers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        public ChangeVariableModifiersCommand()
        {
            UndoString = "Change Variable Subgraph Use";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="modifiers">The new variables modifiers.</param>
        public ChangeVariableModifiersCommand(ModifierFlags modifiers, IReadOnlyList<VariableDeclarationModelBase> variableDeclarationModels)
            : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            Modifiers = modifiers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variables to update.</param>
        /// <param name="modifiers">The new variables modifiers.</param>
        public ChangeVariableModifiersCommand(ModifierFlags modifiers, params VariableDeclarationModelBase[] variableDeclarationModel)
            : this(modifiers, (IReadOnlyList<VariableDeclarationModelBase>)variableDeclarationModel) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="scopeCommand">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeVariableModifiersCommand scopeCommand)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var variable in scopeCommand.VariableDeclarationModels)
                {
                    variable.Modifiers = scopeCommand.Modifiers;
                }

                graphModelState.GraphModel.UpdateSubGraphs();

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to change the display settings of some variables.
    /// </summary>
    [UnityRestricted]
    internal class ChangeVariableDisplaySettingsCommand : UndoableCommand
    {
        /// <summary>
        /// The variables to update.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> VariableDeclarationModels;

        /// <summary>
        /// Whether the variables should be displayed on the inspector only.
        /// </summary>
        /// <remarks>When acting as an input or output for a subgraph, a variable can be displayed on the inspector only,
        /// or on both the inspector and the subgraph node.</remarks>
        public bool ShowOnInspectorOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableDisplaySettingsCommand"/> class.
        /// </summary>
        public ChangeVariableDisplaySettingsCommand()
        {
            UndoString = "Change Variable Display Settings";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableDisplaySettingsCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="showOnInspectorOnly">Whether the variable should only be displayed on the inspector.</param>
        public ChangeVariableDisplaySettingsCommand(bool showOnInspectorOnly, IReadOnlyList<VariableDeclarationModelBase> variableDeclarationModels)
            : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            ShowOnInspectorOnly = showOnInspectorOnly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableScopeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variables to update.</param>
        /// <param name="showOnInspectorOnly">Whether the variable should only be displayed on the inspector.</param>
        public ChangeVariableDisplaySettingsCommand(bool showOnInspectorOnly, params VariableDeclarationModelBase[] variableDeclarationModel)
            : this(showOnInspectorOnly, (IReadOnlyList<VariableDeclarationModelBase>)variableDeclarationModel) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeVariableDisplaySettingsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var variable in command.VariableDeclarationModels)
                {
                    variable.ShowOnInspectorOnly = command.ShowOnInspectorOnly;
                }

                graphModelState.GraphModel.UpdateSubGraphs();

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }

    /// <summary>
    /// Command to update the tooltip of some variables.
    /// </summary>
    [UnityRestricted]
    internal class UpdateTooltipCommand : UndoableCommand
    {
        /// <summary>
        /// The variables to update.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> VariableDeclarationModels;
        /// <summary>
        /// The new tooltip for the variable.
        /// </summary>
        public string Tooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        public UpdateTooltipCommand()
        {
            UndoString = "Edit Tooltip";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="tooltip">The new tooltip for the variable.</param>
        public UpdateTooltipCommand(string tooltip, IReadOnlyList<VariableDeclarationModelBase> variableDeclarationModels) : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="tooltip">The new tooltip for the variable.</param>
        public UpdateTooltipCommand(string tooltip, params VariableDeclarationModelBase[] variableDeclarationModel) : this(tooltip, (IReadOnlyList<VariableDeclarationModelBase>)variableDeclarationModel)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateTooltipCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var variable in command.VariableDeclarationModels)
                {
                    variable.Tooltip = command.Tooltip;
                }

                graphModelState.GraphModel.UpdateSubGraphs();

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
