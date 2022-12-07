// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An observer that automatically processes the graph when it is updated.
    /// </summary>
    /// <remarks>If the preference <see cref="BoolPref.OnlyProcessWhenIdle"/> is true,
    /// the graph will only be processed if the <see cref="ProcessOnIdleStateComponent"/>
    /// has pending changes.
    /// </remarks>
    class AutomaticGraphProcessingObserver : StateObserver
    {
        GraphModelStateComponent m_GraphModelStateComponent;
        ProcessOnIdleStateComponent m_ProcessOnIdleStateComponent;
        Preferences m_Preferences;
        GraphProcessingStateComponent m_GraphProcessingStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticGraphProcessingObserver" /> class.
        /// </summary>
        /// <param name="graphModelStateComponent">The graph model state component.</param>
        /// <param name="processOnIdleComponent">The component that tells us to process the graph when the mouse is idle.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        /// <param name="preferences">The tool preferences.</param>
        public AutomaticGraphProcessingObserver(GraphModelStateComponent graphModelStateComponent, ProcessOnIdleStateComponent processOnIdleComponent, GraphProcessingStateComponent graphProcessingState, Preferences preferences)
            : base(new IStateComponent[]
                {
                    graphModelStateComponent,
                    processOnIdleComponent,
                },
                new[]
                {
                    graphProcessingState
                })
        {
            m_GraphModelStateComponent = graphModelStateComponent;
            m_ProcessOnIdleStateComponent = processOnIdleComponent;
            m_Preferences = preferences;
            m_GraphProcessingStateComponent = graphProcessingState;
        }

        void DoGraphObservation()
        {
            using var graphObservation = this.ObserveState(m_GraphModelStateComponent);
            var gvUpdateType = graphObservation.UpdateType;
            GraphModelStateComponent.Changeset changeset = null;
            IReadOnlyList<GraphProcessingResult> results = null;

            if (gvUpdateType == UpdateType.Partial)
            {
                changeset = m_GraphModelStateComponent.GetAggregatedChangeset(graphObservation.LastObservedVersion);
            }

            if (gvUpdateType != UpdateType.None)
            {
                results = GraphProcessingHelper.ProcessGraph(m_GraphModelStateComponent.GraphModel, changeset, RequestGraphProcessingOptions.Default);
            }

            // Avoid using an updater if we do not need to.
            if (results != null || m_GraphProcessingStateComponent.GraphProcessingPending)
            {
                using var updater = m_GraphProcessingStateComponent.UpdateScope;
                updater.GraphProcessingPending = false;

                if (results != null)
                    updater.SetResults(results,
                        GraphProcessingHelper.GetErrors((Stencil)m_GraphModelStateComponent.GraphModel.Stencil, results));
            }
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            if (m_Preferences.GetBool(BoolPref.OnlyProcessWhenIdle))
            {
                using var idleObservation = this.ObserveState(m_ProcessOnIdleStateComponent);
                if (idleObservation.UpdateType != UpdateType.None)
                {
                    DoGraphObservation();
                }
                else
                {
                    // We should only process the graph at idle time, but the idle timer was not triggered yet.
                    // Thus we only want to display a notification that we will process the graph soon.

                    // We need to check if the graph was modified, but
                    // without updating our last observed version number (it will be
                    // updated when we actually process the graph). We use PeekAtState.
                    using var graphObservation = this.PeekAtState(m_GraphModelStateComponent);
                    var needsProcessing = graphObservation.UpdateType != UpdateType.None;
                    if (m_GraphProcessingStateComponent.GraphProcessingPending != needsProcessing)
                    {
                        using var updater = m_GraphProcessingStateComponent.UpdateScope;
                        updater.GraphProcessingPending = needsProcessing;
                    }
                }
            }
            else
            {
                // Observe m_ProcessOnIdleStateComponent to sync our last observed version.
                using (this.ObserveState(m_ProcessOnIdleStateComponent)) { }

                DoGraphObservation();
            }
        }
    }
}
