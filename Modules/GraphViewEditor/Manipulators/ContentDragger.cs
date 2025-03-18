// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    // drags the contentContainer of a graphview around
    // add to the GraphView
    public class ContentDragger : MouseManipulator
    {
        private Vector2 m_Start;
        public Vector2 panSpeed { get; set; }

        public bool clampToParentEdges { get; set; }

        bool m_Active;

        public ContentDragger()
        {
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt});
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.MiddleMouse});
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;
        }

        protected Rect CalculatePosition(float x, float y, float width, float height)
        {
            var rect = new Rect(x, y, width, height);

            if (clampToParentEdges)
            {
                Rect shadowRect = target.hierarchy.parent.rect;
                if (rect.x < shadowRect.xMin)
                    rect.x = shadowRect.xMin;
                else if (rect.xMax > shadowRect.xMax)
                    rect.x = shadowRect.xMax - rect.width;

                if (rect.y < shadowRect.yMin)
                    rect.y = shadowRect.yMin;
                else if (rect.yMax > shadowRect.yMax)
                    rect.y = shadowRect.yMax - rect.height;

                // Reset size, we never intended to change them in the first place
                rect.width = width;
                rect.height = height;
            }

            return rect;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        void OnCaptureOut(MouseCaptureOutEvent e)
        {
            FinishDrag(e);
        }

        protected void OnMouseDown(MouseDownEvent e)
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

            // SGB-549: Prevent stealing capture from a child element. This shouldn't be necessary if children
            // elements call StopPropagation when they capture the mouse, but we can't be sure of that and thus
            // we are being a bit overprotective here.
            if (target.panel?.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            m_Start = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, e.localMousePosition);

            m_Active = true;
            target.CaptureMouse();
            e.StopImmediatePropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            // Stop the drag even if we miss the mouse up event. Note that it will not work on MacOS if right click is pressed because of UUM-97875.
            if ((e.pressedButtons & 1 << ((int)MouseButton.MiddleMouse)) == 0 && ((e.pressedButtons & 1 << ((int)MouseButton.LeftMouse)) == 0 || !e.modifiers.HasFlag(EventModifiers.Alt)))
            {
                FinishDrag(e);
                return;
            }

            Vector2 diff = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, e.localMousePosition) - m_Start;

            // During the drag update only the view
            Vector3 s = graphView.contentViewContainer.transform.scale;
            graphView.viewTransform.position += Vector3.Scale(diff, s);

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            FinishDrag(e);
        }

        void FinishDrag(EventBase e)
        {
            var graphView = target as GraphView;
            if (graphView == null)
                return;

            Vector3 p = graphView.contentViewContainer.transform.position;
            Vector3 s = graphView.contentViewContainer.transform.scale;

            graphView.UpdateViewTransform(p, s);

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}
