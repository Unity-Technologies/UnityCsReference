// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class TimeDragManipulator : PointerManipulator
    {
        public Action<DiscreteTime> StartDrag;
        public Action<DiscreteTime> SetTime;
        public Action<DiscreteTime> EndDrag;

        bool m_Active;
        readonly ICanvas m_Canvas;

        public TimeDragManipulator(ICanvas canvas)
        {
            m_Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
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

        void OnPointerDown(PointerDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                m_Active = true;

                target.CaptureMouse();

                DiscreteTime time = m_Canvas.WorldPixelToTime(e.position.x);

                if (time > DiscreteTime.Zero)
                {
                    StartDrag?.Invoke(time);
                    SetTime?.Invoke(time);
                }

                e.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (m_Active)
            {
                DiscreteTime time = m_Canvas.WorldPixelToTime(e.position.x);

                if (time < DiscreteTime.Zero)
                    time = DiscreteTime.Zero;

                SetTime?.Invoke(time);

                e.StopPropagation();
            }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (m_Active)
            {
                if (CanStopManipulation(e))
                {
                    m_Active = false;
                    target.ReleaseMouse();
                    e.StopPropagation();
                    EndDrag?.Invoke(m_Canvas.WorldPixelToTime(e.position.x));
                }
            }
        }
    }
}
