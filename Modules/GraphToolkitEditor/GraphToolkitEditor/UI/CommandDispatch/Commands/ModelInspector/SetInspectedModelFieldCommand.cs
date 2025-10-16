// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to set the value of a field on a model.
    /// </summary>
    /// <remarks>
    /// This command is dispatched when users change the value of a field in the inspector for the currently selected <see cref="Model"/>s displayed in the inspector.
    /// </remarks>
    [UnityRestricted]
    internal class SetInspectedModelFieldCommand : SetInspectedObjectFieldCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedModelFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedObjects">The objects that owns the field.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedModelFieldCommand(object value, IReadOnlyList<object> inspectedObjects, FieldInfo field)
            : base(value, inspectedObjects, field)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph view state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetInspectedModelFieldCommand command)
        {
            if (command.InspectedObjects != null && command.Field != null)
            {
                using var changeScope = graphModelState.GraphModel.ChangeDescriptionScope;
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var updater = graphModelState.UpdateScope)
                {
                    SetField(updater, command);

                    updater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }
}
