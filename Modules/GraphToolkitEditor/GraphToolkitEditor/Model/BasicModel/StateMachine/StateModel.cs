// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for nodes that represent a state in a state machine.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal partial class StateModel : PortNodeModel, IRenamable
    {
        const string k_DefaultName = "State";

        const string k_OutgoingPortId = "Outgoing";
        const string k_IncomingPortId = "Incoming";

        /// <summary>
        /// Scope for defining the node options of a state in the <see cref="StateModel.OnDefineOptions"/> method.
        /// </summary>
        /// <remarks>Unlike the node options of a <see cref="NodeModel"/>, the node options of a state are only ever
        /// displayed in the graph inspector: they are never drawn on the state in the state machine canvas.</remarks>
        [UnityRestricted]
        internal class StateDefinitionScope : IOptionsDefinition
        {
            protected readonly StateModel m_StateModel;

            /// <summary>
            /// Creates an instance of a <see cref="StateDefinitionScope"/>.
            /// </summary>
            /// <param name="stateModel">The state that is being defined.</param>
            public StateDefinitionScope(StateModel stateModel)
            {
                m_StateModel = stateModel;
            }

            /// <summary>
            /// Adds a node option to the state.
            /// </summary>
            /// <param name="optionName">The name of the node option.</param>
            /// <param name="dataType">The type of the node option.</param>
            /// <param name="optionId">The unique id of the node option.</param>
            /// <param name="tooltip">The tooltip to show on the node option, if any.</param>
            /// <param name="showInInspectorOnly">Unused: the node options of a state are always shown in the graph inspector only.</param>
            /// <param name="order">The order in which the option will be displayed among the other node options.</param>
            /// <param name="attributes">The attributes used to convey information about the node option, if any.</param>
            /// <param name="initializationCallback">An initialization method for the associated constant to be called right after the node option is created.</param>
            /// <param name="setterAction">A callback method called after a node option's constant has been set.</param>
            /// <returns>The newly added option.</returns>
            public NodeOption AddNodeOption(string optionName, TypeHandle dataType, string optionId = null, string tooltip = null, bool showInInspectorOnly = false, int order = 0, Attribute[] attributes = null, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
            {
                if (dataType == TypeHandle.Unknown || dataType == TypeHandle.Untyped || dataType == TypeHandle.MissingType || dataType == TypeHandle.MissingPort)
                    throw new ArgumentException("Invalid type for node option");

                var resolvedDataType = dataType.Resolve();
                if (resolvedDataType == typeof(Unknown) || resolvedDataType == typeof(Untyped) || resolvedDataType == typeof(MissingPort))
                    throw new ArgumentException("Invalid type for node option");

                optionId ??= optionName;

                // A node option consists in a no connector port with extra info. We add a prefix to avoid id conflicts with regular ports.
                var portId = $"{NodeOption.k_OptionIdPrefix}{optionId}";
                var noConnectorPort = m_StateModel.AddOptionPort(optionName, dataType, portId, attributes, initializationCallback, setterAction);

                // Assigned even when null, which clears the override a previous definition may have left behind.
                noConnectorPort.ToolTip = tooltip;

                var nodeOption = new NodeOption(optionId, noConnectorPort, true, order);
                m_StateModel.AddNodeOption(nodeOption);
                return nodeOption;
            }

            public INodeOption AddNodeOption(string optionName, Type dataType, string optionDisplayName = null, string tooltip = null,
                bool showInInspectorOnly = false, int order = 0, Attribute[] attributes = null, object defaultValue = null)
            {
                Action<Constant> initializationCallback = null;
                if (defaultValue != null)
                    initializationCallback = c => c.ObjectValue = defaultValue;
                return AddNodeOption(optionDisplayName ?? optionName, dataType.GenerateTypeHandle(), optionDisplayName != null ? optionName : null, tooltip, showInInspectorOnly, order, attributes, initializationCallback, _ =>
                {
                    if (!m_StateModel.m_InDefineState)
                        m_StateModel.DefineNode();
                });
            }
        }

        // Fallback color used when no user color is set.
        protected static readonly Color k_DefaultColor = new(75 / 255f, 136 / 255f, 172 / 255f, 1f);

        // Don't serialize these ports when entering play mode, they are created on demand.
        [NonSerialized]
        StatePortModel m_OutPort;
        [NonSerialized]
        StatePortModel m_InPort;
        [SerializeField, HideInInspector]
        ElementColor m_ElementColor;

        [SerializeField, HideInInspector]
        SerializedReferenceDictionary<string, Constant> m_OptionConstantsById;

        // The node options of the previous define pass, used to reuse their ports and to detect the obsolete ones.
        [NonSerialized]
        readonly List<NodeOption> m_PreviousNodeOptions = new List<NodeOption>();

        [NonSerialized]
        bool m_InDefineState;

        /// <inheritdoc />
        internal override IDictionary<string, Constant> ConstantsByPortName => m_OptionConstantsById;

        /// <inheritdoc />
        public override bool AllowSelfConnect => true;

        /// <inheritdoc />
        public override string IconTypeString { get; set; } = "state";

        /// <inheritdoc />
        public override ElementColor ElementColor => m_ElementColor;

        /// <inheritdoc />
        public override void SetColor(Color color) => m_ElementColor.Color = color;

        /// <inheritdoc />
        public override Color DefaultColor => k_DefaultColor;

        /// <inheritdoc />
        public override bool UseColorAlpha => true;

        /// <inheritdoc />
        public override bool HasNodePreview => false;

        /// <summary>
        /// Gets the port on which to connect the outgoing connections of this state.
        /// </summary>
        /// <returns>The port.</returns>
        public StatePortModel GetOutPort()
        {
            if (m_OutPort == null)
                DefinePorts();

            return m_OutPort;
        }

        /// <summary>
        /// Gets the port on which to connect the incoming connections of this state.
        /// </summary>
        /// <returns>The port.</returns>
        public StatePortModel GetInPort()
        {
            if (m_InPort == null)
                DefinePorts();

            return m_InPort;
        }

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> GetPorts() => [ GetOutPort(), GetInPort() ];

        /// <summary>
        /// Initializes a new instance of the <see cref="StateModel"/> class.
        /// </summary>
        public StateModel()
        {
            m_Capabilities = new List<Capabilities>()
            {
                Unity.GraphToolkit.Editor.Capabilities.Selectable,
                Unity.GraphToolkit.Editor.Capabilities.Deletable,
                Unity.GraphToolkit.Editor.Capabilities.Copiable,
                Unity.GraphToolkit.Editor.Capabilities.Renamable,
                Unity.GraphToolkit.Editor.Capabilities.Movable,
                Unity.GraphToolkit.Editor.Capabilities.Colorable
            };
            m_ElementColor = new ElementColor(this);
            m_OptionConstantsById = new SerializedReferenceDictionary<string, Constant>();
        }

        void DefinePorts()
        {
            if (m_OutPort == null)
            {
                m_OutPort = new StatePortModel(PortDirection.Output, this, k_OutgoingPortId);

                GraphModel?.RegisterPort(m_OutPort);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
            }

            if (m_InPort == null)
            {
                m_InPort = new StatePortModel(PortDirection.Input, this, k_IncomingPortId);

                GraphModel?.RegisterPort(m_InPort);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
            }
        }

        /// <summary>
        /// Creates a <see cref="StateDefinitionScope"/> that provides methods to instantiate the node options of the state.
        /// </summary>
        /// <returns>The scope.</returns>
        protected virtual StateDefinitionScope CreateStateDefinitionScope() => new StateDefinitionScope(this);

        /// <summary>
        /// Called by <see cref="DefineNode"/>. Override this method to instantiate the node options of your state type.
        /// </summary>
        /// <param name="scope">The <see cref="StateDefinitionScope"/> used to define the node options on the state.</param>
        /// <remarks>The node options of a state are only ever displayed in the graph inspector.</remarks>
        protected virtual void OnDefineOptions(StateDefinitionScope scope) { }

        /// <inheritdoc />
        public override void DefineNode()
        {
            m_InDefineState = true;
            using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();

            try
            {
                // States serialized before node options existed have no constant dictionary.
                m_OptionConstantsById ??= new SerializedReferenceDictionary<string, Constant>();

                DefinePorts();

                m_PreviousNodeOptions.Clear();
                m_PreviousNodeOptions.AddRange(m_NodeOptions);
                ClearNodeOptions();

                OnDefineOptions(CreateStateDefinitionScope());

                RemoveObsoleteOptionPortsAndConstants();
                m_PreviousNodeOptions.Clear();
            }
            finally
            {
                // A throwing user callback must not leave the reentrancy guard stuck.
                m_InDefineState = false;
            }
        }

        /// <summary>
        /// Adds the no connector input port backing a node option.
        /// </summary>
        /// <remarks>Option ports are deliberately absent from <see cref="GetPorts"/>: they are not part of the state's
        /// topology and must not take part in transition connection or dependency computations.</remarks>
        PortModel AddOptionPort(string optionName, TypeHandle dataType, string portId, Attribute[] attributes,
            Action<Constant> initializationCallback, Action<object> setterAction)
        {
            var portModel = GetReusableOptionPort(optionName, dataType, portId);
            if (portModel != null)
            {
                // Update the attributes in case the user changed them in OnDefineOptions since last time.
                portModel.SetAttributes(attributes);
                portModel.Options = PortModelOptions.IsNodeOption;
            }
            else
            {
                portModel = new PortModel(this, PortDirection.Input, PortOrientation.Horizontal, optionName,
                    PortType.Default, dataType, portId, PortModelOptions.IsNodeOption, attributes, null);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
                GraphModel?.CurrentGraphChangeDescription.AddNewModel(portModel);
            }

            portModel.Capacity = PortCapacity.None;

            GraphModel?.RegisterPort(portModel);
            UpdateConstantForInput(portModel, initializationCallback, setterAction);

            return portModel;
        }

        /// <summary>
        /// Searches the graph and the options of the previous define pass for a port to reuse for a node option.
        /// </summary>
        /// <returns>The port to reuse, or null when none was found.</returns>
        /// <remarks>Because the port id is never empty, the port hash only depends on the state guid, the port id and
        /// the direction, so the port guid is stable when the data type of an option changes.</remarks>
        PortModel GetReusableOptionPort(string optionName, TypeHandle dataType, string portId)
        {
            if (GraphModel == null)
                return null;

            var hash = PortModel.ComputePortHash(this, PortDirection.Input, optionName, PortType.Default, dataType, portId, null);
            if (!GraphModel.TryGetModelFromGuid(hash, out PortModel result))
            {
                result = FindPreviousOptionPort(hash);
                if (result == null)
                    return null;
            }

            result.Title = optionName ?? "";
            result.DataTypeHandle = dataType;
            result.PortType = PortType.Default;

            return result;
        }

        PortModel FindPreviousOptionPort(Hash128 portGuid)
        {
            foreach (var previousOption in m_PreviousNodeOptions)
            {
                if (previousOption.PortModel.Guid == portGuid)
                    return previousOption.PortModel;
            }

            return null;
        }

        /// <summary>
        /// Unregisters the ports of the node options that are no longer defined and removes their constants.
        /// </summary>
        void RemoveObsoleteOptionPortsAndConstants()
        {
            List<PortModel> removedOptionPorts = null;
            foreach (var previousOption in m_PreviousNodeOptions)
            {
                var previousPort = previousOption.PortModel;
                if (m_NodeOptions.Exists(option => option.PortModel == previousPort))
                    continue;

                GraphModel?.UnregisterPort(previousPort);
                removedOptionPorts ??= new List<PortModel>();
                removedOptionPorts.Add(previousPort);
            }

            if (removedOptionPorts != null)
            {
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
                GraphModel?.CurrentGraphChangeDescription.AddDeletedModels(removedOptionPorts);
            }

            // Remove the constants of the options that are no longer defined. The keys are collected first because the
            // dictionary cannot be modified while it is enumerated.
            List<string> obsoleteConstantIds = null;
            foreach (var idAndConstant in m_OptionConstantsById)
            {
                if (m_NodeOptions.TrueForAll(option => option.PortModel.UniqueName != idAndConstant.Key))
                {
                    obsoleteConstantIds ??= new List<string>();
                    obsoleteConstantIds.Add(idAndConstant.Key);
                }
            }

            if (obsoleteConstantIds != null)
            {
                foreach (var id in obsoleteConstantIds)
                    m_OptionConstantsById.Remove(id);
            }
        }

        /// <inheritdoc />
        /// <remarks>Option ports are absent from <see cref="GetPorts"/>, and therefore from the dependent models the
        /// deletion walks, so they are unregistered here or they outlive the state in the graph's guid registry.</remarks>
        public override void OnDeleteNode()
        {
            base.OnDeleteNode();

            foreach (var nodeOption in m_NodeOptions)
            {
                GraphModel?.UnregisterPort(nodeOption.PortModel);
            }
        }

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();

            var baseName = string.IsNullOrEmpty(Title) ? k_DefaultName : Title;
            var uniqueName = GetUniqueName(baseName);
            if (uniqueName != Title)
                Title = uniqueName;

            DefineNode();
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);

            var newName = (sourceNode as IHasTitle)?.Title ?? "New State";
            Title = GetUniqueName(newName);

            DefineNode();
        }

        /// <inheritdoc />
        public override PortModel GetPortFitToConnectTo(PortModel portModel)
        {
            if (portModel.PortType == PortType.State)
            {
                switch (portModel.Direction)
                {
                    case PortDirection.Input:
                        return GetOutPort();
                    case PortDirection.Output:
                        return GetInPort();
                    case PortDirection.None:
                    default:
                        break;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override bool RemoveUnusedMissingPort(PortModel portModel) => false;

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            var setName = newName;
            if (string.IsNullOrEmpty(setName))
            {
                setName = k_DefaultName;
            }

            Title = setName;
        }

        string GetUniqueName(string name)
        {
            if (GraphModel == null)
                return name;

            var otherNames = new List<string>();
            foreach (var node in GraphModel.NodeModels)
            {
                if (node is IHasTitle hasTitle && !ReferenceEquals(hasTitle, this))
                {
                    otherNames.Add(hasTitle.Title);
                }
            }
            return ObjectNames.GetUniqueName(otherNames.ToArray(), name);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_OutPort is { NodeModel : null })
                m_OutPort.NodeModel = this;
            if (m_InPort is { NodeModel : null })
                m_InPort.NodeModel = this;

            m_ElementColor.OwnerElementModel = this;
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        [AutoStaticsCleanupOnCodeReload]
        static List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.createTransitionMenuItem,
            ContextualMenuHelpers.createSelfTransitionMenuItem,
            ContextualMenuHelpers.createPlacematItem,
            ContextualMenuHelpers.createLocalSubgraphFromSelectionItem,

            ContextualMenuHelpers.cutItem,
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.pasteAsNewMenuItem,

            ContextualMenuHelpers.renameItem,
            ContextualMenuHelpers.duplicateItem,
            ContextualMenuHelpers.deleteItem,

            ContextualMenuHelpers.frameSelectionItem,
            ContextualMenuHelpers.colorItem,
            ContextualMenuHelpers.disconnectAllTransitionsItem
        };
    }
}
