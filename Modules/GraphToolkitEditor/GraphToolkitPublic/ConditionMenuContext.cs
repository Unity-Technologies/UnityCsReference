// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Argument passed to a contextual menu handler registered with
    /// <see cref="ConditionMenuAttribute"/>. Adds the transition and the rule whose condition list
    /// the user right-clicked on.
    /// </summary>
    /// <remarks>
    /// A transition can hold several rules, and each rule has its own condition list, so
    /// <see cref="Rule"/> is what the clicked list belongs to and <see cref="Transition"/> is the
    /// transition that rule is part of. Neither is ever <c>null</c>, unlike
    /// <see cref="MenuContext.ClickedObject"/>, which is <c>null</c> when the user right-clicks
    /// empty space in the list.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// [ConditionMenu(typeof(MyStateMachine))]
    /// static void AppendConditionItems(ConditionMenuContext context)
    /// {
    ///     context.AppendAction("Custom/Log rule", () => Debug.Log(context.Rule.Title));
    ///
    ///     if (context.ClickedObject is ICondition condition)
    ///         context.AppendAction("Custom/Log condition", () => Debug.Log(condition));
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public sealed class ConditionMenuContext : MenuContext
    {
        /// <summary>
        /// The state machine that owns the transition being inspected.
        /// </summary>
        public StateMachine StateMachine { get; }

        /// <summary>
        /// The transition being inspected.
        /// </summary>
        public ITransition Transition { get; }

        /// <summary>
        /// The rule of <see cref="Transition"/> whose condition list the user right-clicked on.
        /// </summary>
        public ITransitionRule Rule { get; }

        internal ConditionMenuContext(StateMachine stateMachine, ITransition transition, ITransitionRule rule,
            object clickedObject, Vector2 mousePosition, DropdownMenu menu)
            : base(clickedObject, mousePosition, menu)
        {
            StateMachine = stateMachine;
            Transition = transition;
            Rule = rule;
        }
    }
}
