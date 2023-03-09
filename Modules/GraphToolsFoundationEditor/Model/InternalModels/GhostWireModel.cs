// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a wire in a graph.
    /// </summary>
    /// <remarks>
    /// A ghost wire is usually used as a wire that shows where a wire would connect to during wire
    /// connection manipulations.
    /// </remarks>
    class GhostWireModel : WireModel, IGhostWire
    {
        /// <summary>
        /// The port from which the wire originates.
        /// </summary>
        public override PortModel FromPort { get; set; }

        /// <summary>
        /// The unique id of the originating port.
        /// </summary>
        public override string FromPortId => FromPort?.UniqueName;

        /// <summary>
        /// The unique id of the destination port.
        /// </summary>
        public override string ToPortId => ToPort?.UniqueName;

        /// <summary>
        /// The unique identifier of the node from which the wire originates.
        /// </summary>
        public override SerializableGUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        /// <summary>
        /// The unique identifier of the node to which the wire goes.
        /// </summary>
        public override SerializableGUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        /// <summary>
        /// The port to which the wire goes.
        /// </summary>
        public override PortModel ToPort { get; set; }

        /// <summary>
        /// The label of the wire.
        /// </summary>
        public override string WireLabel { get; set; }

        /// <inheritdoc />
        public Vector2 EndPoint { get; set; } = Vector2.zero;

        /// <summary>
        /// Sets the endpoints of the wire.
        /// </summary>
        /// <param name="toPortModel">The port where the wire goes.</param>
        /// <param name="fromPortModel">The port from which the wire originates.</param>
        public override void SetPorts(PortModel toPortModel, PortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        /// <inheritdoc />
        public override void SetPort(WireSide side, PortModel value)
        {
            if (side == WireSide.From)
            {
                if (value == FromPort)
                    return;
                FromPort = value;
            }
            else
            {
                if (value == ToPort)
                    return;
                ToPort = value;
            }
        }

        /// <summary>
        /// Creates missing ports in the case where the original ports are missing.
        /// </summary>
        /// <param name="inputNode">The node owning the missing input port.</param>
        /// <param name="outputNode">The node owning the missing output port.</param>
        /// <returns>A migration result pair for the input and output port migration.</returns>
        public override (PortMigrationResult, PortMigrationResult) AddMissingPorts(out AbstractNodeModel inputNode, out AbstractNodeModel outputNode)
        {
            inputNode = null;
            outputNode = null;
            return (PortMigrationResult.None, PortMigrationResult.None);
        }
    }
}
