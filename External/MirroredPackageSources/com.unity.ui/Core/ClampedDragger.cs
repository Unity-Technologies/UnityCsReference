using System;

namespace UnityEngine.UIElements
{
    internal class ClampedDragger<T> : Clickable
        where T : IComparable<T>
    {
        [Flags]
        public enum DragDirection
        {
            None = 0,
            LowToHigh = 1 << 0, // i.e. left-to-right, or bottom-to-top drag
            HighToLow = 1 << 1, // i.e. right-to-left, or top-to-bottom
            Free = 1 << 2 // i.e. user is dragging using the drag element, free of any direction constraint
        }

        public event System.Action dragging;

        public DragDirection dragDirection { get; set; }

        BaseSlider<T> slider { get; set; }

        public Vector2 startMousePosition { get; private set; }

        public Vector2 delta => (lastMousePosition - startMousePosition);

        public ClampedDragger(BaseSlider<T> slider, System.Action clickHandler, System.Action dragHandler)
            :
            base(clickHandler, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait)
        {
            dragDirection = DragDirection.None;

            this.slider = slider;

            dragging += dragHandler;
        }

        protected override void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            startMousePosition = localPosition;
            dragDirection = DragDirection.None;
            base.ProcessDownEvent(evt, localPosition, pointerId);
        }

        protected override void ProcessMoveEvent(EventBase evt, Vector2 localPosition)
        {
            // Let base class Clickable handle the mouse event first
            // (although nothing much happens in the base class on pointer drag)
            base.ProcessMoveEvent(evt, localPosition);

            // Take control if we can
            if (dragDirection == DragDirection.None)
                dragDirection = DragDirection.Free;

            // If and when we have control, set value from drag element
            if (dragDirection == DragDirection.Free)
            {
                if (evt.eventTypeId == PointerMoveEvent.TypeId())
                {
                    var pointerMoveEvent = (PointerMoveEvent)evt;
                    if (pointerMoveEvent.pointerId != PointerId.mousePointerId)
                        pointerMoveEvent.isHandledByDraggable = true;
                }

                dragging?.Invoke();
            }
        }
    }
}
