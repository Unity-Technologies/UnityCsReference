// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Computes the changes in connected wires on a node.
    /// </summary>
    class NodeWireDiff
    {
        PortNodeModel m_NodeModel;
        PortDirection m_Direction;
        List<WireModel> m_InitialWires;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeWireDiff"/> class.
        /// </summary>
        /// <param name="nodeModel">The node for which we want to track changes in connected wires.</param>
        /// <param name="portDirection">Specifies whether we should track connections on inputs ports, output ports or both.</param>
        public NodeWireDiff(PortNodeModel nodeModel, PortDirection portDirection)
        {
            m_NodeModel = nodeModel;
            m_Direction = portDirection;
            m_InitialWires = GetWires().ToList();
        }

        /// <summary>
        /// Returns the wires that were added since the <see cref="NodeWireDiff"/> object was created.
        /// </summary>
        /// <returns>The wires that were added.</returns>
        public IEnumerable<WireModel> GetAddedWires()
        {
            var initialWires = new HashSet<WireModel>(m_InitialWires);

            foreach (var wire in GetWires())
            {
                if (!initialWires.Contains(wire))
                {
                    yield return wire;
                }
            }
        }

        /// <summary>
        /// Returns the wires that were removed since the <see cref="NodeWireDiff"/> object was created.
        /// </summary>
        /// <returns>The wires that were removed.</returns>
        public IEnumerable<WireModel> GetDeletedWires()
        {
            var currentWires = new HashSet<WireModel>(GetWires());

            foreach (var wire in m_InitialWires)
            {
                if (!currentWires.Contains(wire))
                {
                    yield return wire;
                }
            }
        }

        IEnumerable<WireModel> GetWires()
        {
            IEnumerable<PortModel> ports = null;
            switch (m_Direction)
            {
                case PortDirection.None:
                    ports = m_NodeModel.Ports;
                    break;

                case PortDirection.Input:
                    ports = (m_NodeModel as InputOutputPortsNodeModel)?.InputsByDisplayOrder ?? m_NodeModel.Ports;
                    break;

                case PortDirection.Output:
                    ports = (m_NodeModel as InputOutputPortsNodeModel)?.OutputsByDisplayOrder ?? m_NodeModel.Ports;
                    break;
            }

            return ports?.SelectMany(p => p.GetConnectedWires()) ?? Enumerable.Empty<WireModel>();
        }
    }
}
