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
        [NonSerialized]
        List<PortModel> m_CachedPorts;

        [NonSerialized]
        OrderedPorts m_CachedPortsInputs;

        [NonSerialized]
        int m_CachedPortsInputsVersion;

        [NonSerialized]
        OrderedPorts m_CachedPortsOutputs;

        [NonSerialized]
        int m_CachedPortsOutputsVersion;

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
        public override IReadOnlyList<PortModel> GetPorts()
        {
            if (InputsById is OrderedPorts inputs && OutputsById is OrderedPorts outputs)
            {
                if (m_CachedPorts != null
                    && ReferenceEquals(inputs, m_CachedPortsInputs) && inputs.Version == m_CachedPortsInputsVersion
                    && ReferenceEquals(outputs, m_CachedPortsOutputs) && outputs.Version == m_CachedPortsOutputsVersion)
                {
                    return m_CachedPorts;
                }

                m_CachedPorts ??= new List<PortModel>(inputs.Count + outputs.Count);
                m_CachedPorts.Clear();
                m_CachedPorts.AddRange(inputs.Values);
                m_CachedPorts.AddRange(outputs.Values);
                m_CachedPortsInputs = inputs;
                m_CachedPortsInputsVersion = inputs.Version;
                m_CachedPortsOutputs = outputs;
                m_CachedPortsOutputsVersion = outputs.Version;
                return m_CachedPorts;
            }

            var listSize = InputPorts.Count + OutputPorts.Count;
            var ports = new List<PortModel>(listSize);
            ports.AddRange(InputsById.Values);
            ports.AddRange(OutputsById.Values);
            return ports;
        }

        /// <inheritdoc />
        public override PortModel GetPortFitToConnectTo(PortModel portModel)
        {
            var portsToChooseFrom = portModel.Direction == PortDirection.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            var compatiblePorts = GraphModel.GetCompatiblePorts(portsToChooseFrom, portModel);
            return compatiblePorts.Count > 0 ? compatiblePorts[0] : null;
        }
    }
}
