// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    internal class ElementResizer : MouseManipulator
    {
        private readonly ResizerDirection direction;

        private readonly VisualElement resizedElement;

        public ElementResizer(VisualElement resizedElement, ResizerDirection direction)
        {
            this.direction = direction;
            this.resizedElement = resizedElement;
            activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        Vector2 m_StartMouse;
        Vector2 m_StartSize;

        Vector2 m_MinSize;
        Vector2 m_MaxSize;

        Vector2 m_StartPosition;

        bool m_DragStarted = false;

        bool m_Active = false;

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            // SGB-549: Prevent stealing capture from a child element. This shouldn't be necessary if children
            // elements call StopPropagation when they capture the mouse, but we can't be sure of that and thus
            // we are being a bit overprotective here.
            if (target.panel?.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            m_Active = true;

            VisualElement resizedTarget = resizedElement.parent;
            if (resizedTarget == null)
                return;
            VisualElement resizedBase = resizedTarget.parent;
            if (resizedBase == null)
                return;
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            e.StopPropagation();
            target.CaptureMouse();
            m_StartMouse = resizedBase.WorldToLocal(e.mousePosition);
            m_StartSize = new Vector2(resizedTarget.resolvedStyle.width, resizedTarget.resolvedStyle.height);
            m_StartPosition = new Vector2(resizedTarget.resolvedStyle.left, resizedTarget.resolvedStyle.top);

            bool minWidthDefined = resizedTarget.resolvedStyle.minWidth != StyleKeyword.Auto;
            bool maxWidthDefined = resizedTarget.resolvedStyle.maxWidth != StyleKeyword.None;
            bool minHeightDefined = resizedTarget.resolvedStyle.minHeight != StyleKeyword.Auto;
            bool maxHeightDefined = resizedTarget.resolvedStyle.maxHeight != StyleKeyword.None;
            m_MinSize = new Vector2(
                minWidthDefined ? resizedTarget.resolvedStyle.minWidth.value : Mathf.NegativeInfinity,
                minHeightDefined ? resizedTarget.resolvedStyle.minHeight.value : Mathf.NegativeInfinity);
            m_MaxSize = new Vector2(
                maxWidthDefined ? resizedTarget.resolvedStyle.maxWidth.value : Mathf.Infinity,
                maxHeightDefined ? resizedTarget.resolvedStyle.maxHeight.value : Mathf.Infinity);

            m_DragStarted = false;
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            VisualElement resizedTarget = resizedElement.parent;
            VisualElement resizedBase = resizedTarget.parent;

            Vector2 mousePos = resizedBase.WorldToLocal(e.mousePosition);
            if (!m_DragStarted)
            {
                if (resizedTarget is IResizable)
                    (resizedTarget as IResizable).OnStartResize();
                m_DragStarted = true;
            }
            if (resizedTarget.isLayoutManual)
            {
                Rect layout = resizedTarget.layout;
                if ((direction & ResizerDirection.Right) != 0)
                {
                    layout.width = Mathf.Clamp(m_StartSize.x + mousePos.x - m_StartMouse.x, m_MinSize.x, Mathf.Min(m_MaxSize.x, resizedBase.layout.xMax - layout.xMin));
                }
                else if ((direction & ResizerDirection.Left) != 0)
                {
                    float delta = mousePos.x - m_StartMouse.x;
                    float previousLeft = layout.xMin;

                    layout.xMin = Mathf.Clamp(delta + m_StartPosition.x, 0, resizedTarget.layout.xMax - m_MinSize.x);
                    layout.width = resizedTarget.resolvedStyle.width + previousLeft - layout.xMin;
                }
                if ((direction & ResizerDirection.Bottom) != 0)
                {
                    layout.height = Mathf.Clamp(m_StartSize.y + mousePos.y - m_StartMouse.y, m_MinSize.y, resizedBase.layout.yMax - layout.yMin);
                }
                else if ((direction & ResizerDirection.Top) != 0)
                {
                    float delta = mousePos.y - m_StartMouse.y;
                    float previousTop = layout.yMin;

                    layout.yMin = Mathf.Clamp(delta + m_StartPosition.y, 0, m_StartSize.y - 1);
                    layout.height = resizedTarget.resolvedStyle.height + previousTop - layout.yMin;
                }

                if (direction != 0)
                {
                    resizedTarget.layout = layout;
                }
            }
            else
            {
                if ((direction & ResizerDirection.Right) != 0)
                {
                    resizedTarget.style.width = Mathf.Clamp(m_StartSize.x + mousePos.x - m_StartMouse.x, m_MinSize.x, Mathf.Min(m_MaxSize.x, resizedBase.layout.xMax - resizedTarget.layout.xMin));
                }
                else if ((direction & ResizerDirection.Left) != 0)
                {
                    float delta = mousePos.x - m_StartMouse.x;
                    float previousLeft = resizedTarget.style.left.value.value;

                    resizedTarget.style.left = Mathf.Clamp(delta + m_StartPosition.x, 0, resizedTarget.layout.xMax - m_MinSize.x);
                    resizedTarget.style.width = resizedTarget.resolvedStyle.width + previousLeft - resizedTarget.style.left.value.value;
                }
                if ((direction & ResizerDirection.Bottom) != 0)
                {
                    resizedTarget.style.height = Mathf.Min(m_MaxSize.y, Mathf.Max(m_MinSize.y, m_StartSize.y + mousePos.y - m_StartMouse.y));
                }
                else if ((direction & ResizerDirection.Top) != 0)
                {
                    float delta = mousePos.y - m_StartMouse.y;
                    float previousTop = resizedTarget.style.top.value.value;

                    resizedTarget.style.top = Mathf.Clamp(delta + m_StartPosition.y, 0, m_StartSize.y - 1);
                    resizedTarget.style.height = resizedTarget.resolvedStyle.height + previousTop - resizedTarget.style.top.value.value;
                }
            }
            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!CanStopManipulation(e))
                return;

            if (m_Active)
            {
                VisualElement resizedTarget = resizedElement.parent;
                if (resizedTarget.style.width != m_StartSize.x || resizedTarget.style.height != m_StartSize.y)
                {
                    if (resizedTarget is IResizable)
                        (resizedTarget as IResizable).OnResized();
                }
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.ReleaseMouse();
                e.StopPropagation();

                m_Active = false;
            }
        }
    }
}
