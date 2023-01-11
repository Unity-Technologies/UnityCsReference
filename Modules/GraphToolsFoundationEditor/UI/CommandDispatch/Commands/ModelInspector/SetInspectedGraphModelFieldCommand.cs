// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to set the value of a field on an model.
    /// </summary>
    class SetInspectedGraphModelFieldCommand : SetInspectedObjectFieldCommand
    {
        public GraphModel InspectedModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedGraphElementModelFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedModel">The model being inspected.</param>
        /// <param name="inspectedObjects">The object that owns the field. Most of the time, it is the same as <paramref name="inspectedModel"/> but can differ if the model is a proxy onto another object.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedGraphModelFieldCommand(object value, GraphModel inspectedModel, IEnumerable<object> inspectedObjects, FieldInfo field)
            : base(value, inspectedObjects, field)
        {
            InspectedModel = inspectedModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetInspectedGraphModelFieldCommand command)
        {
            if (command.InspectedModel != null && command.InspectedObjects != null && command.Field != null && command.InspectedObjects.Any())
            {
                using var changeScope = graphModelState.GraphModel.ChangeDescriptionScope;
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var updater = graphModelState.UpdateScope)
                {
                    SetField(command);
                    updater.MarkUpdated(changeScope.ChangeDescription);
					updater.MarkGraphPropertiesChanged();
                }
            }
        }
    }
}
