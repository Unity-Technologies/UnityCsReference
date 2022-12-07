// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    static class BlackboardViewCommandHandlers
    {
        /// <summary>
        /// Command handler for the <see cref="PasteSerializedDataCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="blackboardState">The blackboard state component.</param>
        /// <param name="command">The command.</param>
        public static void PasteSerializedDataCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardState, PasteSerializedDataCommand command)
        {
            if (!command.Data.IsEmpty())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                    undoStateUpdater.SaveStates(selectionHelper.UndoableSelectionStates);
                }

                using (var graphViewUpdater = graphModelState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                using (var blackboardUpdater = blackboardState?.UpdateScope)
                {
                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection();
                    }

                    CopyPasteData.PasteSerializedData(command.Operation, command.Delta, graphViewUpdater,
                        blackboardUpdater, selectionUpdaters.MainUpdateScope, command.Data, graphModelState.GraphModel, command.SelectedGroup);
                }
            }
        }
    }
}
