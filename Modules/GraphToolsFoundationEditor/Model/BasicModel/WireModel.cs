// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
{
    enum PortMigrationResult
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
    enum WireSide
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
    /// A model that represents a wire in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class WireModel : GraphElementModel
    {
        [SerializeField, FormerlySerializedAs("m_OutputPortReference")]
        PortReference m_FromPortReference;

        [SerializeField, FormerlySerializedAs("m_InputPortReference")]
        PortReference m_ToPortReference;

        [FormerlySerializedAs("m_EdgeLabel")] [SerializeField]
        protected string m_WireLabel;

        internal static string fromPortReferenceFieldName_Internal = nameof(m_FromPortReference);
        internal static string toPortReferenceFieldName_Internal = nameof(m_ToPortReference);

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
            get => m_FromPortReference.GetPortModel(PortDirection.Output, ref m_FromPortModelCache);
            set
            {
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
            get => m_ToPortReference.GetPortModel(PortDirection.Input, ref m_ToPortModelCache);
            set
            {
                var oldPort = ToPort;
                oldPort?.NodeModel.RemoveUnusedMissingPort(oldPort);
                m_ToPortReference.Assign(value);
                m_ToPortModelCache = value;
                OnPortChanged(oldPort, value);
            }
        }

        /// <summary>
        /// The unique id of the originating port.
        /// </summary>
        public virtual string FromPortId => m_FromPortReference.UniqueId;

        /// <summary>
        /// The unique id of the destination port.
        /// </summary>
        public virtual string ToPortId => m_ToPortReference.UniqueId;

        /// <summary>
        /// The unique identifier of the input node of the wire.
        /// </summary>
        public virtual SerializableGUID FromNodeGuid => m_FromPortReference.NodeModelGuid;

        /// <summary>
        /// The unique identifier of the output node of the wire.
        /// </summary>
        public virtual SerializableGUID ToNodeGuid => m_ToPortReference.NodeModelGuid;

        /// <summary>
        /// The label of the wire.
        /// </summary>
        public virtual string WireLabel
        {
            get => string.IsNullOrEmpty(m_WireLabel) ? GetWireOrderLabel() : m_WireLabel;
            set
            {
                if (m_WireLabel == value)
                    return;
                m_WireLabel = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
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
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Movable
            });
        }

        /// <summary>
        /// Notifies the graph model that one of the ports has changed. Derived implementations of WireModel
        /// should call this method whenever a change makes <see cref="FromPort"/> or <see cref="ToPort"/> return
        /// a different value than before.
        /// </summary>
        /// <param name="oldPort">The previous port.</param>
        /// <param name="newPort">The new port.</param>
        protected void OnPortChanged(PortModel oldPort, PortModel newPort)
        {
            GraphModel?.UpdateWire_Internal(this, oldPort, newPort);
        }

        /// <summary>
        /// Sets the endpoints of the wire.
        /// </summary>
        /// <param name="toPortModel">The port where the wire goes.</param>
        /// <param name="fromPortModel">The port from which the wire originates.</param>
        public virtual void SetPorts(PortModel toPortModel, PortModel fromPortModel)
        {
            Assert.IsNotNull(toPortModel);
            Assert.IsNotNull(toPortModel.NodeModel);
            Assert.IsNotNull(fromPortModel);
            Assert.IsNotNull(fromPortModel.NodeModel);

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
        /// <param name="side">The side of the wire on which to set the port.</param>
        /// <param name="value">The new port the wire should have.</param>
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
        }

        /// <summary>
        /// Sets the other side port of the wire.
        /// </summary>
        /// <param name="otherSide">The other side of the wire on which to set the port.</param>
        /// <param name="value">The new port the wire should have on the other side.</param>
        public virtual void SetOtherPort(WireSide otherSide, PortModel value) =>
            SetPort(otherSide.GetOtherSide(), value);

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
                inputResult = m_ToPortReference.AddMissingPort(PortDirection.Input) ?
                    PortMigrationResult.MissingPortAdded : PortMigrationResult.MissingPortFailure;

                inputNode = m_ToPortReference.NodeModel;
            }
            else
            {
                inputResult = PortMigrationResult.MissingPortNotNeeded;
            }

            if (FromPort == null)
            {
                outputResult = m_FromPortReference.AddMissingPort(PortDirection.Output) ?
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
            ResetPortCache();
        }
    }
}
