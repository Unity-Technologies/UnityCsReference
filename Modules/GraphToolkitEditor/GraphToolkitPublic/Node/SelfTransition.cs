// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The base class for all user-accessible self transitions in a <see cref="StateMachine"/> graph.
    /// </summary>
    /// <remarks>
    /// Inherit from this class to define custom self transition types that appear in a state machine graph. A
    /// self transition is anchored on a single <see cref="State"/> rather than connecting two states.
    ///
    /// To create a custom self transition, derive from <see cref="SelfTransition"/>.
    /// Apply a <see cref="TransitionAttribute"/> to the class to further customize the self transition.
    /// Custom transitions appear in the <c>Create Transition</c> menu of a state and can be created on it.
    ///
    /// See also:
    ///
    ///- <see cref="StateMachine"/> for the graph type that contains states and transitions
    ///- <see cref="State"/> for the base class used to define custom states
    ///
    /// </remarks>
    [Serializable]
    public abstract partial class SelfTransition : ISelfTransition
    {
        /// <summary>
        /// The default color of the transition.
        /// </summary>
        public Color DefaultColor
        {
            get => GetImplementation().DefaultColor;
            set => GetImplementation().DefaultColor = value;
        }

        /// <summary>
        /// The text displayed when hovering over the transition.
        /// </summary>
        public string Tooltip
        {
            get => GetImplementation().Tooltip;
            set => GetImplementation().Tooltip = value;
        }

        /// <inheritdoc cref="ITransition.ID" />
        public Hash128 ID => GetImplementation().ID;

        /// <inheritdoc cref="ITransition.FromState" />
        public IState FromState => GetImplementation().FromState;

        /// <inheritdoc cref="ITransition.ToState" />
        public IState ToState => GetImplementation().ToState;

        /// <inheritdoc cref="ITransition.GetRules" />
        public IEnumerable<ITransitionRule> GetRules() => GetImplementation().GetRules();

        /// <inheritdoc cref="ITransition.RuleCount" />
        public int RuleCount => GetImplementation().RuleCount;

        /// <inheritdoc cref="ITransition.GetRule" />
        public ITransitionRule GetRule(int index) => GetImplementation().GetRule(index);

        /// <inheritdoc cref="ITransition.AddRule()" />
        public ITransitionRule AddRule() => GetImplementation().AddRule();

        /// <inheritdoc cref="ITransition.AddRule(ITransitionRule)" />
        public void AddRule(ITransitionRule rule) => GetImplementation().AddRule(rule);

        /// <inheritdoc cref="ITransition.RemoveRule" />
        public void RemoveRule(ITransitionRule rule) => GetImplementation().RemoveRule(rule);

        /// <summary>
        /// Called when the transition is created or when the state machine is enabled.
        /// </summary>
        /// <remarks>
        /// Use this method to perform initialization logic.
        /// </remarks>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the transition is removed or when the graph is disabled.
        /// </summary>
        /// <remarks>
        /// Use this method to perform any cleanup logic.
        /// </remarks>
        public virtual void OnDisable() { }
    }
}
