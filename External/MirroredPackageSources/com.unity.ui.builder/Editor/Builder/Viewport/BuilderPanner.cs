using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderPanner : MouseManipulator
    {
        private BuilderViewport m_Viewport;
        private bool m_Panning;
        private int m_ActivatingButton;

        public BuilderPanner(BuilderViewport viewport)
        {
            m_Viewport = viewport;
            m_Viewport.Q("viewport").AddManipulator(this);
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt | EventModifiers.Control });
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

        void OnMouseDown(MouseDownEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                m_Panning = true;
                m_ActivatingButton = evt.button;
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (m_ActivatingButton != evt.button || !CanStopManipulation(evt))
                return;

            m_Panning = false;
            target.ReleaseMouse();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Panning)
                return;

            m_Viewport.contentOffset += evt.mouseDelta;
            evt.StopPropagation();
        }
    }
}
