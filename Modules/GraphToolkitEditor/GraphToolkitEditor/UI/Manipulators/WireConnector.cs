// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manipulator used to draw a wire from one port to the other.
    /// </summary>
    [UnityRestricted]
    internal class WireConnector : MouseManipulator
    {
        /// <summary>
        /// The wire helper for this connector.
        /// </summary>
        /// <remarks>Internally settable for tests.</remarks>
        public WireDragHelper WireDragHelper { get; internal set; }

        internal const float k_WireCreationDistanceThreshold = WireUtilities.WireCreationDistanceThreshold;

        bool m_Active;
        Vector2 m_MouseDownPosition;
        GraphElement m_NodeUI;

        /// <summary>
        /// Initializes a new instance of the <see cref="WireConnector"/> class.
        /// </summary>
        public WireConnector(GraphView graphView)
        {
            WireDragHelper = new WireDragHelper(graphView);
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
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

            var port = target.GetFirstAncestorOfType<Port>();
            if (port == null || port.PortModel.Capacity == PortCapacity.None)
            {
                return;
            }

            // Check if the mouse is over the port hit box.
            if (!Port.GetPortHitBoxBounds(port, true).Contains(e.mousePosition))
            {
                return;
            }

            m_MouseDownPosition = e.localMousePosition;

            WireDragHelper.CreateWireCandidate();
            WireDragHelper.DraggedPort = port.PortModel;

            if (WireDragHelper.HandleMouseDown(e))
            {
                // Disable all wires except the dragged one.
                WireDragHelper.EnableAllWires(WireDragHelper.GraphView, false, new List<WireModel> { WireDragHelper.WireCandidateModel });
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
                // We need to prevent the node on which the port is from being culled because it would detach the port and loose the mouse capture.
                m_NodeUI = port.PortModel.NodeModel.GetView<GraphElement>(WireDragHelper.GraphView);
                if (m_NodeUI != null)
                    m_NodeUI.PreventCulling = true;
            }
            else
            {
                WireDragHelper.Reset();
            }
        }

        void OnCaptureOut(MouseCaptureOutEvent e)
        {
            StopManipulation(true);
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active) return;
            WireDragHelper.HandleMouseMove(e);
            e.StopPropagation();
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            if (!CanStopManipulation(e))
            {
                // The right mouse button was pressed, we stop dragging the wire.
                StopManipulation(true);
                e.StopPropagation();
                return;
            }

            try
            {
                if (CanPerformConnection(e.localMousePosition))
                    WireDragHelper.HandleMouseUp(e, true, Array.Empty<Wire>(), Array.Empty<PortModel>());
                else
                    Abort();
            }
            finally
            {
                StopManipulation(false);
                e.StopPropagation();
            }
        }

        void OnKeyUp(KeyUpEvent e)
        {
            if (e.keyCode != KeyCode.Escape && e.keyCode != KeyCode.Menu || !m_Active)
                return;

            StopManipulation(true);
            e.StopPropagation();
        }

        void Abort()
        {
            WireDragHelper.Reset();
        }

        bool CanPerformConnection(Vector2 mousePosition)
        {
            return Vector2.Distance(m_MouseDownPosition, mousePosition) > k_WireCreationDistanceThreshold;
        }

        void StopManipulation(bool abort)
        {
            if (m_NodeUI != null)
            {
                m_NodeUI.PreventCulling = false;
                m_NodeUI = null;
            }
            if (!m_Active)
                return;

            if (abort)
                Abort();

            m_Active = false;
            target.ReleaseMouse();
        }
    }
}
