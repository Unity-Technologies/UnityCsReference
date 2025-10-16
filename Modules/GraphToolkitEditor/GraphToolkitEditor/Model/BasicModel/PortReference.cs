// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Reference to a port by its unique id.
    /// </summary>
    [Serializable]
    struct PortReference : ISerializationCallbackReceiver, IEquatable<PortReference>
    {
        [SerializeField, Obsolete]
#pragma warning disable CS0618
        SerializableGUID m_NodeModelGuid;
#pragma warning restore CS0618

        [SerializeField]
        Hash128 m_NodeModelHashGuid;

        [SerializeField]
        string m_UniqueId;

        [SerializeField]
        PortDirection m_PortDirection;

        [SerializeField]
        PortOrientation m_PortOrientation;

        [SerializeField]
        string m_Title;

        public static string uniqueIdFieldName = nameof(m_UniqueId);
#pragma warning disable CS0612
        public static string obsoleteNodeModelGuidFieldName = nameof(m_NodeModelGuid);
#pragma warning restore CS0612
        public static string nodeModelGuidFieldName = nameof(m_NodeModelHashGuid);

        GraphModel m_GraphModel;

        AbstractNodeModel m_CachedNodeModel;

        /// <summary>
        /// The GUID of the node model that owns the port referenced by this instance.
        /// </summary>
        public Hash128 NodeModelGuid => m_NodeModelHashGuid;

        /// <summary>
        /// The unique id of the port referenced by this instance.
        /// </summary>
        public string UniqueId => m_UniqueId;

        public PortDirection PortDirection => m_PortDirection;

        public PortOrientation PortOrientation => m_PortOrientation;

        /// <summary>
        /// The title of the port referenced by this instance.
        /// </summary>
        public string Title => m_Title;

        /// <summary>
        /// The node model that owns the port referenced by this instance.
        /// </summary>
        public AbstractNodeModel NodeModel
        {
            get
            {
                if (m_CachedNodeModel == null)
                {
                    if (m_GraphModel != null && m_GraphModel.TryGetModelFromGuid(m_NodeModelHashGuid, out var node))
                    {
                        m_CachedNodeModel = node as AbstractNodeModel;
                    }
                }

                return m_CachedNodeModel;
            }

            private set
            {
                m_NodeModelHashGuid = value.Guid;
                m_CachedNodeModel = null;
            }
        }

        // for tests
        public void SetUniqueId(string value)
        {
            m_UniqueId = value;
        }

        // for tests
        public void SetTitle(string value)
        {
            m_Title = value;
        }

        // For test and to fix old versions where the port direction wasn't saved.
        public void SetPortDirection(PortDirection portDirection)
        {
            m_PortDirection = portDirection;
        }

        /// <summary>
        /// Sets the port that this instance references.
        /// </summary>
        /// <param name="portModel"></param>
        public void Assign(PortModel portModel)
        {
            Assert.IsNotNull(portModel);
            m_GraphModel = portModel.NodeModel.GraphModel;
            NodeModel = portModel.NodeModel;
            m_UniqueId = portModel.UniqueName;
            m_PortDirection = portModel.Direction;
            m_PortOrientation = portModel.Orientation;
            m_Title = portModel.Title;
        }

        /// <summary>
        /// Sets the graph model used to resolve the port reference.
        /// </summary>
        /// <remarks>The intended use of this method is to initialize the <see cref="m_GraphModel"/> after deserialization.</remarks>
        /// <param name="graphModel">The graph model in which this port reference lives.</param>
        public void AssignGraphModel(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        public PortModel GetPortModel(ref PortModel previousValue)
        {
            var nodeModel = NodeModel;
            if (nodeModel == null)
            {
                return previousValue = null;
            }

            // when removing a set property member, we patch the wires portIndex
            // the cached value needs to be invalidated
            if (previousValue != null && (previousValue.NodeModel == null || previousValue.NodeModel.Guid != nodeModel.Guid || previousValue.Direction != m_PortDirection))
            {
                previousValue = null;
            }

            if (previousValue != null)
                return previousValue;

            previousValue = null;

            AbstractNodeModel nodeModel2 = null;
            nodeModel.GraphModel?.TryGetModelFromGuid(nodeModel.Guid, out nodeModel2);
            if (nodeModel2 != nodeModel)
            {
                NodeModel = nodeModel2;
            }

            switch (nodeModel)
            {
                case InputOutputPortsNodeModel inputOutputPortsNodeModel:
                    var portModelsByGuid = m_PortDirection == PortDirection.Input ? inputOutputPortsNodeModel?.InputsById : inputOutputPortsNodeModel?.OutputsById;
                    if (portModelsByGuid != null && UniqueId != null)
                    {
                        if (portModelsByGuid.TryGetValue(UniqueId, out var v))
                            previousValue = v;
                    }

                    break;

                case PortNodeModel portNodeModel:
                    var portModels = portNodeModel.GetPorts();
                    foreach (var portModel in portModels)
                    {
                        if (portModel.Direction == m_PortDirection && portModel.UniqueName == UniqueId)
                        {
                            previousValue = portModel;
                            break;
                        }
                    }
                    break;
            }

            return previousValue;
        }

        /// <summary>
        /// Gets a string representation of this instance.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            var nodeString = NodeModel is IHasTitle titleNode && !string.IsNullOrEmpty(titleNode.Title)
                ? $"{titleNode.Title}({m_NodeModelHashGuid})"
                : m_NodeModelHashGuid.ToString();
            return $"{m_GraphModel.Guid.ToString()}:{nodeString}@{UniqueId}";
        }

        /// <summary>
        /// Adds a missing port on the node to represent this instance.
        /// </summary>
        /// <param name="direction">Direction of the port.</param>
        /// <param name="orientation">Orientation of the port. This value is horizontal by default.</param>
        /// <returns>True if the port was added.</returns>
        public bool AddMissingPort(PortDirection direction, PortOrientation orientation = PortOrientation.Horizontal)
        {
            if (NodeModel == null && m_GraphModel.TryGetModelFromGuid(NodeModelGuid, out var model) && model is NodePlaceholder missingNodeModel)
            {
                missingNodeModel.AddMissingPort(direction, UniqueId, orientation, Title);
                return true;
            }

            if (!(NodeModel is NodeModel n))
                return false;

            n.AddMissingPort(direction, UniqueId, orientation, Title);
            return true;
        }

        /// <summary>
        /// Resets internal cache.
        /// </summary>
        public void ResetCache()
        {
            m_CachedNodeModel = null;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
#pragma warning disable CS0612
            m_NodeModelGuid = m_NodeModelHashGuid;
#pragma warning restore CS0612
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
#pragma warning disable CS0612
            m_NodeModelHashGuid = m_NodeModelGuid;
#pragma warning restore CS0612
        }

        public bool Equals(PortReference other)
        {
            return m_NodeModelHashGuid.Equals(other.m_NodeModelHashGuid) && m_UniqueId == other.m_UniqueId && Equals(m_GraphModel, other.m_GraphModel);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is PortReference other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(m_NodeModelHashGuid, m_UniqueId, m_GraphModel);
        }

        public static bool operator ==(PortReference left, PortReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PortReference left, PortReference right)
        {
            return !left.Equals(right);
        }
    }
}
