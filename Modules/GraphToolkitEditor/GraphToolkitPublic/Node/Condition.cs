// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The base class for all user-accessible conditions in a <see cref="StateMachine"/> graph.
    /// </summary>
    /// <remarks>
    /// A condition is a single test evaluated on a transition; conditions are combined with the built-in group
    /// condition (logical <c>AND</c>/<c>OR</c>) to decide whether a transition is taken. Derive from
    /// <see cref="Condition{T}"/> to define a custom condition type with its serialized value, or derive from this
    /// class directly to define a valueless condition, such as a trigger, displayed as a label-only row in the
    /// transition inspector.
    ///
    /// To make a custom condition available in a state machine, decorate the derived type with
    /// <see cref="UseWithStateMachineAttribute"/> for the relevant <see cref="StateMachine"/> type. Conditions defined in the
    /// same assembly as the state machine are available without the attribute, unless the state machine sets
    /// <see cref="StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly"/>. Available conditions
    /// appear in the <c>Add</c> menu of a transition's condition list, labeled with the title of a
    /// <see cref="ConditionAttribute"/> applied to the condition type when present, otherwise with the condition type
    /// name formatted for display.
    ///
    /// See also:
    ///
    ///- <see cref="Condition{T}"/> for the base class used to define custom conditions
    ///- <see cref="ConditionView{T}"/> for adding custom UI to a condition's row in the transition inspector
    ///- <see cref="StateMachine"/> for the graph type that contains transitions and conditions
    ///- <see cref="State"/> for the base class used to define custom states
    ///- <see cref="IGroupCondition"/> for reading a transition's condition hierarchy
    ///
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// [Serializable]
    /// [UseWithStateMachine(typeof(MyStateMachine))]
    /// [Condition("Health Threshold")]
    /// public class Health : Condition<float>
    /// {
    ///     public override string Tooltip => "Compares the current health against this value.";
    ///     protected override bool DisplayComparisonDropdown => true;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [Serializable]
    public abstract partial class Condition : ICondition
    {
        /// <summary>
        /// The globally unique identifier for this condition.
        /// </summary>
        public Hash128 ID => GetImplementation().ID;

        /// <summary>
        /// The rule this condition belongs to, or <c>null</c> when the condition is not part of one.
        /// </summary>
        public ITransitionRule Rule => GetImplementation().Rule;

        /// <summary>
        /// The <see cref="StateMachine"/> that contains this condition, or <see langword="null"/> when the condition
        /// is not part of a state machine.
        /// </summary>
        public StateMachine StateMachine => (m_Implementation?.GraphModel as GraphModelImp)?.Graph as StateMachine;

        /// <summary>
        /// The label displayed for the condition in the transition inspector.
        /// </summary>
        /// <remarks>
        /// Override this property to replace the default label, which is the title of a
        /// <see cref="ConditionAttribute"/> applied to the condition type when present, otherwise the condition type
        /// name formatted for display. The title does not affect the label used in the add-condition menu.
        /// </remarks>
        public virtual string Title => null;

        /// <summary>
        /// The text displayed when hovering over the condition in the transition inspector.
        /// </summary>
        public virtual string Tooltip => null;

        
        /// <summary>
        /// Creates a new empty group condition.
        /// </summary>
        /// <returns>The newly created group condition.</returns>
        /// <remarks>
        /// The condition is not added to any group; add it with <see cref="IGroupCondition.Add"/>.
        /// </remarks>
        public static IGroupCondition CreateGroupCondition()
        {
            return new GroupConditionModel();
        }

        /// <summary>
        /// Creates a new variable condition.
        /// </summary>
        /// <param name="variable">The variable the condition compares.</param>
        /// <param name="defaultValue">The initial value the variable is compared against.</param>
        /// <returns>The newly created variable condition.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="variable"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The condition is not added to any group; add it with <see cref="IGroupCondition.Add"/>. The comparison
        /// operator of the new condition defaults to <see cref="ConditionComparison.Equal"/>.
        /// </remarks>
        public static IVariableCondition CreateVariableCondition(IVariable variable, object defaultValue, ConditionComparison comparison = ConditionComparison.Equal)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            var condition = new VariableConditionModel { GraphModel = ((VariableDeclarationModelBase)variable).GraphModel };
            condition.SetVariable((VariableDeclarationModelBase)variable);
            condition.Comparison = comparison;
            if (defaultValue != null && condition.Value != null)
                condition.Value.ObjectValue = defaultValue;

            return condition;
        }
    }

    /// <summary>
    /// The base class for user-accessible conditions holding a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the condition's value.</typeparam>
    /// <remarks>
    /// Inherit from this class to define a custom condition type. The <see cref="Value"/> field is serialized with
    /// the state machine and is displayed and edited in the transition inspector. To read the condition back from an
    /// asset, for example in an importer, cast the <see cref="ICondition"/> to the concrete derived type and read
    /// <see cref="Value"/> and <see cref="Comparison"/>. For a condition without a value, derive from
    /// <see cref="Condition"/> instead.
    /// </remarks>
    [Serializable]
    public abstract partial class Condition<T> : Condition
    {
        /// <summary>
        /// The value of the condition.
        /// </summary>
        /// <remarks>
        /// The field is serialized with the state machine asset, so <typeparamref name="T"/> must work with Unity
        /// serialization rules for <see cref="SerializeField"/>.
        /// </remarks>
        public T Value;

        /// <summary>
        /// The comparison operator applied to the condition's value.
        /// </summary>
        /// <remarks>
        /// The operator is edited in the transition inspector when <see cref="DisplayComparisonDropdown"/> is
        /// overridden to return <see langword="true"/>; otherwise it remains
        /// <see cref="ConditionComparison.Equal"/>. The condition only records the operator and the value; the
        /// consuming runtime is responsible for evaluating them.
        /// </remarks>
        public ConditionComparison Comparison => GetImplementation().Comparison;

        /// <summary>
        /// Whether the transition inspector displays a dropdown to select the condition's comparison operator.
        /// </summary>
        /// <remarks>
        /// Override this property to return <see langword="true"/> to let users pick the operator exposed by
        /// <see cref="Comparison"/>. The offered operators are determined by <see cref="SupportedComparisons"/>.
        /// </remarks>
        protected virtual bool DisplayComparisonDropdown => false;

        /// <summary>
        /// The comparison operators offered by the comparison dropdown.
        /// </summary>
        /// <remarks>
        /// By default this property returns <see langword="null"/> and the dropdown filters the operators by the
        /// value type: ordered types such as numeric types offer every <see cref="ConditionComparison"/> operator,
        /// other types offer equality operators only. Override this property to supply your own list of operators;
        /// an empty list behaves like <see langword="null"/> and falls back to the type-based defaults.
        /// It is only relevant when <see cref="DisplayComparisonDropdown"/> is overridden to return
        /// <see langword="true"/>.
        /// </remarks>
        protected virtual IReadOnlyList<ConditionComparison> SupportedComparisons => null;
    }
}
