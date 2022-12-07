// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The number of connections a port can accept.
    /// </summary>
    enum PortCapacity
    {
        /// <summary>
        /// The port cannot accept any connection.
        /// </summary>
        None,

        /// <summary>
        /// The port can only accept a single connection.
        /// </summary>
        Single,

        /// <summary>
        /// The port can accept multiple connections.
        /// </summary>
        Multi
    }

    /// <summary>
    /// Options for the port.
    /// </summary>
    [Flags]
    enum PortModelOptions
    {
        /// <summary>
        /// No option set.
        /// </summary>
        None = 0,

        /// <summary>
        /// The port has no constant to set its value when not connected.
        /// </summary>
        NoEmbeddedConstant = 1,

        /// <summary>
        /// The port is hidden.
        /// </summary>
        Hidden = 2,

        /// <summary>
        /// Default port options.
        /// </summary>
        Default = None,
    }

    /// <summary>
    /// A model that represents a port in a node.
    /// </summary>
    class PortModel : GraphElementModel, IHasTitle
    {
        static readonly IReadOnlyList<WireModel> k_EmptyWireList = new List<WireModel>();

        string m_UniqueId;

        string m_Title;
        PortType m_PortType;
        TypeHandle m_DataTypeHandle;
        PortDirection m_Direction;

        PortCapacity? m_PortCapacity;

        Type m_PortDataTypeCache;

        string m_TooltipCache;
        string m_TooltipOverride;
        PortNodeModel m_NodeModel;
        PortModelOptions m_Options;
        PortOrientation m_Orientation;

        /// <summary>
        /// The node model that owns this port.
        /// </summary>
        public virtual PortNodeModel NodeModel
        {
            get => m_NodeModel;
            protected set => m_NodeModel = value;
        }

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;
                var oldUniqueName = UniqueName;
                m_Title = value;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <summary>
        /// The port unique name.
        /// </summary>
        /// <remarks>The name only needs to be unique within a node.</remarks>
        public virtual string UniqueName
        {
            get => m_UniqueId ?? Title ?? Guid.ToString();
            set
            {
                if (m_UniqueId == value)
                    return;
                var oldUniqueName = UniqueName;
                m_UniqueId = value;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override void SetGuid(SerializableGUID value)
        {
            var oldUniqueName = UniqueName;
            base.SetGuid(value);
            OnUniqueNameChanged(oldUniqueName, UniqueName);
        }

        /// <summary>
        /// Port options.
        /// </summary>
        public virtual PortModelOptions Options
        {
            get => m_Options;
            set
            {
                if (m_Options == value)
                    return;
                m_Options = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port type (data, execution, etc.).
        /// </summary>
        public virtual PortType PortType
        {
            get => m_PortType;
            set
            {
                if (m_PortType == value)
                    return;
                m_PortType = value;
                // Invalidate cache.
                m_TooltipCache = null;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port direction (input, output, undetermined).
        /// </summary>
        public virtual PortDirection Direction
        {
            get => m_Direction;
            set
            {
                if (m_Direction == value)
                    return;
                var oldDirection = m_Direction;
                m_Direction = value;
                // Invalidate cache.
                m_TooltipCache = null;
                OnDirectionChanged(oldDirection,value);
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The orientation of the port (horizontal, vertical).
        /// </summary>
        public virtual PortOrientation Orientation
        {
            get => m_Orientation;
            set
            {
                if (m_Orientation == value)
                    return;
                m_Orientation = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The capacity of the port in term of connected wires.
        /// </summary>
        public virtual PortCapacity Capacity
        {
            get
            {
                if (m_PortCapacity != null)
                    return m_PortCapacity.Value;

                // If not set, fallback to default behavior.
                return PortType == PortType.Data && Direction == PortDirection.Input ? PortCapacity.Single : PortCapacity.Multi;
            }
            set
            {
                if (m_PortCapacity == value)
                    return;
                m_PortCapacity = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port data type handle.
        /// </summary>
        public virtual TypeHandle DataTypeHandle
        {
            get => m_DataTypeHandle;
            set
            {
                if (m_DataTypeHandle == value)
                    return;
                m_DataTypeHandle = value;
                m_PortDataTypeCache = null;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port data type.
        /// </summary>
        public virtual Type PortDataType
        {
            get
            {
                if (m_PortDataTypeCache == null)
                {
                    Type t = DataTypeHandle.Resolve();
                    m_PortDataTypeCache = t == typeof(void) || t.ContainsGenericParameters ? typeof(Unknown) : t;
                }
                return m_PortDataTypeCache;
            }
        }

        public PortModel(PortNodeModel nodeModel, PortDirection direction, PortOrientation orientation, string portName,
            PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            var hash = new Hash128();
            var gparts = nodeModel.Guid.ToParts();
            hash.Append((int)(gparts.Item1 & uint.MaxValue));
            hash.Append((int)(gparts.Item1 >> 32));
            hash.Append((int)(gparts.Item2 & uint.MaxValue));
            hash.Append((int)(gparts.Item2 >> 32));

            if (string.IsNullOrEmpty(portId))
            {
                hash.Append(portName ?? "");
                hash.Append((int)direction);
                hash.Append(portType.Id);
                hash.Append(dataType.Identification);
            }
            else
            {
                hash.Append(portId);
            }

            // Do not trigger the UniqueName dance.
            base.SetGuid(hash);
            m_UniqueId = portId;

            m_Direction = direction;
            m_Orientation = orientation;
            m_PortType = portType;
            m_DataTypeHandle = dataType;
            m_Title = portName ?? "";
            m_Options = options;
            m_NodeModel = nodeModel;
            // Avoid virtual call.
            base.GraphModel = nodeModel.GraphModel;
        }

        /// <summary>
        /// Notifies the graph model that the port unique name has changed. Derived implementations of PortModel
        /// should call this method whenever a change makes <see cref="UniqueName"/> return a different value than before.
        /// </summary>
        /// <param name="oldUniqueName">The previous name.</param>
        /// <param name="newUniqueName">The new name.</param>
        protected virtual void OnUniqueNameChanged(string oldUniqueName, string newUniqueName)
        {
            GraphModel?.PortWireIndex_Internal.UpdatePortUniqueName(this, oldUniqueName, newUniqueName);
        }

        /// <summary>
        /// Notifies the graph model that the port direction has changed. Derived implementations of PortModel
        /// should call this method whenever a change makes <see cref="Direction"/> return a different value than before.
        /// </summary>
        /// <param name="oldDirection">The previous direction.</param>
        /// <param name="newDirection">The new direction.</param>
        protected virtual void OnDirectionChanged(PortDirection oldDirection, PortDirection newDirection)
        {
            GraphModel?.PortWireIndex_Internal.UpdatePortDirection(this, oldDirection, newDirection);
        }

        /// <summary>
        /// Gets the ports connected to this port.
        /// </summary>
        /// <returns>The ports connected to this port.</returns>
        public virtual IEnumerable<PortModel> GetConnectedPorts()
        {
            if (GraphModel == null)
                return Enumerable.Empty<PortModel>();

            return GraphModel.GetWiresForPort(this)
                .Select(e => Direction == PortDirection.Input ? e.FromPort : e.ToPort)
                .Where(p => p != null);
        }

        /// <summary>
        /// Gets the wires connected to this port.
        /// </summary>
        /// <returns>The wires connected to this port.</returns>
        public virtual IReadOnlyList<WireModel> GetConnectedWires()
        {
            return GraphModel?.GetWiresForPort(this) ?? k_EmptyWireList;
        }

        /// <summary>
        /// Checks whether two ports are connected.
        /// </summary>
        /// <param name="otherPort">The second port.</param>
        /// <returns>True if there is at least one wire that connects the two ports.</returns>
        public bool IsConnectedTo(PortModel otherPort)
        {
            if (GraphModel == null)
                return false;

            var wireModels = GraphModel.GetWiresForPort(this);

            foreach (var wireModel in wireModels)
            {
                if (wireModel.ToPort == otherPort || wireModel.FromPort == otherPort)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets whether this port model has reorderable wires or not.
        /// </summary>
        public virtual bool HasReorderableWires => PortType == PortType.Execution && Direction == PortDirection.Output && this.IsConnected();

        /// <summary>
        /// A constant representing the port default value.
        /// </summary>
        public virtual Constant EmbeddedValue
        {
            get
            {
                if (NodeModel is NodeModel node && node.InputConstantsById.TryGetValue(UniqueName, out var inputModel))
                {
                    return inputModel;
                }

                return null;
            }
        }

        /// <summary>
        /// The tooltip for the port.
        /// </summary>
        /// <remarks>
        /// If the tooltip is not set, or if it's set to null, the default value for the tooltip will be returned.
        /// The default tooltip is "[Input|Output] execution flow" for execution ports (e.g. "Output execution flow"
        /// and "[Input|Output] of type (friendly name of the port type)" for data ports (e.g. "Input of type Float").
        /// </remarks>
        public virtual string ToolTip
        {
            get
            {
                if (m_TooltipOverride != null)
                    return m_TooltipOverride;

                if (m_TooltipCache == null)
                {
                    var newTooltip = new StringBuilder(Direction == PortDirection.Output ? "Output" : "Input");
                    if (PortType == PortType.Execution)
                    {
                        newTooltip.Append(" execution flow");
                    }
                    else if (PortType == PortType.Data)
                    {
                        var stencil = GraphModel.Stencil;
                        newTooltip.Append($" of type {DataTypeHandle.GetMetadata(stencil).FriendlyName}");
                    }

                    m_TooltipCache = newTooltip.ToString();
                }

                return m_TooltipCache;
            }

            set
            {
                if (m_TooltipOverride == value)
                    return;
                m_TooltipOverride = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <summary>
        /// Should the port create a default embedded constant.
        /// </summary>
        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        /// <summary>
        /// Changes the order of a wire among its siblings.
        /// </summary>
        /// <param name="wireModel">The wire to move.</param>
        /// <param name="reorderType">The type of move to do.</param>
        public virtual void ReorderWire(WireModel wireModel, ReorderType reorderType)
        {
            GraphModel?.ReorderWire_Internal(wireModel, reorderType);
        }

        /// <summary>
        /// Gets the order of the wire on this port.
        /// </summary>
        /// <param name="wire">The wire for with to get the order.</param>
        /// <returns>The wire order.</returns>
        public virtual int GetWireOrder(WireModel wire)
        {
            return GetConnectedWires().IndexOf_Internal(wire);
        }
    }
}
