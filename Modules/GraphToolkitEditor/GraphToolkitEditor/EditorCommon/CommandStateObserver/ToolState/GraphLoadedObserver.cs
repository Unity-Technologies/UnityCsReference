// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for <see cref="IStateComponentUpdater"/> that need to be notified when a graph is loaded.
    /// </summary>
    [UnityRestricted]
    internal interface IOnGraphLoaded : IStateComponentUpdater
    {
        /// <summary>
        /// Called when a graph is loaded.
        /// </summary>
        /// <param name="graphModel">The loaded graph.</param>
        void OnGraphLoaded(GraphModel graphModel);
    }

    /// <summary>
    /// An observer that updates the <see cref="GraphViewStateComponent"/> when a graph is loaded.
    /// </summary>
    [UnityRestricted]
    internal class GraphLoadedObserver<TUpdater> : StateObserver
        where TUpdater : class, IOnGraphLoaded, new()
    {
        ToolStateComponent m_ToolStateComponent;
        StateComponent<TUpdater> m_StateComponentToUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphLoadedObserver{TUpdater}"/> class.
        /// </summary>
        public GraphLoadedObserver(ToolStateComponent toolStateComponent, StateComponent<TUpdater> stateComponentToUpdate)
            : base(new IStateComponent[] { toolStateComponent },
                new IStateComponent[] { stateComponentToUpdate })
        {
            m_ToolStateComponent = toolStateComponent;
            m_StateComponentToUpdate = stateComponentToUpdate;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None)
                {
                    using var updater = m_StateComponentToUpdate.UpdateScope;
                    updater.OnGraphLoaded(m_ToolStateComponent.GraphModel);
                }
            }
        }
    }
}
