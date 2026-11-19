// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a variable node, which is a specialized node that references an <see cref="IVariable"/> defined in the graph.
    /// </summary>
    /// <remarks>
    /// Variable nodes are instances of a declared <see cref="IVariable"/> placed on the graph canvas. They are distinct from
    /// <see cref="IVariable"/>s, which are declarations displayed as capsules in the Blackboard.
    /// You can drag and drop a <see cref="IVariable"/> from the Blackboard onto the graph canvas to create a variable node.
    /// A variable node can either retrieve the variable’s value (<see cref="VariableNodeMode.Get"/>) or set it
    /// (<see cref="VariableNodeMode.Set"/>). Use the <see cref="Mode"/> property to determine which role a node plays.
    /// </remarks>
    public interface IVariableNode : INode
    {
        /// <summary>
        /// Retrieves the <see cref="IVariable"/> associated with the node.
        /// </summary>
        /// <remarks>
        /// The variable defines the node’s data type and is shared across all variable nodes that reference it.
        /// It is declared in the graph’s Blackboard.
        /// </remarks>
        public IVariable Variable { get; }

        /// <summary>
        /// The mode of this variable node, which determines its available ports.
        /// </summary>
        /// <remarks>
        /// Determines which ports the node exposes. Refer to <see cref="VariableNodeMode"/> for details on how each mode
        /// affects the node’s ports.
        ///
        /// To change the mode of this node in the graph, use the **Allow to set value in graph** checkbox in its inspector.
        /// </remarks>
        public VariableNodeMode Mode => throw new NotSupportedException();
    }
}
