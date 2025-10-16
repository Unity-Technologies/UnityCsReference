// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper used to register the command handlers for a <see cref="GraphView"/>.
    /// </summary>
    [UnityRestricted]
    internal static class GraphViewCommandsRegistrarHelper
    {
        /// <summary>
        /// Registers the command handlers for the <paramref name="graphView"/>.
        /// </summary>
        /// <param name="registrar">The command handler registrar.</param>
        /// <param name="graphView">The graph view for which to register command handlers.</param>
        public static void RegisterCommands(CommandHandlerRegistrar registrar, GraphView graphView)
        {
            RegisterCommands(registrar, graphView.GraphViewModel.GraphViewState,
                graphView.GraphViewModel.GraphModelState, graphView.GraphViewModel.SelectionState, graphView.GraphViewModel.AutoPlacementState, graphView.GraphTool);
        }

        public static void RegisterCommands(CommandHandlerRegistrar registrar, GraphViewStateComponent graphViewState,
            GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, AutoPlacementStateComponent autoPlacementState, GraphTool graphTool)
        {
            registrar.AddStateComponent(graphViewState);
            registrar.AddStateComponent(graphModelState);
            registrar.AddStateComponent(selectionState);
            registrar.AddStateComponent(autoPlacementState);
            registrar.AddStateComponent(graphTool.UndoState);

            // The CommandHandlerRegistrar does not support command handlers having non-IStateComponent parameters.
            var createWire = new CommandHandlerFunctor<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, AutoPlacementStateComponent, Preferences, CreateWireCommand>();
            createWire.Bind(graphTool.UndoState, graphModelState, selectionState, autoPlacementState, graphTool.Preferences);
            registrar.CommandTarget.RegisterCommandHandler(createWire);

            // The CommandHandlerRegistrar does not support command handlers having non-IStateComponent parameters.
            var createNode = new CommandHandlerFunctor<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, AutoPlacementStateComponent, Preferences, CreateNodeCommand>();
            createNode.Bind(graphTool.UndoState, graphModelState, selectionState, autoPlacementState, graphTool.Preferences);
            registrar.CommandTarget.RegisterCommandHandler(createNode);

            registrar.RegisterDefaultCommandHandler<MoveWireCommand>();
            registrar.RegisterDefaultCommandHandler<ReorderWireCommand>();
            registrar.RegisterDefaultCommandHandler<SplitWireAndInsertExistingNodeCommand>();
            registrar.RegisterDefaultCommandHandler<DisconnectWiresCommand>();
            registrar.RegisterDefaultCommandHandler<DisconnectWiresOnPortCommand>();

            registrar.RegisterDefaultCommandHandler<ConvertWiresToPortalsCommand>();

            registrar.RegisterDefaultCommandHandler<ChangeNodeStateCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeNodeModeCommand>();
            registrar.RegisterDefaultCommandHandler<CollapseNodeCommand>();
            registrar.RegisterDefaultCommandHandler<ShowNodePreviewCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateConstantValueCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateConstantsValueCommand>();
            registrar.RegisterDefaultCommandHandler<CreateOppositePortalCommand>();
            registrar.RegisterDefaultCommandHandler<RevertPortalsToWireCommand>();
            registrar.RegisterDefaultCommandHandler<RevertAllPortalsToWireCommand>();
            registrar.RegisterDefaultCommandHandler<DeleteWireCommand>();

            registrar.RegisterDefaultCommandHandler<ExpandPortCommand>();

            registrar.RegisterDefaultCommandHandler<BuildAllEditorCommand>();

            registrar.RegisterDefaultCommandHandler<AlignNodesCommand>();
            registrar.RegisterDefaultCommandHandler<RenameElementsCommand>();
            registrar.RegisterDefaultCommandHandler<BypassNodesCommand>();
            registrar.RegisterDefaultCommandHandler<MoveElementsCommand>();
            registrar.RegisterDefaultCommandHandler<MoveElementsAndBringPlacematToFrontCommand>();
            registrar.RegisterDefaultCommandHandler<AutoPlaceElementsCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeElementColorCommand>();
            registrar.RegisterDefaultCommandHandler<ResetElementColorCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeElementLayoutCommand>();
            registrar.RegisterDefaultCommandHandler<ChangePlacematLayoutAndBringPlacematToFrontCommand>();
            registrar.RegisterDefaultCommandHandler<CreatePlacematCommand>();
            registrar.RegisterDefaultCommandHandler<ChangePlacematOrderCommand>();
            registrar.RegisterDefaultCommandHandler<DeleteAndSelectPlacematContentCommand>();
            registrar.RegisterDefaultCommandHandler<CreateStickyNoteCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateStickyNoteCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateStickyNoteThemeCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateStickyNoteTextSizeCommand>();
            registrar.RegisterDefaultCommandHandler<SetInspectedModelFieldCommand>();

            registrar.RegisterDefaultCommandHandler<ItemizeNodeCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableDeclarationCommand>();

            registrar.RegisterDefaultCommandHandler<ReframeGraphViewCommand>();

            registrar.RegisterDefaultCommandHandler<PasteDataCommand>();
            registrar.RegisterDefaultCommandHandler<DeleteElementsCommand>();
            registrar.RegisterDefaultCommandHandler<ConvertConstantNodesAndVariableNodesCommand>();

            registrar.RegisterDefaultCommandHandler<SetInspectedGraphModelFieldCommand>();

            registrar.RegisterDefaultCommandHandler<SelectElementsCommand>();
            registrar.RegisterDefaultCommandHandler<DisplayInInspectorCommand>();
            registrar.RegisterDefaultCommandHandler<ClearSelectionCommand>();
            registrar.RegisterDefaultCommandHandler<CreateBlockFromItemLibraryCommand>();
            registrar.RegisterDefaultCommandHandler<InsertBlocksInContextCommand>();
            registrar.RegisterDefaultCommandHandler<CreateSubgraphCommand>();
            registrar.RegisterDefaultCommandHandler<CreateSubgraphNodeFromExistingGraphCommand>();
            registrar.RegisterDefaultCommandHandler<CreateLocalSubgraphFromSelectionCommand>();
            registrar.RegisterDefaultCommandHandler<ConvertLocalToAssetSubgraphCommand>();
            registrar.RegisterDefaultCommandHandler<ConvertAssetToLocalSubgraphCommand>();

            registrar.RegisterDefaultCommandHandler<ExpandSubgraphCommand>();
            registrar.RegisterDefaultCommandHandler<ChangePortTypeCommand>();

            // State machine
            registrar.RegisterDefaultCommandHandler<CreateTransitionSupportCommand>();
            registrar.RegisterDefaultCommandHandler<CreateSingleStateTransitionSupportCommand>();
            registrar.RegisterDefaultCommandHandler<MoveTransitionSupportCommand>();
            registrar.RegisterDefaultCommandHandler<SetEntryPointCommand>();
            registrar.RegisterDefaultCommandHandler<CreateStateFromTransitionCommand>();
            registrar.RegisterDefaultCommandHandler<PasteSingleStateTransitionSupportsCommand>();
            registrar.RegisterDefaultCommandHandler<PasteTransitionSupportsCommand>();
        }
    }
}
