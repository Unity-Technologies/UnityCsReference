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
    class SnapToPortStrategy_Internal : SnapStrategy
    {
        class SnapToPortResult
        {
            public float Offset { get; set; }
            public float Distance => Math.Abs(Offset);
            public PortOrientation PortOrientation { get; set; }

            public void Apply(ref SnapDirection snapDirection, ref Vector2 snappedRect)
            {
                if (PortOrientation == PortOrientation.Horizontal)
                {
                    snappedRect.y += Offset;
                    snapDirection |= SnapDirection.SnapY;
                }
                else
                {
                    snappedRect.x += Offset;
                    snapDirection |= SnapDirection.SnapX;
                }
            }
        }

        /// <summary>
        /// Model of the node we try to snap.
        /// </summary>
         PortNodeModel m_SelectedNodeModel;

        /// <summary>
        /// List of wires connected to the node to snap.
        /// </summary>
        List<WireModel> m_ConnectedWires = new List<WireModel>();

        /// <summary>
        /// Position in the graph of every potential port to snap to.
        /// </summary>
        Dictionary<Port, Vector2> m_ConnectedPortsPos = new Dictionary<Port, Vector2>();

        public override void BeginSnap(GraphElement selectedElement)
        {
            base.BeginSnap(selectedElement);

            m_SelectedNodeModel = selectedElement?.Model as PortNodeModel;
            if (m_SelectedNodeModel != null)
            {
                m_ConnectedWires.Clear(); // should be the case already
                m_ConnectedWires.AddRange(m_SelectedNodeModel.GetConnectedWires());
                m_ConnectedPortsPos = GetConnectedPortPositions(selectedElement?.GraphView, m_ConnectedWires);
            }
        }

        protected override Vector2 ComputeSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement)
        {
            var snappedPosition = sourceRect.position;
            snapDirection = SnapDirection.SnapNone;

            if (m_SelectedNodeModel != null)
            {
                var graphView = selectedElement.GraphView;

                // snapping is used while dragging, so the element position doesn't necessarily match sourceRect
                var delta = snappedPosition - selectedElement.layout.position;
                foreach (var port in m_SelectedNodeModel.Ports
                             .Where(p => p.IsConnected())
                             .Select(p => p.GetView<Port>(graphView))
                             .Where(p => p != null))
                {
                    m_ConnectedPortsPos[port] = GetPortCenterInGraphPosition(port, graphView) + delta;
                }
                var chosenResult = GetClosestSnapToPortResult(graphView);

                chosenResult?.Apply(ref snapDirection, ref snappedPosition);
            }

            return snappedPosition;
        }

        public override void EndSnap()
        {
            base.EndSnap();

            m_ConnectedWires.Clear();
            m_ConnectedPortsPos.Clear();
        }

        static Dictionary<Port, Vector2> GetConnectedPortPositions(GraphView graphView, List<WireModel> wires)
        {
            Dictionary<Port, Vector2> connectedPortsOriginalPos = new Dictionary<Port, Vector2>();
            foreach (var wireModel in wires)
            {
                Port inputPort = wireModel.ToPort.GetView<Port>(graphView);
                if (inputPort != null && !connectedPortsOriginalPos.ContainsKey(inputPort))
                {
                    connectedPortsOriginalPos.Add(inputPort, GetPortCenterInGraphPosition(inputPort, graphView));
                }

                Port outputPort = wireModel.FromPort.GetView<Port>(graphView);
                if (outputPort != null && !connectedPortsOriginalPos.ContainsKey(outputPort))
                {
                    connectedPortsOriginalPos.Add(outputPort, GetPortCenterInGraphPosition(outputPort, graphView));
                }
            }

            return connectedPortsOriginalPos;
        }

        static Vector2 GetPortCenterInGraphPosition(Port port, GraphView graphView)
        {
            var gvPos = new Vector2(graphView.ViewTransform.position.x, graphView.ViewTransform.position.y);
            var gvScale = graphView.ViewTransform.scale.x;

            var connector = port.GetConnector();
            var localCenter = connector.layout.size * .5f;
            return connector.ChangeCoordinatesTo(graphView.contentContainer, localCenter - gvPos) / gvScale;
        }

        SnapToPortResult GetClosestSnapToPortResult(GraphView graphView)
        {
            var results = GetSnapToPortResults(graphView);

            float smallestDraggedDistanceFromNode = float.MaxValue;
            SnapToPortResult closestResult = null;
            foreach (var result in results)
            {
                var distanceFromPortToSnap = result.Distance;
                var isSnapping = IsSnappingToPort(distanceFromPortToSnap);

                if (isSnapping && smallestDraggedDistanceFromNode > distanceFromPortToSnap)
                {
                    smallestDraggedDistanceFromNode = distanceFromPortToSnap;
                    closestResult = result;
                }
            }

            return closestResult;
        }

        IEnumerable<SnapToPortResult> GetSnapToPortResults(GraphView graphView)
        {
            return m_ConnectedWires.Select(e => GetSnapToPortResult(graphView, e)).Where(result => result != null);
        }

        SnapToPortResult GetSnapToPortResult(GraphView graphView, WireModel wire)
        {
            var fromPort = wire.FromPort.GetView<Port>(graphView);
            var toPort = wire.ToPort.GetView<Port>(graphView);

            // We don't want to snap non existing ports and ports with different orientations (to be determined)
            if (fromPort == null || toPort == null || wire.FromPort.Orientation !=wire.ToPort.Orientation)
            {
                return null;
            }

            var orientation = wire.FromPort.Orientation;
            var sourcePort = m_SelectedNodeModel == wire.FromPort.NodeModel ? fromPort : toPort;
            var targetPort = m_SelectedNodeModel == wire.FromPort.NodeModel ? toPort : fromPort;

            var portDelta = m_ConnectedPortsPos[targetPort] - m_ConnectedPortsPos[sourcePort];

            return new SnapToPortResult
            {
                PortOrientation = orientation,
                Offset = orientation == PortOrientation.Horizontal ? portDelta.y : portDelta.x
            };
        }

        bool IsSnappingToPort(float distanceFromSnapPoint) => distanceFromSnapPoint <= SnapDistance;
    }
}
