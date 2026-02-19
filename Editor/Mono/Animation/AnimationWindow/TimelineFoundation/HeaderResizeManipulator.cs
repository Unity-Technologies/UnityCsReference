// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    class HeaderResizeManipulator : PointerManipulator
    {
        public event Action<float> OnDrag;

        bool m_Active;

        public HeaderResizeManipulator(VisualElement anchor)
        {
            target = anchor;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<DragExitedEvent>(ReleaseCapture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<DragExitedEvent>(ReleaseCapture);
        }

        void ReleaseCapture(DragExitedEvent evt)
        {
            target.ReleaseMouse();
            m_Active = false;
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            m_Active = false;
            target.ReleaseMouse();
            evt.StopImmediatePropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (m_Active)
            {
                OnDrag?.Invoke(evt.deltaPosition.x);
                evt.StopImmediatePropagation();
            }
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            m_Active = true;
            target.CaptureMouse();
            evt.StopImmediatePropagation();
        }
    }
}
