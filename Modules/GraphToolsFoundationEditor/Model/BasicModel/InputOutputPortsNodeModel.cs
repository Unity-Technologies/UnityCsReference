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
        protected List<NodeOption> m_NodeOptions = new List<NodeOption>();

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
        /// The list of <see cref="NodeOption"/>.
        /// </summary>
        /// <remarks>The options in this list are created without the use of the <see cref="NodeOptionAttribute"/>.</remarks>
        public IReadOnlyList<NodeOption> NodeOptions => m_NodeOptions;

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
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <returns>The newly created input port.</returns>
        public abstract PortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<Constant> initializationCallback = null,
            Attribute[] attributes = null);

        /// <summary>
        /// Adds a new input port with no connector on a node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed for the port) to be called right after the port is created.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <returns>The newly created no connector input port.</returns>
        public abstract PortModel AddNoConnectorInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<Constant> initializationCallback = null,
            Attribute[] attributes = null);

        /// <summary>
        /// Adds a new output port on the node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <returns>The newly created output port.</returns>
        public abstract PortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);

        /// <summary>
        /// Adds a node option to the node.
        /// </summary>
        /// <param name="name">The name of the node option.</param>
        /// <param name="type">The type of the node option.</param>
        /// <param name="setterAction">A setter method to be called after a node option's constant has changed.</param>
        /// <param name="uniqueId">The unique id of the node option.</param>
        /// <param name="tooltip">The tooltip to show on the node option, if any.</param>
        /// <param name="showInInspectorOnly">Whether the node option is only shown in the inspector. By default, it is shown in both the node and in the inspector.</param>
        /// <param name="order">The order in which the option will be displayed among the other node options.</param>
        /// <param name="initializationCallback">An initialization method for the associated constant to be called right after the node option is created.</param>
        /// <param name="attributes">The attributes used to convey information about the node option, if any.</param>
        /// <remarks>Provides a way to create a node option without the use of the <see cref="NodeOptionAttribute"/>.</remarks>
        protected void AddNodeOption(string name, TypeHandle type, Action<Constant> setterAction, string uniqueId = null, string tooltip = "", bool showInInspectorOnly = false, int order = 0, Action<Constant> initializationCallback = null, Attribute[] attributes = null)
        {
            // A node option consists in a no connector port with extra info.
            var noConnectorPort = AddNoConnectorInputPort(name, PortType.Data, type, uniqueId, options: PortModelOptions.IsNodeOption, attributes: attributes);
            initializationCallback?.Invoke(noConnectorPort.EmbeddedValue);

            if (!string.IsNullOrEmpty(tooltip))
                noConnectorPort.ToolTip = tooltip;

            m_NodeOptions.Add(new NodeOption(noConnectorPort, setterAction, showInInspectorOnly, order));
        }

        /// <inheritdoc />
        public override PortModel GetPortFitToConnectTo(PortModel portModel)
        {
            var portsToChooseFrom = portModel.Direction == PortDirection.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return GraphModel.GetCompatiblePorts(portsToChooseFrom, portModel).FirstOrDefault();
        }
    }
}
