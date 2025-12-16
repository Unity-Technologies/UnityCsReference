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
    /// Provides extension methods related to nodes and ports in a graph.
    /// </summary>
    /// <remarks>
    /// This static class defines utility methods that simplify interactions between graph components,
    /// such as retrieving the <see cref="INode"/> associated with a given <see cref="IPort"/>.
    /// </remarks>
    public static class INodeExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="INode"/> that owns the specified port.
        /// </summary>
        /// <param name="port">The port to get the owning node from.</param>
        /// <returns>The <see cref="INode"/> that contains the specified port.</returns>
        /// <remarks>
        /// This is helpful when analyzing graph structures, especially when traversing connections. The returned node provides context for the port’s role in the graph.
        /// </remarks>
        /// <example>
        /// <code>
        /// IPort port = somePortReference;
        /// INode node = port.GetNode();
        ///
        /// Debug.Log($"Port '{port.name}' belongs to node: {node}.");
        /// </code>
        /// </example>
        public static INode GetNode(this IPort port)
        {
            var nodeModel = (port as PortModel)?.NodeModel as NodeModel;

            if (nodeModel is IUserNodeModelImp imp)
                return imp.Node;

            return nodeModel as INode;
        }
    }

    /// <summary>
    /// Interface for a node.
    /// </summary>
    /// <remarks>
    /// This interface provides methods for accessing input and output ports, which are essential for connecting nodes.
    /// </remarks>
    public interface INode
    {
        /// <summary>
        /// The text displayed when hovering over the node's header.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// The highlight color of the node. The highlight is located on the upper border of nodes, and on the upper and lower borders of context nodes.
        /// </summary>
        public Color DefaultColor { get; set; }

        /// <summary>
        /// The number of input ports on the node.
        /// </summary>
        public int InputPortCount => ((NodeModel)this).InputsByDisplayOrder.Count;

        /// <summary>
        /// Retrieves an input port using its index.
        /// </summary>
        /// <param name="index">The index of the input port.</param>
        /// <returns>The input port at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based. The list of input ports is ordered according to their display order in the node.
        /// </remarks>
        public IPort GetInputPort(int index) => ((NodeModel)this).InputsByDisplayOrder[index];

        /// <summary>
        /// Retrieves all input ports on the node in the order they are displayed.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of input ports.</returns>
        public IEnumerable<IPort> GetInputPorts() => ((NodeModel)this).InputsByDisplayOrder;

        /// <summary>
        /// Retrieves an input port using its name.
        /// </summary>
        /// <param name="name">The unique name of the input port within this node.</param>
        /// <returns>The input port with the specified name, or null if no match is found.</returns>
        /// <remarks>The input port's name is unique within the node's input ports and node options.</remarks>
        public IPort GetInputPortByName(string name) => ((NodeModel)this).InputsById.GetValueOrDefault(name);

        /// <summary>
        /// The number of output ports on the node.
        /// </summary>
        public int OutputPortCount => ((NodeModel)this).OutputsByDisplayOrder.Count;

        /// <summary>
        /// Retrieves an output port using its index in the displayed order.
        /// </summary>
        /// <param name="index">The zero-based index of the output port.</param>
        /// <returns>The output port at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based. The list of output ports is ordered according to their display order in the node.
        /// </remarks>
        public IPort GetOutputPort(int index) => ((NodeModel)this).OutputsByDisplayOrder[index];

        /// <summary>
        /// Retrieves all output ports on the node in the order they are displayed.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of output ports.</returns>
        public IEnumerable<IPort> GetOutputPorts() => ((NodeModel)this).OutputsByDisplayOrder;

        /// <summary>
        /// Retrieves an output port using its name.
        /// </summary>
        /// <param name="name">The unique name of the output port within this node.</param>
        /// <returns>The output port with the specified name, or null if no match is found.</returns>
        /// <remarks>The output port's name is unique within the node's output ports.</remarks>
        public IPort GetOutputPortByName(string name) => ((NodeModel)this).OutputsById.GetValueOrDefault(name);
    }
}
