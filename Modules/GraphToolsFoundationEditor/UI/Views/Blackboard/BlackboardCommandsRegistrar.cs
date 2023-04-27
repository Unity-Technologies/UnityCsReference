// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper to register command handler on the <see cref="BlackboardView"/>.
    /// </summary>
    struct BlackboardCommandsRegistrar
    {
        /// <summary>
        /// Registers command handler on the <paramref name="view"/>.
        /// </summary>
        /// <param name="view">The view to register command handlers on.</param>
        /// <param name="graphTool">The graph tool.</param>
        public static void RegisterCommands(BlackboardView view, BaseGraphTool graphTool)
        {
            new BlackboardCommandsRegistrar(view, view.BlackboardViewModel.GraphModelState, view.BlackboardViewModel.SelectionState,
                view.BlackboardViewModel.ViewState, graphTool).RegisterCommandHandlers();
        }

        /// <summary>
        /// Registers command handler on state components. Mainly useful for tests, where you sometimes do not
        /// have a view holding the state components.
        /// </summary>
        /// <param name="commandTarget">The command target.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="blackboardViewState">The blackboard view state component.</param>
        /// <param name="graphTool">The graph tool.</param>
        public static void RegisterCommands(ICommandTarget commandTarget, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState, BaseGraphTool graphTool)
        {
            new BlackboardCommandsRegistrar(commandTarget, graphModelState, selectionState, blackboardViewState, graphTool).RegisterCommandHandlers();
        }

        ICommandTarget m_CommandTarget;
        GraphModelStateComponent m_GraphModelState;
        SelectionStateComponent m_SelectionState;
        BlackboardViewStateComponent m_BlackboardViewState;
        BaseGraphTool m_GraphTool;

        BlackboardCommandsRegistrar(ICommandTarget commandTarget, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewStateComponent, BaseGraphTool graphTool)
        {
            m_CommandTarget = commandTarget;
            m_GraphModelState = graphModelState;
            m_SelectionState = selectionState;
            m_BlackboardViewState = blackboardViewStateComponent;
            m_GraphTool = graphTool;
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_CommandTarget.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState);
        }

        void RegisterCommandHandler<TCommand>(
            CommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_CommandTarget.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState);
        }

        void RegisterCommandHandler<TParam3, TCommand>(
            CommandHandler<UndoStateComponent, GraphModelStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_CommandTarget.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, handlerParam3);
        }

        void RegisterCommandHandlers()
        {
            m_CommandTarget.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent,
                BlackboardViewStateComponent, SelectionStateComponent, CreateGraphVariableDeclarationCommand>(
                CreateGraphVariableDeclarationCommand.DefaultCommandHandler,
                m_GraphTool.UndoStateComponent, m_GraphModelState, m_BlackboardViewState, m_SelectionState);

            RegisterCommandHandler<BlackboardViewStateComponent, ReorderGroupItemsCommand>(
                ReorderGroupItemsCommand.DefaultCommandHandler, m_BlackboardViewState);

            m_CommandTarget.RegisterCommandHandler<UndoStateComponent, BlackboardViewStateComponent,
                GraphModelStateComponent, SelectionStateComponent, BlackboardGroupCreateCommand>(
                BlackboardGroupCreateCommand.DefaultCommandHandler,
                m_GraphTool.UndoStateComponent, m_BlackboardViewState, m_GraphModelState, m_SelectionState);

            m_CommandTarget.RegisterCommandHandler<BlackboardViewStateComponent, ExpandVariableDeclarationCommand_Internal>(
                ExpandVariableDeclarationCommand_Internal.DefaultCommandHandler, m_BlackboardViewState);

            m_CommandTarget.RegisterCommandHandler<BlackboardViewStateComponent, ExpandVariableGroupCommand_Internal>(
                ExpandVariableGroupCommand_Internal.DefaultCommandHandler, m_BlackboardViewState);

            RegisterCommandHandler<InitializeVariableCommand>(InitializeVariableCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeVariableTypeCommand>(ChangeVariableTypeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ExposeVariableCommand>(ExposeVariableCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler);

            m_CommandTarget.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent,
                BlackboardViewStateComponent, PasteDataCommand>(
                BlackboardViewCommandHandlers.PasteSerializedDataCommandHandler,
                m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState, m_BlackboardViewState);

            RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);

            RegisterCommandHandler<RenameElementCommand>(RenameElementCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateConstantsValueCommand>(UpdateConstantsValueCommand.DefaultCommandHandler);
        }
    }
}
