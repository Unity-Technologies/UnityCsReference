// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a single rule stacked on a <see cref="ITransition"/>.
    /// </summary>
    /// <remarks>
    /// A transition can hold several rules. Each rule owns a root <see cref="IGroupCondition"/> that expresses the
    /// conditions under which the transition is taken. Walk the condition hierarchy from
    /// <see cref="RootCondition"/> using <see cref="IGroupCondition.Get"/>.
    /// This interface is implemented by Unity and is not intended to be implemented by user code.
    /// </remarks>
    public interface ITransitionRule
    {
        /// <summary>
        /// The globally unique identifier for this rule.
        /// </summary>
        Hash128 ID { get; }

        /// <summary>
        /// The title of the rule.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Whether the rule is enabled. A disabled rule is never evaluated when the transition is taken.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// The root condition group for this rule.
        /// </summary>
        IGroupCondition RootCondition { get; }

        /// <summary>
        /// The transition this rule is stacked on, or <c>null</c> when the rule is not part of one.
        /// </summary>
        ITransition Transition { get; }
    }
}
