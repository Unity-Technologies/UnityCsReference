// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for drag and drop events.
    /// </summary>
    /// <remarks>
    /// Drag and drop events are only available on Editor-type Panels.
    ///
    /// Refer to the [[wiki:UIE-Drag-Events|Drag-and-drop events]] manual page for more information and examples.
    /// </remarks>
    /// <seealso cref="DragEnterEvent"/>
    /// <seealso cref="DragExitedEvent"/>
    /// <seealso cref="DragLeaveEvent"/>
    /// <seealso cref="DragPerformEvent"/>
    /// <seealso cref="DragUpdatedEvent"/>
    /// <seealso cref="IPanel.contextType"/>
    public interface IDragAndDropEvent
    {
    }

    /// <summary>
    /// Base class for drag and drop events.
    /// </summary>
    /// <remarks>
    /// Drag and drop events are only available on Editor-type Panels.
    ///
    /// Refer to the [[wiki:UIE-Drag-Events|Drag-and-drop events]] manual page for more information and examples.
    /// </remarks>
    /// <seealso cref="DragEnterEvent"/>
    /// <seealso cref="DragExitedEvent"/>
    /// <seealso cref="DragLeaveEvent"/>
    /// <seealso cref="DragPerformEvent"/>
    /// <seealso cref="DragUpdatedEvent"/>
    /// <seealso cref="IPanel.contextType"/>
    [EventCategory(EventCategory.DragAndDrop)]
    public abstract class DragAndDropEventBase<T> : MouseEventBase<T>, IDragAndDropEvent where T : DragAndDropEventBase<T>, new()
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                          EventPropagation.SkipDisabledElements;
        }

        protected DragAndDropEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// The event sent to a dragged element when the drag and drop process ends.
    /// </summary>
    public class DragExitedEvent : DragAndDropEventBase<DragExitedEvent>
    {
        static DragExitedEvent()
        {
            SetCreateFunction(() => new DragExitedEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DragExitedEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI drag exited event.</param>
        /// <returns>An initialized event.</returns>
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
    }

    /// <summary>
    /// Use the DragEnterEvent class to manage events that occur when dragging enters an element or one of its descendants. The DragEnterEvent does not trickle down and does not bubble up.
    /// </summary>
    public class DragEnterEvent : DragAndDropEventBase<DragEnterEvent>
    {
        static DragEnterEvent()
        {
            SetCreateFunction(() => new DragEnterEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.SkipDisabledElements;
        }

        /// <summary>
        /// Constructor. Avoid renewing events. Instead, use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public DragEnterEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Use the DragLeaveEvent class to manage events sent when dragging leaves an element or one of its descendants. The DragLeaveEvent does not trickle down and does not bubble up.
    /// </summary>
    public class DragLeaveEvent : DragAndDropEventBase<DragLeaveEvent>
    {
        static DragLeaveEvent()
        {
            SetCreateFunction(() => new DragLeaveEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.SkipDisabledElements;
        }

        /// <summary>
        /// Constructor. Avoid renewing events. Instead, use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public DragLeaveEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// The event sent when the element being dragged enters a possible drop target.
    /// </summary>
    public class DragUpdatedEvent : DragAndDropEventBase<DragUpdatedEvent>
    {
        static DragUpdatedEvent()
        {
            SetCreateFunction(() => new DragUpdatedEvent());
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public DragUpdatedEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI drag updated event.</param>
        /// <returns>An initialized event.</returns>
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
    }

    /// <summary>
    /// The event sent to an element when another element is dragged and dropped on the element.
    /// </summary>
    public class DragPerformEvent : DragAndDropEventBase<DragPerformEvent>
    {
        static DragPerformEvent()
        {
            SetCreateFunction(() => new DragPerformEvent());
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public DragPerformEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
        }
    }
}
