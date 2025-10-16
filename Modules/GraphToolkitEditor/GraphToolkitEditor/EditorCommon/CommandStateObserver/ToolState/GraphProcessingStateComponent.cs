// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A state component holding the results of graph processing.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class GraphProcessingStateComponent : StateComponent<GraphProcessingStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="GraphProcessingStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<GraphProcessingStateComponent>, IOnGraphLoaded
        {
            /// <summary>
            /// Sets whether we are waiting for the graph processing to begin.
            /// </summary>
            public bool GraphProcessingPending
            {
                set
                {
                    if (m_State.GraphProcessingPending != value)
                    {
                        m_State.GraphProcessingPending = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// Sets the results of the graph processing.
            /// </summary>
            /// <param name="results">The results.</param>
            public void SetResults(IReadOnlyList<BaseGraphProcessingResult> results)
            {
                m_State.Results = results;
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Clears the graph processing results.
            /// </summary>
            public void OnGraphLoaded(GraphModel graphModel)
            {
                m_State.Results = null;
                m_State.GraphProcessingPending = false;
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        /// <summary>
        /// Whether we are waiting for the graph processing to begin.
        /// </summary>
        public bool GraphProcessingPending { get; private set; }

        /// <summary>
        /// The graph processing results.
        /// </summary>
        public IReadOnlyList<BaseGraphProcessingResult> Results { get; private set; }

        /// <inheritdoc/>
        public override void OnRemovedFromState(IState state)
        {
            base.OnRemovedFromState(state);
            Results = null;
        }
    }
}
