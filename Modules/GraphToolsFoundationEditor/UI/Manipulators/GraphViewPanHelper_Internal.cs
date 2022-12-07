// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class GraphViewPanHelper_Internal
    {
        IVisualElementScheduledItem m_PanSchedule;
        GraphView m_GraphView;
        Action<TimerState> m_OnPan;

        public Vector2 CurrentPanSpeed { get; private set; } = Vector2.zero;
        public Vector2 LastLocalMousePosition { get; private set; }

        public void OnMouseDown(IMouseEvent e, GraphView graphView, Action<TimerState> onPan)
        {
            if (graphView == null)
                return;

            m_GraphView = graphView;
            m_OnPan = onPan;

            if (m_PanSchedule == null)
            {
                var panInterval = GraphView.panIntervalMs_Internal;
                m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(panInterval).StartingIn(panInterval);
                m_PanSchedule.Pause();
            }

            LastLocalMousePosition = e.localMousePosition;
        }

        public void OnMouseMove(IMouseEvent e)
        {
            if (m_GraphView == null)
                return;

            LastLocalMousePosition = e.localMousePosition;
            CurrentPanSpeed = m_GraphView.GetEffectivePanSpeed_Internal(e.mousePosition);
            if (CurrentPanSpeed != Vector2.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }
        }

        public void OnMouseUp(IMouseEvent e)
        {
            LastLocalMousePosition = e.localMousePosition;
            Stop();
        }

        public void Stop()
        {
            m_PanSchedule?.Pause();
            m_OnPan = null;
        }

        void Pan(TimerState timerState)
        {
            if (m_GraphView == null)
                return;

            var travelThisFrame = CurrentPanSpeed * timerState.deltaTime;
            var position = m_GraphView.ContentViewContainer.transform.position - (Vector3)travelThisFrame;
            var scale = m_GraphView.ContentViewContainer.transform.scale;
            m_GraphView.Dispatch(new ReframeGraphViewCommand(position, scale));

            m_OnPan?.Invoke(timerState);
        }
    }
}
