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
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            base.RegisterCallbacksOnTarget();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            base.UnregisterCallbacksFromTarget();
        }

        protected void OnPointerDown(PointerDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;

            if (IsNotMouseEvent(evt))
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
            if (IsNotMouseEvent(evt) && active)
            {
                ProcessMoveEvent(evt, evt.localPosition);
            }
        }

        protected void OnPointerUp(PointerUpEvent evt)
        {
            if (IsNotMouseEvent(evt) && active && CanStopManipulation(evt))
            {
                ProcessUpEvent(evt, evt.localPosition, evt.pointerId);
            }
        }

        protected void OnPointerCancel(PointerCancelEvent evt)
        {
            if (IsNotMouseEvent(evt) && CanStopManipulation(evt))
            {
                ProcessCancelEvent(evt, evt.pointerId);
            }
        }

        static bool IsNotMouseEvent<T>(T evt) where T : PointerEventBase<T>, new()
        {
            // We need to ignore temporarily mouse callback on mobile because they are sent with the wrong type.
            return evt.pointerId != PointerId.mousePointerId;
        }
    }
}
