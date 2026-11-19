// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for the built-in condition that compares a graph variable against a constant value.
    /// </summary>
    /// <remarks>
    /// A variable condition records the variable to compare, the comparison operator, and the constant value
    /// to compare against; it does not evaluate them. When traversing a transition's condition tree with
    /// <see cref="IGroupCondition.Get"/>, test each element for this interface to read variable
    /// conditions. <see cref="Variable"/> and <see cref="Value"/> are <c>null</c> when no variable is assigned
    /// to the condition or when the assigned variable was deleted from the graph.
    /// This interface is implemented by Unity and is not intended to be implemented by user code.
    /// </remarks>
    public interface IVariableCondition : ICondition
    {
        /// <summary>
        /// The variable the condition compares, or <c>null</c> when no variable is assigned or the variable
        /// was deleted from the graph.
        /// </summary>
        IVariable Variable { get; }

        /// <summary>
        /// The comparison operator applied between the variable and <see cref="Value"/>.
        /// </summary>
        ConditionComparison Comparison { get; }

        /// <summary>
        /// The constant value the variable is compared against, boxed as <see cref="object"/>, or <c>null</c>
        /// when no variable is assigned.
        /// </summary>
        /// <remarks>
        /// The value's runtime type matches the data type of <see cref="Variable"/>.
        /// </remarks>
        object Value { get; }
    }
}
