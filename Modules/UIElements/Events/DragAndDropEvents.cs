// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public interface IDragAndDropEvent
    {
    }

    public abstract class DragAndDropEventBase<T> : MouseEventBase<T>, IDragAndDropEvent where T : DragAndDropEventBase<T>, new()
    {
    }

    public class DragExitedEvent : DragAndDropEventBase<DragExitedEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles;
        }

        public DragExitedEvent()
        {
            LocalInit();
        }

        public new static DragExitedEvent GetPooled(Event systemEvent)
        {
            // We get DragExitedEvent if the drag operation ends or if the mouse exits the window during the drag.
            // If drag operation ends, mouse was released, so notify PointerDeviceState about this.
            // If mouse exited window, we will eventually get a DragUpdatedEvent that will restore the pressed state.
            if (systemEvent != null)
            {
                PointerDeviceState.ReleaseButton(PointerId.mousePointerId, systemEvent.button);
            }

            return DragAndDropEventBase<DragExitedEvent>.GetPooled(systemEvent);
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            EventBase pointerEvent = ((IMouseEventInternal)this).sourcePointerEvent as EventBase;
            if (pointerEvent == null)
            {
                // If pointerEvent != null, base.PostDispatch() will take care of this.
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }
            base.PostDispatch(panel);
        }
    }

    public class DragEnterEvent : DragAndDropEventBase<DragEnterEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown;
        }

        public DragEnterEvent()
        {
            LocalInit();
        }
    }

    public class DragLeaveEvent : DragAndDropEventBase<DragLeaveEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown;
        }

        public DragLeaveEvent()
        {
            LocalInit();
        }
    }

    public class DragUpdatedEvent : DragAndDropEventBase<DragUpdatedEvent>
    {
        public new static DragUpdatedEvent GetPooled(Event systemEvent)
        {
            // During a drag operation, if mouse exits window, we get a DragExitedEvent, which releases the mouse button.
            // If the mouse comes back in the window, we get DragUpdatedEvents. We thus make sure the button is
            // flagged as pressed.
            if (systemEvent != null)
            {
                PointerDeviceState.PressButton(PointerId.mousePointerId, systemEvent.button);
            }

            // We adopt the same convention as for MouseMoveEvents.
            // We thus reset e.button.
            DragUpdatedEvent e = DragAndDropEventBase<DragUpdatedEvent>.GetPooled(systemEvent);
            e.button = 0;
            return e;
        }

        internal static DragUpdatedEvent GetPooled(PointerMoveEvent pointerEvent)
        {
            return DragAndDropEventBase<DragUpdatedEvent>.GetPooled(pointerEvent);
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            EventBase pointerEvent = ((IMouseEventInternal)this).sourcePointerEvent as EventBase;
            if (pointerEvent == null)
            {
                // If pointerEvent != null, base.PostDispatch() will take care of this.
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }
            base.PostDispatch(panel);
        }
    }

    public class DragPerformEvent : DragAndDropEventBase<DragPerformEvent>
    {
    }
}
