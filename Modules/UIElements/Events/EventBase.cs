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
        public enum EventFlags
        {
            None = 0,
            Bubbles = 1,
            Capturable = 2,
            Cancellable = 4,
            Pooled = 256
        }

        // Read-only state
        public long timestamp { get; private set; }

        protected EventFlags flags { get; set; }

        public bool bubbles
        {
            get { return (flags & EventFlags.Bubbles) != 0; }
        }

        public bool capturable
        {
            get { return (flags & EventFlags.Capturable) != 0; }
        }

        public IEventHandler target { get; internal set; }

        public bool isPropagationStopped { get; private set; }

        public void StopPropagation()
        {
            isPropagationStopped = true;
        }

        public bool isImmediatePropagationStopped { get; private set; }

        public void StopImmediatePropagation()
        {
            isPropagationStopped = true;
            isImmediatePropagationStopped = true;
        }

        public bool isDefaultPrevented { get; private set; }

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
                        imguiEvent.mousePosition = element.WorldToLocal(m_OriginalMousePosition);
                    }
                }
            }
        }

        public bool dispatch { get; internal set; }


        private Event m_ImguiEvent;

        // We aim to make this internal.
        public /*internal*/ Event imguiEvent
        {
            get { return m_ImguiEvent; }
            protected set
            {
                m_ImguiEvent = value;
                if (m_ImguiEvent != null)
                {
                    originalMousePosition = value.mousePosition; // when assigned, it is assumed that the imguievent is not touched and therefore in world coordinates.
                }
            }
        }

        Vector2 m_OriginalMousePosition;

        public Vector2 originalMousePosition
        {
            get
            {
                return m_OriginalMousePosition;
            }
            private set { m_OriginalMousePosition = value; }
        }

        protected virtual void Init()
        {
            timestamp = DateTime.Now.Ticks;

            flags = EventFlags.None;

            target = null;

            isPropagationStopped = false;
            isImmediatePropagationStopped = false;
            isDefaultPrevented = false;

            propagationPhase = PropagationPhase.None;

            m_CurrentTarget = null;

            dispatch = false;
            imguiEvent = null;
            originalMousePosition = Vector2.zero;
        }

        protected EventBase()
        {
            Init();
        }

        public abstract void Dispose();
    }

    public abstract class EventBase<T> : EventBase where T : EventBase<T>, new()
    {
        static readonly long s_TypeId = RegisterEventType();
        static readonly EventPool<T> s_Pool = new EventPool<T>();

        public static long TypeId()
        {
            return s_TypeId;
        }

        public static T GetPooled()
        {
            T t = s_Pool.Get();
            t.Init();
            t.flags |= EventFlags.Pooled;
            return t;
        }

        // If you want to release, call Dispose instead.
        protected static void ReleasePooled(T evt)
        {
            if ((evt.flags & EventFlags.Pooled) == EventFlags.Pooled)
            {
                s_Pool.Release(evt);
                // To avoid double release from pool
                evt.flags &= ~EventFlags.Pooled;
                // Set target to null before pooling to avoid leaking VisualElement
                evt.target = null;
            }
        }

        public override void Dispose()
        {
            ReleasePooled((T)this);
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
