// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Options for the port.
    /// </summary>
    [Flags]
    [UnityRestricted]
    internal enum PortModelOptions
    {
        /// <summary>
        /// No option set.
        /// </summary>
        None = 0,

        /// <summary>
        /// The port has no constant to set its value when not connected.
        /// </summary>
        NoEmbeddedConstant = 1 << 0,

        /// <summary>
        /// The port is hidden.
        /// </summary>
        Hidden = 1 << 1,

        /// <summary>
        /// The port is a node option.
        /// </summary>
        IsNodeOption = 1 << 2,

        /// <summary>
        /// Default port options.
        /// </summary>
        Default = None,
    }

    /// <summary>
    /// A model that represents a port in a node.
    /// </summary>
    [UnityRestricted]
    internal class PortModel : GraphElementModel, IHasTitle, IPort
    {
        /// <summary>
        /// The separator used in UniqueName for sub ports.
        /// </summary>
        public const char SubPortIdSeparator = '.';

        static StringBuilder s_LabelSuffixBuilder = new();

        string m_PortId;

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
        bool m_IsExpandable;
        PortOrientation m_Orientation;
        IReadOnlyList<Attribute> m_Attributes;
        IPolymorphicPortHandler m_PolymorphicPortHandler;

        List<PortModel> m_SubPorts = new List<PortModel>();

        Constant m_ComputedConstant;

        /// <summary>
        /// The node model that owns this port.
        /// </summary>
        public virtual PortNodeModel NodeModel
        {
            get => m_NodeModel;
            set => m_NodeModel = value;
        }

        /// <inheritdoc />
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public string Title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;
                var oldUniqueName = UniqueName;
                m_Title = value;
                if (UniqueName != oldUniqueName)
                {
                    OnUniqueNameChanged(oldUniqueName, UniqueName);
                }

                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The attributes used to convey information about the port, if any.
        /// </summary>
        public IReadOnlyList<Attribute> Attributes => m_Attributes;

        internal void SetAttributes(IReadOnlyList<Attribute> attributes)
        {
            if (m_Attributes == null && attributes == null || (m_Attributes != null && attributes != null && m_Attributes.Equals(attributes)))
                return;
            m_Attributes = attributes;
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.UIHints);
        }

        /// <summary>
        /// The port unique name.
        /// </summary>
        /// <remarks>The name only needs to be unique within inputs or outputs of a node. The UniqueName of a sub port is computed with the parent port UniqueName and the subPortId or portName, separated by <see cref="SubPortIdSeparator"/>.</remarks>
        public string UniqueName => ComputeUniqueName(m_PortId, Title, Guid, ParentPort?.UniqueName);

        /// <summary>
        /// The port explicit unique id. If specified, it will be used for the unique name.
        /// </summary>
        public string PortId
        {
            get => m_PortId;
            set
            {
                if (m_PortId == value)
                    return;
                ValidateId(m_Title, value);

                var oldUniqueName = UniqueName;
                m_PortId = value;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        internal static string ComputeUniqueName(string uniqueId, string title, Hash128 guid, string parentPortUniqueName)
        {
            var uniqueName = uniqueId ?? title ?? guid.ToString();
            if (parentPortUniqueName != null)
            {
                uniqueName = $"{parentPortUniqueName}{SubPortIdSeparator}{uniqueName}";
            }

            return uniqueName;
        }

        internal Constant ComputedConstant
        {
            get => m_ComputedConstant;
            set => m_ComputedConstant = value;
        }

        /// <inheritdoc />
        public override void SetGuid(Hash128 value)
        {
            var oldUniqueName = UniqueName;
            base.SetGuid(value);
            OnUniqueNameChanged(oldUniqueName, UniqueName);
        }

        /// <summary>
        /// The port options.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual PortModelOptions Options
        {
            get => m_Options;
            set
            {
                if (m_Options == value)
                    return;
                m_Options = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port type.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
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
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port direction (input, output, undetermined).
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
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
                OnDirectionChanged(oldDirection, value);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The orientation of the port (horizontal, vertical).
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual PortOrientation Orientation
        {
            get => m_Orientation;
            set
            {
                if (m_Orientation == value)
                    return;
                m_Orientation = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The capacity of the port in terms of connected wires.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual PortCapacity Capacity
        {
            get
            {
                if (m_PortCapacity != null)
                    return m_PortCapacity.Value;

                if (PortType == PortType.State)
                    return PortCapacity.Multi;

                // If not set, fallback to default behavior.
                return PortDataType != typeof(Untyped) && Direction == PortDirection.Input ? PortCapacity.Single : PortCapacity.Multi;
            }
            set
            {
                if (m_PortCapacity == value)
                    return;
                m_PortCapacity = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The port data type handle.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual TypeHandle DataTypeHandle
        {
            get => IsAutomatic ? PolymorphicPortHandler.ResolvedType : m_DataTypeHandle;
            set
            {
                if (m_DataTypeHandle == value)
                    return;
                m_DataTypeHandle = value;
                m_PortDataTypeCache = null;
                m_TooltipCache = null;
                if (IsPolymorphic && !IsAutomatic)
                    PolymorphicPortHandler.Unresolve();

                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
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
                    var t = DataTypeHandle.Resolve();
                    m_PortDataTypeCache = t == typeof(void) || t.ContainsGenericParameters ? typeof(Unknown) : t;
                }
                return m_PortDataTypeCache;
            }
        }

        /// <summary>
        /// Whether the port is polymorphic.
        /// </summary>
        /// <remarks>A polymorphic port is a flexible port that can accept multiple data types, allowing dynamic connections based on context.</remarks>
        public virtual bool IsPolymorphic => PolymorphicPortHandler != null;

        /// <summary>
        /// Whether the port is expandable.
        /// </summary>
        public bool IsExpandable => m_IsExpandable;

        /// <summary>
        /// Whether the port is polymorphic and its currently selected type is <see cref="TypeHandle.Automatic"/>.
        /// </summary>
        /// <remarks>Only polymorphic ports can have the type <see cref="TypeHandle.Automatic"/>, which automatically adjusts the port data type based on the connection.</remarks>
        public virtual bool IsAutomatic => PolymorphicPortHandler?.SelectedType == TypeHandle.Automatic;

        /// <summary>
        /// The polymorphic handler used to configure supported types and the currently selected type of the polymorphic port.
        /// </summary>
        /// <remarks>A polymorphic port has a list of supported data types that can be selected as its current data type.
        /// The list includes <see cref="TypeHandle.Automatic"/>, which allows the data type to change automatically based on the connection.</remarks>
        public virtual IPolymorphicPortHandler PolymorphicPortHandler
        {
            get => m_PolymorphicPortHandler;
            set
            {
                m_PolymorphicPortHandler = value;
                UpdateDatatypeHandler();
            }
        }

        /// <summary>
        /// The parent of this port when this port is a sub port.
        /// </summary>
        public PortModel ParentPort { get; private set; }

        /// <summary>
        /// The sub ports of this port.
        /// </summary>
        public IReadOnlyList<PortModel> SubPorts => m_SubPorts;

        /// <summary>
        /// Creates an instance of a <see cref="PortModel"/>.
        /// </summary>
        /// <param name="nodeModel">The node of this port.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="orientation">The orientation</param>
        /// <param name="portName">The name of the port.</param>
        /// <param name="portType">The type of port.</param>
        /// <param name="dataType">The <see cref="TypeHandle"/>.</param>
        /// <param name="portId">The id of this port.</param>
        /// <param name="options">The options.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="parentPort">The parent port if it is a sub port.</param>
        /// <remarks>
        /// This constructor creates an instance of a <see cref="PortModel"/>, which represents a port on a node. Ports are fundamental elements for connecting
        /// nodes in a graph, and this constructor allows you to define various properties of the port.
        /// </remarks>
        public PortModel(PortNodeModel nodeModel, PortDirection direction, PortOrientation orientation, string portName,
                         PortType portType, TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, PortModel parentPort)
        {
            ValidateId(portName, portId);

            var hash = ComputePortHash(nodeModel, direction, portName, portType, dataType, portId, parentPort);

            // Do not trigger the UniqueName dance.
            base.SetGuid(hash);
            m_PortId = portId;
            ParentPort = parentPort;

            m_Direction = direction;
            m_Orientation = orientation;
            m_PortType = portType;
            m_Title = portName ?? "";
            m_Options = options;
            m_NodeModel = nodeModel;
            m_Attributes = attributes;
            m_DataTypeHandle = dataType;

            // Avoid virtual call.
            base.GraphModel = nodeModel.GraphModel;
        }

        static void ValidateId(string portName, string portId)
        {
            if (portId == null && portName != null && portName.Contains(PortModel.SubPortIdSeparator))
            {
                throw new ArgumentException($"{PortModel.SubPortIdSeparator} is not a valid character in port names as it is used with sub ports. Use another name or specify a different port id.");
            }

            if (portId != null && portId.Contains(PortModel.SubPortIdSeparator))
            {
                throw new ArgumentException($"{PortModel.SubPortIdSeparator} is not a valid character in port ids as it is used with sub ports.");
            }
        }

        internal static Hash128 ComputePortHash(PortNodeModel nodeModel, PortDirection direction, string portName, PortType portType, TypeHandle dataType, string portId, PortModel parentPort)
        {
            var hash = new Hash128();
            var nodeGuid = nodeModel.Guid;
            hash.Append(nodeGuid);
            if (parentPort != null)
            {
                hash.Append(parentPort.Guid);
            }

            if (string.IsNullOrEmpty(portId))
            {
                hash.Append(portName ?? "");
                hash.Append(portType.Id);
                hash.Append(dataType.Identification);
            }
            else
            {
                hash.Append(portId);
            }

            hash.Append((int)direction);
            return hash;
        }

        /// <summary>
        /// Called when a wire is disconnected from one of the node's port
        /// </summary>
        /// <param name="otherPort">The port mode that was previously connected to this port</param>
        public virtual void OnDisconnection(PortModel otherPort)
        {
            if (IsAutomatic)
            {
                PolymorphicPortHandler.Unresolve();
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Called when a new wire connects this port to another port
        /// </summary>
        /// <param name="otherPort">The other port to which this port is connected to.</param>
        public virtual void OnConnection(PortModel otherPort)
        {
            if (IsAutomatic)
            {
                PolymorphicPortHandler.Resolve(otherPort.DataTypeHandle);
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Updates the port data type after the polymorphic port handle has changed.
        /// </summary>
        public void UpdateDatatypeHandler()
        {
            if (IsPolymorphic)
            {
                var previousType = DataTypeHandle;
                DataTypeHandle = PolymorphicPortHandler.SelectedType;
                if (!IsAutomatic)
                {
                    PolymorphicPortHandler.Unresolve();
                }
                else if (GetConnectedPorts() is { Count: > 0 } connectedPorts)
                {
                    PolymorphicPortHandler.Resolve(connectedPorts[0].DataTypeHandle);
                }

                if (previousType != DataTypeHandle)
                {
                    NodeModel.OnPortDataTypeChanged(this, previousType, DataTypeHandle);
                }
            }
        }

        /// <summary>
        /// Notifies the graph model that the port unique name has changed. Derived implementations of PortModel
        /// must call this method whenever a change makes <see cref="UniqueName"/> return a different value than before.
        /// </summary>
        /// <param name="oldUniqueName">The previous name.</param>
        /// <param name="newUniqueName">The new name.</param>
        protected virtual void OnUniqueNameChanged(string oldUniqueName, string newUniqueName)
        {
            GraphModel?.PortWireIndex.PortUniqueNameChanged(this, oldUniqueName, newUniqueName);
            NodeModel.OnPortUniqueNameChanged(this, oldUniqueName, newUniqueName);

            if (m_SubPorts != null && m_SubPorts.Count > 0)
            {
                foreach (var subPort in m_SubPorts)
                {
                    subPort.OnUniqueNameChanged(ComputeUniqueName(subPort.m_PortId, subPort.Title, subPort.Guid, oldUniqueName), subPort.UniqueName);
                }
            }

        }

        /// <summary>
        /// Notifies the graph model that the port direction has changed. Derived implementations of PortModel
        /// must call this method whenever a change makes <see cref="Direction"/> return a different value than before.
        /// </summary>
        /// <param name="oldDirection">The previous direction.</param>
        /// <param name="newDirection">The new direction.</param>
        protected virtual void OnDirectionChanged(PortDirection oldDirection, PortDirection newDirection)
        {
            GraphModel?.PortWireIndex.PortDirectionChanged(this, oldDirection, newDirection);
        }

        /// <summary>
        /// Tells if a port can be connected to this one, taking into account the polymorphic configuration
        /// </summary>
        /// <param name="otherPort">The port to which we want to know if it can be connected</param>
        /// <returns>True if it can be connected, false otherwise</returns>
        public bool CanConnectPort(PortModel otherPort)
        {
            if (DataTypeHandle.IsAssignableFrom(otherPort.DataTypeHandle))
            {
                return true;
            }
            if (IsAutomatic)
            {
                return PolymorphicPortHandler.CanConnect(otherPort.DataTypeHandle);
            }

            if (otherPort.IsAutomatic)
            {
                return otherPort.PolymorphicPortHandler.CanConnect(DataTypeHandle);
            }

            return false;
        }

        /// <summary>
        /// Gets the ports connected to this port.
        /// </summary>
        /// <returns>The ports connected to this port.</returns>
        public virtual IReadOnlyList<PortModel> GetConnectedPorts()
        {
            if (GraphModel == null)
                return Array.Empty<PortModel>();

            var wires = GetConnectedWires();
            var results = new List<PortModel>(wires.Count);

            for (var i = 0; i < wires.Count; i++)
            {
                var wire = wires[i];
                var port = wire.GetOtherPort(this);
                if (port != null)
                {
                    results.Add(port);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the wires connected to this port.
        /// </summary>
        /// <returns>The wires connected to this port.</returns>
        public virtual IReadOnlyList<WireModel> GetConnectedWires()
        {
            return GraphModel?.GetWiresForPort(this) ?? Array.Empty<WireModel>();
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

            var wireModels = GetConnectedWires();

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
        /// Checks whether this port has any connection.
        /// </summary>
        /// <returns>True if there is at least one wire connected on this port.</returns>
        public bool IsConnected()
        {
            return GetConnectedWires().Count > 0;
        }

        /// <summary>
        /// Checks whether this port is equivalent to another port.
        /// </summary>
        /// <param name="otherPortModel">The second port.</param>
        /// <returns>True if the two ports are owned by the same node, have the same direction and have the same unique name.</returns>
        public bool Equivalent(PortModel otherPortModel)
        {
            if (otherPortModel == null)
                return false;

            return Direction == otherPortModel.Direction && NodeModel.Guid == otherPortModel.NodeModel.Guid && UniqueName == otherPortModel.UniqueName;
        }

        /// <summary>
        /// Gets whether this port model has reorderable wires or not.
        /// </summary>
        public virtual bool HasReorderableWires => PortDataType == typeof(Untyped) && Direction == PortDirection.Output && IsConnected();

        /// <summary>
        /// A constant representing the port default value.
        /// </summary>
        public virtual Constant EmbeddedValue
        {
            get
            {
                if (m_ComputedConstant != null)
                {
                    return m_ComputedConstant;
                }
                if (Direction == PortDirection.Input && NodeModel is NodeModel node && node.InputConstantsById.TryGetValue(UniqueName, out var inputModel))
                {
                    return inputModel;
                }

                return null;
            }
        }

        /// <summary>
        /// The default tooltip for the port.
        /// </summary>
        /// <remarks>
        /// The default tooltip is "[name] [Input|Output] of type (friendly name of the port type)" for ports (e.g. "Input of type Float").
        /// </remarks>
        public virtual string DefaultTooltip
        {
            get
            {
                var portLabel = ComputePortLabel(true);
                if (!string.IsNullOrEmpty(portLabel))
                    portLabel += "\n";

                return portLabel
                    + (Direction == PortDirection.Output ? "Output" : "Input")
                    + $" Type: {DataTypeHandle.FriendlyName}";
            }
        }

        /// <summary>
        /// The tooltip for the port.
        /// </summary>
        /// <remarks>
        /// If the tooltip is not set, or if it's set to null, the default value for the tooltip will be returned.<br/>
        /// Setter implementations must set the <see cref="ChangeHint.Style"/> change hints.
        /// </remarks>
        public virtual string ToolTip
        {
            get
            {
                if (!string.IsNullOrEmpty(m_TooltipOverride))
                    return m_TooltipOverride;

                m_TooltipCache ??= DefaultTooltip;

                return m_TooltipCache;
            }

            set
            {
                if (m_TooltipOverride == value)
                    return;
                m_TooltipOverride = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <summary>
        /// Whether the port creates a default embedded constant.
        /// </summary>
        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Default;

        /// <summary>
        /// Whether this port is expanded.
        /// </summary>
        /// <remarks>Only ports with <see cref="PortModel.IsExpandable"/> can be expanded.</remarks>
        /// <seealso cref="PortModel.SetPortExpanded"/>
        public bool IsExpandedSelf { get; private set; }

        /// <summary>
        /// Whether all ancestors of this port are expanded.
        /// </summary>
        public bool AreAncestorsExpanded => ParentPort == null || (ParentPort.IsExpandedSelf && ParentPort.AreAncestorsExpanded);

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
            using var assetDirtyScope = GraphModel.AssetDirtyScope();
            GraphModel?.ReorderWire(wireModel, reorderType);
        }

        /// <summary>
        /// Gets the order of the wire on this port.
        /// </summary>
        /// <param name="wire">The wire for with to get the order.</param>
        /// <returns>The wire order.</returns>
        public virtual int GetWireOrder(WireModel wire)
        {
            return GetConnectedWires().IndexOf(wire);
        }

        /// <summary>
        /// Adds a sub port to this port.
        /// </summary>
        /// <param name="portModel">The sub port model.</param>
        /// <remarks>Users of the NodeModel class must not use this method directly, but rely on <see cref="NodeModel.AddSubPort"/> instead.</remarks>
        public void AddSubPort(PortModel portModel)
        {
            m_SubPorts.Add(portModel);
            portModel.ParentPort = this;
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Clears all sub ports.
        /// </summary>
        /// <remarks>Users of the NodeModel class must not use this method.</remarks>
        public void ClearSubPorts()
        {
            if (m_SubPorts.Count == 0)
                return;
            m_SubPorts.Clear();
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Sets the expanded state of a port.
        /// </summary>
        /// <param name="expanded">Whether the port is expanded of collapsed.</param>
        /// <remarks>Users of the NodeModel class must not use this method but rely on <see>
        ///         <cref>NodeModel.SetPortExpanded</cref>
        ///     </see>
        ///     instead.</remarks>
        public void SetPortExpanded(bool expanded)
        {
            IsExpandedSelf = expanded;
        }

        /// <summary>
        /// Checks if this port is a descendant of the given port.
        /// </summary>
        /// <param name="ancestor">The potential ancestor.</param>
        /// <returns>True if this port is a descendant of the given port. False otherwise.</returns>
        public bool IsDescendantOf(PortModel ancestor)
        {
            if (ParentPort == ancestor)
                return true;
            if (ParentPort == null)
                return false;

            return ParentPort.IsDescendantOf(ancestor);
        }

        /// <summary>
        /// Generates a descriptive label for the port, including its expandable ancestor ports if applicable.
        /// </summary>
        /// <param name="full">
        /// If <c>true</c>, includes all ancestor ports in the label; if <c>false</c>, includes only up to the nearest collapsed ancestor.
        /// </param>
        /// <returns>
        /// A string representing the port label, optionally including its ancestor hierarchy for context.
        /// </returns>
        internal string ComputePortLabel(bool full)
        {
            if (ParentPort == null)
                return Title;

            //search of the higher collapsed ancestor
            var current = ParentPort;
            PortModel higherCollapsedAncestor = null;
            while (current != null)
            {
                if (!current.IsExpandedSelf || full)
                    higherCollapsedAncestor = current;
                current = current.ParentPort;
            }

            if (higherCollapsedAncestor == null || (!full && higherCollapsedAncestor == ParentPort))
                return Title;

            //aggregate the ancestors
            using var poolHolder = ListPool<PortModel>.Get(out var ancestors);
            current = ParentPort;

            //Don't include the main port name for VariableNodes.
            while (current != null && (full || current != higherCollapsedAncestor) && (current.ParentPort != null || current.NodeModel is not VariableNodeModel))
            {
                ancestors.Add(current);
                current = current.ParentPort;
            }

            if (ancestors.Count == 0)
                return Title;

            // build the label
            s_LabelSuffixBuilder.Clear();
            s_LabelSuffixBuilder.Append(Title);
            s_LabelSuffixBuilder.Append(" (of ");
            for (int i = ancestors.Count - 1; i >= 0; i--)
            {
                s_LabelSuffixBuilder.Append(ancestors[i].Title);

                if (i > 0)
                    s_LabelSuffixBuilder.Append('/');
            }
            s_LabelSuffixBuilder.Append(")");

            return s_LabelSuffixBuilder.ToString();
        }

        internal void SetExpandable(bool expandable)
        {
            m_IsExpandable = expandable;
        }

        bool IPort.IsConnected => IsConnected();

        PortDirection IPort.Direction => Direction;

        string IPort.Name => PortId;

        string IPort.DisplayName => Title;

        string IPort.Tooltip => ToolTip;

        void IPort.GetConnectedPorts(List<IPort> outConnectedPorts)
        {
            outConnectedPorts.Clear();
            ApplyOnAllConnectedPorts(p =>
            {
                outConnectedPorts.Add(p);
                return true;
            });
        }

        bool ApplyOnAllConnectedPorts(Func<IPort, bool> predicate)
        {
            var wires = GetConnectedWires();

            for (var i = 0; i < wires.Count; i++)
            {
                var wire = wires[i];
                var port = wire.GetOtherPort(this);
                if (port == null)
                    return true;
                if (port.NodeModel is WirePortalModel portal)
                {
                    var declaration = portal.DeclarationModel;

                    foreach (var otherNode in (port.NodeModel is WirePortalEntryModel ? GraphModel.GetExitPortals(declaration) : GraphModel.GetEntryPortals(declaration)))
                    {
                        foreach (var portalPort in otherNode.GetPorts())
                        {
                            if (!portalPort.ApplyOnAllConnectedPorts(predicate))
                                return false;
                        }
                    }
                }
                else
                {
                    if (!predicate(port))
                        return false;
                }
            }

            return true;
        }

        IPort IPort.FirstConnectedPort
        {
            get
            {
                IPort first = null;
                ApplyOnAllConnectedPorts(p =>
                {
                    first = p;
                    return false;
                });

                return first;
            }
        }

        bool IPort.TryGetValue<T>(out T value)
        {
            if (EmbeddedValue == null || IsConnected())
            {
                value = default;
                return false;
            }
            return EmbeddedValue.TryGetValue(out value);
        }

        bool IPort.TrySetValue<T>(T value)
        {
            CheckModificationLock();

            if (EmbeddedValue == null || IsConnected())
                return false;

            return EmbeddedValue.TrySetValue(value);
        }

        Type IPort.DataType => PortDataType;

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
             ContextualMenuHelpers.addNodeFromPortItem,
             ContextualMenuHelpers.createVariableFromPortItem,
             ContextualMenuHelpers.copyValueItem,
             ContextualMenuHelpers.pasteValueItem,
             ContextualMenuHelpers.disconnectAllWiresItem,
             ContextualMenuHelpers.expandPortItem,
             ContextualMenuHelpers.collapsePortItem,
         };
    }
}
