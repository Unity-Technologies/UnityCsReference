// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base model that represents a dynamically defined node.
    /// </summary>
    [Serializable]
    abstract class NodeModel : InputOutputPortsNodeModel, IHasProgress, ICollapsible, ICopyPasteCallbackReceiver
    {
        [SerializeField, HideInInspector]
        SerializedReferenceDictionary<string, Constant> m_InputConstantsById;

        [SerializeField, HideInInspector]
        ModelState m_State;

        OrderedPorts m_InputsById;
        OrderedPorts m_OutputsById;
        OrderedPorts m_PreviousInputs;
        OrderedPorts m_PreviousOutputs;

        [SerializeField, HideInInspector]
        bool m_Collapsed;

        [SerializeField, HideInInspector]
        int m_CurrentModeIndex;

        /// <inheritdoc />
        public override string IconTypeString => "node";

        /// <inheritdoc />
        public override ModelState State
        {
            get => m_State;
            set
            {
                if (m_State == value)
                    return;
                m_State = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override bool AllowSelfConnect => false;

        /// <inheritdoc />
        public override bool HasNodePreview => false;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, PortModel> InputsById => m_InputsById;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, PortModel> OutputsById => m_OutputsById;

        /// <summary>
        /// The previous value of <see cref="InputsById"/>.
        /// </summary>
        protected IReadOnlyDictionary<string, PortModel> PreviousInputsById => m_PreviousInputs;

        /// <summary>
        /// The previous value of <see cref="OutputsById"/>.
        /// </summary>
        protected IReadOnlyDictionary<string, PortModel> PreviousOutputsById => m_PreviousOutputs;

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> InputsByDisplayOrder => m_InputsById;

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> OutputsByDisplayOrder => m_OutputsById;

        public IReadOnlyDictionary<string, Constant> InputConstantsById => m_InputConstantsById;

        /// <inheritdoc />
        public virtual bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                if (m_Collapsed == value)
                    return;
                m_Collapsed = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Layout);
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
            set => m_CurrentModeIndex = Modes.ElementAtOrDefault(m_CurrentModeIndex) != null ? value : 0;
        }

        /// <inheritdoc />
        public virtual bool HasProgress => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeModel"/> class.
        /// </summary>
        protected NodeModel()
        {
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
            m_InputConstantsById = new SerializedReferenceDictionary<string, Constant>();
        }

        // Used in tests.
        internal void ClearPorts_Internal()
        {
            foreach (var portModel in m_InputsById.Values)
            {
                GraphModel.UnregisterPort(portModel);
            }

            foreach (var portModel in m_OutputsById.Values)
            {
                GraphModel.UnregisterPort(portModel);
            }

            GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.GraphTopology);
            m_InputsById = new OrderedPorts();
            m_OutputsById = new OrderedPorts();
            m_PreviousInputs = null;
            m_PreviousOutputs = null;
        }

        /// <summary>
        /// Changes the node mode.
        /// </summary>
        /// <param name="newModeIndex">The index of the mode to change to.</param>
        public virtual void ChangeMode(int newModeIndex)
        {
            if (Modes.ElementAtOrDefault(newModeIndex) == null)
                return;

            var existingWires = GetConnectedWires().ToList();
            var oldInputConstants = m_InputConstantsById.ToList();
            m_InputConstantsById.Clear();

            // Remove old ports
            foreach (var kv in m_InputsById)
                GraphModel?.UnregisterPort(kv.Value);
            foreach (var kv in m_OutputsById)
                GraphModel?.UnregisterPort(kv.Value);

            // Set the node mode index
            CurrentModeIndex = newModeIndex;

            // Instantiate the ports of the new node mode
            m_InputsById = new OrderedPorts();
            m_OutputsById = new OrderedPorts();
            OnDefineNode();

            // Keep the same constant values if possible
            CopyInputConstantValues(oldInputConstants);

            foreach (var wire in existingWires)
            {
                if (wire.ToNodeGuid == Guid)
                    ConnectWireToCorrectPort(PortDirection.Input, wire);
                else if (wire.FromNodeGuid == Guid)
                    ConnectWireToCorrectPort(PortDirection.Output, wire);
            }

            GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);

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

                    if (newModePort.Capacity == PortCapacity.Multi || !connectedWires.Any())
                        compatiblePorts.Add(newModePort);
                }

                PortModel newPort;

                if (oldPort.PortType != PortType.MissingPort)
                {
                    // Second choice: Connect to the first compatible port that is not taken.
                    newPort = compatiblePorts.FirstOrDefault(p => !p.GetConnectedWires().Any());
                }
                else
                {
                    // When the old port is a missing port, its unique name is most likely different from its title. Connect with the compatible port with the same title.
                    // When both ports are missing ports, the type cannot be retrieved. Connect with the port that has the same title.
                    newPort = otherPort.PortType == PortType.MissingPort ?
                        newModePorts.FirstOrDefault(p => p.DisplayTitle == oldPort.DisplayTitle) :
                        compatiblePorts.FirstOrDefault(p => p.DisplayTitle == oldPort.DisplayTitle);
                }

                // Last choice: Become a missing port
                newPort ??= this.AddMissingPort(direction, Hash128Extensions.Generate().ToString(), oldPort.Orientation, oldPort.Title);

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
        /// Instantiates the ports of the nodes.
        /// </summary>
        public void DefineNode()
        {
            OnPreDefineNode();

            m_NodeOptions.Clear();
            DefineNodeOptions();

            m_PreviousInputs = m_InputsById;
            m_PreviousOutputs = m_OutputsById;
            m_InputsById = new OrderedPorts(m_InputsById?.Count ?? 0);
            m_OutputsById = new OrderedPorts(m_OutputsById?.Count ?? 0);

            OnDefineNode();

            RemoveObsoleteWiresAndConstants();
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
        protected abstract void OnDefineNode();

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();
            DefineNode();
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/>. Override this function to add node options by using <see cref="InputOutputPortsNodeModel.AddNodeOption"/>.
        /// </summary>
        protected virtual void DefineNodeOptions()
        {
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);
            DefineNode();
        }

        void RemoveObsoleteWiresAndConstants()
        {
            var portsRemoved = false;
            foreach (var kv in m_PreviousInputs
                     .Where<KeyValuePair<string, PortModel>>(kv => !m_InputsById.ContainsKey(kv.Key)))
            {
                if (kv.Value.PortType != PortType.MissingPort)
                {
                    DisconnectPort(kv.Value);
                    GraphModel?.UnregisterPort(kv.Value);
                    portsRemoved = true;
                }
                else if (kv.Value.PortType == PortType.MissingPort && kv.Value.GetConnectedWires().Any())
                {
                    // This is needed to prevent added missing ports that aren't obsolete yet from being overwritten by newly instantiated ports in OnDefineNode().
                    m_InputsById.Add(kv.Value);
                }
            }

            foreach (var kv in m_PreviousOutputs
                .Where<KeyValuePair<string, PortModel>>(kv => !m_OutputsById.ContainsKey(kv.Key)))
            {
                if (kv.Value.PortType != PortType.MissingPort)
                {
                    DisconnectPort(kv.Value);
                    GraphModel?.UnregisterPort(kv.Value);
                    portsRemoved = true;
                }
                else if (kv.Value.PortType == PortType.MissingPort && kv.Value.GetConnectedWires().Any())
                {
                    // This is needed to prevent added missing ports that aren't obsolete yet from being overwritten by newly instantiated ports in OnDefineNode().
                    m_OutputsById.Add(kv.Value);
                }
            }

            if (portsRemoved)
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.GraphTopology);

            // remove input constants that aren't used
            var idsToDeletes = m_InputConstantsById
                .Select(kv => kv.Key)
                .Where(id => !m_InputsById.ContainsKey(id) && m_NodeOptions.All(o => o.PortModel.UniqueName != id)).ToList();
            foreach (var id in idsToDeletes)
            {
                m_InputConstantsById.Remove(id);
            }
        }

        PortModel ReuseOrCreatePortModel(PortModel model, IReadOnlyDictionary<string, PortModel> previousPorts, OrderedPorts newPorts)
        {
            // reuse existing ports when ids match, otherwise add port
            PortModel portModelToAdd;
            if (previousPorts != null && previousPorts.TryGetValue(model.UniqueName, out portModelToAdd)
                || GraphModel != null && GraphModel.TryGetModelFromGuid(model.Guid, out portModelToAdd))
            {
                if (portModelToAdd is IHasTitle toAddHasTitle && model is IHasTitle hasTitle)
                {
                    toAddHasTitle.Title = hasTitle.Title;
                }
                portModelToAdd.DataTypeHandle = model.DataTypeHandle;
                portModelToAdd.PortType = model.PortType;
                if (GraphModel != null && !GraphModel.TryGetModelFromGuid(portModelToAdd.Guid, out _))
                    GraphModel.RegisterPort(portModelToAdd);
            }
            else
            {
                GraphModel?.RegisterPort(model);
                portModelToAdd = model;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.GraphTopology);
            }

            newPorts.Add(portModelToAdd);
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
        /// <returns>The newly created port model.</returns>
        protected virtual PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes)
        {
            return new PortModel(this, direction, orientation, portName, portType, dataType, portId, options, attributes);
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

        /// <inheritdoc />
        public override PortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<Constant> initializationCallback = null,
            Attribute[] attributes = null)
        {
            var portModel = CreatePort(PortDirection.Input, orientation, portName, portType, dataType, portId, options, attributes);
            portModel = ReuseOrCreatePortModel(portModel, m_PreviousInputs, m_InputsById);
            UpdateConstantForInput(portModel, initializationCallback);
            return portModel;
        }

        /// <inheritdoc />
        public override PortModel AddNoConnectorInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<Constant> initializationCallback = null,
            Attribute[] attributes = null)
        {
            var portModel = AddInputPort(portName, portType, dataType, portId, orientation, options, initializationCallback, attributes);
            portModel.Capacity = PortCapacity.None;
            return portModel;
        }

        /// <inheritdoc />
        public override PortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null)
        {
            var portModel = CreatePort(PortDirection.Output, orientation, portName, portType, dataType, portId, options, attributes);
            return ReuseOrCreatePortModel(portModel, m_PreviousOutputs, m_OutputsById);
        }

        /// <inheritdoc />
        public override void OnPortUniqueNameChanged(string oldUniqueName, string newUniqueName)
        {
            if (!m_InputConstantsById.TryGetValue(oldUniqueName, out var constant) || !m_InputsById.TryGetValue(oldUniqueName, out var portModel))
                return;

            m_InputsById.Remove(oldUniqueName);
            m_InputsById.Add(portModel);

            m_InputConstantsById.Remove(oldUniqueName);

            if (!m_InputConstantsById.ContainsKey(newUniqueName))
                m_InputConstantsById.Add(newUniqueName, constant);

            GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Unspecified);
        }

        /// <summary>
        /// Updates an input port's constant.
        /// </summary>
        /// <param name="inputPort">The port to update.</param>
        /// <param name="initializationCallback">An initialization method for the constant to be called right after the constant is created.</param>
        protected void UpdateConstantForInput(PortModel inputPort, Action<Constant> initializationCallback = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById.Remove(id);
                GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Unspecified);
                return;
            }

            if (m_InputConstantsById.TryGetValue(id, out var constant))
            {
                // Destroy existing constant if not compatible
                var embeddedConstantType = GraphModel.Stencil.GetConstantType(inputPort.DataTypeHandle);
                Type portDefinitionType;
                if (embeddedConstantType != null)
                {
                    var instance = (Constant)Activator.CreateInstance(embeddedConstantType);
                    portDefinitionType = instance.Type;
                }
                else
                {
                    portDefinitionType = inputPort.DataTypeHandle.Resolve();
                }

                if (!constant.IsAssignableFrom(portDefinitionType))
                {
                    m_InputConstantsById.Remove(id);
                    GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Unspecified);
                }
                else
                {
                    // We might be reusing a constant for a new compatible port.
                    constant.OwnerModel = inputPort;
                }
            }

            // Create new constant if needed
            if (!m_InputConstantsById.ContainsKey(id)
                && inputPort.CreateEmbeddedValueIfNeeded
                && inputPort.DataTypeHandle != TypeHandle.Unknown
                && GraphModel.Stencil.GetConstantType(inputPort.DataTypeHandle) != null)
            {
                var embeddedConstant = GraphModel.Stencil.CreateConstantValue(inputPort.DataTypeHandle);
                embeddedConstant.OwnerModel = inputPort;
                initializationCallback?.Invoke(embeddedConstant);
                m_InputConstantsById[id] = embeddedConstant;
                GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Unspecified);
            }
        }

        public ConstantNodeModel CloneConstant(ConstantNodeModel source)
        {
            var clone = Activator.CreateInstance(source.GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(source, clone);
            return (ConstantNodeModel)clone;
        }

        void CopyInputConstantValues(List<KeyValuePair<string, Constant>> otherInputConstants)
        {
            var index = 0;
            foreach (var id in m_InputConstantsById.Keys.ToList())
            {
                // First choice: constant with the same Id
                var constantWithSameId = otherInputConstants.FirstOrDefault(c => id == c.Key).Value;
                if (constantWithSameId != null)
                {
                    if (m_InputConstantsById[id].IsAssignableFrom(constantWithSameId.Type))
                        m_InputConstantsById[id] = constantWithSameId;
                }
                else
                {
                    // Second choice: constant at the same index
                    var constantAtSameIndex = otherInputConstants.ElementAtOrDefault(index).Value;
                    if (constantAtSameIndex != null && m_InputConstantsById[id].IsAssignableFrom(constantAtSameIndex.Type))
                        m_InputConstantsById[id] = constantAtSameIndex;
                }
                index++;
            }
        }

        /// <inheritdoc />
        public override bool RemoveUnusedMissingPort(PortModel portModel)
        {
            if (portModel.PortType != PortType.MissingPort || portModel.GetConnectedWires().Any())
                return false;

            GraphModel.UnregisterPort(portModel);
            GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.GraphTopology);
            return portModel.Direction == PortDirection.Input ? m_InputsById.Remove(portModel) : m_OutputsById.Remove(portModel);
        }

        /// <inheritdoc />
        public virtual void OnBeforeCopy()
        {
            foreach (var callbackReceiver in m_InputConstantsById.Values.OfType<ICopyPasteCallbackReceiver>())
            {
                callbackReceiver.OnBeforeCopy();
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterPaste()
        {
            foreach (var callbackReceiver in m_InputConstantsById.Values.OfType<ICopyPasteCallbackReceiver>())
            {
                callbackReceiver.OnAfterPaste();
            }
        }
    }
}
