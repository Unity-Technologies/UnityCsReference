// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Manipulator used to modify wires.
    /// </summary>
    class WireManipulator : MouseManipulator
    {
        bool m_Active;
        Wire m_Wire;
        Vector2 m_PressPos;
        WireDragHelper m_ConnectedWireDragHelper;
        List<WireDragHelper> m_AdditionalWireDragHelpers;
        PortModel m_DetachedPort;
        bool m_DetachedFromInputPort;
        static int s_StartDragDistance = 10;
        MouseDownEvent m_LastMouseDownEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="WireManipulator"/> class.
        /// </summary>
        public WireManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            Reset();
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate, TrickleDown.TrickleDown);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
        }

        void Reset()
        {
            m_Active = false;
            m_Wire = null;
            m_ConnectedWireDragHelper = null;
            m_AdditionalWireDragHelpers = null;
            m_DetachedPort = null;
            m_DetachedFromInputPort = false;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(evt))
            {
                return;
            }

            m_Wire = (evt.target as VisualElement)?.GetFirstOfType<Wire>();
            if (m_Wire != null && m_Wire.WireModel is IPlaceholder)
                return;

            m_PressPos = evt.mousePosition;
            target.CaptureMouse();
            evt.StopPropagation();
            m_LastMouseDownEvent = evt;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            // If the left mouse button is not down, check if the mouse is hovering near the nodes
            if (m_Wire == null)
            {
                var wire = (evt.target as VisualElement)?.GetFirstOfType<Wire>();
                if (wire == null)
                    return;

                var view = wire.RootView;
                var outputPortUI = wire.Output.GetView<Port>(view);
                var inputPortUI = wire.Input.GetView<Port>(view);

                if (inputPortUI == null || outputPortUI == null)
                    return;

                var graphView = (evt.target as VisualElement)?.GetFirstOfType<GraphView>();
                if (graphView == null)
                    return;

                ContentDragger.ChangeMouseCursorTo_Internal(graphView.elementPanel, MouseIsNearPorts(inputPortUI, outputPortUI, evt.mousePosition, out _, out _) ? (int)MouseCursor.Link : (int)MouseCursor.Arrow);
                evt.StopPropagation();

                return;
            }

            evt.StopPropagation();

            var alreadyDetached = (m_DetachedPort != null);

            // If one end of the wire is not already detached then
            if (!alreadyDetached)
            {
                var delta = (evt.mousePosition - m_PressPos).sqrMagnitude;

                if (delta < (s_StartDragDistance * s_StartDragDistance))
                {
                    return;
                }

                var view = m_Wire.RootView;
                var outputPortUI = m_Wire.Output.GetView<Port>(view);
                var inputPortUI = m_Wire.Input.GetView<Port>(view);

                if (inputPortUI == null || outputPortUI == null)
                    return;

                if (!MouseIsNearPorts(inputPortUI, outputPortUI, m_PressPos, out var distanceFromInput, out var distanceFromOutput))
                    return;

                m_DetachedFromInputPort = distanceFromInput < distanceFromOutput;

                PortModel connectedPort;
                Port connectedPortUI;

                if (m_DetachedFromInputPort)
                {
                    connectedPort = m_Wire.Output;
                    connectedPortUI = outputPortUI;

                    m_DetachedPort = m_Wire.Input;
                }
                else
                {
                    connectedPort = m_Wire.Input;
                    connectedPortUI = inputPortUI;

                    m_DetachedPort = m_Wire.Output;
                }

                // Use the wire drag helper of the still connected port
                m_ConnectedWireDragHelper = connectedPortUI.WireConnector.WireDragHelper;
                m_ConnectedWireDragHelper.OriginalWire = m_Wire;
                m_ConnectedWireDragHelper.draggedPort = connectedPort;
                m_ConnectedWireDragHelper.CreateWireCandidate(connectedPort.GraphModel);
                m_ConnectedWireDragHelper.WireCandidateModel.EndPoint = evt.mousePosition;

                // Redirect the last mouse down event to active the drag helper
                var needsSetup = true;
                if (m_ConnectedWireDragHelper.HandleMouseDown(m_LastMouseDownEvent, compatiblePort =>
                    {
                        // We do the setup once here as to avoid getting the additional draggers if HandleMouseDown fails.
                        if (needsSetup)
                        {
                            m_AdditionalWireDragHelpers = GetAdditionalWireDragHelpers(m_DetachedPort, m_Wire, view, connectedPort.GraphModel, evt);
                            needsSetup = false;
                        }
                        return FilterCompatiblePort(compatiblePort);
                    }))
                {
                    m_Active = true;

                    if (m_AdditionalWireDragHelpers != null)
                    {
                        foreach (var wireDrag in m_AdditionalWireDragHelpers)
                        {
                            wireDrag.HandleMouseDown(m_LastMouseDownEvent, compatiblePort => FilterCompatiblePort(compatiblePort));
                        }
                    }

                    var draggedWires = new List<WireModel> { m_ConnectedWireDragHelper.OriginalWire.WireModel };
                    if (m_AdditionalWireDragHelpers != null)
                        draggedWires.AddRange(m_AdditionalWireDragHelpers.Select(wireDrag => wireDrag.OriginalWire.WireModel));

                    // Disable all wires except the dragged ones.
                    WireDragHelper.EnableAllWires_Internal(m_ConnectedWireDragHelper.GraphView, false, draggedWires);
                }
                else
                {
                    Reset();
                }

                m_LastMouseDownEvent = null;
            }

            if (m_Active)
            {
                m_ConnectedWireDragHelper.HandleMouseMove(evt);
                if (m_AdditionalWireDragHelpers != null)
                {
                    foreach (var dragHelper in m_AdditionalWireDragHelpers)
                        dragHelper.HandleMouseMove(evt);
                }
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                target.ReleaseMouse();
                if (m_Active)
                {
                    if (m_AdditionalWireDragHelpers != null)
                    {
                        m_ConnectedWireDragHelper.HandleMouseUp(evt, true, m_AdditionalWireDragHelpers.Select(t => t.OriginalWire), m_AdditionalWireDragHelpers.Select(t => t.draggedPort));
                        foreach (var dragHelper in m_AdditionalWireDragHelpers)
                            dragHelper.HandleMouseUp(evt, false, Enumerable.Empty<Wire>(), Enumerable.Empty<PortModel>());
                    }
                    else
                    {
                        m_ConnectedWireDragHelper.HandleMouseUp(evt, true, Enumerable.Empty<Wire>(), Enumerable.Empty<PortModel>());
                    }

                    evt.StopPropagation();
                }
                Reset();
            }
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    StopDragging();
                    evt.StopPropagation();
                }
            }
        }

        void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (m_Active)
            {
                StopDragging();
                evt.StopPropagation();
            }
        }

        void StopDragging()
        {
            m_ConnectedWireDragHelper.Reset();
            if (m_AdditionalWireDragHelpers != null)
            {
                foreach (var dragHelper in m_AdditionalWireDragHelpers)
                    dragHelper.Reset();
            }

            Reset();
            target.ReleaseMouse();
        }

        static bool MouseIsNearPorts(Port input, Port output, Vector2 worldPos, out float distanceFromInput, out float distanceFromOutput)
        {
            distanceFromInput = float.NegativeInfinity;
            distanceFromOutput = float.NegativeInfinity;

            var reference = input.GraphView.ContentViewContainer;

            var outputPos = reference.WorldToLocal(output.GetGlobalCenter());
            var inputPos = reference.WorldToLocal(input.GetGlobalCenter());
            var referencePos = reference.WorldToLocal(worldPos);

            distanceFromOutput = (referencePos - outputPos).sqrMagnitude;
            distanceFromInput = (referencePos - inputPos).sqrMagnitude;

            return !(distanceFromInput > 50 * 50) || !(distanceFromOutput > 50 * 50);
        }

        static List<WireDragHelper> GetAdditionalWireDragHelpers(PortModel detachedPort, Wire targetWire, RootView rootView, GraphModel graphModel, IMouseEvent evt)
        {
            var connectedWires = detachedPort.GetConnectedWires().ToList();
            if (connectedWires.Count == 0)
                return null;

            var additionalWireDragHelpers = new List<WireDragHelper>();

            foreach (var wire in connectedWires)
            {
                var wireUI = wire.GetView<Wire>(rootView);
                if (wireUI != null && wireUI != targetWire && wireUI.IsSelected())
                {
                    var otherPort = detachedPort == wire.ToPort ? wire.FromPort : wire.ToPort;

                    var wireDragHelper = otherPort.GetView<Port>(rootView)?.WireConnector.WireDragHelper;

                    if (wireDragHelper != null)
                    {
                        wireDragHelper.OriginalWire = wireUI;
                        wireDragHelper.draggedPort = otherPort;
                        wireDragHelper.CreateWireCandidate(graphModel);
                        wireDragHelper.WireCandidateModel.EndPoint = evt.mousePosition;

                        additionalWireDragHelpers.Add(wireDragHelper);
                    }
                }
            }

            return additionalWireDragHelpers;
        }

        bool FilterCompatiblePort(PortModel compatiblePort)
        {
            if (m_AdditionalWireDragHelpers?.Count > 0)
                return compatiblePort.Capacity == PortCapacity.Multi;
            return true;
        }
    }
}
