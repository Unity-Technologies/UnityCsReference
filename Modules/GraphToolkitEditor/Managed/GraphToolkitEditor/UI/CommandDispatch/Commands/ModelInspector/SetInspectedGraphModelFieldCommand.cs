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
    /// Command to set the value of a field on an model.
    /// </summary>
    [UnityRestricted]
    internal class SetInspectedGraphModelFieldCommand : SetInspectedObjectFieldCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedModelFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedObjects">The object that owns the field.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedGraphModelFieldCommand(object value, IReadOnlyList<object> inspectedObjects, FieldInfo field)
            : base(value, inspectedObjects, field)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetInspectedGraphModelFieldCommand command)
        {
            if (command.Field != null && command.InspectedObjects.Count > 0)
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
                    updater.MarkGraphPropertiesChanged();
                }
            }
        }
    }
}
