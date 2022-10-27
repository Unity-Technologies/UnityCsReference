// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for a node that contains both input and output ports.
    /// </summary>
    [Serializable]
    abstract class InputOutputPortsNodeModel : PortNodeModel
    {
        /// <summary>
        /// Gets all the models of the input ports of this node, indexed by a string unique to the node.
        /// </summary>
        public abstract IReadOnlyDictionary<string, PortModel> InputsById { get; }

        /// <summary>
        /// Gets all the models of the output ports of this node, indexed by a string unique to the node.
        /// </summary>
        public abstract IReadOnlyDictionary<string, PortModel> OutputsById { get; }

        /// <summary>
        /// Gets all the models of the input ports of this node, in the order they should be displayed.
        /// </summary>
        public abstract IReadOnlyList<PortModel> InputsByDisplayOrder { get; }

        /// <summary>
        /// Gets all the models of the output ports of this node, in the order they should be displayed.
        /// </summary>
        public abstract IReadOnlyList<PortModel> OutputsByDisplayOrder { get; }

        /// <inheritdoc />
        public override IEnumerable<PortModel> Ports => InputsById.Values.Concat(OutputsById.Values);

        /// <summary>
        /// Adds a new input port on the node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed for the port) to be called right after the port is created.</param>
        /// <returns>The newly created input port.</returns>
        public abstract PortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<Constant> initializationCallback = null);

        /// <summary>
        /// Adds a new output port on the node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <returns>The newly created output port.</returns>
        public abstract PortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default);

        /// <inheritdoc />
        public override PortModel GetPortFitToConnectTo(PortModel portModel)
        {
            var portsToChooseFrom = portModel.Direction == PortDirection.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return GraphModel.GetCompatiblePorts(portsToChooseFrom, portModel).FirstOrDefault();
        }
    }
}
