// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class ZoomManipulator : PointerManipulator
    {
        const float k_MinZoomLevel = 0.1f;
        const float k_MaxZoomLevel = 3.0f;
        const float k_ZoomStep = 0.05f;

        bool m_Active;
        Vector2 m_ZoomCenter;

        public ZoomManipulator()
        {
            activators.Add(new ManipulatorActivationFilter
            { button = MouseButton.RightMouse, modifiers = EventModifiers.Alt });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<WheelEvent>(OnScroll, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(OnScroll, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        protected virtual Rect GetWorldRect() => target.worldBound;

        void OnScroll(WheelEvent e)
        {
            Rect rect = GetWorldRect();
            if (rect.Contains(e.mousePosition))
            {
                float zoomScale = 1f + e.delta.y * k_ZoomStep;
                Zoom(e.mousePosition, zoomScale, rect);
                e.StopPropagation();
            }
        }

        void OnPointerDown(PointerDownEvent e)
        {
            Rect rect = GetWorldRect();
            if (CanStartManipulation(e) && rect.Contains(e.position))
            {
                m_ZoomCenter = e.position;
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (m_Active)
            {
                Vector2 diff = e.deltaPosition;
                float zoomScale = 1f + (diff.x + diff.y) * k_ZoomStep;
                Rect worldRect = GetWorldRect();
                Zoom(m_ZoomCenter, zoomScale, worldRect);
                e.StopPropagation();
            }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (m_Active && CanStopManipulation(e))
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        void Zoom(Vector2 worldCenterPosition, float zoomScale, Rect worldRect)
        {
            // Limit scale
            zoomScale = Mathf.Clamp(zoomScale, k_MinZoomLevel, k_MaxZoomLevel);
            Vector2 normalizedPoint = Rect.PointToNormalized(worldRect, worldCenterPosition);

            ZoomEvent.Send(target, normalizedPoint.x, zoomScale);
        }
    }
}
