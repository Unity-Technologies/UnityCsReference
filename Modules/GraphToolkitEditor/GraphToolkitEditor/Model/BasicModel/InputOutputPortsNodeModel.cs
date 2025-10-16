// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for a node that contains both input and output ports.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class InputOutputPortsNodeModel : PortNodeModel
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

        /// <summary>
        /// Gets the input ports of a node.
        /// </summary>
        public IReadOnlyCollection<PortModel> InputPorts => InputsById.Values as IReadOnlyCollection<PortModel> ?? new List<PortModel>(InputsById.Values);

        /// <summary>
        /// Gets the output ports of a node.
        /// </summary>
        public IReadOnlyCollection<PortModel> OutputPorts => OutputsById.Values as IReadOnlyCollection<PortModel> ?? new List<PortModel>(OutputsById.Values);

        /// <summary>
        /// Gets all the models of the input ports of this node that are visible, in the order they should be displayed.
        /// </summary>
        public virtual IReadOnlyList<PortModel> VisibleInputsByDisplayOrder => InputsByDisplayOrder;

        /// <summary>
        /// Gets all the models of the output ports of this node that are visible, in the order they should be displayed.
        /// </summary>
        public virtual IReadOnlyList<PortModel> VisibleOutputsByDisplayOrder => InputsByDisplayOrder;

        /// <inheritdoc />
        public override IReadOnlyCollection<PortModel> GetPorts()
        {
            var listSize = InputPorts.Count + OutputPorts.Count;
            var ports = new List<PortModel>(listSize);
            ports.AddRange(InputsById.Values);
            ports.AddRange(OutputsById.Values);
            return ports;
        }

        /// <summary>
        /// The list of <see cref="NodeOption"/>.
        /// </summary>
        /// <remarks>The options in this list are created without the use of the <see cref="NodeOptionAttribute"/>.</remarks>
        public IReadOnlyList<NodeOption> NodeOptions => m_NodeOptions;

        /// <inheritdoc />
        public override PortModel GetPortFitToConnectTo(PortModel portModel)
        {
            var portsToChooseFrom = portModel.Direction == PortDirection.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            var compatiblePorts = GraphModel.GetCompatiblePorts(portsToChooseFrom, portModel);
            return compatiblePorts.Count > 0 ? compatiblePorts[0] : null;
        }
    }
}
