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
    /// Manipulator used to draw a wire from one port to the other.
    /// </summary>
    class WireConnector : MouseManipulator
    {
        /// <summary>
        /// The wire helper for this connector.
        /// </summary>
        /// <remarks>Internally settable for tests.</remarks>
        public WireDragHelper WireDragHelper { get; private set; }

        internal const float connectionDistanceThreshold_Internal = 10f;

        bool m_Active;
        Vector2 m_MouseDownPosition;

        public WireConnector(GraphView graphView, Func<GraphModel, GhostWireModel> ghostWireViewModelCreator = null)
        {
            WireDragHelper = new WireDragHelper(graphView, ghostWireViewModelCreator);
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        internal void SetWireDragHelper_Internal(WireDragHelper helper)
        {
            WireDragHelper = helper;
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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

            WireDragHelper.CreateWireCandidate(port.PortModel.GraphModel);
            WireDragHelper.draggedPort = port.PortModel;

            if (WireDragHelper.HandleMouseDown(e))
            {
                // Disable all wires except the dragged one.
                WireDragHelper.EnableAllWires_Internal(WireDragHelper.GraphView, false, new List<WireModel> { WireDragHelper.WireCandidateModel });
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
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
                    WireDragHelper.HandleMouseUp(e, true, Enumerable.Empty<Wire>(), Enumerable.Empty<PortModel>());
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
            return Vector2.Distance(m_MouseDownPosition, mousePosition) > connectionDistanceThreshold_Internal;
        }

        void StopManipulation(bool abort)
        {
            if (!m_Active)
                return;

            if (abort)
                Abort();

            m_Active = false;
            target.ReleaseMouse();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopManipulation(true);
        }
    }
}
