// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Model that represents a variable node.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal partial class VariableNodeModel : NodeModel, ISingleInputPortNodeModel, ISingleOutputPortNodeModel, IHasDeclarationModel, ICloneable
    {
        protected const string k_MainPortName = "MainPortName";
        protected const string k_ValuePortName = "Value";
        protected const string k_OutPortName = "Out";

        [SerializeReference]
        VariableDeclarationModelBase m_DeclarationModel;

        [SerializeField, HideInInspector]
        Hash128 m_DeclarationModelHashGuid;

        [SerializeField, HideInInspector]
        VariableNodeMode m_Mode;

        protected PortModel m_MainPortModel;

        /// <inheritdoc />
        public override string Title => DeclarationModel == null ? "" : DeclarationModel.Title;
        public PortModel MainPortModel => m_MainPortModel;
        public PortModel ValuePortModel { get; private set; }

        /// <inheritdoc />
        public override bool HasAccentBar => Mode == VariableNodeMode.Set;

        public VariableNodeMode Mode
        {
            get => m_Mode == VariableNodeMode.Set && (VariableDeclarationModel?.CanCreateSetVariableNode ?? false) ? VariableNodeMode.Set : VariableNodeMode.Get;
            set
            {
                if (Mode == value)
                    return;

                if (value == VariableNodeMode.Set && VariableDeclarationModel?.CanCreateSetVariableNode != true)
                {
                    Debug.LogError($"Variable '{Title}' cannot be used as a set variable node");
                    return;
                }

                m_Mode = value;
                DefineNode();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.RecreateView);
            }
        }

        /// <inheritdoc />
        public DeclarationModel DeclarationModel
        {
            get
            {
                if (m_DeclarationModel == null && PlaceholderModelHelper.TryGetPlaceholderGraphElementModel(GraphModel, m_DeclarationModelHashGuid, out var placeholderModel))
                {
                    PlaceholderModelHelper.SetPlaceholderCapabilities(this);
                    return placeholderModel as DeclarationModel;
                }

                return m_DeclarationModel;
            }
        }

        /// <inheritdoc />
        public void SetDeclarationModel(DeclarationModel value)
        {
            if (ReferenceEquals(m_DeclarationModel, value))
                return;
            var previousMode = Mode;
            m_DeclarationModel = (VariableDeclarationModelBase)value;
            m_DeclarationModelHashGuid = m_DeclarationModel.Guid;
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            if (m_MainPortModel != null) // We need to update the port type if the declaration model changes, but only if the port has been created. This prevent a double call to OnDefineNode on creation.
                DefineNode();
            if (Mode != previousMode)
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.RecreateView);
        }

        /// <summary>
        /// Gets the <see cref="VariableDeclarationModelBase"/> associated with this node.
        /// </summary>
        public VariableDeclarationModelBase VariableDeclarationModel => DeclarationModel as VariableDeclarationModelBase;

        /// <summary>
        /// Sets the <see cref="VariableDeclarationModelBase"/> associated with this node.
        /// </summary>
        public void SetVariableDeclarationModel(VariableDeclarationModelBase value) => SetDeclarationModel(value);

        /// <summary>
        /// Sets the node mode during initial creation, before <see cref="OnDefineNode"/> runs.
        /// </summary>
        internal void InitializeMode(VariableNodeMode mode)
        {
            m_Mode = mode;
        }

        /// <inheritdoc />
        public PortModel InputPort => Mode == VariableNodeMode.Set ? ValuePortModel : (m_MainPortModel?.Direction == PortDirection.Input ? m_MainPortModel : null);

        /// <inheritdoc />
        public PortModel OutputPort => m_MainPortModel?.Direction == PortDirection.Output ? m_MainPortModel : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableNodeModel"/> class.
        /// </summary>
        public VariableNodeModel()
        {
            SetCapability(Editor.Capabilities.Colorable, false);
        }

        /// <summary>
        /// Updates the port type from the variable declaration type.
        /// </summary>
        public virtual void UpdateTypeFromDeclaration()
        {
            if (m_MainPortModel?.DataTypeHandle != VariableDeclarationModel.DataType)
            {
                DefineNode();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);

                // update connected nodes' ports colors/types
                if (m_MainPortModel != null)
                    foreach (var connectedPortModel in m_MainPortModel.GetConnectedPorts())
                        connectedPortModel.NodeModel.OnConnection(connectedPortModel, m_MainPortModel);
            }
        }

        /// <inheritdoc />
        public override bool CanBeItemized()
        {
            return base.CanBeItemized() && Mode == VariableNodeMode.Get;
        }

        /// <inheritdoc />
        protected override void OnDefineNode(NodeDefinitionScope scope)
        {
            if (Mode == VariableNodeMode.Set)
            {
                ValuePortModel = scope.AddInputPort(k_ValuePortName, DataType, initializationCallback:
                    c =>
                    {
                        // The initial value of the Value port should be equal to the variable declaration's default value.
                        if (VariableDeclarationModel?.InitializationModel != null)
                            c.ObjectValue = VariableDeclarationModel.InitializationModel.ObjectValue;
                    });
            }
            else
            {
                ValuePortModel = null;
            }

            if (m_DeclarationModel != null && m_DeclarationModel.Modifiers.HasFlag(ModifierFlags.Write))
                m_MainPortModel = scope.AddInputPort(k_MainPortName, DataType, options : PortModelOptions.NoEmbeddedConstant);
            else
                m_MainPortModel = scope.AddOutputPort(Mode == VariableNodeMode.Set ? k_OutPortName : k_MainPortName, m_DeclarationModel == null ? TypeHandle.MissingPort : DataType, portId: k_MainPortName);
        }

        /// <inheritdoc />
        public Model Clone()
        {
            var decl = m_DeclarationModel;
            try
            {
                m_DeclarationModel = null;
                var clone = CloneHelpers.CloneUsingScriptableObjectInstantiate(this);
                clone.m_DeclarationModel = decl;
                return clone;
            }
            finally
            {
                m_DeclarationModel = decl;
            }
        }

        /// <inheritdoc />
        public override string Tooltip
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Tooltip))
                    return m_Tooltip;
                return !string.IsNullOrEmpty(VariableDeclarationModel.Tooltip) ? VariableDeclarationModel.Tooltip : base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        /// <summary>
        /// Gets the data type of the variable node.
        /// </summary>
        /// <value>The type of the variable declaration associated with this node, or <see cref="TypeHandle.Unknown"/> if there is none.</value>
        public virtual TypeHandle DataType => VariableDeclarationModel?.DataType ?? TypeHandle.Unknown;

        /// <summary>
        /// Indicates whether this variable node can be converted to a constant.
        /// </summary>
        /// <returns>True if the variable node can be converted to a constant, false if not.</returns>
        public virtual bool CanConvertToConstant()
        {
            foreach (var outputPortModel in OutputsByDisplayOrder)
            {
                if (outputPortModel.PortType == PortType.Default)
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var menuItems = new List<ContextualMenuItem>(base.ContextualMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        static List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.convertToConstantItem,
            new ContextualMenuItem(ContextualMenuHelpers.itemizeItem, 0),
        };
    }
}
