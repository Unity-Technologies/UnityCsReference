// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class SelectionDragger : Dragger
    {
        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement selectedElement { get; set; }
        GraphElement clickedElement { get; set; }

        private GraphViewChange m_GraphViewChange;
        private List<GraphElement> m_MovedElements;

        public SelectionDragger()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;

            m_MovedElements = new List<GraphElement>();
            m_GraphViewChange.movedElements = m_MovedElements;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var selectionContainer = target as ISelection;
            if (selectionContainer == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a control that supports selection");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);

            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private GraphView m_GraphView;

        private Dictionary<GraphElement, Rect> m_OriginalPos;
        private Vector2 m_originalMouse;

        protected new void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            m_GraphView = target as GraphView;
            if (m_GraphView == null)
                return;

            selectedElement = null;

            // avoid starting a manipulation on a non movable object
            clickedElement = e.target as GraphElement;
            if (clickedElement == null)
            {
                var ve = e.target as VisualElement;
                clickedElement = ve.GetFirstAncestorOfType<GraphElement>();
                if (clickedElement == null)
                    return;
            }

            if (CanStartManipulation(e))
            {
                selectedElement = clickedElement;

                if (!selectedElement.IsMovable())
                    return;

                m_OriginalPos = new Dictionary<GraphElement, Rect>();

                foreach (ISelectable s in m_GraphView.selection)
                {
                    GraphElement ce = s as GraphElement;
                    if (ce == null || !ce.IsMovable())
                        continue;

                    m_OriginalPos[ce] = ce.layout;
                }

                m_originalMouse = e.mousePosition;
                m_ItemPanDiff = Vector3.zero;

                if (m_PanSchedule == null)
                {
                    m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
                    m_PanSchedule.Pause();
                }

                m_Active = true;
                target.TakeMouseCapture(); // We want to receive events even when mouse is not over ourself.
                e.StopPropagation();
            }
        }

        private bool m_GoneOut;
        internal const int k_PanAreaWidth = 20;
        internal const int k_PanSpeed = 4;
        internal const int k_PanInterval = 10;
        private IVisualElementScheduledItem m_PanSchedule;
        private Vector3 m_PanDiff = Vector3.zero;
        private Vector3 m_ItemPanDiff = Vector3.zero;
        private Vector2 m_MouseDiff = Vector2.zero;

        protected new void OnMouseMove(MouseMoveEvent e)
        {
            if (!target.HasMouseCapture())
            {
                // We lost the capture. Since we still receive mouse events,
                // the MouseDown target must have taken it in its ExecuteDefaultAction().
                // Stop processing the event sequence, then.
                // FIXME: replace this by a handler on the upcoming LostCaptureEvent.
                m_Active = false;
            }

            if (!m_Active)
                return;

            if (m_GraphView == null)
                return;

            var ve = (VisualElement)e.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, e.localMousePosition);
            m_PanDiff = Vector3.zero;

            if (gvMousePos.x <= k_PanAreaWidth)
                m_PanDiff.x += -k_PanSpeed;
            else if (gvMousePos.x >= m_GraphView.contentContainer.layout.width - k_PanAreaWidth)
                m_PanDiff.x += k_PanSpeed;

            if (gvMousePos.y <= k_PanAreaWidth)
                m_PanDiff.y += -k_PanSpeed;
            else if (gvMousePos.y >= m_GraphView.contentContainer.layout.height - k_PanAreaWidth)
                m_PanDiff.y += k_PanSpeed;

            if (gvMousePos.x < 0 || gvMousePos.y < 0 || gvMousePos.x > m_GraphView.layout.width ||
                gvMousePos.y > m_GraphView.layout.height)
            {
                if (!m_GoneOut)
                {
                    m_PanSchedule.Pause();

                    foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
                    {
                        v.Key.SetPosition(v.Value);
                    }
                    m_GoneOut = true;
                }

                e.StopPropagation();
                return;
            }

            if (m_GoneOut)
            {
                m_GoneOut = false;
            }

            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            // We need to monitor the mouse diff "by hand" because we stop positionning the graph elements once the
            // mouse has gone out.
            m_MouseDiff = m_originalMouse - e.mousePosition;

            foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
            {
                GraphElement ce = v.Key;

                Matrix4x4 g = ce.worldTransform;
                var scale = new Vector3(g.m00, g.m11, g.m22);

                ce.SetPosition(
                    new Rect(v.Value.x - (m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / scale.x,
                        v.Value.y - (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / scale.y,
                        ce.layout.width, ce.layout.height));
            }

            selectedElement = null;
            e.StopPropagation();
        }

        private void Pan(TimerState ts)
        {
            m_GraphView.viewTransform.position -= m_PanDiff;
            m_ItemPanDiff += m_PanDiff;

            foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
            {
                GraphElement ce = v.Key;

                Matrix4x4 g = ce.worldTransform;
                var scale = new Vector3(g.m00, g.m11, g.m22);

                ce.SetPosition(
                    new Rect(v.Value.x - (m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / scale.x,
                        v.Value.y - (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / scale.y,
                        ce.layout.width, ce.layout.height));
            }
        }

        protected new void OnMouseUp(MouseUpEvent e)
        {
            if (m_GraphView == null)
                return;

            if (CanStopManipulation(e))
            {
                if (m_Active && target.HasMouseCapture())
                {
                    if (selectedElement == null)
                    {
                        m_MovedElements.Clear();
                        foreach (GraphElement ce in m_OriginalPos.Keys)
                        {
                            ce.UpdatePresenterPosition();

                            m_MovedElements.Add(ce);
                        }

                        var graphView = target as GraphView;
                        if (graphView != null && graphView.graphViewChanged != null)
                        {
                            graphView.graphViewChanged(m_GraphViewChange);
                        }
                    }

                    m_PanSchedule.Pause();

                    if (m_ItemPanDiff != Vector3.zero)
                    {
                        Vector3 p = m_GraphView.contentViewContainer.transform.position;
                        Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                        m_GraphView.UpdateViewTransform(p, s);
                    }

                    target.ReleaseMouseCapture();
                    e.StopPropagation();
                }

                m_Active = false;
            }
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || m_GraphView == null || !m_Active)
                return;

            // Reset the items to their original pos.
            foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
            {
                v.Key.SetPosition(v.Value);
            }

            m_PanSchedule.Pause();

            if (m_ItemPanDiff != Vector3.zero)
            {
                Vector3 p = m_GraphView.contentViewContainer.transform.position;
                Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                m_GraphView.UpdateViewTransform(p, s);
            }

            m_Active = false;

            target.ReleaseMouseCapture();
            e.StopPropagation();
        }
    }
}
