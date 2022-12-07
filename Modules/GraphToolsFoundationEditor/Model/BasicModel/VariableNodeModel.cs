// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class VariableNodeModel : NodeModel, ISingleInputPortNodeModel, ISingleOutputPortNodeModel, IHasDeclarationModel, IRenamable, ICloneable
    {
        const string k_MainPortName = "MainPortName";

        [SerializeReference]
        VariableDeclarationModel m_DeclarationModel;

        [SerializeField]
        SerializableGUID m_DeclarationModelGuid;

        protected PortModel m_MainPortModel;

        /// <summary>
        /// The human readable name of the data type of the variable declaration model.
        /// </summary>
        public virtual string DataTypeString => VariableDeclarationModel?.DataType.GetMetadata(GraphModel.Stencil).FriendlyName ?? string.Empty;

        /// <summary>
        /// The string used to describe this variable.
        /// </summary>
        public virtual string VariableString => DeclarationModel == null ? string.Empty : VariableDeclarationModel.IsExposed ? "Exposed variable" : "Variable";

        /// <inheritdoc />
        public override string Title => DeclarationModel == null ? "" : DeclarationModel.Title;

        /// <inheritdoc />
        public DeclarationModel DeclarationModel
        {
            get
            {
                if (m_DeclarationModel == null && GraphModel.TryGetModelFromGuid(m_DeclarationModelGuid, out var model) && model is VariableDeclarationPlaceholder missingDeclarationModel)
                {
                    this.SetCapability(Editor.Capabilities.Movable, false);
                    this.SetCapability(Editor.Capabilities.Copiable, false);
                    this.SetCapability(Editor.Capabilities.Droppable, false);

                    return missingDeclarationModel;
                }

                this.SetCapability(Editor.Capabilities.Movable, true);
                this.SetCapability(Editor.Capabilities.Copiable, true);
                this.SetCapability(Editor.Capabilities.Droppable, true);

                return m_DeclarationModel;
            }
            set
            {
                if (ReferenceEquals(m_DeclarationModel, value))
                    return;
                m_DeclarationModel = (VariableDeclarationModel)value;
                m_DeclarationModelGuid = m_DeclarationModel.Guid;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
                DefineNode();
            }
        }

        /// <summary>
        /// The <see cref="VariableDeclarationModel"/> associated with this node.
        /// </summary>
        public VariableDeclarationModel VariableDeclarationModel
        {
            get => DeclarationModel as VariableDeclarationModel;
            set => DeclarationModel = value;
        }

        /// <inheritdoc />
        public PortModel InputPort => m_MainPortModel?.Direction == PortDirection.Input ? m_MainPortModel : null;

        /// <inheritdoc />
        public PortModel OutputPort => m_MainPortModel?.Direction == PortDirection.Output ? m_MainPortModel : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableNodeModel"/> class.
        /// </summary>
        public VariableNodeModel()
        {
            m_Capabilities.Add(Editor.Capabilities.Renamable);
            this.SetCapability(Editor.Capabilities.Colorable, false);
        }

        /// <summary>
        /// Updates the port type from the variable declaration type.
        /// </summary>
        public virtual void UpdateTypeFromDeclaration()
        {
            if (DeclarationModel != null && m_MainPortModel != null)
            {
                m_MainPortModel.DataTypeHandle = VariableDeclarationModel.DataType;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }

            // update connected nodes' ports colors/types
            if (m_MainPortModel != null)
                foreach (var connectedPortModel in m_MainPortModel.GetConnectedPorts())
                    connectedPortModel.NodeModel.OnConnection(connectedPortModel, m_MainPortModel);
        }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            // used by macro outputs
            if (m_DeclarationModel != null /* this node */ && m_DeclarationModel.Modifiers.HasFlag(ModifierFlags.Write))
            {
                if (GetDataType() == TypeHandle.ExecutionFlow)
                    m_MainPortModel = this.AddExecutionInputPort(null);
                else
                    m_MainPortModel = this.AddDataInputPort(null, GetDataType(), k_MainPortName);
            }
            else
            {
                if (GetDataType() == TypeHandle.ExecutionFlow)
                    m_MainPortModel = this.AddExecutionOutputPort(null);
                else
                    m_MainPortModel = this.AddDataOutputPort(null, m_DeclarationModel == null ? TypeHandle.MissingPort : GetDataType(), k_MainPortName);
            }
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            DeclarationModel?.Rename(newName);
        }

        /// <inheritdoc />
        public GraphElementModel Clone()
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
                var tooltip = $"{VariableString}";
                if (!string.IsNullOrEmpty(DataTypeString))
                    tooltip += $" of type {DataTypeString}";
                if (!string.IsNullOrEmpty(VariableDeclarationModel?.Tooltip))
                    tooltip += "\n" + VariableDeclarationModel.Tooltip;

                if (string.IsNullOrEmpty(tooltip))
                    return base.Tooltip;

                return tooltip;
            }
            set => base.Tooltip = value;
        }

        /// <summary>
        /// Gets the data type of the variable node.
        /// </summary>
        /// <returns>The type of the variable declaration associated with this node, or <see cref="TypeHandle.Unknown"/> if there is none.</returns>
        public virtual TypeHandle GetDataType() => VariableDeclarationModel?.DataType ?? TypeHandle.Unknown;
    }
}
