using System;

namespace UnityEngine.UIElements
{
    internal class PointerClickable : Clickable
    {
        public PointerClickable(Action handler) : base(handler) {}
        public PointerClickable(Action<EventBase> handler) : base(handler) {}
        public PointerClickable(Action handler, long delay, long interval) : base(handler, delay, interval) {}

        public Vector2 lastPointerPosition
        {
            get { return lastMousePosition; }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            base.RegisterCallbacksOnTarget();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            base.UnregisterCallbacksFromTarget();
        }

        protected void OnPointerDown(PointerDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;

            if (evt.pointerId != PointerId.mousePointerId)
            {
                ProcessDownEvent(evt, evt.localPosition, evt.pointerId);
                evt.PreventDefault();
            }
            else
            {
                evt.StopImmediatePropagation();
            }
        }

        protected void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != PointerId.mousePointerId && active)
            {
                ProcessMoveEvent(evt, evt.localPosition);
            }
        }

        protected void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != PointerId.mousePointerId && active && CanStopManipulation(evt))
            {
                ProcessUpEvent(evt, evt.localPosition, evt.pointerId);
            }
        }
    }
}
