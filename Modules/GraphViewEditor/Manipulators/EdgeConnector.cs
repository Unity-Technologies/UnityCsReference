// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    interface IEdgeConnectorListener
    {
        void OnDropOutsideAnchor(EdgePresenter edge, Vector2 position);
    }

    internal
    abstract class EdgeConnector : MouseManipulator
    {}

    internal
    class EdgeConnector<TEdgePresenter> : EdgeConnector where TEdgePresenter : EdgePresenter
    {
        private List<NodeAnchorPresenter> m_CompatibleAnchors;
        private TEdgePresenter m_EdgePresenterCandidate;

        private GraphViewPresenter m_GraphViewPresenter;
        private GraphView m_GraphView;
        private bool m_Active;

        private static NodeAdapter s_nodeAdapter = new NodeAdapter();

        private readonly IEdgeConnectorListener m_Listener;

        public EdgeConnector(IEdgeConnectorListener listener)
        {
            m_Listener = listener;
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (!CanStartManipulation(e))
            {
                return;
            }

            var graphElement = e.target as NodeAnchor;
            if (graphElement == null)
            {
                return;
            }

            NodeAnchorPresenter startAnchor = graphElement.GetPresenter<NodeAnchorPresenter>();
            m_GraphView = graphElement.GetFirstAncestorOfType<GraphView>();

            if (startAnchor == null || m_GraphView == null)
            {
                return;
            }

            m_GraphViewPresenter = m_GraphView.presenter;
            if (m_GraphViewPresenter == null)
            {
                return;
            }

            m_Active = true;
            target.TakeCapture();

            m_CompatibleAnchors = m_GraphViewPresenter.GetCompatibleAnchors(startAnchor, s_nodeAdapter);

            foreach (var compatibleAnchor in m_CompatibleAnchors)
            {
                compatibleAnchor.highlight = true;
            }

            m_EdgePresenterCandidate = ScriptableObject.CreateInstance<TEdgePresenter>();

            m_EdgePresenterCandidate.position = new Rect(0, 0, 1, 1);

            bool startFromOutput = (startAnchor.direction == Direction.Output);
            if (startFromOutput)
            {
                m_EdgePresenterCandidate.output = graphElement.GetPresenter<NodeAnchorPresenter>();
                m_EdgePresenterCandidate.input = null;
            }
            else
            {
                m_EdgePresenterCandidate.output = null;
                m_EdgePresenterCandidate.input = graphElement.GetPresenter<NodeAnchorPresenter>();
            }
            m_EdgePresenterCandidate.candidate = true;
            m_EdgePresenterCandidate.candidatePosition = e.mousePosition;

            m_GraphViewPresenter.AddTempElement(m_EdgePresenterCandidate);

            e.StopPropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                m_EdgePresenterCandidate.candidatePosition = e.mousePosition;
                e.StopPropagation();
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (m_Active)
            {
                if (CanStopManipulation(e))
                {
                    NodeAnchorPresenter endAnchor = null;

                    if (m_GraphView != null)
                    {
                        foreach (var compatibleAnchor in m_CompatibleAnchors)
                        {
                            compatibleAnchor.highlight = false;
                            NodeAnchor anchorElement = m_GraphView.Query<NodeAnchor>().Where(el => el.GetPresenter<NodeAnchorPresenter>() == compatibleAnchor).First();
                            if (anchorElement != null && anchorElement.worldBound.Contains(e.mousePosition))
                                endAnchor = compatibleAnchor;
                        }
                    }
                    if (endAnchor == null && m_Listener != null)
                    {
                        m_Listener.OnDropOutsideAnchor(m_EdgePresenterCandidate, e.mousePosition);
                    }

                    m_GraphViewPresenter.RemoveTempElement(m_EdgePresenterCandidate);
                    if (m_EdgePresenterCandidate != null && m_GraphViewPresenter != null)
                    {
                        if (endAnchor != null)
                        {
                            if (m_EdgePresenterCandidate.output == null)
                            {
                                m_EdgePresenterCandidate.output = endAnchor;
                            }
                            else
                            {
                                m_EdgePresenterCandidate.input = endAnchor;
                            }
                            m_EdgePresenterCandidate.output.Connect(m_EdgePresenterCandidate);
                            m_EdgePresenterCandidate.input.Connect(m_EdgePresenterCandidate);

                            m_GraphViewPresenter.AddElement(m_EdgePresenterCandidate);
                        }

                        m_EdgePresenterCandidate.candidate = false;
                    }

                    m_EdgePresenterCandidate = null;
                    m_GraphViewPresenter = null;

                    m_Active = false;
                    e.StopPropagation();
                }
            }
            target.ReleaseCapture();
        }
    }
}
