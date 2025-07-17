// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    enum DragDirection
    {
        Horizontal,
        Vertical,
        All
    }

    class DragManipulator : MouseManipulator
    {
        public event Action<(Vector2 total, Vector2 delta)> translated;
        public event Action translationBegun;
        public event Action translationEnded;
        bool m_Active;
        Vector2 m_StartPosition;
        Vector2 m_PreviousTranslation;
        DragDirection m_Direction;

        public DragManipulator(DragDirection direction)
        {
            m_Direction = direction;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
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
            if (m_Active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            // Overprotecting against children not stopping propagation in user code
            if (target.panel?.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            if (CanStartManipulation(evt))
            {
                m_StartPosition = evt.mousePosition;
                m_PreviousTranslation = Vector2.zero;

                m_Active = true;
                target.CaptureMouse();
                evt.StopPropagation();
                translationBegun?.Invoke();
            }
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_Active)
            {
                var translation = GetTranslation(evt.mousePosition);
                translated?.Invoke((translation, translation - m_PreviousTranslation));
                m_PreviousTranslation = translation;
                evt.StopPropagation();
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (m_Active && CanStopManipulation(evt))
            {
                m_Active = false;
                target.ReleaseMouse();
                evt.StopPropagation();
                translationEnded?.Invoke();
            }
        }

        Vector2 GetTranslation(Vector2 mousePosition)
        {
            var translation = Vector2.zero;
            switch (m_Direction)
            {
                case DragDirection.Vertical:
                    translation = new Vector2(0, mousePosition.y - m_StartPosition.y);
                    break;

                case DragDirection.Horizontal:
                    translation = new Vector2(mousePosition.x - m_StartPosition.x, 0);
                    break;

                case DragDirection.All:
                    translation = mousePosition - m_StartPosition;
                    break;
            }

            return translation;
        }
    }
}
