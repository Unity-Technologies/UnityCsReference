// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public abstract class EdgeDragHelper
    {
        public abstract Edge edgeCandidate { get; set; }
        public abstract Port draggedPort { get; set; }
        public abstract bool HandleMouseDown(MouseDownEvent evt);
        public abstract void HandleMouseMove(MouseMoveEvent evt);
        public abstract void HandleMouseUp(MouseUpEvent evt);
        public abstract void Reset(bool didConnect = false);

        internal const int k_PanAreaWidth = 100;
        internal const int k_PanSpeed = 4;
        internal const int k_PanInterval = 10;
        internal const float k_MinSpeedFactor = 0.5f;
        internal const float k_MaxSpeedFactor = 2.5f;
        internal const float k_MaxPanSpeed = k_MaxSpeedFactor * k_PanSpeed;
    }

    public class EdgeDragHelper<TEdge> : EdgeDragHelper where TEdge : Edge, new()
    {
        protected List<Port> m_CompatiblePorts;
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

        public override Port draggedPort { get; set; }

        public override void Reset(bool didConnect = false)
        {
            if (m_CompatiblePorts != null)
            {
                // Reset the highlights.
                foreach (Port compatiblePort in m_CompatiblePorts)
                {
                    compatiblePort.highlight = true;
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

            if (m_GhostEdge != null)
            {
                m_GhostEdge.input = null;
                m_GhostEdge.output = null;
            }

            m_GhostEdge = null;
            edgeCandidate = null;
            draggedPort = null;
            m_CompatiblePorts = null;
            m_GraphView = null;
        }

        public override bool HandleMouseDown(MouseDownEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;

            if ((draggedPort == null) || (edgeCandidate == null))
            {
                return false;
            }

            m_GraphView = draggedPort.GetFirstAncestorOfType<GraphView>();

            if (m_GraphView == null)
            {
                return false;
            }

            bool startFromOutput = (draggedPort.direction == Direction.Output);

            if (startFromOutput)
            {
                edgeCandidate.output = draggedPort;
                edgeCandidate.input = null;
            }
            else
            {
                edgeCandidate.output = null;
                edgeCandidate.input = draggedPort;
            }

            draggedPort.portCapLit = true;

            if (edgeCandidate.parent == null)
            {
                m_GraphView.AddElement(edgeCandidate);
            }
            edgeCandidate.candidatePosition = mousePosition;

            m_CompatiblePorts = m_GraphView.GetCompatiblePorts(draggedPort, s_nodeAdapter);

            // Only light compatible anchors when dragging an edge.
            foreach (Port port in m_GraphView.ports.ToList())
            {
                port.highlight = false;
            }

            foreach (Port compatiblePort in m_CompatiblePorts)
            {
                compatiblePort.highlight = true;
            }

            edgeCandidate.UpdateEdgeControl();

            if (m_PanSchedule == null)
            {
                m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
                m_PanSchedule.Pause();
            }
            m_WasPanned = false;

            edgeCandidate.layer = Int32.MaxValue;

            return true;
        }

        internal Vector2 GetEffectivePanSpeed(Vector2 mousePos)
        {
            Vector2 effectiveSpeed = Vector2.zero;

            if (mousePos.x <= k_PanAreaWidth)
                effectiveSpeed.x = -(((k_PanAreaWidth - mousePos.x) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
            else if (mousePos.x >= m_GraphView.contentContainer.layout.width - k_PanAreaWidth)
                effectiveSpeed.x = (((mousePos.x - (m_GraphView.contentContainer.layout.width - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

            if (mousePos.y <= k_PanAreaWidth)
                effectiveSpeed.y = -(((k_PanAreaWidth - mousePos.y) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
            else if (mousePos.y >= m_GraphView.contentContainer.layout.height - k_PanAreaWidth)
                effectiveSpeed.y = (((mousePos.y - (m_GraphView.contentContainer.layout.height - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, k_MaxPanSpeed);

            return effectiveSpeed;
        }

        public override void HandleMouseMove(MouseMoveEvent evt)
        {
            var ve = (VisualElement)evt.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, evt.localMousePosition);
            m_PanDiff = GetEffectivePanSpeed(gvMousePos);

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

            // Draw ghost edge if possible port exists.
            Port endPort = GetEndPort(mousePosition);

            if (endPort != null)
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
                    if (m_GhostEdge.output != null)
                        m_GhostEdge.output.portCapLit = false;
                    m_GhostEdge.output = endPort;
                    m_GhostEdge.output.portCapLit = true;
                }
                else
                {
                    if (m_GhostEdge.input != null)
                        m_GhostEdge.input.portCapLit = false;
                    m_GhostEdge.input = endPort;
                    m_GhostEdge.input.portCapLit = true;
                    m_GhostEdge.output = edgeCandidate.output;
                }
            }
            else if (m_GhostEdge != null)
            {
                if (edgeCandidate.input == null)
                {
                    if (m_GhostEdge.input != null)
                        m_GhostEdge.input.portCapLit = false;
                }
                else
                {
                    if (m_GhostEdge.output != null)
                        m_GhostEdge.output.portCapLit = false;
                }
                m_GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdge.input = null;
                m_GhostEdge.output = null;
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

            // Reset the highlights.
            foreach (Port port in m_GraphView.ports.ToList())
            {
                port.highlight = true;
            }

            // Clean up ghost edges.
            if (m_GhostEdge != null)
            {
                if (m_GhostEdge.input != null)
                    m_GhostEdge.input.portCapLit = false;
                if (m_GhostEdge.output != null)
                    m_GhostEdge.output.portCapLit = false;

                m_GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdge.input = null;
                m_GhostEdge.output = null;
                m_GhostEdge = null;
            }

            Port endPort = GetEndPort(mousePosition);

            if (endPort == null && m_Listener != null)
            {
                m_Listener.OnDropOutsidePort(edgeCandidate, mousePosition);
            }

            if (edgeCandidate.input != null)
                edgeCandidate.input.portCapLit = false;

            if (edgeCandidate.output != null)
                edgeCandidate.output.portCapLit = false;

            // If it is an existing valid edge then delete and notify the model (using DeleteElements()).
            if (edgeCandidate.input != null && edgeCandidate.output != null)
            {
                // Save the current input and output before deleting the edge as they will be reset
                Port oldInput = edgeCandidate.input;
                Port oldOutput = edgeCandidate.output;

                m_GraphView.DeleteElements(new[] { edgeCandidate });

                // Restore the previous input and output
                edgeCandidate.input = oldInput;
                edgeCandidate.output = oldOutput;
            }
            // otherwise, if it is an temporary edge then just remove it as it is not already known my the model
            else
            {
                m_GraphView.RemoveElement(edgeCandidate);
            }

            if (endPort != null)
            {
                if (endPort.direction == Direction.Output)
                    edgeCandidate.output = endPort;
                else
                    edgeCandidate.input = endPort;

                m_Listener.OnDrop(m_GraphView, edgeCandidate);
                didConnect = true;
            }
            else
            {
                edgeCandidate.output = null;
                edgeCandidate.input = null;
            }

            edgeCandidate.ResetLayer();

            edgeCandidate = null;
            m_CompatiblePorts = null;
            Reset(didConnect);
        }

        private Port GetEndPort(Vector2 mousePosition)
        {
            if (m_GraphView == null)
                return null;

            Port endPort = null;

            foreach (Port compatiblePort in m_CompatiblePorts)
            {
                Rect bounds = compatiblePort.worldBound;
                float hitboxExtraPadding = bounds.height;

                // Add extra padding for mouse check to the left of input port or right of output port.
                if (compatiblePort.direction == Direction.Input)
                {
                    // Move bounds to the left by hitboxExtraPadding and increase width
                    // by hitboxExtraPadding.
                    bounds.x -= hitboxExtraPadding;
                    bounds.width += hitboxExtraPadding;
                }
                else if (compatiblePort.direction == Direction.Output)
                {
                    // Just add hitboxExtraPadding to the width.
                    bounds.width += hitboxExtraPadding;
                }

                // Check if mouse is over port.
                if (bounds.Contains(mousePosition))
                {
                    endPort = compatiblePort;
                    break;
                }
            }

            return endPort;
        }
    }
}
