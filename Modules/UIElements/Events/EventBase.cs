// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class EventBase : IDisposable
    {
        private static long s_LastTypeId = 0;

        protected static long RegisterEventType() { return ++s_LastTypeId; }

        public abstract long GetEventTypeId();

        [Flags]
        protected internal enum EventFlags
        {
            None = 0,
            Bubbles = 1,
            TricklesDown = 2,
            [Obsolete("Use TrickesDown instead of Capturable")]
            Capturable = TricklesDown,
            Cancellable = 4,
        }

        [Flags]
        enum LifeCycleFlags
        {
            None = 0,
            PropagationStopped = 1,
            ImmediatePropagationStopped = 2,
            DefaultPrevented = 4,
            Dispatching = 8,
            Pooled = 16,
            IMGUIEventIsValid = 32,
            Dispatched = 512,
        }

        // Read-only state
        public long timestamp { get; private set; }

        protected EventFlags flags { get; set; }

        LifeCycleFlags lifeCycleFlags { get; set; }

        protected internal virtual void PreDispatch() {}

        protected internal virtual void PostDispatch() {}

        public bool bubbles
        {
            get { return (flags & EventFlags.Bubbles) != 0; }
        }

        [Obsolete("Use tricklesDown instead of capturable.")]
        public bool capturable { get { return tricklesDown; } }

        public bool tricklesDown
        {
            get { return (flags & EventFlags.TricklesDown) != 0; }
        }

        public IEventHandler target { get; set; }

        internal IEventHandler skipElement { get; set; }

        public bool isPropagationStopped
        {
            get { return (lifeCycleFlags & LifeCycleFlags.PropagationStopped) != LifeCycleFlags.None; }
            private set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.PropagationStopped;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.PropagationStopped;
                }
            }
        }

        public void StopPropagation()
        {
            isPropagationStopped = true;
        }

        public bool isImmediatePropagationStopped
        {
            get { return (lifeCycleFlags & LifeCycleFlags.ImmediatePropagationStopped) != LifeCycleFlags.None; }
            private set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.ImmediatePropagationStopped;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.ImmediatePropagationStopped;
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
            get { return (lifeCycleFlags & LifeCycleFlags.DefaultPrevented) != LifeCycleFlags.None; }
            private set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.DefaultPrevented;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.DefaultPrevented;
                }
            }
        }

        public void PreventDefault()
        {
            if ((flags & EventFlags.Cancellable) == EventFlags.Cancellable)
            {
                isDefaultPrevented = true;
            }
        }

        // Propagation state
        public PropagationPhase propagationPhase { get; internal set; }

        protected IEventHandler m_CurrentTarget;

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
                }
            }
        }

        public bool dispatch
        {
            get { return (lifeCycleFlags & LifeCycleFlags.Dispatching) != LifeCycleFlags.None; }
            internal set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.Dispatching;
                    dispatched = true;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.Dispatching;
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
            get { return (lifeCycleFlags & LifeCycleFlags.Dispatched) != LifeCycleFlags.None; }
            set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.Dispatched;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.Dispatched;
                }
            }
        }

        private Event m_ImguiEvent;
        bool imguiEventIsValid
        {
            get { return (lifeCycleFlags & LifeCycleFlags.IMGUIEventIsValid) != LifeCycleFlags.None; }
            set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.IMGUIEventIsValid;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.IMGUIEventIsValid;
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

        protected virtual void Init()
        {
            timestamp = (long)(Time.realtimeSinceStartup * 1000.0f);

            flags = EventFlags.None;

            target = null;

            skipElement = null;

            isPropagationStopped = false;
            isImmediatePropagationStopped = false;
            isDefaultPrevented = false;

            propagationPhase = PropagationPhase.None;

            originalMousePosition = Vector2.zero;
            m_CurrentTarget = null;

            dispatch = false;
            dispatched = false;
            imguiEventIsValid = false;
            pooled = false;
        }

        protected EventBase()
        {
            m_ImguiEvent = null;
            Init();
        }

        protected bool pooled
        {
            get { return (lifeCycleFlags & LifeCycleFlags.Pooled) != LifeCycleFlags.None; }
            set
            {
                if (value)
                {
                    lifeCycleFlags |= LifeCycleFlags.Pooled;
                }
                else
                {
                    lifeCycleFlags &= ~LifeCycleFlags.Pooled;
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

        public override void Dispose()
        {
            if (--m_RefCount == 0)
            {
                ReleasePooled((T)this);
            }
        }

        public override long GetEventTypeId()
        {
            return s_TypeId;
        }
    }

    // IPropagatableEvent event interface cause the EventDispatcher to propagate this event to the element hierarchy under the target.
    internal interface IPropagatableEvent
    {
    }
}
