// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public interface IEdgeConnectorListener
    {
        void OnDropOutsidePort(Edge edge, Vector2 position);
        void OnDrop(GraphView graphView, Edge edge);
    }

    public abstract class EdgeConnector : MouseManipulator
    {
        public abstract EdgeDragHelper edgeDragHelper { get; }
    }

    public class EdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new()
    {
        EdgeDragHelper m_EdgeDragHelper;
        Edge m_EdgeCandidate;
        protected bool m_Active;

        public EdgeConnector(IEdgeConnectorListener listener)
        {
            m_EdgeDragHelper = new EdgeDragHelper<TEdge>(listener);
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        public override EdgeDragHelper edgeDragHelper { get { return m_EdgeDragHelper; } }

        protected override void RegisterCallbacksOnTarget()
        {
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

        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
            {
                return;
            }

            var graphElement = e.target as Port;
            if (graphElement == null)
            {
                return;
            }

            m_EdgeCandidate = new TEdge();
            m_EdgeDragHelper.draggedPort = graphElement;
            m_EdgeDragHelper.edgeCandidate = m_EdgeCandidate;

            if (m_EdgeDragHelper.HandleMouseDown(e))
            {
                m_Active = true;
                target.TakeMouseCapture();

                e.StopPropagation();
            }
            else
            {
                m_EdgeDragHelper.Reset();
                m_EdgeCandidate = null;
            }
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                m_EdgeDragHelper.HandleMouseMove(e);
                m_EdgeCandidate.candidatePosition = e.mousePosition;
                m_EdgeCandidate.UpdateEdgeControl();

                e.StopPropagation();
            }
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            m_EdgeDragHelper.HandleMouseUp(e);
            m_Active = false;
            target.ReleaseMouseCapture();
            e.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !m_Active)
                return;

            var graphElement = e.target as Port;
            var graphView = graphElement.GetFirstAncestorOfType<GraphView>();
            graphView.RemoveElement(m_EdgeCandidate);

            m_EdgeCandidate.input = null;
            m_EdgeCandidate.output = null;
            m_EdgeCandidate = null;

            m_EdgeDragHelper.Reset();

            m_Active = false;
            target.ReleaseMouseCapture();
            e.StopPropagation();
        }
    }
}
