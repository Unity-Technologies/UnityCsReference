// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    partial class StateMachineImp
    {
        struct ChangedElementBuilder<T>
        {
            public readonly T Element;
            public readonly Hash128 Guid;
            public ChangeKind Kinds;

            public ChangedElementBuilder(Hash128 guid, T element)
            {
                Element = element;
                Guid = guid;
                Kinds = ChangeKind.None;
            }

            public void AddChange(ChangeKind kind)
            {
                Kinds |= kind;
            }
        }

        static ChangedElementBuilder<T> GetOrAddBuilder<T>(Dictionary<Hash128, ChangedElementBuilder<T>> builders,
            Hash128 guid, T element)
        {
            if (builders.TryGetValue(guid, out var builder))
                return builder;

            builder = new ChangedElementBuilder<T>(guid, element);
            builders[guid] = builder;
            return builder;
        }

        // Reused across CollectChangeData calls to avoid re-allocating the outer dictionaries. Cleared at the end of each call.
        [NonSerialized]
        Dictionary<Hash128, ChangedElementBuilder<IState>> m_StateBuilders = new();

        [NonSerialized]
        Dictionary<Hash128, ChangedElementBuilder<ITransition>> m_TransitionBuilders = new();

        [NonSerialized]
        Dictionary<Hash128, ChangedElementBuilder<ISubgraphState>> m_SubgraphStateBuilders = new();

        [NonSerialized]
        Dictionary<Hash128, ChangedElementBuilder<IVariable>> m_VariableBuilders = new();

        protected override void CollectChangeData(GraphChangeDescription changes, ILogger logger)
            => CollectChangeData(changes, logger as StateMachineLogger);

        void CollectChangeData(GraphChangeDescription changes, StateMachineLogger stateMachineLogger)
        {
            if (changes != null)
            {
                foreach (var guid in changes.NewModels)
                {
                    if (TryGetModelFromGuid(guid, out var model))
                        CollectModelChange(guid, model, ChangeKind.Added);
                }

                foreach (var kvp in changes.ChangedModels)
                {
                    if (!TryGetModelFromGuid(kvp.Key, out var model))
                        continue;

                    foreach (var hint in kvp.Value.Hints)
                        CollectModelChange(kvp.Key, model, hint.ToKind());
                }
            }

            m_DeletedPortToNodeGuid.Clear();

            var changedStates = new List<ChangedState>(m_StateBuilders.Count);
            foreach (var sb in m_StateBuilders.Values)
                changedStates.Add(new ChangedState(sb.Element, sb.Guid, sb.Kinds));

            var changedTransitions = new List<ChangedTransition>(m_TransitionBuilders.Count);
            foreach (var tb in m_TransitionBuilders.Values)
                changedTransitions.Add(new ChangedTransition(tb.Element, tb.Guid, tb.Kinds));

            var changedVariables = new List<ChangedVariable>(m_VariableBuilders.Count);
            foreach (var vb in m_VariableBuilders.Values)
                changedVariables.Add(new ChangedVariable(vb.Element, vb.Guid, vb.Kinds));

            var changedSubgraphStates = new List<ChangedSubgraphState>(m_SubgraphStateBuilders.Count);
            foreach (var ssb in m_SubgraphStateBuilders.Values)
                changedSubgraphStates.Add(new ChangedSubgraphState(ssb.Element, ssb.Guid, ssb.Kinds));

            stateMachineLogger?.SetChangeData(changedStates, changedTransitions, changedVariables, changedSubgraphStates);

            m_StateBuilders.Clear();
            m_TransitionBuilders.Clear();
            m_VariableBuilders.Clear();
            m_SubgraphStateBuilders.Clear();
        }

        void CollectModelChange(Hash128 guid, GraphElementModel model, ChangeKind kind)
        {
            if (model is UserStateModelImp stateImp)
            {
                if (stateImp.Node != null)
                    CollectStateChange(guid, stateImp.Node, kind);
            }
            else if (model is ISubgraphState subgraphState)
            {
                CollectSubgraphStateChange(guid, subgraphState, kind);
            }
            else if (model is IState state)
            {
                CollectStateChange(guid, state, kind);
            }
            else if (model is TransitionSupportModel support)
            {
                CollectTransitionChange(support, kind);
            }
            else if (model is TransitionModel rule)
            {
                CollectTransitionChange(rule.TransitionSupportModel, ChangeKind.Data);
            }
            else if (model is ConditionModel condition)
            {
                CollectTransitionChange(ErrorsAndWarningsImp.GetTransitionSupport(condition) as TransitionSupportModel, ChangeKind.Data);
            }
            else if (model is IVariable variable)
            {
                CollectVariableChange(guid, variable, kind);
            }
            else if (model is PortModel { NodeModel: StateModel ownerState })
            {
                if (kind == ChangeKind.Topology)
                    CollectTopologyChange(ownerState);
            }
        }

        void CollectVariableChange(Hash128 guid, IVariable variable, ChangeKind kind)
        {
            var builder = GetOrAddBuilder(m_VariableBuilders, guid, variable);
            builder.AddChange(kind);
            m_VariableBuilders[guid] = builder;
        }

        void CollectTopologyChange(StateModel state)
        {
            if (state != null)
                CollectModelChange(state.Guid, state, ChangeKind.Topology);
        }

        void CollectStateChange(Hash128 guid, IState state, ChangeKind kind)
        {
            var builder = GetOrAddBuilder(m_StateBuilders, guid, state);

            // Since state ports are not exposed to users, topology changes triggered by added ports should not be surfaced to users either.
            if (kind == ChangeKind.Topology && (builder.Kinds & ChangeKind.Added) != 0)
                return;

            builder.AddChange(kind);
            m_StateBuilders[guid] = builder;
        }

        void CollectSubgraphStateChange(Hash128 guid, ISubgraphState subgraphState, ChangeKind kind)
        {
            var builder = GetOrAddBuilder(m_SubgraphStateBuilders, guid, subgraphState);

            if (kind == ChangeKind.Topology && (builder.Kinds & ChangeKind.Added) != 0)
                return;

            builder.AddChange(kind);
            m_SubgraphStateBuilders[guid] = builder;
        }

        void CollectTransitionChange(TransitionSupportModel support, ChangeKind kind)
        {
            if (support == null)
                return;

            var builder = GetOrAddBuilder(m_TransitionBuilders, support.Guid, support.AsPublicTransition());
            builder.AddChange(kind);
            m_TransitionBuilders[support.Guid] = builder;
        }
    }
}
