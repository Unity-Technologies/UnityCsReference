// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal enum PortMigrationResult
    {
        None,
        MissingPortNotNeeded,
        MissingPortAdded,
        MissingPortFailure,
    }

    /// <summary>
    /// Identifies a side of a wire, not taking into account its direction.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal enum WireSide
    {
        /// <summary>
        /// The first defined point of the wire.
        /// </summary>
        From,
        /// <summary>
        /// The second defined point of the wire.
        /// </summary>
        To
    }

    /// <summary>
    /// Extension methods for the <see cref="WireSide"/> enum.
    /// </summary>
    [UnityRestricted]
    internal static class WireSideExtensions
    {
        /// <summary>
        /// Gets the opposite side of an <see cref="WireSide"/>.
        /// </summary>
        /// <param name="side">The side to get the opposite of.</param>
        /// <returns>The opposite side.</returns>
        public static WireSide GetOtherSide(this WireSide side) => side == WireSide.From ? WireSide.To : WireSide.From;
    }

    /// <summary>
    /// A model that represents a wire in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class WireModel : GraphElementModel, IPortWireIndexModel
    {
        [SerializeField, FormerlySerializedAs("m_OutputPortReference")]
        PortReference m_FromPortReference;

        [SerializeField, FormerlySerializedAs("m_InputPortReference")]
        PortReference m_ToPortReference;

        protected string m_WireBubbleText;

        internal const string k_FromPortReferenceFieldName = nameof(m_FromPortReference);
        internal const string k_ToPortReferenceFieldName = nameof(m_ToPortReference);

        PortModel m_FromPortModelCache;

        PortModel m_ToPortModelCache;

        /// <inheritdoc />
        public override GraphModel GraphModel
        {
            get => base.GraphModel;
            set
            {
                base.GraphModel = value;
                m_FromPortReference.AssignGraphModel(value);
                m_ToPortReference.AssignGraphModel(value);
            }
        }

        /// <summary>
        /// The port from which the wire originates.
        /// </summary>
        public virtual PortModel FromPort
        {
            get => m_FromPortReference.GetPortModel(ref m_FromPortModelCache);
            set
            {
                using var assetDirtyScope = GraphModel.AssetDirtyScope();

                var oldPort = FromPort;
                oldPort?.NodeModel.RemoveUnusedMissingPort(oldPort);
                m_FromPortReference.Assign(value);
                m_FromPortModelCache = value;
                OnPortChanged(oldPort, value);
            }
        }

        /// <summary>
        /// The port to which the wire goes.
        /// </summary>
        public virtual PortModel ToPort
        {
            get => m_ToPortReference.GetPortModel(ref m_ToPortModelCache);
            set
            {
                using var assetDirtyScope = GraphModel.AssetDirtyScope();

                var oldPort = ToPort;
                oldPort?.NodeModel.RemoveUnusedMissingPort(oldPort);
                m_ToPortReference.Assign(value);
                m_ToPortModelCache = value;
                OnPortChanged(oldPort, value);
            }
        }

        /// <summary>
        /// The unique identifier of the source port.
        /// </summary>
        public virtual string FromPortId => m_FromPortReference.UniqueId;

        /// <summary>
        /// The unique identifier of the destination port.
        /// </summary>
        public virtual string ToPortId => m_ToPortReference.UniqueId;

        /// <summary>
        /// The unique identifier of the input node of the wire.
        /// </summary>
        public virtual Hash128 FromNodeGuid => m_FromPortReference.NodeModelGuid;

        /// <summary>
        /// The unique identifier of the output node of the wire.
        /// </summary>
        public virtual Hash128 ToNodeGuid => m_ToPortReference.NodeModelGuid;

        /// <summary>
        /// Whether the <see cref="WireBubbleText"/> property has been customized or is computed.
        /// </summary>
        public virtual bool HasCustomWireBubbleText
        {
            get => ! string.IsNullOrEmpty(m_WireBubbleText);
        }

        /// <summary>
        /// The text appearing in the wire's bubble.
        /// If not set, it will default to the wire order.
        /// </summary>
        public virtual string WireBubbleText
        {
            get => string.IsNullOrEmpty(m_WireBubbleText) ? GetWireOrderLabel() : m_WireBubbleText;
            set
            {
                if (m_WireBubbleText == value)
                    return;
                m_WireBubbleText = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        string GetWireOrderLabel()
        {
            var order = FromPort?.GetWireOrder(this) ?? -1;
            return order == -1 ? "" : (order + 1).ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WireModel"/> class.
        /// </summary>
        public WireModel()
        {
            // Not `Editor.Capabilities.Movable`, otherwise SelectionDragger can be used to move the wire UI around.
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Ascendable
            });
        }

        /// <summary>
        /// Notifies the graph model that a port has been updated.
        /// Derived implementations of <see cref="WireModel"/> must invoke this method whenever
        /// changes cause the values of <see cref="FromPort"/> or <see cref="ToPort"/> to differ
        /// from their previous states.
        /// </summary>
        /// <param name="oldPort">The previous port.</param>
        /// <param name="newPort">The new port.</param>
        protected void OnPortChanged(PortModel oldPort, PortModel newPort)
        {
            GraphModel?.UpdateWire(this, oldPort, newPort);
        }

        /// <summary>
        /// Sets the endpoints of the wire.
        /// </summary>
        /// <param name="toPortModel">The port where the wire goes.</param>
        /// <param name="fromPortModel">The port from which the wire originates.</param>
        public virtual void SetPorts(PortModel toPortModel, PortModel fromPortModel)
        {
            if (toPortModel?.NodeModel == null || fromPortModel.NodeModel == null)
                return;

            var oldFromPort = FromPort;
            var oldToPort = ToPort;

            FromPort = fromPortModel;
            ToPort = toPortModel;

            var fromIsDifferent = oldFromPort != fromPortModel;
            var toIsDifferent = oldToPort != toPortModel;
            var needOnConnection = fromIsDifferent || toIsDifferent;

            if (oldFromPort != null && oldToPort != null)
            {
                if (fromIsDifferent)
                {
                    oldFromPort.NodeModel.OnDisconnection(oldFromPort, oldToPort);
                }

                if (toIsDifferent)
                {
                    oldToPort.NodeModel.OnDisconnection(oldToPort, oldFromPort);
                }
            }

            // If either ports have changed, both end need to know about the new connection.
            if (needOnConnection)
            {
                toPortModel.NodeModel.OnConnection(toPortModel, fromPortModel);
                fromPortModel.NodeModel.OnConnection(fromPortModel, toPortModel);
            }
        }

        /// <summary>
        /// Sets the port of the wire on a specific side.
        /// </summary>
        /// <param name="side">The side of the wire where the port is to be set.</param>
        /// <param name="value">The port to set.</param>
        public virtual void SetPort(WireSide side, PortModel value)
        {
            PortModel oldPort;
            PortModel otherPort;
            if (side == WireSide.From)
            {
                if (value == FromPort)
                    return;
                oldPort = FromPort;
                otherPort = ToPort;
                FromPort = value;
            }
            else
            {
                if (value == ToPort)
                    return;
                oldPort = ToPort;
                otherPort = FromPort;
                ToPort = value;
            }

            // All ports must not be null to call OnConnection/OnDisconnection
            if (otherPort == null)
                return;

            if (oldPort != null)
            {
                oldPort.NodeModel.OnDisconnection(oldPort, otherPort);
            }

            if (value != null)
            {
                // Both end of the wire need to know about the new connection.
                value.NodeModel.OnConnection(value, otherPort);
                otherPort.NodeModel.OnConnection(otherPort, value);
            }

            GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
        }

        /// <summary>
        /// Sets the other side port of the wire.
        /// </summary>
        /// <param name="otherSide">The other side of the wire on which to set the port.</param>
        /// <param name="value">The new port the wire must have on the other side.</param>
        public virtual void SetOtherPort(WireSide otherSide, PortModel value) =>
            SetPort(otherSide.GetOtherSide(), value);

        /// <summary>
        /// Gets the port of a wire on a specific side.
        /// </summary>
        /// <param name="side">The side of the wire to get the port from.</param>
        /// <returns>The port connected to the side of the wire.</returns>
        public PortModel GetPort(WireSide side)
        {
            return side == WireSide.To ? ToPort : FromPort;
        }

        /// <summary>
        /// Gets the port of a wire on the other side.
        /// </summary>
        /// <param name="otherSide">The other side of the wire to get the port from.</param>
        /// <returns>The port connected to the other side of the wire.</returns>
        public PortModel GetOtherPort(WireSide otherSide) => GetPort(otherSide.GetOtherSide());

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{m_FromPortReference} -> {m_ToPortReference}";
        }

        /// <summary>
        /// Resets the port cache of the wire.
        /// </summary>
        /// <remarks>After that call, the <see cref="FromPort"/>
        /// and the <see cref="ToPort"/> will be resolved from the port reference data.</remarks>
        public void ResetPortCache()
        {
            m_FromPortModelCache = default;
            m_ToPortModelCache = default;

            m_FromPortReference.ResetCache();
            m_ToPortReference.ResetCache();
        }

        /// <summary>
        /// Updates the port references with the cached ports.
        /// </summary>
        public void UpdatePortFromCache()
        {
            if (m_FromPortModelCache == null || m_ToPortModelCache == null)
                return;

            m_FromPortReference.Assign(m_FromPortModelCache);
            m_ToPortReference.Assign(m_ToPortModelCache);
        }

        /// <summary>
        /// Creates a <see cref="LinkedNodesDependency"/> between the two nodes connected by the wire.
        /// </summary>
        /// <param name="linkedNodesDependency">The resulting dependency.</param>
        /// <param name="parentNodeModel">The node model considered as parent in the dependency.</param>
        /// <returns>True is a dependency was created, false otherwise.</returns>
        public virtual bool CreateDependency(out LinkedNodesDependency linkedNodesDependency,
            out AbstractNodeModel parentNodeModel)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = FromPort,
                ParentPort = ToPort,
            };
            parentNodeModel = ToPort.NodeModel;

            return true;
        }

        /// <summary>
        /// Creates missing ports in the case where the original ports are missing.
        /// </summary>
        /// <param name="inputNode">The node owning the missing input port.</param>
        /// <param name="outputNode">The node owning the missing output port.</param>
        /// <returns>A migration result pair for the input and output port migration.</returns>
        public virtual (PortMigrationResult, PortMigrationResult) AddMissingPorts(out AbstractNodeModel inputNode, out AbstractNodeModel outputNode)
        {
            PortMigrationResult inputResult;
            PortMigrationResult outputResult;

            inputNode = outputNode = null;
            if (ToPort == null)
            {
                inputResult = m_ToPortReference.AddMissingPort(PortDirection.Input, m_ToPortReference.PortOrientation) ?
                    PortMigrationResult.MissingPortAdded : PortMigrationResult.MissingPortFailure;

                inputNode = m_ToPortReference.NodeModel;
            }
            else
            {
                inputResult = PortMigrationResult.MissingPortNotNeeded;
            }

            if (FromPort == null)
            {
                outputResult = m_FromPortReference.AddMissingPort(PortDirection.Output, m_FromPortReference.PortOrientation) ?
                    PortMigrationResult.MissingPortAdded : PortMigrationResult.MissingPortFailure;

                outputNode = m_FromPortReference.NodeModel;
            }
            else
            {
                outputResult = PortMigrationResult.MissingPortNotNeeded;
            }

            return (inputResult, outputResult);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_FromPortReference.PortDirection == PortDirection.None)
            {
                m_FromPortReference.SetPortDirection(PortDirection.Output);
            }
            if (m_ToPortReference.PortDirection == PortDirection.None)
            {
                m_ToPortReference.SetPortDirection(PortDirection.Input);
            }

            ResetPortCache();
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.insertNodeItem,
            ContextualMenuHelpers.insertJunctionPointItem,
            ContextualMenuHelpers.convertToPortalsItem,
            ContextualMenuHelpers.deleteItem,
            ContextualMenuHelpers.reorderWireItem,
        };
    }
}
