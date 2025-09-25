// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace UnityEditor.Experimental.GraphView
{
    public class EdgeManipulator : PointerManipulator
    {
        private bool m_Active;
        private Edge m_Edge;
        private Vector2 m_PressPos;
        private Port m_ConnectedPort;
        private EdgeDragHelper m_ConnectedEdgeDragHelper;
        private List<Edge> m_AdditionalEdges;
        private List<EdgeDragHelper> m_AdditionalEdgeDragHelpers;
        private Port m_DetachedPort;
        private bool m_DetachedFromInputPort;
        private static int s_StartDragDistance = 10;
        private IPointerOrMouseEvent m_LastPointerOrMouseDownEvent;

        public EdgeManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            Reset();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Obsolete, use pointer events instead to handle touch input properly. Kept for compatibility.
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            // Obsolete, use pointer events instead to handle touch input properly. Kept for compatibility.
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void Reset()
        {
            m_Active = false;
            m_Edge = null;
            m_ConnectedPort = null;
            m_ConnectedEdgeDragHelper = null;
            m_AdditionalEdgeDragHelpers = null;
            m_DetachedPort = null;
            m_DetachedFromInputPort = false;
        }

        /// <summary>
        /// Called when a pointer down event occurs.
        /// </summary>
        /// <param name="evt">The pointer down event.</param>
        /// <remarks>
        /// This method is invoked when a pointer down event occurs on the target element.
        /// It checks if the pointer is a touch input and if manipulation can start.
        /// </remarks>
        protected void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.pointerId != PointerId.mousePointerId && evt.pointerType != PointerType.touch)
                return;

            if (!CanStartManipulation(evt))
                return;

            if (TryOnPointerOrMouseDown(evt, evt.position))
            {
                target.CapturePointer(evt.pointerId);
                m_LastPointerOrMouseDownEvent = evt;
            }
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;

            if (TryOnPointerOrMouseDown(evt, evt.mousePosition))
            {
                target.CaptureMouse();
                m_LastPointerOrMouseDownEvent = evt;
            }
        }

        bool TryOnPointerOrMouseDown(EventBase evt, Vector2 position)
        {
            if (m_Active)
            {
                StopDragging();
                evt.StopImmediatePropagation();
                return false;
            }

            m_Edge = (evt.target as VisualElement)?.GetFirstOfType<Edge>();

            m_PressPos = position;
            evt.StopPropagation();

            return true;
        }

        /// <summary>
        /// Called when a pointer move event occurs.
        /// </summary>
        /// <param name="evt">The pointer move event.</param>
        /// <remarks>
        /// This method is invoked when a pointer move event occurs on the target element.
        /// It checks if the pointer has capture and if the manipulation is active.
        /// It then processes the pointer move event accordingly.
        /// </remarks>
        protected void OnPointerMove(PointerMoveEvent evt)
        {
            if (!target.HasPointerCapture(evt.pointerId))
                return;

            if (OnPointerOrMouseMove(evt, evt.position) && m_Active)
            {
                m_ConnectedEdgeDragHelper.HandlePointerMove(evt);

                foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                {
                    edgeDrag.HandlePointerMove(evt);
                }
            }
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (OnPointerOrMouseMove(evt, evt.mousePosition) && m_Active)
            {
                m_ConnectedEdgeDragHelper.HandleMouseMove(evt);

                foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                {
                    edgeDrag.HandleMouseMove(evt);
                }
            }
        }

        bool OnPointerOrMouseMove(EventBase evt, Vector2 position)
        {
            // If the left mouse button is not down then return
            if (m_Edge == null)
            {
                return false;
            }

            evt.StopPropagation();

            bool alreadyDetached = (m_DetachedPort != null);

            // If one end of the edge is not already detached then
            if (!alreadyDetached)
            {
                float delta = (position - m_PressPos).sqrMagnitude;

                if (delta < (s_StartDragDistance * s_StartDragDistance))
                {
                    return false;
                }

                // Determine which end is the nearest to the mouse position then detach it.
                Vector2 outputPos = new Vector2(m_Edge.output.GetGlobalCenter().x, m_Edge.output.GetGlobalCenter().y);
                Vector2 inputPos = new Vector2(m_Edge.input.GetGlobalCenter().x, m_Edge.input.GetGlobalCenter().y);

                float distanceFromOutput = (m_PressPos - outputPos).sqrMagnitude;
                float distanceFromInput = (m_PressPos - inputPos).sqrMagnitude;

                m_DetachedFromInputPort = distanceFromInput < distanceFromOutput;

                if (m_DetachedFromInputPort)
                {
                    m_ConnectedPort = m_Edge.output;
                    m_DetachedPort = m_Edge.input;
                    m_DetachedPort.Disconnect(m_Edge);

                    m_Edge.input = null;
                }
                else
                {
                    m_ConnectedPort = m_Edge.input;
                    m_DetachedPort = m_Edge.output;
                    m_DetachedPort.Disconnect(m_Edge);

                    m_Edge.output = null;
                }

                // Use the edge drag helper of the still connected port

                m_ConnectedEdgeDragHelper = m_ConnectedPort.edgeConnector.edgeDragHelper;
                m_ConnectedEdgeDragHelper.draggedPort = m_ConnectedPort;
                m_ConnectedEdgeDragHelper.edgeCandidate = m_Edge;

                m_AdditionalEdgeDragHelpers = new List<EdgeDragHelper>();
                m_AdditionalEdges = new List<Edge>();

                GraphView gv = m_DetachedPort.GetFirstAncestorOfType<GraphView>();

                if (m_DetachedPort.allowMultiDrag)
                {
                    foreach (var edge in m_DetachedPort.connections)
                    {
                        if (edge != m_Edge && edge.IsSelected(gv))
                        {
                            var otherPort = m_DetachedPort == edge.input ? edge.output : edge.input;

                            var edgeDragHelper = otherPort.edgeConnector.edgeDragHelper;
                            edgeDragHelper.draggedPort = otherPort;
                            edgeDragHelper.edgeCandidate = edge;
                            if (m_DetachedPort == edge.input)
                                edge.input = null;
                            else
                                edge.output = null;

                            m_AdditionalEdgeDragHelpers.Add(edgeDragHelper);
                            m_AdditionalEdges.Add(edge);
                        }
                    }

                    foreach (var edge in m_AdditionalEdges)
                        m_DetachedPort.Disconnect(edge);
                }

                m_Edge.candidatePosition = position;

                // Redirect the last mouse down event to active the drag helper
                if (m_LastPointerOrMouseDownEvent is PointerDownEvent pointerDownEvent && m_ConnectedEdgeDragHelper.HandlePointerDown(pointerDownEvent))
                {
                    m_Active = true;
                    foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                    {
                        edgeDrag.HandlePointerDown(pointerDownEvent);
                    }
                }
                else if (m_LastPointerOrMouseDownEvent is MouseDownEvent mouseDownEvent && m_ConnectedEdgeDragHelper.HandleMouseDown(mouseDownEvent))
                {
                    m_Active = true;
                    foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                    {
                        edgeDrag.HandleMouseDown(mouseDownEvent);
                    }
                }
                else
                {
                    Reset();
                }

                m_LastPointerOrMouseDownEvent = null;
            }

            return true;
        }

        /// <summary>
        /// Called when a pointer up event occurs.
        /// </summary>
        /// <param name="evt">The pointer up event.</param>
        /// <remarks>
        /// This method is invoked when a pointer up event occurs on the target element.
        /// It checks if the pointer has capture and if manipulation can stop.
        /// It then processes the pointer up event accordingly.
        /// </remarks>
        protected void OnPointerUp(PointerUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                target.ReleasePointer(evt.pointerId);
                OnPointerOrMouseUp(evt);
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                target.ReleaseMouse();
                OnPointerOrMouseUp(evt);
            }
        }

        void OnPointerOrMouseUp(EventBase evt)
        {
            if (m_Active)
            {
                // Restore the detached port before potentially delete or reconnect it.
                // This is to ensure that the edge has valid input and output so it can be properly handled by the model.
                RestoreDetachedPort();

                switch (evt)
                {
                    case PointerUpEvent pointerUpEvent:
                    {
                        m_ConnectedEdgeDragHelper.HandlePointerUp(pointerUpEvent);

                        foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                        {
                            edgeDrag.HandlePointerUp(pointerUpEvent);
                        }

                        break;
                    }
                    case MouseUpEvent mouseUpEvent:
                    {
                        m_ConnectedEdgeDragHelper.HandleMouseUp(mouseUpEvent);

                        foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                        {
                            edgeDrag.HandleMouseUp(mouseUpEvent);
                        }

                        break;
                    }
                }
            }
            Reset();
            evt.StopPropagation();
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    StopDragging();
                    evt.StopPropagation();
                }
            }
        }

        void StopDragging()
        {
            RestoreDetachedPort();

            m_ConnectedEdgeDragHelper.Reset();

            foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
            {
                edgeDrag.Reset();
            }

            Reset();
            target.ReleaseMouse();
        }

        private void RestoreDetachedPort()
        {
            if (m_DetachedFromInputPort)
            {
                m_Edge.input = m_DetachedPort;

                m_DetachedPort.Connect(m_Edge);

                foreach (var edge in m_AdditionalEdges)
                {
                    edge.input = m_DetachedPort;

                    m_DetachedPort.Connect(edge);
                }
            }
            else
            {
                m_Edge.output = m_DetachedPort;

                m_DetachedPort.Connect(m_Edge);

                foreach (var edge in m_AdditionalEdges)
                {
                    edge.output = m_DetachedPort;

                    m_DetachedPort.Connect(edge);
                }
            }
        }
    }
}
