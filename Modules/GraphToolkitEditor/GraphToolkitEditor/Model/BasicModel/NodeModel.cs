// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base model that represents a dynamically defined node.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class NodeModel : InputOutputPortsNodeModel, ICollapsible
    {
        /// <summary>
        /// Scope for defining the node in the <see cref="NodeModel.OnDefineNode"/> method. Provides methods to instantiate ports to the node.
        /// </summary>
        [UnityRestricted]
        internal class NodeDefinitionScope : IPortsDefinition, IOptionsDefinition
        {
            protected readonly NodeModel m_NodeModel;

            /// <summary>
            /// Creates an instance of a <see cref="NodeDefinitionScope"/>.
            /// </summary>
            /// <param name="nodeModel">The node that is being defined.</param>
            public NodeDefinitionScope(NodeModel nodeModel)
            {
                m_NodeModel = nodeModel;
            }

            /// <summary>
            /// Adds a new input port on the node.
            /// </summary>
            /// <param name="portName">The name of port to create.</param>
            /// <param name="portType">The type of port to create.</param>
            /// <param name="dataType">The type of data the port to create handles.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="options">The options for the port to create.</param>
            /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
            /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed for the port) to be called right after the port is created.</param>
            /// <param name="setterAction">A callback method called after a node option's constant has been set.</param>
            /// <returns>The newly created input port.</returns>
            public virtual PortModel AddInputPort(string portName, TypeHandle dataType, PortType portType = null,
                string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
                PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
            {
                return m_NodeModel.AddInputPort(portName, dataType, portType, portId, orientation, options, attributes, initializationCallback, setterAction);
            }

            /// <summary>
            /// Adds a new output port on the node.
            /// </summary>
            /// <param name="portName">The name of port to create.</param>
            /// <param name="dataType">The type of data the port to create handles.</param>
            /// <param name="portType">The type of port to create.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="options">The options for the port to create.</param>
            /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
            /// <returns>The newly created output port.</returns>
            public virtual PortModel AddOutputPort(string portName, TypeHandle dataType, PortType portType = null,
                string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
                PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
            {
                return m_NodeModel.AddOutputPort(portName, dataType, portType, portId, orientation, options, attributes);
            }

            /// <summary>
            /// Adds a new input port on the node.
            /// </summary>
            /// <param name="portName">The name of port to create.</param>
            /// <param name="portType">The type of port to create.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="options">The options for the port to create.</param>
            /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
            /// <param name="defaultValue">The default value to assign to the constant associated to the port.</param>
            /// <param name="setterAction">A callback method called after a node option's constant has been set.</param>
            /// <typeparam name="TDataType">The type of data the port to create handles.</typeparam>
            /// <returns>The newly created input port.</returns>
            public PortModel AddInputPort<TDataType>(string portName, PortType portType = null,
                string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
                PortModelOptions options = PortModelOptions.Default,
                Attribute[] attributes = null, TDataType defaultValue = default, Action<object> setterAction = null)
            {
                Action<Constant> initializationCallback = null;

                if (defaultValue is Enum || !EqualityComparer<TDataType>.Default.Equals(defaultValue, default))
                    initializationCallback = constantModel => constantModel.ObjectValue = defaultValue;

                return AddInputPort(portName, typeof(TDataType).GenerateTypeHandle(), portType, portId, orientation, options, attributes, initializationCallback, setterAction);
            }

            /// <summary>
            /// Adds a new data input port with no connector on a node.
            /// </summary>
            /// <param name="portName">The name of port to create.</param>
            /// <param name="dataType">The type of data the port to create handles.</param>
            /// <param name="portType">The type of port to create.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="options">The options for the port to create.</param>
            /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
            /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed
            /// for the port) to be called right after the port is created.</param>
            /// <param name="setterAction">A callback method called after a node option's constant has been set.</param>
            /// <returns>The newly created data input port with no connector.</returns>
            public PortModel AddNoConnectorInputPort(string portName, TypeHandle dataType, PortType portType = null,
                string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
                PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
            {
                var portModel = AddInputPort(portName, dataType, portType, portId, orientation, options, attributes, initializationCallback, setterAction);
                portModel.Capacity = PortCapacity.None;
                return portModel;
            }

            /// <summary>
            /// Adds a new data input port with no connector on a node.
            /// </summary>
            /// <param name="portName">The name of port to create.</param>
            /// <param name="portType">The type of port to create.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="options">The options for the port to create.</param>
            /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
            /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed
            /// for the port) to be called right after the port is created.</param>
            /// <param name="setterAction">A callback method called after a node option's constant has been set.</param>
            /// <typeparam name="TDataType">The type of data the port to create handles.</typeparam>
            /// <returns>The newly created data input port with no connector.</returns>
            public PortModel AddNoConnectorInputPort<TDataType>(string portName, PortType portType = null,
                string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
                PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
            {
                return AddNoConnectorInputPort(portName, typeof(TDataType).GenerateTypeHandle(), portType, portId, orientation, options, attributes, initializationCallback, setterAction);
            }

            /// <summary>
            /// Adds a new output port on the node.
            /// </summary>
            /// <param name="portName">The name of port to create.</param>
            /// <param name="portType">The type of port to create.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="options">The options for the port to create.</param>
            /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
            /// <typeparam name="TDataType">The type of data the port to create handles.</typeparam>
            /// <returns>The newly created output port.</returns>
            public PortModel AddOutputPort<TDataType>(string portName, PortType portType = null,
                string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
                PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
            {
                return AddOutputPort(portName, typeof(TDataType).GenerateTypeHandle(), portType, portId, orientation, options, attributes);
            }

            /// <summary>
            /// Adds a node option to the node.
            /// </summary>
            /// <param name="optionName">The name of the node option.</param>
            /// <param name="dataType">The type of the node option.</param>
            /// <param name="optionId">The unique id of the node option.</param>
            /// <param name="tooltip">The tooltip to show on the node option, if any.</param>
            /// <param name="showInInspectorOnly">Whether the node option is only shown in the inspector. By default, it is shown in both the node and in the inspector.</param>
            /// <param name="order">The order in which the option will be displayed among the other node options.</param>
            /// <param name="attributes">The attributes used to convey information about the node option, if any.</param>
            /// <param name="initializationCallback">An initialization method for the associated constant to be called right after the node option is created.</param>
            /// <param name="setterAction">A callback method called after a node option's constant has been set.</param>
            /// <returns>The newly added option.</returns>
            /// <remarks>Provides a way to create a node option without the use of the <see cref="NodeOptionAttribute"/>.</remarks>
            public NodeOption AddNodeOption(string optionName, TypeHandle dataType, string optionId = null, string tooltip = null, bool showInInspectorOnly = false, int order = 0, Attribute[] attributes = null, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
            {
                if (dataType.Resolve() == typeof(Unknown) || dataType.Resolve() == typeof(Untyped) || dataType.Resolve() == typeof(MissingPort) || dataType == TypeHandle.MissingType)
                    throw new ArgumentException("Invalid type for node option");

                optionId ??= optionName;

                var portId = $"{NodeOption.k_OptionIdPrefix}{optionId}";

                // Now constants for NodeOptions have NodeOption.k_OptionIdPrefix in their id. We need to migrate constants with no prefix to the new id.
                if (!m_NodeModel.m_NodeOptionConstantsMigrated && !m_NodeModel.m_InputConstantsById.ContainsKey(portId))
                {
                    if (m_NodeModel.m_InputConstantsById.Remove(optionId, out var oldConstant))
                    {
                        m_NodeModel.m_InputConstantsById.Add(portId, oldConstant);
                    }
                }

                // A node option consists in a no connector port with extra info. We add a prefix to avoid id conflicts with regular ports.
                var noConnectorPort = AddNoConnectorInputPort(optionName, dataType, PortType.Default, portId, PortOrientation.Horizontal, PortModelOptions.IsNodeOption, attributes, initializationCallback, setterAction);

                if (!string.IsNullOrEmpty(tooltip))
                    noConnectorPort.ToolTip = tooltip;

                var nodeOption = new NodeOption(optionId, noConnectorPort, showInInspectorOnly, order);
                m_NodeModel.AddNodeOption(nodeOption);
                return nodeOption;
            }

            /// <summary>
            /// Adds a new missing port on a node.
            /// </summary>
            /// <param name="direction">The direction of the port the create.</param>
            /// <param name="portId">The ID of the port to create.</param>
            /// <param name="orientation">The orientation of the port to create.</param>
            /// <param name="portName">The name of the port to create.</param>
            /// <returns>The newly created port.</returns>
            public PortModel AddMissingPort(PortDirection direction, string portId,
                PortOrientation orientation = PortOrientation.Horizontal, string portName = null)
            {
                return m_NodeModel.AddMissingPort(direction, portId, orientation, portName);
            }

            IPort IPortsDefinition.AddInputPort(string portName, Type dataType, string portId, PortOrientation orientation, Attribute[] attributes, object defaultValue)
            {
                Action<Constant> initializationCallback = null;
                if (defaultValue != null)
                    initializationCallback = c => c.ObjectValue = defaultValue;
                return AddInputPort(portName, dataType?.GenerateTypeHandle() ?? TypeHandle.Untyped, null, portId, orientation, PortModelOptions.Default, attributes, initializationCallback);
            }

            IPort IPortsDefinition.AddOutputPort(string portName, Type dataType, string portId, PortOrientation orientation, Attribute[] attributes)
            {
                return AddOutputPort(portName, dataType?.GenerateTypeHandle() ?? TypeHandle.Untyped, null, portId, orientation, PortModelOptions.Default, attributes);
            }

            public INodeOption AddNodeOption(string optionName, Type dataType, string optionDisplayName = null, string tooltip = null,
                bool showInInspectorOnly = false, int order = 0, Attribute[] attributes = null, object defaultValue = null)
            {
                Action<Constant> initializationCallback = null;
                if (defaultValue != null)
                    initializationCallback = c => c.ObjectValue = defaultValue;
                return AddNodeOption(optionDisplayName ?? optionName, dataType.GenerateTypeHandle(), optionDisplayName != null ? optionName : null, tooltip, showInInspectorOnly, order, attributes, initializationCallback, _ =>
                {
                    if (!m_NodeModel.m_InDefineNode)
                        m_NodeModel.DefineNode();
                });
            }
        }

        /// <summary>
        /// Scope for defining sub ports. Provides methods to instantiate, configure and add sub ports within the context of a parent port.
        /// </summary>
        class SubPortDefinition : ISubPortDefinition
        {
            readonly NodeModel m_NodeModel;
            bool m_MustSpecifySubPorts;

            /// <summary>
            /// Creates an instance of a  <see cref="SubPortDefinition"/>.
            /// </summary>
            /// <param name="nodeModel">The node that contains the sub ports.</param>
            public SubPortDefinition(NodeModel nodeModel)
            {
                m_NodeModel = nodeModel;
            }

            /// <summary>
            /// The port that is the parent to the sub ports.
            /// </summary>
            public PortModel ParentPort;

            /// <summary>
            /// The ports that were added subsequently to the sub port definition.
            /// </summary>
            public List<PortModel> AddedPorts;

            /// <inheritdoc />
            public bool MustSpecifySubPorts => m_MustSpecifySubPorts;

            /// <inheritdoc />
            public PortModel AddInputSubPort(string portName, TypeHandle typeHandle, Func<object> getter, Action<object> setter, string portId = null, PortModelOptions options = PortModelOptions.None, Attribute[] attributes = null)
            {
                var port = m_NodeModel.AddInputSubPort(ParentPort, portName, typeHandle, getter, setter, portId, options, attributes);
                AddedPorts?.Add(port);
                return port;
            }

            /// <inheritdoc />
            public PortModel AddInputSubPort<TDataType>(string portName, Func<TDataType> getter, Action<TDataType> setter, string portId = null, PortModelOptions options = PortModelOptions.None, Attribute[] attributes = null)
            {
                var port = m_NodeModel.AddInputSubPort(ParentPort, portName, getter, setter, portId, options, attributes);
                AddedPorts?.Add(port);
                return port;
            }

            /// <inheritdoc />
            public PortModel AddFieldSubPort(FieldInfo fieldInfo, string portName = null, string portId = null, PortModelOptions options = PortModelOptions.None, Attribute[] attributes = null)
            {
                var port = m_NodeModel.AddFieldSubPort(ParentPort, fieldInfo, portName, portId, options, attributes);
                AddedPorts?.Add(port);
                return port;
            }

            /// <inheritdoc />
            public PortModel AddPropertySubPort(PropertyInfo propertyInfo, string portName = null, string portId = null, PortModelOptions options = PortModelOptions.None, Attribute[] attributes = null)
            {
                var port = m_NodeModel.AddPropertySubPort(ParentPort, propertyInfo, portName, portId, options, attributes);
                AddedPorts?.Add(port);
                return port;
            }

            /// <inheritdoc />
            public PortModel AddSubPort(string portName, TypeHandle dataType, string portId = null, PortModelOptions options = PortModelOptions.None, Attribute[] attributes = null)
            {
                var port = m_NodeModel.AddSubPort(ParentPort, portName, dataType, portId, options, attributes);
                AddedPorts?.Add(port);
                return port;
            }

            /// <inheritdoc />
            public PortModel AddSubPort<TDataType>(string portName, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
            {
                var port = m_NodeModel.AddSubPort(ParentPort, portName, typeof(TDataType).GenerateTypeHandle(), portId, options, attributes);
                AddedPorts?.Add(port);
                return port;
            }

            internal void RefreshMustSpecifySubPorts()
            {
                m_MustSpecifySubPorts = ParentPort.IsExpandedSelf && ParentPort.AreAncestorsExpanded || RecurseHasWire(ParentPort);
                bool RecurseHasWire(PortModel portModel)
                {
                    return portModel.SubPorts.HasAny(sp => sp.IsConnected() || RecurseHasWire(sp));
                }
            }
        }

        [SerializeField, HideInInspector]
        SerializedReferenceDictionary<string, Constant> m_InputConstantsById;

        [Serializable]
        struct PortInfos
        {
            [NonSerialized]
            public OrderedPorts portsById;

            [NonSerialized]
            public OrderedPorts previousPorts;

            [NonSerialized]
            public List<PortModel> orderedVisiblePorts;

            [SerializeField, HideInInspector]
            public SerializedValueDictionary<string, bool> expandedPortsById;

            /// <summary>
            /// Initializes the <see cref="PortInfos"/> with default values.
            /// </summary>
            public void Initialize()
            {
                portsById = new OrderedPorts();
                orderedVisiblePorts = new List<PortModel>();
                expandedPortsById = new SerializedValueDictionary<string, bool>();
                previousPorts = null;
            }
        }

        [SerializeField, HideInInspector]
        PortInfos m_InputPortInfos;

        [SerializeField, HideInInspector]
        PortInfos m_OutputPortInfos;

        string m_IconTypeString;

        [SerializeField, HideInInspector]
        bool m_Collapsed;

        [SerializeField, HideInInspector]
        int m_CurrentModeIndex;

        [SerializeField, HideInInspector]
        protected ElementColor m_ElementColor;

        bool m_InDefineNode;

        SubPortDefinition m_SubPortDefinition;

        // indicates whether we have migrated node option constants to have the correct id (with the NodeOption.k_OptionIdPrefix prefix).
        [NonSerialized]
        bool m_NodeOptionConstantsMigrated;

        /// <inheritdoc />
        public override string IconTypeString
        {
            get
            {
                if (string.IsNullOrEmpty(m_IconTypeString))
                    m_IconTypeString = GetDefaultIconTypeString();
                return m_IconTypeString;
            }
            set { }
        }

        /// <summary>
        /// Whether the node must have the same icon as its corresponding item in the item library.
        /// </summary>
        public virtual bool HasSameIconAsItemLibrary => true;

        /// <inheritdoc />
        public override bool AllowSelfConnect => false;

        /// <inheritdoc />
        public override bool HasNodePreview => false;

        /// <inheritdoc />
        public override ElementColor ElementColor => m_ElementColor;

        /// <inheritdoc />
        public override bool UseColorAlpha => true;

        /// <summary>
        /// Whether the node can have expandable ports.
        /// </summary>
        public virtual bool CanHaveExpandablePorts => true;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, PortModel> InputsById => m_InputPortInfos.portsById;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, PortModel> OutputsById => m_OutputPortInfos.portsById;

        /// <summary>
        /// The previous value of <see cref="InputsById"/>.
        /// </summary>
        protected IReadOnlyDictionary<string, PortModel> PreviousInputsById => m_InputPortInfos.previousPorts;

        /// <summary>
        /// The previous value of <see cref="OutputsById"/>.
        /// </summary>
        protected IReadOnlyDictionary<string, PortModel> PreviousOutputsById => m_OutputPortInfos.previousPorts;

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> InputsByDisplayOrder => m_InputPortInfos.portsById;

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> OutputsByDisplayOrder => m_OutputPortInfos.portsById;

        /// <summary>
        /// The input ports that are visible.
        /// </summary>
        /// <seealso cref="BuildVisiblePorts"/>
        public override IReadOnlyList<PortModel> VisibleInputsByDisplayOrder
        {
            get
            {
                BuildVisiblePorts(ref m_InputPortInfos);
                return m_InputPortInfos.orderedVisiblePorts;
            }
        }

        /// <summary>
        /// The output ports that are visible.
        /// </summary>
        /// <seealso cref="BuildVisiblePorts"/>
        public override IReadOnlyList<PortModel> VisibleOutputsByDisplayOrder
        {
            get
            {
                BuildVisiblePorts(ref m_OutputPortInfos);
                return m_OutputPortInfos.orderedVisiblePorts;
            }
        }

        /// <summary>
        /// A dictionary mapping the input ports' unique name to their respective constants.
        /// </summary>
        /// <remarks>Each port in the node has a <see cref="PortModel.UniqueName"/> for identification.</remarks>
        public IReadOnlyDictionary<string, Constant> InputConstantsById => m_InputConstantsById;

        /// <inheritdoc />
        /// <remarks>Setter implementations for <see cref="GraphElementModel"/> subclasses must set the <see cref="ChangeHint.Layout"/> change hint.</remarks>
        public virtual bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!IsCollapsible())
                    return;

                if (m_Collapsed == value)
                    return;
                m_Collapsed = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Layout);
            }
        }

        /// <summary>
        /// The different modes of the node.
        /// </summary>
        /// <remarks>The modes of a node share functionalities and are part of the same category.
        /// E.g.: A “BasicOperator” node that has the following modes: “Add” , “divide” , “Multiply”, “Power”, “Square Root” and “Subtract”.
        /// </remarks>
        public virtual List<string> Modes { get; } = new List<string>();

        /// <summary>
        /// The current mode.
        /// </summary>
        public int CurrentModeIndex
        {
            get => m_CurrentModeIndex;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            set => m_CurrentModeIndex = Modes.ElementAtOrDefault(m_CurrentModeIndex) != null ? value : 0;
#pragma warning restore UA2001
        }

        /// <summary>
        /// Whether the node is connected to at least one other node.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                foreach (var inputPort in InputPorts)
                {
                    if (inputPort.IsConnected())
                        return true;
                }
                foreach (var outputPort in OutputPorts)
                {
                    if (outputPort.IsConnected())
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeModel"/> class.
        /// </summary>
        protected NodeModel()
        {
            m_OutputPortInfos.Initialize();
            m_InputPortInfos.Initialize();

            m_InputConstantsById = new SerializedReferenceDictionary<string, Constant>();
            m_ElementColor = new ElementColor(this);
        }

        /// <inheritdoc />
        public override void SetColor(Color color) => m_ElementColor.Color = color;

        // Used in tests.
        internal void ClearPorts()
        {
            foreach (var portModel in m_InputPortInfos.portsById.Values)
            {
                GraphModel.UnregisterPort(portModel);
            }

            foreach (var portModel in m_OutputPortInfos.portsById.Values)
            {
                GraphModel.UnregisterPort(portModel);
            }

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
            m_InputPortInfos.Initialize();
            m_OutputPortInfos.Initialize();
        }

        /// <summary>
        /// Changes the node mode.
        /// </summary>
        /// <param name="newModeIndex">The index of the mode to change to.</param>
        public virtual void ChangeMode(int newModeIndex)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (Modes.ElementAtOrDefault(newModeIndex) == null)
#pragma warning restore UA2001
                return;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var existingWires = GetConnectedWires().ToList();
#pragma warning restore UA2001
            var oldInputConstants = m_InputConstantsById.ToList();
            m_InputConstantsById.Clear();

            // Remove old ports
            foreach (var kv in m_InputPortInfos.portsById)
                GraphModel?.UnregisterPort(kv.Value);
            foreach (var kv in m_OutputPortInfos.portsById)
                GraphModel?.UnregisterPort(kv.Value);

            // Set the node mode index
            CurrentModeIndex = newModeIndex;

            // Instantiate the ports of the new node mode
            m_InputPortInfos.Initialize();
            m_OutputPortInfos.Initialize();

            var nodeDefinitionScope = CreateNodeDefinitionScope();
            OnDefineNode(nodeDefinitionScope);

            m_NodeOptionConstantsMigrated = true;

            // Keep the same constant values if possible
            CopyInputConstantValues(oldInputConstants);

            foreach (var wire in existingWires)
            {
                if (wire.ToNodeGuid == Guid)
                    ConnectWireToCorrectPort(PortDirection.Input, wire);
                else if (wire.FromNodeGuid == Guid)
                    ConnectWireToCorrectPort(PortDirection.Output, wire);
            }

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);

            void ConnectWireToCorrectPort(PortDirection direction, WireModel wire)
            {
                var otherPort = direction == PortDirection.Input ? wire.FromPort : wire.ToPort;
                var oldPort = direction == PortDirection.Input ? wire.ToPort : wire.FromPort;
                var newModePorts = direction == PortDirection.Input ? InputsByDisplayOrder : OutputsByDisplayOrder;

                var compatiblePorts = new List<PortModel>();
                foreach (var newModePort in GraphModel.GetCompatiblePorts(newModePorts, otherPort))
                {
                    var connectedWires = newModePort.GetConnectedWires();
                    if (connectedWires.Contains(wire))
                    {
                        // First choice: Port with the same unique name.
                        // If the wire is already connected to a compatible port on the new mode, its unique name has to be the same as the old port unique name. Keep the wire as is.
                        return;
                    }

                    if (newModePort.Capacity == PortCapacity.Multi || !connectedWires.HasAny())
                        compatiblePorts.Add(newModePort);
                }

                PortModel newPort;

                if (oldPort.PortType != PortType.MissingPort)
                {
                    // Second choice: Connect to the first compatible port that is not taken.
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    newPort = compatiblePorts.FirstOrDefault(p => !p.GetConnectedWires().HasAny());
#pragma warning restore UA2001
                }
                else
                {
                    // When the old port is a missing port, its unique name is most likely different from its title. Connect with the compatible port with the same title.
                    // When both ports are missing ports, the type cannot be retrieved. Connect with the port that has the same title.
                    newPort = otherPort.PortType == PortType.MissingPort ?
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        newModePorts.FirstOrDefault(p => p.Title == oldPort.Title) :
#pragma warning restore UA2001
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        compatiblePorts.FirstOrDefault(p => p.Title == oldPort.Title);
#pragma warning restore UA2001
                }

                // Last choice: Become a missing port
                newPort ??= AddMissingPort(direction, Hash128Helpers.GenerateUnique().ToString(), oldPort.Orientation, oldPort.Title);

                if (newPort != null)
                {
                    if (direction == PortDirection.Input)
                        wire.SetPorts(newPort, otherPort);
                    else
                        wire.SetPorts(otherPort, newPort);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="NodeDefinitionScope"/> that provides methods to instantiate ports on the node.
        /// </summary>
        /// <returns>The scope.</returns>
        protected virtual NodeDefinitionScope CreateNodeDefinitionScope() => new NodeDefinitionScope(this);

        /// <summary>
        /// Instantiates the ports of the nodes.
        /// </summary>
        public void DefineNode()
        {
            m_InDefineNode = true;
            using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();

            OnPreDefineNode();

            m_InputPortInfos.previousPorts = new OrderedPorts(m_NodeOptions.Count);
            m_InputPortInfos.previousPorts = m_InputPortInfos.portsById;
            foreach (var nodeOption in m_NodeOptions)
                m_InputPortInfos.previousPorts.Add(nodeOption.PortModel);

            m_OutputPortInfos.previousPorts = m_OutputPortInfos.portsById;

            m_InputPortInfos.orderedVisiblePorts.Clear();
            m_OutputPortInfos.orderedVisiblePorts.Clear();
            m_NodeOptions.Clear();
            m_NodeOptionsByName.Clear();

            m_InputPortInfos.portsById = new OrderedPorts(m_InputPortInfos.portsById?.Count ?? 0);
            m_OutputPortInfos.portsById = new OrderedPorts(m_OutputPortInfos.portsById?.Count ?? 0);

            var nodeDefinitionScope = CreateNodeDefinitionScope();
            OnDefineNode(nodeDefinitionScope);

            CallOnDefineSubPorts(ref m_InputPortInfos);
            CallOnDefineSubPorts(ref m_OutputPortInfos);

            RemoveObsoleteNodeOptionPorts();
            RemoveObsoleteWiresAndConstants();
            m_InDefineNode = false;
        }

        void RedefinePort(PortModel port)
        {
            // Redefines a single port and its sub ports.
            ref PortInfos portInfos = ref GetPortInfos(port.Direction);
            portInfos.previousPorts = new OrderedPorts(1);
            RecursivelyRemoveSubPorts(portInfos.portsById, portInfos.previousPorts, port);

            void RecursivelyRemoveSubPorts(OrderedPorts portsById, OrderedPorts previousPorts, PortModel portModel)
            {
                foreach (var subPort in portModel.SubPorts)
                {
                    portsById.Remove(subPort);
                    previousPorts.Add(subPort);
                    RecursivelyRemoveSubPorts(portsById, previousPorts, subPort);
                }
            }

            portInfos.orderedVisiblePorts.Clear();

            CallOnDefineSubPorts(ref portInfos, port);

            RemoveObsoleteWiresAndConstants();
        }

        void CallOnDefineSubPorts(ref PortInfos portInfos, PortModel singlePort = null)
        {
            if (GraphModel == null || !CanHaveExpandablePorts) return;

            m_SubPortDefinition ??= new SubPortDefinition(this);

            using var pool = ListPool<PortModel>.Get(out var currentList);
            using var pool2 = ListPool<PortModel>.Get(out var nextList);

            if (singlePort != null)
                nextList.Add(singlePort);
            else
                nextList.AddRange(portInfos.portsById.Values);

            IReadOnlyList<PortModel> portModels = nextList;

            while (portModels.Count > 0)
            {
                currentList.Clear();
                m_SubPortDefinition.AddedPorts = currentList;
                foreach (var port in portModels)
                {
                    if (port.Orientation == PortOrientation.Horizontal && GraphModel.CanExpandPort(port))
                    {
                        port.SetExpandable(true);
                        m_SubPortDefinition.ParentPort = port;
                        m_SubPortDefinition.RefreshMustSpecifySubPorts(); // must be called before ClearSubPorts
                        port.ClearSubPorts();
                        GraphModel.OnDefineSubPorts(m_SubPortDefinition, port);

                        if (m_SubPortDefinition.MustSpecifySubPorts && port.SubPorts.Count == 0)
                        {
                            Debug.LogError($"After OnDefineSubPorts, {port.Direction} port {port.UniqueName} from node {Title}({GetType().Name}) is expanded but has no sub ports");
                        }
                        port.SetPortExpanded((portInfos.expandedPortsById).ContainsKey(port.UniqueName));
                    }
                    else
                    {
                        port.SetExpandable(false);
                        port.ClearSubPorts();
                        portInfos.expandedPortsById.Remove(port.UniqueName);
                    }
                }
                portModels = currentList;

                (currentList, nextList) = (nextList, currentList);
            }

            //Add sub port in the ordered list at the right place.

            int start, end;

            if (singlePort != null)
            {
                start = portInfos.portsById.IndexOf(singlePort);
                end = start + 1;
            }
            else
            {
                start = 0;
                end = portInfos.portsById.Count;
            }

            for (int i = start; i < end; ++i)
            {
                //note: this will add sub port recursively.
                var port = portInfos.portsById[i];
                if (port.SubPorts.Count > 0)
                {
                    portInfos.portsById.InsertRange(i + 1, port.SubPorts);
                    end += port.SubPorts.Count;
                }
            }
        }

        void RemoveObsoleteNodeOptionPorts()
        {
            var removedNodeOptionPorts = new List<PortModel>();
            foreach (var previousInput in m_InputPortInfos.previousPorts.Values)
            {
                if (!previousInput.Options.HasFlag(PortModelOptions.IsNodeOption))
                    continue;

                if (m_NodeOptions.FindIndex(t => t.PortModel == previousInput) == -1)
                {
                    GraphModel?.UnregisterPort(previousInput);
                    removedNodeOptionPorts.Add(previousInput);
                }
            }

            if (removedNodeOptionPorts.Count > 0)
            {
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
                GraphModel?.CurrentGraphChangeDescription.AddDeletedModels(removedNodeOptionPorts);
            }
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/> before the <see cref="OrderedPorts"/> lists are modified.
        /// </summary>
        protected virtual void OnPreDefineNode()
        {
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/>. Override this function to instantiate the ports of your node type.
        /// </summary>
        /// <param name="scope">The <see cref="NodeDefinitionScope"/> to define the ports on the node.</param>
        /// <remarks>Ports that are added with <see cref="PortModel.IsExpandable"/> and are expanded must specify at least one sub port.</remarks>
        protected abstract void OnDefineNode(NodeDefinitionScope scope);

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();
            DefineNode();
        }

        /// <summary>
        /// Gets the default style name for the icon of the node.
        /// </summary>
        /// <remarks>Must be the same icon style name as the corresponding library item if it exists; otherwise, it is the default node icon style name.</remarks>
        protected virtual string GetDefaultIconTypeString()
        {
            if (HasSameIconAsItemLibrary)
            {
                // Try to get the same icon as the library item corresponding to the node
                var attributes = GetType().GetCustomAttributes(typeof(LibraryItemAttribute), true);
                if (attributes.Length > 0 && attributes[0] is LibraryItemAttribute libraryItem && !string.IsNullOrEmpty(libraryItem.StyleName))
                    return libraryItem.StyleName;
            }

            return "node";
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);
            DefineNode();
        }

        void RemoveObsoleteWiresAndConstants()
        {
            var removedPortModels = new List<PortModel>();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var kv in m_InputPortInfos.previousPorts
#pragma warning restore UA2001
                     .Where<KeyValuePair<string, PortModel>>(kv => !m_InputPortInfos.portsById.ContainsKey(kv.Key)))
            {
                if (!kv.Value.Options.HasFlag(PortModelOptions.IsNodeOption) && kv.Value.PortType != PortType.MissingPort)
                {
                    DisconnectPort(kv.Value);
                    GraphModel?.UnregisterPort(kv.Value);
                    removedPortModels.Add(kv.Value);
                }
                else if (kv.Value.PortType == PortType.MissingPort && kv.Value.GetConnectedWires().Count > 0)
                {
                    // Prevents added missing ports that aren't obsolete yet from being overwritten by newly instantiated ports in OnDefineNode().
                    m_InputPortInfos.portsById.Add(kv.Value);
                }
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var kv in m_OutputPortInfos.previousPorts
#pragma warning restore UA2001
                     .Where<KeyValuePair<string, PortModel>>(kv => !m_OutputPortInfos.portsById.ContainsKey(kv.Key)))
            {
                if (!kv.Value.Options.HasFlag(PortModelOptions.IsNodeOption) && kv.Value.PortType != PortType.MissingPort)
                {
                    DisconnectPort(kv.Value);
                    GraphModel?.UnregisterPort(kv.Value);
                    removedPortModels.Add(kv.Value);
                }
                else if (kv.Value.PortType == PortType.MissingPort && kv.Value.GetConnectedWires().Count > 0)
                {
                    // Prevents added missing ports that aren't obsolete yet from being overwritten by newly instantiated ports in OnDefineNode().
                    m_OutputPortInfos.portsById.Add(kv.Value);
                }
            }

            if (removedPortModels.Count > 0)
            {
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
                GraphModel?.CurrentGraphChangeDescription.AddDeletedModels(removedPortModels);
            }

            // remove input constants that aren't used
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var idsToDeletes = m_InputConstantsById
#pragma warning restore UA2001
                .Select(kv => kv.Key)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                .Where(id => !m_InputPortInfos.portsById.ContainsKey(id) && m_NodeOptions.All(o => o.PortModel.UniqueName != id)).ToList();
#pragma warning restore UA2001
            foreach (var id in idsToDeletes)
            {
                m_InputConstantsById.Remove(id);
            }

            // remove expanded status for removed ports
            CleanupExpandedPortDictionary(ref m_InputPortInfos);
            CleanupExpandedPortDictionary(ref m_OutputPortInfos);

            void CleanupExpandedPortDictionary(ref PortInfos portInfos)
            {
                var portsById = portInfos.portsById;
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var idsToDelete = portInfos.expandedPortsById
#pragma warning restore UA2001
                    .Select(kv => kv.Key)
                    .Where(id => !portsById.ContainsKey(id)).ToList();
                foreach (var id in idsToDelete)
                {
                    portInfos.expandedPortsById.Remove(id);
                }
            }
        }

        /// <summary>
        /// Searches for a reusable port in the previous ports or the GraphModel. If a reusable port is found, it is returned. Otherwise, null is returned.
        /// On return, the port must have the passed direction, type and data type.
        /// </summary>
        /// <param name="previousPorts">A dictionary of previous ports identified by their <see cref="PortModel.UniqueName"/>. </param>
        /// <param name="direction">The direction of the port to reuse.</param>
        /// <param name="portName">The name of the port to reuse.</param>
        /// <param name="portType">The type of port to reuse.</param>
        /// <param name="dataType">The type of data of the port to reuse.</param>
        /// <param name="portId">The ID of the port to reuse.</param>
        /// <param name="parentPort">The parent port. Can be null.</param>
        /// <returns>A port that has the passed direction, type and data type.</returns>
        /// <remarks>Given the same parameters, <see cref="GetReusablePort"/> must return an equivalent PortModel to <see cref="CreatePort"/></remarks>
        public virtual PortModel GetReusablePort(IReadOnlyDictionary<string, PortModel> previousPorts, PortDirection direction, string portName, PortType portType, TypeHandle dataType, string portId, PortModel parentPort)
        {
            var hash = PortModel.ComputePortHash(this, direction, portName, portType, dataType, portId, parentPort);
            if (GraphModel != null && (GraphModel.TryGetModelFromGuid(hash, out PortModel result) || (previousPorts != null && previousPorts.TryGetValue(PortModel.ComputeUniqueName(portId, portName, hash, parentPort?.UniqueName), out result))))
            {
                if (result is IHasTitle toAddHasTitle)
                {
                    toAddHasTitle.Title = portName ?? "";
                }
                result.DataTypeHandle = dataType;
                result.PortType = portType;

                return result;
            }

            return null;
        }

        PortModel ReuseOrCreatePortModel(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, IReadOnlyDictionary<string, PortModel> previousPorts, OrderedPorts newPorts, PortModel parentPort)
        {
            // If a port is added outside OnDefineNode, clear the visible ports list to force a rebuild. ( Case of missing ports )
            if (!m_InDefineNode)
                GetPortInfos(direction).orderedVisiblePorts.Clear();

            // reuse existing ports when ids match, otherwise add port

            var portModelToAdd = GetReusablePort(previousPorts, direction, portName, portType, dataType, portId, parentPort);
            if (portModelToAdd != null)
            {
                //Update the attributes and options in case the user changed them in OnDefineNode since last time.
                portModelToAdd.SetAttributes(attributes);

                portModelToAdd.Options = options;
            }
            else
            {
                var model = CreatePort(direction, orientation, portName, portType, dataType, portId, options, attributes, parentPort);
                portModelToAdd = model;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
                GraphModel?.CurrentGraphChangeDescription.AddNewModel(portModelToAdd);
            }
            GraphModel?.RegisterPort(portModelToAdd);

            parentPort?.AddSubPort(portModelToAdd);

            if (!options.HasFlag(PortModelOptions.IsNodeOption) && portModelToAdd.ParentPort == null)
            {
                newPorts.Add(portModelToAdd);
            }

            return portModelToAdd;
        }

        /// <summary>
        /// Creates a new port on the node.
        /// </summary>
        /// <param name="direction">The direction of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="portName">The name of the port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the new port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options of the port model to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <param name="parentPort">The parent port. Can be null.</param>
        /// <returns>The newly created port model.</returns>
        /// <remarks>CreatePort will be called only if <see cref="GetReusablePort"/> returns null.</remarks>
        protected virtual PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, PortModel parentPort)
        {
            return new PortModel(this, direction, orientation, portName, portType, dataType, portId, options, attributes, parentPort);
        }

        /// <summary>
        /// Deletes all the wires connected to a given port.
        /// </summary>
        /// <param name="portModel">The port model to disconnect.</param>
        protected virtual void DisconnectPort(PortModel portModel)
        {
            if (GraphModel != null)
            {
                var wireModels = GraphModel.GetWiresForPort(portModel);
                GraphModel.DeleteWires(wireModels);
            }
        }

        /// <remarks>CreatePort will be called only if <see cref="GetReusablePort"/> returns null.</remarks>
        internal PortModel AddInputPort(string portName, TypeHandle dataType, PortType portType = null,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
        {
            if (!options.HasFlag(PortModelOptions.IsNodeOption) && (portId ?? portName)?.StartsWith(NodeOption.k_OptionIdPrefix) == true)
            {
                throw new ArgumentException($"Input port {portName ?? portId} cannot have an id that starts with the reserved prefix {NodeOption.k_OptionIdPrefix} unless it is a node option.");
            }
            var portModel = ReuseOrCreatePortModel(PortDirection.Input, orientation, portName, portType ?? PortType.Default, dataType, portId, options, attributes, m_InputPortInfos.previousPorts, m_InputPortInfos.portsById, null);
            UpdateConstantForInput(portModel, initializationCallback, setterAction);

            // When the port is hidden but still has connections, we change it to a missing port:
            if (options.HasFlag(PortModelOptions.Hidden) && portModel.GetConnectedWires().Count > 0)
            {
                portModel.DataTypeHandle = TypeHandle.MissingPort;
                portModel.PortType = PortType.MissingPort;
            }

            return portModel;
        }

        /// <summary>
        /// Adds a new missing port on a node.
        /// </summary>
        /// <param name="direction">The direction of the port the create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="portName">The name of the port to create.</param>
        /// <returns>The newly created port.</returns>
        internal PortModel AddMissingPort(PortDirection direction, string portId,
            PortOrientation orientation = PortOrientation.Horizontal, string portName = null)
        {
            if (direction == PortDirection.Input)
                return AddInputPort(portName ?? portId, TypeHandle.MissingPort, PortType.MissingPort, portId, orientation,
                    PortModelOptions.NoEmbeddedConstant);

            return AddOutputPort(portName ?? portId, TypeHandle.MissingPort, PortType.MissingPort, portId, orientation,
                PortModelOptions.NoEmbeddedConstant);
        }

        NodeOption AddNodeOption(NodeOption nodeOption)
        {
            m_NodeOptions.Add(nodeOption);
            m_NodeOptionsByName[nodeOption.Id] = nodeOption;
            return m_NodeOptions[^1];
        }

        internal PortModel AddOutputPort(string portName, TypeHandle dataType, PortType portType = null,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            return ReuseOrCreatePortModel(PortDirection.Output, orientation, portName, portType ?? PortType.Default, dataType, portId, options, attributes, m_OutputPortInfos.previousPorts, m_OutputPortInfos.portsById, null);
        }

        PortModel AddInputSubPort<TDataType>(PortModel parent, string portName, Func<TDataType> getter, Action<TDataType> setter, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            return AddInputSubPortWithDelegates(parent, portName, typeof(TDataType).GenerateTypeHandle(), getter, setter, portId, options, attributes);
        }

        PortModel AddInputSubPort(PortModel parent, string portName, TypeHandle typeHandle, Func<object> getter, Action<object> setter, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            return AddInputSubPortWithDelegates(parent, portName, typeHandle, getter, setter, portId, options, attributes);
        }

        PortModel AddFieldSubPort(PortModel parent, FieldInfo fieldInfo, string portName = null, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            var port = AddMemberInfoSubPort(parent, fieldInfo, fieldInfo.FieldType.GenerateTypeHandle(), portName, portId, options, attributes);

            if (port.Direction == PortDirection.Input)
            {
                if (parent.EmbeddedValue != null)
                {
                    if (port.ComputedConstant is not SubPortFieldInfoConstant spfic)
                        port.ComputedConstant = new SubPortFieldInfoConstant(port, fieldInfo);
                    else
                        spfic.SetMemberInfo(fieldInfo);
                }
                else
                {
                    port.ComputedConstant = null;
                }
            }

            return port;
        }

        PortModel AddPropertySubPort(PortModel parent, PropertyInfo propertyInfo, string portName = null, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            var port = AddMemberInfoSubPort(parent, propertyInfo, propertyInfo.PropertyType.GenerateTypeHandle(), portName, portId, options, attributes);

            if (port.Direction == PortDirection.Input)
            {
                if (parent.EmbeddedValue != null)
                {
                    if (port.ComputedConstant is not SubPortPropertyInfoConstant sppic)
                        port.ComputedConstant = new SubPortPropertyInfoConstant(port, propertyInfo);
                    else
                        sppic.SetMemberInfo(propertyInfo);
                }
                else
                {
                    port.ComputedConstant = null;
                }
            }

            return port;
        }

        PortModel AddInputSubPortWithDelegates(PortModel parent, string portName, TypeHandle typeHandle, Delegate getter, Delegate setter, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            if (!parent.IsExpandable)
            {
                throw new ArgumentException($"Parent port {parent.UniqueName} of port {portName ?? portId} must be expandable.");
            }

            if (parent.Direction == PortDirection.Output)
            {
                throw new ArgumentException($"Parent port {parent.UniqueName} of port {portName ?? portId} must be an input port.");
            }

            var port = AddSubPort(parent, portName, typeHandle, portId, options, attributes);
            if (port.ComputedConstant is not SubPortCustomConstant spcc)
                port.ComputedConstant = new SubPortCustomConstant(port, getter, setter);
            else
                spcc.Set(getter, setter);

            return port;
        }

        PortModel AddMemberInfoSubPort(PortModel parent, MemberInfo memberInfo, TypeHandle typeHandle, string portName, string portId, PortModelOptions options, Attribute[] attributes)
        {
            if (!parent.IsExpandable)
            {
                throw new ArgumentException($"Parent port {parent.UniqueName} of port {portName ?? portId} must be expandable.");
            }
            if (parent.PortDataType != memberInfo.DeclaringType)
            {
                throw new ArgumentException($"Parent port {parent.UniqueName} of port {portName ?? portId} must be of type {parent.PortDataType.FullName}.");
            }

            portName ??= memberInfo.Name;

            var port = CommonAddSubPort(parent, portName, typeHandle, portId, options, attributes);
            return port;
        }

        PortModel AddSubPort(PortModel parent, string portName, TypeHandle dataType, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            var port = CommonAddSubPort(parent, portName, dataType, portId, options, attributes);
            port.ComputedConstant = null;
            return port;
        }

        PortModel CommonAddSubPort(PortModel parent, string portName, TypeHandle dataType, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            if (!parent.IsExpandable)
            {
                throw new ArgumentException($"Parent port {parent.UniqueName} of port {portName ?? portId} must be expandable.");
            }

            var portInfos = GetPortInfos(parent.Direction);
            return ReuseOrCreatePortModel(parent.Direction, parent.Orientation, portName, parent.PortType, dataType, portId, options, attributes, portInfos.previousPorts, portInfos.portsById, parent);
        }

        /// <inheritdoc />
        public override void OnPortUniqueNameChanged(PortModel portModel, string oldUniqueName, string newUniqueName)
        {
            if (portModel.Direction == PortDirection.Input)
            {
                m_InputPortInfos.portsById.ChangePortName(portModel, oldUniqueName);

                if (m_InputConstantsById.Remove(oldUniqueName, out var constant))
                {
                    m_InputConstantsById.TryAdd(newUniqueName, constant);
                }

                if (m_InputPortInfos.expandedPortsById.Remove(oldUniqueName, out var expanded))
                {
                    m_InputPortInfos.expandedPortsById.Add(newUniqueName, expanded);
                }
            }
            else
            {
                m_OutputPortInfos.portsById.ChangePortName(portModel, oldUniqueName);

                if (m_OutputPortInfos.expandedPortsById.Remove(oldUniqueName, out var expanded))
                {
                    m_OutputPortInfos.expandedPortsById.Add(newUniqueName, expanded);
                }
            }

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
        }

        /// <summary>
        /// Sets the expanded state of a <see cref="PortModel"/> created with <see cref="PortModel.IsExpandable"/>
        /// </summary>
        /// <param name="direction">The direction of the port.</param>
        /// <param name="uniqueName">The unique name of the port.</param>
        /// <param name="expanded">Whether to expand or collapse the port.</param>
        public void SetPortExpanded(PortDirection direction, string uniqueName, bool expanded)
        {
            SetPortExpanded(GetPortInfos(direction).portsById[uniqueName], expanded);
        }

        /// <summary>
        /// Sets the expanded state of a <see cref="PortModel"/> created with <see cref="PortModel.IsExpandable"/>
        /// </summary>
        /// <param name="portModel">The port whose expanded state must be changed.</param>
        /// <param name="expanded">Whether to expand or collapse the port.</param>
        public void SetPortExpanded(PortModel portModel, bool expanded)
        {
            if (portModel.NodeModel != this)
            {
                throw new ArgumentException("SetPortExpanded called on another port's node.");
            }

            if (!m_InDefineNode && !portModel.IsExpandable)
            {
                // If we are in DefineNode, we might not know yet if the port is expandable or not.
                throw new ArgumentException("SetPortExpanded called on a not expandable node.");
            }

            var portInfos = GetPortInfos(portModel.Direction);

            if (expanded && portInfos.expandedPortsById.TryAdd(portModel.UniqueName, true))
            {
                portModel.SetPortExpanded(true);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(portModel, ChangeHint.Unspecified);
            }
            else if (!expanded)
            {
                if (portInfos.expandedPortsById.Remove(portModel.UniqueName))
                {
                    GraphModel?.CurrentGraphChangeDescription.AddChangedModel(portModel, ChangeHint.Unspecified);
                }
            }
            portModel.SetPortExpanded(expanded);

            if (!m_InDefineNode)
            {
                if (portModel.SubPorts.Count > 0)
                {
                    portInfos.orderedVisiblePorts.Clear();
                }
                else
                {
                    RedefinePort(portModel);
                }
            }

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data); // The node is marked as changed because the list of visible ports might have changed which is a node responsibility.
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(portModel, ChangeHint.Data);
        }

        /// <summary>
        /// Checks whether this node can be itemized.
        /// </summary>
        /// <returns>True if the node can be itemized, false if not.</returns>
        public virtual bool CanBeItemized()
        {
            if (this is not ISingleOutputPortNodeModel)
                return false;

            foreach (var outputPortModel in OutputsByDisplayOrder)
            {
                if (outputPortModel.PortType == PortType.Default && outputPortModel.GetConnectedPorts().Count > 1)
                    return true;
            }

            return false;
        }

        internal override void OnPortDataTypeChanged(PortModel portModel, TypeHandle previousType, TypeHandle dataTypeHandle)
        {
            if (!m_InDefineNode)
            {
                RedefinePort(portModel);
            }
            UpdateConstantForInput(portModel);
        }

        /// <summary>
        /// Updates an input port's constant.
        /// </summary>
        /// <param name="inputPort">The port to update.</param>
        /// <param name="initializationCallback">An initialization method for the constant to be called right after the constant is created.</param>
        /// <param name="setterAction">A method to be called after the constant value changes.</param>
        protected internal override void UpdateConstantForInput(PortModel inputPort, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById.Remove(id);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
                return;
            }

            Constant newConstant = null;
            if (m_InputConstantsById.TryGetValue(id, out var existingConstant))
            {
                newConstant = GraphModel?.CreateConstantValue(inputPort.DataTypeHandle);
                var portDefinitionType = newConstant != null ? newConstant.Type : inputPort.DataTypeHandle.Resolve();

                if (!existingConstant.IsAssignableFrom(portDefinitionType))
                {
                    // Destroy incompatible constant
                    m_InputConstantsById.Remove(id);
                    GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
                }
                else
                {
                    // Reuse compatible constant.
                    existingConstant.OwnerModel = inputPort;
                    existingConstant.SetterMethod = setterAction;
                    return;
                }
            }

            // Create new constant if needed
            if (inputPort.CreateEmbeddedValueIfNeeded
                && inputPort.DataTypeHandle != TypeHandle.Unknown)
            {
                newConstant ??= GraphModel?.CreateConstantValue(inputPort.DataTypeHandle);
                if (newConstant != null)
                {
                    newConstant.OwnerModel = inputPort;
                    initializationCallback?.Invoke(newConstant);
                    newConstant.SetterMethod = setterAction;
                    m_InputConstantsById[id] = newConstant;
                    GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
                }
            }
        }

        void CopyInputConstantValues(List<KeyValuePair<string, Constant>> otherInputConstants)
        {
            var index = 0;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var id in m_InputConstantsById.Keys.ToList())
#pragma warning restore UA2001
            {
                // First choice: constant with the same id
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var constantWithSameId = otherInputConstants.FirstOrDefault(c => id == c.Key).Value;
#pragma warning restore UA2001
                if (constantWithSameId != null)
                {
                    if (m_InputConstantsById[id].IsAssignableFrom(constantWithSameId.Type))
                        m_InputConstantsById[id] = constantWithSameId;
                }
                else
                {
                    // Second choice: constant at the same index
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var constantAtSameIndex = otherInputConstants.ElementAtOrDefault(index).Value;
#pragma warning restore UA2001
                    if (constantAtSameIndex != null && m_InputConstantsById[id].IsAssignableFrom(constantAtSameIndex.Type))
                        m_InputConstantsById[id] = constantAtSameIndex;
                }
                index++;
            }
        }

        /// <inheritdoc />
        public override bool RemoveUnusedMissingPort(PortModel portModel)
        {
            if (portModel.PortType != PortType.MissingPort || portModel.GetConnectedWires().HasAny())
                return false;

            // If a port is hidden but is a missing port, it is still visible on the node because it has connections. We redefine the node to see if the port should stay visible as a missing port or be hidden.
            if (portModel.Options.HasFlag(PortModelOptions.Hidden))
            {
                DefineNode();
                return false;
            }

            GraphModel.UnregisterPort(portModel);
            GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
            DefineNode();
            return GetPortInfos(portModel.Direction).portsById.Remove(portModel);
        }

        internal void SetConstantForPort(string inputId, Constant newConstant)
        {
            m_InputConstantsById[inputId] = newConstant;
        }

        /// <inheritdoc />
        public override void OnConnection(PortModel selfConnectedPortModel, PortModel otherConnectedPortModel)
        {
            base.OnConnection(selfConnectedPortModel, otherConnectedPortModel);

            GetPortInfos(selfConnectedPortModel.Direction).orderedVisiblePorts.Clear();

            if (!selfConnectedPortModel.AreAncestorsExpanded || m_Collapsed)
            {
                // Port visibility changes: sub-port with a collapsed ancestor becomes visible when connected;
                // top-level port on a collapsed node depends on IsConnected(). Notify so the UI updates.
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override void OnDisconnection(PortModel selfConnectedPortModel, PortModel otherConnectedPortModel)
        {
            base.OnDisconnection(selfConnectedPortModel, otherConnectedPortModel);

            if (!selfConnectedPortModel.AreAncestorsExpanded) // If the disconnected port had one of its ancestor collapsed, it was only visible because it was connected.
            {
                GetPortInfos(selfConnectedPortModel.Direction).orderedVisiblePorts.Clear();

                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
            else if (m_Collapsed)
            {
                // On a collapsed node, top-level port visibility depends on IsConnected().
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Appends all ports that are not connected or which parent is not collapsed.
        /// </summary>
        void BuildVisiblePorts(ref PortInfos portInfos)
        {
            if (portInfos.orderedVisiblePorts.Count == 0 && portInfos.portsById.Count > 0)
            {
                foreach (var port in portInfos.portsById.Values)
                {
                    if (port.ParentPort == null || port.AreAncestorsExpanded || port.IsConnected() || port.ParentPort.IsConnected() && port.ParentPort.IsExpandedSelf)
                        portInfos.orderedVisiblePorts.Add(port);
                }
            }
        }

        ref PortInfos GetPortInfos(PortDirection direction)
        {
            return ref direction == PortDirection.Input ? ref m_InputPortInfos : ref m_OutputPortInfos;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_ElementColor.OwnerElementModel = this;
        }
    }
}
