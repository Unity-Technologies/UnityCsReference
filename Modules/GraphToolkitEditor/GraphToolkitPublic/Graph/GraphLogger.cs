// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Provides methods for logging messages, warnings, and errors associated with a graph.
    /// </summary>
    /// <remarks>
    /// <c>GraphLogger</c> integrates with the Unity Console to display logs relevant to graph operations.
    /// <br/>
    /// <br/>
    /// When a context is provided,
    /// the message is also visually represented in the graph editor using appropriate markers (error, warning, or info).
    /// <br/>
    /// <br/>
    /// Console messages are only shown if the graph editor for the corresponding graph is currently open. If the editor is closed,
    /// the logs will not appear in the Unity Console.
    /// </remarks>
    public class GraphLogger
    {
        internal ErrorsAndWarningsImp errorsAndWarnings { get; set; }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="context">
        /// Optional context object to associate with the error message. The context is typically a node in the graph that the error relates to.
        /// </param>
        /// <remarks>
        /// When a <paramref name="context"/> is provided, the system displays an error marker next to the specified object in the graph editor.
        /// The error message also appears in the Unity Console. If no context is given, the message is logged to the console only.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the graph.
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
        /// Optional context object to associate with the warning message. The context is typically a node in the graph that the warning relates to.
        /// </param>
        /// <remarks>
        /// When a <paramref name="context"/> is provided, the system displays a warning marker next to the specified object in the graph editor.
        /// The warning message also appears in the Unity Console. If no context is given, the message is logged to the console only.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the graph.
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
        /// Optional context object to associate with the message. The context is typically a node in the graph that the message relates to.
        /// </param>
        /// <remarks>
        /// When a <paramref name="context"/> is provided, the system displays an info marker next to the specified object in the graph editor.
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
        /// Context object to associate with the error message. The context is typically a node in the graph that the error relates to.
        /// </param>
        /// <param name="graphLogAction">A <see cref="GraphLogAction"/> that might be invoked with the provided context.</param>
        /// <remarks>
        /// The system displays an error marker next to the specified object in the Graph Editor and the provided <paramref name="graphLogAction"/> might be invoked for that context.
        /// The error message also appears in the Unity Console.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the graph.
        /// Use errors for situations where Unity can't recover or proceed normally.
        /// </remarks>
        public void LogError(object message, object context, GraphLogAction graphLogAction)
        {
            errorsAndWarnings.LogError(message, context, graphLogAction);
        }

        /// <summary>
        /// Logs a warning message with an associated marker action.
        /// </summary>
        /// <param name="message">The warning message to display.</param>
        /// <param name="context">
        /// Context object to associate with the warning message. The context is typically a node in the graph that the warning relates to.
        /// </param>
        /// <param name="graphLogAction">A <see cref="GraphLogAction"/> that might be invoked with the provided context.</param>
        /// <remarks>
        /// The system displays a warning marker next to the specified object in the Graph Editor and the provided <paramref name="graphLogAction"/> might be invoked for that context.
        /// The warning message also appears in the Unity Console.
        /// Use the <paramref name="context"/> parameter to help users identify the source of the issue within the graph.
        /// Use warnings for situations where Unity can recover or proceed, but users might be unaware of the side effects.
        /// </remarks>
        public void LogWarning(object message, object context, GraphLogAction graphLogAction)
        {
            errorsAndWarnings.LogWarning(message, context, graphLogAction);
        }

        /// <summary>
        /// Logs an informational message with an associated marker action.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="context">
        /// Context object to associate with the message. The context is typically a node in the graph that the message relates to.
        /// </param>
        /// <param name="graphLogAction">A <see cref="GraphLogAction"/> that might be invoked with the provided context.</param>
        /// <remarks>
        /// The system displays an info marker next to the specified object in the Graph Editor and the provided <paramref name="graphLogAction"/> might be invoked for that context.
        /// The message also appears in the Unity Console.
        /// Use for communicating non-critical information.
        /// </remarks>
        public void Log(object message, object context, GraphLogAction graphLogAction)
        {
            errorsAndWarnings.Log(message, context, graphLogAction);
        }
    }
}
