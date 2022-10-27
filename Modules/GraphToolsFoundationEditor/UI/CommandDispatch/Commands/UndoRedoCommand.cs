// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command sent on undo/redo.
    /// </summary>
    class UndoRedoCommand : ICommand
    {
        bool IsRedo { get; }

        public UndoRedoCommand(bool isRedo)
        {
            IsRedo = isRedo;
        }

        /// <summary>
        /// Default command handler for undo/redo.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, UndoRedoCommand command)
        {
            undoState.Undo(command.IsRedo);
        }
    }
}
