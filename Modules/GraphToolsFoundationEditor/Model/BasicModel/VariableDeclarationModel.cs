// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
{
    [Flags]
    enum ModifierFlags
    {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write,
    }

    /// <summary>
    /// Variable flags.
    /// </summary>
    [Flags]
    enum VariableFlags
    {
        /// <summary>
        /// Empty flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// The variable was automatically generated.
        /// </summary>
        Generated = 1,

        /// <summary>
        /// The variable is hidden.
        /// </summary>
        Hidden = 2,
    }

    /// <summary>
    /// A model that represents a variable declaration in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class VariableDeclarationModel : DeclarationModel, IGroupItemModel, ICopyPasteCallbackReceiver
    {
        [SerializeField, HideInInspector]
        TypeHandle m_DataType;
        [SerializeField]
        bool m_IsExposed;
        [SerializeField]
        string m_Tooltip;

        [SerializeReference]
        Constant m_InitializationValue;

        [SerializeField, HideInInspector]
        int m_Modifiers;

        [SerializeField, FormerlySerializedAs("variableFlags")]
        VariableFlags m_VariableFlags;

        /// <summary>
        /// The variable flags.
        /// </summary>
        public VariableFlags VariableFlags
        {
            get => m_VariableFlags;
            set
            {
                if (m_VariableFlags == value)
                    return;
                m_VariableFlags = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The read/write modifiers.
        /// </summary>
        public virtual ModifierFlags Modifiers
        {
            get => (ModifierFlags)m_Modifiers;
            set
            {
                if (m_Modifiers == (int)value)
                    return;
                m_Modifiers = (int)value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            GraphModel.RenameVariable(this, newName);

            var references = GraphModel.FindReferencesInGraph<VariableNodeModel>(this).OfType<AbstractNodeModel>();
            GraphModel.CurrentGraphChangeDescription?.AddChangedModels(references.Append<GraphElementModel>(this), ChangeHint.Data);

            if (this.IsInputOrOutput())
            {
                foreach (var recursiveSubgraphNode in GraphModel.GetRecursiveSubgraphNodes())
                    recursiveSubgraphNode.Update();
            }
        }

        /// <summary>
        /// Gets the name of the variable with non-alphanumeric characters replaced by an underscore.
        /// </summary>
        /// <returns>The name of the variable with non-alphanumeric characters replaced by an underscore.</returns>
        public virtual string GetVariableName() => Title.CodifyString_Internal();

        /// <inheritdoc />
        public virtual IEnumerable<GraphElementModel> ContainedModels => Enumerable.Repeat(this, 1);

        /// <summary>
        /// The type of the variable.
        /// </summary>
        public virtual TypeHandle DataType
        {
            get => m_DataType;
            set
            {
                if (m_DataType == value)
                    return;
                m_DataType = value;
                m_InitializationValue = null;
                if (GraphModel.Stencil.RequiresInitialization(this))
                    CreateInitializationValue();

                if (GraphModel != null)
                {
                    GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);

                    var variableReferences = GraphModel.FindReferencesInGraph<VariableNodeModel>(this);
                    foreach (var usage in variableReferences)
                    {
                        usage.UpdateTypeFromDeclaration();
                    }
                }
            }
        }

        /// <summary>
        /// Whether the variable is shown in the inspector.
        /// </summary>
        public virtual bool IsExposed
        {
            get => m_IsExposed;
            set
            {
                if (m_IsExposed == value)
                    return;
                m_IsExposed = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// A tooltip to show on nodes associated with this variable.
        /// </summary>
        public virtual string Tooltip
        {
            get => m_Tooltip;
            set
            {
                if (m_Tooltip == value)
                    return;
                m_Tooltip = value;
                if (GraphModel is { CurrentGraphChangeDescription: { } })
                {
                    GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
                    var references = GraphModel.FindReferencesInGraph<VariableNodeModel>(this);
                    GraphModel.CurrentGraphChangeDescription.AddChangedModels(references, ChangeHint.Style);
                }
            }
        }

        /// <summary>
        /// The default value for this variable.
        /// </summary>
        public virtual Constant InitializationModel
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
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public virtual GroupModel ParentGroup { get; set; }

        /// <summary>
        /// Sets the <see cref="InitializationModel"/> to a new value.
        /// </summary>
        public virtual void CreateInitializationValue()
        {
            Debug.Assert(GraphModel != null, $"{nameof(GraphModel)} needs to be set before calling {nameof(CreateInitializationValue)}.");

            if (GraphModel.Stencil.GetConstantType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);
                GraphModel.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Returns if this variable is used in the graph, it won't be selected when select unused is dispatched.
        /// </summary>
        /// <returns>If this variable is used in the graph, it won't be selected when select unused is dispatched.</returns>
        public virtual bool IsUsed()
        {
            foreach (var node in GraphModel.NodeModels.OfType<VariableNodeModel>())
            {
                if (ReferenceEquals(node.VariableDeclarationModel, this) && node.Ports.Any(t => t.IsConnected()))
                    return true;
            }

            return false;
        }

        bool Equals(VariableDeclarationModel other)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(other) && DataType.Equals(other.DataType) && IsExposed == other.IsExposed;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((VariableDeclarationModel)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
                int hashCode = base.GetHashCode();
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ m_DataType.GetHashCode();
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ m_IsExposed.GetHashCode();
                return hashCode;
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

            if (InitializationModel != null)
                InitializationModel.OwnerModel = this;
        }

        /// <inheritdoc />
        public virtual void OnBeforeCopy()
        {
            (InitializationModel as ICopyPasteCallbackReceiver)?.OnBeforeCopy();
        }

        /// <inheritdoc />
        public virtual void OnAfterPaste()
        {
            (InitializationModel as ICopyPasteCallbackReceiver)?.OnAfterPaste();
        }
    }
}
