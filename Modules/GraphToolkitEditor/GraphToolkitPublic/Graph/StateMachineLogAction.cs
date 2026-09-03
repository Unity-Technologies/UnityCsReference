// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An <see cref="Action"/> to associate with a message logged using the <see cref="StateMachineLogger"/> accepting an <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// <c>StateMachineLogAction</c> serves as a way to link user executable actions to messages logged using the <see cref="StateMachineLogger"/> when handling <see cref="StateMachine.OnStateMachineChanged"/>.
    /// The <see cref="object"/> parameter is the same as the context passed when logging messages with the <see cref="StateMachineLogger"/>.
    /// </remarks>
    public class StateMachineLogAction : ILogAction
    {
        /// <summary>
        /// The description of the action.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The action to execute.
        /// </summary>
        public Action<object> Action { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineLogAction" /> class.
        /// </summary>
        /// <param name="description">The description of the action.</param>
        /// <param name="action">The action to execute.</param>
        /// <remarks>
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="action"/> is null.
        /// </remarks>
        public StateMachineLogAction(string description, Action<object> action)
        {
            Description = description;
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }
    }
}
