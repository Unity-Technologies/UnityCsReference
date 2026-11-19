// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for editor windows that display and edit graphs.
    /// </summary>
    /// <remarks>
    /// Graph Toolkit editor windows implement this interface to provide access to the graph being edited.
    /// Check if an EditorWindow implements this interface to access the graph.
    /// </remarks>
    public interface IGraphWindow
    {
        /// <summary>
        /// Gets the graph currently being edited in this window.
        /// </summary>
        /// <value>
        /// The graph being edited, or null if no graph is currently loaded.
        /// </value>
        Graph Graph { get; }

        /// <summary>
        /// Gets the state machine currently being edited in this window.
        /// </summary>
        /// <value>
        /// The state machine being edited, or null if no state machine is currently loaded.
        /// </value>
        StateMachine StateMachine => throw new NotSupportedException();
    }
}
