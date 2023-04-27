// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Manages the temporary wires uses while creating or modifying a wire.
    /// </summary>
    class WireDragHelper
    {
        List<PortModel> m_AllPorts;
        List<PortModel> m_CompatiblePorts;
        GhostWireModel m_GhostWireModel;
        Wire m_GhostWire;
        public GraphView GraphView { get; }
        readonly Func<GraphModel, GhostWireModel> m_GhostWireViewModelCreator;

        GraphViewPanHelper_Internal m_PanHelper = new GraphViewPanHelper_Internal();

        public WireDragHelper(GraphView graphView, Func<GraphModel, GhostWireModel> ghostWireViewModelCreator)
        {
            GraphView = graphView;
            m_GhostWireViewModelCreator = ghostWireViewModelCreator;
            Reset();
        }

        Wire CreateGhostWire(GraphModel graphModel)
        {
            GhostWireModel ghostWire;

            if (m_GhostWireViewModelCreator != null)
            {
                ghostWire = m_GhostWireViewModelCreator.Invoke(graphModel);
            }
            else
            {
                ghostWire = new GhostWireModel { GraphModel = graphModel };
            }

            var ui = ModelViewFactory.CreateUI<Wire>(GraphView, ghostWire);
            return ui;
        }

        GhostWireModel m_WireCandidateModel;
        Wire m_WireCandidate;
        PortModel m_PreviousEndPortModel;

        public GhostWireModel WireCandidateModel => m_WireCandidateModel;

        public void CreateWireCandidate(GraphModel graphModel)
        {
            m_WireCandidate = CreateGhostWire(graphModel);
            m_WireCandidateModel = m_WireCandidate.WireModel as GhostWireModel;
        }

        void ClearWireCandidate()
        {
            m_WireCandidateModel = null;
            m_WireCandidate = null;
        }

        public PortModel draggedPort { get; set; }
        public Wire OriginalWire { get; set; }

        public void Reset()
        {
            if (m_AllPorts != null)
            {
                for (var i = 0; i < m_AllPorts.Count; i++)
                {
                    var pv = m_AllPorts[i].GetView<Port>(GraphView);
                    if (pv != null)
                    {
                        pv.SetEnabled(true);
                    }
                }
                m_AllPorts = null;
            }
            m_CompatiblePorts = null;

            if (m_GhostWire != null)
            {
                GraphView.RemoveElement(m_GhostWire);
            }

            if (m_WireCandidate != null)
            {
                GraphView.RemoveElement(m_WireCandidate);
            }

            if (WireCandidateModel != null && m_GhostWireModel != null)
            {
                ClearWillConnect(m_GhostWireModel, WireCandidateModel.ToPort == null ? WireSide.To : WireSide.From);
            }

            m_PanHelper.Stop();

            if (draggedPort != null)
            {
                ClearWillConnect(draggedPort);
                draggedPort = null;
            }

            m_GhostWire = null;
            ClearWireCandidate();
            EnableAllWires_Internal(GraphView);
        }

        /// <summary>
        /// Handles mouse down event.
        /// </summary>
        /// <param name="evt">The mouse down event.</param>
        /// <param name="compatiblePortsFilter">The predicate that filters the compatible ports. Ports that don't match the predicate are discarded.</param>
        /// <returns></returns>
        public bool HandleMouseDown(MouseDownEvent evt, Predicate<PortModel> compatiblePortsFilter = null)
        {
            var mousePosition = evt.mousePosition;

            if (draggedPort == null || WireCandidateModel == null || draggedPort.PortType == PortType.MissingPort || draggedPort.DataTypeHandle == TypeHandle.MissingPort)
            {
                return false;
            }

            if (m_WireCandidate == null)
                return false;

            if (m_WireCandidate.parent == null)
            {
                GraphView.AddElement(m_WireCandidate);
            }

            var startFromOutput = draggedPort.Direction == PortDirection.Output;

            WireCandidateModel.EndPoint = mousePosition;
            m_WireCandidate.SetEnabled(false);

            if (startFromOutput)
            {
                WireCandidateModel.FromPort = draggedPort;
                WireCandidateModel.ToPort = null;
            }
            else
            {
                WireCandidateModel.FromPort = null;
                WireCandidateModel.ToPort = draggedPort;
            }

            GetCompatiblePorts_Internal(compatiblePortsFilter);

            HighlightCompatiblePorts_Internal();

            m_WireCandidate.UpdateFromModel();

            m_PanHelper.OnMouseDown(evt, GraphView, Pan);

            m_WireCandidate.Layer = Int32.MaxValue;

            return true;
        }

        public void HandleMouseMove(MouseMoveEvent evt)
        {
            m_PanHelper.OnMouseMove(evt);

            var mousePosition = evt.mousePosition;

            WireCandidateModel.EndPoint = mousePosition;
            m_WireCandidate.UpdateFromModel();

            // Draw ghost wire if possible port exists.
            var endPort = GetEndPort(mousePosition);

            if (m_PreviousEndPortModel != null && m_PreviousEndPortModel != endPort?.PortModel)
                ClearWillConnect(m_PreviousEndPortModel);

            if (endPort != null)
            {
                if (m_GhostWire == null)
                {
                    m_GhostWire = CreateGhostWire(endPort.PortModel.GraphModel);
                    m_GhostWireModel = m_GhostWire.WireModel as GhostWireModel;

                    m_GhostWire.pickingMode = PickingMode.Ignore;
                    GraphView.AddElement(m_GhostWire);
                }

                Debug.Assert(m_GhostWireModel != null);

                var sideForEndPort = WireCandidateModel.FromPort == null ? WireSide.From : WireSide.To;
                m_GhostWireModel.SetPort(sideForEndPort, endPort.PortModel);
                ClearWillConnect(m_GhostWireModel, sideForEndPort);
                endPort.WillConnect = true;

                var otherSide = sideForEndPort.GetOtherSide();
                m_GhostWireModel.SetPort(otherSide, WireCandidateModel.GetPort(otherSide));

                m_GhostWire.UpdateFromModel();
                m_PreviousEndPortModel = endPort.PortModel;
            }
            else if (m_GhostWire != null && m_GhostWireModel != null)
            {
                ClearWillConnect(m_GhostWireModel, WireCandidateModel.ToPort == null ? WireSide.To : WireSide.From);

                GraphView.RemoveElement(m_GhostWire);
                m_GhostWireModel.ToPort = null;
                m_GhostWireModel.FromPort = null;
                m_GhostWireModel = null;
                m_GhostWire = null;
            }
        }

        internal static void EnableAllWires_Internal(GraphView graphView, bool isEnabled = true, List<WireModel> exemptedWires = null)
        {
            if (graphView.GraphViewModel == null || graphView.GraphModel == null)
                return;

            foreach (var otherWire in graphView.GraphModel.WireModels)
            {
                if (exemptedWires == null || !exemptedWires.Contains(otherWire))
                {
                    var view = otherWire.GetView_Internal(graphView);
                    view?.SetEnabled(isEnabled);
                }
            }
        }

        void ClearWillConnect(WireModel wireModel)
        {
            ClearWillConnect(wireModel, WireSide.From);
            ClearWillConnect(wireModel, WireSide.To);
        }

        void ClearWillConnect(WireModel wireModel, WireSide wireSide)
        {
            ClearWillConnect(wireModel?.GetPort(wireSide));
        }

        void ClearWillConnect(PortModel portModel)
        {
            var port = portModel?.GetView<Port>(GraphView);
            if (port != null)
                port.WillConnect = false;
        }

        void Pan(TimerState timerState)
        {
            WireCandidateModel.GetView<Wire>(GraphView)?.UpdateFromModel();
        }

        public void HandleMouseUp(MouseUpEvent evt, bool isFirstWire, IEnumerable<Wire> otherWires, IEnumerable<PortModel> otherPorts)
        {
            var mousePosition = evt.mousePosition;

            var position = GraphView.ContentViewContainer.transform.position;
            var scale = GraphView.ContentViewContainer.transform.scale;
            GraphView.Dispatch(new ReframeGraphViewCommand(position, scale));

            StopHighlightingCompatiblePorts_Internal();

            // Clean up ghost wires.
            if (m_GhostWireModel != null)
            {
                ClearWillConnect(m_GhostWireModel);

                GraphView.RemoveElement(m_GhostWire);
                m_GhostWireModel.ToPort = null;
                m_GhostWireModel.FromPort = null;
                m_GhostWireModel = null;
                m_GhostWire = null;
            }

            ClearWillConnect(WireCandidateModel);

            var removeWireCandidate = WireCandidateModel?.ToPort == null || WireCandidateModel?.FromPort == null;

            var endPort = GetEndPort(mousePosition);
            if (endPort != null)
            {
                m_WireCandidate.SetEnabled(true);

                if (WireCandidateModel != null)
                {
                    if (endPort.PortModel.Direction == PortDirection.Output)
                        WireCandidateModel.FromPort = endPort.PortModel;
                    else
                        WireCandidateModel.ToPort = endPort.PortModel;
                }
            }

            // Let the first wire handle the batch command for all wires
            if (isFirstWire)
            {
                var affectedWires = (OriginalWire != null
                        ? Enumerable.Repeat(OriginalWire, 1)
                        : Enumerable.Empty<Wire>())
                    .Concat(otherWires);

                if (endPort != null)
                {
                    if (OriginalWire == null)
                        CreateNewWire(m_WireCandidate.WireModel.FromPort, m_WireCandidate.WireModel.ToPort);
                    else
                        MoveWires(affectedWires, endPort);
                }
                else
                {
                    removeWireCandidate = false;

                    if (OriginalWire == null)
                    {
                        DropWiresOutside(Enumerable.Repeat(m_WireCandidate, 1), Enumerable.Repeat(draggedPort, 1), mousePosition);
                    }
                    else
                    {
                        DropWiresOutside(affectedWires.Append(m_WireCandidate), Enumerable.Repeat(draggedPort, 1).Concat(otherPorts), mousePosition);
                    }
                }
            }

            if (removeWireCandidate)
            {
                // If it is an existing valid wire then delete and notify the model (using DeleteElements()).
                GraphView.RemoveElement(m_WireCandidate);
            }

            m_WireCandidate?.ResetLayer();

            ClearWireCandidate();
            m_CompatiblePorts = null;
            m_AllPorts = null;
            m_PanHelper.OnMouseUp(evt);
            Reset();

            OriginalWire = null;
        }

        protected virtual void DropWiresOutside(IEnumerable<Wire> wires,
            IEnumerable<PortModel> portModels, Vector2 worldPosition)
        {
            if (!(GraphView.GraphModel?.Stencil is Stencil stencil))
                return;

            var wiresToConnect = wires
                .Zip(portModels, (e, p) => new { wire = e, port = p })
                .Select(e => (e.wire.WireModel, e.port.Direction == PortDirection.Input ? WireSide.From : WireSide.To))
                .ToList();

            var wiresToDelete = wires.Where(w => w.IsGhostWire).Select(w => w.WireModel).ToList();

            stencil.CreateNodesFromWires(GraphView, wiresToConnect, worldPosition, wiresToDelete);
        }

        protected virtual void MoveWires(IEnumerable<Wire> wires, Port newPort)
        {
            var wiresToMove = wires.Select(e => e.WireModel).ToList();
            GraphView.Dispatch(new MoveWireCommand(newPort.PortModel, wiresToMove));
        }

        protected virtual void CreateNewWire(PortModel fromPort, PortModel toPort)
        {
            GraphView.Dispatch(new CreateWireCommand(toPort, fromPort));
        }

        Port GetEndPort(Vector2 mousePosition)
        {
            Port endPort = null;

            foreach (var compatiblePort in m_CompatiblePorts)
            {
                var compatiblePortUI = compatiblePort.GetView<Port>(GraphView);
                if (compatiblePortUI == null || compatiblePortUI.resolvedStyle.visibility != Visibility.Visible)
                    continue;

                // Check if the mouse is over the port hit box.
                if (Port.GetPortHitBoxBounds(compatiblePortUI).Contains(mousePosition))
                    endPort = compatiblePortUI;
            }

            return endPort;
        }

        protected internal void GetCompatiblePorts_Internal(Predicate<PortModel> compatiblePortsFilter = null)
        {
            m_AllPorts = GraphView.GraphModel.GetPortModels().ToList();
            m_CompatiblePorts = GraphView.GraphModel.GetCompatiblePorts(m_AllPorts, draggedPort);

            // Filter compatible ports
            if (compatiblePortsFilter != null)
            {
                m_CompatiblePorts = m_CompatiblePorts.FindAll(compatiblePortsFilter);
            }
        }

        protected internal void HighlightCompatiblePorts_Internal()
        {
            // Only light compatible anchors when dragging a wire.
            for (var i = 0; i < m_AllPorts.Count; i++)
            {
                var pv = m_AllPorts[i].GetView<Port>(GraphView);
                if (pv != null)
                {
                    pv.SetEnabled(false);
                }
            }

            for (var i = 0; i < m_CompatiblePorts.Count; i++)
            {
                var pv = m_CompatiblePorts[i].GetView<Port>(GraphView);
                if (pv != null)
                {
                    pv.SetEnabled(true);
                }
            }

            var portUI = draggedPort.GetView<Port>(GraphView);
            if (portUI != null)
            {
                portUI.WillConnect = true;
                portUI.SetEnabled(true);
            }
        }

        protected internal void StopHighlightingCompatiblePorts_Internal()
        {
            for (var i = 0; i < m_AllPorts.Count; i++)
            {
                var pv = m_AllPorts[i].GetView<Port>(GraphView);
                if (pv != null)
                {
                    pv.SetEnabled(true);
                }
            }
        }
    }
}
