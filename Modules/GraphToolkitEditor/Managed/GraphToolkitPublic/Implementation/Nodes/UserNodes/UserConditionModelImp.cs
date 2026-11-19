// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    /// <summary>
    /// Internal model that backs a user-defined <see cref="Condition"/> in a <see cref="StateMachine"/> graph.
    /// </summary>
    [Serializable]
    class UserConditionModelImp : ConditionModel, IHasInspectorSurrogate, IUserConditionModel, IComparisonConditionModel
    {
        [SerializeReference]
        Condition m_Condition;

        [SerializeField]
        ConditionComparison m_Comparison;

        public Condition Condition => m_Condition;

        /// <inheritdoc />
        ICondition IUserConditionModel.UserCondition => m_Condition;

        public ConditionComparison Comparison
        {
            get => m_Comparison;
            set
            {
                m_Comparison = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        // Lets the inspector display and edit the value field on the user Condition rather than on this model.
        public object Surrogate => m_Condition;

        public void InitCustomCondition(Condition condition)
        {
            m_Condition = condition;
            m_Condition.SetImplementation(this);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_Condition?.SetImplementation(this);
        }

        /// <inheritdoc />
        public override void OnAfterClone()
        {
            base.OnAfterClone();

            m_Condition?.SetImplementation(this);
        }

        /// <inheritdoc />
        public override string ToString(int indentLevel = 0)
        {
            var value = GetValueFieldInfo()?.GetValue(m_Condition);
            return GetIndentationString(indentLevel) + (value?.ToString() ?? m_Condition?.GetType().Name ?? string.Empty);
        }

        /// <summary>
        /// Returns the <see cref="Condition{T}.Value"/> field declared on the closed generic base of the user
        /// <see cref="Condition"/> type, or <see langword="null"/> when the model has no condition or the condition
        /// is valueless.
        /// </summary>
        internal FieldInfo GetValueFieldInfo()
        {
            for (var type = m_Condition?.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Condition<>))
                    return type.GetField(nameof(Condition<int>.Value));
            }

            return null;
        }
    }
}
