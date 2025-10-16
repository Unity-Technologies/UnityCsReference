// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// This command changes a polymorphic port type
    /// </summary>
    class ChangePortTypeCommand : UndoableCommand
    {
        /// <summary>
        /// PortModel to change the selected type
        /// </summary>
        public PortModel PortModel;

        /// <summary>
        /// Index of the type to select (based on the Types collection from the PolymorphicPort)
        /// </summary>
        public uint NewTypeIndex;

        /// <summary>
        /// Undo stack display label
        /// </summary>
        public ChangePortTypeCommand() => UndoString = "Change port type";

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangePortTypeCommand command)
        {
            if (!command.PortModel.IsPolymorphic)
            {
                throw new ArgumentException("ChangePortTypeCommand can be used only with polymorphic ports");
            }

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.PortModel.PolymorphicPortHandler.SetSelectedTypeIndex(command.NewTypeIndex);
                command.PortModel.UpdateDatatypeHandler();
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
