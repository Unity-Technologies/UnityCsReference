// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class GraphProcessingStatusObserver_Internal : StateObserver
    {
        Label m_StatusLabel;
        GraphProcessingStateComponent m_GraphProcessingStateComponent;

        public GraphProcessingStatusObserver_Internal(Label statusLabel, GraphProcessingStateComponent graphProcessingState)
            : base(graphProcessingState)
        {
            m_StatusLabel = statusLabel;
            m_GraphProcessingStateComponent = graphProcessingState;
        }

        public override void Observe()
        {
            using (var observation = this.ObserveState(m_GraphProcessingStateComponent))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    m_StatusLabel?.EnableInClassList(
                        GraphViewEditorWindow.graphProcessingPendingUssClassName,
                        m_GraphProcessingStateComponent.GraphProcessingPending);
                }
            }
        }
    }
}
