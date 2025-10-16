// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Changes the <see cref="GraphView"/> offset when the mouse is clicked and dragged in its background.
    /// </summary>
    [UnityRestricted]
    internal class ContentDragger : PointerManipulator
    {
        Vector2 m_Start;
        public Vector2 panSpeed { get; set; }

        public bool ClampToParentWires { get; set; }

        bool m_Active;
        bool m_DidDrag;
        int m_MouseButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentDragger"/> class.
        /// </summary>
        public ContentDragger()
        {
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
            if (! VisualElementBridge.IsOSXContextualMenuPlatform)
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            panSpeed = new Vector2(1, 1);
            ClampToParentWires = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<PointerDownEvent>(OnMouseDown);
            target.RegisterCallback<PointerUpEvent>(OnMouseUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnMouseDown);
            target.UnregisterCallback<PointerUpEvent>(OnMouseUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected void OnMouseDown(PointerDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            m_Active = true;
            m_DidDrag = false;
            m_MouseButton = e.button;

            graphView.RegisterCallback<ContextualMenuPopulateEvent>( OnContextualMenuPopulate, TrickleDown.TrickleDown);

            m_Start = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localPosition);

            EditorGUIUtilityBridge.SetCursor(MouseCursor.Pan);
            target.RegisterCallback<PointerMoveEvent>(OnMouseMove);

            e.StopImmediatePropagation();
        }

        protected void OnMouseMove(PointerMoveEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if ((e.pressedButtons & (1 << m_MouseButton)) == 0)
            {
                StopManipulation();
            }

            if (!target.HasPointerCapture(e.pointerId))
                target.CapturePointer(e.pointerId);

            var diff = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localPosition) - m_Start;

            const float kDragThreshold = 8;

            // We want to apply a threshold to the drag in the case of a right click to keep the right click menu in case of a very small ( potentially unwanted ) pan.
            if (m_DidDrag || m_MouseButton != (int)MouseButton.RightMouse || diff.sqrMagnitude > kDragThreshold*kDragThreshold)
            {
                m_DidDrag = true;

                // During the drag update only the view
                var scale = graphView.ContentViewContainer.resolvedStyle.scale.value;
                var position = graphView.ContentViewContainer.resolvedStyle.translate + Vector3.Scale(diff, scale);
                graphView.UpdateViewTransform(position, scale);

                EditorGUIUtilityBridge.SetCursor(MouseCursor.Pan);

                e.StopPropagation();
            }
        }

        protected void OnMouseUp(PointerUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            StopManipulation();
            if (m_DidDrag)
            {
                //If we did drag, we stop the propagation to cancel the right click menu.
                e.StopImmediatePropagation();
            }
        }

        protected void OnMouseCaptureOutEvent(PointerCaptureOutEvent evt)
        {
            if (!m_Active)
                return;
            StopManipulation();
        }

        void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (!m_Active)
                return;
            StopManipulation();
        }

        internal void StopManipulation()
        {
            var graphView = target as GraphView;
            if (graphView == null)
                return;

            graphView.UnregisterCallback<ContextualMenuPopulateEvent>( OnContextualMenuPopulate, TrickleDown.TrickleDown);

            var position = graphView.ContentViewContainer.resolvedStyle.translate;
            var scale = graphView.ContentViewContainer.resolvedStyle.scale.value;
            graphView.Dispatch(new ReframeGraphViewCommand(position, scale));

            if (m_Active)
            {
                m_Active = false;
                target.ReleaseMouse();

                EditorGUIUtilityBridge.SetCursor(MouseCursor.Arrow);
                target.UnregisterCallback<PointerMoveEvent>(OnMouseMove);
            }
        }
    }
}
