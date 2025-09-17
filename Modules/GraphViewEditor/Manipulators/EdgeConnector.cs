// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace UnityEditor.Experimental.GraphView
{
    public interface IEdgeConnectorListener
    {
        void OnDropOutsidePort(Edge edge, Vector2 position);
        void OnDrop(GraphView graphView, Edge edge);
    }

    public abstract class EdgeConnector : PointerManipulator
    {
        public abstract EdgeDragHelper edgeDragHelper { get; }
    }

    public class EdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new()
    {
        readonly EdgeDragHelper m_EdgeDragHelper;
        Edge m_EdgeCandidate;
        private bool m_Active;
        Vector2 m_MouseDownPosition;

        internal const float k_ConnectionDistanceTreshold = 10f;

        public EdgeConnector(IEdgeConnectorListener listener)
        {
            m_EdgeDragHelper = new EdgeDragHelper<TEdge>(listener);
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        public override EdgeDragHelper edgeDragHelper => m_EdgeDragHelper;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Obsolete, use pointer events instead to handle touch input properly. Kept for compatibility.
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            // Obsolete, should use pointer events instead to handle touch input properly. Kept for compatibility.
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            if (!CanStartManipulation(e))
                return;

            OnPointerOrMouseDown(e, e.localMousePosition);
        }

        /// <summary>
        /// Called when a pointer down event occurs on the target element.
        /// </summary>
        /// <param name="e">The pointer down event.</param>
        /// <remarks>
        /// It checks if the manipulation can start and then processes the event accordingly.
        /// </remarks>
        protected virtual void OnPointerDown(PointerDownEvent e)
        {
            if (e.pointerId != PointerId.mousePointerId && e.pointerType != PointerType.touch)
                return;

            if (!CanStartManipulation(e))
                return;

            OnPointerOrMouseDown(e, e.localPosition);
        }

        void OnPointerOrMouseDown(EventBase e, Vector2 localPosition)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (target is not Port graphElement)
                return;

            m_MouseDownPosition = localPosition;

            m_EdgeCandidate = new TEdge();
            m_EdgeDragHelper.draggedPort = graphElement;
            m_EdgeDragHelper.edgeCandidate = m_EdgeCandidate;

            switch (e)
            {
                case PointerDownEvent pointerDownEvent when m_EdgeDragHelper.HandlePointerDown(pointerDownEvent):
                    m_Active = true;
                    target.CapturePointer(pointerDownEvent.pointerId);
                    e.StopPropagation();
                    break;
                case MouseDownEvent mouseDownEvent when m_EdgeDragHelper.HandleMouseDown(mouseDownEvent):
                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();
                    break;
                default:
                    m_EdgeDragHelper.Reset();
                    m_EdgeCandidate = null;
                    break;
            }
        }

        void OnCaptureOut(MouseCaptureOutEvent e)
        {
            OnCaptureOut();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            OnCaptureOut();
        }

        void OnCaptureOut()
        {
            m_Active = false;
            if (m_EdgeCandidate != null)
                Abort();
        }

        /// <summary>
        /// Called when a pointer move event occurs on the target element.
        /// </summary>
        /// <param name="e">The pointer move event.</param>
        /// <remarks>
        /// It processes the pointer move event and updates the edge candidate's position.
        /// </remarks>
        protected virtual void OnPointerMove(PointerMoveEvent e)
        {
            OnPointerOrMouseMove(e, e.position);
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            OnPointerOrMouseMove(e, e.mousePosition);
        }

        void OnPointerOrMouseMove(EventBase e, Vector2 position)
        {
            if (!m_Active)
                return;

            switch (e)
            {
                case PointerMoveEvent pointerMoveEvent when !target.HasPointerCapture(pointerMoveEvent.pointerId):
                    return;
                case PointerMoveEvent pointerMoveEvent:
                    m_EdgeDragHelper.HandlePointerMove(pointerMoveEvent);
                    break;
                case MouseMoveEvent mouseMoveEvent:
                    m_EdgeDragHelper.HandleMouseMove(mouseMoveEvent);
                    break;
            }

            m_EdgeCandidate.candidatePosition = position;
            m_EdgeCandidate.UpdateEdgeControl();
            e.StopPropagation();
        }

        /// <summary>
        /// Called when a pointer up event occurs on the target element.
        /// </summary>
        /// <param name="e">The pointer up event.</param>
        /// <remarks>
        /// It checks if the manipulation can stop and processes the pointer up event accordingly.
        /// </remarks>
        protected virtual void OnPointerUp(PointerUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            OnPointerOrMouseUp(e, e.localPosition);
            target.ReleasePointer(e.pointerId);
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            OnPointerOrMouseUp(e, e.localMousePosition);
            target.ReleaseMouse();
        }

        void OnPointerOrMouseUp(EventBase e, Vector2 localPosition)
        {
            if (CanPerformConnection(localPosition))
            {
                switch (e)
                {
                    case PointerUpEvent pointerUpEvent:
                        m_EdgeDragHelper.HandlePointerUp(pointerUpEvent);
                        break;
                    case MouseUpEvent mouseUpEvent:
                        m_EdgeDragHelper.HandleMouseUp(mouseUpEvent);
                        break;
                }
            }
            else
                Abort();

            m_Active = false;
            m_EdgeCandidate = null;
            e.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !m_Active)
                return;

            Abort();

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        protected virtual void Abort()
        {
            var graphView = target?.GetFirstAncestorOfType<GraphView>();
            graphView?.RemoveElement(m_EdgeCandidate);

            m_EdgeCandidate.input = null;
            m_EdgeCandidate.output = null;
            m_EdgeCandidate = null;

            m_EdgeDragHelper.Reset();
        }

        bool CanPerformConnection(Vector2 mousePosition)
        {
            return Vector2.Distance(m_MouseDownPosition, mousePosition) > k_ConnectionDistanceTreshold;
        }
    }
}
