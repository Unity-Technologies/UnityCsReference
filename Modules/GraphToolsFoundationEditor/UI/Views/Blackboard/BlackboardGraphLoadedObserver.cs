// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An observer that updates the <see cref="BlackboardView"/> state components when a graph is loaded.
    /// </summary>
    class BlackboardGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        SelectionStateComponent m_SelectionStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardGraphLoadedObserver"/> class.
        /// </summary>
        public BlackboardGraphLoadedObserver(ToolStateComponent toolStateComponent, BlackboardViewStateComponent blackboardViewStateComponent, SelectionStateComponent selectionStateComponent)
            : base(new [] { toolStateComponent},
                new IStateComponent[] { blackboardViewStateComponent, selectionStateComponent })
        {
            m_ToolStateComponent = toolStateComponent;
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
                    using (var updater = m_BlackboardViewStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForGraph(m_ToolStateComponent.GraphModel);
                    }
                    using (var updater = m_SelectionStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForGraph(m_ToolStateComponent.GraphModel);
                    }
                }
            }
        }
    }
}
