// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Specifies the scope of a <see cref="IVariable"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="VariableKind"/> enum defines the scope of a variable relative to subgraph usage. It determines whether a variable is
    /// scoped locally to the current graph, passed into the graph from a parent (<see cref="Input"/>), or exposed back to the parent (<see cref="Output"/>).
    /// </remarks>
    public enum VariableKind
    {
        /// <summary>
        /// A variable used only within the current graph.
        /// </summary>
        Local,

        /// <summary>
        /// A variable used as an input to a subgraph.
        /// </summary>
        /// <remarks>
        /// This kind exposes the variable to the parent graph when the graph is used as a subgraph.
        /// The parent graph can provide a value for it.
        /// </remarks>
        Input,

        /// <summary>
        /// A variable used as an output from a subgraph.
        /// </summary>
        /// <remarks>
        /// This kind exposes the variable to the parent graph when the graph is used as a subgraph.
        /// The subgraph assigns a value that the parent graph can read.
        /// </remarks>
        Output
    }
}
