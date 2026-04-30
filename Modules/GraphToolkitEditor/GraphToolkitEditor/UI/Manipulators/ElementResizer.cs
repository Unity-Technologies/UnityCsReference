// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manipulator to handle interactions to resize an element.
    /// </summary>
    class ElementResizer : MouseManipulator
    {
        readonly ResizerDirection m_Direction;
        readonly VisualElement m_ResizedElement;

        readonly GraphViewPanHelper m_PanHelper = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementResizer"/> class.
        /// </summary>
        /// <param name="resizedElement">The resized element.</param>
        /// <param name="direction">The directions in which the element can be resized.</param>
        public ElementResizer(VisualElement resizedElement, ResizerDirection direction)
        {
            m_Direction = direction;
            m_ResizedElement = resizedElement;
            activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        VisualElement m_ResizedTarget;
        VisualElement m_ResizedBase;

        Vector2 m_StartMouse;
        Vector2 m_LastMousePosition;
        Vector2 m_StartSize;
        Vector2 m_StartPosition;

        Vector2 m_MinSize;
        Vector2 m_MaxSize;

        Rect m_NewRect;

        bool m_DragStarted;

        bool m_Active;

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            m_Active = true;

            m_ResizedTarget = m_ResizedElement.parent;
            if (m_ResizedTarget == null)
                return;

            m_ResizedBase = m_ResizedTarget.parent;
            if (m_ResizedBase == null)
                return;

            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            e.StopPropagation();
            target.CaptureMouse();

            m_LastMousePosition = e.mousePosition;
            m_StartMouse = m_ResizedBase.WorldToLocal(m_LastMousePosition);
            m_StartSize = new Vector2(m_ResizedTarget.resolvedStyle.width, m_ResizedTarget.resolvedStyle.height);
            m_StartPosition = new Vector2(m_ResizedTarget.resolvedStyle.left, m_ResizedTarget.resolvedStyle.top);
            m_NewRect = new Rect(m_StartPosition, m_StartSize);

            var minWidthDefined = m_ResizedTarget.resolvedStyle.minWidth != StyleKeyword.Auto;
            var maxWidthDefined = m_ResizedTarget.resolvedStyle.maxWidth != StyleKeyword.None;
            var minHeightDefined = m_ResizedTarget.resolvedStyle.minHeight != StyleKeyword.Auto;
            var maxHeightDefined = m_ResizedTarget.resolvedStyle.maxHeight != StyleKeyword.None;
            m_MinSize = new Vector2(
                minWidthDefined ? m_ResizedTarget.resolvedStyle.minWidth.value : Mathf.NegativeInfinity,
                minHeightDefined ? m_ResizedTarget.resolvedStyle.minHeight.value : Mathf.NegativeInfinity);
            m_MaxSize = new Vector2(
                maxWidthDefined ? m_ResizedTarget.resolvedStyle.maxWidth.value : Mathf.Infinity,
                maxHeightDefined ? m_ResizedTarget.resolvedStyle.maxHeight.value : Mathf.Infinity);

            if (m_ResizedTarget is GraphElement graphElement)
            {
                graphElement.PositionIsOverriddenByManipulator = true;
            }

            if (m_ResizedTarget is IResizeListener listener)
            {
                listener.OnStartResize();
            }

            var graphView = (m_ResizedTarget as GraphElement)?.GraphView;
            if (graphView != null)
                m_PanHelper.OnMouseDown(e, graphView, OnPan);

            m_DragStarted = false;
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_PanHelper.OnMouseMove(e);

            m_LastMousePosition = e.mousePosition;
            ApplyResize(m_LastMousePosition);

            e.StopPropagation();
        }

        void ApplyResize(Vector2 worldMousePosition)
        {
            if (!m_Active || m_ResizedTarget == null || m_ResizedBase == null)
                return;

            var mousePos = m_ResizedBase.WorldToLocal(worldMousePosition);
            if (!m_DragStarted)
            {
                m_DragStarted = true;
            }

            if ((m_Direction & ResizerDirection.Right) != 0)
            {
                m_NewRect.width = Mathf.Min(m_MaxSize.x, Mathf.Max(m_MinSize.x, m_StartSize.x + mousePos.x - m_StartMouse.x));

                m_ResizedTarget.style.width = m_NewRect.width;
            }
            else if ((m_Direction & ResizerDirection.Left) != 0)
            {
                var delta = mousePos.x - m_StartMouse.x;

                if (m_StartSize.x - delta < m_MinSize.x)
                    delta = -m_MinSize.x + m_StartSize.x;
                else if (m_StartSize.x - delta > m_MaxSize.x)
                    delta = -m_MaxSize.x + m_StartSize.x;

                m_NewRect.x = delta + m_StartPosition.x;
                m_NewRect.width = -delta + m_StartSize.x;

                m_ResizedTarget.style.left = m_NewRect.x;
                m_ResizedTarget.style.width = m_NewRect.width;
            }

            if ((m_Direction & ResizerDirection.Bottom) != 0)
            {
                m_NewRect.height = Mathf.Min(m_MaxSize.y, Mathf.Max(m_MinSize.y, m_StartSize.y + mousePos.y - m_StartMouse.y));

                m_ResizedTarget.style.height = m_NewRect.height;
            }
            else if ((m_Direction & ResizerDirection.Top) != 0)
            {
                var delta = mousePos.y - m_StartMouse.y;

                if (m_StartSize.y - delta < m_MinSize.y)
                    delta = -m_MinSize.y + m_StartSize.y;
                else if (m_StartSize.y - delta > m_MaxSize.y)
                    delta = -m_MaxSize.y + m_StartSize.y;

                m_NewRect.y = delta + m_StartPosition.y;
                m_NewRect.height = -delta + m_StartSize.y;

                m_ResizedTarget.style.top = m_NewRect.y;
                m_ResizedTarget.style.height = m_NewRect.height;
            }

            if (m_ResizedTarget is IResizeListener listener)
            {
                listener.OnResizing();
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!CanStopManipulation(e))
                return;

            if (m_Active)
            {
                if (m_NewRect != new Rect(m_StartPosition, m_StartSize) &&
                    m_ResizedTarget is GraphElement { Model: IResizable resizable } element &&
                    element.GraphElementModel.IsResizable())
                {
                    if (element is Placemat placemat)
                    {
                        List<(PlacematModel, PlacematModel)> placematToBringInFrontOf = null;
                        var placematModels = placemat.GraphView.GraphModel.PlacematModels;
                        var index = placematModels.IndexOf(placemat.PlacematModel);

                        for (var j = 0; j < index; ++j)
                        {
                            var subPlacemat = placematModels[j];

                            var newPlacematPosition = m_NewRect;

                            if (newPlacematPosition.Contains(subPlacemat.PositionAndSize.position) && newPlacematPosition.Contains(subPlacemat.Position + subPlacemat.PositionAndSize.size))
                            {
                                placematToBringInFrontOf ??= [];

                                placematToBringInFrontOf.Add((subPlacemat, placemat.PlacematModel));
                            }
                        }

                        element.GraphView.Dispatch(new ChangePlacematLayoutAndBringPlacematToFrontCommand(resizable, m_NewRect, placematToBringInFrontOf));
                    }
                    else
                    {
                        element.GraphView.Dispatch(new ChangeElementLayoutCommand(resizable, m_NewRect));
                    }
                }
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.ReleaseMouse();
                e.StopPropagation();

                m_PanHelper.OnMouseUp(e);
                m_Active = false;

                if (m_ResizedTarget is GraphElement graphElement)
                {
                    graphElement.PositionIsOverriddenByManipulator = false;
                }

                if (m_ResizedTarget is IResizeListener listener)
                {
                    listener.OnStopResize();
                }
                
                m_ResizedTarget = null;
                m_ResizedBase = null;
            }
        }

        void OnPan(TimerState ts)
        {
            ApplyResize(m_LastMousePosition);
        }
    }
}
