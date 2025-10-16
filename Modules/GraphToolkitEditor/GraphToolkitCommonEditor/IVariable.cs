// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a graph variable.
    /// </summary>
    /// <remarks>
    /// Variables are data containers that are accessible throughout a graph. You can manage variables in the Blackboard and reference them with variable nodes.
    /// A variable has a name, a data type, a kind that defines its visibility within subgraphs, and an optional default value.
    /// Use <see cref="TryGetDefaultValue{T}"/> to retrieve the default value if one exists.
    /// Variables are distinct from variable nodes. However, you can drag variables onto the graph canvas, where they are instantiated as variable nodes.
    /// </remarks>
    public interface IVariable
    {
        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        /// <remarks>
        /// This name is used to identify the variable in the UI and when matching references in the graph.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets the data type of the variable.
        /// </summary>
        /// <remarks>
        /// This type determines the value type the variable holds and influences the data type of the ports
        /// created by corresponding variable nodes.
        /// </remarks>
        Type DataType { get; }

        /// <summary>
        /// Gets the kind of the variable, such as Local, Input, or Output.
        /// </summary>
        /// <remarks>
        /// This property defines the scope of the variable. Use <see cref="VariableKind.Local"/> for variables used only within the current graph.
        /// Use <see cref="VariableKind.Input"/> or <see cref="VariableKind.Output"/> to expose variables to or from a subgraph.
        /// This value does not determine the <see cref="PortDirection"/> of the port on the variable node.
        /// </remarks>
        VariableKind VariableKind { get; }

        /// <summary>
        /// Tries to retrieve the default value of the variable.
        /// </summary>
        /// <typeparam name="T">The expected type of the default value.</typeparam>
        /// <param name="value">The retrieved default value, if available.</param>
        /// <returns><c>true</c> if a default value exists and is of the correct type; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Use this method to retrieve the variable’s default value in a type-safe way.
        /// </remarks>
        bool TryGetDefaultValue<T>(out T value);
    }
}
