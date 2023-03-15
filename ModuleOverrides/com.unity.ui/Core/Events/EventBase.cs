// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The base class for all UIElements events.
    /// </summary>
    public abstract class EventBase : IDisposable
    {
        private static long s_LastTypeId = 0;

        /// <summary>
        /// Registers an event class to the event type system.
        /// </summary>
        /// <returns>The type ID.</returns>
        protected static long RegisterEventType() { return ++s_LastTypeId; }

        /// <summary>
        /// Retrieves the type ID for this event instance.
        /// </summary>
        public virtual long eventTypeId => - 1;

        [Flags]
        internal enum EventPropagation
        {
            None = 0,
            Bubbles = 1,
            TricklesDown = 2,
            Cancellable = 4,
            SkipDisabledElements = 8,
            IgnoreCompositeRoots = 16,
        }

        [Flags]
        enum LifeCycleStatus
        {
            None = 0,
            PropagationStopped = 1,
            ImmediatePropagationStopped = 2,
            DefaultPrevented = 4,
            Dispatching = 8,
            Pooled = 16,
            IMGUIEventIsValid = 32,
            StopDispatch = 64,
            PropagateToIMGUI = 128,
            Dispatched = 512,
            Processed = 1024,
            ProcessedByFocusController = 2048,
        }

        internal EventCategory eventCategory { get; }


        static ulong s_NextEventId = 0;

        // Read-only state
        /// <summary>
        /// The time when the event was created, in milliseconds.
        /// </summary>
        /// <remarks>
        /// This value is relative to the start time of the current application.
        /// </remarks>
        public long timestamp { get; private set; }

        internal ulong eventId { get; private set; }

        internal ulong triggerEventId { get; private set; }

        internal void SetTriggerEventId(ulong id)
        {
            triggerEventId = id;
        }

        internal EventPropagation propagation { get; set; }

        internal PropagationPaths path { get; set; }

        LifeCycleStatus lifeCycleStatus { get; set; }

        /// <undoc/>
        [Obsolete("Override PreDispatch(IPanel panel) instead.")]
        protected virtual void PreDispatch() {}

        /// <summary>
        /// Allows subclasses to perform custom logic before the event is dispatched.
        /// </summary>
        /// <param name="panel">The panel where the event will be dispatched.</param>
        protected internal virtual void PreDispatch(IPanel panel)
        {
#pragma warning disable 618
            PreDispatch();
#pragma warning restore 618
        }

        /// <undoc/>
        [Obsolete("Override PostDispatch(IPanel panel) instead.")]
        protected virtual void PostDispatch() {}

        /// <summary>
        /// Allows subclasses to perform custom logic after the event has been dispatched.
        /// </summary>
        /// <param name="panel">The panel where the event has been dispatched.</param>
        protected internal virtual void PostDispatch(IPanel panel)
        {
#pragma warning disable 618
            PostDispatch();
#pragma warning restore 618
            processed = true;
        }

        /// <summary>
        /// Returns whether this event type bubbles up in the event propagation path.
        /// </summary>
        public bool bubbles
        {
            get { return (propagation & EventPropagation.Bubbles) != 0; }
            protected set
            {
                if (value)
                {
                    propagation |= EventPropagation.Bubbles;
                }
                else
                {
                    propagation &= ~EventPropagation.Bubbles;
                }
            }
        }

        /// <summary>
        /// Returns whether this event is sent down the event propagation path during the TrickleDown phase.
        /// </summary>
        public bool tricklesDown
        {
            get { return (propagation & EventPropagation.TricklesDown) != 0; }
            protected set
            {
                if (value)
                {
                    propagation |= EventPropagation.TricklesDown;
                }
                else
                {
                    propagation &= ~EventPropagation.TricklesDown;
                }
            }
        }

        internal bool bubblesOrTricklesDown =>
            (propagation & (EventPropagation.Bubbles | EventPropagation.TricklesDown)) != 0;

        internal bool skipDisabledElements
        {
            get { return (propagation & EventPropagation.SkipDisabledElements) != 0; }
            set
            {
                if (value)
                {
                    propagation |= EventPropagation.SkipDisabledElements;
                }
                else
                {
                    propagation &= ~EventPropagation.SkipDisabledElements;
                }
            }
        }

        internal bool ignoreCompositeRoots
        {
            get { return (propagation & EventPropagation.IgnoreCompositeRoots) != 0; }
            set
            {
                if (value)
                {
                    propagation |= EventPropagation.IgnoreCompositeRoots;
                }
                else
                {
                    propagation &= ~EventPropagation.IgnoreCompositeRoots;
                }
            }
        }

        // Original target. May be different than 'target' when propagating event and 'target.isCompositeRoot' is true.
        internal IEventHandler leafTarget { get; private set; }

        IEventHandler m_Target;

        /// <summary>
        /// The target visual element that received this event. Unlike currentTarget, this target does not change when the event is sent to other elements along the propagation path.
        /// </summary>
        public IEventHandler target
        {
            get { return m_Target; }
            set
            {
                m_Target = value;
                if (leafTarget == null)
                {
                    leafTarget = value;
                }
            }
        }

        internal List<IEventHandler> skipElements { get; } = new List<IEventHandler>();

        internal bool Skip(IEventHandler h)
        {
            return skipElements.Contains(h);
        }

        /// <summary>
        /// Whether StopPropagation() was called for this event.
        /// </summary>
        public bool isPropagationStopped
        {
            get { return (lifeCycleStatus & LifeCycleStatus.PropagationStopped) != LifeCycleStatus.None; }
            private set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.PropagationStopped;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.PropagationStopped;
                }
            }
        }

        /// <summary>
        /// Stops propagating this event. The event is not sent to other elements along the propagation path. This method does not prevent other event handlers from executing on the current target.
        /// </summary>
        public void StopPropagation()
        {
            isPropagationStopped = true;
        }

        /// <summary>
        /// Indicates whether StopImmediatePropagation() was called for this event.
        /// </summary>
        public bool isImmediatePropagationStopped
        {
            get { return (lifeCycleStatus & LifeCycleStatus.ImmediatePropagationStopped) != LifeCycleStatus.None; }
            private set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.ImmediatePropagationStopped;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.ImmediatePropagationStopped;
                }
            }
        }

        /// <summary>
        /// Immediately stops the propagation of the event. The event isn't sent to other elements along the propagation path. This method prevents other event handlers from executing on the current target.
        /// </summary>
        public void StopImmediatePropagation()
        {
            isPropagationStopped = true;
            isImmediatePropagationStopped = true;
        }

        /// <summary>
        /// Returns true if the default actions should not be executed for this event.
        /// </summary>
        public bool isDefaultPrevented
        {
            get { return (lifeCycleStatus & LifeCycleStatus.DefaultPrevented) != LifeCycleStatus.None; }
            private set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.DefaultPrevented;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.DefaultPrevented;
                }
            }
        }

        /// <summary>
        /// Indicates whether the default actions are prevented from being executed for this event.
        /// </summary>
        public void PreventDefault()
        {
            if ((propagation & EventPropagation.Cancellable) == EventPropagation.Cancellable)
            {
                isDefaultPrevented = true;
            }
        }

        // Propagation state
        /// <summary>
        /// The current propagation phase for this event. 
        /// </summary>
        public PropagationPhase propagationPhase { get; internal set; }

        IEventHandler m_CurrentTarget;

        /// <summary>
        /// The current target of the event. This is the VisualElement, in the propagation path, for which event handlers are currently being executed.
        /// </summary>
        public virtual IEventHandler currentTarget
        {
            get { return m_CurrentTarget; }
            internal set
            {
                m_CurrentTarget = value;

                if (imguiEvent != null)
                {
                    var element = currentTarget as VisualElement;
                    if (element != null)
                    {
                        imguiEvent.mousePosition = element.WorldToLocal(originalMousePosition);
                    }
                    else
                    {
                        imguiEvent.mousePosition = originalMousePosition;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the event is being dispatched to a visual element. An event cannot be redispatched while it being dispatched. If you need to recursively dispatch an event, it is recommended that you use a copy of the event.
        /// </summary>
        public bool dispatch
        {
            get { return (lifeCycleStatus & LifeCycleStatus.Dispatching) != LifeCycleStatus.None; }
            internal set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.Dispatching;
                    dispatched = true;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.Dispatching;
                }
            }
        }

        internal void MarkReceivedByDispatcher()
        {
            Debug.Assert(dispatched == false, "Events cannot be dispatched more than once.");
            dispatched = true;
        }

        bool dispatched
        {
            get { return (lifeCycleStatus & LifeCycleStatus.Dispatched) != LifeCycleStatus.None; }
            set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.Dispatched;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.Dispatched;
                }
            }
        }

        internal bool processed
        {
            get { return (lifeCycleStatus & LifeCycleStatus.Processed) != LifeCycleStatus.None; }
            private set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.Processed;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.Processed;
                }
            }
        }

        internal bool processedByFocusController
        {
            get { return (lifeCycleStatus & LifeCycleStatus.ProcessedByFocusController) != LifeCycleStatus.None; }
            set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.ProcessedByFocusController;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.ProcessedByFocusController;
                }
            }
        }

        internal bool stopDispatch
        {
            get { return (lifeCycleStatus & LifeCycleStatus.StopDispatch) != LifeCycleStatus.None; }
            set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.StopDispatch;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.StopDispatch;
                }
            }
        }

        internal bool propagateToIMGUI
        {
            get { return (lifeCycleStatus & LifeCycleStatus.PropagateToIMGUI) != LifeCycleStatus.None; }
            set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.PropagateToIMGUI;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.PropagateToIMGUI;
                }
            }
        }

        private Event m_ImguiEvent;

        // Since we recycle events (in their pools) and we do not free/reallocate a new imgui event
        // at each recycling (m_ImguiEvent is never null), we use this flag to know whether m_ImguiEvent
        // represents a valid Event.
        bool imguiEventIsValid
        {
            get { return (lifeCycleStatus & LifeCycleStatus.IMGUIEventIsValid) != LifeCycleStatus.None; }
            set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.IMGUIEventIsValid;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.IMGUIEventIsValid;
                }
            }
        }

        // We aim to make this internal.
        /// <summary>
        /// The IMGUIEvent at the source of this event. The source can be null since not all events are generated by IMGUI.
        /// </summary>
        public /*internal*/ Event imguiEvent
        {
            get { return imguiEventIsValid ? m_ImguiEvent : null; }
            protected set
            {
                if (m_ImguiEvent == null)
                {
                    m_ImguiEvent = new Event();
                }

                if (value != null)
                {
                    m_ImguiEvent.CopyFrom(value);
                    imguiEventIsValid = true;
                    originalMousePosition = value.mousePosition; // when assigned, it is assumed that the imguievent is not touched and therefore in world coordinates.
                }
                else
                {
                    imguiEventIsValid = false;
                }
            }
        }

        /// <summary>
        /// The original mouse position of the IMGUI event, before it is transformed to the current target local coordinates.
        /// </summary>
        public Vector2 originalMousePosition { get; private set; }

        internal EventDebugger eventLogger { get; set; }

        internal bool log => eventLogger != null;

        /// <summary>
        /// Resets all event members to their initial values.
        /// </summary>
        protected virtual void Init()
        {
            LocalInit();
        }

        void LocalInit()
        {
            timestamp = Panel.TimeSinceStartupMs();

            triggerEventId = 0;
            eventId = s_NextEventId++;

            propagation = EventPropagation.None;

            path?.Release();
            path = null;
            leafTarget = null;
            target = null;

            skipElements.Clear();

            isPropagationStopped = false;
            isImmediatePropagationStopped = false;
            isDefaultPrevented = false;

            propagationPhase = PropagationPhase.None;

            originalMousePosition = Vector2.zero;
            m_CurrentTarget = null;

            dispatch = false;
            stopDispatch = false;
            propagateToIMGUI = true;

            dispatched = false;
            processed = false;
            processedByFocusController = false;
            imguiEventIsValid = false;
            pooled = false;

            eventLogger = null;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        protected EventBase() : this(EventCategory.Default)
        {
        }

        internal EventBase(EventCategory category)
        {
            eventCategory = category;
            m_ImguiEvent = null;
            LocalInit();
        }

        /// <summary>
        /// Whether the event is allocated from a pool of events.
        /// </summary>
        protected bool pooled
        {
            get { return (lifeCycleStatus & LifeCycleStatus.Pooled) != LifeCycleStatus.None; }
            set
            {
                if (value)
                {
                    lifeCycleStatus |= LifeCycleStatus.Pooled;
                }
                else
                {
                    lifeCycleStatus &= ~LifeCycleStatus.Pooled;
                }
            }
        }

        internal abstract void Acquire();
        /// <summary>
        /// Implementation of IDisposable.
        /// </summary>
        public abstract void Dispose();
    }

    /// <summary>
    /// Generic base class for events, implementing event pooling and automatic registration to the event type system.
    /// </summary>
    [EventCategory(EventCategory.Default)]
    public abstract class EventBase<T> : EventBase where T : EventBase<T>, new()
    {
        static readonly long s_TypeId = RegisterEventType();
        static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(() => new T());

        internal static void SetCreateFunction(Func<T> createMethod)
        {
            s_Pool.CreateFunc = createMethod;
        }

        int m_RefCount;

        protected EventBase() : base(EventCategory)
        {
            m_RefCount = 0;
        }

        /// <summary>
        /// Retrieves the type ID for this event instance.
        /// </summary>
        /// <returns>The type ID.</returns>
        public static long TypeId()
        {
            return s_TypeId;
        }

        internal static readonly EventCategory EventCategory = EventInterestReflectionUtils.GetEventCategory(typeof(T));

        /// <summary>
        /// Resets all event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();

            if (m_RefCount != 0)
            {
                Debug.Log("Event improperly released.");
                m_RefCount = 0;
            }
        }

        /// <summary>
        /// Gets an event from the event pool. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <returns>An initialized event.</returns>
        public static T GetPooled()
        {
            T t = s_Pool.Get();
            t.Init();
            t.pooled = true;
            t.Acquire();
            return t;
        }

        internal static T GetPooled(EventBase e)
        {
            T t = GetPooled();
            if (e != null)
            {
                t.SetTriggerEventId(e.eventId);
            }
            return t;
        }

        static void ReleasePooled(T evt)
        {
            if (evt.pooled)
            {
                // Reset the event before pooling to avoid leaking VisualElement
                evt.Init();

                s_Pool.Release(evt);

                // To avoid double release from pool
                evt.pooled = false;
            }
        }

        internal override void Acquire()
        {
            m_RefCount++;
        }

        /// <summary>
        /// Implementation of IDispose.
        /// </summary>
        /// <remarks>
        /// If the event was instantiated from an event pool, the event is released when Dispose is called.
        /// </remarks>
        public sealed override void Dispose()
        {
            if (--m_RefCount == 0)
            {
                ReleasePooled((T)this);
            }
        }

        /// <summary>
        /// Retrieves the type ID for this event instance.
        /// </summary>
        public override long eventTypeId => s_TypeId;
    }
}
