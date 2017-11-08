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
    abstract class EdgeDragHelper
    {
        public abstract Edge edgeCandidate { get; set; }
        public abstract NodeAnchor draggedNodeAnchor { get; set; }
        public abstract bool HandleMouseDown(MouseDownEvent evt);
        public abstract void HandleMouseMove(MouseMoveEvent evt);
        public abstract void HandleMouseUp(MouseUpEvent evt);
        public abstract void Reset(bool didConnect = false);

        internal const int k_PanAreaWidth = 20;
        internal const int k_PanSpeed = 4;
        internal const int k_PanInterval = 10;
    }

    internal
    class EdgeDragHelper<TEdge> : EdgeDragHelper where TEdge : Edge, new()
    {
        protected List<NodeAnchor> m_CompatibleAnchors;
        private Edge m_GhostEdge;
        protected GraphView m_GraphView;
        protected static NodeAdapter s_nodeAdapter = new NodeAdapter();
        protected readonly IEdgeConnectorListener m_Listener;

        private IVisualElementScheduledItem m_PanSchedule;
        private Vector3 m_PanDiff = Vector3.zero;
        private bool m_WasPanned;

        public bool resetPositionOnPan { get; set; }

        public EdgeDragHelper(IEdgeConnectorListener listener)
        {
            m_Listener = listener;
            resetPositionOnPan = true;
            Reset();
        }

        public override Edge edgeCandidate { get; set; }

        public override NodeAnchor draggedNodeAnchor { get; set; }

        public override void Reset(bool didConnect = false)
        {
            if (m_CompatibleAnchors != null)
            {
                // Remove highlights.
                foreach (NodeAnchor compatibleAnchor in m_CompatibleAnchors)
                {
                    compatibleAnchor.highlight = false;
                }
            }

            // Clean up ghost edge.
            if ((m_GhostEdge != null) && (m_GraphView != null))
            {
                m_GraphView.RemoveElement(m_GhostEdge);
            }

            if (m_WasPanned)
            {
                if (!resetPositionOnPan || didConnect)
                {
                    Vector3 p = m_GraphView.contentViewContainer.transform.position;
                    Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                    m_GraphView.UpdateViewTransform(p, s);
                }
            }

            if (m_PanSchedule != null)
                m_PanSchedule.Pause();

            m_GhostEdge = null;
            edgeCandidate = null;
            draggedNodeAnchor = null;
            m_CompatibleAnchors = null;
            m_GraphView = null;
        }

        public override bool HandleMouseDown(MouseDownEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;

            if ((draggedNodeAnchor == null) || (edgeCandidate == null))
            {
                return false;
            }

            m_GraphView = draggedNodeAnchor.GetFirstAncestorOfType<GraphView>();

            if (m_GraphView == null)
            {
                return false;
            }

            bool startFromOutput = (draggedNodeAnchor.direction == Direction.Output);

            if (startFromOutput)
            {
                edgeCandidate.output = draggedNodeAnchor;
                edgeCandidate.input = null;
            }
            else
            {
                edgeCandidate.output = null;
                edgeCandidate.input = draggedNodeAnchor;
            }

            if (edgeCandidate.parent == null)
            {
                m_GraphView.AddElement(edgeCandidate);
            }
            edgeCandidate.candidatePosition = mousePosition;

            m_CompatibleAnchors = m_GraphView.GetCompatibleAnchors(draggedNodeAnchor, s_nodeAdapter);

            foreach (NodeAnchor compatibleAnchor in m_CompatibleAnchors)
            {
                compatibleAnchor.highlight = true;
            }

            edgeCandidate.UpdateEdgeControl();

            if (m_PanSchedule == null)
            {
                m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
                m_PanSchedule.Pause();
            }
            m_WasPanned = false;

            return true;
        }

        public override void HandleMouseMove(MouseMoveEvent evt)
        {
            var ve = (VisualElement)evt.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, evt.localMousePosition);
            m_PanDiff = Vector3.zero;

            if (gvMousePos.x <= k_PanAreaWidth)
                m_PanDiff.x = -k_PanSpeed;
            else if (gvMousePos.x >= m_GraphView.contentContainer.layout.width - k_PanAreaWidth)
                m_PanDiff.x = k_PanSpeed;

            if (gvMousePos.y <= k_PanAreaWidth)
                m_PanDiff.y = -k_PanSpeed;
            else if (gvMousePos.y >= m_GraphView.contentContainer.layout.height - k_PanAreaWidth)
                m_PanDiff.y = k_PanSpeed;

            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            Vector2 mousePosition = evt.mousePosition;

            edgeCandidate.candidatePosition = mousePosition;

            // Draw ghost edge if possible anchor exists.
            NodeAnchor endAnchor = GetEndAnchor(mousePosition);

            if (endAnchor != null)
            {
                if (m_GhostEdge == null)
                {
                    m_GhostEdge = new TEdge();
                    m_GhostEdge.isGhostEdge = true;
                    m_GraphView.AddElement(m_GhostEdge);
                }

                if (edgeCandidate.output == null)
                {
                    m_GhostEdge.input = edgeCandidate.input;
                    m_GhostEdge.output = endAnchor;
                }
                else
                {
                    m_GhostEdge.input = endAnchor;
                    m_GhostEdge.output = edgeCandidate.output;
                }
            }
            else if (m_GhostEdge != null)
            {
                m_GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdge = null;
            }
        }

        private void Pan(TimerState ts)
        {
            m_GraphView.viewTransform.position -= m_PanDiff;
            m_WasPanned = true;
        }

        public override void HandleMouseUp(MouseUpEvent evt)
        {
            bool didConnect = false;

            Vector2 mousePosition = evt.mousePosition;

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

            NodeAnchor endAnchor = GetEndAnchor(mousePosition);

            if (endAnchor == null && m_Listener != null)
            {
                m_Listener.OnDropOutsideAnchor(edgeCandidate, mousePosition);
            }

            m_GraphView.RemoveElement(edgeCandidate);

            if (endAnchor != null)
            {
                if (edgeCandidate.output == null)
                    edgeCandidate.output = endAnchor;
                else
                    edgeCandidate.input = endAnchor;

                m_Listener.OnDrop(m_GraphView, edgeCandidate);
                didConnect = true;
            }
            else
            {
                edgeCandidate.output = null;
                edgeCandidate.input = null;
            }

            edgeCandidate = null;
            m_CompatibleAnchors = null;
            Reset(didConnect);
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
