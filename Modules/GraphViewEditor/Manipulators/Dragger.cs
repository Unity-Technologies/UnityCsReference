// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
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
                Rect shadowRect = target.shadow.parent.rect;
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
                    var targetScale = ce.transform.scale;
                    diff.x *= targetScale.x;
                    diff.y *= targetScale.y;
                }

                Rect rect = CalculatePosition(target.layout.x + diff.x, target.layout.y + diff.y, target.layout.width, target.layout.height);

                if (target.style.positionType == PositionType.Manual)
                {
                    target.layout = rect;
                }
                else if (target.style.positionType == PositionType.Absolute)
                {
                    target.style.positionLeft = rect.x;
                    target.style.positionTop = rect.y;
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
