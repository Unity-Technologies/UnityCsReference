// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public enum Capture
    {
        NoCapture = 0,
        Capture = 1
    }

    internal enum CallbackPhase
    {
        TargetAndBubbleUp = 1 << 0,
        CaptureAndTarget = 1 << 1,
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
        public int capturingCallbackCount { get; private set; }
        public int bubblingCallbackCount { get; private set; }

        public EventCallbackList()
        {
            m_List = new List<EventCallbackFunctorBase>();
            capturingCallbackCount = 0;
            bubblingCallbackCount = 0;
        }

        public EventCallbackList(EventCallbackList source)
        {
            m_List = new List<EventCallbackFunctorBase>(source.m_List);
            capturingCallbackCount = 0;
            bubblingCallbackCount = 0;
        }

        public bool Contains(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                if (m_List[i].IsEquivalentTo(eventTypeId, callback, phase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Remove(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                if (m_List[i].IsEquivalentTo(eventTypeId, callback, phase))
                {
                    m_List.RemoveAt(i);

                    if (phase == CallbackPhase.CaptureAndTarget)
                    {
                        capturingCallbackCount--;
                    }
                    else if (phase == CallbackPhase.TargetAndBubbleUp)
                    {
                        bubblingCallbackCount--;
                    }

                    return true;
                }
            }
            return false;
        }

        public void Add(EventCallbackFunctorBase item)
        {
            m_List.Add(item);

            if (item.phase == CallbackPhase.CaptureAndTarget)
            {
                capturingCallbackCount++;
            }
            else if (item.phase == CallbackPhase.TargetAndBubbleUp)
            {
                bubblingCallbackCount++;
            }
        }

        public void AddRange(EventCallbackList list)
        {
            m_List.AddRange(list.m_List);

            foreach (var item in list.m_List)
            {
                if (item.phase == CallbackPhase.CaptureAndTarget)
                {
                    capturingCallbackCount++;
                }
                else if (item.phase == CallbackPhase.TargetAndBubbleUp)
                {
                    bubblingCallbackCount++;
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
            capturingCallbackCount = 0;
            bubblingCallbackCount = 0;
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

        bool UnregisterCallback(long eventTypeId, Delegate callback, Capture useCapture)
        {
            if (callback == null)
            {
                return false;
            }

            EventCallbackList callbackList = GetCallbackListForWriting();
            var callbackPhase = useCapture == Capture.Capture ? CallbackPhase.CaptureAndTarget : CallbackPhase.TargetAndBubbleUp;
            return callbackList.Remove(eventTypeId, callback, callbackPhase);
        }

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useCapture == Capture.Capture ? CallbackPhase.CaptureAndTarget : CallbackPhase.TargetAndBubbleUp;
            if (ShouldRegisterCallback(eventTypeId, callback, callbackPhase))
            {
                EventCallbackList callbackList = GetCallbackListForWriting();
                callbackList.Add(new EventCallbackFunctor<TEventType>(callback, callbackPhase));
            }
        }

        public void RegisterCallback<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useCapture == Capture.Capture ? CallbackPhase.CaptureAndTarget : CallbackPhase.TargetAndBubbleUp;
            if (ShouldRegisterCallback(eventTypeId, callback, callbackPhase))
            {
                EventCallbackList callbackList = GetCallbackListForWriting();
                callbackList.Add(new EventCallbackFunctor<TEventType, TCallbackArgs>(callback, userArgs, callbackPhase));
            }
        }

        public bool UnregisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            return UnregisterCallback(eventTypeId, callback, useCapture);
        }

        public bool UnregisterCallback<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            return UnregisterCallback(eventTypeId, callback, useCapture);
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

        public bool HasCaptureHandlers()
        {
            return m_Callbacks != null && m_Callbacks.capturingCallbackCount > 0;
        }

        public bool HasBubbleHandlers()
        {
            return m_Callbacks != null && m_Callbacks.bubblingCallbackCount > 0;
        }
    }
}
