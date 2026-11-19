// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="ConditionModel"/> that compares a blackboard variable against a constant value.
    /// </summary>
    /// <remarks>
    /// The condition only records the variable, the operator and the value to compare against; it does not
    /// evaluate them. The consuming runtime is responsible for evaluation and should compare using
    /// <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/> for equality, so comparison-value
    /// types that need value semantics should implement <see cref="IEquatable{T}"/>.
    /// </remarks>
    [Serializable]
    internal class VariableConditionModel : ConditionModel, IComparisonConditionModel, IVariableCondition
    {
        [SerializeField]
        Hash128 m_VariableGuid;

        [SerializeField]
        ConditionComparison m_Comparison;

        [SerializeReference]
        Constant m_Value;

        // The graph the variable was assigned from. Only used to validate a condition that is temporarily detached
        // from any graph, for example while it sits in a group that has not been attached to a graph yet. It is not
        // serialized: a deserialized condition always comes back attached to its graph, so GraphModel is enough.
        [NonSerialized]
        GraphModel m_VariableGraphModel;

        /// <summary>
        /// Whether a variable has been assigned to this condition.
        /// </summary>
        public bool HasVariable => m_VariableGuid.isValid;

        /// <summary>
        /// Gets a display title for the variable reference: the variable name, or a placeholder when none is
        /// assigned or the variable has been deleted.
        /// </summary>
        /// <returns>The variable's title, "(No variable)" when none is assigned, or "(Missing)" when deleted.</returns>
        public string GetDisplayTitle()
        {
            if (!m_VariableGuid.isValid)
                return "(No variable)";
            return Variable?.Title ?? "(Missing)";
        }

        /// <summary>
        /// The variable declaration this condition compares, or null when none is assigned or it was deleted.
        /// </summary>
        public VariableDeclarationModelBase Variable
        {
            get
            {
                if (m_VariableGuid.isValid && GraphModel != null
                    && GraphModel.TryGetModelFromGuid<VariableDeclarationModelBase>(m_VariableGuid, out var declaration))
                    return declaration;
                return null;
            }
        }

        /// <summary>
        /// Checks whether the assigned variable can be used in the specified <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="graphModel">The graph model in which the variable would be used.</param>
        /// <returns>False when the assigned variable belongs to another graph; true otherwise.</returns>
        /// <remarks>
        /// A condition that is detached from any graph has no <see cref="GraphModel"/> to resolve its variable
        /// against, so the graph the variable was assigned from is used instead. A variable that resolves in no
        /// graph at all -- one that was deleted -- does not belong to another graph and is accepted.
        /// </remarks>
        public bool CanUseVariableIn(GraphModel graphModel)
        {
            if (!m_VariableGuid.isValid)
                return true;

            var variableGraphModel = GraphModel ?? m_VariableGraphModel;
            if (variableGraphModel == null || variableGraphModel == graphModel)
                return true;

            return !variableGraphModel.TryGetModelFromGuid<VariableDeclarationModelBase>(m_VariableGuid, out _);
        }

        /// <summary>
        /// The comparison operator.
        /// </summary>
        public ConditionComparison Comparison
        {
            get => m_Comparison;
            set
            {
                m_Comparison = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The constant value the variable is compared against.
        /// </summary>
        public Constant Value => m_Value;

        IVariable IVariableCondition.Variable => Variable;

        object IVariableCondition.Value => Variable == null ? null : m_Value?.ObjectValue;

        /// <summary>
        /// Assigns the variable this condition compares.
        /// </summary>
        /// <param name="variable">The variable declaration, or null to clear the assignment.</param>
        /// <remarks>
        /// When the new variable's type differs from the current value's type, the value is recreated with the
        /// new type's default and the operator is reset to <see cref="ConditionComparison.Equal"/>.
        /// </remarks>
        public void SetVariable(VariableDeclarationModelBase variable)
        {
            m_VariableGuid = variable?.Guid ?? default;
            m_VariableGraphModel = variable?.GraphModel;

            if (variable == null)
                m_Value = null;
            else if (m_Value == null || m_Value.GetTypeHandle() != variable.DataType)
                RecreateValue(variable.DataType);

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        void RecreateValue(TypeHandle dataType)
        {
            m_Value = GraphModel?.CreateConstantValue(dataType);
            FixupValueOwner();
            m_Comparison = ConditionComparison.Equal;
        }

        void FixupValueOwner()
        {
            if (m_Value != null)
                m_Value.OwnerModel = this;
        }

        /// <summary>
        /// Recreates the value and resets the operator when the assigned variable's type no longer matches the
        /// stored value's type (for example after the variable's type changed in the blackboard).
        /// </summary>
        /// <returns>True if the value was reconciled to a new type; false otherwise.</returns>
        public bool ReconcileValueType()
        {
            var variable = Variable;
            if (variable == null || (m_Value != null && m_Value.GetTypeHandle() == variable.DataType))
                return false;

            RecreateValue(variable.DataType);
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            return true;
        }

        /// <inheritdoc />
        public override string ToString(int indentLevel = 0)
        {
            var indent = GetIndentationString(indentLevel);
            var name = GetDisplayTitle();
            if (!m_VariableGuid.isValid)
                return indent + name;
            var value = m_Value?.ObjectValue?.ToString() ?? string.Empty;
            return $"{indent}{name} {m_Comparison.ToGlyph()} {value}";
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            FixupValueOwner();
        }

        /// <inheritdoc />
        public override void OnAfterClone()
        {
            base.OnAfterClone();
            FixupValueOwner();
        }
    }
}
