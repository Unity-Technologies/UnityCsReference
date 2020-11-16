using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderZoomer : MouseManipulator
    {
        public static readonly float DefaultScale = 1;
        public static readonly float ZoomStepDistance = 10;

        public List<float> zoomScaleValues { get; set; } = new List<float>() { 0.25f, 0.5f, 0.75f, DefaultScale, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 4f, 5f };

        private bool m_Zooming = false;
        private Vector2 m_PressPos;
        private Vector2 m_LastZoomPos;
        private BuilderViewport m_Viewport;

        public BuilderZoomer(BuilderViewport viewport)
        {
            m_Viewport = viewport;
            m_Viewport.Q("viewport").AddManipulator(this);
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Alt});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        private static float CalculateNewZoom(float currentZoom, float wheelDelta, List<float> zoomValues)
        {
            currentZoom = Mathf.Clamp(currentZoom, zoomValues[0], zoomValues[zoomValues.Count - 1]);

            if (Mathf.Approximately(wheelDelta, 0))
            {
                return currentZoom;
            }

            var currentZoomIndex = zoomValues.IndexOf(currentZoom);

            if (currentZoomIndex == -1)
            {
                return DefaultScale;
            }
            else
            {
                currentZoomIndex =
                    Mathf.Clamp(currentZoomIndex + ((wheelDelta > 0) ? 1 : -1), 0, zoomValues.Count - 1);
                return zoomValues[currentZoomIndex];
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                m_Zooming = true;
                m_PressPos = evt.localMousePosition;
                m_LastZoomPos = m_PressPos;
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_Zooming && !CanStopManipulation(evt))
                return;

            m_Zooming = false;
            target.ReleaseMouse();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Zooming || Mathf.Abs(evt.localMousePosition.x - m_LastZoomPos.x) < ZoomStepDistance)
                return;

            Zoom(evt.mouseDelta.x, m_PressPos);
            m_LastZoomPos = evt.localMousePosition;
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
