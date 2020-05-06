using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Use this enum to specify during which phases the event handler is executed.
    /// </summary>
    public enum TrickleDown
    {
        /// <summary>
        /// The event handler should be executed during the AtTarget and BubbleUp phases.
        /// </summary>
        NoTrickleDown = 0,
        /// <summary>
        /// The event handler should be executed during the AtTarget and BubbleUp phases.
        /// </summary>
        TrickleDown = 1
    }

    internal enum CallbackPhase
    {
        TargetAndBubbleUp = 1 << 0,
        TrickleDownAndTarget = 1 << 1
    }

    internal class EventCallbackListPool
    {
        readonly Stack<EventCallbackList> m_Stack = new Stack<EventCallbackList>();

        public EventCallbackList Get(EventCallbackList initializer)
        {
            EventCallbackList element;
            if (m_Stack.Count == 0)
            {
                if (initializer != null)
                    element = new EventCallbackList(initializer);
                else
                    element = new EventCallbackList();
            }
            else
            {
                element = m_Stack.Pop();
                if (initializer != null)
                    element.AddRange(initializer);
            }
            return element;
        }

        public void Release(EventCallbackList element)
        {
            element.Clear();
            m_Stack.Push(element);
        }
    }

    internal class EventCallbackList
    {
        List<EventCallbackFunctorBase> m_List;
        public int trickleDownCallbackCount { get; private set; }
        public int bubbleUpCallbackCount { get; private set; }

        public EventCallbackList()
        {
            m_List = new List<EventCallbackFunctorBase>();
            trickleDownCallbackCount = 0;
            bubbleUpCallbackCount = 0;
        }

        public EventCallbackList(EventCallbackList source)
        {
            m_List = new List<EventCallbackFunctorBase>(source.m_List);
            trickleDownCallbackCount = 0;
            bubbleUpCallbackCount = 0;
        }

        public bool Contains(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return Find(eventTypeId, callback, phase) != null;
        }

        public EventCallbackFunctorBase Find(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                if (m_List[i].IsEquivalentTo(eventTypeId, callback, phase))
                {
                    return m_List[i];
                }
            }
            return null;
        }

        public bool Remove(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                if (m_List[i].IsEquivalentTo(eventTypeId, callback, phase))
                {
                    m_List.RemoveAt(i);

                    if (phase == CallbackPhase.TrickleDownAndTarget)
                    {
                        trickleDownCallbackCount--;
                    }
                    else if (phase == CallbackPhase.TargetAndBubbleUp)
                    {
                        bubbleUpCallbackCount--;
                    }

                    return true;
                }
            }
            return false;
        }

        public void Add(EventCallbackFunctorBase item)
        {
            m_List.Add(item);

            if (item.phase == CallbackPhase.TrickleDownAndTarget)
            {
                trickleDownCallbackCount++;
            }
            else if (item.phase == CallbackPhase.TargetAndBubbleUp)
            {
                bubbleUpCallbackCount++;
            }
        }

        public void AddRange(EventCallbackList list)
        {
            m_List.AddRange(list.m_List);

            foreach (var item in list.m_List)
            {
                if (item.phase == CallbackPhase.TrickleDownAndTarget)
                {
                    trickleDownCallbackCount++;
                }
                else if (item.phase == CallbackPhase.TargetAndBubbleUp)
                {
                    bubbleUpCallbackCount++;
                }
            }
        }

        public int Count
        {
            get { return m_List.Count; }
        }

        public EventCallbackFunctorBase this[int i]
        {
            get { return m_List[i]; }
            set { m_List[i] = value; }
        }

        public void Clear()
        {
            m_List.Clear();
            trickleDownCallbackCount = 0;
            bubbleUpCallbackCount = 0;
        }
    }

    internal class EventCallbackRegistry
    {
        private static readonly EventCallbackListPool s_ListPool = new EventCallbackListPool();

        private static EventCallbackList GetCallbackList(EventCallbackList initializer = null)
        {
            return s_ListPool.Get(initializer);
        }

        private static void ReleaseCallbackList(EventCallbackList toRelease)
        {
            s_ListPool.Release(toRelease);
        }

        private EventCallbackList m_Callbacks;
        private EventCallbackList m_TemporaryCallbacks;
        int m_IsInvoking;

        public EventCallbackRegistry()
        {
            m_IsInvoking = 0;
        }

        EventCallbackList GetCallbackListForWriting()
        {
            if (m_IsInvoking > 0)
            {
                if (m_TemporaryCallbacks == null)
                {
                    if (m_Callbacks != null)
                    {
                        m_TemporaryCallbacks = GetCallbackList(m_Callbacks);
                    }
                    else
                    {
                        m_TemporaryCallbacks = GetCallbackList();
                    }
                }

                return m_TemporaryCallbacks;
            }
            else
            {
                if (m_Callbacks == null)
                {
                    m_Callbacks = GetCallbackList();
                }

                return m_Callbacks;
            }
        }

        EventCallbackList GetCallbackListForReading()
        {
            if (m_TemporaryCallbacks != null)
            {
                return m_TemporaryCallbacks;
            }

            return m_Callbacks;
        }

        bool ShouldRegisterCallback(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            if (callback == null)
            {
                return false;
            }

            EventCallbackList callbackList = GetCallbackListForReading();
            if (callbackList != null)
            {
                return !callbackList.Contains(eventTypeId, callback, phase);
            }

            return true;
        }

        bool UnregisterCallback(long eventTypeId, Delegate callback, TrickleDown useTrickleDown)
        {
            if (callback == null)
            {
                return false;
            }

            EventCallbackList callbackList = GetCallbackListForWriting();
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDownAndTarget : CallbackPhase.TargetAndBubbleUp;
            return callbackList.Remove(eventTypeId, callback, callbackPhase);
        }

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDownAndTarget : CallbackPhase.TargetAndBubbleUp;

            EventCallbackList callbackList = GetCallbackListForReading();
            if (callbackList == null || callbackList.Contains(eventTypeId, callback, callbackPhase) == false)
            {
                callbackList = GetCallbackListForWriting();
                callbackList.Add(new EventCallbackFunctor<TEventType>(callback, callbackPhase));
            }
        }

        public void RegisterCallback<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDownAndTarget : CallbackPhase.TargetAndBubbleUp;

            EventCallbackList callbackList = GetCallbackListForReading();
            if (callbackList != null)
            {
                var functor = callbackList.Find(eventTypeId, callback, callbackPhase) as EventCallbackFunctor<TEventType, TCallbackArgs>;
                if (functor != null)
                {
                    functor.userArgs = userArgs;
                    return;
                }
            }
            callbackList = GetCallbackListForWriting();
            callbackList.Add(new EventCallbackFunctor<TEventType, TCallbackArgs>(callback, userArgs, callbackPhase));
        }

        public bool UnregisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            return UnregisterCallback(eventTypeId, callback, useTrickleDown);
        }

        public bool UnregisterCallback<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            return UnregisterCallback(eventTypeId, callback, useTrickleDown);
        }

        internal bool TryGetUserArgs<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TrickleDown useTrickleDown, out TCallbackArgs userArgs) where TEventType : EventBase<TEventType>, new()
        {
            userArgs = default(TCallbackArgs);

            if (callback == null)
                return false;

            EventCallbackList list = GetCallbackListForReading();
            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDownAndTarget : CallbackPhase.TargetAndBubbleUp;
            var functor = list.Find(eventTypeId, callback, callbackPhase) as EventCallbackFunctor<TEventType, TCallbackArgs>;

            if (functor == null)
                return false;

            userArgs = functor.userArgs;

            return true;
        }

        public void InvokeCallbacks(EventBase evt)
        {
            if (m_Callbacks == null)
            {
                return;
            }

            m_IsInvoking++;

            for (var i = 0; i < m_Callbacks.Count; i++)
            {
                if (evt.isImmediatePropagationStopped)
                    break;

                m_Callbacks[i].Invoke(evt);
            }

            m_IsInvoking--;

            if (m_IsInvoking == 0)
            {
                // If callbacks were modified during callback invocation, update them now.
                if (m_TemporaryCallbacks != null)
                {
                    ReleaseCallbackList(m_Callbacks);
                    m_Callbacks = GetCallbackList(m_TemporaryCallbacks);
                    ReleaseCallbackList(m_TemporaryCallbacks);
                    m_TemporaryCallbacks = null;
                }
            }
        }

        public bool HasTrickleDownHandlers()
        {
            return m_Callbacks != null && m_Callbacks.trickleDownCallbackCount > 0;
        }

        public bool HasBubbleHandlers()
        {
            return m_Callbacks != null && m_Callbacks.bubbleUpCallbackCount > 0;
        }
    }
}
