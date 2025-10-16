// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command sent on undo/redo.
    /// </summary>
    [UnityRestricted]
    internal class UndoRedoCommand : ICommand
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
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, UndoRedoCommand command)
        {
            undoState.Undo(command.IsRedo);
        }
    }
}
