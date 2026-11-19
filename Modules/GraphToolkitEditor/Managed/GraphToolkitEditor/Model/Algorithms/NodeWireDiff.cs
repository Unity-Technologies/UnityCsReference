// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Computes the changes in connected wires on a node.
    /// </summary>
    [UnityRestricted]
    internal class NodeWireDiff
    {
        PortNodeModel m_NodeModel;
        PortDirection m_Direction;
        HashSet<WireModel> m_InitialWires;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeWireDiff"/> class.
        /// </summary>
        /// <param name="nodeModel">The node for which we want to track changes in connected wires.</param>
        /// <param name="portDirection">Specifies whether we should track connections on inputs ports, output ports or both.</param>
        public NodeWireDiff(PortNodeModel nodeModel, PortDirection portDirection)
        {
            m_NodeModel = nodeModel;
            m_Direction = portDirection;
            m_InitialWires = GetWires();
        }

        /// <summary>
        /// Returns the wires that were added since the <see cref="NodeWireDiff"/> object was created.
        /// </summary>
        /// <returns>The wires that were added.</returns>
        public IReadOnlyCollection<WireModel> GetAddedWires()
        {
            var currentWires = GetWires();
            if (currentWires == null || currentWires.Count == 0)
            {
                return Array.Empty<WireModel>();
            }

            currentWires.ExceptWith(m_InitialWires);
            return currentWires;
        }

        /// <summary>
        /// Returns the wires that were removed since the <see cref="NodeWireDiff"/> object was created.
        /// </summary>
        /// <returns>The wires that were removed.</returns>
        public IReadOnlyCollection<WireModel> GetDeletedWires()
        {
            var currentWires = GetWires();
            if (currentWires == null || currentWires.Count == 0)
            {
                return m_InitialWires;
            }

            // This is a simple copy, no rehash needed.
            var initialWires = new HashSet<WireModel>(m_InitialWires);
            initialWires.ExceptWith(currentWires);
            return initialWires;
        }

        HashSet<WireModel> GetWires()
        {
            IReadOnlyCollection<PortModel> ports = null;
            switch (m_Direction)
            {
                case PortDirection.None:
                    ports = m_NodeModel.GetPorts();
                    break;

                case PortDirection.Input:
                    ports = (m_NodeModel as InputOutputPortsNodeModel)?.InputsByDisplayOrder ?? m_NodeModel.GetPorts();
                    break;

                case PortDirection.Output:
                    ports = (m_NodeModel as InputOutputPortsNodeModel)?.OutputsByDisplayOrder ?? m_NodeModel.GetPorts();
                    break;
            }

            HashSet<WireModel> wires = null;
            if (ports != null)
            {
                var wireCount = 0;
                foreach (var port in ports)
                {
                    wireCount += port.GetConnectedWires().Count;
                }

                wires = new HashSet<WireModel>(wireCount);
                foreach (var port in ports)
                {
                    wires.UnionWith(port.GetConnectedWires());
                }
            }

            return wires;
        }
    }
}
