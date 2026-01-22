// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    class ChartSelectionManipulator : PointerManipulator
    {
        readonly Action<float> m_NotifyPosition;

        bool m_Active;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChartSelectionManipulator(Action<float> notifyPosition)
        {
            m_NotifyPosition = notifyPosition;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_Active = false;
        }

        public bool Disabled { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void StopCapturing(EventBase e)
        {
            m_Active = false;
            e.StopPropagation();
        }

        void OnPointerDown(PointerDownEvent e)
        {
            if (Disabled)
                return;

            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                target.CaptureMouse();
                m_Active = target.HasPointerCapture(e.pointerId);
                e.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (!m_Active)
                return;

            // We somehow lost capture
            if (!target.HasPointerCapture(e.pointerId))
            {
                StopCapturing(e);
                return;
            }

            var contentRect = target.contentRect;
            var pos = Math.Clamp(e.localPosition.x, contentRect.xMin, contentRect.xMax);

            m_NotifyPosition(pos);
            e.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (!m_Active)
                return;

            if (!target.HasPointerCapture(e.pointerId))
            {
                StopCapturing(e);
                return;
            }

            if (!CanStopManipulation(e))
                return;

            var contentRect = target.contentRect;
            var pos = Math.Clamp(e.localPosition.x, contentRect.xMin, contentRect.xMax);

            m_NotifyPosition(pos);

            target.ReleasePointer(e.pointerId);
            StopCapturing(e);
        }
    }
}
