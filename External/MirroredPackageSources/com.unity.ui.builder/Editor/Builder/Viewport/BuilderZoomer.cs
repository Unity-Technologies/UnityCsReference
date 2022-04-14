using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderZoomer : PointerManipulator
    {
        private const int MinScaleStart = 25;           // This is the minimum scale (25%), increment in small steps
        private const int MaxScaleStart = 150;          // At 150%, let's increment in larger steps
        private const int MaxScaleEnd = 500;            // until we reach the max scale (500%)
        private const int LowScaleIncrement = 5;        // Low scale increment step (5%)
        private const int HighScaleIncrement = 25;      // High scale increment step (25%)
        private List<float> m_ZoomScaleValues;
        private const float DefaultScale = 1;
        private const float ZoomStepDistance = 10;

        // Full zoom scale list available with the Zoomer manipulator
        public List<float> zoomScaleValues
        {
            get
            {
                if (m_ZoomScaleValues == null)
                {
                    m_ZoomScaleValues = new List<float>();
                    for (var val = MinScaleStart; val < MaxScaleStart; val += LowScaleIncrement)
                        m_ZoomScaleValues.Add((float)Math.Round(val/100.0, 2));
                    for (var val = MaxScaleStart; val <= MaxScaleEnd; val += HighScaleIncrement)
                        m_ZoomScaleValues.Add((float)Math.Round(val/100.0, 2));
                }
                return m_ZoomScaleValues;
            }
        }

        // Short list of zoom scales for the Zoom menu
        public List<float> zoomMenuScaleValues { get; } = new() { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 4f, 5f };

        bool m_Zooming;
        Vector2 m_PressPos;
        Vector2 m_LastZoomPos;
        readonly BuilderViewport m_Viewport;

        public BuilderZoomer(BuilderViewport viewport)
        {
            m_Viewport = viewport;
            m_Viewport.Q("viewport").AddManipulator(this);
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Alt});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        private static float CalculateNewZoom(float currentZoom, float wheelDelta, List<float> zoomValues)
        {
            currentZoom = Mathf.Clamp(currentZoom, zoomValues[0], zoomValues[^1]);

            if (Mathf.Approximately(wheelDelta, 0))
            {
                return currentZoom;
            }

            var currentZoomIndex = zoomValues.IndexOf(currentZoom);

            if (currentZoomIndex == -1)
            {
                return DefaultScale;
            }

            currentZoomIndex =
                Mathf.Clamp(currentZoomIndex + ((wheelDelta > 0) ? 1 : -1), 0, zoomValues.Count - 1);
            return zoomValues[currentZoomIndex];
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                m_Zooming = true;
                m_PressPos = evt.localPosition;
                m_LastZoomPos = m_PressPos;
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!m_Zooming || !CanStopManipulation(evt))
                return;

            m_Zooming = false;
            target.ReleaseMouse();
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_Zooming || Mathf.Abs(evt.localPosition.y - m_LastZoomPos.y) < ZoomStepDistance)
                return;

            Zoom(evt.deltaPosition.y, m_PressPos);
            m_LastZoomPos = evt.localPosition;
            evt.StopPropagation();
        }

        void OnWheel(WheelEvent evt)
        {
            if (MouseCaptureController.IsMouseCaptured())
                return;

            Zoom(-evt.delta.y, evt.localMousePosition);
            evt.StopPropagation();
        }

        void Zoom(float delta, Vector2 zoomCenter)
        {
            if (BuilderProjectSettings.disableMouseWheelZooming)
                return;

            var oldScale = m_Viewport.zoomScale;

            m_Viewport.zoomScale = CalculateNewZoom(m_Viewport.zoomScale, delta, zoomScaleValues);
            m_Viewport.contentOffset = zoomCenter + (m_Viewport.zoomScale / oldScale) * (m_Viewport.contentOffset - zoomCenter);
        }
    }
}
