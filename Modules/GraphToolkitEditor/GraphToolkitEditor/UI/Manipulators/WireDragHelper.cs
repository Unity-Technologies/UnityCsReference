// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolkit.ItemLibrary.Editor;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manages the temporary wires uses while creating or modifying a wire.
    /// </summary>
    [UnityRestricted]
    internal class WireDragHelper
    {
        List<PortModel> m_AllPorts;
        List<PortModel> m_CompatiblePorts;
        WireModel m_GhostWireModel;
        AbstractWire m_GhostWire;
        GraphViewPanHelper m_PanHelper = new();
        AbstractWire m_WireCandidate;
        PortModel m_PreviousEndPortModel;

        /// <summary>
        /// The graph view in which the wire is created.
        /// </summary>
        public GraphView GraphView { get; }

        /// <summary>
        /// The model of the wire candidate.
        /// </summary>
        public AbstractGhostWireModel WireCandidateModel { get; private set; }

        /// <summary>
        /// The port from which the wire is being dragged.
        /// </summary>
        public PortModel DraggedPort { get; set; }

        /// <summary>
        /// The original wire that is being modified.
        /// </summary>
        public AbstractWire OriginalWire { get; set; }

        /// <summary>
        /// A delegate that creates a ghost wire view model. If not set, a <see cref="GhostWireModel"/> is created.
        /// </summary>
        public Func<GraphModel, AbstractGhostWireModel> GhostWireViewModelCreator { get; set; } = null;

        public WireDragHelper(GraphView graphView)
        {
            GraphView = graphView;
            Reset();
        }

        (AbstractWire, AbstractGhostWireModel) CreateGhostWire(GraphModel graphModel)
        {
            AbstractGhostWireModel ghostWire;

            ghostWire = GhostWireViewModelCreator != null ? GhostWireViewModelCreator.Invoke(graphModel) : new GhostWireModel { GraphModel = graphModel };

            var ui = ModelViewFactory.CreateUI<AbstractWire>(GraphView, ghostWire);
            return (ui, ghostWire);
        }

        /// <summary>
        /// Creates a ghost wire and its model.
        /// </summary>
        public void CreateWireCandidate()
        {
            (m_WireCandidate, WireCandidateModel) = CreateGhostWire(GraphView.GraphModel);
        }

        void ClearWireCandidate()
        {
            WireCandidateModel = null;
            m_WireCandidate = null;
        }

        /// <summary>
        /// Resets the wire drag helper.
        /// </summary>
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

            if (DraggedPort != null)
            {
                ClearWillConnect(DraggedPort);
                DraggedPort = null;
            }

            m_GhostWire = null;
            ClearWireCandidate();
            EnableAllWires(GraphView);
            GraphView.IsWireDragging = false;
            EditorGUIUtilityBridge.SetCursor(MouseCursor.Arrow);
        }

        /// <summary>
        /// Handles mouse down event.
        /// </summary>
        /// <param name="evt">The mouse down event.</param>
        /// <param name="compatiblePortsFilter">The predicate that filters the compatible ports. Ports that don't match the predicate are discarded.</param>
        public bool HandleMouseDown(MouseDownEvent evt, Predicate<PortModel> compatiblePortsFilter = null)
        {
            var mousePosition = evt.mousePosition;

            if (DraggedPort == null || WireCandidateModel == null || DraggedPort.PortType == PortType.MissingPort || DraggedPort.DataTypeHandle == TypeHandle.MissingPort)
            {
                return false;
            }

            if (m_WireCandidate == null)
                return false;

            if (m_WireCandidate.parent == null)
            {
                GraphView.AddElement(m_WireCandidate);
            }

            var startFromOutput = DraggedPort.Direction == PortDirection.Output;

            m_WireCandidate.SetEnabled(false);
            WireCandidateModel.FromWorldPoint = mousePosition;
            WireCandidateModel.ToWorldPoint = mousePosition;

            if (startFromOutput)
            {
                WireCandidateModel.FromPort = DraggedPort;
                WireCandidateModel.ToPort = null;
            }
            else
            {
                WireCandidateModel.FromPort = null;
                WireCandidateModel.ToPort = DraggedPort;
            }

            GetCompatiblePorts(compatiblePortsFilter);

            HighlightCompatiblePorts();

            GraphView.IsWireDragging = true;

            m_WireCandidate.DoCompleteUpdate();

            m_PanHelper.OnMouseDown(evt, GraphView, Pan);

            m_WireCandidate.Layer = Int32.MaxValue;

            return true;
        }

        /// <summary>
        /// Handles mouse move event.
        /// </summary>
        /// <param name="evt">The mouse move event.</param>
        public void HandleMouseMove(MouseMoveEvent evt)
        {
            m_PanHelper.OnMouseMove(evt);

            var mousePosition = evt.mousePosition;

            if (DraggedPort.Direction == PortDirection.Output)
            {
                WireCandidateModel.ToWorldPoint = mousePosition;
            }
            else
            {
                WireCandidateModel.FromWorldPoint = mousePosition;
            }
            m_WireCandidate.DoCompleteUpdate();

            EditorGUIUtilityBridge.SetCursor(MouseCursor.Link);

            // Draw ghost wire if possible port exists.
            var endPort = GetEndPort(mousePosition);

            if (m_PreviousEndPortModel != null && m_PreviousEndPortModel != endPort?.PortModel)
                ClearWillConnect(m_PreviousEndPortModel);

            if (endPort != null)
            {
                if (m_GhostWire == null)
                {
                    (m_GhostWire, m_GhostWireModel) = CreateGhostWire(endPort.PortModel.GraphModel);

                    m_GhostWire.pickingMode = PickingMode.Ignore;
                    GraphView.AddElement(m_GhostWire);
                }

                Debug.Assert(m_GhostWireModel != null);

                var sideForEndPort = WireCandidateModel.FromPort == null ? WireSide.From : WireSide.To;
                var previousEndPort = m_GhostWireModel?.GetPort(sideForEndPort);
                if (previousEndPort != null && previousEndPort.Guid != endPort.PortModel.Guid)
                    ClearWillConnect(previousEndPort);

                m_GhostWireModel?.SetPort(sideForEndPort, endPort.PortModel);
                endPort.WillConnect = true;
                // When the port will connect, show the node hover border.
                ToggleNodeHoverBorder(endPort, true);

                var otherSide = sideForEndPort.GetOtherSide();
                m_GhostWireModel?.SetPort(otherSide, WireCandidateModel.GetPort(otherSide));

                m_GhostWire.DoCompleteUpdate();
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

        internal static void EnableAllWires(GraphView graphView, bool isEnabled = true, List<WireModel> exemptedWires = null)
        {
            if (graphView.GraphViewModel == null || graphView.GraphModel == null)
                return;

            foreach (var otherWire in graphView.GraphModel.WireModels)
            {
                if (exemptedWires == null || !exemptedWires.Contains(otherWire))
                {
                    var view = otherWire.GetView(graphView);
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
            {
                port.WillConnect = false;
                // When the port will not connect, clear the node hover border.
                ToggleNodeHoverBorder(port, false);
            }
        }

        void ToggleNodeHoverBorder(Port port, bool showHoverBorder)
        {
            var nodeUI = port.PortModel.NodeModel.GetView<NodeView>(GraphView);
            if (nodeUI != null)
            {
                nodeUI.Border.Hovered = showHoverBorder;

                // Also toggle the hover border of the container, if there is any.
                if (port.PortModel.NodeModel.Container is GraphElementModel container && container.GetView(GraphView) is GraphElement containerUI)
                    containerUI.Border.Hovered = showHoverBorder;
            }
        }

        void Pan(TimerState timerState)
        {
            WireCandidateModel.GetView<Wire>(GraphView)?.DoCompleteUpdate();
        }

        /// <summary>
        /// Handles mouse up event.
        /// </summary>
        /// <param name="evt">The mouse up event.</param>
        /// <param name="isFirstWire">Whether this is the first wire in a batch of wires.</param>
        /// <param name="otherWires">The other wires that are being dragged with this wire.</param>
        /// <param name="otherPorts">The ports for the <paramref name="otherWires"/>.</param>
        public void HandleMouseUp(MouseUpEvent evt, bool isFirstWire, IEnumerable<AbstractWire> otherWires, IEnumerable<PortModel> otherPorts)
        {
            var mousePosition = evt.mousePosition;

            var position = GraphView.ContentViewContainer.resolvedStyle.translate;
            var scale = GraphView.ContentViewContainer.resolvedStyle.scale.value;
            GraphView.Dispatch(new ReframeGraphViewCommand(position, scale));

            StopHighlightingCompatiblePorts();

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
                    if (WireCandidateModel.FromPort == null)
                        WireCandidateModel.FromPort = endPort.PortModel;
                    else
                        WireCandidateModel.ToPort = endPort.PortModel;
                }
            }

            // Let the first wire handle the batch command for all wires
            if (isFirstWire)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var affectedWires = (OriginalWire != null
#pragma warning restore RS0030
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    ? Enumerable.Repeat(OriginalWire, 1)
#pragma warning restore RS0030
                    : Array.Empty<Wire>())
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
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        DropWiresOutside(Enumerable.Repeat(m_WireCandidate, 1), Enumerable.Repeat(DraggedPort, 1), mousePosition);
#pragma warning restore RS0030
                    }
                    else
                    {
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        DropWiresOutside(affectedWires.Append(m_WireCandidate), Enumerable.Repeat(DraggedPort, 1).Concat(otherPorts), mousePosition);
#pragma warning restore RS0030
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

        /// <summary>
        /// Handler for when wires are dropped outside of any port.
        /// </summary>
        /// <param name="wires">The dragged wires.</param>
        /// <param name="portModels">The ports from which the wires are dragged.</param>
        /// <param name="worldPosition">The location where the mouse is released.</param>
        protected virtual void DropWiresOutside(IEnumerable<AbstractWire> wires,
            IEnumerable<PortModel> portModels, Vector2 worldPosition)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var wiresToConnect = wires
#pragma warning restore RS0030
                .Zip(portModels, (e, p) => new { wire = e, port = p })
                .Select(e => (e.wire.WireModel, e.port.Direction == PortDirection.Input ? WireSide.From : WireSide.To))
                .ToList();

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var wiresToDelete = wires.Where(w => w.Model is IGhostWireModel).Select(w => w.WireModel).ToList();
#pragma warning restore RS0030

            CreateNodesFromWires(GraphView, wiresToConnect, worldPosition, wiresToDelete);
        }

        /// <summary>
        /// Prompt the Item Library to create nodes to connect
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="wires">The wires to connect.</param>
        /// <param name="worldPosition">The position on which the nodes will be created.</param>
        /// <param name="wiresToDelete">The wires that need to be deleted, if any.</param>
        static void CreateNodesFromWires(GraphView view, IReadOnlyList<(WireModel model, WireSide side)> wires, Vector2 worldPosition, List<WireModel> wiresToDelete)
        {
            var localPosition = view.ContentViewContainer.WorldToLocal(worldPosition);
            Action<ItemLibraryItem> createNode = item =>
            {
                if (item is GraphNodeModelLibraryItem nodeItem)
                    view.Dispatch(CreateNodeCommand.OnWireSide(nodeItem, wires, localPosition));

                if (item is VariableLibraryItem variableItem)
                {
                    var blackboardSection = view.GraphModel.GetSectionModel(GraphModel.DefaultSectionName);
                    var index = blackboardSection.Items.Count;

                    view.Dispatch(CreateNodeCommand.OnWireSide(variableItem, wires, localPosition, blackboardSection, index));
                }

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var allWiresToDelete = wires.Where(w => w.model is IGhostWireModel).Select(w => w.model).ToList();
#pragma warning restore RS0030
                if (wiresToDelete != null)
                    allWiresToDelete.AddRange(wiresToDelete);

                if (allWiresToDelete.Count > 0)
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var modelView in allWiresToDelete.Select(wireModel => wireModel.GetView(view)))
#pragma warning restore RS0030
                    {
                        if (modelView is GraphElement element)
                            view.RemoveElement(element);
                    }
                }
            };
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var portModels = wires.Select(e => e.model.GetOtherPort(e.side)).ToList();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            switch (portModels.First().Direction)
#pragma warning restore RS0030
            {
                case PortDirection.Output:
                    ItemLibraryService.ShowOutputToGraphNodes(view, portModels, worldPosition, createNode);
                    break;

                case PortDirection.Input:
                    ItemLibraryService.ShowInputToGraphNodes(view, portModels, worldPosition, createNode);
                    break;
                case PortDirection.None:
                default:
                    break;
            }
        }

        /// <summary>
        /// Moves the wires to the new port.
        /// </summary>
        /// <param name="wires">The wires to move.</param>
        /// <param name="newPort">The new port to which the wires are moved.</param>
        protected virtual void MoveWires(IEnumerable<AbstractWire> wires, Port newPort)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var wiresToMove = wires.Select(e => e.WireModel).ToList();
#pragma warning restore RS0030
            GraphView.Dispatch(new MoveWireCommand(newPort.PortModel, wiresToMove));
        }

        /// <summary>
        /// Creates a new wire between the two ports.
        /// </summary>
        /// <param name="fromPort">The port from which the wire starts.</param>
        /// <param name="toPort">The port to which the wire ends.</param>
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
                var parentNodeUI = compatiblePortUI?.PortModel?.NodeModel.GetView<NodeView>(GraphView);
                if (compatiblePortUI == null || compatiblePortUI.resolvedStyle.visibility != Visibility.Visible || (parentNodeUI?.IsCulled() ?? false))
                    continue;

                // Check if the mouse is over the port hit box.
                if (Port.GetPortHitBoxBounds(compatiblePortUI).Contains(mousePosition))
                    endPort = compatiblePortUI;
            }

            return endPort;
        }

        internal void GetCompatiblePorts(Predicate<PortModel> compatiblePortsFilter = null)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_AllPorts = GraphView.GraphModel.GetPortModels().ToList();
#pragma warning restore RS0030
            m_CompatiblePorts = GraphView.GraphModel.GetCompatiblePorts(m_AllPorts, DraggedPort);

            // Filter compatible ports
            if (compatiblePortsFilter != null)
            {
                m_CompatiblePorts = m_CompatiblePorts.FindAll(compatiblePortsFilter);
            }
        }

        internal void HighlightCompatiblePorts()
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

            var portUI = DraggedPort.GetView<Port>(GraphView);
            if (portUI != null)
            {
                portUI.WillConnect = true;
                portUI.SetEnabled(true);
            }
        }

        internal void StopHighlightingCompatiblePorts()
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
