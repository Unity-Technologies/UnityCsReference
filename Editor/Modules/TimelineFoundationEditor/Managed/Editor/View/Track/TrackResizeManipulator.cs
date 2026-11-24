// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class TrackResizeManipulator : PointerManipulator
    {
        bool m_Active;

        readonly TrackHeaderElement m_Header;

        public TrackResizeManipulator(TrackHeaderElement header)
        {
            m_Header = header;
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
                target.CapturePointer(e.pointerId);
                e.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (m_Active && target.HasPointerCapture(e.pointerId))
            {
                m_Header.ExtensionDragged_Internal(e.position);
                e.StopPropagation();
            }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (m_Active && target.HasPointerCapture(e.pointerId) && CanStopManipulation(e))
            {
                m_Active = false;
                target.ReleasePointer(e.pointerId);
                m_Header.ExtensionDropped_Internal(e.position);
                e.StopPropagation();
            }
        }
    }
}
