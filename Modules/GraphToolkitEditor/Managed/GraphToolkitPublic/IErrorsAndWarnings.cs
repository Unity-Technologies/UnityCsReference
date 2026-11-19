// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a container that handles errors, warnings, and informational messages related to a graph or its elements.
    /// </summary>
    /// <remarks>
    /// Implement this interface to provide logging capabilities for graph-related components. Logged messages can optionally be
    /// associated with a specific object in the graph, which enables displaying visual markers in the UI. All messages are also
    /// forwarded to the Unity Console.
    /// </remarks>
    public interface IErrorsAndWarnings
    {
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
        public void LogError(object message, object context = null);

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
        /// Use warnings for situations where Unity can recover/proceed but users may be unaware of the side effects.
        /// </remarks>
        public void LogWarning(object message, object context = null);

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
        public void Log(object message, object context = null);
    }
}
