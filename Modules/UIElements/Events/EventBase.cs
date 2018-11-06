// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

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
        }

        // Read-only state
        public long timestamp { get; private set; }

        internal EventPropagation propagation { get; set; }

        LifeCycleStatus lifeCycleStatus { get; set; }

        protected internal virtual void PreDispatch() {}

        protected internal virtual void PostDispatch() {}

        public bool bubbles
        {
            get { return (propagation & EventPropagation.Bubbles) != 0; }
        }

        public bool tricklesDown
        {
            get { return (propagation & EventPropagation.TricklesDown) != 0; }
        }

        public IEventHandler target { get; set; }

        internal IEventHandler skipElement { get; set; }

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

        protected virtual void Init()
        {
            LocalInit();
        }

        void LocalInit()
        {
            timestamp = (long)(Time.realtimeSinceStartup * 1000.0f);

            propagation = EventPropagation.None;

            target = null;

            skipElement = null;

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
            imguiEventIsValid = false;
            pooled = false;
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
