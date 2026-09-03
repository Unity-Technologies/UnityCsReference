// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Describes a single state that was added, or modified, paired with the categories of changes that affected it.
    /// </summary>
    /// <remarks>
    /// A removed <see cref="IState"/> does not have a matching `ChangedState`.
    /// <br/>
    /// <br/>
    /// A subgraph state is reported through <see cref="ChangedSubgraphState"/> instead, in
    /// <see cref="StateMachineChanges.ChangedSubgraphStates"/>.
    /// <br/>
    /// <br/>
    /// For a usage example, see <see cref="StateMachineChanges"/>.
    /// </remarks>
    public readonly struct ChangedState
    {
        readonly IState m_State;
        readonly Hash128 m_ID;
        readonly ChangeKind m_ChangeKinds;

        internal ChangedState(IState state, Hash128 id, ChangeKind changeKinds)
        {
            m_State = state;
            m_ID = id;
            m_ChangeKinds = changeKinds;
        }

        /// <summary>The state that was modified.</summary>
        public IState State => m_State;

        /// <summary>The unique identifier of the state.</summary>
        public Hash128 ID => m_ID;

        /// <summary>
        /// The categories of changes that affected this state, as a set of <see cref="ChangeKind"/> flags.
        /// </summary>
        /// <remarks>
        /// Contains <see cref="ChangeKind.Added"/> when the state was added to the state machine, and
        /// <see cref="ChangeKind.Topology"/> when the transitions connected to the state changed, in addition
        /// to any change categories reported by the state machine (for example, <see cref="ChangeKind.Data"/> or
        /// <see cref="ChangeKind.Layout"/>).
        /// </remarks>
        public ChangeKind ChangeKinds => m_ChangeKinds;
    }

    /// <summary>
    /// Describes a single subgraph state that was added, or modified, paired with the categories of changes that affected it.
    /// </summary>
    /// <remarks>
    /// A removed <see cref="ISubgraphState"/> does not have a matching `ChangedSubgraphState`.
    /// <br/>
    /// <br/>
    /// For a usage example, see <see cref="StateMachineChanges"/>.
    /// </remarks>
    public readonly struct ChangedSubgraphState
    {
        readonly ISubgraphState m_SubgraphState;
        readonly Hash128 m_ID;
        readonly ChangeKind m_ChangeKinds;

        internal ChangedSubgraphState(ISubgraphState subgraphState, Hash128 id, ChangeKind changeKinds)
        {
            m_SubgraphState = subgraphState;
            m_ID = id;
            m_ChangeKinds = changeKinds;
        }

        /// <summary>The subgraph state that was modified.</summary>
        public ISubgraphState SubgraphState => m_SubgraphState;

        /// <summary>The unique identifier of the subgraph state.</summary>
        public Hash128 ID => m_ID;

        /// <summary>
        /// The categories of changes that affected this subgraph state, as a set of <see cref="ChangeKind"/> flags.
        /// </summary>
        /// <remarks>
        /// Contains <see cref="ChangeKind.Added"/> when the subgraph state was added to the state machine, and
        /// <see cref="ChangeKind.Topology"/> when the transitions connected to it changed, in addition to any
        /// change categories reported by the state machine (for example, <see cref="ChangeKind.Data"/>).
        /// </remarks>
        public ChangeKind ChangeKinds => m_ChangeKinds;
    }

    /// <summary>
    /// Describes a single transition that was added, or modified, paired with the categories of changes that affected it.
    /// </summary>
    /// <remarks>
    /// A removed <see cref="ITransition"/> does not have a matching `ChangedTransition`.
    /// <br/>
    /// <br/>
    /// Changes to the rules and conditions carried by a transition are categorized as <see cref="ChangeKind.Data"/>
    /// on the transition itself. Read <see cref="ITransition.GetRules"/> to find what changed.
    /// <br/>
    /// <br/>
    /// For a usage example, see <see cref="StateMachineChanges"/>.
    /// </remarks>
    public readonly struct ChangedTransition
    {
        readonly ITransition m_Transition;
        readonly Hash128 m_ID;
        readonly ChangeKind m_ChangeKinds;

        internal ChangedTransition(ITransition transition, Hash128 id, ChangeKind changeKinds)
        {
            m_Transition = transition;
            m_ID = id;
            m_ChangeKinds = changeKinds;
        }

        /// <summary>The transition that was modified.</summary>
        public ITransition Transition => m_Transition;

        /// <summary>The unique identifier of the transition.</summary>
        public Hash128 ID => m_ID;

        /// <summary>
        /// The categories of changes that affected this transition, as a set of <see cref="ChangeKind"/> flags.
        /// </summary>
        /// <remarks>
        /// Contains <see cref="ChangeKind.Added"/> when the transition was added to the state machine, and
        /// <see cref="ChangeKind.Data"/> when the transition itself, or any of its rules or conditions, changed.
        /// </remarks>
        public ChangeKind ChangeKinds => m_ChangeKinds;
    }

    /// <summary>
    /// The set of changes to a state machine reported to <see cref="StateMachine.OnStateMachineChanged"/> in a single
    /// change event.
    /// </summary>
    /// <remarks>
    /// Access this through <see cref="StateMachineLogger.StateMachineChanges"/> inside
    /// <see cref="StateMachine.OnStateMachineChanged"/>. Inspect <see cref="ChangedStates"/>,
    /// <see cref="ChangedTransitions"/>, <see cref="ChangedVariables"/>, and <see cref="ChangedSubgraphStates"/> to
    /// react to what was added or modified. Each entry's `ChangeKinds` property is a set of
    /// <see cref="ChangeKind"/> bit-flags describing the categories of change that apply.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// public override void OnStateMachineChanged(StateMachineLogger stateMachineLogger)
    /// {
    ///     StateMachineChanges changes = stateMachineLogger.StateMachineChanges;
    ///
    ///     foreach (ChangedState changedState in changes.ChangedStates)
    ///     {
    ///         if ((changedState.ChangeKinds & ChangeKind.Added) != 0)
    ///             Debug.Log($"State added: {changedState.State.Title}");
    ///
    ///         if ((changedState.ChangeKinds & ChangeKind.Topology) != 0 && !changedState.State.IsConnected)
    ///             stateMachineLogger.LogWarning("This state has no transitions.", changedState.State);
    ///     }
    ///
    ///     foreach (ChangedTransition changedTransition in changes.ChangedTransitions)
    ///     {
    ///         // Rule and condition edits arrive as ChangeKind.Data on the owning transition.
    ///         if ((changedTransition.ChangeKinds & ChangeKind.Data) == 0)
    ///             continue;
    ///
    ///         foreach (ITransitionRule rule in changedTransition.Transition.GetRules())
    ///             Debug.Log($"Rule updated: {rule.Title}");
    ///     }
    ///
    ///     foreach (ChangedVariable changedVariable in changes.ChangedVariables)
    ///         Debug.Log($"Variable changed: {changedVariable.Variable.Name} ({changedVariable.ChangeKinds})");
    ///
    ///     foreach (ChangedSubgraphState changedSubgraphState in changes.ChangedSubgraphStates)
    ///         Debug.Log($"Subgraph state changed: {changedSubgraphState.SubgraphState.Title}");
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class StateMachineChanges
    {
        IReadOnlyList<ChangedState> m_ChangedStates;
        IReadOnlyList<ChangedTransition> m_ChangedTransitions;
        IReadOnlyList<ChangedVariable> m_ChangedVariables;
        IReadOnlyList<ChangedSubgraphState> m_ChangedSubgraphStates;

        internal void SetChangeData(
            IReadOnlyList<ChangedState> changedStates,
            IReadOnlyList<ChangedTransition> changedTransitions,
            IReadOnlyList<ChangedVariable> changedVariables,
            IReadOnlyList<ChangedSubgraphState> changedSubgraphStates)
        {
            m_ChangedStates = changedStates ?? Array.Empty<ChangedState>();
            m_ChangedTransitions = changedTransitions ?? Array.Empty<ChangedTransition>();
            m_ChangedVariables = changedVariables ?? Array.Empty<ChangedVariable>();
            m_ChangedSubgraphStates = changedSubgraphStates ?? Array.Empty<ChangedSubgraphState>();
        }

        /// <summary>
        /// The states that were added or modified in this change event.
        /// </summary>
        /// <remarks>
        /// Each entry's <see cref="ChangedState.ChangeKinds"/> indicates the kind of change (for example, `"Added"`,
        /// `"Topology"`, or one of the state machine's standard change categories).
        /// Removed states are not reported.
        /// </remarks>
        public IReadOnlyList<ChangedState> ChangedStates => m_ChangedStates ?? Array.Empty<ChangedState>();

        /// <summary>
        /// The transitions that were added or modified in this change event.
        /// </summary>
        /// <remarks>
        /// Each entry's <see cref="ChangedTransition.ChangeKinds"/> indicates the kind of change (for example,
        /// `"Added"` or `"Data"`). Changes to a transition's rules and conditions are reported as
        /// <see cref="ChangeKind.Data"/> on the transition that owns them.
        /// </remarks>
        public IReadOnlyList<ChangedTransition> ChangedTransitions => m_ChangedTransitions ?? Array.Empty<ChangedTransition>();

        /// <summary>
        /// The variables that were added or modified in this change event.
        /// </summary>
        /// <remarks>
        /// Each entry's <see cref="ChangedVariable.ChangeKinds"/> indicates the kind of change (for example, `"Added"`,
        /// or one of the state machine's standard change categories).
        /// </remarks>
        public IReadOnlyList<ChangedVariable> ChangedVariables => m_ChangedVariables ?? Array.Empty<ChangedVariable>();

        /// <summary>
        /// The subgraph states that were added or modified in this change event.
        /// </summary>
        /// <remarks>
        /// Each entry's <see cref="ChangedSubgraphState.ChangeKinds"/> indicates the kind of change (for example,
        /// `"Added"`, `"Topology"`, or one of the state machine's standard change categories).
        /// Removed subgraph states are not reported.
        /// </remarks>
        public IReadOnlyList<ChangedSubgraphState> ChangedSubgraphStates => m_ChangedSubgraphStates ?? Array.Empty<ChangedSubgraphState>();
    }
}
