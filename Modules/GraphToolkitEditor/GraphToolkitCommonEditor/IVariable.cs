// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

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
        /// Gets or sets the name of the variable.
        /// </summary>
        /// <remarks>
        /// This name is used to identify the variable in the UI and when matching references in the graph.
        /// </remarks>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the data type of the variable.
        /// </summary>
        /// <remarks>
        /// This type determines the value type the variable holds and influences the data type of the ports
        /// created by corresponding variable nodes.
        /// </remarks>
        Type DataType { get; set; }

        /// <summary>
        /// Gets or sets the kind of the variable, such as Local, Input, or Output.
        /// </summary>
        /// <remarks>
        /// This property defines the scope of the variable. Use <see cref="VariableKind.Local"/> for variables used only within the current graph.
        /// Use <see cref="VariableKind.Input"/> or <see cref="VariableKind.Output"/> to expose variables to or from a subgraph.
        /// This value does not determine the <see cref="PortDirection"/> of the port on the variable node.
        /// </remarks>
        VariableKind VariableKind { get; set; }

        /// <summary>
        /// Whether the variable has at least one connected variable node in the graph.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The number of variable nodes associated with this variable in the graph.
        /// </summary>
        int NodeCount { get; }

        /// <summary>
        /// The <see cref="Graph"/> that owns this variable.
        /// </summary>
        Graph Graph => (((VariableDeclarationModelBase)this).GraphModel as GraphModelImp)?.Graph;

        /// <summary>
        /// The globally unique identifier for this variable declaration.
        /// </summary>
        Hash128 ID { get; }

        /// <summary>
        /// Retrieves the default value of the variable.
        /// </summary>
        /// <typeparam name="T">The expected type of the default value.</typeparam>
        /// <param name="value">The retrieved default value, if available.</param>
        /// <returns><c>true</c> if a default value exists and is of the correct type; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Use this method to retrieve the variable’s default value in a type-safe way.
        /// </remarks>
        bool TryGetDefaultValue<T>(out T value);

        /// <summary>
        /// Sets the default value of the variable.
        /// </summary>
        /// <typeparam name="T">The type of the default value to set that matches the <see cref="DataType"/>.</typeparam>
        /// <param name="value">The default value to set.</param>
        /// <returns><c>true</c> if the default value was successfully set; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The default value must be of a type compatible with the variable's <see cref="DataType"/>.
        /// </remarks>
        bool TrySetDefaultValue<T>(T value);

        /// <summary>
        /// Removes the current variable from its graph.
        /// </summary>
        /// <param name="forceRemove">If true, removes the variable and all variable nodes referencing it. If false, removal fails if the variable is referenced in nodes.</param>
        void RemoveFromGraph(bool forceRemove = false);

        /// <summary>
        /// Retrieves all variable nodes associated with this variable.
        /// </summary>
        /// <param name="nodes">The list to populate with variable nodes. Must not be <c>null</c>; otherwise, an <see cref="ArgumentException"/> is thrown.</param>
        /// <remarks>
        /// This method clears the provided list and populates it with all variable nodes in the graph that reference this variable.
        /// </remarks>
        void GetNodes(List<IVariableNode> nodes);
    }
}
