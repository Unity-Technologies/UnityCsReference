// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper used to register the command handlers for a <see cref="GraphView"/>.
    /// </summary>
    struct GraphViewCommandsRegistrar
    {
        /// <summary>
        /// Registers the command handlers for the <paramref name="graphView"/>.
        /// </summary>
        /// <param name="graphView">The graph view for which to register command handlers.</param>
        /// <param name="graphTool">The tool, used to get the <see cref="Preferences"/> and the <see cref="UndoStateComponent"/>.</param>
        public static void RegisterCommands(GraphView graphView, BaseGraphTool graphTool)
        {
            new GraphViewCommandsRegistrar(graphView, graphView.GraphViewModel.GraphViewState, graphView.GraphViewModel.GraphModelState, graphView.GraphViewModel.SelectionState, graphView.GraphViewModel.AutoPlacementState, graphTool).RegisterCommandHandlers();
        }

        internal static void RegisterCommands_Internal(ICommandTarget commandTarget, GraphViewStateComponent graphViewState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, AutoPlacementStateComponent autoPlacementState, BaseGraphTool graphTool)
        {
            new GraphViewCommandsRegistrar(commandTarget, graphViewState, graphModelState, selectionState, autoPlacementState, graphTool).RegisterCommandHandlers();
        }

        ICommandTarget m_CommandTarget;
        GraphViewStateComponent m_GraphViewState;
        GraphModelStateComponent m_GraphModelState;
        SelectionStateComponent m_SelectionState;
        AutoPlacementStateComponent m_AutoPlacementState;
        BaseGraphTool m_GraphTool;

        GraphViewCommandsRegistrar(ICommandTarget commandTarget, GraphViewStateComponent graphViewState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, AutoPlacementStateComponent autoPlacementState, BaseGraphTool graphTool)
        {
            m_CommandTarget = commandTarget;
            m_GraphViewState = graphViewState;
            m_GraphModelState = graphModelState;
            m_SelectionState = selectionState;
            m_AutoPlacementState = autoPlacementState;
            m_GraphTool = graphTool;
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_CommandTarget.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState);
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_CommandTarget.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState);
        }

        void RegisterCommandHandler<TParam3, TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, AutoPlacementStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_CommandTarget.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState, m_AutoPlacementState, handlerParam3);
        }

        void RegisterCommandHandlers()
        {
            RegisterCommandHandler<Preferences, CreateWireCommand>(
                CreateWireCommand.DefaultCommandHandler, m_GraphTool.Preferences);
            RegisterCommandHandler<Preferences, CreateNodeCommand>(CreateNodeCommand.DefaultCommandHandler,
                m_GraphTool.Preferences);

            RegisterCommandHandler<MoveWireCommand>(MoveWireCommand.DefaultCommandHandler);
            RegisterCommandHandler<ReorderWireCommand>(ReorderWireCommand.DefaultCommandHandler);
            RegisterCommandHandler<SplitWireAndInsertExistingNodeCommand>(SplitWireAndInsertExistingNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<DisconnectWiresCommand>(DisconnectWiresCommand.DefaultCommandHandler);

            m_CommandTarget.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, GraphModelStateComponent, SelectionStateComponent, ConvertWiresToPortalsCommand>(
                ConvertWiresToPortalsCommand.DefaultCommandHandler, m_GraphTool.UndoStateComponent, m_GraphViewState, m_GraphModelState, m_SelectionState);

            RegisterCommandHandler<ChangeNodeStateCommand>(ChangeNodeStateCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeNodeModeCommand>(ChangeNodeModeCommand.DefaultCommandHandler);
            RegisterCommandHandler<CollapseNodeCommand>(CollapseNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ShowNodePreviewCommand>(ShowNodePreviewCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateConstantsValueCommand>(UpdateConstantsValueCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateOppositePortalCommand>(CreateOppositePortalCommand.DefaultCommandHandler);
            RegisterCommandHandler<DeleteWireCommand>(DeleteWireCommand.DefaultCommandHandler);

            m_CommandTarget.RegisterCommandHandler<BuildAllEditorCommand>(BuildAllEditorCommand.DefaultCommandHandler);

            RegisterCommandHandler<AlignNodesCommand>(AlignNodesCommand.DefaultCommandHandler);
            RegisterCommandHandler<RenameElementCommand>(RenameElementCommand.DefaultCommandHandler);
            RegisterCommandHandler<BypassNodesCommand>(BypassNodesCommand.DefaultCommandHandler);
            RegisterCommandHandler<MoveElementsCommand>(MoveElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<AutoPlaceElementsCommand>(AutoPlaceElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeElementColorCommand>(ChangeElementColorCommand.DefaultCommandHandler);
            RegisterCommandHandler<ResetElementColorCommand>(ResetElementColorCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeElementLayoutCommand>(ChangeElementLayoutCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreatePlacematCommand>(CreatePlacematCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangePlacematOrderCommand>(ChangePlacematOrderCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateStickyNoteCommand>(CreateStickyNoteCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateStickyNoteCommand>(UpdateStickyNoteCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateStickyNoteThemeCommand>(UpdateStickyNoteThemeCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateStickyNoteTextSizeCommand>(UpdateStickyNoteTextSizeCommand.DefaultCommandHandler);
            RegisterCommandHandler<SetInspectedGraphElementModelFieldCommand>(SetInspectedGraphElementModelFieldCommand.DefaultCommandHandler);

            RegisterCommandHandler<ItemizeNodeCommand>(ItemizeNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<LockConstantNodeCommand>(LockConstantNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeVariableDeclarationCommand>(ChangeVariableDeclarationCommand.DefaultCommandHandler);

            m_CommandTarget.RegisterCommandHandler<GraphViewStateComponent, GraphModelStateComponent, SelectionStateComponent, ReframeGraphViewCommand>(
                ReframeGraphViewCommand.DefaultCommandHandler, m_GraphViewState, m_GraphModelState, m_SelectionState);

            RegisterCommandHandler<PasteDataCommand>(PasteDataCommand.DefaultCommandHandler);
            RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ConvertConstantNodesAndVariableNodesCommand>(
                ConvertConstantNodesAndVariableNodesCommand.DefaultCommandHandler);

            RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateBlockFromItemLibraryCommand>(CreateBlockFromItemLibraryCommand.DefaultCommandHandler);
            RegisterCommandHandler<InsertBlocksInContextCommand>(InsertBlocksInContextCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateSubgraphCommand>(CreateSubgraphCommand.DefaultCommandHandler);
            m_CommandTarget.RegisterCommandHandler<GraphModelStateComponent, UpdateSubgraphCommand>(UpdateSubgraphCommand.DefaultCommandHandler, m_GraphModelState);
        }
    }
}
