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
    }

    public class DragPerformEvent : DragAndDropEventBase<DragPerformEvent>
    {
    }
}
