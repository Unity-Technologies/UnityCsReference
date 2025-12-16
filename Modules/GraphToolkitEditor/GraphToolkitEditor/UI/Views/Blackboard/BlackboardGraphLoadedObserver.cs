// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An observer that updates the <see cref="BlackboardView"/> state components when a graph is loaded.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        BlackboardContentStateComponent m_BlackboardContentStateComponent;
        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        SelectionStateComponent m_SelectionStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardGraphLoadedObserver"/> class.
        /// </summary>
        public BlackboardGraphLoadedObserver(ToolStateComponent toolStateComponent,
                                             BlackboardContentStateComponent blackboardContentStateComponent,
                                             BlackboardViewStateComponent blackboardViewStateComponent,
                                             SelectionStateComponent selectionStateComponent)
            : base(new IStateComponent[] { toolStateComponent },
                   new IStateComponent[] { blackboardContentStateComponent, blackboardViewStateComponent, selectionStateComponent })
        {
            m_ToolStateComponent = toolStateComponent;
            m_BlackboardContentStateComponent = blackboardContentStateComponent;
            m_BlackboardViewStateComponent = blackboardViewStateComponent;
            m_SelectionStateComponent = selectionStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None)
                {
                    using (var updater = m_BlackboardContentStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForGraph();
                    }
                    using (var updater = m_BlackboardViewStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForGraph(m_ToolStateComponent.GraphModel);
                    }
                    using (var updater = m_SelectionStateComponent.UpdateScope)
                    {
                        updater.OnGraphLoaded(m_ToolStateComponent.GraphModel);
                    }
                }
            }
        }
    }
}
