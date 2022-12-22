// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderPanner : PointerManipulator
    {
        private readonly BuilderViewport m_Viewport;
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
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                m_Panning = true;
                m_ActivatingButton = evt.button;
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (m_ActivatingButton != evt.button || !CanStopManipulation(evt))
                return;

            m_Panning = false;
            target.ReleaseMouse();
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_Panning)
                return;

            Vector2 deltaPosition = evt.deltaPosition;
            m_Viewport.contentOffset += deltaPosition;
            evt.StopPropagation();
        }
    }
}
