// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a variable node, which is a specialized node that references a <see cref="IVariable"/> defined in the graph.
    /// </summary>
    /// <remarks>
    /// Variable nodes represent a reference to a declared <see cref="IVariable"/> in the graph.
    /// They are distinct from <see cref="IVariable"/>s, which are declarations displayed as capsules in the graph’s Blackboard.
    /// You can drag and drop a <see cref="IVariable"/> from the Blackboard into the graph canvas to create a variable node.
    /// The variable node is an instance of the declared <see cref="IVariable"/> and appears in the graph.
    /// </remarks>
    public interface IVariableNode : INode
    {
        /// <summary>
        /// Retrieves the <see cref="IVariable"/> associated with the node.
        /// </summary>
        /// <remarks>
        /// This property returns the variable that this node references. The variable defines the node’s data type and determines
        /// the port behavior. The returned variable is declared in the graph's Blackboard and shared across all variable nodes that reference it.
        /// </remarks>
        public IVariable Variable { get; }
    }
}
