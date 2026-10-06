// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a transition in a <see cref="StateMachine"/>.
    /// </summary>
    /// <remarks>
    /// A transition represents the connection from a source <see cref="IState"/> to a destination
    /// <see cref="IState"/>, or a self transition anchored on a single state (in which case
    /// <see cref="FromState"/> and <see cref="ToState"/> are the same state, and the transition is an
    /// <see cref="ISelfTransition"/>). A transition can carry several stacked rules; enumerate
    /// <see cref="GetRules"/> to inspect each <see cref="ITransitionRule"/> and the conditions under which
    /// the transition is taken.
    /// This interface is implemented by Unity and is not intended to be implemented by user code.
    /// </remarks>
    public interface ITransition
    {
        /// <summary>
        /// The globally unique identifier for this transition.
        /// </summary>
        Hash128 ID { get; }

        /// <summary>
        /// The state the transition originates from.
        /// </summary>
        IState FromState { get; }

        /// <summary>
        /// The state the transition goes to. This is the same state as <see cref="FromState"/> for a self transition.
        /// </summary>
        IState ToState { get; }

        /// <summary>
        /// Retrieves the rules stacked on this transition, in the order they appear.
        /// </summary>
        /// <remarks>
        /// A single transition can hold several <see cref="ITransitionRule"/>s. Each rule has its own set of
        /// conditions; the transition is taken when the conditions of one of its enabled rules are met.
        /// </remarks>
        /// <returns>The rules stacked on this transition, in the order they appear.</returns>
        IEnumerable<ITransitionRule> GetRules();

        /// <summary>
        /// The icon displayed for this transition in the transition inspector, or <see langword="null"/> if none is set.
        /// </summary>
        Texture2D Icon { get; set; }

        /// <summary>
        /// The fill color of the transition's arrow.
        /// </summary>
        /// <remarks>
        /// This value sets the transition arrow's fill color only. Use <see cref="LineColor"/> to color the transition wire and arrow border.
        /// </remarks>
        /// <example>
        /// <code>
        /// ITransition startTransition = myStateMachine.GetTransitions(startState, nextState).First();
        /// startTransition.FillColor = Color.green;
        /// </code>
        /// </example>
        Color FillColor { get; set; }

        /// <summary>
        /// The color of the transition's wire and arrow border.
        /// </summary>
        /// <remarks>
        /// This value sets the transition's wire and arrow border color only. Use <see cref="FillColor"/> to set the transition arrow's fill color.
        /// </remarks>
        /// <example>
        /// <code>
        /// ITransition startTransition = myStateMachine.GetTransitions(startState, nextState).First();
        /// startTransition.LineColor = Color.green;
        /// </code>
        /// </example>
        Color LineColor { get; set; }

        /// <summary>
        /// The line width override applied to the transition.
        /// </summary>
        /// <remarks>
        /// Setting the value to 0 removes the width override on the transition, so the transition reverts to the default thickness.
        /// On a state-to-state transition, this property sets the width of the transition's wire and of the border
        /// of its arrow. On a self transition, this property sets the width of the transition arrow's border.
        /// </remarks>
        /// <example>
        /// <code>
        /// ITransition startTransition = myStateMachine.GetTransitions(startState, nextState).First();
        /// startTransition.WidthOverride = 4f;
        /// </code>
        /// </example>
        float WidthOverride { get; set; }

        /// <summary>
        /// The opacity multiplier applied to the transition.
        /// </summary>
        /// <remarks>
        /// The opacity multiplier is clamped to the [0, 1] range when set.
        /// On a transition between two states, this property fades the transition wire and leaves the arrow solid.
        /// On a self transition, this property fades the transition arrow's border, leaving the arrow head solid.
        /// </remarks>
        /// <example>
        /// <code>
        /// ITransition startTransition = myStateMachine.GetTransitions(startState, nextState).First();
        /// startTransition.Opacity = 0.5f;
        /// </code>
        /// </example>
        float Opacity { get; set; }

        /// <summary>
        /// Whether the transition is drawn with a dashed pattern.
        /// </summary>
        /// <example>
        /// <code>
        /// ITransition startTransition = myStateMachine.GetTransitions(startState, nextState).First();
        /// startTransition.IsDashed = true;
        /// </code>
        /// </example>
        bool IsDashed { get; set; }

        /// <summary>
        /// The number of rules stacked on this transition.
        /// </summary>
        int RuleCount { get; }

        /// <summary>
        /// Retrieves the rule at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the rule to retrieve.</param>
        /// <returns>The rule at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="RuleCount"/>.
        /// </exception>
        /// <remarks>
        /// Throws <see cref="System.ArgumentOutOfRangeException"/> when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="RuleCount"/>.
        /// </remarks>
        ITransitionRule GetRule(int index);

        /// <summary>
        /// Creates a new empty transition rule and appends it to this transition.
        /// </summary>
        /// <returns>The newly created <see cref="ITransitionRule"/>.</returns>
        /// <remarks>
        /// The new rule is added at the end of the transition rule set.
        /// </remarks>
        ITransitionRule AddRule();

        /// <summary>
        /// Adds an existing rule to this transition.
        /// </summary>
        /// <param name="rule">The rule to add.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="rule"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="rule"/> is not a valid rule, already belongs to a transition in another state machine, or is not accepted by this transition.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if <paramref name="rule"/> is the last rule of the transition it belongs to.</exception>
        /// <remarks>
        /// If the <paramref name="rule"/> already belongs to another transition of the same state machine, it is first
        /// removed from that transition: the rule is moved, not copied. A rule cannot be moved between state machines.
        /// Because a transition always keeps at least one rule, a rule cannot be moved out of a transition that holds it
        /// as its only rule; use <see cref="StateMachine.Disconnect"/> to remove that transition instead.
        /// The rule is added at the end of the transition rule set.
        /// Throws <see cref="System.ArgumentNullException"/> when <paramref name="rule"/> is <c>null</c>.
        /// Throws <see cref="System.ArgumentException"/> when <paramref name="rule"/> is not a valid rule, already belongs to a transition in another state machine, or is not accepted by this transition.
        /// Throws <see cref="System.InvalidOperationException"/> when <paramref name="rule"/> is the last rule of the transition it belongs to.
        /// </remarks>
        void AddRule(ITransitionRule rule);

        /// <summary>
        /// Removes a rule from this transition.
        /// </summary>
        /// <param name="rule">The rule to remove. Must belong to this transition.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="rule"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="rule"/> does not belong to this transition.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if <paramref name="rule"/> is the last rule of this transition.</exception>
        /// <remarks>
        /// A transition always keeps at least one rule. To remove all rules, use <see cref="StateMachine.Disconnect"/> instead.
        /// Throws <see cref="System.ArgumentNullException"/> when <paramref name="rule"/> is <c>null</c>.
        /// Throws <see cref="System.ArgumentException"/> when <paramref name="rule"/> does not belong to this transition.
        /// Throws <see cref="System.InvalidOperationException"/> when <paramref name="rule"/> is the last rule of this transition.
        /// </remarks>
        void RemoveRule(ITransitionRule rule);
    }
}
