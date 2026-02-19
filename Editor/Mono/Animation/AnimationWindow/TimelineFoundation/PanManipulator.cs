// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

// Code copied from Unity\Editor\Graphs\UnityEditor.Graphs\Animation\AnimatorControllerTool.cs
// TODO - investigate making this publicly available in editor code
namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    class PanManipulator : PointerManipulator
    {
        Vector2 m_LastPosition;
        bool m_Active;

        public PanManipulator()
        {
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter
            { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            activators.Add(new ManipulatorActivationFilter
            { button = MouseButton.MiddleMouse, modifiers = EventModifiers.Alt });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        protected virtual Rect GetWorldRect() => target.worldBound;

        void Pan(Vector2 from, Vector2 to)
        {
            Rect rect = GetWorldRect();
            Vector2 diff = from - to;
            float xFactor = Mathf.Clamp(diff.x / rect.width, -1f, 1f);
            float yFactor = Mathf.Clamp(diff.y / rect.height, -1f, 1f);
            var panFactor = new Vector2(xFactor, yFactor);

            PanEvent.Send(target, panFactor);
        }

        void OnPointerDown(PointerDownEvent e)
        {
            Rect rect = GetWorldRect();
            if (CanStartManipulation(e) && rect.Contains(e.position))
            {
                m_LastPosition = e.position;
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (m_Active)
            {
                Pan(m_LastPosition, e.position);
                m_LastPosition = e.position;
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
    }
}
