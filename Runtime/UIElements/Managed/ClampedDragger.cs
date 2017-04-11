// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    internal class ClampedDragger : Clickable
    {
        [Flags]
        public enum DragDirection
        {
            None = 0,
            LowToHigh = 1 << 0,    // i.e. left-to-right, or top-to-bottom drag
            HighToLow = 1 << 1,    // i.e. right-to-left, or bottom-to-top
            Free = 1 << 2        // i.e. user is dragging using the drag element, free of any direction constraint
        }

        public event System.Action dragging;

        public DragDirection dragDirection { get; set; }

        Slider slider { get; set; }

        public Vector2 startMousePosition { get; private set; }

        public Vector2 delta
        {
            get { return lastMousePosition - startMousePosition; }
        }

        public ClampedDragger(Slider slider, System.Action clickHandler, System.Action dragHandler) :
            base(clickHandler, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait)
        {
            dragDirection = DragDirection.None;

            this.slider = slider;

            dragging += dragHandler;
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (CanStartManipulation(evt))
                    {
                        startMousePosition = evt.mousePosition;
                        dragDirection = DragDirection.None;
                        // Let base class Clickable handle the mouse event
                        base.HandleEvent(evt, finalTarget);
                        return EventPropagation.Stop;
                    }
                    break;

                case EventType.MouseDrag:
                    if (this.HasCapture())
                    {
                        // Let base class Clickable handle the mouse event first
                        // (although nothing much happens in the base class on mouse drags)
                        base.HandleEvent(evt, finalTarget);

                        // The drag element does the real work

                        // Take control if we can
                        if (dragDirection == DragDirection.None)
                            dragDirection = DragDirection.Free;

                        // If and when we have control, set value from drag element
                        if (dragDirection == DragDirection.Free)
                        {
                            if (dragging != null)
                                dragging();
                        }

                        return EventPropagation.Stop;
                    }
                    break;

                case EventType.MouseUp:
                    if (CanStopManipulation(evt))
                    {
                        // Let base class Clickable handle the mouse event
                        return base.HandleEvent(evt, finalTarget);
                    }
                    break;
            }
            return EventPropagation.Continue;
        }
    }
}
