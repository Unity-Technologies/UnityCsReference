// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
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
                if (rect.x < target.parent.layout.xMin)
                    rect.x = target.parent.layout.xMin;
                else if (rect.xMax > target.parent.layout.xMax)
                    rect.x = target.parent.layout.xMax - rect.width;

                if (rect.y < target.parent.layout.yMin)
                    rect.y = target.parent.layout.yMin;
                else if (rect.yMax > target.parent.layout.yMax)
                    rect.y = target.parent.layout.yMax - rect.height;

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
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
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

            m_Start = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, e.localMousePosition);

            m_Active = true;
            target.TakeMouseCapture();
            e.StopPropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active || !target.HasMouseCapture())
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

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

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            Vector3 p = graphView.contentViewContainer.transform.position;
            Vector3 s = graphView.contentViewContainer.transform.scale;

            graphView.UpdateViewTransform(p, s);

            m_Active = false;
            target.ReleaseMouseCapture();
            e.StopPropagation();
        }
    }
}
