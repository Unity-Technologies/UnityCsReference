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
        void OnDropOutsideAnchor(Edge edge, Vector2 position);
        void OnDrop(GraphView graphView, Edge edge);
    }

    internal
    abstract class EdgeConnector : MouseManipulator
    {}

    internal
    class EdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new()
    {
        protected List<NodeAnchor> m_CompatibleAnchors;
        protected TEdge m_EdgeCandidate;
        private TEdge m_GhostEdge;

        protected GraphView m_GraphView;
        protected bool m_Active;

        protected static NodeAdapter s_nodeAdapter = new NodeAdapter();

        protected readonly IEdgeConnectorListener m_Listener;

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

            var graphElement = e.target as NodeAnchor;
            if (graphElement == null)
            {
                return;
            }

            m_GraphView = graphElement.GetFirstAncestorOfType<GraphView>();
            if (m_GraphView == null)
            {
                return;
            }

            m_Active = true;
            target.TakeMouseCapture();

            m_CompatibleAnchors = m_GraphView.GetCompatibleAnchors(graphElement, s_nodeAdapter);

            foreach (NodeAnchor compatibleAnchor in m_CompatibleAnchors)
            {
                compatibleAnchor.highlight = true;
            }

            m_EdgeCandidate = new TEdge();

            bool startFromOutput = (graphElement.direction == Direction.Output);
            if (startFromOutput)
            {
                m_EdgeCandidate.output = graphElement;
                m_EdgeCandidate.input = null;
            }
            else
            {
                m_EdgeCandidate.output = null;
                m_EdgeCandidate.input = graphElement;
            }
            m_EdgeCandidate.candidatePosition = e.mousePosition;

            m_GraphView.AddElement(m_EdgeCandidate);

            m_EdgeCandidate.UpdateEdgeControl();

            // Call anchor changed after the edge has been fully setup ( added to graph ... )
            e.StopPropagation();
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                m_EdgeCandidate.candidatePosition = e.mousePosition;

                // Draw ghost edge if possible anchor exists.
                NodeAnchor endAnchor = GetEndAnchor(e.mousePosition);
                if (endAnchor != null)
                {
                    if (m_GhostEdge == null)
                    {
                        m_GhostEdge = new TEdge();
                        m_GhostEdge.isGhostEdge = true;
                        m_GraphView.AddElement(m_GhostEdge);
                    }

                    if (m_EdgeCandidate.output == null)
                    {
                        m_GhostEdge.input = m_EdgeCandidate.input;
                        m_GhostEdge.output = endAnchor;
                    }
                    else
                    {
                        m_GhostEdge.input = endAnchor;
                        m_GhostEdge.output = m_EdgeCandidate.output;
                    }
                }
                else if (m_GhostEdge != null)
                {
                    m_GraphView.RemoveElement(m_GhostEdge);
                    m_GhostEdge = null;
                }

                e.StopPropagation();
            }
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (m_Active)
            {
                if (CanStopManipulation(e) && m_GraphView != null)
                {
                    m_Active = false;

                    // Remove highlights.
                    foreach (NodeAnchor compatibleAnchor in m_CompatibleAnchors)
                    {
                        compatibleAnchor.highlight = false;
                    }

                    // Clean up ghost edges.
                    if (m_GhostEdge != null)
                    {
                        m_GraphView.RemoveElement(m_GhostEdge);
                        m_GhostEdge = null;
                    }

                    NodeAnchor endAnchor = GetEndAnchor(e.mousePosition);

                    if (endAnchor == null && m_Listener != null)
                    {
                        m_Listener.OnDropOutsideAnchor(m_EdgeCandidate, e.mousePosition);
                    }

                    m_GraphView.RemoveElement(m_EdgeCandidate);

                    if (endAnchor != null)
                    {
                        if (m_EdgeCandidate.output == null)
                            m_EdgeCandidate.output = endAnchor;
                        else
                            m_EdgeCandidate.input = endAnchor;

                        m_Listener.OnDrop(m_GraphView, m_EdgeCandidate);
                    }

                    m_EdgeCandidate = null;
                    m_CompatibleAnchors = null;

                    target.ReleaseMouseCapture();
                    e.StopPropagation();
                }
            }
        }

        private NodeAnchor GetEndAnchor(Vector2 mousePosition)
        {
            if (m_GraphView == null)
                return null;

            NodeAnchor endAnchor = null;

            foreach (NodeAnchor compatibleAnchor in m_CompatibleAnchors)
            {
                Rect bounds = compatibleAnchor.worldBound;
                float hitboxExtraPadding = bounds.height;

                // Add extra padding for mouse check to the left of input anchor or right of output anchor.
                if (compatibleAnchor.direction == Direction.Input)
                {
                    // Move bounds to the left by hitboxExtraPadding and increase width
                    // by hitboxExtraPadding.
                    bounds.x -= hitboxExtraPadding;
                    bounds.width += hitboxExtraPadding;
                }
                else if (compatibleAnchor.direction == Direction.Output)
                {
                    // Just add hitboxExtraPadding to the width.
                    bounds.width += hitboxExtraPadding;
                }

                // Check if mouse is over anchor.
                if (bounds.Contains(mousePosition))
                {
                    endAnchor = compatibleAnchor;
                    break;
                }
            }

            return endAnchor;
        }
    }
}
