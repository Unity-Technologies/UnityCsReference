// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a condition attached to a state machine transition.
    /// </summary>
    /// <remarks>
    /// A transition is taken only when its conditions are met. Conditions are organized in groups:
    /// every transition has a root group condition, and each group combines its nested conditions
    /// with a logical operation. Use <see cref="IGroupCondition"/> to traverse the condition
    /// hierarchy from the root group.
    /// This interface is not intended to be implemented by user code; to author a custom condition,
    /// derive from <see cref="Condition{T}"/> instead.
    /// </remarks>
    public interface ICondition
    {
        /// <summary>
        /// The globally unique identifier for this condition.
        /// </summary>
        Hash128 ID { get; }

        /// <summary>
        /// The rule this condition belongs to, or <c>null</c> when the condition is not part of one.
        /// </summary>
        /// <remarks>
        /// Together with <see cref="ITransitionRule.Transition"/>, this walks up from a condition to
        /// the transition it takes part in. A condition that was created but never added to a group
        /// has no rule yet.
        /// </remarks>
        ITransitionRule Rule { get; }
    }
}
