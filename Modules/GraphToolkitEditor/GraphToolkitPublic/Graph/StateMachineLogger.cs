// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Provides methods for logging messages, warnings, and errors associated with a state machine.
    /// </summary>
    /// <remarks>
    /// <c>StateMachineLogger</c> integrates with the Unity Console to display logs relevant to state machine operations.
    /// <br/>
    /// <br/>
    /// When a context is provided,
    /// the message is also visually represented in the state machine editor using appropriate markers (error, warning, or info).
    /// <br/>
    /// <br/>
    /// Console messages are only shown if the state machine editor for the corresponding state machine is currently open. If the editor is closed,
    /// the logs will not appear in the Unity Console.
    /// <br/>
    /// <br/>
    /// The logger also provides read-only access to what changed in the state machine via <see cref="StateMachineChanges"/>.
    /// </remarks>
    public class StateMachineLogger : ILogger
    {
        internal ErrorsAndWarningsImp errorsAndWarnings { get; set; }

        /// <summary>
        /// The set of changes recorded on the state machine during the current
        /// <see cref="StateMachine.OnStateMachineChanged"/> callback.
        /// </summary>
        /// <remarks>
        /// Inspect the collections on <see cref="Editor.StateMachineChanges"/> (states, transitions, variables, and
        /// subgraph states) to react to what was added, removed or modified. Only valid inside
        /// <see cref="StateMachine.OnStateMachineChanged"/>.
        /// </remarks>
        public StateMachineChanges StateMachineChanges { get; private set; }

        internal void SetChangeData(
            IReadOnlyList<ChangedState> changedStates,
            IReadOnlyList<ChangedTransition> changedTransitions,
            IReadOnlyList<ChangedVariable> changedVariables,
            IReadOnlyList<ChangedSubgraphState> changedSubgraphStates)
        {
            StateMachineChanges ??= new StateMachineChanges();
            StateMachineChanges.SetChangeData(changedStates, changedTransitions, changedVariables, changedSubgraphStates);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="context">
        /// Optional context object to associate with the error message. The context is typically a state in the state machine that the error relates to.
        /// </param>
        /// <remarks>
        /// When a <paramref name="context"/> is provided, the system displays an error marker next to the specified object in the state machine editor.
        /// The error message also appears in the Unity Console. If no context is given, the message is logged to the console only.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the state machine.
        /// Use errors for situations where Unity can not recover or proceed normally.
        /// </remarks>
        public void LogError(object message, object context = null)
        {
            ((IErrorsAndWarnings)errorsAndWarnings).LogError(message, context);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to display.</param>
        /// <param name="context">
        /// Optional context object to associate with the warning message. The context is typically a state in the state machine that the warning relates to.
        /// </param>
        /// <remarks>
        /// When a <paramref name="context"/> is provided, the system displays a warning marker next to the specified object in the state machine editor.
        /// The warning message also appears in the Unity Console. If no context is given, the message is logged to the console only.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the state machine.
        /// Use warnings for situations where Unity can recover/proceed, but users may be unaware of the side effects.
        /// </remarks>
        public void LogWarning(object message, object context = null)
        {
            ((IErrorsAndWarnings)errorsAndWarnings).LogWarning(message, context);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="context">
        /// Optional context object to associate with the message. The context is typically a state in the state machine that the message relates to.
        /// </param>
        /// <remarks>
        /// When a <paramref name="context"/> is provided, the system displays an info marker next to the specified object in the state machine editor.
        /// The message also appears in the Unity Console. If no context is given, the message is logged to the console only.
        /// Use for communicating non-critical information.
        /// </remarks>
        public void Log(object message, object context = null)
        {
            ((IErrorsAndWarnings)errorsAndWarnings).Log(message, context);
        }

        /// <summary>
        /// Logs an error message with an associated marker action.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="context">
        /// Context object to associate with the error message. The context is typically a state in the state machine that the error relates to.
        /// </param>
        /// <param name="stateMachineLogAction">A <see cref="StateMachineLogAction"/> that might be invoked with the provided context.</param>
        /// <remarks>
        /// The system displays an error marker next to the specified object in the state machine editor and the provided <paramref name="stateMachineLogAction"/> might be invoked for that context.
        /// The error message also appears in the Unity Console.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the state machine.
        /// Use errors for situations where Unity can't recover or proceed normally.
        /// </remarks>
        public void LogError(object message, object context, StateMachineLogAction stateMachineLogAction)
        {
            errorsAndWarnings.LogError(message, context, stateMachineLogAction);
        }

        /// <summary>
        /// Logs a warning message with an associated marker action.
        /// </summary>
        /// <param name="message">The warning message to display.</param>
        /// <param name="context">
        /// Context object to associate with the warning message. The context is typically a state in the state machine that the warning relates to.
        /// </param>
        /// <param name="stateMachineLogAction">A <see cref="StateMachineLogAction"/> that might be invoked with the provided context.</param>
        /// <remarks>
        /// The system displays a warning marker next to the specified object in the state machine editor and the provided <paramref name="stateMachineLogAction"/> might be invoked for that context.
        /// The warning message also appears in the Unity Console.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the state machine.
        /// Use warnings for situations where Unity can recover or proceed, but users might be unaware of the side effects.
        /// </remarks>
        public void LogWarning(object message, object context, StateMachineLogAction stateMachineLogAction)
        {
            errorsAndWarnings.LogWarning(message, context, stateMachineLogAction);
        }

        /// <summary>
        /// Logs an informational message with an associated marker action.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="context">
        /// Context object to associate with the message. The context is typically a state in the state machine that the message relates to.
        /// </param>
        /// <param name="stateMachineLogAction">A <see cref="StateMachineLogAction"/> that might be invoked with the provided context.</param>
        /// <remarks>
        /// The system displays an info marker next to the specified object in the state machine editor and the provided <paramref name="stateMachineLogAction"/> might be invoked for that context.
        /// The message also appears in the Unity Console.
        /// Use for communicating non-critical information.
        /// </remarks>
        public void Log(object message, object context, StateMachineLogAction stateMachineLogAction)
        {
            errorsAndWarnings.Log(message, context, stateMachineLogAction);
        }
    }
}
