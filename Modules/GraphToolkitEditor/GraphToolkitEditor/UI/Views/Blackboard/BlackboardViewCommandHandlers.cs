// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal static class BlackboardViewCommandHandlers
    {
        /// <summary>
        /// Command handler for the <see cref="PasteDataCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="blackboardViewState">The blackboard state component.</param>
        /// <param name="command">The command.</param>
        public static void PasteSerializedDataCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState, PasteDataCommand command)
        {
            if (!command.Data.IsEmpty() && graphModelState.GraphModel != null)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                    undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
                }

                using (var graphViewUpdater = graphModelState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                using (var blackboardViewUpdater = blackboardViewState?.UpdateScope)
                {
                    using var changeScope = graphModelState.GraphModel.ChangeDescriptionScope;

                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection();
                    }

                    CopyPasteData.PasteSerializedData(command.Operation, command.Delta,
                        blackboardViewUpdater, selectionUpdaters.MainUpdateScope, command.Data,
                        graphModelState.GraphModel, command.SelectedGroup);

                    graphViewUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }
}
