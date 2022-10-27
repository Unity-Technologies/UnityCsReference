// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
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

            m_MouseDownPosition = e.localMousePosition;

            WireDragHelper.CreateWireCandidate(port.PortModel.GraphModel);
            WireDragHelper.draggedPort = port.PortModel;

            if (WireDragHelper.HandleMouseDown(e))
            {
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
            m_Active = false;
            if (WireDragHelper.WireCandidateModel != null)
                Abort();
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active) return;
            WireDragHelper.HandleMouseMove(e);
            e.StopPropagation();
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            try
            {
                if (CanPerformConnection(e.localMousePosition))
                    WireDragHelper.HandleMouseUp(e, true, Enumerable.Empty<Wire>(), Enumerable.Empty<PortModel>());
                else
                    Abort();
            }
            finally
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !m_Active)
                return;

            Abort();

            m_Active = false;
            target.ReleaseMouse();
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
    }
}
