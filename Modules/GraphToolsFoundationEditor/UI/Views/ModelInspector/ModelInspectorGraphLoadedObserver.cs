// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An observer that updates the <see cref="ModelInspectorView"/> state components when a graph is loaded.
    /// </summary>
    class ModelInspectorGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        ModelInspectorStateComponent m_InspectorViewStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorGraphLoadedObserver"/> class.
        /// </summary>
        public ModelInspectorGraphLoadedObserver(ToolStateComponent toolStateComponent, ModelInspectorStateComponent inspectorViewStateComponent)
            : base(new[] { toolStateComponent },
                new[] { inspectorViewStateComponent })
        {
            m_ToolStateComponent = toolStateComponent;
            m_InspectorViewStateComponent = inspectorViewStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None)
                {
                    using (var updater = m_InspectorViewStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForGraph(m_ToolStateComponent.GraphModel);
                    }
                }
            }
        }
    }
}
