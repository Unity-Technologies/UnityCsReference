// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class EdgeManipulator : MouseManipulator
    {
        private bool m_Active;
        private Edge m_Edge;
        private EdgePresenter m_EdgePresenter;
        private Vector2 m_PressPos;
        private Port m_ConnectedPort;
        private EdgeDragHelper m_ConnectedEdgeDragHelper;
        private Port m_DetachedPort;
        private bool m_DetachedFromInputPort;
        private static int s_StartDragDistance = 10;
        private MouseDownEvent m_LastMouseDownEvent;

        public EdgeManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            Reset();
        }

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

        private void Reset()
        {
            m_Active = false;
            m_Edge = null;
            m_ConnectedPort = null;
            m_ConnectedEdgeDragHelper = null;
            m_DetachedPort = null;
            m_DetachedFromInputPort = false;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(evt))
            {
                return;
            }

            m_Edge = (evt.target as VisualElement).parent as Edge;

            if (m_Edge != null)
            {
                m_EdgePresenter = m_Edge.GetPresenter<EdgePresenter>();
            }

            m_PressPos = evt.mousePosition;
            target.TakeMouseCapture();
            evt.StopPropagation();
            m_LastMouseDownEvent = evt;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            /// If the left mouse button is not down then return
            if (m_Edge == null)
            {
                return;
            }

            evt.StopPropagation();

            bool alreadyDetached = (m_DetachedPort != null);

            // If one end of the edge is not already detached then
            if (!alreadyDetached)
            {
                float delta = (evt.mousePosition - m_PressPos).magnitude;

                if (delta < s_StartDragDistance)
                {
                    return;
                }

                /// Determine which end is the nearest to the mouse position then detach it.
                Vector2 outputPos = new Vector2(m_Edge.output.GetGlobalCenter().x, m_Edge.output.GetGlobalCenter().y);
                Vector2 inputPos = new Vector2(m_Edge.input.GetGlobalCenter().x, m_Edge.input.GetGlobalCenter().y);

                float distanceFromOutput = (m_PressPos - outputPos).magnitude;
                float distanceFromInput = (m_PressPos - inputPos).magnitude;

                m_DetachedFromInputPort = distanceFromInput < distanceFromOutput;

                if (m_DetachedFromInputPort)
                {
                    m_ConnectedPort = m_Edge.output;
                    m_DetachedPort = m_Edge.input;
                    m_DetachedPort.Disconnect(m_Edge);

                    m_Edge.input = null;
                    if (m_EdgePresenter != null)
                    {
                        m_EdgePresenter.input = null;
                    }
                }
                else
                {
                    m_ConnectedPort = m_Edge.input;
                    m_DetachedPort = m_Edge.output;
                    m_DetachedPort.Disconnect(m_Edge);

                    m_Edge.output = null;
                    if (m_EdgePresenter != null)
                    {
                        m_EdgePresenter.output = null;
                    }
                }

                // Use the edge drag helper of the still connected port
                m_ConnectedEdgeDragHelper = m_ConnectedPort.edgeConnector.edgeDragHelper;
                m_ConnectedEdgeDragHelper.draggedPort = m_ConnectedPort;
                m_ConnectedEdgeDragHelper.edgeCandidate = m_Edge;
                m_Edge.candidatePosition = evt.mousePosition;

                // Redirect the last mouse down event to active the drag helper

                if (m_ConnectedEdgeDragHelper.HandleMouseDown(m_LastMouseDownEvent))
                {
                    m_Active = true;
                }
                else
                {
                    Reset();
                }
                m_LastMouseDownEvent = null;
            }

            if (m_Active)
            {
                if (m_EdgePresenter != null)
                {
                    m_EdgePresenter.candidatePosition = evt.mousePosition;
                }
                m_ConnectedEdgeDragHelper.HandleMouseMove(evt);
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                if (m_Active)
                {
                    GraphView graphView = m_Edge.GetFirstAncestorOfType<GraphView>();

                    // Restore the detached port before potentially delete or reconnect it.
                    // This is to ensure that the edge has valid input and output so it can be properly handled by the model.
                    RestoreDetachedPort();

                    m_ConnectedEdgeDragHelper.HandleMouseUp(evt);

                    if ((m_EdgePresenter != null) && ((m_EdgePresenter.input == null) || (m_EdgePresenter.output == null)))
                    {
                        m_EdgePresenter.input = null;
                        m_EdgePresenter.output = null;
                        graphView.presenter.RemoveElement(m_EdgePresenter);
                    }
                }
                Reset();
                if (target.HasMouseCapture())
                {
                    target.ReleaseMouseCapture();
                }
                evt.StopPropagation();
            }
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    RestoreDetachedPort();

                    m_ConnectedEdgeDragHelper.Reset();
                    Reset();
                    target.ReleaseMouseCapture();
                    evt.StopPropagation();
                }
            }
        }

        private void RestoreDetachedPort()
        {
            if (m_DetachedFromInputPort)
            {
                m_Edge.input = m_DetachedPort;

                if (m_EdgePresenter)
                {
                    m_EdgePresenter.input = m_DetachedPort.GetPresenter<PortPresenter>();
                }
                else
                {
                    m_DetachedPort.Connect(m_Edge);
                }
            }
            else
            {
                m_Edge.output = m_DetachedPort;

                if (m_EdgePresenter)
                {
                    m_EdgePresenter.output = m_DetachedPort.GetPresenter<PortPresenter>();
                }
                else
                {
                    m_DetachedPort.Connect(m_Edge);
                }
            }
        }
    }
}
