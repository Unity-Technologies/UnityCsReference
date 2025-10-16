// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to open or close a section in the inspector.
    /// </summary>
    [UnityRestricted]
    internal class ExpandExpandablePortInInspectorCommand : ICommand
    {
        /// <summary>
        /// The ports to modify.
        /// </summary>
        public string UniqueName;

        /// <summary>
        /// True if the ports need to be expanded, false otherwise.
        /// </summary>
        public bool Expanded;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandExpandablePortInInspectorCommand"/> class.
        /// </summary>
        /// <param name="uniqueName">The unique name of the port models.</param>
        /// <param name="expanded">True if the ports need to be expanded, false otherwise.</param>
        public ExpandExpandablePortInInspectorCommand(string uniqueName, bool expanded)
        {
            UniqueName = uniqueName;
            Expanded = expanded;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="modelInspectorState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(ModelInspectorStateComponent modelInspectorState, ExpandExpandablePortInInspectorCommand command)
        {
            using (var updater = modelInspectorState.UpdateScope)
            {
                updater.SetExpandablePortsExpanded(command.UniqueName, command.Expanded);
            }
        }
    }
}
