// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IDragAndDropEvent
    {
    }

    public abstract class DragAndDropEventBase<T> : MouseEventBase<T>, IDragAndDropEvent, IPropagatableEvent where T : DragAndDropEventBase<T>, new()
    {
    }

    public class DragExitedEvent : DragAndDropEventBase<DragExitedEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.TricklesDown | EventFlags.Bubbles;
        }

        public DragExitedEvent()
        {
        }
    }

    public class DragEnterEvent : DragAndDropEventBase<DragEnterEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.TricklesDown;
        }

        public DragEnterEvent()
        {
        }
    }

    public class DragLeaveEvent : DragAndDropEventBase<DragLeaveEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.TricklesDown;
        }

        public DragLeaveEvent()
        {
            Init();
        }
    }

    public class DragUpdatedEvent : DragAndDropEventBase<DragUpdatedEvent>
    {
    }

    public class DragPerformEvent : DragAndDropEventBase<DragPerformEvent>
    {
    }
}
