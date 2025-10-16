// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [Serializable]
    [UnityRestricted]
    internal class TransitionInspectorStateComponent : PersistedStateComponent<TransitionInspectorStateComponent.StateUpdater>
    {
        [Serializable]
        struct CollapseState
        {
            [SerializeField]
            List<Hash128> m_CollapsedTransitions;

            public void AddCollapsedTransition(Model transition)
            {
                m_CollapsedTransitions ??= new List<Hash128>();
                if (!m_CollapsedTransitions.Contains(transition.Guid))
                    m_CollapsedTransitions.Add(transition.Guid);
            }

            public void RemoveCollapsedTransition(Model transition)
            {
                m_CollapsedTransitions?.Remove(transition.Guid);
            }

            public bool IsTransitionCollapsed(Model transition)
            {
                return m_CollapsedTransitions?.Contains(transition.Guid) ?? false;
            }

            public bool Move(CollapseState stateTransitionState, SimpleChangeset currentChangeset)
            {
                bool result = false;
                m_CollapsedTransitions ??= new List<Hash128>();
                var changedRows = new HashSet<Hash128>(m_CollapsedTransitions);
                if (stateTransitionState.m_CollapsedTransitions != null)
                    changedRows.SymmetricExceptWith(stateTransitionState.m_CollapsedTransitions);
                if (changedRows.Count != 0)
                {
                    currentChangeset.ChangedModels.UnionWith(changedRows);
                    result = true;

                    m_CollapsedTransitions = stateTransitionState.m_CollapsedTransitions;
                }
                stateTransitionState.m_CollapsedTransitions = null;
                return result;
            }
        }

        [SerializeField]
        CollapseState m_TransitionState;

        [SerializeField]
        CollapseState m_TransitionSupportState;

        [SerializeField]
        CollapseState m_StateTransitionState;

        [SerializeField]
        CollapseState m_StateTransitionSupportState;
        /// <summary>
        /// The updater for the <see cref="TransitionInspectorStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<TransitionInspectorStateComponent>, IOnGraphLoaded
        {
            /// <summary>
            /// Sets the collapsed state of the transition in the inspector.
            /// </summary>
            /// <param name="transitionModel">The model for which to set the state.</param>
            /// <param name="collapsed">True if the variable should be expanded, false otherwise.</param>
            /// <param name="onState">True if the collapsed state is for the state inspector, False if the collapsed state is for the transition support inspector.</param>
            public void SetTransitionCollapsed(TransitionModel transitionModel, bool collapsed, bool onState)
            {
                if (onState)
                {
                    if (collapsed)
                        m_State.m_StateTransitionState.AddCollapsedTransition(transitionModel);
                    else
                        m_State.m_StateTransitionState.RemoveCollapsedTransition(transitionModel);
                }
                else
                {
                    if (collapsed)
                        m_State.m_TransitionState.AddCollapsedTransition(transitionModel);
                    else
                        m_State.m_TransitionState.RemoveCollapsedTransition(transitionModel);
                }
                m_State.CurrentChangeset.ChangedModels.Add(transitionModel.Guid);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Sets the collapsed state of the transition in the inspector.
            /// </summary>
            /// <param name="transitionSupportModel">The model for which to set the state.</param>
            /// <param name="collapsed">True if the variable should be expanded, false otherwise.</param>
            /// <param name="onState">True if the collapsed state is for the state inspector, False if the collapsed state is for the transition support inspector.</param>
            public void SetTransitionSupportCollapsed(TransitionSupportModel transitionSupportModel, bool collapsed, bool onState)
            {
                if (onState)
                {
                    if (collapsed)
                        m_State.m_StateTransitionSupportState.AddCollapsedTransition(transitionSupportModel);
                    else
                        m_State.m_StateTransitionSupportState.RemoveCollapsedTransition(transitionSupportModel);
                }
                else
                {
                    if (collapsed)
                        m_State.m_TransitionSupportState.AddCollapsedTransition(transitionSupportModel);
                    else
                        m_State.m_TransitionSupportState.RemoveCollapsedTransition(transitionSupportModel);
                }
                m_State.CurrentChangeset.ChangedModels.Add(transitionSupportModel.Guid);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The asset for which we want to load a state component.</param>
            public void OnGraphLoaded(GraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }
        }

        ChangesetManager<SimpleChangeset> m_ChangesetManager = new();

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        SimpleChangeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public SimpleChangeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <summary>
        /// Gets the collapsed state of a transition model.
        /// </summary>
        /// <param name="model">The transition model.</param>
        /// <returns>True is the UI for the model should be collapsed. False otherwise.</returns>
        /// <param name="onState">True if the collapsed state is for the state inspector, False if the collapsed state is for the transition support inspector.</param>
        public bool GetTransitionModelCollapsed(TransitionModel model, bool onState)
        {
            if (onState)
                return m_StateTransitionState.IsTransitionCollapsed(model);
            return m_TransitionState.IsTransitionCollapsed(model);
        }

        /// <summary>
        /// Gets the collapsed state of a transition support model.
        /// </summary>
        /// <param name="model">The transition support model.</param>
        /// <returns>True is the UI for the model should be collapsed. False otherwise.</returns>
        /// <param name="onState">True if the collapsed state is for the state inspector, False if the collapsed state is for the transition support inspector.</param>
        public bool GetTransitionSupportModelCollapsed(TransitionSupportModel model, bool onState)
        {
            if (onState)
                return m_StateTransitionSupportState.IsTransitionCollapsed(model);
            return m_TransitionSupportState.IsTransitionCollapsed(model);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is TransitionInspectorStateComponent blackboardViewStateComponent)
            {
                bool result = m_StateTransitionState.Move(blackboardViewStateComponent.m_StateTransitionState, CurrentChangeset);
                result = m_TransitionState.Move(blackboardViewStateComponent.m_TransitionState, CurrentChangeset) || result;
                result = m_TransitionSupportState.Move(blackboardViewStateComponent.m_TransitionState, CurrentChangeset) || result;
                result = m_StateTransitionSupportState.Move(blackboardViewStateComponent.m_TransitionState, CurrentChangeset) || result;

                if (result)
                    SetUpdateType(UpdateType.Partial);
            }
        }
    }
}
