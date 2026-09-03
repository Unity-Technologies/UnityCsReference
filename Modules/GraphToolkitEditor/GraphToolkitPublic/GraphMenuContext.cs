// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Argument passed to a contextual menu handler registered for a <see cref="Graph"/> type.
    /// </summary>
    /// <remarks>
    /// A method decorated with <see cref="GraphMenuAttribute"/> or
    /// <see cref="BlackboardMenuAttribute"/> for a <see cref="Graph"/> subclass must take this type.
    /// For a <see cref="StateMachine"/> subclass, take <see cref="StateMachineMenuContext"/> instead.
    /// </remarks>
    public sealed class GraphMenuContext : MenuContext
    {
        /// <summary>
        /// The graph that owns the view the user right-clicked on.
        /// </summary>
        public Graph Graph { get; }

        internal GraphMenuContext(Graph graph, object clickedObject, Vector2 mousePosition, DropdownMenu menu)
            : base(clickedObject, mousePosition, menu)
        {
            Graph = graph;
        }
    }
}
