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
    }

    public class DragPerformEvent : DragAndDropEventBase<DragPerformEvent>
    {
    }
}
