// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Holds a list of <see cref="GraphProcessingErrorModel"/>.
/// </summary>
[Serializable]
class GraphProcessingErrorsStateComponent : StateComponent<GraphProcessingErrorsStateComponent.StateUpdater>
{
    public class GraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        GraphProcessingErrorsStateComponent m_GraphProcessingErrorStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphLoadedObserver"/> class.
        /// </summary>
        public GraphLoadedObserver(ToolStateComponent toolStateComponent, GraphProcessingErrorsStateComponent graphProcessingErrorStateComponent)
            : base(new [] { toolStateComponent},
                new IStateComponent[] { graphProcessingErrorStateComponent })
        {
            m_ToolStateComponent = toolStateComponent;
            m_GraphProcessingErrorStateComponent = graphProcessingErrorStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None)
                {
                    using var updater = m_GraphProcessingErrorStateComponent.UpdateScope;
                    updater.Clear();
                }
            }
        }
    }

    /// <summary>
    /// The updater for <see cref="GraphProcessingErrorsStateComponent"/>.
    /// </summary>
    public class StateUpdater : BaseUpdater<GraphProcessingErrorsStateComponent>
    {
        /// <summary>
        /// Sets the error list.
        /// </summary>
        /// <param name="errorModels">The errors.</param>
        public void SetResults(IEnumerable<GraphProcessingErrorModel> errorModels)
        {
            m_State.m_Errors.Clear();

            if (errorModels != null)
                m_State.m_Errors.AddRange(errorModels);
            m_State.SetUpdateType(UpdateType.Complete);
        }

        /// <summary>
        /// Clears the error list.
        /// </summary>
        public void Clear()
        {
            m_State.m_Errors.Clear();
            m_State.SetUpdateType(UpdateType.Complete);
        }
    }

    List<GraphProcessingErrorModel> m_Errors = new();

    /// <summary>
    /// The errors.
    /// </summary>
    public IReadOnlyList<GraphProcessingErrorModel> Errors => m_Errors;

    /// <inheritdoc/>
    public override void OnRemovedFromState(IState state)
    {
        base.OnRemovedFromState(state);
        m_Errors.Clear();
    }
}
