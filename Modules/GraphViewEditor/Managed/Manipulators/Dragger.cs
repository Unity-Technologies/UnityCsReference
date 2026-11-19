// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class Dragger : MouseManipulator
    {
        private Vector2 m_Start;
        protected bool m_Active;

        public Vector2 panSpeed { get; set; }

        public bool clampToParentEdges { get; set; }

        public Dragger()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;
            m_Active = false;
        }

        protected Rect CalculatePosition(float x, float y, float width, float height)
        {
            var rect = new Rect(x, y, width, height);

            if (clampToParentEdges)
            {
                Rect shadowRect = target.hierarchy.parent.rect;
                rect.x = Mathf.Clamp(rect.x, shadowRect.xMin, Mathf.Abs(shadowRect.xMax - rect.width));
                rect.y = Mathf.Clamp(rect.y, shadowRect.yMin, Mathf.Abs(shadowRect.yMax - rect.height));

                // Reset size, we never intended to change them in the first place
                rect.width = width;
                rect.height = height;
            }

            return rect;
        }

        protected override void RegisterCallbacksOnTarget()
        {
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

            GraphElement ce = e.target as GraphElement;
            if (ce != null && !ce.IsMovable())
            {
                return;
            }

            // SGB-549: Prevent stealing capture from a child element. This shouldn't be necessary if children
            // elements call StopPropagation when they capture the mouse, but we can't be sure of that and thus
            // we are being a bit overprotective here.
            if (target.panel?.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            if (CanStartManipulation(e))
            {
                m_Start = e.localMousePosition;

                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            GraphElement ce = e.target as GraphElement;
            if (ce != null && !ce.IsMovable())
            {
                return;
            }

            if (m_Active)
            {
                Vector2 diff = e.localMousePosition - m_Start;

                if (ce != null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var targetScale = ce.transform.scale;
#pragma warning restore CS0618 // Type or member is obsolete
                    diff.x *= targetScale.x;
                    diff.y *= targetScale.y;
                }

                Rect rect = CalculatePosition(target.layout.x + diff.x, target.layout.y + diff.y, target.layout.width, target.layout.height);

                if (target.isLayoutManual)
                {
                    target.layout = rect;
                }
                else if (target.resolvedStyle.position == Position.Absolute)
                {
                    target.style.left = rect.x;
                    target.style.top = rect.y;
                }

                e.StopPropagation();
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            GraphElement ce = e.target as GraphElement;
            if (ce != null && !ce.IsMovable())
            {
                return;
            }

            if (m_Active)
            {
                if (CanStopManipulation(e))
                {
                    var graphElement = target as GraphElement;
                    if (graphElement != null)
                        graphElement.UpdatePresenterPosition();

                    m_Active = false;
                    target.ReleaseMouse();
                    e.StopPropagation();
                }
            }
        }
    }
}
