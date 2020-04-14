// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public abstract class EventBase : IDisposable
    {
        private static long s_LastTypeId = 0;

        protected static long RegisterEventType() { return ++s_LastTypeId; }

        public virtual long eventTypeId => - 1;

        [Flags]
        internal enum EventPropagation
        {
            None = 0,
            Bubbles = 1,
            TricklesDown = 2,
            Cancellable = 4,
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
        }

        static ulong s_NextEventId = 0;

        // Read-only state
        public long timestamp { get; private set; }

        internal ulong eventId { get; private set; }

        internal ulong triggerEventId { get; private set; }

        internal void SetTriggerEventId(ulong id)
        {
            triggerEventId = id;
        }

        internal EventPropagation propagation { get; set; }

        PropagationPaths m_Path;
        internal PropagationPaths path
        {
            get
            {
                if (m_Path == null)
                {
                    PropagationPaths.Type pathTypesRequested = (tricklesDown ? PropagationPaths.Type.TrickleDown : PropagationPaths.Type.None);
                    pathTypesRequested |= (bubbles ? PropagationPaths.Type.BubbleUp : PropagationPaths.Type.None);
                    m_Path = PropagationPaths.Build(leafTarget as VisualElement, pathTypesRequested);
                    EventDebugger.LogPropagationPaths(this, m_Path);
                }

                return m_Path;
            }
            set
            {
                if (value != null)
                    m_Path = PropagationPaths.Copy(value);
            }
        }

        LifeCycleStatus lifeCycleStatus { get; set; }

        [Obsolete("Override PreDispatch(IPanel panel) instead.")]
        protected virtual void PreDispatch() {}

        protected internal virtual void PreDispatch(IPanel panel)
        {
#pragma warning disable 618
            PreDispatch();
#pragma warning restore 618
        }

        [Obsolete("Override PostDispatch(IPanel panel) instead.")]
        protected virtual void PostDispatch() {}

        protected internal virtual void PostDispatch(IPanel panel)
        {
#pragma warning disable 618
            PostDispatch();
#pragma warning restore 618
            processed = true;
        }

        public bool bubbles
        {
            get { return (propagation & EventPropagation.Bubbles) != 0; }
        }

        public bool tricklesDown
        {
            get { return (propagation & EventPropagation.TricklesDown) != 0; }
        }

        // Original target. May be different than 'target' when propagating event and 'target.isCompositeRoot' is true
        internal IEventHandler leafTarget { get; private set; }

        IEventHandler m_Target;

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

        public void StopPropagation()
        {
            isPropagationStopped = true;
        }

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

        public void StopImmediatePropagation()
        {
            isPropagationStopped = true;
            isImmediatePropagationStopped = true;
        }

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

        public void PreventDefault()
        {
            if ((propagation & EventPropagation.Cancellable) == EventPropagation.Cancellable)
            {
                isDefaultPrevented = true;
            }
        }

        // Propagation state
        public PropagationPhase propagationPhase { get; internal set; }

        IEventHandler m_CurrentTarget;

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

        public Vector2 originalMousePosition { get; private set; }

        internal EventDebugger eventLogger { get; set; }

        internal bool log => eventLogger != null;

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

            m_Path?.Release();
            m_Path = null;
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
            imguiEventIsValid = false;
            pooled = false;

            eventLogger = null;
        }

        protected EventBase()
        {
            m_ImguiEvent = null;
            LocalInit();
        }

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
        public abstract void Dispose();
    }

    public abstract class EventBase<T> : EventBase where T : EventBase<T>, new()
    {
        static readonly long s_TypeId = RegisterEventType();
        static readonly ObjectPool<T> s_Pool = new ObjectPool<T>();

        int m_RefCount;

        protected EventBase()
        {
            m_RefCount = 0;
        }

        public static long TypeId()
        {
            return s_TypeId;
        }

        protected override void Init()
        {
            base.Init();

            if (m_RefCount != 0)
            {
                Debug.Log("Event improperly released.");
                m_RefCount = 0;
            }
        }

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

        public sealed override void Dispose()
        {
            if (--m_RefCount == 0)
            {
                ReleasePooled((T)this);
            }
        }

        public override long eventTypeId => s_TypeId;
    }
}
