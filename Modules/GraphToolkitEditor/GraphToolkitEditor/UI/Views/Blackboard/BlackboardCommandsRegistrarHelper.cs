// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper to register command handler on the <see cref="BlackboardView"/>.
    /// </summary>
    [UnityRestricted]
    internal static class BlackboardCommandsRegistrarHelper
    {
        /// <summary>
        /// Registers command handlers on the <paramref name="view"/>.
        /// </summary>
        /// <param name="registrar">The command handler registrar.</param>
        /// <param name="view">The view to register command handlers on.</param>
        /// <remarks>
        /// This method registers command handlers for the specified <see cref="BlackboardView"/>. It ensures that the view responds to relevant commands by associating them
        /// with their respective handlers.
        /// </remarks>
        public static void RegisterCommands(CommandHandlerRegistrar registrar, BlackboardView view)
        {
            RegisterCommands(registrar, view.BlackboardRootViewModel.GraphModelState, view.BlackboardRootViewModel.SelectionState,
                view.BlackboardRootViewModel.ViewState, view.BlackboardRootViewModel.BlackboardContentState, view.GraphTool);
        }

        public static void RegisterCommands(CommandHandlerRegistrar registrar, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState,
            BlackboardContentStateComponent blackboardContentState, GraphTool graphTool)
        {
            registrar.AddStateComponent(graphModelState);
            registrar.AddStateComponent(selectionState);
            registrar.AddStateComponent(blackboardViewState);
            registrar.AddStateComponent(blackboardContentState);
            registrar.AddStateComponent(graphTool.UndoState);

            registrar.RegisterDefaultCommandHandler<CreateGraphVariableDeclarationCommand>();

            registrar.RegisterDefaultCommandHandler<ReorderGroupItemsCommand>();
            registrar.RegisterDefaultCommandHandler<BlackboardGroupCreateCommand>();
            registrar.RegisterDefaultCommandHandler<ExpandVariableDeclarationCommand>();
            registrar.RegisterDefaultCommandHandler<ExpandVariableGroupCommand>();

            registrar.RegisterDefaultCommandHandler<InitializeVariableCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableTypeCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableScopeCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableModifiersCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableDisplaySettingsCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateTooltipCommand>();

            registrar.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent,
                BlackboardViewStateComponent, PasteDataCommand>(BlackboardViewCommandHandlers.PasteSerializedDataCommandHandler);

            registrar.RegisterDefaultCommandHandler<DisplayInInspectorCommand>();

            registrar.RegisterDefaultCommandHandler<DeleteElementsCommand>();
            registrar.RegisterDefaultCommandHandler<SelectElementsCommand>();

            registrar.RegisterDefaultCommandHandler<ClearSelectionCommand>();

            registrar.RegisterDefaultCommandHandler<RenameElementsCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateConstantValueCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateConstantsValueCommand>();
        }
    }
}
