// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents a variable declaration in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class VariableDeclarationModel : VariableDeclarationModelBase
    {
        const VariableScope k_VariableScopeMigrationValue = (VariableScope)int.MaxValue;

        [SerializeField, HideInInspector]
        TypeHandle m_DataType;

#pragma warning disable CS0649
        [SerializeField, HideInInspector, Obsolete("Exposed is no longer used : use Scope")]
        bool m_IsExposed;
#pragma warning restore CS0649

        [SerializeField, HideInInspector]
        VariableScope m_Scope = k_VariableScopeMigrationValue;

        [SerializeField, HideInInspector]
        bool m_ShowOnInspectorOnly = false;

        [SerializeField, HideInInspector]
        string m_Tooltip;

        [SerializeReference]
        Constant m_InitializationValue;

        [SerializeField, HideInInspector]
        int m_Modifiers;

        [SerializeField, FormerlySerializedAs("variableFlags"), HideInInspector]
        VariableFlags m_VariableFlags;

        /// <inheritdoc />
        public override VariableFlags VariableFlags
        {
            get => m_VariableFlags;
            set
            {
                if (m_VariableFlags == value)
                    return;
                m_VariableFlags = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override ModifierFlags Modifiers
        {
            get => (ModifierFlags)m_Modifiers;
            set
            {
                if (m_Modifiers == (int)value)
                    return;
                m_Modifiers = (int)value;

                foreach (var variableNodeModel in GraphModel.FindReferencesInGraph<VariableNodeModel>(this))
                {
                    if (ReferenceEquals(variableNodeModel.DeclarationModel, this))
                    {
                        variableNodeModel.DefineNode();
                    }
                }

                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override VariableScope Scope
        {
            get => m_Scope;
            set
            {
                if (m_Scope == value)
                    return;
                m_Scope = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override bool ShowOnInspectorOnly
        {
            get => m_ShowOnInspectorOnly;
            set
            {
                if (m_ShowOnInspectorOnly == value)
                    return;
                m_ShowOnInspectorOnly = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override void Rename(string newName)
        {
            if (!IsRenamable())
                return;

            GraphModel.RenameVariable(this, newName);

            if (IsInputOrOutput)
            {
                foreach (var recursiveSubgraphNode in GraphModel.GetSelfReferringSubgraphNodes())
                    recursiveSubgraphNode.Update();
            }
        }

        /// <inheritdoc />
        public override TypeHandle DataType
        {
            get => m_DataType;
            set
            {
                if (m_DataType == value)
                    return;
                m_DataType = value;
                m_InitializationValue = null;
                if (GraphModel != null)
                {
                    if (GraphModel.VariableDeclarationRequiresInitialization(this))
                        CreateInitializationValue();

                    GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);

                    var variableReferences = GraphModel.FindReferencesInGraph<VariableNodeModel>(this);
                    foreach (var usage in variableReferences)
                    {
                        usage.UpdateTypeFromDeclaration();
                    }
                }
            }
        }

        /// <inheritdoc />
        public override string Tooltip
        {
            get => string.IsNullOrEmpty(m_Tooltip) ? DefaultTooltip : m_Tooltip;
            set
            {
                if (m_Tooltip == value)
                    return;
                m_Tooltip = value;
                if (GraphModel is { CurrentGraphChangeDescription: not null })
                {
                    using var dirtyScope = GraphModel.AssetDirtyScope();

                    GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
                    var references = GraphModel.FindReferencesInGraph<VariableNodeModel>(this);
                    foreach (var reference in references)
                    {
                        GraphModel.CurrentGraphChangeDescription.AddChangedModel(reference, ChangeHint.Style);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override Constant InitializationModel
        {
            get => m_InitializationValue;
            set
            {
                if (m_InitializationValue == value)
                    return;
                // Unregister ourselves as the owner of the old constant.
                if (m_InitializationValue != null)
                    m_InitializationValue.OwnerModel = null;
                m_InitializationValue = value;
                if (m_InitializationValue != null)
                    m_InitializationValue.OwnerModel = this;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override void CreateInitializationValue()
        {
            Debug.Assert(GraphModel != null, $"{nameof(GraphModel)} needs to be set before calling {nameof(CreateInitializationValue)}.");

            if (GraphModel?.GetConstantType(DataType) != null && !DataType.IsCustomTypeHandle())
            {
                InitializationModel = GraphModel.CreateConstantValue(DataType);
                GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (Version <= SerializationVersion.GTF_V_0_13_0)
            {
                if (m_Modifiers == 1 << 2)
                {
                    m_Modifiers = (int)ModifierFlags.ReadWrite;
                }
            }

            if (m_Scope == k_VariableScopeMigrationValue)
            {
#pragma warning disable CS0612
#pragma warning disable CS0618
                m_Scope = m_IsExposed ? VariableScope.Exposed : VariableScope.Local;
#pragma warning restore CS0612
#pragma warning restore CS0618
            }

            if (InitializationModel != null)
                InitializationModel.OwnerModel = this;
        }
    }
}
